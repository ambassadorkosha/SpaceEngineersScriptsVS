using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using ServerMod;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Specials.Production {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Assembler), true, new string[] { "LargeAssembler", "LargeAssemblerT02", "LargeAssemblerT03", "LargeAssemblerT04" })]
    public class AdjustableAssembler : Adjustable {
        private static bool initedControls = false;
        
        public override float getMAX_SPEED_POINTS() { return 7f; }
        public override float getMAX_POWER_POINTS() { return 7f; }
        public override float getMAX_YEILD_POINTS() { return 7f; }
        public override float getBASE_SPEED() { return 1f; }
        public override float getSPEEDBonus() { return 1f; }

        public override void Refresh() {
            bool currentState = block.Enabled;
            var myAssembler = block as IMyAssembler;
            var currentWorkMode = myAssembler.Mode;
            bool currentCoopStatus = myAssembler.CooperativeMode;
            
            block.Enabled = !currentState;
            block.Enabled = currentState;
            myAssembler.Mode = currentWorkMode;
            myAssembler.CooperativeMode = currentCoopStatus;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            
            if (!initedControls) {
                initedControls = true;
                CreateControls();
            }
        }

        public static void CreateControls () {
            var SpeedControl = CreateControl<IMyAssembler>("AdjustableAssembler_Speed", "Assemble Speed", "Speed of assembling");
            var PowerControl = CreateControl<IMyAssembler>("AdjustableAssembler_Power", "Power Effectiveness", "Power savings");
            
            SpeedControl.Writer = (b, t) => t.Append(b.GetAs<Adjustable>().pspeed + " %");
            SpeedControl.Getter = (b) => b.GetAs<Adjustable>().pspeed;
            SpeedControl.Setter = (b, v) => { b.GetAs<Adjustable>().SetPointsClient ((int)MathHelper.Clamp(v,0,100), -1, -1); SetPoints(SpeedControl, PowerControl, null); };

            PowerControl.Writer = (b, t) => t.Append(b.GetAs<Adjustable>().ppower + " %");
            PowerControl.Getter = (b) => b.GetAs<Adjustable>().ppower;
            PowerControl.Setter = (b, v) => { b.GetAs<Adjustable>().SetPointsClient (-1, (int)MathHelper.Clamp(v,0,100), -1); SetPoints(SpeedControl, PowerControl, null); };
        }

    }
}
