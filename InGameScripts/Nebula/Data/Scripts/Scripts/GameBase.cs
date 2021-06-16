using System;
using System.Collections.Generic;
using Digi;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Game;
using ServerMod.Radar;
using Sandbox.Game.Entities;
using Scripts;
using Scripts.Specials.Economy;
using Scripts.Specials.AutoTools;
using Scripts.Specials.GPS;
using Scripts.Specials;
using Draygo.API;
using Scripts.Shared;
using Scripts.Specials.Blocks;
using ServerMod.Specials;
using Scripts.Specials.Doors;
using Scripts.Specials.SlimGarage;
using Digi2.AeroWings;
using Scripts.Specials.Blocks.Cockpit;
using Scripts.Specials.Safezones;
using Scripts.Specials.Trader;
using VRageMath;
using Slime;
using Scripts.Specials.POI;
using Scripts.Specials.StartShipSpawner;

namespace ServerMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class GameBase : MySessionComponentBase {
        public static HudAPIv2 HudAPI;
        public static Random r = new Random ();
        public static bool isDedicated = false;
        public static GameBase instance = null;
        
        public readonly Dictionary<long, MyPlanet> planets = new Dictionary<long, MyPlanet>();
        public readonly Dictionary<long, Ship> gridToShip = new Dictionary<long, Ship>();
        public readonly Dictionary<long, IMyCharacter> characters = new Dictionary<long, IMyCharacter>();

        public bool worldLoaded = false;
        private bool needRemoveFrameLogic = false;
 
        private List<Ship> addlist = new List<Ship>();
        private List<long> removelist = new List<long>();

        private List<Action> unloadActions = new List<Action>();
        
        public GameBase () { instance = this; }

        public static MyPlanet GetPlanetWithGeneratorId (string GeneratorName)
        {
            foreach (var x in GameBase.instance.planets)
            {
                if (x.Value.Generator.Id.SubtypeName == GeneratorName)
                {
                    return x.Value;
                }
            }
            return null;
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent) {
            base.Init(sessionComponent);

            isDedicated = MyAPIGateway.Session.isTorchServer();
            
            FrameExecutor.addFrameLogic(new OnlineFactions());
            //FrameExecutor.addFrameLogic(new PlayerGenerator());
            

            FactionSafeZoneBlock.Init();

            Zeppelin.Init();

            HudAPI = new HudAPIv2();
            GPSGroup.Init();
            
            CustomControl.Init();
            ThrowBlock.Init();
            CockpitUpgrade.Init();

            CockpitUserInput.Init();
            RealisticThruster.InitHandlers();
            

            CockpitUpgrade3.Init();

            LooseMoneyOnDeath.Init();
            DoorsAPI.Init();
            RefillEnergy.Init();
            LastLoginTracker.Init();
            BankingSystem.Init();
            CustomDamageSystem.Init();
            AutoTools.Init();
            CharacterTeleporter.Init();
            EMP.Init();
            SlimGarage.Init();
            Eleron.Init();
            BlockReactionsOnKeys.Init();
            AbstractTrader.Init();
            ArmorRebalance.Init();
            TorchExtensions.Init();
            Thruster360.Init();

            UpgraderTool.Init();

            AntiEscapeCreativeSystem.Init ();
            POICore.Init();
            SpawnerBlockLogic.InitNetworking();

            MyAPIGateway.Session.OnSessionReady += OnSessionReady;
			
			CapturableSafeZone.Initialize();
		}

        public static bool IsInNaturalGravity (Vector3 vector)
        {
            var bb = new BoundingBoxD (vector, vector);
            foreach (var x in instance.planets)
            {
                if (x.Value.IntersectsWithGravityFast(ref bb)) {
                    return true;
                }
            }
            return false;
        }

        public static MyPlanet GetClosestPlanet(Vector3 vector)
        {
            var bb = new BoundingBoxD(vector, vector);
            foreach (var x in instance.planets)
            {
                if (x.Value.IntersectsWithGravityFast(ref bb))
                {
                    return x.Value;
                }
            }
            return null;
        }


        public void OnPlanetAdded (MyPlanet planet) {
            //if (planet.Generator.Id.SubtypeName == "EarthLike") {
            //    AntiEscapeCreativeSystem.AddRestrictedArea (planet.WorldMatrix.Translation, planet.AtmosphereRadius);
            //}
        }
        
        public override void LoadData() {
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded; 
        }
        
        protected override void UnloadData() { 
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
            RadarClient.Close();
            AutoTools.Unload();
            GPSGroup.Close();
            DoorsAPI.Unload();
            RefillEnergy.Close();
            ThrowBlock.Close();
            CockpitUserInput.Close();
            RealisticThruster.CloseHandlers();
            CharacterTeleporter.Close();
            EMP.Close();
            POICore.Close();
            SlimGarage.Unload();

            foreach (var x in unloadActions)
            {
                try
                {
                    x.Invoke();
                } catch (Exception e)
                {
                    Log.ChatError(e);
                }
                
            }
            //TerminalBlockOptions.Unload();
            //AutoOreCollect.Unload();
            //AutoOreCollect.Unload();
        }

