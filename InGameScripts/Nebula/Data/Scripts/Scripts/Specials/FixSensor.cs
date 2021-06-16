using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Sandbox.Common.ObjectBuilders;

namespace ServerMod.Specials
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SensorBlock), true, new string[] { "SmallBlockSensor", "LargeBlockSensor" })]
    public class FixSensor : MyGameLogicComponent  {
        private IMySensorBlock myBlock;
        
        private int state = 0; 

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            myBlock = (Entity as IMySensorBlock);
            myBlock.PropertiesChanged += MyBlock_PropertiesChanged;
            myBlock.OnMarkForClose += MyBlock_OnMarkForClose;

        }

        private void MyBlock_PropertiesChanged(IMyTerminalBlock obj) {
            myBlock.DetectAsteroids = false;
        }

        private void MyBlock_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
             myBlock.OnMarkForClose -= MyBlock_OnMarkForClose;
             myBlock.PropertiesChanged -= MyBlock_OnMarkForClose;
        }
    }
}
