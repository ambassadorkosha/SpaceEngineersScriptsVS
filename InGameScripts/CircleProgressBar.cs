using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using MyBatteryBlock = Sandbox.ModAPI.Ingame;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;




namespace Scripts.Specials
{

    /// <summary>
    /// Ritor's ProgressCircleBar Script
    /// Version 1.2
    /// 10/04/2021
    /// 
    /// Скрипт поддерживает отображение индикаторов тяги (перехвата) для маршевых (направленных назад по отношению к кокпиту)
    /// двигателей, Заряд батарей, объем газа/жидкости в баках кислорода, водорода, нефти и керосина если таковые имеются на 
    /// корабле. (По идее скрипту всеравно как называются блоки т.к. он ищет по описанию блока в файлах игры)
    /// Также поддерживается отображение индикаторов для групп блоков. Группа должна содержать блоки
    /// одного типа (батареи, баки, двигатели), иначе скрипт укажет на ошибку(Error! в названии индикатора)
    /// Также если группа маршевых трастеров пуста (нет кокпита или нет такого типа трастера) то 
    /// на экране появится  пустое пространство, как будто вы вставили разделитель (читай ниже).   
    /// Максимальное количество символов в названии индикатора - 9б если символов больше то они обрежутся
    /// и в конец добавится многоточие.
    ///    
    /// 
    /// Скрипт поддерживает до трех линий на ЛСД панели (включительно) для отрисовки спрайтов
    /// 
    /// Рамка индикаторов имеет четыре цвета: 
    /// - Зеленый - блоки выбранного типа готовы к работе (включены,
    /// запитаны топливом (например водородные двигатели)); 
    /// - Красный - блоки не готовы к работе (выключены или топливо не поступает (опять же те же водородные
    /// двигатели, если бак в накопителе то они к работе не готовы и рамка индикатора красная);
    /// - Желтый - ЧТО ТО НЕ ТАК! например из один или несколько из группы маршевых трастеров выключен,
    /// или один бак из всех баков данного типа в накопителе а остальные нет, или одна из 
    /// батарей находится в режиме зарядки;
    /// - Голубой - используется для баков и батарей. Для баков показывает что включен накопитель,
    /// для батарей указывает что батареи переведены в состояние "Зарядка"
    /// 
    /// 
    ///=================  пример конфига: =================
    ///
    /// UseSprites  - это должно быть вверху своих данных ЛСД
    /// lcd0 - (или lcd1 или lcd2  и т.п.) задает номер панели в кокпите, все указанное после номера 
    /// ЛСД будет отрисовано на нем (для текстовой панели не указывается)
    /// 
    /// hyd/ker - выводит индикатор для керосиновых и/или водородных двигателей (считаются вместе т.к. жрут топливо)
    /// ion - выводит индикатор для ионных двигателей
    /// batteries - выводит индикатор для батарей
    /// heli - выводит индикатор для вертолетных винтов (считает и большие винты)
    /// oil - выводит индикатор для нефтяных баков
    /// jets - выводит индикатор для атмосферный ускорителей (которые реактивные)   
    /// hover - выводит индикатор для ховер-двигателей (только для тех под днищем грида)
    /// hydrogen - выводит индикатор для водородных баков
    /// oxygen - выводит индикатор для кислородных баков
    ///  ---  три дефиса слитно это разделитель. Хотите добавить пространство между спрайтами?
    ///  Ставьте три дефиса. 
    /// MyGroup - название вашей группы блоков. отобразит индикатор для группы или укажет
    /// что группы с таким названием не существует, или укажет на ошибку если группа 
    /// состоит из разнородных блоков (хотя статус вкл/выкл всеравно будет отрабатывать).
    /// 
    /// </summary>