        public static void AddUnloadAction (Action a)
        {
            instance.unloadActions.Add (a);
        }
        
        public override void SaveData() {
            base.SaveData();
            if (MyAPIGateway.Multiplayer.IsServer) 
            {
                LastLoginTracker.Save();
                POICore.Save();
            }
        }

        private void OnSessionReady() {
            if (!worldLoaded) {
                worldLoaded = true;
            }
        }
        
        private void OnEntityAdded(IMyEntity ent) {
            try
            {
                var grid = ent as IMyCubeGrid;
                if (grid != null)
                {
                    if (!gridToShip.ContainsKey(grid.EntityId))
                    {
                        Ship shipGrid = new Ship(grid);
                        addlist.Add(shipGrid);
                        grid.OnMarkForClose += OnMarkForClose;
                        return;
                    }
                }

                var p = ent as MyPlanet;
                if (p != null)
                {
                    if (!planets.ContainsKey(p.EntityId))
                    {
                        planets.Add(ent.EntityId, p);
                        OnPlanetAdded(p);
                        p.OnMarkForClose += OnMarkForClose;
                        return;
                    }
                }

                var c = ent as IMyCharacter;
                if (c != null)
                {
                    if (!characters.ContainsKey(c.EntityId))
                    {
                        characters.Add(c.EntityId, c);
                        c.OnMarkForClose += OnMarkForClose;
                    }
                    else
                    {
                        characters[c.EntityId] = c;
                        c.OnMarkForClose += OnMarkForClose;
                    }
                }
            } catch (Exception e)
            {
                Log.ChatError (e);
            }
        }

        private void OnMarkForClose(IMyEntity ent) {
            if (ent is IMyCubeGrid) {
                removelist.Add(ent.EntityId);
                ent.OnMarkForClose -= OnMarkForClose;
            }

            if (ent is MyPlanet) {
                planets.Remove(ent.EntityId);
                ent.OnMarkForClose -= OnMarkForClose;
            }

            if (ent is IMyCharacter)
            {
                var c = ent as IMyCharacter;
                characters.Remove(c.EntityId);
                c.OnMarkForClose -= OnMarkForClose;
            }
        }
        
        
        public override void UpdateBeforeSimulation() {
            //MyVisualScriptLogicProvider.FogSetAll(0.9999f, 0.9999f, Color.Gray, 0.5f, 0.9f);
            
            //MyTransparentGeometry.AddBillboardOrientedCull(MyAPIGateway.Session.Camera.Position, MyStringId.GetOrCompute("Square"), Color.PaleGreen*0.7f, MyAPIGateway.Session.Camera.Position + (MyAPIGateway.Session.Camera.WorldMatrix.Forward * 10), MyAPIGateway.Session.Camera.WorldMatrix.Left, MyAPIGateway.Session.Camera.WorldMatrix.Up, 1);

            
            FrameExecutor.Update();
            //bool type = FrameExecutor.currentFrame % 30 == 0;
            foreach (var x in gridToShip.Values) {
                try {
                    x.BeforeSimulation();
                } catch (Exception e) {
                    Log.ChatError("GB: Update ERROR ", e);
                }
            }
            
            try {
                foreach (var x in addlist) {
                    if (x == null) {
                        Log.Error("GB: Add: NULL SHIP");
                        continue;
                    }

                    if (x.grid == null) {
                        Log.Error("GB: Add: NULL GRID SHIP");
                        continue;
                    }

                    if (!gridToShip.ContainsKey(x.grid.EntityId)) {
                        gridToShip.Add(x.grid.EntityId, x);
                    } else {
                        gridToShip[x.grid.EntityId] = x;
                    }
                    
                }
                addlist.Clear();
                
                
                foreach (var x in removelist) {
                    gridToShip.Remove(x);
                }
                removelist.Clear();
                
            } catch (Exception e) {
                Log.ChatError("GB: Delete or Add: ", e);
            }
            
            try {
                
            } catch (Exception e) {
                Log.ChatError("GB: Delete", e);
            }
        }

        public override void UpdateAfterSimulation() {
            base.UpdateAfterSimulation();
            
            foreach (var x in gridToShip.Values) {
                x.AfterSimulation();
            }

            //if (FrameExecutor.currentFrame % 100 == 1)
            //{
            //    Log.ChatError("Current frame:" + TorchExtensions.CurrentFrame());
            //}
            //HungerGames.Tick();
        }

        public override void Draw()
        {
            base.Draw();
            foreach (var x in gridToShip.Values)
            {
                x.Draw();
            }

            //HungerGames.Draw();
        }

    }


   
}