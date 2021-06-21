using System.Collections.Generic;
using Digi;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using ServerMod;
using SpaceEngineers.Game.ModAPI;
using VRage.Utils;

namespace Scripts.Specials.Safezones
{
    public class CustomSaveZoneControls
    {
        public static void CreateControls()
        {
            var vanilaControls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMySafeZoneBlock>(out vanilaControls);
            var AutoEnableChkBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMySafeZoneBlock>("Safezones.AutoEnable");
            AutoEnableChkBox.Title = MyStringId.GetOrCompute("AutoEnable");
            AutoEnableChkBox.Tooltip = MyStringId.GetOrCompute("Auto turn on safezone if allowed.");
            AutoEnableChkBox.SupportsMultipleBlocks = true;
            AutoEnableChkBox.Getter = (Block) =>
            {
                return Block.GetAs<CustomSafeZoneLogic>().IsAutoEnable;
            };
            AutoEnableChkBox.Setter = (Block, value) =>
            {
                Block.GetAs<CustomSafeZoneLogic>().IsAutoEnable = value;
                CustomSafeZoneLogic.sync.SendMessageToServer(Block.EntityId, value);
            };
            AutoEnableChkBox.OnText = MySpaceTexts.SwitchText_On;
            AutoEnableChkBox.OffText = MySpaceTexts.SwitchText_Off;
            AutoEnableChkBox.Enabled = Block => Block.GetAs<CustomSafeZoneLogic>()?.CheckBoxEnabled(Block) ?? false;
            AutoEnableChkBox.Visible = Block => Block.GetAs<CustomSafeZoneLogic>()?.Visibility(AutoEnableChkBox, Block) ?? false;
            MyAPIGateway.TerminalControls.AddControl<IMySafeZoneBlock>(AutoEnableChkBox);
        }
    }
}
