using Digi;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Specials.Production;
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using ServerMod;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Scripts {

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Refinery), true, new string[] { "LargeRefinery", "LargeRefineryVanilla" })]
    public class AdjustableRef : Adjustable {
        private static bool initedControls = false;
        
        public override float getMAX_SPEED_POINTS() { return 7f; }
        public override float getMAX_POWER_POINTS() { return 7f; }
        public override float getMAX_YEILD_POINTS() { return 7f; }
        public override float getBASE_SPEED() { return 8f; }
        public override float getSPEEDBonus() { return 1f; }
        
        public override void Refresh() {
            bool currentState = block.Enabled;
            block.Enabled = !currentState;
            block.Enabled = currentState;
        }
         
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            if (!initedControls) {
                initedControls = true;
                CreateControls();
            }
        }

        
        public static void CreateControls () {
            var SpeedControl = CreateControl<IMyRefinery>("AdjustableRef_Speed", "Assemble Speed", "Speed of assembling");
            var PowerControl = CreateControl<IMyRefinery>("AdjustableRef_Power", "Power Effectiveness", "Power savings");
            var YieldControl = CreateControl<IMyRefinery>("AdjustableRef_Yield", "Ore saving", "Ore to ingot multiplier. (It doesn't change speed)");
            
            SpeedControl.Writer = (b, t) => t.Append(b.GetAs<Adjustable>().pspeed + " %");
            SpeedControl.Getter = (b) => b.GetAs<Adjustable>().pspeed;
            SpeedControl.Setter = (b, v) => { b.GetAs<Adjustable>().SetPointsClient ((int)MathHelper.Clamp(v,0,100), -1, -1); SetPoints(SpeedControl, PowerControl, YieldControl); };

            PowerControl.Writer = (b, t) => t.Append(b.GetAs<Adjustable>().ppower + " %");
            PowerControl.Getter = (b) => b.GetAs<Adjustable>().ppower;
            PowerControl.Setter = (b, v) => { b.GetAs<Adjustable>().SetPointsClient (-1, (int)MathHelper.Clamp(v,0,100), -1); SetPoints(SpeedControl, PowerControl, YieldControl); };

            YieldControl.Writer = (b, t) => t.Append(b.GetAs<Adjustable>().pyeild + " %");
            YieldControl.Getter = (b) => b.GetAs<Adjustable>().pyeild;
            YieldControl.Setter = (b, v) => { b.GetAs<Adjustable>().SetPointsClient (-1, -1, (int)MathHelper.Clamp(v,0,100)); SetPoints(SpeedControl, PowerControl, YieldControl); };
        }


        
    }



}