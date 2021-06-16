using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Digi;
using DrawSprites;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Scripts.Base;
using Scripts.Shared.GUI;
using Scripts.Specials.SlimGarage;
using ServerMod;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using static Scripts.Specials.LCDScripts.AutoLCDBuffer;
using MyShipConnectorStatus = Sandbox.ModAPI.Ingame.MyShipConnectorStatus;

namespace Scripts.Specials.LCDScripts
{
    [MyTextSurfaceScript("AutoLCD", "Auto LCD Nebula")]
    // ReSharper disable once UnusedMember.Global
    internal class AutoLCDNebula : MyTSSCommon
    {
        private readonly IMyTextSurface _surface;
        private MySpriteDrawFrame frame;
        private readonly IMyCubeBlock _Block;
        private readonly IMyTerminalBlock _terminalBlock;
        private IMyGridTerminalSystem GTS;
        private readonly Vector2 _size;
        private readonly RectangleF _viewport;
        private readonly float _SideOffset, _PercentageOffset, _SpaceLength;
        private float lineHeight, InDivisionsWidth, TextSize = 0.8f;
        private float LargestRightText;
        private bool IsScrolling;
        private readonly Dictionary<string,Pair<float,int>> HScrollDict = new Dictionary<string, Pair<float,int>>();

        private int LinesOffScree, MaxLinesOnScreen, LinesBehind, i, LastLine;
        
        private AutoLCDBuffer Buffer = new AutoLCDBuffer();
        private readonly List<MyPhysicalItemDefinition> InventoryItems;
        private enum Type { all, component, ore, ingot, ammo, tool }

        private delegate void func(int order);
        private class LineAction
        {
            public int Order;
            public func Action;
            public void Invoke()
            {
                Action.Invoke(Order);
            }
        }
        private readonly List<LineAction> lActions = new List<LineAction>();

        private string CustomDataBuffer = "";
        private readonly StringBuilder m_sb = new StringBuilder();
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;
        public AutoLCDNebula(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size) {
            try 
            {
                _surface = surface;
                _Block = block;
                _terminalBlock = block as IMyTerminalBlock;
                _size = surface.SurfaceSize * 0.985f;
                _SideOffset = (surface.SurfaceSize * 0.015f).X;
                _viewport = new RectangleF((size - _size) / 2, _size);
                frame = _surface.DrawFrame();
                m_fontId = "Debug";

                _PercentageOffset = _surface.MeasureStringInPixels(new StringBuilder("__100.0%"), m_fontId, TextSize).X;
                _SpaceLength = _surface.MeasureStringInPixels(new StringBuilder(" "), m_fontId, TextSize).X;
                
                InventoryItems = MyDefinitionManager.Static.GetPhysicalItemDefinitions().ToList();
                InventoryItems = RemoveUnneeded(InventoryItems);
            } 
            catch (Exception exception) { HandleException(exception); }
        }

        public override void Run()
        {
            base.Run();
            try
            {
                m_sb.Clear();
                m_sb.Append("[");
                lineHeight = _surface.MeasureStringInPixels(m_sb, m_fontId, TextSize).Y * 1.2f;
                InDivisionsWidth = _surface.MeasureStringInPixels(m_sb, m_fontId, TextSize).X;

                GTS = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(_Block.CubeGrid);
                if (GTS == null) return;

                GetCustomData();
                
                LastLine = 0;
                Scroll();
                
                var OrderedActions = lActions.OrderBy(action => action.Order);
                using (frame)
                {
                    foreach (var a in OrderedActions) a.Invoke();
                }

                MaxLinesOnScreen = (int)((_surface.SurfaceSize.Y - lineHeight) / lineHeight);
                LinesOffScree = LastLine - 1 - MaxLinesOnScreen;
                IsScrolling = (LastLine - 1 + LinesBehind) * lineHeight > _surface.SurfaceSize.Y - lineHeight;
            }
            catch (Exception exception)
            {
                HandleException(exception, "Run");
            }
        }
        private DateTime ScrollPause = DateTime.Now;

