using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Scripts.Specials;
using ServerMod;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Scripts
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), true, new string[] {
        "LargeBaseBattery",
		"LargeBaseBatteryT02",
		"LargeBaseBatteryT03",
		"LargeBaseBatteryT04",
		"LargeBaseBatteryT05",
		"LargeBaseBatteryT06",
		"LargeBaseBatteryT07",
		"LargeBaseBatteryT08",
		"LargeBaseBatteryT09",
		"LargeBaseBatteryT10",
		"LargeBaseBatteryT11",
		"LargeBaseBatteryT12",
        "Large3x3x2BaseBattery",
		"Large3x3x2BaseBatteryT02",
		"Large3x3x2BaseBatteryT03",
		"Large3x3x2BaseBatteryT04",
		"Large3x3x2BaseBatteryT05",
		"Large3x3x2BaseBatteryT06",
		"Large3x3x2BaseBatteryT07",
		"Large3x3x2BaseBatteryT08",
		"Large3x3x2BaseBatteryT09",
		"Large3x3x2BaseBatteryT10",
		"Large3x3x2BaseBatteryT11",
		"Large3x3x2BaseBatteryT12",
        })]
    public class BaseBattaries : MyGameLogicComponent {
        IMyBatteryBlock block;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            block = (Entity as IMyBatteryBlock);
            block.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            BaseBattaries_EnabledChanged(null);
            (block as IMyFunctionalBlock).EnabledChanged += BaseBattaries_EnabledChanged;
            
        }

        private void BaseBattaries_EnabledChanged(IMyTerminalBlock obj)
        {
            if (block.Enabled && !CanBeEnabled())
            {
                block.Enabled = false;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            if (block.Enabled && !CanBeEnabled())
            {
                block.Enabled = false;
            }
        }

        public bool CanBeEnabled ()
        {
            if (!block.CubeGrid.IsStatic)
            {
                return false;
            }

            //var ship = block.CubeGrid.GetShip();
            //if (ship == null)
            //{
            //    return false;
            //} 
            //var core = LimitsChecker.GetMainCore(ship);
            //if (core == null)
            //{
            //    return false;
            //}
            //
            //if (!core.IsBase())
            //{
            //    return false;
            //}

            return true;
        }
    }
}
