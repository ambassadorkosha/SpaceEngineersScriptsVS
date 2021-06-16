using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Shared;
using Scripts.Specials.Blocks.Reactions;
using Scripts.Specials.Messaging;
using Slime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace Scripts.Specials.Blocks.Reactions
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false)]
    public class ThrusterReactionsOnKeys : BlockReactionsOnKeys
    {

        private static bool Initialized = false;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            if (!Initialized)
            {
                Initialized = true;
                InitBlockReactionsActions<ThrusterReactionsOnKeys, IMyThrust>("Thruster");
                InitControls();
            }
        }

        public static void InitControls ()
        {
            MyAPIGateway.TerminalControls.CreateSlider<ThrusterReactionsOnKeys, IMyThrust>("Thuster_Pitch_Threshold", "Pitch React (Left/Right)", "Minimal amount of ", -0.9f, 0.9f, (x) => GetValue(x, "PITCH"), (x, v) => GetDescription(x, "PITCH", v), (x,v)=> SetReaction (x, "PITCH", v));
            MyAPIGateway.TerminalControls.CreateSlider<ThrusterReactionsOnKeys, IMyThrust>("Thuster_Yaw_Threshold", "Yaw React (Up/Down)", "Pick how it will react on your control", -0.9f, 0.9f, (x) => GetValue(x, "YAW"), (x, v) => GetDescription(x, "YAW", v), (x, v) => SetReaction(x, "YAW", v));
            MyAPIGateway.TerminalControls.CreateSlider<ThrusterReactionsOnKeys, IMyThrust>("Thuster_Roll_Threshold", "Roll React (Q/E)", "Pick how it will react on your control", -0.9f, 0.9f, (x) => GetValue(x, "ROLL"), (x, v) => GetDescription (x, "ROLL", v), (x, v) => SetReaction(x, "ROLL", v));
        }

        static Regex RemoveSpaceDuplicatesRegex = new Regex("[ ]{2,}", RegexOptions.None);
        
        public static string RemoveSpaceDuplicates(string s)
        {
            return RemoveSpaceDuplicatesRegex.Replace(s, " ");
        }


        public static void SetReaction (ThrusterReactionsOnKeys logic, string name, float value)
        {
            try
            {
                string vv = "";
                string vv1 = (value < 0 ? "I" : "") + name;
                string vv2 = (value < 0 ? "" : "I") + name;
                if (value != 0)
                {

                    vv = (value < 0 ? "I" : "") + name + ":" + Math.Abs(value) + "=100";
                }

                string newV = logic.reactionString ?? "";

                
                if (value == 0)
                {
                    newV = ReactionSet.Set(newV, vv1, "");
                    newV = ReactionSet.Set(newV, vv2, "");
                }
                else
                {
                    newV = ReactionSet.Set(newV, vv1, vv);
                    newV = ReactionSet.Set(newV, vv2, "");
                    newV = ReactionSet.Set(newV, "NONE", "NONE=0");
                }

                newV = RemoveSpaceDuplicates(newV);
                logic.reactionString = newV;
                logic.OnOptionsChanged();
            } catch (Exception e)
            {
                Log.ChatError (e);
            }
            
        }

        public static float GetValue(ThrusterReactionsOnKeys logic, string name)
        {
            try
            {
                if (logic.reactionString == null || logic.reactionString == "")
                {
                    return 0;
                }
                var v = ReactionSet.GetThreshold(logic.reactionString, name);
                if (v.HasValue) return v.Value;
                v = ReactionSet.GetThreshold(logic.reactionString, "I" + name);
                if (v.HasValue) return -v.Value;
                return 0;
            }
            catch (Exception e)
            {
                Log.ChatError(e);
                return 0;
            }
            
        }

        public static void GetDescription(ThrusterReactionsOnKeys logic, string name, StringBuilder sb)
        {
            try
            {
                var v = GetValue(logic, name);
                if (v == 0)
                {
                    sb.Append("0 (Disabled)");
                }
                else
                {
                    sb.Append(v);
                }
            }
            catch (Exception e)
            {
                Log.ChatError(e);
                sb.Append("Error");
            }
            
        }

        public override void OnOptionsChanged()
        {
            keyReactions = ReactionSet.Parse(reactionString, AddReactions);
        }

        public ThrusterReactions AddReactions(string action, string actionInfo, List<VRage.Input.MyKeys> keys, List<MoveIndicator> indicators)
        {
            //Log.ChatError ($"AddReactions: [{action}] [{actionInfo}] [{keys.Count}] [{indicators.Count}]");

            var reactions = new List<KeysReactions>();
            float overrideValue;

            if (float.TryParse(action, out overrideValue))
            {
                return new ThrusterReactions()
                {
                    keys = keys,
                    moveIndicators = indicators,
                    power = overrideValue / 100f,
                    thrust = Entity as IMyThrust
                };
            }

            return null;
        }

        public override IMyTerminalControlTextbox GetControl()
        {
            return null;
        }
    }
}
