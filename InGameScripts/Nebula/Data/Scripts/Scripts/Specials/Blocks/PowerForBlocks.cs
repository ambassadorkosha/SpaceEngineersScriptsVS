using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;

namespace ServerMod.Specials {

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipWelder), true, new string[] {
    "LargeShipWelder", "LargeShipWelderT01", "LargeShipWelderT02", "LargeShipWelderT03", "LargeShipWelderT04", "SmallShipWelder", "SmallShipWelderT01", "SmallShipWelderT02", "SmallShipWelderT03", "SmallShipWelderT04" })] //add here more
    public class WelderPowerBoost : PowerBoost  {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            var subtype = (Entity as IMyCubeBlock).BlockDefinition.SubtypeName;
            if (subtype.Equals("LargeShipWelder")) setPowerConsumption(2.5f);
            if (subtype.Equals("LargeShipWelderT01")) setPowerConsumption(2.5f);
            if (subtype.Equals("LargeShipWelderT02")) setPowerConsumption(3.5f);
            if (subtype.Equals("LargeShipWelderT03")) setPowerConsumption(4.5f);
            if (subtype.Equals("LargeShipWelderT04")) setPowerConsumption(5.5f);
            if (subtype.Equals("SmallShipWelder")) setPowerConsumption(0.5f);
            if (subtype.Equals("SmallShipWelderT01")) setPowerConsumption(0.5f);
            if (subtype.Equals("SmallShipWelderT02")) setPowerConsumption(1f);
            if (subtype.Equals("SmallShipWelderT03")) setPowerConsumption(1.5f);
            if (subtype.Equals("SmallShipWelderT04")) setPowerConsumption(2f);
        }
    }
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipGrinder), true, new string[] {
    "LargeShipGrinder", "LargeShipGrinderT01", "LargeShipGrinderT02", "LargeShipGrinderT03", "LargeShipGrinderT04", "SmallShipGrinder", "SmallShipGrinderT01", "SmallShipGrinderT02", "SmallShipGrinderT03", "SmallShipGrinderT04" })] //add here more
    public class GrinderPowerBoost : PowerBoost  {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            var subtype = (Entity as IMyFunctionalBlock).BlockDefinition.SubtypeName;
            if (subtype.Equals("LargeShipGrinder")) setPowerConsumption(2.5f);
            if (subtype.Equals("LargeShipGrinderT01")) setPowerConsumption(2.5f);
            if (subtype.Equals("LargeShipGrinderT02")) setPowerConsumption(3.5f);
            if (subtype.Equals("LargeShipGrinderT03")) setPowerConsumption(4.5f);
            if (subtype.Equals("LargeShipGrinderT04")) setPowerConsumption(5.5f);
            if (subtype.Equals("SmallShipGrinder")) setPowerConsumption(0.5f);
            if (subtype.Equals("SmallShipGrinderT01")) setPowerConsumption(0.5f);
            if (subtype.Equals("SmallShipGrinderT02")) setPowerConsumption(1f);
            if (subtype.Equals("SmallShipGrinderT03")) setPowerConsumption(1.5f);
            if (subtype.Equals("SmallShipGrinderT04")) setPowerConsumption(2f);
        }
    }
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Drill), true, new string[] {
    "LargeBlockDrill", "LargeBlockDrillT01", "LargeBlockDrillT02", "LargeBlockDrillT03", "LargeBlockDrillT04", "SmallBlockDrill", "SmallBlockDrillT01", "SmallBlockDrillT02", "SmallBlockDrillT03", "SmallBlockDrillT04" })] //add here more
    public class DrillPowerBoost : PowerBoost  {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            var subtype = (Entity as IMyFunctionalBlock).BlockDefinition.SubtypeName;
            if (subtype.Equals("LargeBlockDrill")) setPowerConsumption(2.5f);
            if (subtype.Equals("LargeBlockDrillT01")) setPowerConsumption(2.5f);
            if (subtype.Equals("LargeBlockDrillT02")) setPowerConsumption(3.5f);
            if (subtype.Equals("LargeBlockDrillT03")) setPowerConsumption(4.5f);
            if (subtype.Equals("LargeBlockDrillT04")) setPowerConsumption(5.5f);
            if (subtype.Equals("SmallBlockDrill")) setPowerConsumption(0.5f);
            if (subtype.Equals("SmallBlockDrillT01")) setPowerConsumption(0.5f);
            if (subtype.Equals("SmallBlockDrillT02")) setPowerConsumption(1f);
            if (subtype.Equals("SmallBlockDrillT03")) setPowerConsumption(1.5f);
            if (subtype.Equals("SmallBlockDrillT04")) setPowerConsumption(2f);
        }
    }
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TimerBlock), true, new string[] { "TimerBlockLarge", "TimerBlockSmall" })] //add here more
    public class TimerPowerBoost : PowerBoost  {
        public static readonly float MIN_TIMEOUT = 3f;
        private IMyTimerBlock myBlock;
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            myBlock = (Entity as IMyTimerBlock);
            myBlock.PropertiesChanged += MyBlockOnPropertiesChanged;
            var subtype = (Entity as IMyFunctionalBlock).BlockDefinition.SubtypeName;
            if (subtype.Equals("TimerBlockLarge")) setPowerConsumption(0.5f);
            if (subtype.Equals("TimerBlockSmall")) setPowerConsumption(0.2f);
        }
        
        
        private void MyBlockOnPropertiesChanged(IMyTerminalBlock obj) {
            if (myBlock.TriggerDelay < MIN_TIMEOUT) {
                myBlock.TriggerDelay = MIN_TIMEOUT;
            }
        }
    }
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), true, new string[] { "LargeProgrammableBlock", "SmallProgrammableBlock", "HotProgrammableBlock", "HotProgrammableBlockSmall" })] //add here more
    public class ProgrammableBlockPowerBoost : PowerBoost  {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            var subtype = (Entity as IMyFunctionalBlock).BlockDefinition.SubtypeName;
            if (subtype.Equals("LargeProgrammableBlock")) setPowerConsumption(0.3f);
            if (subtype.Equals("SmallProgrammableBlock")) setPowerConsumption(0.2f);
            if (subtype.Equals("HotProgrammableBlock")) setPowerConsumption(1.5f);
            if (subtype.Equals("HotProgrammableBlockSmall")) setPowerConsumption(1f);
        }
    }
    
    
    public abstract class PowerBoost : MyGameLogicComponent  {
        public void setPowerConsumption(float power) {
            (Entity as IMyCubeBlock)?.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, power);
            (Entity as IMyCubeBlock)?.ResourceSink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, power);
        }
    }
}
