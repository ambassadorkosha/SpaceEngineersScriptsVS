using Digi;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts;
using Scripts.Specials.Radar;
using Scripts.Specials.Undone;
using ServerMod.Specials;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;
using Digi2.AeroWings;
using Scripts.Specials.ShipClass;
using NebulaPhysics;
using Scripts.Specials;
using Scripts.Specials.Wings;
using Slime;
using Scripts.Specials.Blocks.StackableMultipliers;
using Sandbox.Game;
using Scripts.Shared;
using VRage.Game.Components;
using Scripts.Specials.Blocks;
using Scripts.Specials.ExtraInfo;
using Scripts.Specials.Blocks.ShipSkills;
using Scripts.Specials.ShipSkills;

namespace ServerMod
{

	public class GridRemover : Action1<long> {
            Ship ship;
            bool subgrids = false;

            public GridRemover(Ship ship, bool subgrids = false) {
                this.ship = ship;
                this.subgrids = subgrids;
            }

            public void run(long k) {
                ship.SafeDeleteGrid(subgrids);
            }
        }

    public class Protection {
        public long protectedUntill;
        public bool canPlaceBlocks;
        public Dictionary<long, long> protectors = new Dictionary<long, long>();

        public void AddBuildProtector(long id, long untill) {
            if (protectors.ContainsKey(id)) {
                protectors[id] = untill;
            } else {
                protectors.Add(id, untill);
            }
        }
    }
    
    public class Ship {

        public IMyCubeGrid grid;
        public IMyCubeGrid largestOfSubgrids;

        public Protection protection = new Protection();
        public MyPlanet closestPlanet;
        public ShipInfo radarInfo = new ShipInfo().Init();
        public HashSet<MyShipController> Cockpits = new HashSet<MyShipController>();
        public HashSet<BlockReactionsOnKeys> BlocksWithReactions = new HashSet<BlockReactionsOnKeys>();

		
		public HashSet<IMyThrust> thrusters = new HashSet<IMyThrust>();
        public HashSet<IMyThrust> Hovers = new HashSet<IMyThrust>();
        public HashSet<RealisticThruster> realistic = new HashSet<RealisticThruster>();
        
        public HashSet<IMyGyro> gyros = new HashSet<IMyGyro>();
        public HashSet<Zeppelin> zeppelin = new HashSet<Zeppelin>();
        public HashSet<EMPEffect> empEffects = new HashSet<EMPEffect>();
        public HashSet<Thruster360> thruster360s = new HashSet<Thruster360>();
        public HashSet<Eleron> elerons = new HashSet<Eleron>();
        public HashSet<Afterburner> afterburners = new HashSet<Afterburner>();
        public HashSet<ArmorModule> armorModules = new HashSet<ArmorModule>();
        public HashSet<Stabilizator> Stabilizators = new HashSet<Stabilizator>();

        public HashSet<IMyBeacon> beacons = new HashSet<IMyBeacon>();
        public HashSet<IMyTextPanel> TextPanels = new HashSet<IMyTextPanel>();
        public HashSet<IMyCargoContainer> CargoBoxes  = new HashSet<IMyCargoContainer>();
        public HashSet<IMyGasTank> GasTank = new HashSet<IMyGasTank>();
        public HashSet<IMyPowerProducer> PowerProducers = new HashSet<IMyPowerProducer>();
        
        public Vector3 CurrentCockpitWingSumVector = Vector3.Zero;
        public Vector3 CurrentCockpitWingStopForce = Vector3.Zero;
        public Vector3 CurrentAirFriction = Vector3.Zero;

        public Vector3D PilotActions { get; private set; }  // [Pitch, Yaw, Roll]
        public Vector3D PilotTrimmerValues { get; private set; }  // [Pitch, Yaw, Roll]
        public IMyShipController PilotCockpit { get; private set; }

        public HashSet<LimitedBlock> limitedBlocks = new HashSet<LimitedBlock>();
        public HashSet<SpecBlock> limitsProducer = new HashSet<SpecBlock>();
        public AutoTimer limitsLastChecked = new AutoTimer(30, GameBase.r.Next(30));

        public bool skipFriction = false;
        public AutoTimer updateClosestPlanetTimer = new AutoTimer(30);
        public Timer destroyTimer;
        public float extraFriction = 0;
        public Vector3D initVector;

