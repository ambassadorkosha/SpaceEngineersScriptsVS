using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Specials.POI;
using ServerMod;
using Slime;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Specials {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RadioAntenna), false, new string[] { "TraderSpecBlock", "AssistantSpecBlock", "AssistantSpecBlockSmall", "BaseSpecBlock", "SpySpecBlock", "InterceptorSpecBlock", "CorvetSpecBlock", "CruiserSpeckBlock", "CraftCarrierSpecBlock", "LinkorSpecBlock", "TitanSpecBlock", "CorvetSpecBlockSmall", "AdminSpecBlock", "AdminSpecBlockSmall" })]
    public class RSpecBlock : RadioAntennaSpecBlock {
        public static Dictionary<int, int> LIMIT_TRADER = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 15, 
            LimitsChecker.TYPE_TURRETS, 5, 
            LimitsChecker.TYPE_JUMPDRIVES, 1, 
            LimitsChecker.TYPE_AFTERBURNER, 1,
            LimitsChecker.TYPE_TORPEDO, 8
            );
        public static Dictionary<int, int> LIMIT_ASSISTANT = LimitsChecker.From(LimitsChecker.TYPE_WELDERS, 8);
        public static Dictionary<int, int> LIMIT_BASE_STATIC = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 999, 
            LimitsChecker.TYPE_SAFEZONE, 1, 
            LimitsChecker.TYPE_TURRETS, 999, 
            LimitsChecker.TYPE_ROCKETS, 999, 
            LimitsChecker.TYPE_JUMPDRIVES, 999, 
            LimitsChecker.TYPE_WELDERS, 999, 
            LimitsChecker.TYPE_NANOBOTS, 6, 
            LimitsChecker.TYPE_AFTERBURNER, 999,
            LimitsChecker.TYPE_TORPEDO, 9999);
        public static Dictionary<int, int> LIMIT_BASE_DYNAMIC = LimitsChecker.From(
            LimitsChecker.TYPE_JUMPDRIVES, 999, 
            LimitsChecker.TYPE_AFTERBURNER, 999,
            LimitsChecker.TYPE_TORPEDO, 20);

        public static Dictionary<int, int> LIMIT_SPY = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 25, 
            LimitsChecker.TYPE_TURRETS, 2, 
            LimitsChecker.TYPE_AFTERBURNER, 1,
            LimitsChecker.TYPE_TORPEDO, 8);
        public static Dictionary<int, int> LIMIT_INTERCEPTOR = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 45, 
            LimitsChecker.TYPE_TURRETS, 1, 
            LimitsChecker.TYPE_ROCKETS, 6, 
            LimitsChecker.TYPE_AFTERBURNER, 3,
            LimitsChecker.TYPE_TORPEDO, 12);
        public static Dictionary<int, int> LIMIT_CORVET = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 60, 
            LimitsChecker.TYPE_TURRETS, 8, 
            LimitsChecker.TYPE_ROCKETS, 8, 
            LimitsChecker.TYPE_AFTERBURNER, 1, 
            LimitsChecker.TYPE_ARMOR_MODULES, 3,
            LimitsChecker.TYPE_TORPEDO, 20);

        public static Dictionary<int, int> LIMIT_CRUISER = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 110, 
            LimitsChecker.TYPE_TURRETS, 15, 
            LimitsChecker.TYPE_ROCKETS, 12,
            LimitsChecker.TYPE_JUMPDRIVES, 1, 
            LimitsChecker.TYPE_WELDERS, 2, 
            LimitsChecker.TYPE_AFTERBURNER, 1, 
            LimitsChecker.TYPE_ARMOR_MODULES, 6, 
            LimitsChecker.TYPE_POI, 1,
            LimitsChecker.TYPE_TORPEDO, 24);
        public static Dictionary<int, int> LIMIT_AIRCRAFTCARRIER = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 120,  
            LimitsChecker.TYPE_TURRETS, 25, 
            LimitsChecker.TYPE_ROCKETS, 20, 
            LimitsChecker.TYPE_JUMPDRIVES, 10,
            LimitsChecker.TYPE_WELDERS, 12, 
            LimitsChecker.TYPE_NANOBOTS, 3,
            LimitsChecker.TYPE_ARMOR_MODULES, 8, 
            LimitsChecker.TYPE_POI, 1,
            LimitsChecker.TYPE_TORPEDO, 40);
        public static Dictionary<int, int> LIMIT_LINKOR = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 200, 
            LimitsChecker.TYPE_TURRETS, 50, 
            LimitsChecker.TYPE_ROCKETS, 20, 
            LimitsChecker.TYPE_JUMPDRIVES, 10, 
            LimitsChecker.TYPE_WELDERS, 2, 
            LimitsChecker.TYPE_NANOBOTS, 1,
            LimitsChecker.TYPE_ARMOR_MODULES, 10,
            LimitsChecker.TYPE_TORPEDO, 60,
            LimitsChecker.TYPE_POI, 1);
        public static Dictionary<int, int> LIMIT_BATTLESHIP = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 300, 
            LimitsChecker.TYPE_TURRETS, 200, 
            LimitsChecker.TYPE_ROCKETS, 30, 
            LimitsChecker.TYPE_JUMPDRIVES, 10,
            LimitsChecker.TYPE_POI, 1,
            LimitsChecker.TYPE_TORPEDO, 100,
            LimitsChecker.TYPE_ARMOR_MODULES, 15);

        public static Dictionary<int, int> LIMIT_ADMIN = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 99999, 
            LimitsChecker.TYPE_SAFEZONE, 999, 
            LimitsChecker.TYPE_TURRETS, 999, 
            LimitsChecker.TYPE_ROCKETS, 999, 
            LimitsChecker.TYPE_JUMPDRIVES, 999, 
            LimitsChecker.TYPE_WELDERS, 999, 
            LimitsChecker.TYPE_NANOBOTS, 999, 
            LimitsChecker.TYPE_AFTERBURNER, 999, 
            LimitsChecker.TYPE_ARMOR_MODULES, 999,
            LimitsChecker.TYPE_TORPEDO, 100);

        public static Dictionary<int, int> LIMIT_ADMIN_ZERO_LIMITS = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 0,
            LimitsChecker.TYPE_SAFEZONE, 0,
            LimitsChecker.TYPE_TURRETS, 0,
            LimitsChecker.TYPE_ROCKETS, 0,
            LimitsChecker.TYPE_JUMPDRIVES, 0,
            LimitsChecker.TYPE_WELDERS, 0,
            LimitsChecker.TYPE_NANOBOTS, 0,
            LimitsChecker.TYPE_AFTERBURNER, 0,
            LimitsChecker.TYPE_ARMOR_MODULES, 0,
            LimitsChecker.TYPE_TORPEDO, 100);

        public override void Init(MyObjectBuilder_EntityBase definition) {
            base.Init(definition);
            switch (block.SlimBlock.BlockDefinition.Id.SubtypeName) {
				//								    	priority,  maxPCU, minRadius,  limits,   canBeLargeGrid,SmallGrid, shipPrefix
                case "TraderSpecBlock": 		SetOptions(0,       30000,3000,     3000, LIMIT_TRADER,          true, true, "[TRADER]"); break;
                case "AssistantSpecBlock": 
                case "AssistantSpecBlockSmall": SetOptions(1,       20000,3000,     3000, LIMIT_ASSISTANT,       true, true, "[ASSIST]"); break;
                
                case "SpySpecBlock": 			SetOptions(2,       10000,3000,     1000, LIMIT_SPY,            false, true, "[SPY]"); canCustomizeName = true; break;
                case "InterceptorSpecBlock": 	SetOptions(3,       20000,3000,     2000, LIMIT_INTERCEPTOR,    false, true, "[INTERCEPTOR]"); break;
                case "CorvetSpecBlock":
                case "CorvetSpecBlockSmall": 	SetOptions(4,       25000,3000,     6000, LIMIT_CORVET,           true, true, "[CORVETTE]"); break;
                case "CruiserSpeckBlock": 		SetOptions(5,       30000,5000,     7000, LIMIT_CRUISER,          true, true, "[CRUISER]"); break;
                case "CraftCarrierSpecBlock": 	SetOptions(6,      180000,10000,     10000, LIMIT_AIRCRAFTCARRIER,  true, true, "[CRAFTCARRIER]"); break;
                case "LinkorSpecBlock": 		SetOptions(7,       35000,15000,     12000, LIMIT_LINKOR,           true, true, "[BATTLESHIP]"); break;
                case "TitanSpecBlock": 			SetOptions(8,       50000,20000,     15000, LIMIT_BATTLESHIP,        true, true, "[TITAN]"); break;
                case "BaseSpecBlock":           SetOptions(9999, 99999999,999999,   10000, LIMIT_BASE_STATIC, true, true, "[BASE]"); isBase = true; break;
                case "AdminSpecBlock":          
                case "AdminSpecBlockSmall":     SetOptions(999999,99999999, 999999, 1, LIMIT_ADMIN,             true, true, "[ADMIN]"); break;
                case "AdminZeroLimitsSpecBlock":
                case "AdminZeroLimitsSpecBlockSmall":     SetOptions(9999999,99999999, 999999, 1, LIMIT_ADMIN_ZERO_LIMITS,             true, true, "[ADMIN]"); break;
                default: {
					SetOptions(0,    99999999,  0,1, LimitsChecker.EMPTY_LIMITS, true, true, "[UNKNOWN]"); break;
                }
            }
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, new string[] { "TorpedoSmallSpecBlock", "TorpedoLargeSpecBlock" })]
    public class TorpedoLimitBlock : LimitedForeverBlock
    {
        private static Dictionary<int, int> LIMITS = LimitsChecker.From(LimitsChecker.TYPE_TORPEDO, 1);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            SetOptions(LIMITS);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, new string[] { "TorpedoSmallSpecBlock", "TorpedoLargeSpecBlock" })]
    public class TorpedoSpecBlock : SpecBlock
    {
        public static Dictionary<int, int> LIMIT_TORPEDO = LimitsChecker.From(LimitsChecker.TYPE_TORPEDO, 1);

        public override void Init(MyObjectBuilder_EntityBase definition)
        {
            base.Init(definition);
            switch (block.SlimBlock.BlockDefinition.Id.SubtypeName)
            {
                //								    	priority,  maxPCU, minRadius,  limits,   canBeLargeGrid,SmallGrid, shipPrefix
                case "TorpedoSmallSpecBlock": SetOptions(-1, 30000, 50, LIMIT_TORPEDO, true, true); break;
                case "TorpedoLargeSpecBlock": SetOptions(-1, 30000, 50, LIMIT_TORPEDO, true, true); break;
                default:
                {
                    SetOptions(0, 99999999, 30, LimitsChecker.EMPTY_LIMITS, true, true); break;
                }
            }
        }
    }

    public enum SpecBlockActivationError {
        NONE,
        NOT_ENABLED,
        HAS_LARGE_BLOCKS,
        HAS_SMALL_BLOCKS,
        HAS_PCU_LIMIT,
        HAS_BLOCKS_LIMIT,
        ANOTHER_CORE,
        POI
    }

    public abstract class RadioAntennaSpecBlock : SpecBlock {
        private static IMyTerminalControlSlider radiusSlider;

        private int minRadius;
        private string shipPrefix;
        private float MAX;

        public IMyRadioAntenna antenna;

        private void SetRadius(float rad)
        {
            try
            {
                InitSlider();
                antenna.Radius = 1;
                radiusSlider.Setter.Invoke(block, rad);
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        public override bool CanBeApplied(List<IMyCubeGrid> grids)
        {
            if (IsBase())
            {
                var aabb = grids.GetAABB();
                if (!POICore.CanCoreWork(aabb.Center, (float)aabb.HalfExtents.Max()))
                {
                    activationError = SpecBlockActivationError.POI;
                    return false;
                }
            }

            return base.CanBeApplied(grids);
        }
        public override bool IsBase()
        {
            return isBase;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            antenna = (Entity as IMyRadioAntenna);
            MAX = (block.SlimBlock.BlockDefinition as MyRadioAntennaDefinition).MaxBroadcastRadius;

            if (!MyAPIGateway.Session.isTorchServer())
            {
                block.PropertiesChanged += BlockOnPropertiesChanged;
            }
        }


        private static void InitSlider()
        {
            if (radiusSlider == null)
            {
                var list = new List<IMyTerminalControl>();
                MyAPIGateway.TerminalControls.GetControls<IMyRadioAntenna>(out list);
                foreach (var x in list)
                {
                    if (x.Id == "Radius")
                    {
                        radiusSlider = x as IMyTerminalControlSlider;
                        return;
                    }
                }
            }
        }

        protected void SetOptions(int priority, int maxPCU, int maxBlocks, int minRadius, Dictionary<int, int> limits, bool canBeLargeGrid, bool canBeSmallGrid, string shipPrefix)
        {
            this.shipPrefix = shipPrefix;
            this.minRadius = minRadius;
            SetOptions (priority, maxPCU, maxBlocks, limits, canBeLargeGrid, canBeSmallGrid);
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            BlockOnPropertiesChanged(block);
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            BlockOnPropertiesChanged(block);
            if (Math.Abs(antenna.Radius - minRadius) < 0)
            {
                SetRadius(minRadius);
            }
        }

        protected override void BlockOnAppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder arg2)
        {
            base.BlockOnAppendingCustomInfo(arg1, arg2);
            arg2.Append("MIN BROADCAST RADIUS: ").Append(minRadius).Append("m\r\n");
        }

        protected virtual void BlockOnPropertiesChanged(IMyTerminalBlock obj)
        {
            if (!antenna.Enabled) { antenna.Enabled = true; return; }
            if (!antenna.EnableBroadcasting) { antenna.EnableBroadcasting = true; return; }
            if (!antenna.ShowShipName) { antenna.ShowShipName = true; return; }
            if (antenna.Radius < minRadius || antenna.Radius > minRadius)
            {
                if (isBase && !block.CubeGrid.IsStatic) { return; }
                antenna.Radius = minRadius;
                return;
            }
            if (!canCustomizeName && !antenna.HudText.StartsWith(shipPrefix))
            {
                antenna.HudText = shipPrefix + antenna.HudText.Substring(0, Math.Min(antenna.HudText.Length, 100));
                return;
            }
        }
    }
    
    public abstract class SpecBlock : MyGameLogicComponent {
        public IMyFunctionalBlock block;
        
        private Dictionary<int, int> limits;
        public Dictionary<int, int> foundLimits = new Dictionary<int, int>();
        
        private int priority;
        private int maxPCU;
        private int maxBlocks;
        
        
        private bool canBeLargeGrid;
        private bool canBeSmallGrid;
        
        protected bool canCustomizeName;
        protected bool isBase;

        public SpecBlockActivationError activationError = SpecBlockActivationError.NONE;

        public int GetPriority() { return priority; }

        public virtual bool IsBase ()
        {
            return false;
        }

        protected void SetOptions(int priority, int maxPCU, int maxBlocks, Dictionary<int, int> limits, bool canBeLargeGrid, bool canBeSmallGrid) {
            this.priority = priority;
            this.limits = limits;
            this.maxBlocks = maxBlocks;
            this.maxPCU = maxPCU;
            
            this.canBeLargeGrid = canBeLargeGrid;
            this.canBeSmallGrid = canBeSmallGrid;
        }

        public virtual bool CanBeApplied(List<IMyCubeGrid> grids) {
            if (!block.IsWorking) {
                activationError = SpecBlockActivationError.NOT_ENABLED;
                //Log.ChatError("Not enabled");
                return false;
            }
            var totalPCU = 0;
            var totalBlocks = 0;
            foreach (var x in grids) {
                if (!canBeLargeGrid && x.GridSizeEnum == MyCubeSize.Large) {
                    //Log.ChatError("Cant be large");
                    activationError = SpecBlockActivationError.HAS_LARGE_BLOCKS;
                    return false;
                }

                if (!canBeSmallGrid && x.GridSizeEnum == MyCubeSize.Small) {
                    activationError = SpecBlockActivationError.HAS_SMALL_BLOCKS;
                    //Log.ChatError("Cant be small");
                    return false;
                }
                totalPCU += (x as MyCubeGrid).BlocksPCU;
                totalBlocks += (x as MyCubeGrid).BlocksCount;
                if (totalPCU > maxPCU) {
                    activationError = SpecBlockActivationError.HAS_PCU_LIMIT;
                    //Log.ChatError("Max pcu");
                    return false;
                }
                if (totalBlocks > maxBlocks)
                {
                    activationError = SpecBlockActivationError.HAS_PCU_LIMIT;
                    //Log.ChatError("Max pcu");
                    return false;
                }
            }

            //var af = 0;
            //foreach (var x in grids)
            //{
            //    af += x.GetShip().afterburners.Count;
            //    if(af > limits[LimitsChecker.TYPE_AFTERBURNER]) {
            //        activationError = SpecBlockActivationError.AF_LIMIT;
            //        //Log.ChatError("Can't be applied: af limit");
            //        return false;
            //    }
            //}

            activationError = SpecBlockActivationError.NONE;
            return true;
        }

        public Dictionary<int, int> GetLimits() {
            if (isBase) { return block.CubeGrid.IsStatic ? RSpecBlock.LIMIT_BASE_STATIC : RSpecBlock.LIMIT_BASE_DYNAMIC; } 
            return limits;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            block = (Entity as IMyFunctionalBlock);
            if (!MyAPIGateway.Session.isTorchServer()) {
                
                block.AppendingCustomInfo += BlockOnAppendingCustomInfo;
                block.OnMarkForClose += BlockOnOnMarkForClose;
                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame() {
            base.UpdateOnceBeforeFrame();
            block.RefreshCustomInfo();
        }
        
        public override void UpdateAfterSimulation100() {
            base.UpdateAfterSimulation100();
            block.RefreshCustomInfo();
        }

        private void BlockOnOnMarkForClose(IMyEntity obj) {
            block.AppendingCustomInfo -= BlockOnAppendingCustomInfo;
            block.OnMarkForClose -= BlockOnOnMarkForClose;
        }

        protected virtual void BlockOnAppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder arg2) {
            arg2.Append("\r\nSTATUS: ").Append(GetErrorInfo()).Append("\r\n");

            arg2.Append("\r\n>>> ").Append(block.SlimBlock.BlockDefinition.Id.SubtypeName.Replace("SpecBlock", "")).Append(" Gives: <<<\r\n");
            
            arg2.Append("PCU LIMIT: ").Append(maxPCU).Append("\r\n");
            arg2.Append("BLOCK LIMIT: ").Append(maxBlocks).Append("\r\n");

            if (limits== null) return;

            arg2.Append("==============\r\n");

            foreach (var x in LimitsChecker.allKeys) {
                var am1 = foundLimits.GetValueOrDefault(x, 0);
                var am2 = limits.GetValueOrDefault(x, 0);
                if (am1 == 0 && am2 == 0) continue;

                arg2.Append(LimitsChecker.GetTypeDesciption(x)).Append(" : ").Append(am1).Append("/").Append(am2).Append("\r\n");
            }
        }

        public String GetErrorInfo() {
            switch (activationError) {
                case SpecBlockActivationError.NONE: return "Activated";
                case SpecBlockActivationError.NOT_ENABLED: return "Disabled, Block is not enabled";
                case SpecBlockActivationError.ANOTHER_CORE: return "Disabled, Another spec-block used";
                case SpecBlockActivationError.HAS_PCU_LIMIT: return "Disabled, PCU limit";
                case SpecBlockActivationError.HAS_LARGE_BLOCKS: return "Disabled, Ship has large subparts";
                case SpecBlockActivationError.HAS_SMALL_BLOCKS: return "Disabled, Ship has small subparts";
                case SpecBlockActivationError.HAS_BLOCKS_LIMIT: return "Disabled, Blocks limit";
                case SpecBlockActivationError.POI: return "Disabled, POI Near";
                default: return "Unknown status";
            }
        }
    }
}