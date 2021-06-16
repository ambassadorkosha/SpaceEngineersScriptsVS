using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Shared;
using ServerMod;
using Slime;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Specials.Dungeon.AirDrops
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class AirDropSystem
        : SessionComponentWithSettings<List<AirDropSettings>>
    {
        static MyObjectBuilderType Component = MyObjectBuilderType.Parse("MyObjectBuilder_Component");
        public static string GPSmark = "~airdropgpsid:";
        public static Random random = new Random();
        private static int counter = 0;
        private static Dictionary<long, AirDropGPSItem> GPSCache = new Dictionary<long, AirDropGPSItem>();
        private static List<IMyPlayer> playersGPSCache = new List<IMyPlayer>();
        private static bool _playerInit = false;
        private static int MAXPENDINGDROPS = 999;
        protected override List<AirDropSettings> GetDefault()
        {
            return InitDefaultSettings();
        }
        protected override string GetFileName() { return "AirDrops"; }


        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            if (MyAPIGateway.Session.IsServer)
            {
                try
                {
                    foreach (var item in Settings)
                    {
                        Log.Info($"[AIR DROP] Init drop rule: {item.Gps.GPSName}");
                        var actTime = random.Next(item.MinTriggerIntervalInSeconds * 60, item.MaxTriggerIntervalInSeconds * 60);
                        var delayTime = random.Next(item.MinFirstTriggerDelayInSeconds * 60, item.MaxFirstTriggerDelayInSeconds * 60);
                        item.SpawnTimer = new AutoTimer(actTime, delayTime);
                        Log.Info($"[AIR DROP] Activation in: {(delayTime / 60)} seconds, interval set to: {(actTime / 60)}");
                    }
                }
                catch (Exception e)
                {
                    Log.ChatError(e);
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                if (!_playerInit && MyAPIGateway.Session?.Player?.Character != null)
                {
                    _playerInit = true;
                    ClearOldGPS();
                }
            }

            if (MyAPIGateway.Session.IsServer)
            {
                try
                {
                    foreach (var x in Settings)
                    {
                        if (x.SpawnTimer.tick())
                        {
                            Log.Info($"[AIR DROP] {x.Gps.GPSName} triggered!");
                            var actTime = random.Next(x.MinTriggerIntervalInSeconds * 60, x.MaxTriggerIntervalInSeconds * 60);
                            x.SpawnTimer = new AutoTimer(actTime, actTime);
                            Log.Info($"[AIR DROP] Next activation in: {(actTime / 60)} seconds, interval set to: {(actTime / 60)}");
                            if (x.Prefabs.Count == 0)
                            {
                                Log.Info($"[AIR DROP] Prefabs not found!");
                                return;
                            }
                            var prefab = random.NextWithChance(x.Prefabs, (yx) => yx.Chance, true);
                            var pos = CoordinatesFunction(x, prefab);
                            if (!pos.HasValue)
                            {
                                Log.Info($"[AIR DROP] Position for spawn not found!");
                                return;
                            }
                            var spawnAt = pos.Value;
                            var gps = x.Gps;
                            if (gps != null)
                            {
                                //Entry point, AirDrop will dropped in 00:00:00
                                if (GPSCache.Count >= MAXPENDINGDROPS)
                                {
                                    Log.Info($"[AIR DROP] Max pending AirDrops reached!");
                                    return;
                                }
                                var id = GenUnicId();
                                string unicdesc = GPSmark + id + "~";
                                Log.Info($"[AIR DROP] add GPS id: {id}");
                                MyVisualScriptLogicProvider.AddGPSForAll(gps.GPSName + " spawn soon.", unicdesc, spawnAt, gps.GPSColor);
                                FrameExecutor.addDelayedLogic(x.BeforeSpawnDelayInSeconds * 60, new DelayedSpawn(x, prefab, spawnAt));
                                GPSCache.Add(id, new AirDropGPSItem()
                                { GPSName = gps.GPSName, Position = spawnAt, Color = gps.GPSColor, GPSDescription = unicdesc, AfterSpawnRemoveDelay = x.AfterSpawnDelayInSeconds, SpawnTime = DateTime.UtcNow + TimeSpan.FromSeconds(x.BeforeSpawnDelayInSeconds) }); ;
                            }
                            else
                            {
                                Spawn(x, prefab, spawnAt);
                            }
                        }
                    }

                    if (counter++ % (60) == 0)
                    {
                        UpdateGPS();
                    }
                }
                catch (Exception e)
                {
                    Log.ChatError(e);
                }
            }
        }

        private static void ClearOldGPS()
        {
            Gps.RemoveWithDescription(GPSmark, MyAPIGateway.Session.Player.IdentityId, true);
            Log.Info($"[AIR DROP] Clear old GPS");
        }

        private static void UpdateGPS()
        {
            playersGPSCache.Clear();
            MyAPIGateway.Players.GetPlayers(playersGPSCache);
            var currtime = DateTime.UtcNow;
            var toRem = new List<long>(2);
            bool needDel = false;
            bool spawnedStage = false;
            foreach (var item in GPSCache)
            {
                var diff = (item.Value.SpawnTime - currtime).StripMilliseconds();
                if (item.Value.SpawnTime <= currtime)
                {
                    if ((item.Value.SpawnTime + TimeSpan.FromSeconds(item.Value.AfterSpawnRemoveDelay)) < currtime)
                    {
                        toRem.Add(item.Key);
                        needDel = true; //need del gps
                    }
                    else
                    {
                        spawnedStage = true; //drop spawned
                    }
                }

                string gpsName;
                if (spawnedStage)
                {
                    gpsName = item.Value.GPSName + " spawned.";
                }
                else
                {
                    gpsName = item.Value.GPSName + $" spawn in {diff:hh\\:mm\\:ss}";
                }

                foreach (var x in playersGPSCache)
                {
                    var gps = Gps.GetWithDescription(item.Value.GPSDescription, x.IdentityId);
                    if (gps != null)
                    {
                        if (needDel)
                        {
                            MyAPIGateway.Session.GPS.RemoveGps(x.IdentityId, gps);
                            continue;
                        }
                        gps.Name = gpsName;
                        MyAPIGateway.Session.GPS.ModifyGps(x.IdentityId, gps); //or re add if fail
                    }
                    else
                    {
                        MyVisualScriptLogicProvider.AddGPS(gpsName, item.Value.GPSDescription, item.Value.Position, item.Value.Color, 0, x.IdentityId);
                    }
                }
            }

            foreach (var id in toRem)
            {
                GPSCache.Remove(id);
            }
        }

        private static long GenUnicId()
        {
            var id = random.Next(1, MAXPENDINGDROPS);
            while (GPSCache.ContainsKey(id))
            {
                id = random.Next(1, MAXPENDINGDROPS);
            }

            return id;
        }

        public List<AirDropSettings> InitDefaultSettings()
        {
            AirDropSettings settings = new AirDropSettings();

            settings.Gps = new AirDropGps()
            {
                GPSName = "AirDrop",
                GPSColor = Color.Red
            };

            settings.MinTriggerIntervalInSeconds = 60 * 3;
            settings.MaxTriggerIntervalInSeconds = 60 * 6;
            settings.MinFirstTriggerDelayInSeconds = 120;
            settings.MaxFirstTriggerDelayInSeconds = 240;
            settings.BeforeSpawnDelayInSeconds = 60 * 5;
            settings.AfterSpawnDelayInSeconds = 60 * 5;

            settings.SpawnOptions.Planets.Add("EarthLike2");
            settings.SpawnOptions.Planets.Add("EarthMedieval");
            settings.SpawnOptions.SpawnPoints.Add(new SpawnPoint()
            {
                Center = Vector3.Zero,
                Radius = 3000f,
                IsPlanetOnly = false
            });

            settings.Prefabs = new List<AirDropVariant>() {
                new AirDropVariant () {
                    Prefab = "airdrop_1",
                    Chance = 0.5f,
                    SpecificLoot = new List<LootGroup>() {
                        new LootGroup()
                        {
                            Chance = 0.2f,
                            Loot = new List<LootVaraint>()
                            {
                                new LootVaraint()
                                {
                                    Id = "Ore/Uranium",
                                    Min = 1111,
                                    Max = 2222
                                },
                            }
                        }
                    }
                }
            };

            settings.Loot = new List<LootGroup>() {
                new LootGroup () {
                    Chance = 0.2f,
                    Loot = new List<LootVaraint>()
                    {
                        new LootVaraint()
                        {
                            Id = "Ore/Ice",
                            Min = 10000,
                            Max = 100000
                        },

                        new LootVaraint()
                        {
                            Id = "Ore/Iron",
                            Min = 666,
                            Max = 999
                        }
                    }
                }
            };

            var list = new List<AirDropSettings>();
            list.Add(settings);
            return list;
        }

        private class DelayedSpawn : Action1<long>
        {
            private AirDropSettings s;
            private Vector3 at;
            private AirDropVariant prefab;

            public DelayedSpawn(AirDropSettings s, AirDropVariant prefab, Vector3 at)
            {
                this.s = s;
                this.at = at;
                this.prefab = prefab;
            }

            public void run(long t)
            {
                AirDropSystem.Spawn(s, prefab, at);
            }
        }


        private static void Spawn(AirDropSettings s, AirDropVariant prefab, Vector3 spawnAt)
        {
            try
            {
                bool inGravity = GameBase.IsInNaturalGravity(spawnAt);
                var prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(prefab.Prefab);
                var items = new Dictionary<MyDefinitionId, double>();
                if (prefab.SpecificLoot != null)
                {
                    var extraLoot = random.NextWithChance(prefab.SpecificLoot, (x) => x.Chance, true);
                    if (extraLoot != null && extraLoot.Loot != null)
                    {
                        foreach (var y in extraLoot.Loot)
                        {
                            var am = random.NextDouble(y.Min, y.Max);
                            if (y.Definition.TypeId == Component) am = (int)am;
                            items.Sum(y.Definition, am);
                        }

                    }
                }

                if (s.Loot != null)
                {
                    var loot = random.NextWithChance(s.Loot, (x) => x.Chance, true);
                    if (loot != null && loot.Loot != null)
                    {
                        foreach (var y in loot.Loot)
                        {
                            var am = random.NextDouble(y.Min, y.Max);
                            if (y.Definition.TypeId == Component) am = (int)am;
                            items.Sum(y.Definition, am);
                        }
                    }
                }

                var owner = s.Ownership;
                if (s.Ownership == -1) owner = Relations.FindBot("Space Pirates");

                var spawned = prefabDefinition.spawnPrefab(spawnAt, Vector3.Forward, Vector3.Up, owner, (x) =>
                {
                    x.IsStatic = inGravity ? true : false;
                    var inventories = new List<IMyInventory>();
                    foreach (var y in x.GetFatBlocks())
                    {
                        if (y is IMyCargoContainer)
                        {
                            for (var z = 0; z < y.InventoryCount; z++)
                            {
                                inventories.Add(y.GetInventory(z));
                            }
                        }
                    }

                    inventories.AddItems(items);
                }, VRage.Game.MyOwnershipShareModeEnum.None);

                Log.Info($"[AIR DROP]spawned = {spawned}");
            }
            catch (Exception e)
            {
                Log.Info($"[AIR DROP] AirDrop spawned at {e}");
                Log.ChatError(e);
            }
        }

        public static bool IsNearSafezone(Vector3 vector, float extra = 0f)
        {
            foreach (var x in MySessionComponentSafeZones.SafeZones)
            {
                if ((vector - x.WorldMatrix.Translation).Length() < extra + (x.Shape == MySafeZoneShape.Box ? (x.Size.Max()) : x.Radius))
                {
                    return true;
                }
            }
            return false;
        }

        public static Vector3? CoordinatesFunction(AirDropSettings s, AirDropVariant dropVariant)
        {
            List<SpawnPoint> points = new List<SpawnPoint>();
            s.SpawnOptions.GetAllSpawnPoints(points);
            if (points.Count == 0) return null;
            //return random.NextVector(s.Radius, s.Radius, s.Radius);
            for (var attempts = 10; attempts > 0; attempts--)
            {
                var spawnPoint = random.Next(points);
                var vector = spawnPoint.Center + random.NextVector(spawnPoint.Radius, spawnPoint.Radius, spawnPoint.Radius);

                if (IsNearSafezone(vector, 1000)) continue;

                var pl = GameBase.GetClosestPlanet(vector);
                if (pl == null)
                {
                    if (!dropVariant.CanSpawnInSpace) continue;
                    return vector;
                }
                else
                {
                    if (!dropVariant.CanSpawnOnPlanets) continue;
                    vector = pl.GetClosestSurfacePointGlobal(vector);
                    var v = (vector - pl.WorldMatrix.Translation);
                    return vector;
                }
            }
            return null;
        }
    }
}
