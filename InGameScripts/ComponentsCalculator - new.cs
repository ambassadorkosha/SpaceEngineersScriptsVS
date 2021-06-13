using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRageMath;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Digi;
using ServerMod;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Scripts.Shared.GUI;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.GameServices;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace MyMod.Specials 
{
    [ProtoContract]
    public class CompCalcParams
    {
        [ProtoMember(1)] public Mode Mode = Mode.Help;
        [ProtoMember(2)] public GridConnections gridLinkType = GridConnections.Grid;
        [ProtoMember(3)] public BlockMode blockMode = BlockMode.Current;
        [ProtoMember(4)] public FilterMode filterMode = FilterMode.None;
        [ProtoMember(5)] public bool isApiEnabled;
        [ProtoMember(6)] public Language Lang = Language.Client;
    }
    public enum GridConnections
    {
        Grid = 0,
        Subparts = 1,
        Connectors = 2,
        LandingGear = 3
    }
    public enum Mode {
        Help = 0,
        Blocks = 1,
        Ores = 2,
        Ingots = 3,
        IngotsPlus = 4
    }
    public enum BlockMode
    {
        Current = 0,
        Total = 1,
        Inventory = 2,
        Missing = 3
    }
    public enum FilterMode
    {
        None = 0,
        Default = 1,
        Blacklist = 2,
        Whitelist = 3
    }
    public enum Language
    {
        Client = 0,
        English = 1,
        Russian = 2,
    }

    [MyTextSurfaceScript("ComponentsCalculatorNew", "Components Calculator Nebula")]
    internal class ComponentsCalculatorNew : MyTSSCommon 
    {
        private readonly IMyTextSurface _surface;
        private readonly IMyCubeBlock _block;
        private readonly Vector2 _size;
        private readonly IMyTerminalBlock _terminalBlock;
        private readonly RectangleF _viewport;

        private bool enabled = true;
        private List<string> filter = new List<string>();

        private CompCalcParams Set = new CompCalcParams();
        
        private GUIBase Settings;
        private bool IsSettingsOn;
        private GUIBase Main;

        private GUIBase Error;
        private CStaticText ErrorMessage;
        private CButton ErrorButton;
        
        private CCalculatorText CalculatorText;
        private CDropDownList Language_DropDownList;
        private CDropDownList Mode_DropDownList;
        private CDropDownList gridLink_DropDownList;
        private CStaticText blockModeText;
        private CStaticText filterModeText;
        private CDropDownList blockMode_DropDownList;
        private CDropDownList filterMode_DropDownList;
        private CCheckBox apiCCheckBox;
        

        public ComponentsCalculatorNew(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size) {
            try
            {
                _surface = surface;
                _block = block;
                _size = surface.SurfaceSize;
                _terminalBlock = block as IMyTerminalBlock;
                _viewport = new RectangleF((size - _size) / 2, _size);

                MainScreen();
                SettingScreen();
                errorScreen();
                
            }
            catch (Exception exception)
            {
                Log.ChatError(exception);
                //HandleException(exception, "init");
            }
        }

        private void MainScreen()
        {
            try
            {
                Main = new GUIBase();
                CalculatorText = new CCalculatorText(_viewport, _size, "", m_foregroundColor);
                var SettingsButton = new CButton(new RectangleF(_viewport.Width - 140, 0, 140.0f, 30.0f), "Settings", m_foregroundColor, null, delegate { IsSettingsOn = !IsSettingsOn; });

                Main.AddControl(SettingsButton);
                Main.AddControl(CalculatorText);
            }
            catch(Exception exception){HandleException(exception, "MainScreen");}
        }
        private void SettingScreen()
        {
            try
            {
                Settings = new GUIBase();
                
                var CloseButton = new CButton(new RectangleF(_viewport.Width - 140, 0, 140.0f, 30.0f), "Close", m_foregroundColor, null, delegate { IsSettingsOn = !IsSettingsOn; });
                Settings.AddControl(CloseButton);

                var DDSize = new Vector2(150, 20);
                var textAlignment = TextAlignment.RIGHT;
                
                var LanguageText = new CStaticText(new RectangleF(20.0f, 20.0f, 150.0f, 20.0f), "Language", m_foregroundColor, true, textAlignment);
                Language_DropDownList = new CDropDownList(new RectangleF(new Vector2(200, 20), DDSize), "", m_foregroundColor, save);
                Language_DropDownList.AddItem("Client");
                Language_DropDownList.AddItem("English");
                Language_DropDownList.AddItem("Russian");
                Language_DropDownList.SelectedItem = (int) Set.Lang;
                
                Settings.AddControl(LanguageText);
                Settings.AddControl(Language_DropDownList);
                
                var gridLinkTypeText = new CStaticText(new RectangleF(20.0f, 70.0f, 150.0f, 20.0f), "Grid Connections", m_foregroundColor, true, textAlignment);
                gridLink_DropDownList = new CDropDownList(new RectangleF(new Vector2(200, 70), DDSize), "", m_foregroundColor, save);
                gridLink_DropDownList.AddItem("Grid");
                gridLink_DropDownList.AddItem("Subparts");
                gridLink_DropDownList.AddItem("Connectors");
                gridLink_DropDownList.AddItem("LandingGear");
                gridLink_DropDownList.SelectedItem = (int) Set.gridLinkType;

                Settings.AddControl(gridLinkTypeText);
                Settings.AddControl(gridLink_DropDownList);

                var ModeText = new CStaticText(new RectangleF(20.0f, 120.0f, 150.0f, 20.0f), "Mode", m_foregroundColor, true, textAlignment);
                Mode_DropDownList = new CDropDownList(new RectangleF(new Vector2(200, 120), DDSize), "", m_foregroundColor, save);
                Mode_DropDownList.AddItem("Help");
                Mode_DropDownList.AddItem("Blocks");
                Mode_DropDownList.AddItem("Ores");
                Mode_DropDownList.AddItem("Ingots");
                Mode_DropDownList.AddItem("IngotsPlus");
                Mode_DropDownList.SelectedItem = (int) Set.Mode;

                Settings.AddControl(ModeText);
                Settings.AddControl(Mode_DropDownList);

                blockModeText = new CStaticText(new RectangleF(20.0f, 160.0f, 150.0f, 20.0f), "Block Mode", m_foregroundColor, true, textAlignment);
                blockMode_DropDownList = new CDropDownList(new RectangleF(new Vector2(200, 160), DDSize), "", m_foregroundColor, save);
                blockMode_DropDownList.AddItem("Current");
                blockMode_DropDownList.AddItem("Total");
                blockMode_DropDownList.AddItem("Inventory");
                blockMode_DropDownList.AddItem("Missing");
                blockMode_DropDownList.SelectedItem = (int) Set.blockMode;

                Settings.AddControl(blockModeText);
                Settings.AddControl(blockMode_DropDownList);

                filterModeText = new CStaticText(new RectangleF(20.0f, 200.0f, 150.0f, 20.0f), "filter Mode", m_foregroundColor, true, textAlignment);
                filterMode_DropDownList = new CDropDownList(new RectangleF(new Vector2(200, 200), DDSize), "", m_foregroundColor);
                filterMode_DropDownList.AddItem("None");
                filterMode_DropDownList.AddItem("Default");
                filterMode_DropDownList.AddItem("Blacklist");
                filterMode_DropDownList.AddItem("Whitelist");
                filterMode_DropDownList.SelectedItem = (int) Set.filterMode;

                Settings.AddControl(filterModeText);
                Settings.AddControl(filterMode_DropDownList);

                apiCCheckBox = new CCheckBox(new RectangleF(90.0f, 240.0f, 200.0f, 20.0f), "Enable Api?", m_foregroundColor, save) {Checked = Set.isApiEnabled};
                Settings.AddControl(apiCCheckBox);
            }
            catch (Exception exception) { HandleException(exception, "SettingScreen"); }
        }
        private void errorScreen()
        {
            try
            {
                Error = new GUIBase();

                ErrorMessage = new CStaticText(new RectangleF(new Vector2(), new Vector2(100, 20)), "", m_foregroundColor, true, TextAlignment.LEFT);
                ErrorButton = new CButton(new RectangleF(_viewport.Width - 140, 0, 140.0f, 30.0f), "Reset", m_foregroundColor, null,
                    delegate
                    {
                        Set = new CompCalcParams(); 
                        enabled = true; 
                        RewriteData();
                    });

                Error.AddControl(ErrorMessage);
                Error.AddControl(ErrorButton);
            }
            catch (Exception exception) { HandleException(exception, "errorScreen"); }
        }
        private void save(Control control)
        {
            try
            {
                Set.gridLinkType = (GridConnections) gridLink_DropDownList.SelectedItem;
                Set.Mode = (Mode)Mode_DropDownList.SelectedItem;
                Set.blockMode = (BlockMode) blockMode_DropDownList.SelectedItem;
                Set.filterMode = (FilterMode) filterMode_DropDownList.SelectedItem;
                Set.isApiEnabled = apiCCheckBox.Checked;
                Set.Lang = (Language) Language_DropDownList.SelectedItem;

                RewriteData(false);
            } 
            catch (Exception exception) { HandleException(exception, "save"); }
        }
        public override void Run()
        {
            base.Run();

            GetData();
            
            if (!enabled)
            {
                Error.DrawInto(this, m_size);
                return;
            }

            if (Set.Mode == Mode.Help)
            {
                HandleException(null);
                Error.DrawInto(this, m_size);
                return;
            }

            try
            {
                var data = new List<string>();
                
                var blocks = GetBlocks(GetLinkTypeEnum(Set.gridLinkType));
                Dictionary<string, double> items;

                if (Set.Mode == Mode.IngotsPlus)
                {
                    try
                    {
                        Dictionary<string, double[]> report = new Dictionary<string, double[]>();
                        Func<string, bool, string> GetDictId = (name, isOreIngot) =>
                        {
                            string id = ((isOreIngot ? ItemsLibrary.IngotToReportLink : ItemsLibrary.OreToReportLink).ContainsKey(name) ? (isOreIngot ? ItemsLibrary.IngotToReportLink : ItemsLibrary.OreToReportLink)[name] : name);
                            if (!report.ContainsKey(id)) report[id] = new double[2];
                            return id;
                        };

                        items = GetItems(blocks, Mode.Ingots);
                        foreach (KeyValuePair<string, double> kvp in items)
                        {
                            report[GetDictId(kvp.Key, true)][1] += kvp.Value;
                        }

                        items.Clear();
                        items = GetItems(blocks, Mode.Ores);
                        foreach (KeyValuePair<string, double> kvp in items)
                        {
                            report[GetDictId(kvp.Key, false)][0] += kvp.Value;
                        }

                        foreach (var kvp in report.OrderBy(key => key.Key))
                        {
                            if (kvp.Key.Equals("Ice") || kvp.Key.Equals("Oil")) data.Add($"{kvp.Key}: {kvp.Value[0].toHumanQuantity()}");
                            else data.Add($"{Translate(kvp.Key,ItemsLibrary.Translation, (int)Set.Lang)}: {kvp.Value[1].toHumanQuantity()} ({kvp.Value[0].toHumanQuantity()})");
                        }
                    }
                    catch (Exception ex)
                    {
                        data.Add("EXCEPTION");
                        _surface.WriteText(ex.ToString());
                    }
                }
                else
                {
                    items = GetItems(blocks, Set.Mode);
                    data.AddRange(items.OrderBy(key => key.Key).Select(item => $"{Translate(item.Key, ItemsLibrary.Translation, (int)Set.Lang)}: {item.Value.toHumanQuantity()}"));
                    if (Set.isApiEnabled)
                    {
                        DisplayData(items);
                    }
                }

                if (!IsSettingsOn)
                {
                    CalculatorText.data = data;
                    CalculatorText.color = m_foregroundColor;
                    CalculatorText.makeColumns = true;
                    Main.DrawInto(this, m_size);
                }
                else
                {
                    var Visible = (Mode) Mode_DropDownList.SelectedItem == Mode.Blocks;
                    blockMode_DropDownList.IsVisible = Visible;
                    blockModeText.IsVisible = Visible;

                    Settings.DrawInto(this, m_size);
                }
            }
            catch (Exception exception)
            {
                HandleException(exception, "Run");
            }
        }
        private static GridLinkTypeEnum? GetLinkTypeEnum(GridConnections gc) {
            switch (gc) {
                case GridConnections.Grid: return null;
                case GridConnections.Subparts: return GridLinkTypeEnum.Mechanical;
                case GridConnections.Connectors: return GridLinkTypeEnum.Logical;
                case GridConnections.LandingGear: return GridLinkTypeEnum.NoContactDamage;
                default: return null;
            }
        }

        private string Translate(string ToTranslate, IReadOnlyDictionary<string, List<string>> Library, int InLang)
        {
            return InLang != (int)Language.Client ? (Library.ContainsKey(ToTranslate) ? Library[ToTranslate][InLang-1] : ToTranslate) : ToTranslate;
        }


        private void GetData() {
            try {
                if (_terminalBlock.CustomData == "")
                {
                    RewriteData(false);
                    return;
                }
                var ini = new MyIni();
                var reset = false;
                if (!ini.TryParse(_terminalBlock.CustomData, "settings")) {
                    reset = true;
                } else
                {
                    if (!ini.Get("settings", "enabled").TryGetBoolean(out enabled))
                    {
                        enabled = true;
                        reset = true;
                    }
                    if (!Enum.TryParse(ini.Get("settings", "Language").ToString(), true, out Set.Lang))
                    {
                        Set.Lang = Language.English;
                        reset = true;
                    }
                    if (!Enum.TryParse(ini.Get("settings", "gridLinkType").ToString(), true, out Set.gridLinkType))
                    {
                        Set.gridLinkType = GridConnections.Grid;
                        reset = true;
                    }
                    else { gridLink_DropDownList.SelectedItem = (int) Set.gridLinkType; }
                    if (!Enum.TryParse(ini.Get("settings", "mode").ToString(), true, out Set.Mode))
                    {
                        Set.Mode = Mode.Help;
                        reset = true;
                    }
                    else { Mode_DropDownList.SelectedItem = (int) Set.Mode; }
                    if (!Enum.TryParse(ini.Get("settings", "blockMode").ToString(), true, out Set.blockMode))
                    {
                        Set.blockMode = BlockMode.Current;
                        reset = true;
                    }
                    else { blockMode_DropDownList.SelectedItem = (int) Set.blockMode; }
                    if (!Enum.TryParse(ini.Get("settings", "filterMode").ToString(), true, out Set.filterMode))
                    {
                        Set.filterMode = FilterMode.Default;
                        reset = true;
                    }
                    else { filterMode_DropDownList.SelectedItem = (int) Set.filterMode; }
                    if (!ini.Get("settings", "enableApi").TryGetBoolean(out Set.isApiEnabled))
                    {
                        Set.isApiEnabled = false;
                        reset = true;
                    }
                    else { apiCCheckBox.Checked = Set.isApiEnabled; }
                    string flt;
                    if(!ini.Get("settings", "filter").TryGetString(out flt))
                    {
                        filter.Clear();
                        reset = true;
                    } else
                    {
                        filter = flt.Replace(" ", "").Split(',').ToList();
                    }
                }

                if (reset) RewriteData();
            } catch (Exception exception) { HandleException(exception); }
        }
        private void RewriteData(bool reset = true) {
            MyIni ini = new MyIni();
            if (reset) { Set = new CompCalcParams {Mode = Mode.Blocks}; }
            if (m_block is IMyCockpit) Set.Mode = Mode.Blocks;
            
            ini.Set("settings", "enabled", enabled);
            ini.Set("settings", "Language", Set.Lang.ToString());
            ini.Set("settings", "mode", Set.Mode.ToString());
            ini.Set("settings", "gridLinkType", Set.gridLinkType.ToString());
            ini.Set("settings", "blockMode", Set.blockMode.ToString());
            ini.Set("settings", "filterMode", Set.filterMode.ToString());
            ini.Set("settings", "filter", filter.Count == 0 ? "None" : string.Join(",", filter));
            ini.Set("settings", "enableApi", Set.isApiEnabled);
            
            var sb = new StringBuilder();
            sb.Append("\r\n;====================HOW TO:===================================");
            sb.Append("\r\n;enabled : true/false");
            sb.Append("\r\n;Language : ["+string.Join(",", Enum.GetNames(typeof(Language)))+ "]");
            sb.Append("\r\n;mode : ["+string.Join(",", Enum.GetNames(typeof(Mode)))+"] < change this");
            sb.Append("\r\n;gridLinkType : [" + string.Join(",", Enum.GetNames(typeof(GridConnections)))+"]");
            sb.Append("\r\n;blockMode : [" + string.Join(",", Enum.GetNames(typeof(BlockMode))) + "] (many separated with `,` )");
            sb.Append("\r\n;filterMode : [" + string.Join(",", Enum.GetNames(typeof(FilterMode)))+"]");
            sb.Append("\r\n;filter : None or text");
            sb.Append("\r\n;enableApi : true/false");
            
            SetCustomDData (ini.ToString() + sb.ToString());
        }
        private void DisplayData(Dictionary<string, double> items)
        {
            var ini = new MyIni();
            var data = string.Join(",", items.Select(item => $"{item.Key}:{item.Value}"));

            ini.TryParse(_terminalBlock.CustomData);
            ini.Set("api", "items", data);
            
            SetCustomDData (ini.ToString());
        }
        
        private Dictionary<string, double> GetItems(List<IMySlimBlock> blocks, Mode what_to_get)
        { 
            Dictionary<string, double> items = new Dictionary<string, double>(StringComparer.InvariantCultureIgnoreCase);
            var gotInvetory = false;
            if (what_to_get == Mode.Blocks) {
                if (Set.blockMode == BlockMode.Current) { blocks.ForEach(block => block.GetComponents(items)); }
                if (Set.blockMode == BlockMode.Total ) { blocks.ForEach(block => block.GetTotalComponents(items)); }
                if (Set.blockMode == BlockMode.Missing) { blocks.ForEach(block => block.GetMissingComponents(items)); }
                if (Set.blockMode == BlockMode.Inventory)
                {
                    blocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items,"","",Set.Lang == Language.Client,true));
                    gotInvetory = true;
                    
                    if (Set.filterMode == FilterMode.Default) 
                    {
                        var itemsToRemove = items.Keys.Except(ItemsLibrary.defaultComponents).ToList();
                        foreach (var item in itemsToRemove) items.Remove(item);
                    }
                }
            }

            if (what_to_get == Mode.Ores) { blocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items, "MyObjectBuilder_Ore", "", Set.Lang == Language.Client, true)); gotInvetory = true;}
            if (what_to_get == Mode.Ingots) { blocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items, "MyObjectBuilder_Ingot", "", Set.Lang == Language.Client, true)); gotInvetory = true;}
            if (Set.filterMode == FilterMode.Blacklist) { foreach (var item in filter) items.Remove(item);
            } 
            else if (Set.filterMode == FilterMode.Whitelist) 
            {
                var itemsToRemove = items.Keys.Except(filter, StringComparer.InvariantCultureIgnoreCase).ToList();
                foreach (var item in itemsToRemove) items.Remove(item);
            }

            if (!gotInvetory && Set.Lang == Language.Client)
            {
                var Translations = new Dictionary<string, string>();
                var TransItems = new Dictionary<string, double>(StringComparer.InvariantCultureIgnoreCase);
                blocks.ForEach(block => block.GetComponentsTranslation(Translations));
                foreach (var item in items.Where(item => Translations.ContainsKey(item.Key)))
                {
                    var TextName = Translations[item.Key];
                    var Value = item.Value;
                    
                    TransItems.Add(TextName,Value);
                }
                return TransItems;
            }
            return items;
        }

        private void SetCustomDData (string x)
        {
            if (_terminalBlock.CustomData != x) _terminalBlock.CustomData = x;
        }
        
        private List<IMySlimBlock> GetBlocks(GridLinkTypeEnum? _linkType) {
            var blocks = new List<IMySlimBlock>();
            var cubeGrid = _block.CubeGrid;

            if (_linkType.HasValue) { MyAPIGateway.GridGroups.GetGroup(cubeGrid, _linkType.Value).ForEach(grid => grid.GetBlocks(blocks)); }
            else { cubeGrid.GetBlocks(blocks); }
            return blocks;
        }
        
        private void HandleException(Exception exception, string where = "null") 
        {
            if (exception == null)
            {
                var buffer = new List<string>();
                if (Set.Mode == Mode.Help) {
                    buffer.Add("Component counter");
                    buffer.Add("Open LCD Custom Data to edit");
                    buffer.Add("Or press reset for default configuration");
                    buffer.Add("=============================");
                    buffer.Add("Счётчик компонентов");
                    buffer.Add("Настройки находятся в Custom Data");
                    buffer.Add("Или нажмите Reset для стандартных настроек");
                    
                    buffer.Add("\r\n;====================HOW TO:===================================");
                    buffer.Add("\r\n;enabled : true/false");
                    buffer.Add("\r\n;Language : ["+string.Join(",", Enum.GetNames(typeof(Language)))+ "]");
                    buffer.Add("\r\n;mode : ["+string.Join(",", Enum.GetNames(typeof(Mode)))+"] < change this");
                    buffer.Add("\r\n;gridLinkType : [" + string.Join(",", Enum.GetNames(typeof(GridConnections)))+"]");
                    buffer.Add("\r\n;blockMode : [" + string.Join(",", Enum.GetNames(typeof(BlockMode))) + "] (many separated with `,` )");
                    buffer.Add("\r\n;filterMode : [" + string.Join(",", Enum.GetNames(typeof(FilterMode)))+"]");
                    buffer.Add("\r\n;filter : None or text");
                    buffer.Add("\r\n;enableApi : true/false");
                }
                ErrorMessage.Text.Title = string.Join("\n", buffer);
            }
            else
            {
                enabled = false;
                ErrorMessage.Text.Title = "In " + where + ": " + exception.Message;
            }
        }
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;
    }
}