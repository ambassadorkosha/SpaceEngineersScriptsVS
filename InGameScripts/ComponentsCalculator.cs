using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Sandbox.Definitions;
using Scripts.Specials.LCDScripts;
using VRage.Game.ModAPI.Ingame.Utilities;
using ServerMod;
using Scripts.Specials.Messaging;
using Digi;

namespace MyMod.Specials {
    [MyTextSurfaceScript("ComponentsCalculator", "Components Calculator")]
    internal class ComponentsCalculator : MyTSSCommon {
        // сделать комменты к кастом дате, на основном экране и на режимах, дописать handleExeption

        private readonly IMyTextSurface _surface;
        private readonly IMyCubeBlock _block;
        private readonly Vector2 _size;
        private readonly IMyTerminalBlock _terminalBlock;
        private readonly RectangleF _viewport;

        private bool enabled = true;
        private GridConnections linkType = GridConnections.Grid;
        private Mode mode = Mode.Help;
        private BlockMode blockMode = BlockMode.Inventory;
        private FilterMode filterMode = FilterMode.Default;
        private List<string> filter = new List<string>();
        private bool enableApi = false;
        private bool enableDraw = true;

        private static readonly List<string> defaultComponents = new List<string>() {
            "BulletproofGlass",
            "Computer",
            "Construction",
            "Detector",
            "Display",
            "Girder",
            "GravityGenerator",
            "InteriorPlate",
            "InteriorPlateBox",
            "LargeTube",
            "LargeTubeBox",
            "MetalGrid",
            "MetalGridBox",
            "Medical",
            "Motor",
            "MotorBox",
            "PowerCell",
            "PowerCellBox",
            "RadioCommunication",
            "Reactor",
            "RebelComponent",
            "SmallTube",
            "SmallTubeBox",
            "SolarCell",
            "SteelPlate",
            "SteelPlateBox",
            "Superconductor",
            "Thrust",
            "HeavyComp",
            "CompT02",
            "CompT03",
            "CompT04",
            "CompT05",
            "CompT06",
            "CompT07",
            "CompT08",
            "CompT09",
            "CompT10",
            "CompT11",
            "CompT12"
        };
        private static readonly Dictionary<string, string> OreToReportLink = new Dictionary<string, string>()
        {
            { "Lateryt", "Aluminum" },
            { "∑UnknownMaterial", "Cezium" },
            { "Malachit", "Copper" },
            { "⌊UnknownMaterial", "Diamond" },
            { "∏UnknownMaterial", "Kaliforn" },
            { "FrozenOil", "Oil" },
            { "∇UnknownMaterial", "Radium" },
        };
        private static readonly Dictionary<string, string> IngotToReportLink = new Dictionary<string, string>()
        {
            { "AluminumIngot", "Aluminum" },
            { "Cez", "Cezium" },
            { "CopperIngot", "Copper" },
            { "PalladiumIngot", "Palladium" },
            { "Rad", "Radium" },
         };

        // конструктор
        public ComponentsCalculator(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size) {
            try {
                _surface = surface;
                _block = block;
                _size = surface.SurfaceSize;
                _terminalBlock = block as IMyTerminalBlock;
                _viewport = new RectangleF((size - _size) / 2, _size);

                string name = _block.BlockDefinition.SubtypeName;

                if (name.Contains("Corner")) { enableDraw = false; }
            } catch (Exception exception) { HandleException(exception); }
        }