        private void Scroll()
        {
            var MouseScrollWheelInput = MyAPIGateway.Input.DeltaMouseScrollWheelValue();
            
            if (IsScrolling)
            {
                Vector3D Origin;
                Vector3D Direction;
                if (LCDGuiHelper.ExtractCameraOriginAndDirection(out Origin, out Direction))
                {
                    var ProjectedPoint = LCDGuiHelper.ProjectRay(Block.WorldMatrix, _Block.LocalAABB, Direction, Origin, m_size, Block.CubeGrid.GridSize);
                    
                    if (ProjectedPoint.isRayOnScreen(m_size) && MouseScrollWheelInput != 0)
                    {
                        var ToScroll = MouseScrollWheelInput / 30;
                        if (LinesOffScree + ToScroll < 0) LinesBehind -= LinesOffScree;
                        else LinesBehind -= ToScroll;
                        ScrollPause = DateTime.Now + TimeSpan.FromSeconds(10);
                    }
                }

                if (ScrollPause <= DateTime.Now)
                {
                    if (i >= 3)
                    {
                        if (LinesOffScree <= 0) LinesBehind = -1;
                        LinesBehind++;
                        i = -1;
                    }
                    i++;
                }
                
                LinesBehind = Math.Max(LinesBehind, 0) + Math.Min(LinesOffScree, 0);
                LastLine -= LinesBehind;
            }
        }
        private void UpdateInvSprites(int InOrder)
        {
            var set = Buffer.InventorySort[InOrder];
            LastLine = frame.DrawItemAmountTextAUTOLCD(UpdateInv(set), LastLine, set.Name, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, LargestRightText, InDivisionsWidth, set.isProgBarCubeVisible, set.SType); 
        }
        private void UpdateMisSprites(int InOrder)
        {
            var set = Buffer.MissingSort[InOrder];
            LastLine = frame.DrawItemAmountTextAUTOLCD(UpdateInv(set,true), LastLine, set.Name, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, LargestRightText, InDivisionsWidth, set.isProgBarCubeVisible, set.SType); 
        }
        private void UpdateCargoSprites(int InOrder)
        {
            var set = Buffer.CargoSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, true, b => b.FatBlock is IMyCargoContainer);
            var CargoFill = UpdateCargo(blocks, set.IsSeparate, set.Name);
            LastLine = frame.DrawDefaultAUTOLCD(CargoFill,LastLine,new Vector2(_size.X, LastLine * lineHeight + _viewport.Y),m_foregroundColor,TextSize,lineHeight,_SideOffset,InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType);
        }
        private void UpdateCargoALLSprites(int InOrder)
        {
            var set = Buffer.CargoAllSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid,true, b => b.FatBlock is IMyCargoContainer || b.FatBlock is IMyShipConnector ||  b.FatBlock is IMyAssembler ||  b.FatBlock is IMyRefinery || b.FatBlock is IMyReactor);
            var CargoFill = UpdateCargo(blocks, set.IsSeparate, set.Name);
            LastLine = frame.DrawDefaultAUTOLCD(CargoFill,LastLine,new Vector2(_size.X, LastLine * lineHeight + _viewport.Y),m_foregroundColor,TextSize,lineHeight,_SideOffset,InDivisionsWidth,set.isProgBarCubeVisible, _PercentageOffset, set.SType);
        }
        private void UpdatePowerSprites(int InOrder)
        {
            var set = Buffer.PowerSort[InOrder];
            var blocks = GetBlocks(set.Where,set.isGroup,set.IsOnSameGrid,true,b=> b.FatBlock is IMyPowerProducer);
            var totalOutput = 0d;
            var MaxOutput = 0d;
            
            var reactorTotalOutput = 0d;
            var MaxReactorTotalOutput = 0d;
            var RAmount = 0;
            var generatorTotalOutput = 0d;
            var MaxGeneratorTotalOutput = 0d;
            var EAmount = 0;
            var solarTotalOutput = 0d;
            var MaxSolarTotalOutput = 0d;
            var SAmount = 0;
            var turbineTotalOutput = 0d;
            var MaxTurbineTotalOutput = 0d;
            var TAmount = 0;
            var batteryTotalOutput = 0d;
            var MaxBatteryTotalOutput = 0d;
            var batteryTotalInput = 0d;
            var MaxBatteryTotalInput = 0d;
            var batteryTotalStorage = 0d;
            var MaxBatteryTotalStorage = 0d;
            var BAmount = 0;
            
            foreach (var b in blocks)
            {
                var block = b.FatBlock as IMyPowerProducer;
                if (block == null) return;
                if (block is IMyReactor)
                {
                    reactorTotalOutput += block.CurrentOutput;
                    MaxReactorTotalOutput += block.MaxOutput;
                    RAmount++;
                }
                else if (block.SubtypeName().Contains("Engine"))
                {
                    generatorTotalOutput += block.CurrentOutput;
                    MaxGeneratorTotalOutput += block.MaxOutput;
                    EAmount++;
                }
                else if (block is IMySolarPanel)
                {
                    solarTotalOutput += block.CurrentOutput;
                    MaxSolarTotalOutput += block.MaxOutput;
                    SAmount++;
                }
                else if (block.SubtypeName().Contains("WindTurbine"))
                {
                    turbineTotalOutput += block.CurrentOutput;
                    MaxTurbineTotalOutput += block.MaxOutput;
                    TAmount++;
                }
                else if (block is IMyBatteryBlock)
                {
                    var battery = block as IMyBatteryBlock;
                    batteryTotalOutput += battery.CurrentOutput;
                    MaxBatteryTotalOutput += battery.MaxOutput;
                    batteryTotalInput += battery.CurrentInput;
                    MaxBatteryTotalInput += battery.MaxInput;
                    batteryTotalStorage += battery.CurrentStoredPower;
                    MaxBatteryTotalStorage += battery.MaxStoredPower;
                    BAmount++;
                }
            }

            if (RAmount != 0) LastLine = frame.DrawDefaultAUTOLCD(new List<Pair<string, double[]>> {new Pair<string, double[]>("Reactor", new[] {reactorTotalOutput, MaxReactorTotalOutput})}, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType,  AutoLCDInfoType.PowerUsing);
            if (EAmount != 0) LastLine = frame.DrawDefaultAUTOLCD(new List<Pair<string, double[]>> {new Pair<string, double[]>("Engines", new[] {generatorTotalOutput, MaxGeneratorTotalOutput})}, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType, AutoLCDInfoType.PowerUsing);
            if (SAmount != 0) LastLine = frame.DrawDefaultAUTOLCD(new List<Pair<string, double[]>> {new Pair<string, double[]>("Solars", new[] {solarTotalOutput, MaxSolarTotalOutput})}, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType, AutoLCDInfoType.PowerUsing);
            if (TAmount != 0) LastLine = frame.DrawDefaultAUTOLCD(new List<Pair<string, double[]>> {new Pair<string, double[]>("Turbines", new[] {turbineTotalOutput, MaxTurbineTotalOutput})}, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType, AutoLCDInfoType.PowerUsing);
            if (BAmount != 0)
            {
                frame.DrawText("Batteries:", _SideOffset * 2, LastLine * lineHeight + _viewport.Y, m_foregroundColor, TextSize * SpriteGI.TextHeight, InFontId: "Debug");
                frame.DrawText("(IN " + batteryTotalInput.toHumanQuantityEnergy() + " / OUT " + batteryTotalOutput.toHumanQuantityEnergy() + ")", _size.X - _SideOffset, LastLine * lineHeight + _viewport.Y, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT,"Debug");
                LastLine++;
                LastLine = frame.DrawDefaultAUTOLCD(new List<Pair<string, double[]>> {new Pair<string, double[]>("Stored", new[] {batteryTotalStorage, MaxBatteryTotalStorage})}, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset*2, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType, AutoLCDInfoType.PowerStored);
                LastLine = frame.DrawDefaultAUTOLCD(new List<Pair<string, double[]>> {new Pair<string, double[]>("Output", new[] {batteryTotalOutput, MaxBatteryTotalOutput})}, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset*2, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType, AutoLCDInfoType.PowerUsing);
                LastLine = frame.DrawDefaultAUTOLCD(new List<Pair<string, double[]>> {new Pair<string, double[]>("Input", new[] {batteryTotalInput, MaxBatteryTotalInput})}, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset*2, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType, AutoLCDInfoType.PowerUsing);
            }
            
            totalOutput += solarTotalOutput + turbineTotalOutput + batteryTotalOutput + generatorTotalOutput + reactorTotalOutput;
            MaxOutput += MaxSolarTotalOutput + MaxTurbineTotalOutput + MaxBatteryTotalOutput + MaxGeneratorTotalOutput + MaxReactorTotalOutput;
            
            if(MaxOutput <= 0) frame.DrawText("No power source found!", _SideOffset * 2, LastLine * lineHeight + _viewport.Y, m_foregroundColor, TextSize * SpriteGI.TextHeight, InFontId: "Debug");
            if(set.Where == "") LastLine = frame.DrawDefaultAUTOLCD(new List<Pair<string, double[]>> {new Pair<string, double[]>("Total Output", new[] {totalOutput, MaxOutput})}, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType, AutoLCDInfoType.PowerUsing);
        }
        private void UpdatePowerStoredSprites(int InOrder)
        {
            var set = Buffer.PowerStoredSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid,true, b => b.FatBlock is IMyBatteryBlock);
            var list = new List<Pair<string, double[]>>();
            
            var batteryTotalStorage = 0d;
            var MaxBatteryTotalStorage = 0d;
            var BAmount = 0;
            foreach (var block in blocks)
            {
                var battery = block.FatBlock as IMyBatteryBlock;
                if(battery == null) return;
                if(set.IsSeparate)list.Add(new Pair<string, double[]>(battery.DisplayNameText, new double[] {battery.CurrentStoredPower, battery.MaxStoredPower}));
                else
                {
                    batteryTotalStorage += battery.CurrentStoredPower;
                    MaxBatteryTotalStorage += battery.MaxStoredPower;
                    BAmount++;
                }
            }
            if (!set.IsSeparate && BAmount != 0) LastLine = frame.DrawDefaultAUTOLCD(new List<Pair<string, double[]>> {new Pair<string, double[]>(set.Name == "" ? "Power Stored" : set.Name, new[] {batteryTotalStorage, MaxBatteryTotalStorage})}, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType, AutoLCDInfoType.PowerStored);
            else LastLine = frame.DrawDefaultAUTOLCD(list, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType, AutoLCDInfoType.PowerStored);
            
        }
        private void UpdatePowerUsedSprites(int InOrder)
        {
            var set = Buffer.PowerUsedSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid);
            var list = new List<Pair<string, double[]>>();
            var CurrentPowerUse = 0d;
            var MaxRequiredPowerUse = 0d;
            
            foreach (var block in blocks.Select(b => b.FatBlock))
            {
                if(block.MaxRequiredPowerInput() <= 0) continue;
                if(set.IsSeparate)list.Add(new Pair<string, double[]>(block.DisplayNameText, new double[] {block.CurrentPowerInput(), block.MaxRequiredPowerInput()}));
                else
                {
                    CurrentPowerUse += block.CurrentPowerInput();
                    MaxRequiredPowerUse += block.MaxRequiredPowerInput();
                }
            }
            if(!set.IsSeparate)list.Add(new Pair<string, double[]>(set.Name == "" ? "Power Used" : set.Name, new [] {CurrentPowerUse, MaxRequiredPowerUse}));
            var orderedList = list.OrderByDescending(l => l.v[0]);
            if (set.IsSeparate)
            {
                var Take = (int) set.items.First().Value;
                var TopList = Take == 0 ? orderedList : orderedList.Take((int) set.items.First().Value);
                LastLine = frame.DrawDefaultAUTOLCD(TopList, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, ShowType.Default, AutoLCDInfoType.PowerUsing);
            }
            else LastLine = frame.DrawDefaultAUTOLCD(orderedList, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, ShowType.Default, AutoLCDInfoType.PowerUsing);
        }
        private void UpdatePowerTimeSprites(int InOrder) //todo complete this function
        {
            var set = Buffer.PowerTimeSort[InOrder];
            
            var PowerBlocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, true, b => b.FatBlock is IMyPowerProducer).Select(b => b.FatBlock as IMyPowerProducer).ToArray();

            //float MaxReactorTime = 0;
            foreach (var reactor in PowerBlocks.Where(b => b is IMyReactor))
            {
                
            }
            //float MaxBatteryTime = 0;
            foreach (var battery in PowerBlocks.Where(b => b is IMyBatteryBlock))
            {
                
            }
            //float MaxEngineTime = 0;
            foreach (var Engine in PowerBlocks.Where(b=> b.SubtypeName().Contains("Engine")))
            {
                
            }
            
            frame.DrawText("Work in progress",_size.X/2,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
            LastLine++;
        }
        private void UpdateChargeSprites(int InOrder)
        {
            var set = Buffer.ChargeSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, true, b => b.FatBlock is IMyJumpDrive);
            var list = new List<Pair<string, double[]>>();
            var CurrentCharge = 0d;
            var MaxCharge = 0d;
            float MaxTimeToCharge = 0;

            foreach (var block in blocks)
            {
                var jumpDrive = block.FatBlock as IMyJumpDrive;
                if (jumpDrive == null) return;
                if (set.IsSeparate)
                {
                    if (!set.isZeroHidden) list.Add(new Pair<string, double[]>(jumpDrive.DisplayNameText, new double[] {jumpDrive.CurrentStoredPower, jumpDrive.MaxStoredPower}));
                    else
                    {
                        var input = jumpDrive.SlimBlock.FatBlock.CurrentPowerInput() / 3600f * 0.8f;
                        var TimeToCharge = (jumpDrive.MaxStoredPower - jumpDrive.CurrentStoredPower) / input;
                        list.Add(new Pair<string, double[]>(jumpDrive.DisplayNameText, new double[] {jumpDrive.CurrentStoredPower, jumpDrive.MaxStoredPower, TimeToCharge}));
                        
                    }
                }
                else
                {
                    CurrentCharge += jumpDrive.CurrentStoredPower;
                    MaxCharge += jumpDrive.MaxStoredPower;
                    var input = jumpDrive.SlimBlock.FatBlock.CurrentPowerInput() / 3600f * 0.8f;
                    var TimeToCharge = (jumpDrive.MaxStoredPower - jumpDrive.CurrentStoredPower) / input;
                    if (TimeToCharge > MaxTimeToCharge) MaxTimeToCharge = TimeToCharge;
                }
            }

            if (!set.IsSeparate) list.Add(!set.isZeroHidden ? new Pair<string, double[]>(set.Name == "" ? "Jump Charge" : set.Name, new[] {CurrentCharge, MaxCharge}) : new Pair<string, double[]>(set.Name == "" ? "Jump Charge Time" : set.Name, new[] {CurrentCharge, MaxCharge, MaxTimeToCharge}));
            
            LastLine = frame.DrawDefaultAUTOLCD(list, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType, !set.isZeroHidden ? AutoLCDInfoType.PowerStored : AutoLCDInfoType.Time);
        }
        private void UpdateDamageSprites(int InOrder)
        {
            var set = Buffer.DamageSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, false,b => !b.IsFullIntegrity);
            var list = new List<Pair<string, double[]>>();
            foreach (var block in blocks)
            {
                list.Add(new Pair<string, double[]>(block.FatBlock.DisplayNameText, new double[] {block.Integrity, block.MaxIntegrity}));
            }
            if(blocks.Count != 0) LastLine = frame.DrawDefaultAUTOLCD(list, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset, InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType);
            else
            {
                frame.DrawText("No damaged blocks found.",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                LastLine++;
            }
        }
        private void UpdateDockedSprites(int InOrder)
        {
            var set = Buffer.DockedSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, true, true, b=> b.FatBlock is IMyShipConnector);
            var list = new List<Pair<string, string>>();
            foreach (var block in blocks)
            {
                var Connector = block.FatBlock as IMyShipConnector;
                if(Connector == null) continue;
                var txt = "";
                if(Connector.Enabled)
                    switch (Connector.Status)
                    {
                        case MyShipConnectorStatus.Unconnected:
                            txt = "Unconnected";
                            break;
                        case MyShipConnectorStatus.Connectable:
                            txt = "Near: " + Connector.OtherConnector.CubeGrid.DisplayName;
                            break;
                        case MyShipConnectorStatus.Connected:
                            txt = Connector.OtherConnector.CubeGrid.DisplayName;
                            break;
                }
                else txt = "Off";
                list.Add(new Pair<string, string>(Connector.DisplayNameText, txt));
            }
            LastLine = frame.DrawSimpleAUTOLCD(list, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset);
        }
        private void UpdateBlockCountSprites(int InOrder)
        {
            var set = Buffer.BlockCountSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, false);
            var DicList = new Dictionary<string,int[]>();
            foreach (var b in blocks)
            {
                var block = b.FatBlock;
                var blockSubtypeName = block.DefinitionDisplayNameText;
                if (!DicList.ContainsKey(blockSubtypeName)) DicList.Add(blockSubtypeName,new []{1});
                else DicList[blockSubtypeName][0]++;
            }
            LastLine = frame.DrawSimpleAUTOLCD(DicList, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset);
        }
        private void UpdateProdCountSprites(int InOrder)
        {
            var set = Buffer.ProdCountSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, true, b => b.FatBlock is IMyProductionBlock);
            var DicList = new Dictionary<string,int[]>();
            foreach (var b in blocks)
            {
                var block = b.FatBlock as IMyProductionBlock;
                if (block == null) return;
                var blockSubtypeName = block.DefinitionDisplayNameText;
                if (!DicList.ContainsKey(blockSubtypeName))
                {
                    DicList.Add(blockSubtypeName,new []{1, block.IsProducing && block.Enabled ? 1 : 0});
                }
                else
                {
                    DicList[blockSubtypeName][0]++;
                    if(block.IsProducing && block.Enabled) DicList[blockSubtypeName][1]++;
                }
            }
            LastLine = frame.DrawSimpleAUTOLCD(DicList, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset);
        }
        private void UpdateEnableCountSprites(int InOrder)
        {
            var set = Buffer.EnabledCountSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, false);
            var DicList = new Dictionary<string,int[]>();
            foreach (var b in blocks)
            {
                var block = b.FatBlock;
                var blockSubtypeName = block.DefinitionDisplayNameText;
                if (!DicList.ContainsKey(blockSubtypeName))
                {

                    DicList.Add(blockSubtypeName,new []{1, block.IsWorking ? 1 : 0});
                }
                else
                {
                    DicList[blockSubtypeName][0]++;
                    if(block.IsWorking) DicList[blockSubtypeName][1]++;
                }
            }
            LastLine = frame.DrawSimpleAUTOLCD(DicList, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset);
        }
        private void UpdateWorkingSprites(int InOrder)
        {
            var set = Buffer.WorkingSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, false);
            var list = new List<Pair<string, string>>();
            foreach (var block in blocks)
            {
                var FatBlock = block.FatBlock;
                var Prod = FatBlock as IMyProductionBlock;
                if (Prod != null)
                {
                    list.Add(new Pair<string, string>(Prod.DisplayNameText, Prod.Enabled ? Prod.IsProducing ? "Working" : "Idle" : "Off"));
                    continue;
                }
                var GasGen = FatBlock as IMyGasGenerator;
                if (GasGen != null)
                {
                    list.Add(new Pair<string, string>(GasGen.DisplayNameText, GasGen.Enabled  ? "On" : "Off"));
                    continue;
                }
                var door = FatBlock as IMyDoor;
                if (door != null)
                {
                    list.Add(new Pair<string, string>(door.DisplayNameText, door.Enabled ? door.Status.ToString() : "Off"));
                    continue;
                }
                var Battery = FatBlock as IMyBatteryBlock;
                if (Battery != null)
                {
                    list.Add(new Pair<string, string>(Battery.DisplayNameText, Battery.Enabled ? Battery.ChargeMode.ToString() : "Off"));
                    continue;
                }
                var Power = FatBlock as IMyPowerProducer;
                if (Power != null)
                {
                    list.Add(new Pair<string, string>(Power.DisplayNameText, Power.Enabled ?  ((double)Power.CurrentOutput).toHumanQuantityEnergy() : "Off"));
                    continue;
                }
                var AirVent = FatBlock as IMyAirVent;
                if (AirVent != null)
                {
                    list.Add(new Pair<string, string>(AirVent.DisplayNameText, AirVent.Enabled ? AirVent.CanPressurize ? AirVent.Depressurize ? "Depressurize On" : "Depressurize Off" : "Leaking" : "Off"));
                    continue;
                }
                var GasTank = FatBlock as IMyGasTank;
                if (GasTank != null)
                {
                    list.Add(new Pair<string, string>(GasTank.DisplayNameText, GasTank.Enabled ? (GasTank.Stockpile ? "Stockpile On " : "Stockpile Off ") + GasTank.FilledRatio.ToString("0%") : "Off"));
                    continue;
                }
                var Projector = FatBlock as IMyProjector;
                if (Projector != null)
                {
                    list.Add(new Pair<string, string>(Projector.DisplayNameText, Projector.Enabled ? Projector.IsProjecting ? "Projecting" : "Idle" : "Off"));
                    continue;
                }
                var Connector = FatBlock as IMyShipConnector;
                if (Connector != null)
                {
                    list.Add(new Pair<string, string>(Connector.DisplayNameText, Connector.Enabled ? Connector.Status.ToString() : "Off"));
                }
            }
            LastLine = frame.DrawSimpleAUTOLCD(list, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset);
        }
        private void UpdatePropBoolSprites(int InOrder)
        {
            var set = Buffer.PropBoolSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, false);
            foreach (var block in blocks)
            {
                var terminalBlock = block.FatBlock as IMyTerminalBlock;
                if (terminalBlock == null) return;
                if (set.Name == "") LastLine = frame.DrawSimpleAUTOLCD(new List<Pair<string, string>> {new Pair<string, string>(terminalBlock.DisplayNameText,"")}, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset);
                
                var list = new List<Pair<string, string>>();
                var Properties = new List<ITerminalProperty>();
                terminalBlock.GetProperties(Properties, p => p.Is<bool>() && p.Id.Contains(set.Name));
                foreach (var pr in Properties)
                {
                    var txt = terminalBlock.GetValueBool(pr.Id).ToString();
                    if (set.exceptions.Count >= 3)
                    {
                        txt = terminalBlock.GetValueBool(pr.Id) ? set.exceptions[1] : set.exceptions[2];
                        list.Add(new Pair<string, string>(set.Name == "" ? pr.Id : set.exceptions[0],txt));
                    }
                    else list.Add(new Pair<string, string>(set.Name == "" ? pr.Id : set.Name,txt));
                }
                LastLine = frame.DrawSimpleAUTOLCD(list, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset);
            }
        }
        private void UpdateDetailsSprites(int InOrder)
        {
            var set = Buffer.DetailsSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, false);
            var Regex = new Regex(set.Name + "(?s).*");
            foreach (var block in blocks)
            {
                var term = block.FatBlock as IMyTerminalBlock;
                if(term == null) return;
                var Details = Buffer.RegexParse(Regex, term.DetailedInfo, false);
                if (!set.isZeroHidden)
                {
                    frame.DrawText(term.DisplayNameText,_SideOffset*2,LastLine * lineHeight, m_foregroundColor,TextSize * SpriteGI.TextHeight, InFontId: m_fontId);
                    LastLine++;
                }
                frame.DrawText(Details,_SideOffset*5,LastLine * lineHeight, m_foregroundColor,TextSize * SpriteGI.TextHeight, InFontId: m_fontId);
                LastLine += Details.Count(x => x == '\n');
            }
        }
        private void UpdateAmountSprites(int InOrder) //todo complete this function
        {
            frame.DrawText("Work in progress",_size.X/2,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
            LastLine++;
        }
        private void UpdateOxygenSprites(int InOrder)
        {
            var set = Buffer.OxygenSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, true, b => b.FatBlock is IMyAirVent || b.FatBlock is IMyOxygenFarm || b.FatBlock is IMyGasTank);
            var oxygenCount = 0;
            var Oxy = 0d;
            var FarmCount = 0;
            var far = 0d;
            foreach (var b in blocks)
            {
                var Vent = b.FatBlock as IMyAirVent;
                if (Vent != null)
                {
                    frame.DrawText(Vent.DisplayNameText,_SideOffset*2,LastLine * lineHeight, m_foregroundColor,TextSize * SpriteGI.TextHeight, InFontId: m_fontId);
                    frame.DrawText(Vent.Enabled ? Vent.CanPressurize ? $" {Vent.GetOxygenLevel() * 100 :F1}%" : "Leaking" : "Off",_size.X - _SideOffset*2,LastLine * lineHeight, m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                    LastLine++;
                    frame.DrawProgressBarAUTOLCD(new double[]{Vent.GetOxygenLevel() * 10,  10}, new Vector2(_SideOffset*2, LastLine * lineHeight), m_foregroundColor, lineHeight, _size.X - _SideOffset*3, InDivisionsWidth, set.isProgBarCubeVisible);
                    LastLine++;
                }
                var Farm = b.BlockDefinition as MyOxygenFarmDefinition;
                var FarmB = b.FatBlock as IMyOxygenFarm;
                if (Farm != null && FarmB != null)
                {
                    far += FarmB.GetOutput();
                    FarmCount++;
                }
                var GasTank = b.FatBlock as IMyGasTank;
                if (GasTank != null)
                {
                    Oxy += GasTank.FilledRatio;
                    oxygenCount++;
                }
            }

            if (FarmCount != 0)
            {
                var prc = far / FarmCount;
                frame.DrawText("Oxygen Farms", _SideOffset * 2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, InFontId: m_fontId);
                frame.DrawText($"{prc * 100:F1}%", _size.X - _SideOffset * 2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                LastLine++;
                frame.DrawProgressBarAUTOLCD(new []{prc * 10, 10}, new Vector2(_SideOffset*2, LastLine * lineHeight), m_foregroundColor, lineHeight, _size.X - _SideOffset*3, InDivisionsWidth, set.isProgBarCubeVisible);
                LastLine++;
            }

            if (oxygenCount != 0)
            {
                var prc = Oxy / oxygenCount;
                frame.DrawText("Oxygen Tanks", _SideOffset * 2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, InFontId: m_fontId);
                frame.DrawText($"{ prc * 100 :F1}%", _size.X - _SideOffset * 2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                LastLine++;
                frame.DrawProgressBarAUTOLCD(new []{prc * 10, 10}, new Vector2(_SideOffset*2, LastLine * lineHeight), m_foregroundColor, lineHeight, _size.X - _SideOffset*3, InDivisionsWidth, set.isProgBarCubeVisible);
                LastLine++;
            }
        }
        private void UpdateTanksSprites(int InOrder)
        {
            var set = Buffer.TanksSort[InOrder];
            var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid, true, b => b.FatBlock is IMyGasTank);
            var List = new Dictionary<string,double[]>();
            var NList = new List<Pair<string, double[]>>();
            foreach (var b in blocks)
            {
                var GasTank = b.FatBlock as IMyGasTank;
                var GasTankDef = b.BlockDefinition as MyGasTankDefinition;
                if (GasTank != null && GasTankDef != null)
                {
                    if (!set.IsSeparate)
                    {
                        var Subtype = GasTankDef.StoredGasId.SubtypeId.String;
                        if (List.ContainsKey(Subtype))
                        {
                            List[Subtype][0] += GasTank.Capacity * GasTank.FilledRatio;
                            List[Subtype][1] += GasTank.Capacity;
                        }
                        else
                        {
                            List.Add(Subtype, new[] {GasTank.Capacity * GasTank.FilledRatio, GasTank.Capacity});
                        }
                    }
                    else
                    {
                        NList.Add(new Pair<string, double[]>(GasTank.DisplayNameText,new[] {GasTank.Capacity * GasTank.FilledRatio, GasTank.Capacity}));
                    }
                }
            }

            if (!set.IsSeparate)
            {
                foreach (var l in List)
                {
                    NList.Add(new Pair<string, double[]>(l.Key, l.Value));
                }
            }
            LastLine = frame.DrawDefaultAUTOLCD(NList,LastLine,new Vector2(_size.X, LastLine * lineHeight + _viewport.Y),m_foregroundColor,TextSize,lineHeight,_SideOffset,InDivisionsWidth, set.isProgBarCubeVisible, _PercentageOffset, set.SType);
        }
        private void UpdateEchoSprites(int InOrder)
        {
            var set = Buffer.EchoSort[InOrder];
            frame.DrawText(set,_SideOffset,LastLine * lineHeight, m_foregroundColor,TextSize * SpriteGI.TextHeight, InFontId: m_fontId);
            LastLine++;
        }
        private void UpdateCenterSprites(int InOrder)
        {
            var set = Buffer.CenterSort[InOrder];
            frame.DrawText(set,_size.X/2,LastLine * lineHeight, m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
            LastLine++;
        }
        private void UpdateRightSprites(int InOrder)
        {
            var set = Buffer.RightSort[InOrder];
            frame.DrawText(set,_size.X,LastLine * lineHeight, m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
            LastLine++;
        }
        private void UpdateHScrollSprites(int InOrder)
        {
            var set = Buffer.HScrollSort[InOrder];
            
            if (!HScrollDict.ContainsKey(set.k))
            {
                var StringLength = _surface.MeasureStringInPixels(new StringBuilder(set.k), m_fontId, TextSize).X;
                HScrollDict.Add(set.k, new Pair<float, int>(StringLength,0));
            }
            else
            {
                if (HScrollDict[set.k].v >= HScrollDict[set.k].k + _SpaceLength*2) HScrollDict[set.k].v = 0;
                HScrollDict[set.k].v += 2;
            }

            var text = "";
            var count = (int)(_size.X / (HScrollDict[set.k].k + _SpaceLength*2));
            for (int j = 0; j < count + 2; j++)
            {
                text += set.k + "  ";
            }
            if(!set.v) frame.DrawText(text,-HScrollDict[set.k].k + HScrollDict[set.k].v,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
            else frame.DrawText(text,_size.X + HScrollDict[set.k].k - HScrollDict[set.k].v,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
            LastLine++;

        }
        private void UpdateCustomDataSprites(int InOrder)
        {
            var set = Buffer.CustomDataSort[InOrder];
            var block = GTS.GetBlockWithName(set);
            if (block != null)
            {
                frame.DrawText(block.CustomData,_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                LastLine++;
            }
            else
            {
                frame.DrawText("Missing/Wrong Name!",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                LastLine++;
            }
        }
        private void UpdateTextLCDSprites(int InOrder)
        {
            var set = Buffer.TextLCDSort[InOrder];
            var block = GTS.GetBlockWithName(set) as IMyTextPanel;
            if (block != null)
            {
                frame.DrawText(block.GetText(),_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                LastLine++;
            }
            else
            {
                frame.DrawText("Missing/Wrong Name!",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                LastLine++;
            }
        }
        private void UpdateTimeSprites(int InOrder)
        {
            var set = Buffer.TimeSort[InOrder];
            var totalSeconds = (int)(DateTime.Now.TimeOfDay + TimeSpan.FromHours(set.k)).TotalSeconds;
            if (totalSeconds >= 86400) totalSeconds -= 86400;
            frame.DrawText($"{totalSeconds / 3600}:{totalSeconds / 60 % 60}:{totalSeconds % 60}",set.v ? _size.X/2 : _SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, set.v ? TextAlignment.CENTER : TextAlignment.LEFT, m_fontId);
            LastLine++;
        }
        private void UpdateDateSprites(int InOrder)
        {
            var set = Buffer.DateSort[InOrder];
            var txt = DateTime.Now + TimeSpan.FromHours(set.k);
            frame.DrawText(txt.ToString("MM/dd/yyyy"),set.v ? _size.X/2 : _SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, set.v ? TextAlignment.CENTER : TextAlignment.LEFT, m_fontId);
            LastLine++;
        }
        private void UpdateDateTimeSprites(int InOrder)
        {
            var set = Buffer.DateTimeSort[InOrder];
            var txt = DateTime.Now + TimeSpan.FromHours(set.k.k);
            frame.DrawText(txt.ToString(set.v),set.k.v ? _size.X/2 : _SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, set.k.v ? TextAlignment.CENTER : TextAlignment.LEFT, m_fontId);
            LastLine++;
        }
        private void UpdateCountDownSprites(int InOrder)
        {
            var set = Buffer.CountDownSort[InOrder];
            var Alignment = TextAlignment.LEFT;
            var Pos = _SideOffset;
            switch (set.v)
            {
                case 1:
                {
                    Alignment = TextAlignment.CENTER;
                    Pos = _size.X / 2;
                    break;
                }
                case 2:
                {
                    Alignment = TextAlignment.RIGHT;
                    Pos = _size.X;
                    break;
                }
            }
            
            if(DateTime.Now > set.k) frame.DrawText("EXPIRED",Pos,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, Alignment, m_fontId);
            else
            {
                var txt = -(int)(DateTime.Now - set.k).TotalSeconds;
                frame.DrawText(txt.toHumanTime2(true),Pos,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, Alignment, m_fontId);
            }
            LastLine++;
            
        }
        private void UpdatePosSprites(int InOrder)
        {
            var set = Buffer.PosSort[InOrder];
            var block = GTS.GetBlockWithName(set.v);
            var Pos = block?.GetPosition() ?? _Block.GetPosition();
            switch (set.k)
            {
                case 0:
                {
                    frame.DrawText("Location:",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                    frame.DrawText( "{"+ $"X:{Pos.X:F0} Y:{Pos.Y:F0} Z:{Pos.Z:F0}" + "}",_size.X - _SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                    LastLine++;
                    break;
                }
                case 1:
                {
                    frame.DrawText("Location:",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                    LastLine++;
                    var List = new List<Pair<string, string>>
                    {
                        new Pair<string, string>("   X", $"{Pos.X:F0}"),
                        new Pair<string, string>("   Y", $"{Pos.Y:F0}"),
                        new Pair<string, string>("   Z", $"{Pos.Z:F0}")
                    };
                    LastLine = frame.DrawSimpleAUTOLCD(List,LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y),m_foregroundColor,TextSize,lineHeight,_SideOffset);
                    break;
                }
                case 2:
                {
                    var txt = $"GPS:Location:{Pos.X:F2}:{Pos.Y:F2}:{Pos.Z:F2}:";
                    var TextPanel = _terminalBlock as IMyTextPanel;
                    TextPanel?.WriteText(txt);
                    frame.DrawText(txt,_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                    LastLine++;
                    break;
                }
            }
        }
        private void UpdateAltitudeSprites(int InOrder)
        {
            var set = Buffer.AltitudeSort[InOrder];
            var pos = _Block.CubeGrid.Physics.CenterOfMassWorld;
            if (GameBase.IsInNaturalGravity(pos))
            {
                var Planet = GameBase.GetClosestPlanet(pos);
                double Altitude;
                if (set)
                {
                    Altitude = (pos - Planet.PositionComp.GetPosition()).Length() - Planet.AverageRadius;
                }
                else
                {
                    Altitude = Planet.GetClosestSurfacePointGlobal(ref pos).Length();
                }
                frame.DrawText(set ? "Altitude:" : "Ground:",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                frame.DrawText($"{Altitude :N1} m",_size.X,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                LastLine++;
            }
            else
            {
                frame.DrawText("Space",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                LastLine++;
            }
        }
        private void UpdateSpeedSprites(int InOrder)
        {
            var set = Buffer.SpeedSort[InOrder];
            var Speed = (double)_Block.CubeGrid.Physics.Speed;
            var _speed = Speed;
            var txt = $"{Speed :N1} m/s";
            switch (set.k)
            {
                case 1: _speed = Speed * 18 / 5; txt = $"{ _speed:N1} km/h"; break;
                case 2: _speed = Speed * 2.23694; txt = $"{_speed:N1} mph"; break;
            }

            frame.DrawText("Speed:",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
            frame.DrawText(txt,_size.X - _SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
            LastLine++;
            if (set.v <= 0) return;
            frame.DrawProgressBarAUTOLCD(new []{_speed,set.v},new Vector2(_SideOffset,LastLine * lineHeight),m_foregroundColor,lineHeight,_size.X - _SideOffset *2,InDivisionsWidth,true);
            LastLine++;
        }
        private void UpdateAccelSprites(int InOrder)
        {
            var set = Buffer.AccelSort[InOrder];
            var Accel = (double)_Block.CubeGrid.Physics.LinearAcceleration.Length();
                
            frame.DrawText("Acceleration:",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
            frame.DrawText($"{Accel :N1} m/s\xB2",_size.X,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
            LastLine++;
            if (set == 0) return;
            frame.DrawProgressBarAUTOLCD(new []{Accel,set},new Vector2(_SideOffset,LastLine * lineHeight),m_foregroundColor,lineHeight,_size.X - _SideOffset *2,InDivisionsWidth,true);
            LastLine++;
        }
        private void UpdateGravitySprites(int InOrder) //todo if inside any ShipController
        {
            var set = Buffer.GravitySort[InOrder];
            
            var NaturalGravity = _Block.CubeGrid.Physics.Gravity.Length();
            var TotalGravity = MyAPIGateway.Session.Player.Character.Physics.Gravity.Length() / 2;
            //if() 
            frame.DrawText("Work in progress",_size.X/2,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
            LastLine++;
            switch (set)
            {
                case 0:
                {
                    frame.DrawText("Natural",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                    frame.DrawText("Artificial",_size.X/2,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
                    frame.DrawText("Total",_size.X - _SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                    LastLine++;
                    frame.DrawText($"{NaturalGravity :N1} m/s\xB2",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                    frame.DrawText($"{TotalGravity - NaturalGravity :N1} m/s\xB2",_size.X/2,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
                    frame.DrawText($"{TotalGravity :N1} m/s\xB2",_size.X - _SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                    LastLine++;
                    break;
                }
                case 1:
                {
                    frame.DrawText("Natural gravity:",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                    frame.DrawText($"{NaturalGravity :N1} m/s\xB2",_size.X,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                    LastLine++;
                    break;
                }
                case 2:
                {
                    frame.DrawText("Artificial gravity:",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                    frame.DrawText($"{TotalGravity - NaturalGravity :N1} m/s\xB2",_size.X,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                    LastLine++;
                    break;
                }
                case 3:
                {
                    frame.DrawText("Total gravity:",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                    frame.DrawText($"{TotalGravity :N1} m/s\xB2",_size.X,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                    LastLine++;
                    break;
                }
            }
        }
        private void UpdateStopSprites(int InOrder)
        {
            //var set = Buffer.StopSort[InOrder];
        }
        private void UpdateShipMassSprites(int InOrder) // todo Complete this
        {
            var set = Buffer.ShipMassSort[InOrder];
            var mass = (double)_Block.CubeGrid.Physics.Mass;
            if (!set.v[0])
            {
                frame.DrawText("Ship Mass:", _SideOffset *2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                frame.DrawText(mass.toHumanWeight(), _size.X - _SideOffset *2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                LastLine++;
                if (set.k <= 0) return;
                frame.DrawProgressBarAUTOLCD(new []{mass,set.k},new Vector2(_SideOffset,LastLine * lineHeight),m_foregroundColor,lineHeight,_size.X - _SideOffset *2,InDivisionsWidth,true);
                LastLine++;
            }
            else
            {
                frame.DrawText("ShipMassBase in progress", _size.X/2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
                LastLine++;
            }
        }
        private void UpdateMassSprites(int InOrder) // todo Complete this
        {
            var set = Buffer.MassSort[InOrder];
            frame.DrawText("Work in progress",_size.X/2,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
            LastLine++;
        }
        private void UpdateOccupiedSprites(int InOrder)
        {
            var set = Buffer.OccupiedSort[InOrder];
            var Blocks = GetBlocks(set.Where,set.isGroup,set.IsOnSameGrid,true,b => b.FatBlock is IMyShipController);
            var List = new List<Pair<string,string>>();
            foreach (var b in Blocks)
            {
                var block = b.FatBlock as IMyShipController;
                if(block != null) List.Add(new Pair<string, string>(block.DisplayNameText,block.IsUnderControl ? block.Pilot.DisplayName : "Free"));
            }
            LastLine = frame.DrawSimpleAUTOLCD(List, LastLine, new Vector2(_size.X, LastLine * lineHeight + _viewport.Y), m_foregroundColor, TextSize, lineHeight, _SideOffset * 2);
        }
        private void UpdateDampenersSprites(int InOrder) // todo try without PilotCockpit
        {
            var ship = _Block.CubeGrid.GetShip().PilotCockpit;
            if (ship != null)
            {
                frame.DrawText("Dampeners:", _SideOffset * 2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                frame.DrawText(ship.DampenersOverride ? "On" : "Off", _size.X - _SideOffset * 2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                LastLine++;
            }
            else
            {
                frame.DrawText("Cannot find control block", _SideOffset * 2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
            }
        }
        private void UpdateDistanceSprites(int InOrder) //todo check
        {
            var set = Buffer.DistanceSort[InOrder];
            var MyPos = _Block.CubeGrid.Physics.CenterOfMassWorld;
            var Distance = (MyPos - set.v).Length();
            frame.DrawText(set.k == "" ? "Distance:" : set.k + ":",_SideOffset * 2,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
            frame.DrawText(set.v != Vector3D.NegativeInfinity ? Distance.toPhysicQuantity("m") : "Wrong format", _size.X -_SideOffset * 2, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
            LastLine++;
        }
        private void UpdateGridCoresSprites(int InOrder)
        {
            var set = Buffer.CoresSort[InOrder];
            var Players = new List<IMyPlayer>();
            IMyPlayer Player;
            if (set == "")
            {
                var id = _terminalBlock.GetOwnerOrBuilder();
                MyAPIGateway.Players.GetPlayers(Players, p => p.IdentityId == id);
            }
            else
            {
                MyAPIGateway.Players.GetPlayers(Players, p => p.DisplayName == set);
            }

            if (Players.Count != 0)
            {
                Player = Players.First();
                var rel = _Block.GetUserRelationToOwner(Player.IdentityId);
                if (rel == MyRelationsBetweenPlayerAndBlock.Enemies ||
                    rel == MyRelationsBetweenPlayerAndBlock.Neutral ||
                    rel == MyRelationsBetweenPlayerAndBlock.NoOwnership)
                {
                    frame.DrawText("Non friendly player",_size.X/2,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
                    LastLine++;
                    return;
                }
                    
            }
            else
            {
                frame.DrawText("Player offline",_size.X/2,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
                LastLine++;
                return;
            }
            var Grids = Player.Grids;
            var Ships = new HashSet<Ship>();
            frame.DrawText(Player.DisplayName + " streaming grids",_size.X/2,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.CENTER, m_fontId);
            LastLine++;
            foreach (var Ship in GameBase.instance.gridToShip)
            {
                if (Grids.Any(g => g == Ship.Key)) Ships.Add(Ship.Value);
            }
            foreach (var Ship in Ships)
            {
                float FullestCoreIntP = 0f, MaxIntegrity = 1f;
                foreach (var Core in Ship.beacons)
                {
                    var minus = Core.BlockDefinition.SubtypeId == "NoFreezeLargeGridCore" ? 0 : 100;
                    var SlimBlock = Core.SlimBlock;
                    if (FullestCoreIntP < SlimBlock.Integrity - minus)
                    {
                        FullestCoreIntP = SlimBlock.Integrity - minus;
                        MaxIntegrity = SlimBlock.MaxIntegrity - minus;
                    }
                }
                var Days = 14 * (FullestCoreIntP / MaxIntegrity);
                var Hours = (Days - (int)Days) * 24;
                frame.DrawText(Ship.grid.DisplayName + ":",_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                frame.DrawText(Ship.beacons.Count != 0 ? Ship.beacons.Count > 1 ? $"{Ship.beacons.Count} Cores {(int)Days} Days {Hours :N0}h": $"{(int)Days} Days {Hours :N0}h" : "No grid core", _size.X, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                LastLine++;
            }
        }
        private void UpdateGarageSprites(int InOrder)
        {
            var set = Buffer.GarageSort[InOrder];
            var blocks = GetBlocks(set.Where,set.isGroup,set.IsOnSameGrid,false, block => block.FatBlock is IMyProjector);
            foreach (var block in blocks)
            {
                var garage = block.FatBlock.GetAs<GarageBlockLogic>();
                if(garage == null) continue;
                
                var Status = garage.m_currentStatus;
                var txt = Status == BlockState.Contains_ReadyForLoad ? Buffer.RegexParse(Buffer.nameRegex, garage.ShipInfo) : Status == BlockState.Busy ? "Busy" : "Empty";
                
                frame.DrawText( block.FatBlock.DisplayNameText,_SideOffset,LastLine * lineHeight,m_foregroundColor,TextSize * SpriteGI.TextHeight, TextAlignment.LEFT, m_fontId);
                frame.DrawText(txt, _size.X, LastLine * lineHeight, m_foregroundColor, TextSize * SpriteGI.TextHeight, TextAlignment.RIGHT, m_fontId);
                
                LastLine++;
            }
        }
        private void Clear()
        {
            Buffer = new AutoLCDBuffer();
            lActions.Clear();
            LargestRightText = _surface.MeasureStringInPixels(new StringBuilder("Quota"),m_fontId,TextSize).X;
            LinesOffScree = 0;
        }
        private void ClearAfterCollection()
        {
            if(Buffer.HScrollSort.Count == 0) HScrollDict.Clear();
        }
        private void GetCustomData()
        {
            if (_terminalBlock.CustomData == "" || _terminalBlock.CustomData == CustomDataBuffer) return; 
            CustomDataBuffer = _terminalBlock.CustomData;

            Clear();
            
            var line = 1;
            foreach (var Text in CustomDataBuffer.Split('\n'))
            {
                if (Text.StartsWith("//")) {} // skip
                else if (Text.StartsWith("FontSize"))
                {
                    ChangeFontSize(Text);
                }
                else if (Text.StartsWith("Cores"))
                {
                    Buffer.ParseCoresCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateGridCoresSprites});
                }
                else if (Text.StartsWith("Garages"))
                {
                    Buffer.ParseGaragesCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateGarageSprites});
                }
                else if (Text.StartsWith("Inventory"))
                {
                    Buffer.ParseInventoryCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateInvSprites});
                }
                else if (Text.StartsWith("Missing"))
                {
                    Buffer.ParseInventoryCommand(Text, line, true);
                    lActions.Add(new LineAction{Order = line, Action = UpdateMisSprites});
                }
                else if (Text.StartsWith("CargoALL"))
                {
                    Buffer.ParseCargoCommand(Text, line, true);
                    lActions.Add(new LineAction{Order = line, Action = UpdateCargoALLSprites});
                }
                else if (Text.StartsWith("Cargo"))
                {
                    Buffer.ParseCargoCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateCargoSprites});
                }
                else if (Text.StartsWith("PowerStored"))
                {
                    Buffer.ParsePowerCommand(Text,line,true);
                    lActions.Add(new LineAction{Order = line, Action = UpdatePowerStoredSprites});
                }
                else if (Text.StartsWith("PowerUsed"))
                {
                    Buffer.ParsePowerUsedCommand(Text,line);
                    lActions.Add(new LineAction{Order = line, Action = UpdatePowerUsedSprites});
                }
                else if (Text.StartsWith("PowerTime"))
                {
                    Buffer.ParsePowerTimeCommand(Text,line);
                    lActions.Add(new LineAction{Order = line, Action = UpdatePowerTimeSprites});
                }
                else if (Text.StartsWith("Power"))
                {
                    Buffer.ParsePowerCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdatePowerSprites});
                }
                else if (Text.StartsWith("Charge"))
                {
                    Buffer.ParseChargeCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateChargeSprites});
                }
                else if (Text.StartsWith("Damage"))
                {
                    Buffer.ParseDamageCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateDamageSprites});
                }
                else if (Text.StartsWith("Docked"))
                {
                    Buffer.ParseDockedCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateDockedSprites});
                }
                else if (Text.StartsWith("BlockCount"))
                {
                    Buffer.ParseBlockCountCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateBlockCountSprites});
                }
                else if (Text.StartsWith("ProdCount"))
                {
                    Buffer.ParseProdCountCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateProdCountSprites});
                }
                else if (Text.StartsWith("EnabledCount"))
                {
                    Buffer.ParseEnableCountCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateEnableCountSprites});
                }
                else if (Text.StartsWith("Working"))
                {
                    Buffer.ParseWorkingCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateWorkingSprites});
                }
                else if (Text.StartsWith("PropBool"))
                {
                    Buffer.ParsePropBoolCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdatePropBoolSprites});
                }
                else if (Text.StartsWith("Details"))
                {
                    Buffer.ParseDetailsCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateDetailsSprites});
                }
                else if (Text.StartsWith("Amount"))
                {
                    Buffer.ParseAmountCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateAmountSprites});
                }
                else if (Text.StartsWith("Oxygen"))
                {
                    Buffer.ParseOxygenCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateOxygenSprites});
                }
                else if (Text.StartsWith("Tanks"))
                {
                    Buffer.ParseTanksCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateTanksSprites});
                }
                else if (Text.ToLower().StartsWith("echo"))
                {
                    Buffer.ParseEchoCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateEchoSprites});
                }
                else if (Text.StartsWith("Center"))
                {
                    Buffer.ParseCenterCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateCenterSprites});
                }
                else if (Text.StartsWith("Right"))
                {
                    Buffer.ParseEchoCommand(Text, line, true);
                    lActions.Add(new LineAction{Order = line, Action = UpdateRightSprites});
                }
                else if (Text.StartsWith("HScroll"))
                {
                    Buffer.ParseHScrollCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateHScrollSprites});
                }
                else if (Text.StartsWith("CustomData"))
                {
                    Buffer.ParseCustomDataCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateCustomDataSprites});
                }
                else if (Text.StartsWith("TextLCD"))
                {
                    Buffer.ParseTextLCDCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateTextLCDSprites});
                }
                else if (Text.StartsWith("Time"))
                {
                    Buffer.ParseTimeCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateTimeSprites});
                }
                else if (Text.StartsWith("DateTime"))
                {
                    Buffer.ParseDateTimeCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateDateTimeSprites});
                }
                else if (Text.StartsWith("Date"))
                {
                    Buffer.ParseDateCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateDateSprites});
                }
                else if (Text.StartsWith("Countdown"))
                {
                    Buffer.ParseCountDownCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateCountDownSprites});
                }
                else if (Text.StartsWith("Pos"))
                {
                    Buffer.ParsePosCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdatePosSprites});
                }
                else if (Text.StartsWith("Altitude"))
                {
                    Buffer.ParseAltitudeCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateAltitudeSprites});
                }
                else if (Text.StartsWith("Speed"))
                {
                    Buffer.ParseSpeedCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateSpeedSprites});
                }
                else if (Text.StartsWith("Accel"))
                {
                    Buffer.ParseAccelCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateAccelSprites});
                }
                else if (Text.StartsWith("Gravity"))
                {
                    Buffer.ParseGravityCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateGravitySprites});
                }
                else if (Text.StartsWith("Stop"))
                {
                    Buffer.ParseStopCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateStopSprites});
                }
                else if (Text.StartsWith("ShipMass"))
                {
                    Buffer.ParseShipMassCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateShipMassSprites});
                    
                }
                else if (Text.StartsWith("Mass"))
                {
                    Buffer.ParseMassCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateMassSprites});
                }
                
                else if (Text.StartsWith("Occupied"))
                {
                    Buffer.ParseOccupiedCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateOccupiedSprites});
                }
                else if (Text.StartsWith("Dampeners"))
                {
                    lActions.Add(new LineAction{Order = line, Action = UpdateDampenersSprites});
                }
                else if (Text.StartsWith("Distance"))
                {
                    Buffer.ParseDistanceCommand(Text, line);
                    lActions.Add(new LineAction{Order = line, Action = UpdateDistanceSprites});
                }
                line++;
            }

            ClearAfterCollection();
        }
        private static IEnumerable<Pair<string, double[]>> UpdateCargo(IEnumerable<IMySlimBlock> blocks, bool InMerge, string name)
        {
            var CargoFill = new List<Pair<string, double[]>>();
            if (InMerge)
            {
                long totalMax = 0;
                long totalFill = 0;
                foreach (var b in blocks)
                {
                    var box = b.FatBlock.GetInventory();
                    totalMax += box.MaxVolume.RawValue;
                    totalFill += box.CurrentVolume.RawValue;
                }
                CargoFill.Add(new Pair<string, double[]>( name == "" ? "Total Cargo" : name, new []{(double)totalFill / 1000, (double)totalMax / 1000}));
            }
            else
            {
                foreach (var box in blocks)
                {
                    var Fat = box.FatBlock;
                    var inv = Fat.GetInventory();
                    CargoFill.Add(new Pair<string, double[]>(Fat.DisplayNameText, new []{(double)inv.CurrentVolume.RawValue / 1000, (double)inv.MaxVolume.RawValue / 1000}));
                }
            }

            return CargoFill;
        }
        private IEnumerable<Pair<string, double[]>> UpdateInv(SortClass set, bool isMissing = false)
        {
            var NItems = new List<Pair<string, double[]>>();
            try
            {
                var blocks = GetBlocks(set.Where, set.isGroup, set.IsOnSameGrid);
                foreach (var _item in set.items)
                {
                    var removeItems = new List<MyPhysicalItemDefinition>();
                    foreach (var item in set.exceptions)
                    {
                        Type _removeType;
                        var _removeItems = new List<MyPhysicalItemDefinition>();
                        if (item.Contains("/"))
                        {
                            var str = item.Split('/');
                            var isTypeFound = Enum.TryParse(str[0].ToLower(), out _removeType);
                            var subs = str[1].Split(',');
                            foreach (var sub in subs)
                            {
                                _removeItems.AddRange(isTypeFound ? GetFromAllItems(_removeType, sub) : GetFromAllItems(subtype: sub));
                            }
                        }
                        else _removeItems = Enum.TryParse(item.ToLower(), out _removeType) ? GetFromAllItems(_removeType) : GetFromAllItems(subtype: item);
                        removeItems.AddRange(_removeItems);
                    }
                    
                    var allItems = new List<MyPhysicalItemDefinition>();
                    Type _type;
                    if (_item.Key.Contains("/"))
                    {
                        var str = _item.Key.Split('/');
                        var isTypeFound = Enum.TryParse(str[0].ToLower(), out _type);
                        var subs = str[1].Split(',');
                        foreach (var sub in subs)
                        {
                            allItems.AddRange(isTypeFound ? GetFromAllItems(_type, sub) : GetFromAllItems(subtype: sub));
                        }
                        
                    }
                    else allItems = Enum.TryParse(_item.Key.ToLower(), out _type) ? GetFromAllItems(_type) : GetFromAllItems(subtype: _item.Key);
                    
                    var items = new Dictionary<MyPhysicalItemDefinition, double>();
                    if (_item.Key.Contains("/"))
                    {
                        var str = _item.Key.Split('/');
                        var subs = str[1].Split(',');
                        var isTypeFound = Enum.TryParse(str[0].ToLower(), out _type);
                        foreach (var sub in subs)
                        {
                            var bufferItems = isTypeFound ? GetItems(blocks, _type, sub) : GetItems(blocks, Type.all, sub);
                            foreach (var item in bufferItems)
                            {
                                if (items.ContainsKey(item.Key)) items[item.Key] += item.Value;
                                else items.Add(item.Key,item.Value);
                            }
                        }
                    }
                    else items = Enum.TryParse(_item.Key.ToLower(), out _type) ? GetItems(blocks, _type) : GetItems(blocks, Type.all, _item.Key);

                    foreach (var item in allItems.Where(item => !removeItems.Contains(item)))
                    {
                        m_sb.Clear();
                        m_sb.Append(_item.Value.toHumanQuantity());
                        var LRT = _surface.MeasureStringInPixels(m_sb, m_fontId, TextSize).X;
                        if (LargestRightText <= LRT) LargestRightText = LRT;
                        
                        if (items.ContainsKey(item))
                        {
                            if (isMissing && items[item] < _item.Value) NItems.Add(new Pair<string, double[]>(item.DisplayNameText, new[] {items[item], _item.Value}));
                            else if (!isMissing) NItems.Add(new Pair<string, double[]>(item.DisplayNameText, new[] {items[item], _item.Value}));
                        }
                        else if(isMissing || !set.isZeroHidden) NItems.Add(new Pair<string, double[]>(item.DisplayNameText, new[] {0, _item.Value}));
                    }
                }
            }
            catch (Exception e)
            {
                HandleException(e,"UpdateInv");
            }
            return NItems;
        }
        private List<MyPhysicalItemDefinition> GetFromAllItems(Type InType = Type.all, string subtype = "")
        {
            var Items = new List<MyPhysicalItemDefinition>();
            switch (InType)
            {
                case Type.all: Items = GetItems("", subtype); break;
                case Type.component: Items = GetItems("MyObjectBuilder_Component", subtype); break;
                case Type.ingot: Items = GetItems("MyObjectBuilder_Ingot", subtype); break;
                case Type.ore: Items = GetItems("MyObjectBuilder_Ore", subtype); break;
                case Type.ammo: Items = GetItems("MyObjectBuilder_AmmoMagazine", subtype); break;
                case Type.tool: Items = GetItems("MyObjectBuilder_PhysicalGunObject", subtype); break;
            }
            return Items;
        }
        private List<MyPhysicalItemDefinition> GetItems(string type, string subtype)
        {
            var Items = new List<MyPhysicalItemDefinition>();
            foreach (var Item in InventoryItems) { if ((string.IsNullOrWhiteSpace(type) || Item.Id.TypeId.ToString().Contains(type)) && Item.Id.SubtypeName.Contains(subtype)) Items.Add(Item); }
            return Items;
        }
        private static Dictionary<MyPhysicalItemDefinition, double> GetItems(List<IMySlimBlock> InBlocks, Type InType, string subtypeId = "")
        {
            var items = new Dictionary<MyPhysicalItemDefinition, double>();
            switch (InType)
            {
                case Type.all: InBlocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items, "", subtypeId, true)); break;
                case Type.component: InBlocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items, "MyObjectBuilder_Component", subtypeId, true)); break;
                case Type.ore: InBlocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items, "MyObjectBuilder_Ore", subtypeId, true)); break;
                case Type.ingot: InBlocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items, "MyObjectBuilder_Ingot", subtypeId, true)); break;
                case Type.ammo: InBlocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items, "MyObjectBuilder_AmmoMagazine", subtypeId, true)); break;
                case Type.tool: InBlocks.ForEach(block => InventoryUtils.GetInventoryItems(block.FatBlock, items, "MyObjectBuilder_PhysicalGunObject", subtypeId, true)); break;
            }
            return items;
        }
        private void ChangeFontSize(string text)
        {
            var buffer = text.Replace("FontSize", "").Replace(" ", "");
            float num;
            float.TryParse(buffer, out num);
            if(num <= 0) return;
            TextSize = num;
        }
        private List<IMySlimBlock> GetBlocks(string name = "", bool isGroup = false, bool isOneGrid = false, bool InGarageIgnore = true, Func<IMySlimBlock,bool> filter = null)
        {
            var terminalBlocks = new List<IMyTerminalBlock>();
            var ignoreName = "Bugagagaga";
            if (InGarageIgnore) ignoreName = "Garage";
            if (isGroup)
            {
                if (isOneGrid) GTS.GetBlockGroupWithName(name).GetBlocks(terminalBlocks, block => block.IsSameConstructAs(_terminalBlock) && !block.SubtypeName().Contains(ignoreName));
                else GTS.GetBlockGroupWithName(name).GetBlocks(terminalBlocks, block => !block.SubtypeName().Contains(ignoreName));
                return terminalBlocks.Select(b => b.SlimBlock).ToList();
            }
            GTS.GetBlocks(terminalBlocks);
            return isOneGrid 
                ? terminalBlocks.Where(b => b.IsSameConstructAs(_terminalBlock)).Select(b => b.SlimBlock).Where(block => (filter == null || filter.Invoke(block)) && block.FatBlock.DisplayNameText.Contains(name) && !block.FatBlock.SubtypeName().Contains(ignoreName)).ToList() 
                : terminalBlocks.Select(b => b.SlimBlock).Where(block => (filter == null || filter.Invoke(block)) && block.FatBlock.DisplayNameText.Contains(name) && !block.FatBlock.SubtypeName().Contains(ignoreName)).ToList();
        }
        private static List<MyPhysicalItemDefinition> RemoveUnneeded(IEnumerable<MyPhysicalItemDefinition> x)
        {
            var removeList = new List<string>{"GoodAI Bot Feedback","CubePlacer"};
            return x.Where(i => !removeList.Contains(i.DisplayNameText)).ToList();
        }
        private void HandleException(Exception exception, string where = "")
        {
            var txt = "Error: AutoLCD::" + where + ": " + exception;
            using (frame = _surface.DrawFrame()) frame.DrawText("Error, press F on screen to read \n and please send text to IEnterNI",_size.X/2,_size.Y/2,Color.Red,SpriteGI.TextHeight / 2, TextAlignment.CENTER);
            var TextPanel = _terminalBlock as IMyTextPanel;
            TextPanel?.WriteText(txt);
        }
    }
}