		public bool active = true;
        public bool isWheel = true;
        public bool isBot = false;
        public bool SortThrusters;
        public bool isApplied = false;

        public Vector3 physicalThrustersSumForce = Vector3.Zero;


        public MassCache massCache;
        public RealisticTorque realisticTorque = new RealisticTorque();

        public Atmosphere AtmosphereProperties;
		public OrientationOrderedBlocks wingList;

		public ShipHoverDrives HoverDrives;

        public List<IMyCubeGrid> connectedGrids = new List<IMyCubeGrid>();

        public float damageReduction = 1f;

        public Ship (IMyCubeGrid grid) {
            this.grid = grid;
            //damageMultiplier = new WhileOnAndConnectedMultiplierEffect(4, grid, grid.EntityId, grid.EntityId, 0.8f);

            massCache = new MassCache(this);
            var gg = grid as MyCubeGrid;
            if (gg.BlocksCount == 1 && gg.FindBlock<IMyWheel>() != null) {
                active = false;
                isWheel = true;
            }
			AtmosphereProperties = new Atmosphere();
			initVector = grid.WorldMatrix.Translation;
            wingList = new OrientationOrderedBlocks();
			HoverDrives = new ShipHoverDrives();

			grid.FindBlocks((x)=>Grid_OnBlockAdded(x));

            grid.OnBlockAdded += Grid_OnBlockAdded;
            grid.OnBlockRemoved += Grid_OnBlockRemoved;
            grid.OnMarkForClose += Grid_OnMarkForClose;
            grid.OnGridSplit += Grid_OnGridSplit;
        }
        

		public void Draw ()
		{
            Eleron.RunDraw (grid);
            //foreach (var x in thrusters)
            //{
            //    Vector3D center;
            //    x.SlimBlock.ComputeWorldCenter(out center);
            //    PhysicsHelper.Draw (Color.White, center, x.WorldMatrix.Forward*10, 0.2f, "SciFiEngineThrustMiddle");//, );
            //    PhysicsHelper.Draw (Color.White, center, x.WorldMatrix.Forward*10, 0.2f, "EngineThrustMiddle");//, );
            //}
		}

		public void AfterSimulation ()
		{
            ShipSubpartsLogic.AfterSimulation(this);
        }

        public void SafeDeleteGrid (bool subgrids = false) { //TODO subgrids - eject pilos from subgrids
            var buffer = grid.FindBlocks((x)=>{
                if (x.FatBlock == null) return false;
                if (x.FatBlock is IMyCockpit) return true;
                if (x.FatBlock is IMyLandingGear) return true;
                if (x.FatBlock is IMyShipConnector) return true;
                return false;
            });
                    
            foreach (var y in buffer) {
                var cock = y.FatBlock as IMyCockpit;
                if (cock != null) { cock.RemovePilot(); continue; }
                var land = y.FatBlock as IMyLandingGear;
                if (land != null && land.IsLocked) { land.Unlock(); continue; }
                var conn = y.FatBlock as IMyShipConnector;
                if (conn != null && conn.IsConnected) { conn.Disconnect(); continue; }
            }
                   
            grid.Close();
        }