        // получение и вывод всех компонентов
        public override void Run() {
            base.Run();

            GetData ();

            if (!enabled) { return; }

            try {
                List<string> data = new List<string>();

                if (mode == Mode.Help) {
                    data.Add("Component counter");
                    data.Add("Open LCD Custom Data to edit");
                    data.Add("=============================");
                    data.Add("Счётчик компонентов");
                    data.Add("Настройки находятся в Custom Data");
                } else {
                    List<IMySlimBlock> blocks = GetBlocks(GetLinkTypeEnum(linkType));
                    //SortedDictionary<string, double> items = new SortedDictionary(GetItems(blocks));
                    Dictionary<string, double> items;

                    if (mode.HasFlag(Mode.IngotsPlus))
                    {
                        try
                        {
                            Dictionary<string, double[]> report = new Dictionary<string, double[]>();
                            Func<string, bool, string> GetDictId = (name, mode) => // false - ore, true - ingot
                            {
                                string id = ((mode ? IngotToReportLink : OreToReportLink).ContainsKey(name) ? (mode ? IngotToReportLink : OreToReportLink)[name] : name);
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
                            //foreach (KeyValuePair<string, double[]> kvp in report)
                            foreach (var kvp in report.OrderBy(key => key.Key))
                            {
                                if (kvp.Key.Equals("Ice") || kvp.Key.Equals("Oil"))
                                    data.Add($"{kvp.Key}: {kvp.Value[0].toHumanQuantity()}");
                                else
                                    data.Add($"{kvp.Key}: {kvp.Value[1].toHumanQuantity()} ({kvp.Value[0].toHumanQuantity()})");
                            }
                        }
                        catch (Exception ex)
                        {
                            data.Add("EXCEPTION");
                            //_surface.ContentType = ContentType.TEXT_AND_IMAGE;
                            _surface.WriteText(ex.ToString());
                            //_terminalBlock.CustomData = ex.ToString();
                        }
                    }
                    else
                    {
                        items = GetItems(blocks, mode);
                        foreach (var item in items.OrderBy(key => key.Key))
                            data.Add($"{item.Key}: {item.Value.toHumanQuantity()}");
                        if (enableApi) { DisplayData(items); }
                    }
                }

                if (enableDraw) { LCDHelper.Display(_surface, _size, _viewport, data, mode != Mode.Help, m_foregroundColor); }
            } catch (Exception exception) { HandleException(exception); }
        }

        public static GridLinkTypeEnum? GetLinkTypeEnum(GridConnections gc) {
            switch (gc) {
                case GridConnections.Grid: return null;
                case GridConnections.Subparts: return GridLinkTypeEnum.Mechanical;
                case GridConnections.Connectors: return GridLinkTypeEnum.Logical;
                case GridConnections.LandingGear: return GridLinkTypeEnum.NoContactDamage;
                default: return null;
            }
        }
        
        // выводит полученные данные в CustomData
        private void DisplayData(Dictionary<string, double> items) {
            MyIni ini = new MyIni();

            string data = string.Join(",", items.Select(item => $"{item.Key}:{item.Value}"));

            ini.TryParse(_terminalBlock.CustomData);
            ini.Set("api", "items", data);
            
            SetCustomDdata (ini.ToString());
        }

        // получает предметы по настройкам
        private Dictionary<string, double> GetItems(List<IMySlimBlock> blocks, Mode what_to_get) {
            Dictionary<string, double> items = new Dictionary<string, double>(StringComparer.InvariantCultureIgnoreCase);

            if (what_to_get.HasFlag(Mode.Blocks)) {
                if (blockMode.HasFlag(BlockMode.Current)) { blocks.ForEach(block => block.GetComponents(items)); }
                if (blockMode.HasFlag(BlockMode.Total)) { blocks.ForEach(block => block.GetTotalComponents(items)); }
                if (blockMode.HasFlag(BlockMode.Missing)) { blocks.ForEach(block => block.GetMissingComponents(items)); }
                if (blockMode.HasFlag(BlockMode.Inventory)) {
                    blocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items));
                    if (filterMode == FilterMode.Default) {
                        var itemsToRemove = items.Keys.Except(defaultComponents).ToList();
                        foreach (var item in itemsToRemove) items.Remove(item);
                    }
                }
            }

