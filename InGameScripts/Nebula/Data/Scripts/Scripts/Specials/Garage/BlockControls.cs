using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Scripts.Specials.SlimGarage
{
    public class BlockControls
    {
        public static void CreateControls()
        {
            SlimGarage.WriteToLogDbg("CreateControls start.");
            var vanilaControls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMyProjector>(out vanilaControls);

            vanilaControls[9].Visible = Block => Hide(Block);
            vanilaControls[10].Visible = Block => Hide(Block);
            vanilaControls[11].Visible = Block => Hide(Block);
            vanilaControls[8].Visible = Block => Hide(Block);
            vanilaControls[29].Visible = Block => Hide(Block);
            vanilaControls[30].Visible = Block => Hide(Block);
            vanilaControls[31].Visible = Block => Hide(Block);
            vanilaControls[27].Visible = Block => Hide(Block);
            vanilaControls[26].Visible = Block => Hide(Block);
            // foreach(var item in controlList)
            // {
            // Logging.Instance.WriteLine(item.Id + " " + controlList.IndexOf(item));
            // }
            var KeepBuilderChkBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyProjector>("SlimGarage.KeepBuilderChkBox");
            KeepBuilderChkBox.Title = MyStringId.GetOrCompute("Keep original builder");
            KeepBuilderChkBox.Tooltip = MyStringId.GetOrCompute("Saves original builder on spawn, you can lose PCU/limits!");
            KeepBuilderChkBox.SupportsMultipleBlocks = true;
            KeepBuilderChkBox.Getter = (Block) =>
            {
                return Block.GetAs<GarageBlockLogic>().KeepBuilder;
            };
            KeepBuilderChkBox.Setter = (Block, value) =>
            {
                Block.GetAs<GarageBlockLogic>().KeepBuilder = value;
            };
            KeepBuilderChkBox.OnText = MySpaceTexts.SwitchText_On;
            KeepBuilderChkBox.OffText = MySpaceTexts.SwitchText_Off;
            KeepBuilderChkBox.Enabled = Block => Block.GetAs<GarageBlockLogic>()?.CheckBoxEnabled(Block) ?? false;
            KeepBuilderChkBox.Visible = Block => Block.GetAs<GarageBlockLogic>()?.Visibility(KeepBuilderChkBox, Block) ?? false;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(KeepBuilderChkBox);

            var GridsList = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyProjector>("SlimGarage.GridsList");
            GridsList.Title = MyStringId.GetOrCompute("Grid groups for save: ");
            GridsList.SupportsMultipleBlocks = false;
            GridsList.VisibleRowsCount = 12;
            GridsList.Multiselect = false;
            GridsList.ListContent = GarageBlockLogic.GetGridsAround;
            GridsList.ItemSelected = GarageBlockLogic.SetSelectedGrid;
            GridsList.Enabled = Block => Block.GetAs<GarageBlockLogic>()?.Visibility(GridsList, Block) ?? false;
            GridsList.Visible = Block => Block.GetAs<GarageBlockLogic>()?.Visibility(GridsList, Block) ?? false;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(GridsList);

            var SaveButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("SlimGarage.SaveButton");
            SaveButton.Title = MyStringId.GetOrCompute("Save this ship");
            SaveButton.Tooltip = MyStringId.GetOrCompute("Saves this ship to the garage");
            SaveButton.SupportsMultipleBlocks = false;
            SaveButton.Action = (b) => b.GetAs<GarageBlockLogic>()?.Click(SaveButton);
            SaveButton.Enabled = Block => Block.GetAs<GarageBlockLogic>()?.ButtonsCooldown(SaveButton) ?? false;
            SaveButton.Visible = Block => Block.GetAs<GarageBlockLogic>()?.Visibility(SaveButton, Block) ?? false;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(SaveButton);

            var ShowButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("SlimGarage.ShowButton");
            ShowButton.Title = MyStringId.GetOrCompute("Show projection");
            ShowButton.Tooltip = MyStringId.GetOrCompute("Show projection for future spawn for 2 minutes");
            ShowButton.SupportsMultipleBlocks = false;
            ShowButton.Action = (b) => b.GetAs<GarageBlockLogic>().Click(ShowButton);
            ShowButton.Enabled = Block => Block.GetAs<GarageBlockLogic>()?.ButtonsCooldown(ShowButton) ?? false;
            ShowButton.Visible = Block => Block.GetAs<GarageBlockLogic>()?.Visibility(ShowButton, Block) ?? false;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(ShowButton);

            var LoadButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("SlimGarage.LoadButton");
            LoadButton.Title = MyStringId.GetOrCompute("Load stored ship");
            LoadButton.Tooltip = MyStringId.GetOrCompute("Load stored ship");
            LoadButton.SupportsMultipleBlocks = false;
            LoadButton.Action = (b) => b.GetAs<GarageBlockLogic>().Click(LoadButton);
            LoadButton.Enabled = Block => Block.GetAs<GarageBlockLogic>()?.ButtonsCooldown(LoadButton) ?? false;
            LoadButton.Visible = Block => Block.GetAs<GarageBlockLogic>()?.Visibility(LoadButton, Block) ?? false;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(LoadButton);

            var label1 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyProjector>("SlimGarage.Sepr1");

            label1.Label = GarageBlockLogic.Separator;
            label1.Visible = Block => Block.GetAs<GarageBlockLogic>()?.Visibility(label1, Block) ?? false;
            label1.Enabled = Block => true;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(label1);
            //"———————Settings for Grinding———————"


            var ClearButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("SlimGarage.ClearButton");
            ClearButton.Title = MyStringId.GetOrCompute("Grind ship");
            ClearButton.Tooltip = MyStringId.GetOrCompute("Clear ship blueprint and disassemble grid into items");
            ClearButton.SupportsMultipleBlocks = false;
            ClearButton.Action = (b) => b.GetAs<GarageBlockLogic>().Click(ClearButton);
            ClearButton.Enabled = Block => Block.GetAs<GarageBlockLogic>()?.ButtonsCooldown(ClearButton) ?? false;
            ClearButton.Visible = Block => Block.GetAs<GarageBlockLogic>()?.Visibility(ClearButton, Block) ?? false;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(ClearButton);

            var label2 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyProjector>("SlimGarage.Sepr2");
            label2.Label = GarageBlockLogic.Separator2;
            label2.Visible = Block => Block.GetAs<GarageBlockLogic>()?.Visibility(label2, Block) ?? false;
            label2.Enabled = Block => true;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(label2);
        }

        /// <summary>
        /// Hide unused vanilla projector controls
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static bool Hide(IMyTerminalBlock block)
        {
            try
            {
                if (block is IMyProjector)
                {
                    if (SlimGarage.GarageSubtypes.Contains(block.BlockDefinition.SubtypeId))
                    {
                        return false;
                    }
                }
            }
            catch { }
            return true;
        }
    }
}