    ///=================  пример конфига: =================
    ///
    /// UseSprites  - это должно быть вверху своих данных ЛСД
    /// lcd0 - (или lcd1 или lcd2  и т.п.) задает номер панели в кокпите, все указанное после номера 
    /// ЛСД будет отрисовано на нем (для текстовой панели не указывается)
    /// 
    /// hyd/ker - выводит индикатор для керосиновых и/или водородных двигателей (считаются вместе т.к. жрут топливо)
    /// ion - выводит индикатор для ионных двигателей
    /// batteries - выводит индикатор для батарей
    /// heli - выводит индикатор для вертолетных винтов (считает и большие винты)
    /// oil - выводит индикатор для нефтяных баков
    /// jets - выводит индикатор для атмосферный ускорителей (которые реактивные)   
    /// hover - выводит индикатор для ховер-двигателей (только для тех под днищем грида)
    /// hydrogen - выводит индикатор для водородных баков
    /// oxygen - выводит индикатор для кислородных баков
    ///  ---  три дефиса слитно это разделитель. Хотите добавить пространство между спрайтами?
    ///  Ставьте три дефиса. 
    /// MyGroup - название вашей группы блоков. отобразит индикатор для группы или укажет
    /// что группы с таким названием не существует, или укажет на ошибку если группа 
    /// состоит из разнородных блоков (хотя статус вкл/выкл всеравно будет отрабатывать).
    /// 

    [MyTextSurfaceScriptAttribute("CircleProgressBar", "Circle ProgressBar")]
    public class CircleBarLCDScript : MyTSSCommon
    {


        private IMyCubeBlock ts_block;

        public CircleBarLCDScript(IMyTextSurface lcd_surface, IMyCubeBlock ts_block, Vector2 size) : base(lcd_surface, ts_block, size)
        {
            this.ts_block = ts_block;
        }

        #region Vars

        string MainCockpitTag = "[MyRefCockpit]"; //Тег основного кокпита, добавте в имя  кокпита, который желаете сделать основным

        string spritetag = "UseSprite"; //Тег который нужно добавить в свои данные ЛСД или кокпита чтобы скрипт 
        //понял что тут надо рисовать спрайты.
        


        float MinSpriteScalier = 0.4f; // минимальный множитель размера индикатора

       
        string temp_txt = "";
        string[] sArgs;

        IMyCockpit Cockpit;     

        RectangleF viewport;


        //------------------------- Colors ------------------------------------------

        Color BackColor = Color.Black;
        Color ChargeLoad = Color.Cyan;
        Color Online = Color.Green;
        Color Offline = Color.Red;
        Color Attention = Color.Yellow;
        Color TextColor = Color.White;

        //-------------------------Cruising Thrusters as IMyTerminalBlock------------------------------------------

        List<IMyTerminalBlock> _HeliTHR = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> _IonTHR = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> _AtmoTHR = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> _KerAndHydTHR = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> _Hover = new List<IMyTerminalBlock>();

        //-------------------------Tanks as IMyTerminalBlock--------------------------------------------
        List<IMyTerminalBlock> _OilTanks = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> _HydrogenTanks = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> _OxygenTanks = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> _KeroseneTanks = new List<IMyTerminalBlock>();

        //------------------------- Batteries as IMyTerminalBlock --------------------------------------
        List<IMyTerminalBlock> _Batteries = new List<IMyTerminalBlock>();

        //----------------------------------------------------------------------------------------------

        List<IMyTerminalBlock> _Splitter = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> _Cache = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> _myCockpits = new List<IMyTerminalBlock>();

        List<IMyTerminalBlock> _SearchGroup;

        Dictionary<List<IMyTerminalBlock>, string> ValueIdentify;



        #endregion

