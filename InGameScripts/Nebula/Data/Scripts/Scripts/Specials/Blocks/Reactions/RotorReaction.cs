using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace Scripts.Specials.Blocks.Reactions
{
    //WE Dont have simple rotor

    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorStator), false)]
    //public class BasicRotorReaction : ARotorReaction <IMyMotorBase> {
    //    private static bool Initialized = false;
    //    public override void Init(MyObjectBuilder_EntityBase objectBuilder)
    //    {
    //        base.Init(objectBuilder);
    //        Log.ChatError("INIT");
    //        if (!Initialized)
    //        {
    //            Initialized = true;
    //            InitBlockReactionsActions<BasicRotorReaction, IMyMotorBase>("Rotor");
    //            InitControls();
    //        }
    //    }
    //}

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorAdvancedStator), false)]
    public class AdvancedRotorReaction : ARotorReaction<IMyMotorAdvancedStator> {
        private static bool Initialized = false;
        private static IMyTerminalControlTextbox TextBox;

        public override IMyTerminalControlTextbox GetControl()
        {
            return TextBox;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            if (!Initialized)
            {
                Initialized = true;
                TextBox = InitBlockReactionsActions<AdvancedRotorReaction, IMyMotorAdvancedStator>("AdvRotor");
                InitControls();
            }
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_PistonBase), false)]
    public class PistonReaction : ExtendedReactions<IMyPistonBase>
    {
        private static bool Initialized = false;
        private static IMyTerminalControlTextbox TextBox;

        public override IMyTerminalControlTextbox GetControl() { return TextBox; }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            if (!Initialized) {
                Initialized = true;
                InitControls();
            }
        }

        public static void InitControls()
        {
            TextBox = InitBlockReactionsActions<PistonReaction, IMyPistonBase>("Piston");

            AddSliderForDoubleMouse<IMyPistonBase>("PistonReaction_Pitch", "Pitch React (Left/Right)", "Rotor RPM on mouse", "PITCH", -30, 30, true);
            AddSliderForDoubleMouse<IMyPistonBase>("PistonReaction_Yaw", "Yaw React (Up/Down)", "Rotor RPM on mouse", "YAW", -30, 30, true);

            AddSliderForDoubleKeys<IMyPistonBase>("PistonReaction", "RPM on Q/E", "Piston velocity on pressing keys", "Q/E", -30, 30, true);
            AddSliderForDoubleKeys<IMyPistonBase>("PistonReaction", "RPM on W/S", "Piston velocity on pressing keys", "W/S", -30, 30, true);
            AddSliderForDoubleKeys<IMyPistonBase>("PistonReaction", "RPM on A/D", "Piston velocity on pressing keys", "A/D", -30, 30, true);

            AddSliderForDoubleKeys<IMyPistonBase>("PistonReaction", "RPM on C/SPACE", "Rotor RPM on pressing keys", "C/SPACE", -30, 30, true);

            AddSliderForDoubleKeys<IMyPistonBase>("PistonReaction", "RPM on NUM7/NUM9", "Rotor RPM on pressing keys", "NUM7/NUM9", -30, 30, false);
            AddSliderForDoubleKeys<IMyPistonBase>("PistonReaction", "RPM on NUM4/NUM6", "Rotor RPM on pressing keys", "NUM4/NUM6", -30, 30, false);
            AddSliderForDoubleKeys<IMyPistonBase>("PistonReaction", "RPM on NUM8/NUM5", "Rotor RPM on pressing keys", "NUM8/NUM5", -30, 30, false);
        }

        public override void OnOptionsChanged()
        {
            keyReactions = ReactionSet.Parse(reactionString, AddReactions);
        }

        public PistonReactions AddReactions(string action, string actionInfo, List<VRage.Input.MyKeys> keys, List<MoveIndicator> indicators)
        {
            var reactions = new List<KeysReactions>();
            float overrideValue;

            if (float.TryParse(action, out overrideValue))
            {
                return new PistonReactions()
                {
                    keys = keys,
                    moveIndicators = indicators,
                    velocity = overrideValue,
                    rotor = Entity as IMyPistonBase
                };
            }

            return null;
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ExtendedPistonBase), false)]
    public class ExtPistonReaction : BlockReactionsOnKeys
    {
        private static bool Initialized = false;
        private static IMyTerminalControlTextbox TextBox;

        public override IMyTerminalControlTextbox GetControl() { return TextBox; }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            if (!Initialized)
            {
                Initialized = true;
                InitControls();
            }
        }

        public static void InitControls()
        {
            TextBox = InitBlockReactionsActions<ExtPistonReaction, IMyExtendedPistonBase>("ExtPiston");

            AddSliderForDoubleMouse<IMyExtendedPistonBase>("ExtPistonReaction_Pitch", "Pitch React (Left/Right)", "Rotor RPM on mouse", "PITCH", -30, 30, true);
            AddSliderForDoubleMouse<IMyExtendedPistonBase>("ExtPistonReaction_Yaw", "Yaw React (Up/Down)", "Rotor RPM on mouse", "YAW", -30, 30, true);

            AddSliderForDoubleKeys<IMyExtendedPistonBase>("ExtPistonReaction", "Velocity on Q/E", "Piston velocity on pressing keys", "Q/E", -30, 30, true);
            AddSliderForDoubleKeys<IMyExtendedPistonBase>("ExtPistonReaction", "Velocity  on W/S", "Piston velocity on pressing keys", "W/S", -30, 30, true);
            AddSliderForDoubleKeys<IMyExtendedPistonBase>("ExtPistonReaction", "Velocity  on A/D", "Piston velocity on pressing keys", "A/D", -30, 30, true);

            AddSliderForDoubleKeys<IMyExtendedPistonBase>("ExtPistonReaction", "Velocity on C/SPACE", "Piston velocity on pressing keys", "C/SPACE", -30, 30, true);

            AddSliderForDoubleKeys<IMyExtendedPistonBase>("ExtPistonReaction", "Velocity on NUM7/NUM9", "Piston velocity on pressing keys", "NUM7/NUM9", -30, 30, false);
            AddSliderForDoubleKeys<IMyExtendedPistonBase>("ExtPistonReaction", "Velocity on NUM4/NUM6", "Piston velocity on pressing keys", "NUM4/NUM6", -30, 30, false);
            AddSliderForDoubleKeys<IMyExtendedPistonBase>("ExtPistonReaction", "Velocity on NUM8/NUM5", "Piston velocity on pressing keys", "NUM8/NUM5", -30, 30, false);
        }

        public override void OnOptionsChanged()
        {
            keyReactions = ReactionSet.Parse(reactionString, AddReactions);
        }

        public PistonReactions AddReactions(string action, string actionInfo, List<VRage.Input.MyKeys> keys, List<MoveIndicator> indicators)
        {
            var reactions = new List<KeysReactions>();
            float overrideValue;

            if (float.TryParse(action, out overrideValue))
            {
                return new PistonReactions()
                {
                    keys = keys,
                    moveIndicators = indicators,
                    velocity = overrideValue,
                    rotor = Entity as IMyPistonBase
                };
            }

            return null;
        }
    }

    public abstract class ExtendedReactions<T> : BlockReactionsOnKeys {

        
    }

    public abstract class ARotorReaction<T> : BlockReactionsOnKeys
    {
        public static void InitControls ()
        {
            AddSliderForDoubleMouse<T> ("RotorReaction_Pitch", "Pitch React (Left/Right)", "Rotor RPM on mouse", "PITCH", -30, 30, true);
            AddSliderForDoubleMouse<T> ("RotorReaction_Yaw", "Yaw React (Up/Down)", "Rotor RPM on mouse", "YAW", -30, 30, true);
            
            AddSliderForDoubleKeys<T> ("RotorReaction", "RPM on Q/E", "Rotor RPM on pressing keys", "Q/E", -30, 30, true);
            AddSliderForDoubleKeys<T> ("RotorReaction", "RPM on W/S", "Rotor RPM on pressing keys", "W/S", -30, 30, true);
            AddSliderForDoubleKeys<T>("RotorReaction", "RPM on A/D", "Rotor RPM on pressing keys", "A/D", -30, 30, true);

            AddSliderForDoubleKeys<T>("RotorReaction", "RPM on C/SPACE", "Rotor RPM on pressing keys", "C/SPACE", -30, 30, true);

            AddSliderForDoubleKeys<T> ("RotorReaction", "RPM on NUM7/NUM9", "Rotor RPM on pressing keys","NUM7/NUM9", -30, 30, false);
            AddSliderForDoubleKeys<T> ("RotorReaction", "RPM on NUM4/NUM6", "Rotor RPM on pressing keys","NUM4/NUM6", -30, 30, false);
            AddSliderForDoubleKeys<T>("RotorReaction", "RPM on NUM8/NUM5", "Rotor RPM on pressing keys", "NUM8/NUM5", -30, 30, false);
        }

        public override void OnOptionsChanged()
        {
            keyReactions = ReactionSet.Parse(reactionString, AddReactions);
        }

        public RotorReactions AddReactions(string action, string actionInfo, List<VRage.Input.MyKeys> keys, List<MoveIndicator> indicators)
        {
            var reactions = new List<KeysReactions>();
            float overrideValue;

            if (float.TryParse(action, out overrideValue))
            {
                return new RotorReactions()
                {
                    keys = keys,
                    moveIndicators = indicators,
                    velocity = overrideValue,
                    rotor = Entity as IMyMotorStator
                };
            }
            
            return null;
        }
    }
}
