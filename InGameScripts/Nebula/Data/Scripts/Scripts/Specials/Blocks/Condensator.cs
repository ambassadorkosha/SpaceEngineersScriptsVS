using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace ServerMod.Specials
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, new string[] {
		"Condensator", "CondensatorT02", "CondensatorT03", "CondensatorT04", "CondensatorT05", "CondensatorT06", "CondensatorT07", "CondensatorT08", "CondensatorT09", "CondensatorT10", "CondensatorT11", "CondensatorT12",
		"CondensatorSmall", "CondensatorSmallT02", "CondensatorSmallT03", "CondensatorSmallT04", "CondensatorSmallT05", "CondensatorSmallT06", "CondensatorSmallT07", "CondensatorSmallT08", "CondensatorSmallT09", "CondensatorSmallT10", "CondensatorSmallT11", "CondensatorSmallT12"
	})]
    public class Condensator : MyGameLogicComponent {
        private static float ENERGY_LOSS = 0.00033f;
        private IMyBatteryBlock block;
        private float max;
        private MyResourceSourceComponent sink;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            block = (Entity as IMyBatteryBlock);
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            
            sink = block.Components.Get<MyResourceSourceComponent>();
            max = (block.SlimBlock.BlockDefinition as MyBatteryBlockDefinition).MaxStoredPower;
        }

        public override void UpdateBeforeSimulation100() {
            base.UpdateBeforeSimulation100();
             if (block.ChargeMode == Sandbox.ModAPI.Ingame.ChargeMode.Recharge) return;
            var now = sink.RemainingCapacityByType(MyResourceDistributorComponent.ElectricityId);
            if (now > 0) {
                var v = Math.Max(now - max * ENERGY_LOSS, 0);
                sink.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, v);
            }
        }
    }
}