        public override void Run()
        {

            IMyTextSurface lcd = null;
            int surfaceIndex;
            try
            {
                base.Run();
                if (MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ts_block.CubeGrid) == null
                  || !((IMyTerminalBlock)ts_block).CustomData.Contains(spritetag))
                    return;

                _Cache.Clear();
                _myCockpits.Clear();
                _IonTHR.Clear();
                _AtmoTHR.Clear();
                _HeliTHR.Clear();
                _KerAndHydTHR.Clear();
                _Hover.Clear();
                _OilTanks.Clear();
                _HydrogenTanks.Clear();
                _OxygenTanks.Clear();
                _KeroseneTanks.Clear();
                _Batteries.Clear();
                _Splitter.Clear();


                MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ts_block.CubeGrid).GetBlocksOfType<IMyTerminalBlock>(_Cache, (b) => {

                    if (b.CubeGrid == ts_block.CubeGrid)
                    {
                        var cockpit = b as IMyCockpit; //Found Cockpits
                        if (cockpit != null)
                        {
                            _myCockpits.Add(cockpit);
                            if (cockpit.CustomName.Contains(MainCockpitTag)) { Cockpit = cockpit; } //Found Cocpit with Tag == MainCockpitTag
                            else { Cockpit = _myCockpits[0] as IMyCockpit; } // Set first as referense Cockpit
                                                                             // if (cockpit.CustomData.Contains(spritetag)) { _TSProvider.Add(cockpit); }
                            return false;
                        }
                        var thrust = b as IMyThrust; //Found and sort Cruising thrusters
                        if (thrust != null)
                        {
                            if (Cockpit != null)
                            {
                                var CruisingTHR = Base6Directions.GetOppositeDirection(Cockpit.Orientation.Forward); //Cruising Thrust Direction
                                var sn = thrust.BlockDefinition.SubtypeName;

                                if (CruisingTHR == thrust.Orientation.Forward && (sn.Contains("SmallThrust") || sn.Contains("LargeThrust")))
                                {
                                    _IonTHR.Add(thrust); //Ion Thrusters
                                    return false;
                                }
                                if (CruisingTHR == thrust.Orientation.Forward && (sn.Contains("LargeAtm") || sn.Contains("SmallAtm")) && (!sn.Contains("Vert") || !sn.Contains("Medium")))
                                {
                                    _AtmoTHR.Add(thrust); // Atmospheric Jets
                                    return false;
                                }
                                if (CruisingTHR == thrust.Orientation.Forward && (sn.Contains("MediumAtmosphericThrust") || sn.Contains("VertAtmosphericThrust")))
                                {
                                    _HeliTHR.Add(thrust); //Heli medium and large heli thrusters (if it is using as cruising thrusters)
                                    return false;
                                }
                                if (CruisingTHR == thrust.Orientation.Forward && (sn.Contains("HydrogenThrust") || sn.Contains("KeroseneThrust")))
                                {
                                    _KerAndHydTHR.Add(thrust); // Hydrogen and Kerosine thrusters
                                    return false;
                                }
                                if (thrust.Orientation.Forward == Base6Directions.GetOppositeDirection(Cockpit.Orientation.Up) && sn.Contains("Hover"))
                                {
                                    _Hover.Add(thrust); //Hover engines that oriented down by the grid
                                    return false;
                                }
                                return false;
                            }
                            return false;
                        }
                        var tank = b as IMyGasTank; //Fond Tanks
                        if (tank != null)
                        {
                            var sn = tank.BlockDefinition.SubtypeName;
                            if (sn.Contains("OilTank"))
                            {
                                _OilTanks.Add(tank);
                                return false;
                            }
                            else if (sn.Contains("HydrogenTank"))
                            {
                                _HydrogenTanks.Add(tank);
                                return false;
                            }
                            else if (sn.Contains("KeroseneTank"))
                            {
                                _KeroseneTanks.Add(tank);
                                return false;
                            }
                            else
                            {
                                _OxygenTanks.Add(tank);
                                return false;
                            }
                        }
                        var battery = b as IMyBatteryBlock; //Found Batteries
                        if (battery != null)
                        {
                            var sn = b.BlockDefinition.SubtypeName;
                            if (sn.Contains("Battery"))
                            {
                                _Batteries.Add(battery as IMyTerminalBlock);
                                return false;
                            }
                            return false;
                        }
                        return false;
                    }
                    return false;
                });


                ValueIdentify = new Dictionary<List<IMyTerminalBlock>, string>
                {
                  { _IonTHR, "Ion" }, {_HeliTHR, "Heli" }, {_AtmoTHR, "Jets"}, {_KerAndHydTHR, "Hyd/Ker" }, {_Hover, "Hover" },
                  {_OilTanks, "Oil"}, {_HydrogenTanks, "Hydrogen"}, {_OxygenTanks, "Oxygen"}, {_KeroseneTanks, "Kerosene"},
                  {_Batteries, "Batteries"}, {_Splitter, "---"}

                };


                _Splitter.Add(ts_block as IMyTerminalBlock);

                var provider = ts_block as IMyTerminalBlock;
                temp_txt = provider.CustomData.Substring(provider.CustomData.IndexOf(spritetag) + spritetag.Length).Trim();
                string[] sections = ("\n" + temp_txt).Split(new[] { "\nLCD", "\nlcd", "\nLcd" }, StringSplitOptions.None).Where(x => !string.IsNullOrEmpty(x)).Select(s => s.Trim()).ToArray();

