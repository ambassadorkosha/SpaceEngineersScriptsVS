using Digi;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace Scripts.Shared
{
    public static class GuiHelper
    {
        public static IMyTerminalControlSlider CreateSlider<T,Z> (this IMyTerminalControls system, string id, string name, string tooltip, float min, float max, 
            Func<T, float> getter, Action<T, StringBuilder> writer, Action<T, float> setter, Func<T, bool> enabled= null, Func<T, bool> visible = null, bool update = false) where T : MyGameLogicComponent
        {
            var XControl = system.CreateControl<IMyTerminalControlSlider, Z>(typeof(T).Name + "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);
            XControl.SetLimits(min, max);

            XControl.Enabled = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible == null ? true : enabled(bb);
            };

            XControl.Visible = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible == null ? true : visible(bb);
            };


            XControl.Getter = (b) => getter(b.GetAs<T>());
            XControl.Writer = (b, t) => writer(b.GetAs<T>(), t);
            XControl.Setter = (b, v) =>
            {
                setter(b.GetAs<T>(), v);
                if (update) XControl.UpdateVisual();
            };

            system.AddControl<Z>(XControl);

            return XControl;
        }

        public static IMyTerminalControlButton CreateButton<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip, Action<T> action, Func<T, bool> enabled = null, Func<T, bool> visible = null) where T : MyGameLogicComponent
        {
            var XControl = system.CreateControl<IMyTerminalControlButton, Z>(typeof(T).Name + "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);
            XControl.Enabled = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible == null ? true : enabled(bb);
            };

            XControl.Visible = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible == null ? true : visible(bb);
            };


            XControl.Action = (b) => action(b.GetAs<T>());

            system.AddControl<Z>(XControl);

            return XControl;
        }

        public static IMyTerminalControlColor CreateColorPicker<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip, Func<T, Color> getter, Action<T, Color> setter, Func<T, bool> enabled = null, Func<T, bool> visible = null) where T : MyGameLogicComponent
        {
            var XControl = system.CreateControl<IMyTerminalControlColor, Z>(typeof(T).Name + "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);
            XControl.Enabled = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible == null ? true : enabled(bb);
            };

            XControl.Visible = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible == null ? true : visible(bb);
            };


            XControl.Getter = (b) => getter(b.GetAs<T>());
            XControl.Setter = (b, v) => setter(b.GetAs<T>(), v);

            system.AddControl<Z>(XControl);

            return XControl;
        }

        public static IMyTerminalControlCombobox CreateCombobox<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip, Func<T, long> getter, Action<T, long> setter, List<MyStringId> texts, Func<T, bool> enabled = null, Func<T, bool> visible=null, bool update = false) where T : MyGameLogicComponent
        {
            var XControl = system.CreateControl<IMyTerminalControlCombobox, Z>(typeof(T).Name + "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);

            XControl.Getter = (b) => getter(b.GetAs<T>());
            XControl.Setter = (b, v) =>
            {
                setter(b.GetAs<T>(), v);
                if(update) XControl.UpdateVisual();
            };

            XControl.Enabled = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible == null ? true : enabled(bb);
            };

            XControl.Visible = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible == null ? true : visible(bb);
            };


            XControl.ComboBoxContent = (b) =>
            {
                var c = 0;
                foreach (var x in texts) {
                    var i = new VRage.ModAPI.MyTerminalControlComboBoxItem();
                    i.Key = c;
                    i.Value = x;
                    c++;
                    b.Add(i);
                }
            };
            XControl.SupportsMultipleBlocks = true;
            system.AddControl<Z>(XControl);

            return XControl;
        }


        public static IMyTerminalControlCheckbox CreateCheckbox<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip,
            Func<T, bool> getter,  Action<T, bool> setter, Func<T, bool> enabled = null, Func<T, bool> visible = null, bool update = false) where T : MyGameLogicComponent
        {
            var XControl = system.CreateControl<IMyTerminalControlCheckbox, Z>(typeof(T).Name + "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);

            XControl.Enabled = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible == null ? true : enabled(bb);
            };

            XControl.Visible = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible == null ? true : visible(bb);
            };


            XControl.Getter = (b) => getter(b.GetAs<T>());
            XControl.Setter = (b, v) =>
            {
                setter(b.GetAs<T>(), v);
                if(update) XControl.UpdateVisual();
            };
            
            system.AddControl<Z>(XControl);

            return XControl;
        }


        public static IMyTerminalControlTextbox CreateTextbox<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip,
           Func<T, StringBuilder> getter, Action<T, StringBuilder> setter, Func<T, bool> enabled, Func<T, bool> visible, bool update = false) where T : MyGameLogicComponent
        {
            var XControl = system.CreateControl<IMyTerminalControlTextbox, Z>(typeof(T).Name + "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name ?? "WTF");
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);
            XControl.Enabled = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return enabled(bb);
            };

            XControl.Visible = (b) =>
            {
                var bb = b.GetAs<T>();
                if (bb == null) return false;
                return visible(bb);
            };

            XControl.Getter = (b) => getter(b.GetAs<T>());
            XControl.Setter = (b, v) =>
            {
                setter(b.GetAs<T>(), v);
                if (update) XControl.UpdateVisual();
            };
            system.AddControl<Z>(XControl);
            return XControl;
        }
    }
}
