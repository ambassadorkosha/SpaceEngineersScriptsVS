
        // НАЧАЛО СКРИПТА
        // Сия поебень - скрипт для главной базы на сервере "Nebula"

        float info = 0;
        string otladka = "", otladka_2 = "", ass_stat = "", ref_stat = "", prefix = "", base_name = "", auto_on_ref = "Выкл";
        bool Pred_nastr = false, pereinit = false, light_day_off = false, use_id_owner = true, ref_on_kd = false;
        int load = 0, zamedlenie = 0, update_time = 100, update_name_time = 1000, update_properties = 600;
        int bat = 0, wind_turb = 0, upgr_mod = 0, garage = 0, gat_tur = 0, miss_tur = 0, couch = 0, desk_corn = 0, bad = 0, weapon_rack = 0, base_ass = 0, basic_ass = 0, refin = 0, electrol = 0, oil_ref = 0, keros_ref = 0,
        hyd_tank = 0, oil_tank = 0, keros_tank = 0, connectors = 0, disel_gen = 0, solar_pan = 0, welder = 0, nanite = 0, cargo = 0, doors = 0, small_cargo = 0, lcd = 0, sartir = 0, design = 0;

        IMyTextPanel LCD_main;
        List<string> error_list = new List<string>();                                               //Список ошибок 
        List<IMyRefinery> All_ref_list = new List<IMyRefinery>();                                       //Все рефки, ледоломы и т.д.
        List<IMyRefinery> IceCrusher_list = new List<IMyRefinery>();                                    //Ледоломы                  
        List<IMyRefinery> OreCrusher_list = new List<IMyRefinery>();                                    //Рудоломы
        List<IMyTerminalBlock> LCD_all_list = new List<IMyTerminalBlock>();                             //LCD панельки
        List<IMyTerminalBlock> Bad_all_list = new List<IMyTerminalBlock>();                             //Кровати
        List<IMyTerminalBlock> Oil_all_list = new List<IMyTerminalBlock>();                             //Нефтезаводы
        List<IMyTerminalBlock> door_all_list = new List<IMyTerminalBlock>();                            //Двери
        List<IMyTerminalBlock> Base_ref_list = new List<IMyTerminalBlock>();                            //Очистители базы
        List<IMyTerminalBlock> Couch_all_list = new List<IMyTerminalBlock>();                           //Диваны
        List<IMyTerminalBlock> Keros_all_list = new List<IMyTerminalBlock>();                           //Экстракторы кероса
        List<IMyTerminalBlock> Power_all_list = new List<IMyTerminalBlock>();                           //Источники энергии
        List<IMyLightingBlock> Light_all_list = new List<IMyLightingBlock>();                           //Всё освещение
        List<IMyTerminalBlock> Garage_all_list = new List<IMyTerminalBlock>();                          //Гаражи
        List<IMyTerminalBlock> Coсpit_all_list = new List<IMyTerminalBlock>();                          //Кокпиты
        List<IMyTerminalBlock> Welder_all_list = new List<IMyTerminalBlock>();                          //Сварщики
        List<IMyTerminalBlock> Sartir_all_list = new List<IMyTerminalBlock>();                          //Толчки
        List<IMyTerminalBlock> LCD_Ass_Ref_list = new List<IMyTerminalBlock>();                         //LCD состояния рефок и сборщиков
        List<IMyTerminalBlock> Gas_Gen_all_list = new List<IMyTerminalBlock>();                         //Электролизёры/керос/битум
        List<IMyTerminalBlock> Battery_all_list = new List<IMyTerminalBlock>();                         //АКБ
        List<IMyTerminalBlock> X_CargoContainers = new List<IMyTerminalBlock>();                        //X-Контейнеры
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
        List<IMyTerminalBlock> Missile_Turret_list = new List<IMyTerminalBlock>();                      //Ракетницы
        List<IMyTerminalBlock> Designator_all_list = new List<IMyTerminalBlock>();                      //Десигнаторы
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
        List<IMyTerminalBlock> Missile_Turret_all_list = new List<IMyTerminalBlock>();                  //Ракетницы/десигнаторы
        List<IMyTerminalBlock> CargoContainers_all_list = new List<IMyTerminalBlock>();                 //Абсолютно все контейнеры/хранилища
        List<IMyTerminalBlock> Small_CargoContainers_list = new List<IMyTerminalBlock>();               //Малые контейнеры

        List<IMyCargoContainer> Cargo_with_out_ice = new List<IMyCargoContainer>();                     //Все конты, не содержащие лёд
        List<IMyCargoContainer> Cargo_with_out_ore = new List<IMyCargoContainer>();                     //Все конты, не содержащие руду
        List<IMyCargoContainer> Cargo_with_out_ammo = new List<IMyCargoContainer>();                    //Все конты, не содержащие боеприпасов
        List<IMyCargoContainer> Cargo_with_out_ingot = new List<IMyCargoContainer>();                   //Все конты, не содержащие слитки
        List<IMyCargoContainer> Cargo_with_out_bitum = new List<IMyCargoContainer>();                   //Все конты, не содержащие битум
        List<IMyCargoContainer> Cargo_with_out_buttle = new List<IMyCargoContainer>();                  //Все конты, не содержащие баллонов
        List<IMyCargoContainer> Cargo_with_out_components = new List<IMyCargoContainer>();              //Все конты, не содержащие компоненты
        List<IMyCargoContainer> Cargo_with_out_instruments = new List<IMyCargoContainer>();             //Все конты, не содержащие инструменты

        List<IMyCargoContainer> Cargo_with_ice = new List<IMyCargoContainer>();                         //Все конты, содержащие лёд
        List<IMyCargoContainer> Cargo_with_ore = new List<IMyCargoContainer>();                         //Все конты, содержащие руду
        List<IMyCargoContainer> Cargo_with_ammo = new List<IMyCargoContainer>();                        //Все конты, содержащие боеприпасы
        List<IMyCargoContainer> Cargo_with_ingot = new List<IMyCargoContainer>();                       //Все конты, содержащие слитки
        List<IMyCargoContainer> Cargo_with_bitum = new List<IMyCargoContainer>();                       //Все конты, содержащие битум
        List<IMyCargoContainer> Cargo_with_buttle = new List<IMyCargoContainer>();                      //Все конты, содержащие баллоны
        List<IMyCargoContainer> Cargo_with_components = new List<IMyCargoContainer>();                  //Все конты, содержащие компоненты
        List<IMyCargoContainer> Cargo_with_instruments = new List<IMyCargoContainer>();                 //Все конты, содержащие инструменты
        
        bool filterThis(IMyTerminalBlock block)                         //Фильтр поиска блоков в одном гриде
        {
            return block.CubeGrid == Me.CubeGrid;
        }
        public Program()
        {
            //Me.Enabled = true;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(LCD_all_list, filterThis);
            LCD_main = LCD_all_list.ConvertAll(x => (IMyTextPanel)x).Find(x => x.CustomName.Contains("LCD_main"));                                      //LCD
            LCD_Ass_Ref_list = LCD_all_list.FindAll(x => x.CustomName.Contains("LCD_Очистка/сборка"));                                                  //LCD

            //Ошибки
            if (LCD_main == null) { error_list.Add("Нет LCD с именем <<LCD_main>>!"); }
            if (LCD_Ass_Ref_list.Count == 0) { error_list.Add("Нет LCD с именем <<LCD_Очистка/сборка>>!"); }
        }

        public void Main(string args)
        {
            load++;
            zamedlenie++;
            if ((LCD_main == null) || (LCD_Ass_Ref_list.Count == 0))
            { Echo("Инициализация не успешна,\nвыход из main... " + "\n" + String.Join("\n", error_list) + "\n" + InsertActivityChar(load) + "\n"); return; }
            else { Echo("Инициализация успешна  " + InsertActivityChar(load) + "\n" + zamedlenie % update_time + "\n" + String.Join("\n", error_list)); }

            //Преднастройка
            if (Pred_nastr == false)
            {
                List<IMyTextPanel> LCD_panel = LCD_Ass_Ref_list.ConvertAll(x => (IMyTextPanel)x);
                SetLcdConfig(LCD_main, ContentType.TEXT_AND_IMAGE, 0.78f, 0.0f, Color.Black, 0, 100, 0, "ОТЛАДКА");
                foreach (var i in LCD_panel)
                {
                    SetLcdConfig(i, ContentType.TEXT_AND_IMAGE, 0.78f, 0.0f, Color.Black, 85, 0, 60, "Monospace");        //Экраны состояния рефок/сборщиков
                }
                Pred_nastr = true;
            }
            //Переинициализация
            if (pereinit == false)
            {
                GridTerminalSystem.GetBlocksOfType<IMyDoor>(door_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType(TerminalBlocks_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(LCD_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Coсpit_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyGasTank>(Gas_Tank_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyRefinery>(Base_ref_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(Light_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(Power_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyProjector>(Projector_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(Battery_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(Gas_Gen_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(SolarPanel_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(Welder_base_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(Connector_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyAssembler>(Base_Assembler_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyUpgradeModule>(Upgrade_mudule_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(CargoContainers_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyLargeGatlingTurret>(Gatling_Turret_all_list, filterThis);
                GridTerminalSystem.GetBlocksOfType<IMyLargeMissileTurret>(Missile_Turret_all_list, filterThis);

                Garage_all_list = Projector_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("Garage"));                                          //Гаражи
                Oil_all_list = Gas_Gen_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("OilRefinery"));                                          //Нефтезаводы
                Bad_all_list = Coсpit_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockBed"));                                         //Кровати 
                Oil_Tank_all_list = Gas_Tank_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("OilTank"));                                        //Нефтебаки
                Generator_all_list = Power_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("OilEngine"));                                        //Дизель генераторы
                Base_ref_list = Base_ref_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeRefinery"));                                      //Рефки
                Couch_all_list = Coсpit_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockCouch"));                                     //Диваны
                Sartir_all_list = Coсpit_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockToilet"));                                   //Сартиры
                Keros_all_list = Gas_Gen_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("KeroseneGenerator"));                                  //Экстракторы кероса
                Base_projector_list = Light_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("ClassicSpotBar"));                                  //Прожекторы
                Base_Battery_list = Battery_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("Large3x3x2Base"));                                  //Батареи базы
                DeskCorner_all_list = Coсpit_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockDesk"));                                 //Стол
                Keros_Tank_all_list = Gas_Tank_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("KeroseneTank"));                                 //Керосиновые баки
                Missile_Turret_list = Missile_Turret_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("Missile"));                                //Ракетницы
                Welder_all_list = Welder_base_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeShipWelder"));                               //Сварщики
                Electrolize_all_list = Gas_Gen_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("OxygenGenerator"));                              //Газ генераторы
                Hydrogen_Tank_all_list = Gas_Tank_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("HydrogenTank"));                              //Водородные баки
                Designator_all_list = Missile_Turret_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("Designator"));                             //Десигнаторы
                Nanite_base_all_list = Welder_base_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("RepairSystem"));                             //Наниты
                Wind_Turbine_all_list = TerminalBlocks_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("WindTurbine"));                          //Ветряки
                Base_Assembler_list = Base_Assembler_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeAssembler"));                         //Сборщики
                Basic_Assembler_list = Base_Assembler_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("BasicAssembler"));                        //Инструментарии
                X_CargoContainers = CargoContainers_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("X-LargeContainer"));                        //Огромные конты
                WeaponRack_all_list = CargoContainers_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockWeaponRack"));                  //Оружейные шкафчики 
                Small_CargoContainers_list = CargoContainers_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("SmallContainer"));                 //Малые конты
                WeaponRack_all_list.AddRange(CargoContainers_all_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("LargeBlockLockerRoomCorner")));    //+Шкафчики

                bat = wind_turb = upgr_mod = garage = gat_tur = miss_tur = couch = desk_corn = bad = weapon_rack = base_ass = basic_ass = refin = electrol = 
                oil_ref = keros_ref = hyd_tank = oil_tank = keros_tank = connectors = disel_gen = solar_pan = welder = nanite = cargo = doors = small_cargo = lcd = sartir = design = 0;

                //Сорт                                           
                Cargo_with_out_bitum = CargoContainers_all_list.ConvertAll(x => (IMyCargoContainer)x); Cargo_with_out_bitum.RemoveAll(x => x.CustomName.Contains("Битум"));                         //Все конты, не содержащие битум
                Cargo_with_out_ice = CargoContainers_all_list.ConvertAll(x => (IMyCargoContainer)x); Cargo_with_out_ice.RemoveAll(x => x.CustomName.Contains("Лед"));                               //Все конты, не содержащие лёд
                Cargo_with_out_ore = CargoContainers_all_list.ConvertAll(x => (IMyCargoContainer)x); Cargo_with_out_ore.RemoveAll(x => x.CustomName.Contains("Руда"));                              //Все конты, не содержащие руду
                Cargo_with_out_ingot = CargoContainers_all_list.ConvertAll(x => (IMyCargoContainer)x); Cargo_with_out_ingot.RemoveAll(x => x.CustomName.Contains("Слитки"));                        //Все конты, не содержащие слитки
                Cargo_with_out_components = CargoContainers_all_list.ConvertAll(x => (IMyCargoContainer)x); Cargo_with_out_components.RemoveAll(x => x.CustomName.Contains("Компоненты"));          //Все конты, не содержащие компоненты
                Cargo_with_out_instruments = CargoContainers_all_list.ConvertAll(x => (IMyCargoContainer)x); Cargo_with_out_instruments.RemoveAll(x => x.CustomName.Contains("Инструменты"));       //Все конты, не содержащие инструменты
                Cargo_with_out_ammo = CargoContainers_all_list.ConvertAll(x => (IMyCargoContainer)x); Cargo_with_out_ammo.RemoveAll(x => x.CustomName.Contains("Боезапас"));                        //Все конты, не содержащие боеприпасов
                Cargo_with_out_buttle = CargoContainers_all_list.ConvertAll(x => (IMyCargoContainer)x); Cargo_with_out_buttle.RemoveAll(x => x.CustomName.Contains("Баллоны"));                     //Все конты, не содержащие баллонов
                
                Cargo_with_bitum = CargoContainers_all_list.FindAll(x => x.CustomName.Contains("Битум")).ConvertAll(x => (IMyCargoContainer)x);                                                     //Все конты, содержащие битум
                Cargo_with_ice = CargoContainers_all_list.FindAll(x => x.CustomName.Contains("Лед")).ConvertAll(x => (IMyCargoContainer)x);                                                         //Все конты, содержащие лёд
                Cargo_with_ore = CargoContainers_all_list.FindAll(x => x.CustomName.Contains("Руда")).ConvertAll(x => (IMyCargoContainer)x);                                                        //Все конты, содержащие руду
                Cargo_with_ingot = CargoContainers_all_list.FindAll(x => x.CustomName.Contains("Слитки")).ConvertAll(x => (IMyCargoContainer)x);                                                    //Все конты, содержащие слитки
                Cargo_with_components = CargoContainers_all_list.FindAll(x => x.CustomName.Contains("Компоненты")).ConvertAll(x => (IMyCargoContainer)x);                                           //Все конты, содержащие компоненты
                Cargo_with_instruments = CargoContainers_all_list.FindAll(x => x.CustomName.Contains("Инструменты")).ConvertAll(x => (IMyCargoContainer)x);                                         //Все конты, содержащие инструменты
                Cargo_with_ammo = CargoContainers_all_list.FindAll(x => x.CustomName.Contains("Боезапас")).ConvertAll(x => (IMyCargoContainer)x);                                                   //Все конты, содержащие боеприпасы
                Cargo_with_buttle = CargoContainers_all_list.FindAll(x => x.CustomName.Contains("Баллоны")).ConvertAll(x => (IMyCargoContainer)x);                                                  //Все конты, содержащие баллоны
                pereinit = true;
            }

            //Отключаем рудоломки и ледоломки
            {
                GridTerminalSystem.GetBlocksOfType(All_ref_list);
                IceCrusher_list = All_ref_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("IceCrusher"));
                OreCrusher_list = All_ref_list.FindAll(x => x.BlockDefinition.SubtypeId.Contains("OreCrusher"));

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
                //Протухание инициализации
                if ((zamedlenie % update_name_time) == 0) { pereinit = false; }
                if ((zamedlenie % update_properties) == 0)
                {
                    CustomData();
                    lcd = Rename(LCD_all_list, "LCD", false, false, false, lcd, true);                                                           //LCD
                    bat = Rename(Base_Battery_list, "АКБ", false, false, false, bat, true);                                                      //АКБ
                    doors = Rename(door_all_list, "Дверь", false, false, false, doors, true);                                                    //Двери
                    garage = Rename(Garage_all_list, "Гараж", true, false, false, garage, true);                                                 //Гаражи
                    refin = Rename(Base_ref_list, "Очиститель", true, false, true, refin, true);                                                 //Рефки
                    cargo = Rename(X_CargoContainers, "Конт", false, false, true, cargo, false);                                                 //Большие конты
                    sartir = Rename(Sartir_all_list, "Сартир", false, false, false, sartir, false);                                              //Сартиры
                    welder = Rename(Welder_all_list, "Сварщик", false, false, false, welder, true);                                              //Сварщики
                    bad = Rename(Bad_all_list, "Какая-то кровать", false, false, false, bad, false);                                             //Кровати
                    oil_ref = Rename(Oil_all_list, "Нефтезавод", false, false, false, oil_ref, true);                                            //Нефтезаводы
                    nanite = Rename(Nanite_base_all_list, "Нанитка", true, false, false, nanite, true);                                          //Наниты
                    design = Rename(Designator_all_list, "Десигнатор", false, true, false, design, true);                                        //Десигнаторы
                    base_ass = Rename(Base_Assembler_list, "Сборщик", true, false, false, base_ass, true);                                       //Сборщики
                    gat_tur = Rename(Gatling_Turret_all_list, "Гатлинг", false, true, false, gat_tur, true);                                     //Турели гатлинга
                    miss_tur = Rename(Missile_Turret_list, "Ракетница", false, true, false, miss_tur, true);                                     //Ракетницы
                    wind_turb = Rename(Wind_Turbine_all_list, "Ветряк", false, false, false, wind_turb, true);                                   //Ветряные мельницы
                    couch = Rename(Couch_all_list, "Какой-то блять диван", false, false, false, couch, false);                                   //Диваны
                    oil_tank = Rename(Oil_Tank_all_list, "Нефтяной бак", false, false, false, oil_tank, true);                                   //Нефте баки
                    connectors = Rename(Connector_all_list, "Коннектор", false, false, false, connectors, true);                                 //Коннекторы
                    electrol = Rename(Electrolize_all_list, "Электоролизёр", false, false, false, electrol, true);                               //Электролизёры
                    basic_ass = Rename(Basic_Assembler_list, "Инструментарий", true, false, false, basic_ass, true);                             //Инструментарии
                    keros_ref = Rename(Keros_all_list, "Экстрактор керосина", false, false, false, keros_ref, true);                             //Керос баки
                    small_cargo = Rename(Small_CargoContainers_list, "Конт", false, false, true, small_cargo, false);                            //Малые конты
                    hyd_tank = Rename(Hydrogen_Tank_all_list, "Водородный бак", false, false, false, hyd_tank, true);                            //Водородные баки
                    disel_gen = Rename(Generator_all_list, "Дизель генератор", false, false, false, disel_gen, true);                            //Дизель генераторы
                    solar_pan = Rename(SolarPanel_all_list, "Солнечная панель", false, false, false, solar_pan, true);                           //Солнечные панели
                    keros_tank = Rename(Keros_Tank_all_list, "Керосиновый бак", false, false, false, keros_tank, true);                          //Керос баки
                    upgr_mod = Rename(Upgrade_mudule_all_list, "Модуль улучшения", false, false, false, upgr_mod, true);                         //Модули апгрейда
                    weapon_rack = Rename(WeaponRack_all_list, "Оружейный шкафчик", false, false, false, weapon_rack, true);                      //Оружейные шкафчики
                    desk_corn = Rename(DeskCorner_all_list, "Какой-то угловой стол", false, false, false, desk_corn, false);                     //Столы
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
            //Включаем рефки и инструментарии
            {
                if (args == "Ref_On") { ref_on_kd = true; }
                if (args == "Ref_Off") { ref_on_kd = false; }

                if ((zamedlenie % 40) == 0)
                {
                    List<IMyAssembler> Instrum = Basic_Assembler_list.ConvertAll(x => (IMyAssembler)x);
                    foreach (var i in Instrum)
                    {
                        i.Enabled = true;
                    }
                }
                if (ref_on_kd)
                {
                    auto_on_ref = "Вкл";
                    if ((zamedlenie % 5) == 0)
                    {
                        foreach (var cargo in Cargo_with_ore)
                        {
                            if (cargo.CustomName.Contains("[Руда] 1"))
                            {
                                List<MyInventoryItem> Items = new List<MyInventoryItem>();
                                cargo.GetInventory(0).GetItems(Items);

                                if (cargo.GetInventory(0).ItemCount > 0)
                                {
                                    if (Items[0].Type.TypeId.Contains("Ore"))
                                    {
                                        if (Base_ref_list.Count > 0)
                                        {
                                            foreach (var Ref in Base_ref_list)
                                            {
                                                if (!Ref.GetInventory(0).IsFull)
                                                {
                                                    cargo.GetInventory(0).TransferItemTo(Ref.GetInventory(0), Items[0]);
                                                    //break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if ((zamedlenie % 20) == 0)
                    {
                        List<IMyRefinery> Refins = Base_ref_list.ConvertAll(x => (IMyRefinery)x);
                        int ref_count = 0;

                        foreach (var Ref in Refins)
                        {
                            if (Ref.GetInventory(0).ItemCount > 0)
                            {
                                Ref.Enabled = true;
                            }
                            else
                            {
                                ref_count++;
                                Ref.Enabled = false;
                            }
                        }
                        if (ref_count == Base_ref_list.Count)
                        {
                            ref_on_kd = false;
                        }
                    }    
                }
                else
                {
                    auto_on_ref = "Выкл";
                }
            }

            //Сортирвка
            if ((zamedlenie % update_time) == 0)
            {
                Sorting(Cargo_with_out_ore, Cargo_with_ore, "Ore");
                Sorting(Cargo_with_out_ingot, Cargo_with_ingot, "Ingot");
                Sorting(Cargo_with_out_components, Cargo_with_components, "Component");
                Sorting(Cargo_with_out_instruments, Cargo_with_instruments, "GunObject");
                Sorting(Cargo_with_out_ammo, Cargo_with_ammo, "Ammo");
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
            //Вся хуйня - на дисплеи
            List<IMyTextPanel> LCD_pan = LCD_Ass_Ref_list.ConvertAll(x => (IMyTextPanel)x);
            foreach (var i in LCD_pan)
            {
                i.WriteText(ref_stat + "\n" + ass_stat, false);
                i.WriteText("\n" + " Автовключение рефок:  " + "[" + auto_on_ref + "]", true);
            }
            LCD_main.WriteText("Отключение света днём: " + light_day_off + "\n" + "Обновление наименований через " + (update_name_time - zamedlenie % update_name_time) + "\n" +
                "Обновление настроек через: " + (update_properties - zamedlenie % update_properties) + "\n" + "Сортировка через: " + (update_time - zamedlenie % update_time), false);
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
        //Переименовывание + настройка
        public int Rename (List<IMyTerminalBlock> list, string name, bool ShowInTerminal, bool ShowInToolbarConfig, bool ShowInInventory, int list_count, bool use_id)
        {
            foreach (var i in list)
            {
                //Отображенеи в терминале
                if (ShowInTerminal) { if (i.ShowInTerminal == false) { i.ShowInTerminal = true; } } else { if (i.ShowInTerminal == true) { i.ShowInTerminal = false; } }
                //Отображение в инвентаре
                if (ShowInInventory) { if (i.ShowInInventory == false) { i.ShowInInventory = true; } } else { if (i.ShowInInventory == true) { i.ShowInInventory = false; } }
                //Отображение в панели инструментов
                if (ShowInToolbarConfig) { if (i.ShowInToolbarConfig == false) { i.ShowInToolbarConfig = true; } } else { if (i.ShowInToolbarConfig == true) { i.ShowInToolbarConfig = false; } }
            }
            if (list.Count != list_count)
            {
                foreach (var i in list)
                {
                    long ID = 0;
                    if (use_id)
                    {
                        ID = i.OwnerId;
                    }
                    //Ренейм
                    if (i.CustomName.Contains(SetBlocksName(name + prefix, ID)) == false)
                    {
                        i.CustomName = SetBlocksName(name + prefix, ID);
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
            string status = "Не варит";
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

            if (ID == 0)
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
        void Sorting(List<IMyCargoContainer> cargo_from, List<IMyCargoContainer> cargo_to, string typeID)
        {
            foreach (var cargo in cargo_from)
            {
                List<MyInventoryItem> Items = new List<MyInventoryItem>();
                cargo.GetInventory(0).GetItems(Items);

                foreach (var item in Items)
                {
                    //Битум
                    if (item.Type.SubtypeId == "FrozenOil")
                    {
                        if (!Cargo_with_bitum.Contains(cargo))
                        {
                            foreach (var carg in Cargo_with_bitum)
                            {
                                if (!carg.GetInventory(0).IsFull)
                                {
                                    cargo.GetInventory(0).TransferItemTo(carg.GetInventory(0), item);
                                    break;
                                }
                            }
                        }
                    }
                    //Лёд
                    if (item.Type.SubtypeId == "Ice")
                    {
                        if (!Cargo_with_ice.Contains(cargo))
                        {
                            foreach (var carg in Cargo_with_ice)
                            {
                                if (!carg.GetInventory(0).IsFull)
                                {
                                    cargo.GetInventory(0).TransferItemTo(carg.GetInventory(0), item);
                                    break;
                                }
                            }
                        }    
                    }
                    //Баллоны
                    if (item.Type.TypeId.Contains("ContainerObject"))
                    {
                        if(!Cargo_with_buttle.Contains(cargo))
                        {
                            foreach (var carg in Cargo_with_buttle)
                            {
                                if (!carg.GetInventory(0).IsFull)
                                {
                                    cargo.GetInventory(0).TransferItemTo(carg.GetInventory(0), item);
                                    break;
                                }
                            }
                        }
                    }
                    //Всё остальное
                    if (item.Type.TypeId.Contains(typeID))
                    {
                        if (!item.Type.SubtypeId.Contains("FrozenOil"))
                        {
                            if (!item.Type.SubtypeId.Contains("Ice"))
                            {
                                if (!item.Type.SubtypeId.Contains("Bottle"))
                                {
                                    foreach (var carg in cargo_to)
                                    {
                                        List<MyInventoryItem> Items_to = new List<MyInventoryItem>();
                                        carg.GetInventory(0).GetItems(Items_to);
                                        foreach (var it in Items_to)
                                        {
                                            if (it.Type.SubtypeId == item.Type.SubtypeId)
                                            {
                                                if (!carg.GetInventory(0).IsFull)
                                                {
                                                    cargo.GetInventory(0).TransferItemTo(carg.GetInventory(0), item);
                                                }
                                            }
                                        }
                                    }   
                                    foreach (var carg in cargo_to)
                                    {
                                        if (!carg.GetInventory(0).IsFull)
                                        {
                                            cargo.GetInventory(0).TransferItemTo(carg.GetInventory(0), item);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //Сборщики
            if (Base_Assembler_all_list.Count > 0)
            {
                foreach (IMyAssembler Assembler in Base_Assembler_all_list)
                {
                    if (Assembler.GetInventory(1).ItemCount > 0)
                    {
                        List<MyInventoryItem> Items_ass;
                        Items_ass = new List<MyInventoryItem>();

                        Assembler.GetInventory(1).GetItems(Items_ass);

                        foreach (MyInventoryItem Item in Items_ass)
                        {
                            foreach (var carg in Cargo_with_components)
                            {
                                List<MyInventoryItem> Items_to = new List<MyInventoryItem>();
                                carg.GetInventory(0).GetItems(Items_to);
                                foreach (var it in Items_to)
                                {
                                    if (it.Type.SubtypeId == Item.Type.SubtypeId)
                                    {
                                        if (!carg.GetInventory(0).IsFull)
                                        {
                                            Assembler.GetInventory(1).TransferItemTo(carg.GetInventory(0), Item);
                                        }
                                    }
                                }
                            }
                            foreach (var carg in Cargo_with_components)
                            {
                                if (!carg.GetInventory(0).IsFull)
                                {
                                    Assembler.GetInventory(1).TransferItemTo(carg.GetInventory(0), Item);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            //Рефки
            if (Base_ref_all_list.Count > 0)
            {
                foreach (IMyRefinery Refinery in Base_ref_all_list)
                {
                    if (Refinery.GetInventory(1).ItemCount > 0)
                    {
                        List<MyInventoryItem> Items_ref;
                        Items_ref = new List<MyInventoryItem>();

                        Refinery.GetInventory(1).GetItems(Items_ref);

                        foreach (MyInventoryItem Item in Items_ref)
                        {
                            foreach (var carg in Cargo_with_ingot)
                            {
                                List<MyInventoryItem> Items_to = new List<MyInventoryItem>();
                                carg.GetInventory(0).GetItems(Items_to);
                                foreach (var it in Items_to)
                                {
                                    if (it.Type.SubtypeId == Item.Type.SubtypeId)
                                    {
                                        if (!carg.GetInventory(0).IsFull)
                                        {
                                            Refinery.GetInventory(1).TransferItemTo(carg.GetInventory(0), Item);
                                        }
                                    }
                                }
                            }
                            foreach (var carg in Cargo_with_ingot)
                            {
                                if (!carg.GetInventory(0).IsFull)
                                {
                                    Refinery.GetInventory(1).TransferItemTo(carg.GetInventory(0), Item);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
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