                for (int i = 0; i < sections.Length; i++)
                {
                    string[] lines = sections[i].Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    if (lines.Length > 0 && int.TryParse(lines.First(), out surfaceIndex))
                    {
                        if (surfaceIndex < (provider as IMyTextSurfaceProvider).SurfaceCount)
                        {
                            lcd = (provider as IMyTextSurfaceProvider).GetSurface(surfaceIndex);
                            sArgs = lines.Skip(1).ToArray();
                        }
                    }
                    else
                    {
                        lcd = (provider as IMyTextSurfaceProvider).GetSurface(0);
                        sArgs = lines;
                    }
                    ContentCollector(lcd);
                }


            }


            catch (Exception ex)
            {
                if (lcd != null)
                {
                    //surface.ContentType = ContentType.TEXT_AND_IMAGE;
                    lcd.WriteText("ERROR DESCRIPTION:\n" + ex);
                }
            }


        }


        #region Calcs

        public float CurrentThrust(List<IMyTerminalBlock> thrusts)
        {
            float allthr = 0;
            float curthr = 0;

            foreach (IMyThrust t in thrusts)
            {
                if (t.ThrustOverridePercentage > 0 && t.MaxEffectiveThrust > 0)
                {
                    allthr += t.ThrustOverridePercentage;
                }
                else if (t.ThrustOverridePercentage == 0 && t.MaxEffectiveThrust > 0)
                {
                    allthr += t.CurrentThrust / t.MaxEffectiveThrust;
                }
                else
                {
                    allthr = 0;
                }
            }

            curthr = allthr / thrusts.Count;

            return curthr;
        }


        public float TankLoad(List<IMyTerminalBlock> tank)
        {
            float tanksFilledRatio = 0;
            float totalload = 0;

            foreach (IMyGasTank t in tank)
            {
                tanksFilledRatio += (float)t.FilledRatio;
            }

            totalload = tanksFilledRatio / tank.Count;
            return totalload;
        }


        public float BatteryChargeLvl(List<IMyTerminalBlock> batt)
        {
            float storedPower = 0;

            float chargelvl = 0;

            foreach (IMyBatteryBlock t in batt)
            {
                storedPower += t.CurrentStoredPower / t.MaxStoredPower;
            }

            chargelvl = storedPower / batt.Count;

            return chargelvl;
        }

        #endregion

        #region DrawLogics


        public void ContentCollector(IMyTextSurface lcd)
        {
         
            lcd.ScriptBackgroundColor = Color.Black;

            var frame = lcd.DrawFrame();

            viewport = new RectangleF((lcd.TextureSize - lcd.SurfaceSize) / 2f, lcd.SurfaceSize);

            List<IMyTerminalBlock> tmp_list;

            string BarName = "";

            for (int i = 0; i < sArgs.Length; i++)
            {
                tmp_list = new List<IMyTerminalBlock>();

                bool isExist = true;

                if (ValueIdentify.Values.Select(x => x.ToLower()).Contains(sArgs[i]) && (ValueIdentify.FirstOrDefault(x => x.Value.ToLower() == sArgs[i]).Key).Count > 0)
                {

                    tmp_list = ValueIdentify.FirstOrDefault(x => x.Value.ToLower() == sArgs[i]).Key;
                    BarName = sArgs[i];

                    if (lcd != null)
                    {
                        DrawSprites(lcd, ref frame, viewport, i, tmp_list, BarName, isExist);
                    }

                }
                else if (!ValueIdentify.Values.Select(x => x.ToLower()).Contains(sArgs[i]) && sArgs[i] != spritetag)
                {

                    var foundgroup = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ts_block.CubeGrid).GetBlockGroupWithName(sArgs[i]);

                    if (foundgroup != null)
                    {
                        _SearchGroup = new List<IMyTerminalBlock>();
                        foundgroup.GetBlocks(_SearchGroup, b => b.CubeGrid == ts_block.CubeGrid);

                        if (_SearchGroup.Count > 0)
                        {
                            tmp_list = _SearchGroup;
                            BarName = sArgs[i];

                            if (lcd != null)
                            {
                                DrawSprites(lcd, ref frame, viewport, i, tmp_list, BarName, isExist);
                            }

                        }
                    }
                    else
                    {
                        BarName = sArgs[i];
                        isExist = false;
                        if (lcd != null)
                        {
                            DrawSprites(lcd, ref frame, viewport, i, tmp_list, BarName, isExist);
                        }
                    }
                }

            }

            frame.Dispose();

        }


        public void DrawSprites(IMyTextSurface lcd, ref MySpriteDrawFrame frame, RectangleF viewport, int count, List<IMyTerminalBlock> identify, string barname, bool isExist)
        {
            int MaxNameLen = 9;
            string BarNameValue = "";
            float income = 0;
            bool IsThr = false;
            bool IsGasTank = false;
            bool IsBattery = false;
            bool IsSplitter = false;
            int InList = identify.Count;


            if (barname.Length > MaxNameLen) {BarNameValue = $"{barname.Remove(MaxNameLen-3)}...";}
            else { BarNameValue = barname; }

            if (identify.All(x => x is IMyThrust)) { income = CurrentThrust(identify); IsThr = true; }
            if (identify.All(x => x is IMyGasTank)) { income = TankLoad(identify); IsGasTank = true; }
            if (identify.All(x => x is IMyBatteryBlock)) { income = BatteryChargeLvl(identify); IsBattery = true; }
            if (identify.All(x => x is IMyTextSurfaceProvider)) { IsSplitter = true; }

            float percentage = income;


            bool StatusIsAllActive = identify.All(h => h.IsWorking);  // is all vars in list is active and can work


            Color bordercolor;

            if (!StatusIsAllActive && (identify.Where(h => h.IsWorking).Count() > 0))
            {
                bordercolor = Attention;
            }
            else
            {
                bordercolor = StatusIsAllActive ? Online : Offline;
            }


            if (IsBattery && StatusIsAllActive)
            {
                int automode = identify.Where(x => (x as IMyBatteryBlock).ChargeMode.HasFlag(MyBatteryBlock.ChargeMode.Auto)).Count();
                int dischargemode = identify.Where(x => (x as IMyBatteryBlock).ChargeMode.HasFlag(MyBatteryBlock.ChargeMode.Discharge)).Count();
                int rechargemode = identify.Where(x => (x as IMyBatteryBlock).ChargeMode.HasFlag(MyBatteryBlock.ChargeMode.Recharge)).Count();
                if (rechargemode == InList)
                { bordercolor = ChargeLoad; }
                else if ((automode > 0 || dischargemode > 0) && rechargemode > 0) { bordercolor = Attention; }
                else { bordercolor = StatusIsAllActive ? Online : Offline; }
            }


            if (IsGasTank && StatusIsAllActive)
            {
                int StockpileOn = identify.Where(x => (x as IMyGasTank).Stockpile == true).Count();
                int StockpileOff = identify.Where(x => (x as IMyGasTank).Stockpile == false).Count();
                if (StockpileOn == InList) { bordercolor = ChargeLoad; }
                else if (StockpileOff > 0 && StockpileOn > 0) { bordercolor = Attention; }
                else { bordercolor = StatusIsAllActive ? Online : Offline; }
            }


            if (IsThr && identify.Where(h => h.IsWorking == false).Count() == InList)
            {
                percentage = StatusIsAllActive ? MathHelper.Clamp(income, 0, 1) : 0;
            }



            if (!IsThr && !IsGasTank && !IsBattery && !IsSplitter) { BarNameValue = "Error!"; }


            float minscalier = Math.Max(Math.Min(lcd.SurfaceSize.X / 512, lcd.SurfaceSize.Y / 512), MinSpriteScalier);

            int corrector = (int)Math.Round(255 * percentage, 0);

            int a = 0 + corrector;
            int c = 255 - corrector;
            int b = 0;

            if (a <= 125)
            {
                b = (0 + corrector) / 2;
                MathHelper.Clamp(b, 0, 60);
            }
            else if (a > 125)
            {
                b = (255 - corrector) / 2;
                MathHelper.Clamp(b, 0, 125);
            }


            Color CalculatedColor = IsThr ? new Color(a, b, c) : new Color(c, b, a);


            Vector2 size = new Vector2(100, 100) * minscalier;

            var position = SetBarPosition(lcd, viewport, size, count);

            if (IsSplitter && isExist)
            {
                SplitterSprite(position, size, minscalier, ref frame);
            }
            else if (isExist)
            {
                CirclebarSprites(position, size, CalculatedColor, bordercolor, BarNameValue, percentage, minscalier, ref frame);
            }           
            else if (!isExist)
            {
                NoGroup(position, size, minscalier, ref frame, BarNameValue);
            }

        }


        public Vector2 SetBarPosition(IMyTextSurface lcd, RectangleF viewport, Vector2 size, int count)
        {
            Vector2 startpos = viewport.Position + size * 0.75f;
            float StepMod = 1.2f;
            Vector2 stepX = new Vector2(size.X * StepMod, 0);
            Vector2 stepY = new Vector2(0, size.Y * StepMod);


            float lineEndborder = lcd.SurfaceSize.X - size.X / 2;
            float line_width = startpos.X + stepX.X * count;

            int linenumb = 0;

            int mover = 0;

            Vector2 position = new Vector2(0, 0);

            if (line_width < lineEndborder)
            {
                mover = count;
                position = startpos + stepX * mover + stepY * linenumb;
            }
            else if (line_width > lineEndborder && line_width < lineEndborder * 2)
            {
                linenumb = 1;
                mover = count - (int)(lcd.SurfaceSize.X / stepX.X);

                position = startpos + stepX * mover + stepY * linenumb + new Vector2(0, size.Y / 2);
            }
            else if (line_width > lineEndborder * 2 && line_width < lineEndborder * 3)
            {
                linenumb = 2;
                mover = count - (int)(lcd.SurfaceSize.X / stepX.X) * 2;

                position = startpos + stepX * mover + stepY * linenumb + new Vector2(0, size.Y);
            }
            else { position = -size * 2; }


            Vector2 totalpos = position;
            return totalpos;
        }


        #endregion

        #region Sprites
        public void CirclebarSprites(Vector2 position, Vector2 size, Color CalculatedColor, Color bordercolor, string BarNameValue, float percentage, float minscalier, ref MySpriteDrawFrame frame)
        {

            float baserotation = -(float)Math.PI / 2;
            float rotator = (float)Math.PI * 2 * percentage;
            float ClampedRotator = (float)MathHelper.Clamp(rotator, 0, Math.PI);


            var FirstPartCircle = new MySprite(SpriteType.TEXTURE, "SemiCircle", position, size * 0.93f, CalculatedColor, null, TextAlignment.CENTER, baserotation + rotator); //поворот на 360
            frame.Add(FirstPartCircle);

            var SecondPartCircle = new MySprite(SpriteType.TEXTURE, "SemiCircle", position, size * 0.93f, CalculatedColor, null, TextAlignment.CENTER, baserotation + ClampedRotator);
            frame.Add(SecondPartCircle);

            if (rotator < Math.PI)
            {
                var CowerClipingCircle = new MySprite(SpriteType.TEXTURE, "SemiCircle", position + size * 0.05f, size * 1.14f, BackColor, null, TextAlignment.CENTER, baserotation);
                frame.Add(CowerClipingCircle);
            }

            var BarBorder = new MySprite(SpriteType.TEXTURE, "CircleHollow", position, size * 1.14f, bordercolor, null, TextAlignment.CENTER, 0f);
            frame.Add(BarBorder);

            var CentrHoleCircle = new MySprite(SpriteType.TEXTURE, "Circle", position, size / 1.45f, BackColor, null, TextAlignment.CENTER, 0f);
            frame.Add(CentrHoleCircle);

            var ThrPercentage = new MySprite(SpriteType.TEXT, $"{Math.Round(percentage * 100, 0)}%", position + new Vector2(0, -15 * minscalier), null, TextColor, "DEBUG", TextAlignment.CENTER, 0.9f * minscalier);
            frame.Add(ThrPercentage);

            var Barname = new MySprite(SpriteType.TEXT, BarNameValue, position + new Vector2(0, size.Y / 2 + 10), null, TextColor, "DEBUG", TextAlignment.CENTER, 1.1f * minscalier);
            frame.Add(Barname);

        }

        public void SplitterSprite(Vector2 position, Vector2 size, float minscalier, ref MySpriteDrawFrame frame)
        {
            var Splitter = new MySprite(SpriteType.TEXTURE, "CircleHollow", position, size * 1.12f, BackColor, null, TextAlignment.CENTER, 0f);
            frame.Add(Splitter);
        }

        public void NoGroup(Vector2 position, Vector2 size, float minscalier, ref MySpriteDrawFrame frame, string BarnameValue)
        {
            var NoBlock = new MySprite(SpriteType.TEXT, $"No group \nwith name\n{BarnameValue}", position - size / 2, null, TextColor, "DEBUG", TextAlignment.LEFT, 0.8f * minscalier);
            frame.Add(NoBlock);
        }    


        #endregion



        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

    }
}