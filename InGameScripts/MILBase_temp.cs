
        // НАЧАЛО СКРИПТА
        // Сия поебень - скрипт для главной базы на сервере "Nebula"

        IMyTextPanel LCD_main;

        int zamedlenie = 0;
        int load = 0;
        float info = 0;
        string base_name = "", auto_on_ref = "Выкл";
        string prefix = "";
        int update_time = 100, update_name_time = 1000;
        bool use_id_owner = true, ref_on_kd = false;
        string ref_stat = "";
        string ass_stat = "";
        bool Pred_nastr = false;
        string otladka = "";
        bool light_day_off = false;
        int bat = 0, wind_turb = 0, upgr_mod = 0, garage = 0, gat_tur = 0, miss_tur = 0, couch = 0, desk_corn = 0, bad = 0, weapon_rack = 0, base_ass = 0, basic_ass = 0, refin = 0, electrol = 0, oil_ref = 0, keros_ref = 0,
            hyd_tank = 0, oil_tank = 0, keros_tank = 0, connectors = 0, disel_gen = 0, solar_pan = 0, welder = 0, nanite = 0;

        string[] CargoTypes =
        {
            "Руда",
            "Компоненты",
            "Инструменты",
            "Лед",
            "Слитки",
            "Баллоны",
            "Битум",
            "Боезопас"
        };

        List<string> error_list = new List<string>();                                               //Список ошибок 

        Dictionary<string, List<IMyTerminalBlock>> Cargos;
        List<IMyRefinery> All_ref_list = new List<IMyRefinery>();                                       //Все рефки, ледоломы и т.д.
        List<IMyTextPanel> LCD_all_list = new List<IMyTextPanel>();                                     //LCD панельки
        List<IMyRefinery> IceCrusher_list = new List<IMyRefinery>();                                    //Ледоломы                  
        List<IMyRefinery> OreCrusher_list = new List<IMyRefinery>();                                    //Рудоломы
        List<IMyTextPanel> LCD_Ass_Ref_list = new List<IMyTextPanel>();                                 //LCD состояния рефок и сборщиков
        List<IMyTerminalBlock> Bad_all_list = new List<IMyTerminalBlock>();                             //Кровати
        List<IMyTerminalBlock> Oil_all_list = new List<IMyTerminalBlock>();                             //Нефтезаводы
        List<IMyTerminalBlock> Base_ref_list = new List<IMyTerminalBlock>();                            //Очистители базы
        List<IMyTerminalBlock> Couch_all_list = new List<IMyTerminalBlock>();                           //Диваны
        List<IMyTerminalBlock> Keros_all_list = new List<IMyTerminalBlock>();                           //Экстракторы кероса
        List<IMyTerminalBlock> Power_all_list = new List<IMyTerminalBlock>();                           //Источники энергии
        List<IMyLightingBlock> Light_all_list = new List<IMyLightingBlock>();                           //Всё освещение
        List<IMyTerminalBlock> Garage_all_list = new List<IMyTerminalBlock>();                          //Гаражи
        List<IMyTerminalBlock> Coсpit_all_list = new List<IMyTerminalBlock>();                          //Кокпиты
        List<IMyTerminalBlock> Welder_all_list = new List<IMyTerminalBlock>();                          //Сварщики
        List<IMyTerminalBlock> Gas_Gen_all_list = new List<IMyTerminalBlock>();                         //Электролизёры/керос/битум
        List<IMyTerminalBlock> Battery_all_list = new List<IMyTerminalBlock>();                         //АКБ
        List<IMyTerminalBlock> Gas_Tank_all_list = new List<IMyTerminalBlock>();                        //Баки
        List<IMyTerminalBlock> Oil_Tank_all_list = new List<IMyTerminalBlock>();                        //Нефтяные баки
        List<IMyTerminalBlock> Base_ref_all_list = new List<IMyTerminalBlock>();                        //Все рефки базы, ледоломы и т.д.
        List<IMyTerminalBlock> Base_Battery_list = new List<IMyTerminalBlock>();                        //АКБ Базовые
        List<IMyTerminalBlock> Connector_all_list = new List<IMyTerminalBlock>();                       //Коннекторы
        List<IMyTerminalBlock> Projector_all_list = new List<IMyTerminalBlock>();                       //Проекторы
        List<IMyTerminalBlock> Generator_all_list = new List<IMyTerminalBlock>();                       //Дизель генераторы
        List<IMyTerminalBlock> DeskCorner_all_list = new List<IMyTerminalBlock>();                      //Столы
        List<IMyTerminalBlock> Keros_Tank_all_list = new List<IMyTerminalBlock>();                      //Керосиновые баки
        List<IMyTerminalBlock> Base_Assembler_list = new List<IMyTerminalBlock>();                      //Обычные сборщики базы
        List<IMyTerminalBlock> SolarPanel_all_list = new List<IMyTerminalBlock>();                      //Солнечные панели
        List<IMyLightingBlock> Base_projector_list = new List<IMyLightingBlock>();                      //Прожекторы базы
        List<IMyTerminalBlock> WeaponRack_all_list = new List<IMyTerminalBlock>();                      //Шкафчики
        List<IMyTerminalBlock> Basic_Assembler_list = new List<IMyTerminalBlock>();                     //Инструментарии базы
        List<IMyTerminalBlock> Welder_base_all_list = new List<IMyTerminalBlock>();                     //Строительные блоки
        List<IMyTerminalBlock> Nanite_base_all_list = new List<IMyTerminalBlock>();                     //Наниты
        List<IMyTerminalBlock> Electrolize_all_list = new List<IMyTerminalBlock>();                     //Электролизёры
        List<IMyTerminalBlock> Wind_Turbine_all_list = new List<IMyTerminalBlock>();                    //Ветряки
        List<IMyTerminalBlock> Hydrogen_Tank_all_list = new List<IMyTerminalBlock>();                   //Водородные баки
        List<IMyTerminalBlock> Base_Assembler_all_list = new List<IMyTerminalBlock>();                  //Все сборщики базы
        List<IMyTerminalBlock> Upgrade_mudule_all_list = new List<IMyTerminalBlock>();                  //Модули апгрейда 
        List<IMyTerminalBlock> TerminalBlocks_all_list = new List<IMyTerminalBlock>();                  //Все блоки
        List<IMyTerminalBlock> Gatling_Turret_all_list = new List<IMyTerminalBlock>();                  //Гатлинги
        List<IMyTerminalBlock> Missile_Turret_all_list = new List<IMyTerminalBlock>();                  //Ракетницы
        List<IMyTerminalBlock> CargoContainers_all_list = new List<IMyTerminalBlock>();                 //Контейнеры


        bool filterThis(IMyTerminalBlock block)                         //Фильтр поиска блоков в одном гриде
        {
            return block.CubeGrid == Me.CubeGrid;
        }
        public Program()
        {
            Me.Enabled = true;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            Cargos = new Dictionary<string, List<IMyTerminalBlock>>();
            LCD_main = GridTerminalSystem.GetBlockWithName("LCD_main") as IMyTextPanel;


            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(LCD_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Coсpit_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(Gas_Tank_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(Base_ref_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(Light_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(Power_all_list, filterThis);

            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(Battery_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(Gas_Gen_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(SolarPanel_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(Welder_base_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(Connector_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(Base_Assembler_all_list, filterThis);

            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(CargoContainers_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyLargeGatlingTurret>(Gatling_Turret_all_list, filterThis);
            GridTerminalSystem.GetBlocksOfType<IMyLargeMissileTurret>(Missile_Turret_all_list, filterThis);

            LCD_Ass_Ref_list = LCD_all_list.FindAll(x => x.CustomName.Contains("LCD_Очистка/сборка"));                                                  //LCD

            Oil_all_list = Gas_Gen_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("OilRefinery"));                                          //Нефтезаводы
            Bad_all_list = Coсpit_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockBed"));                                         //Кровати 
            Oil_Tank_all_list = Gas_Tank_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("OilTank"));                                        //Нефтебаки
            Generator_all_list = Power_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("OilEngine"));                                        //Дизель генераторы
            Base_ref_list = Base_ref_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeRefinery"));                                      //Рефки
            Couch_all_list = Coсpit_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockCouch"));                                     //Диваны
            Keros_all_list = Gas_Gen_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("KeroseneGenerator"));                                  //Экстракторы кероса
            Base_projector_list = Light_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("ClassicSpotBar"));                                  //Прожекторы
            Base_Battery_list = Battery_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("Large3x3x2Base"));                                  //Батареи базы
            DeskCorner_all_list = Coсpit_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockDesk"));                                 //Стол
            Keros_Tank_all_list = Gas_Tank_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("KeroseneTank"));                                 //Керосиновые баки
            Welder_all_list = Welder_base_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeShipWelder"));                               //Сварщики
            Electrolize_all_list = Gas_Gen_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("OxygenGenerator"));                              //Газ генераторы
            Hydrogen_Tank_all_list = Gas_Tank_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("HydrogenTank"));                              //Водородные баки
            Nanite_base_all_list = Welder_base_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("RepairSystem"));                             //Наниты

            Base_Assembler_list = Base_Assembler_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeAssembler"));                         //Сборщики
            Basic_Assembler_list = Base_Assembler_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("BasicAssembler"));                        //Инструментарии
            WeaponRack_all_list = CargoContainers_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockWeaponRack"));                  //Оружейные шкафчики 
            WeaponRack_all_list.AddRange(CargoContainers_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockLockerRoomCorner")));    //+Шкафчики

            //Ошибки
            if (LCD_main == null) { error_list.Add("Нет LCD с именем <<LCD_main>>!"); }
            if (LCD_Ass_Ref_list.Count == 0) { error_list.Add("Нет LCD с именем <<LCD_Очистка/сборка>>!"); }
        }

        public void Main(string args)
        {
            load++;
            zamedlenie++;
            if ((LCD_main == null) || (LCD_Ass_Ref_list.Count == 0))
            {
                Echo("Инициализация не успешна,\nвыход из main... " + "\n" + String.Join("\n", error_list) + "\n" + InsertActivityChar(load) + "\n");
                return;
            }
            else
            {
                Echo("Инициализация успешна  " + InsertActivityChar(load) + "\n" + zamedlenie % update_time + "\n" + String.Join("\n", error_list));
            }

            //Преднастройка
            if (Pred_nastr == false)
            {
                SetLcdConfig(LCD_main, ContentType.TEXT_AND_IMAGE, 0.78f, 0.0f, Color.Black, 0, 100, 0, "ОТЛАДКА");
                foreach (var i in LCD_Ass_Ref_list)
                {
                    SetLcdConfig(i, ContentType.TEXT_AND_IMAGE, 0.78f, 0.0f, Color.Black, 85, 0, 60, "Monospace");        //Экраны состояния рефок/сборщиков
                }
                Pred_nastr = true;
            }

            GridTerminalSystem.GetBlocksOfType(All_ref_list);
            GridTerminalSystem.GetBlocksOfType(Projector_all_list, filterThis);
            IceCrusher_list = All_ref_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("IceCrusher"));
            OreCrusher_list = All_ref_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("OreCrusher"));
            Garage_all_list = Projector_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("Garage"));

            //Отключаем рудоломки и ледоломки
            {
                foreach (var i in IceCrusher_list)
                {
                    if (i.Enabled == true)
                    {
                        i.Enabled = false;
                    }
                }
                foreach (var i in OreCrusher_list)
                {
                    if (i.Enabled == true)
                    {
                        i.Enabled = false;
                    }
                }
            }
            //Переименовываем блоки и меняем им параметры
            {
                if ((zamedlenie % update_time) == 0)
                {
                    CustomData();
                    GridTerminalSystem.GetBlocksOfType(Upgrade_mudule_all_list, filterThis);
                    GridTerminalSystem.GetBlocksOfType(TerminalBlocks_all_list, filterThis);
                    Wind_Turbine_all_list = TerminalBlocks_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("WindTurbine"));

                    bat = Rename(Battery_all_list, "АКБ", false, false, bat);                                                       //АКБ
                    wind_turb = Rename(Wind_Turbine_all_list, "Ветряк", false, false, wind_turb);                                   //Ветряные мельницы
                    upgr_mod = Rename(Upgrade_mudule_all_list, "Модуль улучшения", false, false, upgr_mod);                         //Модули апгрейда
                    garage = Rename(Garage_all_list, "Гараж", false, false, garage);                                                //Гаражи
                    gat_tur = Rename(Gatling_Turret_all_list, "Гатлинг", false, true, gat_tur);                                     //Турели гатлинга
                    miss_tur = Rename(Missile_Turret_all_list, "Ракетница", false, true, miss_tur);                                 //Ракетницы
                    couch = Rename(Couch_all_list, "Какой-то блять диван", false, false, couch);                                    //Диваны
                    desk_corn = Rename(DeskCorner_all_list, "Какой-то угловой стол", false, false, desk_corn);                      //Столы
                    bad = Rename(Bad_all_list, "Какая-то кровать", false, false, bad);                                              //Кровати
                    weapon_rack = Rename(WeaponRack_all_list, "Оружейный шкафчик", false, false, weapon_rack);                      //Оружейные шкафчики
                    base_ass = Rename(Base_Assembler_list, "Сборщик", true, false, base_ass);                                       //Сборщики
                    basic_ass = Rename(Basic_Assembler_list, "Инструментарий", true, false, basic_ass);                             //Инструментарии
                    refin = Rename(Base_ref_list, "Очиститель", true, false, refin);                                                //Рефки
                    electrol = Rename(Electrolize_all_list, "Электоролизёр", false, false, electrol);                               //Электролизёры
                    oil_ref = Rename(Oil_all_list, "Нефтезавод", false, false, oil_ref);                                            //Нефтезаводы
                    keros_ref = Rename(Keros_all_list, "Экстрактор керосина", false, false, keros_ref);                             //Керос баки
                    hyd_tank = Rename(Hydrogen_Tank_all_list, "Водородный бак", false, false, hyd_tank);                            //Водородные баки
                    oil_tank = Rename(Oil_Tank_all_list, "Нефтяной бак", false, false, oil_tank);                                   //Нефте баки
                    keros_tank = Rename(Keros_Tank_all_list, "Керосиновый бак", false, false, keros_tank);                          //Керос баки
                    connectors = Rename(Connector_all_list, "Коннектор", false, false, connectors);                                 //Коннекторы
                    disel_gen = Rename(Generator_all_list, "Дизель генератор", false, false, disel_gen);                            //Дизель генераторы
                    solar_pan = Rename(SolarPanel_all_list, "Солнечная панель", false, false, solar_pan);                           //Солнечные панели
                    welder = Rename(Welder_all_list, "Сварщик", false, false, welder);                                              //Сварщики
                    nanite = Rename(Nanite_base_all_list, "Нанитка", true, false, nanite);                                          //Наниты
                }
            }
            //Статусы рефок/сборщиков
            {
                if ((zamedlenie % 20) == 0)
                {
                    ref_stat = " Очистители: \n";
                    List<IMyRefinery> Base_Refins = Base_ref_list.ConvertAll(x => (IMyRefinery)x);
                    List<IMyAssembler> Base_Assemblers = Base_Assembler_list.ConvertAll(x => (IMyAssembler)x);
                    foreach (var i in Base_Refins)
                    {
                        int leight = SetBlocksName("", i.OwnerId).Length;
                        int ostat = 20 - leight;
                        ref_stat += SetBlocksName("", i.OwnerId) + new String('_', ostat) + GetRefStatus(i) + "\n";
                    }
                    ass_stat = " Сборщики: \n";
                    foreach (var i in Base_Assemblers)
                    {
                        int leight = SetBlocksName("", i.OwnerId).Length;
                        int ostat = 20 - leight;
                        ass_stat += SetBlocksName("", i.OwnerId) + new String('_', ostat) + GetAssStatus(i) + "\n";
                    }
                }
            }
            //Включаем рефки
            {
                if (args == "Ref_On") { ref_on_kd = true; }
                if (args == "Ref_Off") { ref_on_kd = false; }

                if (ref_on_kd)
                {
                    auto_on_ref = "Вкл";
                    if ((zamedlenie % 30) == 0)
                    {
                        if (Base_ref_list.Count > 0)
                        {
                            List<IMyRefinery> Refins = Base_ref_list.ConvertAll(x => (IMyRefinery)x);
                            foreach (var Ref in Refins)
                            {
                                if (Ref.GetInventory(0).ItemCount > 0)
                                {
                                    Ref.Enabled = true;
                                }
                                else
                                {
                                    Ref.Enabled = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    auto_on_ref = "Выкл";
                }
            }
            //Отключаем свет днём
            if ((zamedlenie % 800) == 0)
            {
                if(light_day_off)
                {
                    List<IMySolarPanel> Sol_panels = SolarPanel_all_list.ConvertAll(x => (IMySolarPanel)x);
                    foreach (var i in Sol_panels)
                    {
                        info = i.CurrentOutput;
                        if (i.CurrentOutput >= 5.0f)
                        {
                            foreach (var light in Base_projector_list)
                            {
                                light.Enabled = false;
                            }
                        }
                        else
                        {
                            foreach (var light in Base_projector_list)
                            {
                                light.Enabled = true;
                            }
                        }
                    }
                }
            }
            //Отключение дизель генераторов
            if ((zamedlenie % 70) == 0)
            {
                List<IMyBatteryBlock> Batts = Base_Battery_list.ConvertAll(x => (IMyBatteryBlock)x);
                foreach (var i in Batts)
                {
                    float bat_charge = GetChargeBat(i);
                    if (bat_charge >= 100.0f)
                    {
                        List<IMyPowerProducer> Batterys = Generator_all_list.ConvertAll(x => (IMyPowerProducer)x);
                        foreach (var z in Batterys)
                        {
                            z.Enabled = false;
                        }
                    }
                }
            }
            //Сортировка
            if ((zamedlenie % 60) == 0)
            {
                foreach (string CargoType in CargoTypes)
                {
                    Cargos[CargoType] = new List<IMyTerminalBlock>();

                    GridTerminalSystem.SearchBlocksOfName("[" + CargoType + "]", Cargos[CargoType]);
                }
                SortAssemberItems();
                SortRefineryItems();
                SortCargos();
            }

            //Вся хуйня - на дисплеи
            foreach (var i in LCD_Ass_Ref_list)
            {
                i.WriteText(ref_stat + "\n" + ass_stat, false);
                i.WriteText("\n" + " Автовключение рефок:  " + "[" + auto_on_ref + "]", true);
            }
            LCD_main.WriteText(zamedlenie % update_time + "\n" + Base_projector_list.Count() + "\n" + SolarPanel_all_list.Count() + "\n" + info + "\n" + base_name + "\n" + prefix + "\n" + use_id_owner + "\n"
               + "Отключение света днём: " + light_day_off, false);
        }
        //Свои данные
        public void CustomData()
        {
            if (Me.CustomData.Length < 1)
            {
                Me.CustomData = "Сия поебень - скрипт для кораблей/баз, предназначенный для\nупорядочивания пиздеца и некоторых других фич\n v 0.1 Okabe edition\n" +
                                "1) Название базы/корабля: <?>\n" +
                                "2) Префикс, (Будет отображаться в конце имён блоков): <?>\n" +
                                "3) Частота обновления (в тиках): <100>\n" +
                                "4) Добавлять в конце названия блока ник владельца (true/false)?: <true>\n"+
                                "5) Отключать свет днём (true/false)?: <true>";
            }
            else
            {
                string custom_date = Me.CustomData;
                string[] custom_date_list = custom_date.Split('\n');
                string[] base_name_list = custom_date_list[3].Split(':');
                string[] prefix_name_list = custom_date_list[4].Split(':');
                string[] update_time_list = custom_date_list[5].Split(':');
                string[] use_id_owner_list = custom_date_list[6].Split(':');
                string[] use_light_day_night_list = custom_date_list[7].Split(':');

                base_name = base_name_list[1].TrimStart(' ', '<').TrimEnd('>');
                prefix = " " + prefix_name_list[1].TrimStart(' ', '<').TrimEnd('>');
                update_time = Int32.Parse(update_time_list[1].TrimStart(' ', '<').TrimEnd('>'));
                string use_id_owner_flag = use_id_owner_list[1].TrimStart(' ', '<').TrimEnd('>');
                string light_day_off_str = use_light_day_night_list[1].TrimStart(' ', '<').TrimEnd('>');

                if (use_id_owner_flag == "true") { use_id_owner = true; } else if (use_id_owner_flag == "false") { use_id_owner = false; }
                if (light_day_off_str == "true") { light_day_off = true; } else if (light_day_off_str == "false") { light_day_off = false; }
            }
            return;
        }

        //Переименовывание
        public int Rename (List<IMyTerminalBlock> list, string name, bool ShowInTerminal, bool ShowInToolbarConfig, int list_count)
        {
            if (list.Count != list_count)
            {
                foreach (var i in list)
                {
                    if (ShowInTerminal == false)
                    {
                        if (i.ShowInTerminal == true)
                        {
                            i.ShowInTerminal = false;
                        }
                    }
                    if (ShowInToolbarConfig == false)
                    {
                        if (i.ShowInToolbarConfig == true)
                        {
                            i.ShowInToolbarConfig = false;
                        }
                    }
                    
                    if (i.CustomName.Contains(SetBlocksName(name + prefix, i.OwnerId)) == false)
                    {
                        i.CustomName = SetBlocksName(name + prefix, i.OwnerId);
                    }
                }
            }
            return list.Count;
        }

        //Состояние батарей
        public float GetChargeBat(IMyBatteryBlock batt)
        {
            float stor_power = batt.CurrentStoredPower;
            return stor_power;
        }
        //Состояние рефок
        public string GetRefStatus(IMyRefinery refin)
        {
            string status = "Не работает";
            string[] ref_det_info = refin.DetailedInfo.Split('\n');
            string[] def_ref_energy = ref_det_info[1].Split(':');
            string[] cur_ref_energy = ref_det_info[2].Split(':');


            if (cur_ref_energy[1].Contains("."))
            {
                string[] def_ref_energy_1 = def_ref_energy[1].Split('.');
                string[] cur_ref_energy_1 = cur_ref_energy[1].Split('.');
                string value_def = def_ref_energy_1[1].Remove(0, 3);
                string value_cur = cur_ref_energy_1[1].Remove(0, 3);
                int def_ref_energy_2 = Int32.Parse(def_ref_energy_1[0].TrimStart(' '));
                int cur_ref_energy_2 = Int32.Parse(cur_ref_energy_1[0].TrimStart(' '));

                if (cur_ref_energy_2 >= 100)
                {
                    status = "Работает";
                }
                if ((value_cur == "GW") || (cur_ref_energy_2 > 500))
                {
                    status = "Хуярит";
                }
            }
            if (refin.Enabled == false)
            {
                status = "Выкл";
            }
            return status;
        }
        //Состояние сборщиков
        public string GetAssStatus(IMyAssembler assembler)
        {
            string status = "";
            string[] ref_det_info = assembler.DetailedInfo.Split('\n');
            string[] def_ass_energy = ref_det_info[1].Split(':');
            string[] cur_ass_energy = ref_det_info[2].Split(':');


            if (cur_ass_energy[1].Contains("."))
            {
                string[] def_ass_energy_1 = def_ass_energy[1].Split('.');
                string[] cur_ass_energy_1 = cur_ass_energy[1].Split('.');
                string value_def = def_ass_energy_1[1].Remove(0, 3);
                string value_cur = cur_ass_energy_1[1].Remove(0, 3);
                int def_ass_energy_2 = Int32.Parse(def_ass_energy_1[0].TrimStart(' '));
                int cur_ass_energy_2 = Int32.Parse(cur_ass_energy_1[0].TrimStart(' '));

                if (cur_ass_energy_2 >= 1)
                {
                    status = "Включен";
                }
                if (cur_ass_energy_2 >= 10)
                {
                    status = "Работает";
                }
                if ((value_cur == "GW") || (cur_ass_energy_2 > 90))
                {
                    status = "Хуярит";
                }
            }
            if (assembler.Enabled == false)
            {
                status = "Выкл";
            }
            return status;
        }
        //Какой грид в гараже
        public string GetGridInGarage(IMyProjector garage)
        {
            string grid_name = "Пустой";
            //string[] garage_info = garage.DetailedInfo.Split('\n');
            //string[] garage_info = garage.CustomInfo.Split('\n');
            //if(garage_info[4].Contains("Ship"))
            {
                //string [] grid_name_stroka = garage_info[5].Split(':');
                //grid_name = grid_name_stroka[1].TrimStart(' ');
            }
            //grid_name = garage_info.Length.ToString();
            grid_name = garage.DetailedInfo;
            return grid_name;
        }
        //Ники
        public string SetBlocksName(string name, long ID)
        {
            //Owner_ID классных ребят
            /*long okabe_owner_ID = 144115188075858435;          string Okabe = "Okabe";
            long vase72_owner_ID = 144115188075858437;           string Vase72 = "Vase72";
            long kalter_owner_ID = 144115188075858443;           string Kalter = "Kalter";
            long lion_owner_ID = 144115188075858441;             string Lion = "Lion";
            long lubomir_owner_ID = 144115188075858445;          string Lubomir = "Lubomir";
            long mikopolo_owner_ID = 144115188075858434;         string Nicopolo = "Nicopolo";
            long santa_owner_ID = 144115188075858446;            string Santa = "Santa";
            long xamlo_owner_ID = 144115188075858440;            string Xamlo = "Xamlo";
            long kosh_owner_ID = 144115188075858457;             string Kosh = "Kosh";
            //long gidonic_owner_ID = 144115188075858440;        string Gidonic = "Gidonic";*/

            if (use_id_owner == false)
            {
                return name + "  ";
            }
            else
            {
                string nic = "";
                switch (ID)
                {
                    case 144115188075858435: nic = "[Okabe]"; break;
                    case 144115188075858437: nic = "[Vase72]"; break;
                    case 144115188075858443: nic = "[Kalter]"; break;
                    case 144115188075858441: nic = "[Lion]"; break;
                    case 144115188075858445: nic = "[Lubomir]"; break;
                    case 144115188075858434: nic = "[Nicopolo]"; break;
                    case 144115188075858446: nic = "[Santa]"; break;
                    case 144115188075858440: nic = "[Xamlo]"; break;
                    case 144115188075858457: nic = "[Kosh]"; break;
                }
                return name + " " + nic;
            }
        }
        //Задать настройки всем дисплеям
        public void SetLcdConfig(IMyTextSurface lcd, ContentType type, float font_size, float text_padding, Color background_color, int R, int G, int B, string fount)
        {
            lcd.ContentType = type;
            lcd.FontSize = font_size;
            lcd.TextPadding = text_padding;
            lcd.BackgroundColor = background_color;
            lcd.FontColor = new Color(R, G, B);
            lcd.Font = fount;
        }
        //Сортировка
        //Сборщики
        void SortAssemberItems()
        {
            if (Base_Assembler_all_list.Count > 0)
            {
                foreach (IMyAssembler Assembler in Base_Assembler_all_list)
                {
                    if (Assembler.GetInventory(1).ItemCount > 0)
                    {
                        List<MyInventoryItem> Items;
                        Items = new List<MyInventoryItem>();

                        Assembler.GetInventory(1).GetItems(Items);

                        foreach (MyInventoryItem Item in Items)
                        {
                            if (Cargos.ContainsKey("Компоненты"))
                            {
                                foreach (IMyCargoContainer Cargo in Cargos["Компоненты"])
                                {
                                    if (!Cargo.GetInventory(0).IsFull)
                                    {
                                        Assembler.GetInventory(1).TransferItemTo(Cargo.GetInventory(0), Item);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        //Очистители
        void SortRefineryItems()
        {
            if (Base_ref_all_list.Count > 0)
            {
                foreach (IMyRefinery Refinery in Base_ref_all_list)
                {
                    if (Refinery.GetInventory(1).ItemCount > 0)
                    {
                        List<MyInventoryItem> Items;
                        Items = new List<MyInventoryItem>();

                        Refinery.GetInventory(1).GetItems(Items);

                        foreach (MyInventoryItem Item in Items)
                        {
                            if (Cargos.ContainsKey("Слитки"))
                            {
                                foreach (IMyCargoContainer Cargo in Cargos["Слитки"])
                                {
                                    if (!Cargo.GetInventory(0).IsFull)
                                    {
                                        Refinery.GetInventory(1).TransferItemTo(Cargo.GetInventory(0), Item);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        //Контейнеры
        void SortCargos()
        {
            List<MyInventoryItem> Items;

            foreach (string CargoType in CargoTypes)
            {
                if (Cargos.ContainsKey(CargoType))
                {
                    foreach (IMyCargoContainer Cargo in Cargos[CargoType])
                    {
                        if (Cargo.GetInventory(0).ItemCount > 0)
                        {
                            Items = new List<MyInventoryItem>();
                            Cargo.GetInventory(0).GetItems(Items);
                            foreach (MyInventoryItem Item in Items)
                            {
                                string ItemFullType = Item.Type.ToString();

                                int firstStringPosition = ItemFullType.IndexOf("_");
                                int secondStringPosition = ItemFullType.IndexOf("/");
                                string ItemType = ItemFullType.Substring(firstStringPosition + 1, secondStringPosition - firstStringPosition - 1);
                                string otladka = firstStringPosition + "\n" + secondStringPosition + "\n" + ItemType;

                                if (ItemFullType.IndexOf("Ice") != -1) ItemType = "Ice";

                                if (ItemType != CargoType)
                                {
                                    if (Cargos.ContainsKey(ItemType.ToUpper()))
                                    {
                                        foreach (IMyCargoContainer CargoDst in Cargos[ItemType.ToUpper()])
                                        {
                                            if (!CargoDst.GetInventory(0).IsFull)
                                            {
                                                Cargo.GetInventory(0).TransferItemTo(CargoDst.GetInventory(0), Item);
                                                break;
                                            }
                                        }
                                    }
                                    else if (Cargos.ContainsKey("STUFF"))
                                    {
                                        foreach (IMyCargoContainer CargoDst in Cargos["STUFF"])
                                        {
                                            if (!CargoDst.GetInventory(0).IsFull)
                                            {
                                                Cargo.GetInventory(0).TransferItemTo(CargoDst.GetInventory(0), Item);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Лёд

            /*if (CargoContainers_all_list.Count > 0)
            {
                foreach (IMyCargoContainer Cargo in CargoContainers_all_list)
                {
                    if (Cargo.GetInventory(1).ItemCount > 0)
                    {
                        List<MyInventoryItem> Items;
                        Items = new List<MyInventoryItem>();

                        Refinery.GetInventory(1).GetItems(Items);

                        foreach (MyInventoryItem Item in Items)
                        {
                            if (Cargos.ContainsKey("Слитки"))
                            {
                                foreach (IMyCargoContainer Cargo in Cargos["Слитки"])
                                {
                                    if (!Cargo.GetInventory(0).IsFull)
                                    {
                                        Refinery.GetInventory(1).TransferItemTo(Cargo.GetInventory(0), Item);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            */
        }
        //Крутилка
        public char InsertActivityChar(int counter)
        {
            switch ((counter / 10) % 4)
            {
                case 0: { return '|'; }
                case 1: { return '/'; }
                case 2: { return '-'; }
                case 3: { return '\\'; }
            }
            return '†';
        }
        public void Save()
        { }
        // КОНЕЦ СКРИПТА