            if (what_to_get.HasFlag(Mode.Ores)) { blocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items, "MyObjectBuilder_Ore")); }
            if (what_to_get.HasFlag(Mode.Ingots)) { blocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items, "MyObjectBuilder_Ingot")); }
            if (filterMode == FilterMode.Blacklist) {
                foreach (var item in filter) items.Remove(item);
            } else if (filterMode == FilterMode.Whitelist) {
                var itemsToRemove = items.Keys.Except(filter, StringComparer.InvariantCultureIgnoreCase).ToList();
                foreach (var item in itemsToRemove) items.Remove(item);
            }

            return items;
        }

        // получает настройки из CustomData
        private void GetData() {
            try {
                MyIni ini = new MyIni();
                bool reset = false;
                if (!ini.TryParse(_terminalBlock.CustomData, "settings")) {
                    reset = true;
                } else
                {
                    if (!ini.Get("settings", "enabled").TryGetBoolean(out enabled))
                    {
                        enabled = true;
                        reset = true;
                    }
                    if (!Enum.TryParse(ini.Get("settings", "gridLinkType").ToString(), true, out linkType))
                    {
                        linkType = GridConnections.Grid;
                        reset = true;
                    }
                    if (!Enum.TryParse(ini.Get("settings", "mode").ToString(), true, out mode))
                    {
                        mode = Mode.Help;
                        reset = true;
                    }
                    if (!Enum.TryParse(ini.Get("settings", "blockMode").ToString(), true, out blockMode))
                    {
                        blockMode = BlockMode.Current;
                        reset = true;
                    }
                    if (!Enum.TryParse(ini.Get("settings", "filterMode").ToString(), true, out filterMode))
                    {
                        filterMode = FilterMode.Default;
                        reset = true;
                    }
                    if (!ini.Get("settings", "enableApi").TryGetBoolean(out enableApi))
                    {
                        enableApi = false;
                        reset = true;
                    }
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

                if (reset) ResetData();
            } catch (Exception exception) { HandleException(exception); }
        }

        // востанавливает CustomData до последнего состояния (или сбрасывает)
        private void ResetData() {
            MyIni ini = new MyIni();
            ini.Set("settings", "enabled", enabled);
            ini.Set("settings", "mode", mode.ToString());
            ini.Set("settings", "gridLinkType", linkType.ToString());
            ini.Set("settings", "blockMode", blockMode.ToString());
            ini.Set("settings", "filterMode", filterMode.ToString());
            ini.Set("settings", "filter", filter.Count == 0 ? "None" : string.Join(",", filter));
            ini.Set("settings", "enableApi", enableApi);

            var sb = new StringBuilder();
            sb.Append("\r\n;====================HOW TO:===================================");
            sb.Append("\r\n;enabled : true/false");
            sb.Append("\r\n;mode : ["+string.Join(",", Enum.GetNames(typeof(Mode)))+"] < change this");
            sb.Append("\r\n;gridLinkType : [" + string.Join(",", Enum.GetNames(typeof(GridConnections)))+"]");
            sb.Append("\r\n;blockMode : [" + string.Join(",", Enum.GetNames(typeof(BlockMode))) + "] (many separated with `,` )");
            sb.Append("\r\n;filterMode : [" + string.Join(",", Enum.GetNames(typeof(FilterMode)))+"]");
            sb.Append("\r\n;filter : None or text");
            sb.Append("\r\n;enableApi : true/false");

            SetCustomDdata (ini.ToString() + sb.ToString());
        }

        public void SetCustomDdata (String x)
        {
            if (_terminalBlock.CustomData != x)
            {
                _terminalBlock.CustomData = x;
            }
        }

        // получает блоки по типу подключения
        private List<IMySlimBlock> GetBlocks(GridLinkTypeEnum? linkType) {
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            IMyCubeGrid cubeGrid = _block.CubeGrid;

            if (linkType.HasValue) { MyAPIGateway.GridGroups.GetGroup(cubeGrid, linkType.Value).ForEach(grid => grid.GetBlocks(blocks)); } else { cubeGrid.GetBlocks(blocks); }

            return blocks;
        }

        

        // обрабатывает исключение
        private void HandleException(Exception exception) {
            enabled = false;
            SetCustomDdata("Exception occured!\n" + exception.Message);
            Log.ChatError("Error: CompCalc");
        }

        // тип поиска компонентов
        [Flags]
        private enum Mode {
            Help = 0,
            Blocks = 1,
            Ores = 2,
            Ingots = 4,
            IngotsPlus = 8,
        }

        // тип поиска компонентов в блоках
        [Flags]
        private enum BlockMode {
            None = 0,
            Current = 1,
            Total = 2,
            Inventory = 4,
            Missing = 8
        }

        // тип фильтрации
        private enum FilterMode {
            None,
            Default,
            Blacklist,
            Whitelist
        }
        
        public enum GridConnections {
            Grid,
            Subparts,
            Connectors,
            LandingGear
        }


        // обновляется каждые 100 тиков
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;
    }
}