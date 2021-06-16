using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials.Messaging;
using ServerMod;
using Slime;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Scripts.Specials.StartShipSpawner
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SpawnerSession : SessionComponentWithSettings<CoreSettings>
    {
        public static SpawnerSession Instance;
        public static Random random = new Random();
        private static List<IMyPlayer> m_tempPlayersList = new List<IMyPlayer>();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            Instance = this;
            UnPackCooldowns(Instance.Settings);
            SaveData();
        }

        public override void SaveData()
        {
            PackCooldowns(Instance.Settings);
            base.SaveData();
        }

        protected override CoreSettings GetDefault()
        {
            var planets = new List<string> { "EarthLike2", "EarthMedieval" };
            var spawnPoints = new List<SpawnPoint>() { new SpawnPoint()
            {
                Center = Vector3.Zero,
                Radius = 3000f,
                IsPlanetOnly = false
            }};
            var places = new SpawnPlaces()
            {
                Planets = planets,
                SpawnPoints = spawnPoints
            };
            var spawnBehavior = new SpawnBehaviorVariant()
            {
                BehaviorName = "ExampleBehavior",
                Prefab = "RESPAWN_ROVER",
                Position = Vector3D.Zero,
                CooldownInMinutes = 1,
                CanSpawnInSpace = true,
                CanSpawnOnPlanets = true,
                MinAboveVoxels = 10,
                MaxAboveVoxels = 100,
                MinEnemyDistance = 1000,
                ResetIdentity = false,
                MaxPlayerRadius = 100
            };
            var spawnBehaviors = new List<SpawnBehaviorVariant>()
            {
                spawnBehavior
            };
            var cooldowns = new List<CooldownForXML>();
            return new CoreSettings()
            {
                MessageType = MessageType.Request,
                SpawnBehaviors = spawnBehaviors,
                SpawnCooldownsXMLList = cooldowns,
                Places = places
            };
        }

        protected override string GetFileName() { return "SpawnerSettings"; }

        public void TrySpawn(Vector3D triggerPos, string name)
        {
            var beh = Settings.SpawnBehaviors.Where(x => x.BehaviorName.Contains(name));

            if (beh != null && beh.Any())
            {
                var behavior = beh.FirstOrDefault();
                var players = GameBase.instance.characters.Where(x => (x.Value.GetPosition() - triggerPos).LengthSquared() < behavior.MaxPlayerRadius * behavior.MaxPlayerRadius);

                if (players != null && players.Any())
                {
                    var player = players.FirstOrDefault().Value.GetPlayer();
                    if (CheckOrSetCooldown(behavior.CooldownInMinutes, name, player.IdentityId))
                    {
                        ToPlayer("Cooldown!", player.IdentityId);
                        return;
                    }
                    else
                    {
                        ToLog($"Spawn starship for {player.DisplayName}, behavior: {name}");
                        TrySpawnInternal(behavior, player);
                    }
                }
            }
            else
            {
                ToLog($"Behavior with name [{name}] not found! Check your config!");
                return;
            }
        }

        private void PackCooldowns(CoreSettings settings)
        {
            settings.SpawnCooldownsXMLList.Clear();
            foreach (var temp in settings.SpawnCooldowns)
            {
                settings.SpawnCooldownsXMLList.Add(new CooldownForXML(temp.Key, temp.Value));
            }
        }

        private void UnPackCooldowns(CoreSettings settings)
        {
            if (settings.SpawnCooldownsXMLList == null)
            {
                settings.SpawnCooldownsXMLList = new List<CooldownForXML>();
                return;
            }

            if (settings.SpawnCooldowns == null)
            {
                settings.SpawnCooldowns = new Dictionary<long, DateTime>();
                return;
            }

            var currentTime = DateTime.Now.ToUniversalTime();
            foreach (var temp in settings.SpawnCooldownsXMLList)
            {
                if (!settings.SpawnCooldowns.ContainsKey(temp.PlayerID))
                {
                    if (temp.DateTime <= currentTime)
                    {
                        continue; //skip old cooldowns
                    }
                    settings.SpawnCooldowns.Add(temp.PlayerID, temp.DateTime);
                }
            }
        }

        /// <summary>
        /// Check for cooldown, if not adds new and do cleanup. return true if cooldown
        /// </summary>
        /// <param name="cooldown">In minutes</param>
        /// <param name="name">Behavior name</param>
        /// <param name="playerID">Player entity id </param>
        /// <returns></returns>
        private bool CheckOrSetCooldown(int cooldown, string name, long playerID)
        {
            var currentTime = DateTime.Now.ToUniversalTime();
            if (Settings.SpawnCooldowns.ContainsKey(playerID))
            {
                if (Settings.SpawnCooldowns[playerID] >= currentTime)
                {
                    return true;
                }
                else
                {
                    Settings.SpawnCooldowns.Remove(playerID);
                }
            }

            var usecooldown = new TimeSpan(0, 0, cooldown, 0);
            Settings.SpawnCooldowns.Add(playerID, currentTime + usecooldown);
            return false;
        }

        private void TrySpawnInternal(SpawnBehaviorVariant spawnVariant, IMyPlayer player)
        {
            try
            {
                var owner = player.IdentityId;
                MyPrefabDefinition prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(spawnVariant.Prefab);
                var spawnAreaCenterTemp = CoordinatesFunction(spawnVariant, prefabDefinition, owner);
                if (spawnAreaCenterTemp == null || spawnAreaCenterTemp == Vector3D.Zero)
                {
                    ToLog("Failed to find place to spawn.");
                    ToPlayer("Fail to find good place.", player.IdentityId);
                    return;
                }

                Vector3D spawnAreaCenter = (Vector3D)spawnAreaCenterTemp;
                bool inGravity = GameBase.IsInNaturalGravity(spawnAreaCenter);
                var pl = GameBase.GetClosestPlanet(spawnAreaCenter);
                var up = spawnAreaCenter - pl.WorldMatrix.Translation;
                up.Normalize();

                var spawned = prefabDefinition.spawnPrefab(spawnAreaCenter, Vector3.CalculatePerpendicularVector(up), up, 0, (ob) =>
                 {
                     if (ob is MyObjectBuilder_CubeGrid)
                     {
                         var grid = ob as MyObjectBuilder_CubeGrid;
                         grid.IsPowered = true;
                         grid.IsStatic = true;
                         grid.IsRespawnGrid = true;

                         foreach (var block in grid.CubeBlocks)
                         {
                             block.BuiltBy = owner;
                             block.Owner = owner;
                         }
                     }
                     else
                     {
                         ToLog("MyObjectBuilder is not MyObjectBuilder_CubeGrid ! real type = " + ob.GetType().ToString());
                     }
                 },
                (grid) =>
                {
                    var player_pos_matrix = Matrix.CreateWorld(spawnAreaCenter, Vector3.Forward, up);
                    player_pos_matrix.Translation = player_pos_matrix.Translation + player_pos_matrix.Forward * 1 + player_pos_matrix.Up * 11;
                    player.Character.Teleport(player_pos_matrix);

                    foreach (var block in grid.GetFatBlocks())
                    {
                        if (block is MyCockpit)
                        {
                            MyAPIGateway.Parallel.StartBackground(() =>
                           {
                               MyAPIGateway.Parallel.Sleep(1000); //absolutely needed or fail to attach
                               MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                              {
                                  ((IMyCockpit)block).AttachPilot(player.Character);
                                  grid.IsStatic = false;
                              });
                           });
                            return;
                        }
                    }
                }, MyOwnershipShareModeEnum.Faction);
            }
            catch (Exception e)
            {
                ToLog($"[spawnship] spawn catch {e}");
                ToLog(e.ToString());
            }
        }

        private bool IsNearSafezone(Vector3 vector, float extra = 0f)
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

        private Vector3? CoordinatesFunction(SpawnBehaviorVariant spawnVariant, MyPrefabDefinition prefab, long playerID)
        {
            List<SpawnPoint> points = new List<SpawnPoint>();
            Vector3? VectorForSpawn = null;
            Instance.Settings.Places.GetAllSpawnPoints(points);
            ToLog($"Use behavior: [{spawnVariant.BehaviorName}], spawn points: [{points.Count}]");
            if (points.Count == 0)
            {
                ToLog($"Spawn points not found in [{spawnVariant.BehaviorName}], check your config!");
                return null;
            }
            for (var attempts = 100; attempts > 0; attempts--)
            {
                //idea from MyPlanet GetClosestPlanet
                var spawnPoint = points.MinBy((x) => (float)((Vector3D.DistanceSquared(x.Center, spawnVariant.Position) - (x.Radius * x.Radius))));
                ToLog($"Point to spawn center: [{spawnPoint.Center}]");
                var vector = spawnPoint.Center + random.NextVector(spawnPoint.Radius, spawnPoint.Radius, spawnPoint.Radius);

                if (IsNearSafezone(vector, 3000)) continue;

                var pl = GameBase.GetClosestPlanet(vector);
                if (pl == null)
                {
                    if (!spawnVariant.CanSpawnInSpace) continue;
                    VectorForSpawn = vector;
                }
                else
                {
                    if (!spawnVariant.CanSpawnOnPlanets) continue;
                    vector = pl.GetClosestSurfacePointGlobal(vector);
                    var v = (vector - pl.WorldMatrix.Translation);
                    VectorForSpawn = vector;
                }

                if (VectorForSpawn != null)
                {
                    var air = pl.GetAirDensity((Vector3D)VectorForSpawn);
                    if (air < 0.95) continue;
                    //Enemy player check
                    m_tempPlayersList.Clear();
                    MyAPIGateway.Multiplayer.Players.GetPlayers(m_tempPlayersList);
                    foreach (var p in m_tempPlayersList)
                    {
                        if (p.PromoteLevel >= MyPromoteLevel.SpaceMaster) continue; //ignore admins.

                        if (MyIDModule.GetRelationPlayerPlayer(p.Identity.IdentityId, playerID) == VRage.Game.Entity.MyRelationsBetweenPlayers.Enemies)
                        {
                            if ((p.GetPosition() - (Vector3)VectorForSpawn).LengthSquared() < 3000 * 3000)
                            {
                                continue; //enemy too close
                            }
                        }
                    }

                    VectorForSpawn = MyAPIGateway.Entities.FindFreePlace((Vector3)VectorForSpawn, prefab.BoundingBox.Size.Max() / 2);
                    if (VectorForSpawn != null && VectorForSpawn != Vector3D.Zero) // null check is needed because FindFreePlace can return null
                    {
                        break;
                    }
                }
            }
            return VectorForSpawn;
        }

        public static void ToPlayer(string msg, long playerID)
        {
            Common.SendChatMessage(msg, "StartShipSpawner", playerID);
        }

        public static void ToLog(string msg)
        {
            var isServer = MyAPIGateway.Session.IsServer;
            string pref = "[SpawnerCore][Client] ";
            if (isServer)
            {
                pref = "[SpawnerCore][Server] ";
            }

            if (MyLog.Default != null)
            {
                MyLog.Default.Error(pref + msg);
            }
            // Common.SendChatMessage(pref + msg, "SpawnerCore");
        }
    }
}