        public void DoPhysics() {
            if (grid.Physics == null || !grid.Physics.IsActive || grid.IsStatic) return;
            try {
                if (updateClosestPlanetTimer.tick ()) {
                    updateClosestPlanet();
                } 
                //ZeppelinLogic.UpdateBeforeSimulation(this);
                //PhysicalThrusters.RunBoostLogic (grid);
                WingLogic.BeforeSimulation(grid, this);
                Stabilizator.Logic(this);
                HoverDrives.UpdateHovers(closestPlanet, grid, this);
                realisticTorque.ApplyRealisticTorque(this);
                AirFriction.ApplyFriction(this);
            } catch (Exception e) {
                //Log.ChatError("??", e);
            }
        }

        

        
        public void BeforeSimulation () {
            if (grid.isFrozen ()) return;

            ShipSubpartsLogic.BeforeSimulation(this);
                
            UpdatePilotActions();
            DoPhysics();

            if (!MyAPIGateway.Session.isTorchServer()) {
                if (limitsLastChecked.tick()) {
                    LimitsChecker.CheckLimitsInGrid(grid);
                }    
            }

            if (destroyTimer != null && MyAPIGateway.Session.IsServer && destroyTimer.tick()) {
                MyAPIGateway.Utilities.InvokeOnGameThread(()=>{ SafeDeleteGrid (); });
            }
        }
        private void UpdatePilotActions()
        {
			PilotCockpit = null;
			PilotActions = Vector3D.Zero;
            if (Cockpits.Count == 0)
                return;

            PilotCockpit = Cockpits.FirstElement<MyShipController>();

            foreach (IMyShipController C in Cockpits)
            {
                if (PilotCockpit.Pilot != null && C.IsControllingCockpit())
                {
                    PilotCockpit = C;
                    break;
                }
            }
            
            PilotActions = new Vector3D(PilotCockpit.RotationIndicator.Y / 10.0f, PilotCockpit.RotationIndicator.X / 10.0f, PilotCockpit.RollIndicator);
        }
        private void Grid_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
            grid.OnBlockAdded -= Grid_OnBlockAdded;
            grid.OnBlockRemoved -= Grid_OnBlockRemoved;
            grid.OnMarkForClose -= Grid_OnMarkForClose;
            grid.OnGridSplit -= Grid_OnGridSplit;
        }

        private void Grid_OnBlockRemoved(IMySlimBlock obj) {
            onAddedRemoved (obj, false);
		}
        private void Grid_OnBlockAdded(IMySlimBlock obj) {
            onAddedRemoved (obj, true);
		}

        private void Grid_OnGridSplit(IMyCubeGrid arg1, IMyCubeGrid arg2) {
            if (destroyTimer != null) {
                var ship1 = arg1.GetShip();
                var ship2 = arg2.GetShip();
                var time = destroyTimer.getTime();
                ship1.destroyTimer = new Timer (time);
                ship2.destroyTimer = new Timer (time);
                ship1.initVector = this.initVector;
                ship2.initVector = this.initVector;
            }

            LimitsChecker.OnGridSplit(arg1, arg2);
        }

        internal void onAddedRemoved(IMySlimBlock obj, bool added) {
            if (!active) return;
            var fat = obj.FatBlock;
            if (fat != null) {
                if (fat.GetAs<WingTN>() != null) {
                    if (added) {
                        wingList.Add(fat as IMyTerminalBlock);
                    } else {
                        wingList.Remove(fat as IMyTerminalBlock);
                    }
                } else if (fat.GetAs<Eleron>() != null)
				{
					if (added)
					{
						wingList.Add(fat as IMyTerminalBlock);
					}
					else
					{
						wingList.Remove(fat as IMyTerminalBlock);
					}
				}


                if (ShipHoverDrives.IsHover(fat)) {
                    if (added) {
                        HoverDrives.Add(fat as IMyFunctionalBlock);
                        Hovers.Add(fat as IMyThrust);
                    } else {
                        HoverDrives.Remove(fat as IMyFunctionalBlock);
                        Hovers.Remove(fat as IMyThrust);
                    }
                }

                
                RegisterUnregisterGameLogic(fat, added, afterburners);
                RegisterUnregisterGameLogic(fat, added, thruster360s);
                RegisterUnregisterGameLogic(fat, added, elerons);
                RegisterUnregisterGameLogic(fat, added, limitedBlocks);
                RegisterUnregisterGameLogic(fat, added, BlocksWithReactions);
                RegisterUnregisterGameLogic(fat, added, realistic);
                RegisterUnregisterGameLogic(fat, added, limitsProducer);
                RegisterUnregisterGameLogic(fat, added, zeppelin);
                RegisterUnregisterGameLogic(fat, added, empEffects);

                RegisterUnregisterGameLogic(fat, added, armorModules);
                RegisterUnregisterGameLogic(fat, added, Stabilizators);


                

                var sl = fat as IMySolarPanel;
                if (sl != null) { if (added) { new SolarPanelAllwaysMaxOutput(sl); } }
                
                var cc = fat as MyShipController;
                if (cc != null && cc.BlockDefinition.EnableShipControl)
                {
                    RegisterUnregisterType(fat, added, Cockpits);
                }
               
                RegisterUnregisterType (fat, added, thrusters);
                RegisterUnregisterType (fat, added, gyros);
                RegisterUnregisterType (fat, added, TextPanels);
                RegisterUnregisterType (fat, added, CargoBoxes);
                RegisterUnregisterType (fat, added, GasTank);
                RegisterUnregisterType (fat, added, PowerProducers);
                RegisterUnregisterType (fat, added, beacons);
            }
        }

