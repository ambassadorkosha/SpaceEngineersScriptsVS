using System.Collections.Generic;
using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using ServerMod;
using Slime;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Scripts.Specials {
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Assembler), false, new string[] { "EventAssembler2" } )]
    public class Assembler : LimitedOnOffBlock {
        private IMyProductionBlock productionBlock;
        
        private static Dictionary<int, int> POI = LimitsChecker.From(LimitsChecker.TYPE_POI, 1);
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            SetOptions(POI);

            productionBlock = block as IMyProductionBlock;
            productionBlock.EnabledChanged += ProductionBlockOnEnabledChanged;
            productionBlock.OnMarkForClose += ProductionBlockOnOnMarkForClose;
        }

        private void ProductionBlockOnOnMarkForClose(IMyEntity obj) {
            productionBlock.EnabledChanged -= ProductionBlockOnEnabledChanged;
            productionBlock.OnMarkForClose -= ProductionBlockOnOnMarkForClose;
        }

        private void ProductionBlockOnEnabledChanged(IMyTerminalBlock obj) {
            obj.CubeGrid.OverFatBlocks((x) => {
                var ann = (x as IMyRadioAntenna);
                if (ann != null) { ann.Enabled = productionBlock.Enabled; }
            });
        }
    }
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_JumpDrive), false)]
    public class Warpdrive : LimitedOnOffBlock {
        private static Dictionary<int, int> JUMPS = LimitsChecker.From(LimitsChecker.TYPE_JUMPDRIVES, 1);
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            SetOptions(JUMPS);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_JumpDrive), true)]
    public class Warpdrive2 : EMPEffectOnOff {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            //Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME |  MyEntityUpdateEnum.EACH_10TH_FRAME |  MyEntityUpdateEnum.EACH_FRAME;
            //NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME |  MyEntityUpdateEnum.EACH_10TH_FRAME |  MyEntityUpdateEnum.EACH_FRAME;
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipWelder), false, new string[] {
         "SELtdLargeNanobotBuildAndRepairSystem", 
         "LargeShipWelder", "LargeShipWelderT01", "LargeShipWelderT02", "SmallShipWelder", "SmallShipWelderT01", "SmallShipWelderT02",
         "LargeShipWelderT03", "LargeShipWelderT04", "SmallShipWelderT03", "SmallShipWelderT04"
     })]
    public class LimitedWeldersAndNanobots : LimitedOnOffBlock {
        private static Dictionary<int, int> NANOBOTS = LimitsChecker.From(LimitsChecker.TYPE_NANOBOTS, 1);
        private static Dictionary<int, int> WELDERS1 = LimitsChecker.From(LimitsChecker.TYPE_WELDERS, 1);
        private static Dictionary<int, int> WELDERS2 = LimitsChecker.From(LimitsChecker.TYPE_WELDERS, 2);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            switch (fblock.SlimBlock.BlockDefinition.Id.SubtypeName) {
                case "SELtdLargeNanobotBuildAndRepairSystem" : { SetOptions(NANOBOTS); break; }
                case "LargeShipWelder" : 
                case "LargeShipWelderT01" : 
                case "LargeShipWelderT02" : 
                case "SmallShipWelder" : 
                case "SmallShipWelderT01" : 
                case "SmallShipWelderT02" : { SetOptions(WELDERS1); break; }
                
                case "LargeShipWelderT03" : 
                case "LargeShipWelderT04" : 
                case "SmallShipWelderT03" : 
                case "SmallShipWelderT04" : { SetOptions(WELDERS2); break; }
            }
        }

        public override bool CheckConditions(SpecBlock specblock)
        {
            return base.CheckConditions(specblock) && (MyAPIGateway.Multiplayer.Players.IsOnline(block.BuiltBy()) || (specblock != null && specblock.block.SubtypeName() == "BaseSpecBlock"));
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false, new string[] {"", "LargeGatlingTurretT02", "LargeGatlingTurretT03", "LargeGatlingTurretT04","SmallGatlingTurret", "SmallGatlingTurretT02","SmallGatlingTurretT03", "SmallGatlingTurretT04",   "DefensiveTurret",
                                                                                                     "CompactLargeGatlingTurret", "CompactLargeGatlingTurretT02", "CompactLargeGatlingTurretT03", "CompactLargeGatlingTurretT04" })]
    public class LimitedLargeGatlingTurret : LimitedOnOffBlock {
        private static Dictionary<int, int> WEAPON5 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 5, LimitsChecker.TYPE_TURRETS, 1);
        private static Dictionary<int, int> WEAPON3 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 3, LimitsChecker.TYPE_TURRETS, 1);
        private static Dictionary<int, int> WEAPON1 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 1, LimitsChecker.TYPE_TURRETS, 1);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            switch (fblock.SlimBlock.BlockDefinition.Id.SubtypeName) {
                case "":
                case "LargeGatlingTurretT02":
                case "LargeGatlingTurretT03":
                case "LargeGatlingTurretT04": {
                    SetOptions(WEAPON5);
                    break;
                }

                case "CompactLargeGatlingTurret":
                case "CompactLargeGatlingTurretT02":
                case "CompactLargeGatlingTurretT03":
                case "CompactLargeGatlingTurretT04": {
                    SetOptions(WEAPON3);
                    break;
                }
                case "DefensiveTurret": {
                    SetOptions(WEAPON3);
                    break;
                }

                case "SmallGatlingTurret":
                case "SmallGatlingTurretT02":
                case "SmallGatlingTurretT03":
                case "SmallGatlingTurretT04": {
                    SetOptions(WEAPON1);
                    break;
                }
            }
        }
    }
    
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false, new string[] {
         //"MA_Designator_sm", "MA_Designator",
         "CompactLargeMissileTurret", "CompactLargeMissileTurretT02","CompactLargeMissileTurretT03","CompactLargeMissileTurretT04",
         "", "LargeMissileTurretT02","LargeMissileTurretT03","LargeMissileTurretT04",
         "SmallMissileTurret", "SmallMissileTurretT02","SmallMissileTurretT03","SmallMissileTurretT04",         
     })]
    public class LimitedLargeMissileTurret : LimitedOnOffBlock {
        private static Dictionary<int, int> WEAPON5 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 5, LimitsChecker.TYPE_TURRETS, 1, LimitsChecker.TYPE_ROCKETS, 1);
        private static Dictionary<int, int> WEAPON3 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 3, LimitsChecker.TYPE_TURRETS, 1, LimitsChecker.TYPE_ROCKETS, 1);
        private static Dictionary<int, int> WEAPON1 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 1, LimitsChecker.TYPE_TURRETS, 1, LimitsChecker.TYPE_ROCKETS, 1);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);

            switch (fblock.SlimBlock.BlockDefinition.Id.SubtypeName) {
                case "CompactLargeMissileTurret":
                case "CompactLargeMissileTurretT02":
                case "CompactLargeMissileTurretT03":
                case "CompactLargeMissileTurretT04": {
                    SetOptions(WEAPON3, false);
                    break;
                }

                case "":
                case "LargeMissileTurretT02":
                case "LargeMissileTurretT03":
                case "LargeMissileTurretT04": {
                    SetOptions(WEAPON5, false);
                    break;
                }
                
                case "SmallMissileTurret":
                case "SmallMissileTurretT02":
                case "SmallMissileTurretT03":
                case "SmallMissileTurretT04": {
                    SetOptions(WEAPON1, false);
                    break;
                }
            }
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), false, new string[] {"LargeInteriorTurret"})]
    public class LimitedInteriorTurret : LimitedOnOffBlock {
        private static Dictionary<int, int> LIMIT = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 1, LimitsChecker.TYPE_TURRETS, 1);
        public LimitedInteriorTurret() {
            SetOptions(LIMIT);
        }
    }



    #region ROCKETS

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), false, new string[] { "LargeMissileLauncher", "LargeMissileLauncherT02", "LargeMissileLauncherT03", "LargeMissileLauncherT04" })]
    public class LargeMissileLauncherLimits : LimitedOnOffBlock {
        private static Dictionary<int, int> WEAPON5 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 4, LimitsChecker.TYPE_ROCKETS, 1);
        private static Dictionary<int, int> WEAPON1 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 1, LimitsChecker.TYPE_ROCKETS, 1);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);

            switch (fblock.SlimBlock.BlockDefinition.Id.SubtypeName) {
                case "LargeMissileLauncher":
                case "LargeMissileLauncherT02":
                case "LargeMissileLauncherT03":
                case "LargeMissileLauncherT04": {
                    SetOptions(WEAPON5, false);
                    break;
                }
            }
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), false, new string[] { "", "SmallMissileLauncherT02", "SmallMissileLauncherT03", "SmallMissileLauncherT04" })]
    public class LimitedLargeMissileLauncherLimits : DisabledOnSubpartsLimitedOnOffBlock
    {
        private static Dictionary<int, int> WEAPON5 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 4, LimitsChecker.TYPE_ROCKETS, 1);
        private static Dictionary<int, int> WEAPON1 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 1, LimitsChecker.TYPE_ROCKETS, 1);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            switch (fblock.SlimBlock.BlockDefinition.Id.SubtypeName)
            {
                case "":
                case "SmallMissileLauncherT02":
                case "SmallMissileLauncherT03":
                case "SmallMissileLauncherT04":
                    {
                        SetOptions(WEAPON1, false);
                        break;
                    }
            }
        }
    }

    #endregion ROCKETS


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false, new string[] { "SmallRocketLauncherReload", "SmallRocketLauncherReloadT02", "SmallRocketLauncherReloadT03", "SmallRocketLauncherReloadT04" })]
    public class LimitedMissileLauncherReload : DisabledOnSubpartsLimitedOnOffBlock
    {
        private static Dictionary<int, int> WEAPON3 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 3, LimitsChecker.TYPE_ROCKETS, 1);
        public LimitedMissileLauncherReload()
        {
            SetOptions(WEAPON3, false);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false, new string[] { "", "SmallGatlingGunT02", "SmallGatlingGunT03", "SmallGatlingGunT04" })]
    public class LimitedSmallGatlingGun : DisabledOnSubpartsLimitedOnOffBlock
    {
        private static Dictionary<int, int> LIMIT = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 1);
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            SetOptions(LIMIT);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), false, new string[] { "LargeScriptedMissileLauncher", "LargeScriptedMissileLauncherT02", "LargeScriptedMissileLauncherT03", "LargeScriptedMissileLauncherT04" })]
    public class LimitedLargeScriptedMissileLauncher : LimitedOnOffBlock
    {
        private static Dictionary<int, int> WEAPON5 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 4, LimitsChecker.TYPE_ROCKETS, 1, LimitsChecker.TYPE_TURRETS, 1);
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            SetOptions(WEAPON5, false);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false, new string[] { "SmallScriptedRocketLauncherReload", "SmallScriptedRocketLauncherReloadT02", "SmallScriptedRocketLauncherReloadT03", "SmallScriptedRocketLauncherReloadT04" })]
    public class LimitedScriptedMissileLauncherReload : LimitedOnOffBlock
    {
        private static Dictionary<int, int> WEAPON3 = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 3, LimitsChecker.TYPE_ROCKETS, 1, LimitsChecker.TYPE_TURRETS, 1);
        public LimitedScriptedMissileLauncherReload()
        {
            SetOptions(WEAPON3, false);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false, new string[] { "SmallScriptedGatlingGun", "SmallScriptedGatlingGunT02", "SmallScriptedGatlingGunT03", "SmallScriptedGatlingGunT04" })]
    public class LimitedScriptedSmallGatlingGun : LimitedOnOffBlock
    {
        private static Dictionary<int, int> LIMIT = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 1, LimitsChecker.TYPE_TURRETS, 1);
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            SetOptions(LIMIT);
        }
    }
}