        private void RegisterUnregisterGameLogic<T> (IMyCubeBlock fat, bool added, ICollection<T> collection) where T : MyGameLogicComponent
        {
            var t = fat.GetAs<T>();
            if (t != null)
            {
                if (added) collection.Add(t);
                else collection.Remove(t);
            }
        }

        private void RegisterUnregisterType<T>(IMyCubeBlock fat, bool added, ICollection<T> collection) where T : IMyCubeBlock
        {
            if (fat is T)
            {
                if (added) collection.Add((T)fat);
                else collection.Remove((T)fat);
                if (fat is IMyThrust) SortThrusters = true; 
            }
        }


        public double getElevation () {
            if (closestPlanet == null) {
                return -1d;
            } else {
                var position = grid.WorldAABB.Center;
                Vector3D closestSurfacePointGlobal = closestPlanet.GetClosestSurfacePointGlobal(ref position);
                return Vector3D.Distance(closestSurfacePointGlobal, position);
            }
        }

        public double getElevation2() {
            if (closestPlanet == null) {
                return -1d;
            } else {
                if (!closestPlanet.HasAtmosphere) return -1d;
                var distance = (grid.WorldAABB.Center - closestPlanet.PositionComp.GetPosition()).Length();
                return Math.Max(0, distance - closestPlanet.AverageRadius);
            }
        }

		public float forcesMultiplier = 1; //Fix high gravity bug;


        private List<IMyCubeGrid> connectedGridsBuffer = new List<IMyCubeGrid>();
		public void OnClosestPlanetChanged ()
		{
            
			var connected = grid.GetConnectedGrids (GridLinkTypeEnum.Physical, connectedGridsBuffer, true);
			var isRavcor = closestPlanet == null ? false : closestPlanet.Generator.Id.SubtypeName == "Ravcor";
			var newforcesMultiplier = isRavcor ? 0.2f : 1f;            //Log.ChatError("OnClosestPlanetChanged:" + newforcesMultiplier + " Connected:" + connected.Count);
            foreach (var g in connected)
			{
				var ship = g.GetShip();
				ship.updateClosestPlanetTimer.reset();
				ship.closestPlanet = closestPlanet;
                //Log.ChatError("Change?: " + ship.forcesMultiplier + " " + newforcesMultiplier);
                if (ship.forcesMultiplier != newforcesMultiplier)
				{
                    //Log.ChatError ("forcesMultiplier changed: " + ship.forcesMultiplier + " " + newforcesMultiplier);
					ship.forcesMultiplier = newforcesMultiplier;
					foreach (var th in ship.thrusters)
					{
						th.ThrustMultiplier *= forcesMultiplier;
						var thb = th.GetAs<ThrusterBase>();
						if (isRavcor)
						{
							thb.AddEffect(new EndlessMultiplierEffect (1, g.EntityId, th.EntityId, forcesMultiplier));
							thb.Recalculate();
						} else {
							thb.RemoveMultiplierEffect (1, g.EntityId);
						}
					}
				}
			}
		}

        public void updateClosestPlanet() {
            var position = grid.WorldAABB.Center;// position
            var aabb = new BoundingBoxD(position, position);
            updateClosestPlanetTimer.reset();

            if (closestPlanet != null && (!closestPlanet.Closed || !closestPlanet.MarkedForClose)) {
                if (closestPlanet.IntersectsWithGravityFast(ref aabb)) {
                    return;
                }
            }

            foreach (var pl in GameBase.instance.planets) {
                if (pl.Value.IntersectsWithGravityFast(ref aabb)) {
					if (closestPlanet != pl.Value)
					{
                        closestPlanet = pl.Value;
                        OnClosestPlanetChanged();
                    }
                    return;
                }
            }

            if (closestPlanet != null)
            {
                closestPlanet = null;
                OnClosestPlanetChanged();
            }
        }

		
    }
}