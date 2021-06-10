using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using System.Linq;

namespace Script1
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        /*
         * R e a d m e
         * -----------
         * 
         * In this file you can include any instructions or other comments you want to have injected onto the 
         * top of your final script. You can safely delete this file if you do not want any such comments.
         */

        public const double MaxAcceleration = 3.0;                      // Максимальное ускорение, развиваемое при горизонтальном полете

        public const double StrafeMaxSpeed = 2.0;                       // Максимальная скорость движения боком (м/с)
        public const double ControlRotationSensitivity = 0.4;           // Чувствительность при повороте вокруг вертикальной оси
        public const double ControlAccelerationSensitivity = 20.0;      // Чувствительность к кнопкам ускорения (веперд/назад)

        public const double DirectionErrorCorrectionCoefficient = 0.5;  // Корректирование ошибки направления движения при повороте корабля влево-вправо во время движения

        public const double BlinkFreq = 500.0;                          // Частота мигания элементов интерфейса, в миллисекундах
        public const bool HUDOnCockpit = true;                          // рисовать индикаторы на кокпит
        public const UpdateFrequency DefaultFreq = UpdateFrequency.Update1; // Частота обновления
        IMyCockpit Ŷ;
        List<IMyGyro> ŵ;
        IMyTextSurface ų;
        List<IMyCargoContainer> Ų;
        List<IMyThrust> ű;
        float Ű;
        int ů;
        List<IMyBatteryBlock> Ů;
        List<IMyConveyorSorter> ŭ;
        List<IMyConveyorSorter> Ŭ;
        Ż[] ū;
        long Ū;
        const long ũ = 1000000;
        double Ũ;
        Vector3D ŧ;
        double ŗ;
        double Ŧ;
        bool ť;
        Ĥ ø;
        TimeSpan ŷ;
        bool Ɣ;
        int ƕ;
        int Ɠ;
        int ƒ;
        MySprite Ƒ;
        MySprite Ɛ;
        public enum Ə
        {
            Ǝ = 0, ƍ = 1, ƌ = 2, Ƌ = 3, Ɗ = 4, Ɖ = 5, ƈ = 6, Ƈ = 7, Ɔ = 8, ƅ = 9,
            Ƅ = 10, ƃ = 11, Ƃ = 12, Ɓ = 13, ƀ = 14, ſ = 15, ž = 16, Ž = 17, ż
        }
        class Ż
        {
            public string ź;
            public string Ź;
            public string Ÿ;
            public bool Ť;
            public bool ţ;
            public MyInventoryItemFilter Ļ;
            public Ż(string ŏ, string Ŏ, string ō, bool Ō, bool ŋ)
            {
                Ÿ = ō;
                ź = ŏ;
                Ź = Ŏ;
                Ť = Ō;
                ţ = ŋ;
                Ļ = new MyInventoryItemFilter(Ź, ţ);
            }
        }
        bool Ŋ;
        double ŉ;
        public Program()
        {
            ƕ = 0;
            Ɠ = 1;
            ƒ = 2;
            Ɣ = false;
            ŷ = TimeSpan.Zero;
            ť = false;
            Ŧ = 0;
            Ŋ = false;
            Ũ = 0;
            ŉ = 0;
            Ū = 0;
            ŗ = 0;
            Runtime.UpdateFrequency = UpdateFrequency.None;
            ø = new Ĥ();
            ų = Me.GetSurface(0);
            if (ų != null)
            {
                ų.Font = "Monospace";
                ų.FontSize = 0.8f;
                ų.FontColor = new Color(100, 200, 100, 255);
                ų.WriteText("");
                ų.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            }
            Ų = new List<IMyCargoContainer>();
            Ů = new List<IMyBatteryBlock>();
            ŵ = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(ŵ, ƹ);
            if (ŵ.Count == 0)
            {
                ų.WriteText("Гироскопы не найдены,\nуправление будет невозможно\n", true);
            }
            ƚ();
            ű = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(ű, Ƹ);
            GridTerminalSystem.GetBlocksOfType(Ů, ƹ);
            GridTerminalSystem.GetBlocksOfType(Ų, ƹ);
            if (!ƴ(out Ŷ)) return;
            ŧ = Ŷ.WorldMatrix.Forward;
            if (Ŷ.BlockDefinition.SubtypeId.StartsWith("SmallBlockCockpit"))
            {
                ƕ = 1;
                Ɠ = 0;
                ƒ = 2;
            }
            if (ŭ == null)
            {
                ŭ = new List<IMyConveyorSorter>();
                IMyBlockGroup ň = GridTerminalSystem.GetBlockGroupWithName("Waste");
                if (ň != null) ň.GetBlocksOfType<IMyConveyorSorter>(ŭ, ƹ);
                else
                {
                    Echo("Waste");
                    //ų.WriteText("Не найдена группа\nсортировщиков с названием:\nWaste\n", true);
                }
            }
            if (Ŭ == null)
            {
                Ŭ = new List<IMyConveyorSorter>();
                IMyBlockGroup ň = GridTerminalSystem.GetBlockGroupWithName("Store");
                if (ň != null) ň.GetBlocksOfType<IMyConveyorSorter>(Ŭ, ƹ);
                else
                {
                    Echo("Store");
                    //ų.WriteText("Не найдена группа\nсортировщиков с названием:\nStore\n", true);
                }
            }
            for (int ü = 0; ü < Ŷ.SurfaceCount; ü++)
            {
                IMyTextSurface Ň = Ŷ.GetSurface(ü);
                Ň.ContentType = ContentType.SCRIPT;
                Ň.Script = "";
                Ň.ScriptBackgroundColor = new Color(0, 0, 0, 0);
                Ň.ScriptForegroundColor = new Color(255, 255, 0);
            }
            Runtime.UpdateFrequency = DefaultFreq;
            ū = new Ż[(int)Ə.ż];
            ū[0] = new Ż("Камни", "MyObjectBuilder_Ore/Stone", "stone", false, false);
            ū[1] = new Ż("Лед", "MyObjectBuilder_Ore/Ice", "ice", false, false);
            ū[2] = new Ż("Кобальт", "MyObjectBuilder_Ore/Cobalt", "cobalt", false, false);
            ū[3] = new Ż("Золото", "MyObjectBuilder_Ore/Gold", "gold", false, false);
            ū[4] = new Ż("Железо", "MyObjectBuilder_Ore/Iron", "iron", false, false);
            ū[5] = new Ż("Магний", "MyObjectBuilder_Ore/Magnesium", "magnesium", false, false);
            ū[6] = new Ż("Никель", "MyObjectBuilder_Ore/Nickel", "nickel", false, false);
            ū[7] = new Ż("Органика", "MyObjectBuilder_Ore/Organic", "organic", false, false);
            ū[8] = new Ż("Платина", "MyObjectBuilder_Ore/Platinum", "platinum", false, false);
            ū[9] = new Ż("Металлолом", "MyObjectBuilder_Ore/Scrap", "scrap", false, false);
            ū[10] = new Ż("Кремний", "MyObjectBuilder_Ore/Silicon", "silicon", false, false);
            ū[11] = new Ż("Серебро", "MyObjectBuilder_Ore/Silver", "silver", false, false);
            ū[12] = new Ż("Уран", "MyObjectBuilder_Ore/Uranium", "uranium", false, false);
            ū[13] = new Ż("Латерит", "MyObjectBuilder_Ore/Lateryt", "lateryt", false, false);
            ū[14] = new Ż("Угольный порошок", "MyObjectBuilder_Ore/CoalPowder", "coalpowder", false, false);
            ū[15] = new Ż("Малахит", "MyObjectBuilder_Ore/Malachit", "malachit", false, false);
            ū[16] = new Ż("Нефтеносный сланец", "MyObjectBuilder_Ore/FrozenOil", "frozenoil", false, false);
            ū[17] = new Ż("Палладий", "MyObjectBuilder_Ore/Palladium", "palladium", false, false);
            for (int ü = 2; ü < ū.Count(); ü++)
            {
                ū[ü].Ť = true;
            }
            Ơ(MyConveyorSorterMode.Whitelist);
            Ū = 0;
            foreach (IMyCargoContainer ņ in Ų)
            {
                Ū += ņ.GetInventory().MaxVolume.RawValue;
            }
            Ƒ = new MySprite(SpriteType.TEXTURE, "Danger", new Vector2(260, 250), new Vector2(100f, 100f), new Color(255, 255, 255, 255), "Monospace", TextAlignment.LEFT);
            Ɛ = new MySprite(SpriteType.TEXTURE, "IconEnergy", new Vector2(260, 250), new Vector2(100f, 100f), new Color(128, 255, 128, 255), "Monospace", TextAlignment.LEFT);
        }
        public void Main(string Ņ, UpdateType ń)
        {
            ǉ();
            ŗ = ŕ();
            if (Ņ == "+V")
            {
                Ũ += 1.0;
                Ũ = MathHelper.Clamp(Ũ, -110.0f, 110.0f);
            }
            else if (Ņ == "-V")
            {
                Ũ += -1.0;
                Ũ = MathHelper.Clamp(Ũ, -110.0f, 110.0f);
            }
            if (Ņ == "+V10")
            {
                Ũ += 10.0;
                Ũ = MathHelper.Clamp(Ũ, -110.0f, 110.0f);
            }
            else if (Ņ == "-V10")
            {
                Ũ += -10.0;
                Ũ = MathHelper.Clamp(Ũ, -110.0f, 110.0f);
            }
            if (Ņ == "+H")
            {
                if (ŗ > 4.0)
                {
                    ŗ += 1.0;
                }
                else
                {
                    ŗ += 0.1;
                }
                Ŕ(ŗ);
            }
            if (Ņ == "-H")
            {
                if (ŗ > 4.0)
                {
                    ŗ -= 1.0;
                }
                else
                {
                    ŗ -= 0.1;
                }
                Ŕ(ŗ);
            }
            if (Ņ == "auto_height")
            {
                ť = !ť;
                if (ť)
                {
                    Ŧ = ś();
                }
            }
            if (Ņ == "stop")
            {
                Ũ = 0.0;
            }
            if (Ņ == "ice")
            {
                ū[(int)Ə.ƍ].Ť = !ū[(int)Ə.ƍ].Ť;
                Ơ(MyConveyorSorterMode.Whitelist);
            }
            if (Ņ == "stone")
            {
                ū[(int)Ə.Ǝ].Ť = !ū[(int)Ə.Ǝ].Ť;
                Ơ(MyConveyorSorterMode.Whitelist);
            }
            ŉ = ŉ * 0.99 + Runtime.LastRunTimeMs * 0.01;
            Ń();
            if (ť && Ŧ > 0)
            {
                ř();
            }
            ƙ();
            ǈ();
        }
        void Ń()
        {
            Vector3D ł = Ŷ.GetShipVelocities().LinearVelocity;
            double Ł = ł.Length();
            Vector3D ŀ = Ŷ.GetNaturalGravity();
            Vector3D Ŀ = Vector3D.Normalize(-ŀ);
            Vector3D Á = Vector3D.Normalize(Vector3D.Cross(Ŷ.WorldMatrix.Forward, Ŀ));
            Vector3D ľ = Vector3D.Normalize(Vector3D.Cross(Ŀ, Á));
            Vector3D Ľ = Vector3D.Zero;
            if (Ŷ.MoveIndicator.X != 0)
            {
                ľ += Á * Ŷ.MoveIndicator.X * ControlRotationSensitivity;
                ŧ = Vector3D.Normalize(ľ);
            }
            else if (Ŷ.RollIndicator != 0)
            {
                Ľ = Á * Ŷ.RollIndicator * StrafeMaxSpeed;
            }
            if (Ŷ.MoveIndicator.Z != 0)
            {
                Ũ += -Ŷ.MoveIndicator.Z * ControlAccelerationSensitivity / 60.0;
                Ũ = MathHelper.Clamp(Ũ, -110.0f, 110.0f);
            }
            Vector3D ļ = ä.ã(ł, ľ) * DirectionErrorCorrectionCoefficient;
            Vector3D Ő = ł - ľ * Ũ + ļ - Ľ;
            double Œ = 0;
            Vector3D š = Vector3D.Zero;
            if (Ő != Vector3D.Zero)
            {
                š = Ő;
                Œ = š.Normalize();
            }
            Ő = š * Math.Min(Œ, MaxAcceleration);
            Vector3D Ţ = ŀ + Ő;
            Vector3D Š = Vector3D.Normalize(-Ţ);
            Vector3D ş = Vector3D.Normalize(Vector3D.Cross(ľ, Š));
            Vector3D Ş = Vector3D.Normalize(Vector3D.Cross(Š, ş));
            ƫ.ð(ŵ, Ŷ, Ş, Š);
        }
        void Ü(Vector3D ï, Vector3D ë, Vector3D í, double ŝ = 2.0)
        {
            Vector3D î = Ŷ.WorldMatrix.Forward;
            Vector3D Ç = Ŷ.WorldMatrix.Up;
            Vector3D Á = Ŷ.WorldMatrix.Right;
            Vector3D ì = -í;
            double ê = ä.m(ref î, ref ï, ref ë, ref í);
            MatrixD é = MatrixD.CreateFromAxisAngle(ë, -ê);
            î = Vector3D.TransformNormal(î, é);
            î = Vector3D.Normalize(Vector3D.ProjectOnPlane(ref î, ref í));
            î = Vector3D.ProjectOnPlane(ref î, ref í);
            double è = ä.m(ref î, ref ï, ref í, ref ë);
            MatrixD ç = MatrixD.CreateFromAxisAngle(í, -è);
            MatrixD æ = é * ç;
            Ç = Vector3D.TransformNormal(Ç, æ);
            double å = ä.m(ref Ç, ref ë, ref ï, ref ì);
            foreach (IMyGyro Ŝ in ŵ)
            {
                Ŝ.GyroOverride = true;
                Ŝ.Yaw = (float)(ê * ŝ);
                Ŝ.Pitch = (float)(è * ŝ);
                Ŝ.Roll = (float)(å * ŝ);
            }
        }
        double ś()
        {
            Vector3D Ś;
            if (Ŷ.TryGetPlanetPosition(out Ś))
            {
                Echo(Vector3D.Distance(Ś, ű[0].GetPosition()).ToString());
                return Vector3D.Distance(Ś, ű[0].GetPosition());
            }
            else return 0;
        }
        void ř()
        {
            double Ř = ś();
            if (Ř > 0)
            {
                double ŗ = Ɩ();
                double Ŗ = ŗ + (Ŧ - Ř);
                Ŕ(Ŗ);
            }
        }
        double ŕ()
        {
            try
            {
                if (ű.Count > 0)
                {
                    Echo("[" + ű[0].GetValue<float>("height_target_min").ToString() + "]");
                    return ű[0].GetValue<float>("height_target_min");
                }
                else return 0;
            }
            catch { }
            return 0;
        }
        void Ŕ(double ő)
        {
            try
            {
                foreach (IMyThrust œ in ű)
                {
                    Echo("[" + œ.GetValue<float>("height_target_min").ToString() + "]");
                    œ.SetValue<float>("height_target_min", (float)ő);
                }
            }
            catch { };
        }
        double Ɩ()
        {
            double DistanceToSurface = 0;
            try
            {
                Ŷ.TryGetPlanetElevation(MyPlanetElevation.Surface, out DistanceToSurface);
                Echo(DistanceToSurface.ToString());
                return DistanceToSurface;
            }
            catch { }
            return 0;
        }
        void ǉ()
        {
            ŷ += Runtime.TimeSinceLastRun;
            if (ŷ.TotalMilliseconds > BlinkFreq)
            {
                ŷ = TimeSpan.Zero;
                Ɣ = !Ɣ;
            }
        }
        void ǈ()
        {
            ǂ();
            Ǉ();
            ƽ();
            Ŋ = !Ŋ;
        }
        void Ǉ()
        {
            if (Ŷ.SurfaceCount <= ƕ) return;
            IMyTextSurface Ň = Ŷ.GetSurface(ƕ);
            if (Ŋ) Ň.ContentType = ContentType.SCRIPT;
            else
            {
                Ň.ContentType = ContentType.NONE;
                using (MySpriteDrawFrame ù = Ň.DrawFrame())
                {
                    Color Ƽ = new Color(128, 255, 128, 255);
                    ø.ī(ù, String.Format("ТИК: {0}", Runtime.LastRunTimeMs.ToString("F03")), 140, 30, Ƽ, 0.5f);
                    ø.ī(ù, String.Format("СР: {0}", ŉ.ToString("F03")), 160, 50, Ƽ, 0.5f);
                    ø.ī(ù, String.Format("ИНСТ: {0}", Runtime.CurrentInstructionCount.ToString()), 160, 70, Ƽ, 0.5f);
                    float ǆ = (float)Math.PI * 0.5f;
                    Vector3D ǅ = Ŷ.GetShipVelocities().LinearVelocity;
                    if (ǅ != Vector3D.Zero)
                    {
                        float Ł = (float)ǅ.Normalize();
                        double Ǆ = Vector3D.Dot(Ŷ.WorldMatrix.Forward, ǅ);
                        ǆ = (float)(Ł * Math.Sign(Ǆ) / 110.0 * Math.PI + Math.PI) * 0.5f;
                    }
                    float ǃ = (float)(Ũ / 110.0 * Math.PI + Math.PI) * 0.5f;
                    Ć.Ĕ(ù, ø, 10, 50, 236, ǆ, ǃ, 0.5f, String.Format("СКР Т: {0}", Ŷ.GetShipSpeed().ToString("F02")), String.Format("СКР У: {0}", Ũ.ToString("F02")));
                    ø.ī(ù, "Высота:", 10, 30, Ƽ, 0.5f);
                    ø.ī(ù, ŗ.ToString("F01") + " м", 10, 50, Ƽ, 0.5f);
                    ø.ī(ù, Ɩ().ToString("F03") + " м", 10, 70, Ƽ, 0.5f);
                    if (ť && Ɣ)
                    {
                        ø.ī(ù, "АПВ ВКЛ", 10, 90, Ƽ);
                        Ƒ.Position = new Vector2(108, 60);
                        Ƒ.Size = new Vector2(40, 40);
                        ù.Add(Ƒ);
                    }
                    if (ū[(int)Ə.ƍ].Ť)
                    {
                        Ƒ.Position = new Vector2(10, 130);
                        Ƒ.Size = new Vector2(30, 30);
                        ù.Add(Ƒ);
                        ø.ī(ù, "Лед", 10, 145, Ƽ);
                        ø.ī(ù, "Сохр.", 10, 160, Ƽ);
                    }
                    if (ū[(int)Ə.Ǝ].Ť)
                    {
                        Ƒ.Position = new Vector2(216, 130);
                        Ƒ.Size = new Vector2(30, 30);
                        ù.Add(Ƒ);
                        ø.ī(ù, "Камень", 200, 145, Ƽ);
                        ø.ī(ù, "Сохр.", 205, 160, Ƽ);
                    }
                }
                Ň.ContentType = ContentType.SCRIPT;
            }
        }
        void ǂ()
        {
            if (Ŷ.SurfaceCount <= Ɠ) return;
            IMyTextSurface Ň = Ŷ.GetSurface(Ɠ);
            if (Ŋ) Ň.ContentType = ContentType.SCRIPT;
            else
            {
                Ň.ContentType = ContentType.NONE;
                using (MySpriteDrawFrame ù = Ň.DrawFrame())
                {
                    Color Ƽ = new Color(128, 255, 128, 255);
                    if (Ų.Count > 0)
                    {
                        long ǁ = 0;
                        long ǀ = 0;
                        foreach (IMyCargoContainer ņ in Ų)
                        {
                            ǁ += ņ.GetInventory().CurrentVolume.RawValue;
                            ǀ += ņ.GetInventory().CurrentMass.RawValue;
                        }
                        double ƿ = (double)(ǁ) / (double)Ū;
                        double ƾ = (double)ǀ / (double)ũ;
                        float Ë = (float)(ƿ * Math.PI);
                        Ć.ú(ù, ø, 10, 40, 246, Ë, 0.5f, "Объем: " + (ƿ * 100.0f).ToString("F02") + "%", "Масса: " + (0.001 * ƾ).ToString("F01") + "т");
                    }
                }
                Ň.ContentType = ContentType.SCRIPT;
            }
        }
        void ƽ()
        {
            if (Ŷ.SurfaceCount <= ƒ) return;
            IMyTextSurface Ň = Ŷ.GetSurface(ƒ);
            if (Ŋ) Ň.ContentType = ContentType.SCRIPT;
            else
            {
                Ň.ContentType = ContentType.NONE;
                using (MySpriteDrawFrame ù = Ň.DrawFrame())
                {
                    Color Ƽ = new Color(128, 255, 128, 255);
                    float ƻ = ƣ();
                    float ǋ = (float)(ƻ * Math.PI);
                    Ɛ.Position = new Vector2(52, 175);
                    Ɛ.Size = new Vector2(24, 24);
                    ù.Add(Ɛ);
                    if (Ů.Count > 0 && Ů[0].ChargeMode == ChargeMode.Recharge && Ɣ)
                    {
                        Ć.ú(ù, ø, 10, 40, 246, ǋ, 0.5f, (ƻ * 100.0f).ToString("F02") + "%", "Заряжается");
                        Ƒ.Position = new Vector2(112, 145);
                        Ƒ.Size = new Vector2(40, 40);
                        ù.Add(Ƒ);
                    }
                    else
                    {
                        Ć.ú(ù, ø, 10, 40, 246, ǋ, 0.5f, (ƻ * 100.0f).ToString("F02") + "%", null);
                    }
                }
                Ň.ContentType = ContentType.SCRIPT;
            }
        }
        void ú(MySpriteDrawFrame ù, float ö, float õ, float ĉ, float ċ, string đ, string Đ = null)
        {
            float Æ = (ĉ - 10) * 0.5f;
            float Ę = (float)Math.Cos(ċ - Math.PI);
            float ė = (float)Math.Sin(ċ - Math.PI);
            Vector2 ý = new Vector2(ö + 5 + Æ, õ + 5 + Æ);
            Vector2 Ė = new Vector2(Ę, ė) * (Æ * 0.3f);
            Vector2 ĕ = new Vector2(Ę, ė) * (Æ - 2);
            Vector2 Þ = ý + Ė;
            Vector2 û = ý + ĕ;
            ø.Ě(ù, ý.X, ý.Y, Æ, 6, Color.Green);
            const float ơ = 10;
            ø.Ĺ(ù, ý.X - Æ - ơ, ý.Y, ý.X - Æ + ơ, ý.Y, 4, Color.Green);
            ø.Ĺ(ù, ý.X, ý.Y - Æ + ơ, ý.X, ý.Y - Æ - ơ, 4, Color.Green);
            ø.Ĺ(ù, ý.X + Æ - ơ, ý.Y, ý.X + Æ + ơ, ý.Y, 4, Color.Green);
            ø.Ĺ(ù, Þ.X, Þ.Y, û.X, û.Y, 8, Color.Yellow);
            ø.ī(ù, đ, ý.X, ý.Y + 2, Color.White, 0.8f, TextAlignment.CENTER);
            if (Đ != null) ø.ī(ù, Đ, ý.X, ý.Y + 22, Color.White, 0.8f, TextAlignment.CENTER);
        }
        void Ĕ(MySpriteDrawFrame ù, float ö, float õ, float ĉ, float ē, float Ē, string đ, string Đ = null)
        {
            float Æ = (ĉ - 10) * 0.5f;
            float ď = Æ * 0.6f;
            Vector2 Ď = new Vector2((float)Math.Cos(ē - Math.PI), (float)Math.Sin(ē - Math.PI));
            Vector2 ý = new Vector2(ö + 5 + Æ, õ + 5 + Æ);
            ø.Ě(ù, ý.X, ý.Y, ď, 6, Color.Green);
            const float ơ = 10;
            ø.Ĺ(ù, ý.X - ď - ơ, ý.Y, ý.X - ď + ơ, ý.Y, 4, Color.Green);
            ø.Ĺ(ù, ý.X, ý.Y - ď + ơ, ý.X, ý.Y - ď - ơ, 4, Color.Green);
            ø.Ĺ(ù, ý.X + ď - ơ, ý.Y, ý.X + ď + ơ, ý.Y, 4, Color.Green);
            Vector2 Þ = ý + Ď * (ď + 0.2f * Æ);
            Vector2 û = ý + Ď * ď;
            ø.Ĺ(ù, Þ.X, Þ.Y, û.X, û.Y, 8, Color.Yellow);
            Ď.X = (float)Math.Cos(Ē - Math.PI);
            Ď.Y = (float)Math.Sin(Ē - Math.PI);
            Þ = ý + Ď * (ď - 0.2f * Æ);
            û = ý + Ď * ď;
            ø.Ĺ(ù, Þ.X, Þ.Y, û.X, û.Y, 8, Color.Red);
            ø.ī(ù, đ, ý.X, ý.Y + 2, Color.White, 0.8f, TextAlignment.CENTER);
            if (Đ != null) ø.ī(ù, Đ, ý.X, ý.Y + 22, Color.White, 0.8f, TextAlignment.CENTER);
        }
        void Ơ(MyConveyorSorterMode Ɵ)
        {
            List<MyInventoryItemFilter> ƞ = new List<MyInventoryItemFilter>();
            List<MyInventoryItemFilter> Ɲ = new List<MyInventoryItemFilter>();
            foreach (Ż Ɯ in ū)
            {
                try
                {
                    if (Ɯ.Ť) Ɲ.Add(Ɯ.Ļ);
                    else ƞ.Add(Ɯ.Ļ);
                }
                catch { }
            }
            foreach (IMyConveyorSorter ƛ in ŭ)
            {
                ƛ.SetFilter(Ɵ, ƞ);
            }
            foreach (IMyConveyorSorter ƛ in Ŭ)
            {
                ƛ.SetFilter(Ɵ, Ɲ);
            }
        }
        void ƚ()
        {
            foreach (IMyGyro Ŝ in ŵ)
            {
                Ŝ.Yaw = 0;
                Ŝ.Pitch = 0;
                Ŝ.Roll = 0;
                Ŝ.GyroOverride = false;
            }
        }
        void ƙ()
        {
            ů++;
            if (ů > 10)
            {
                ů = 0;
                if (Ů == null) Ű = 0;
                else
                {
                    float Ƙ = 0;
                    float Ɨ = 0;
                    foreach (IMyBatteryBlock Ƣ in Ů)
                    {
                        Ƙ += Ƣ.CurrentStoredPower;
                        Ɨ += Ƣ.MaxStoredPower;
                    }
                    Ű = Ƙ / Ɨ;
                }
            }
        }
        float ƣ()
        {
            return Ű;
        }
        bool ƹ(IMyTerminalBlock Ʒ)
        {
            return Ʒ.CubeGrid == Me.CubeGrid;
        }
        bool Ƹ(IMyTerminalBlock Ʒ)
        {
            return Ʒ.CubeGrid == Me.CubeGrid && Ʒ.CustomName.Contains("HoverEngineSmallMil");

        }
        œ Huynya<œ>() where œ : class, IMyTerminalBlock
        {
            List<œ> Ƶ = new List<œ>();
            GridTerminalSystem.GetBlocksOfType(Ƶ, ƹ);
            if (Ƶ.Count > 0) return Ƶ[0];
            else return null;
        }
        bool ƴ<œ>(out œ Ƴ, string Ʋ = "") where œ : class, IMyTerminalBlock
        {
            if (Ʋ.Length == 0)
            {
                Ƴ = Huynya<œ>();
                if (Ƴ == null)
                {
                    ų.WriteText("Блок типа\n" + typeof(œ).Name + "\nне найден\n", true);
                    return false;
                }
                else return true;
            }
            else
            {
                Ƴ = GridTerminalSystem.GetBlockWithName(Ʋ) as œ;
                if (Ƴ == null)
                {
                    ų.WriteText("Блок типа\n" + typeof(œ).Name + "\nс именем\n<" + Ʋ + ">\nне найден\n", true);
                    return false;
                }
                else return true;
            }
        }
        void Ʊ(string ư)
        {
            string[] Ư = ư.Split(',');
            for (int ü = 0; ü < ū.Count(); ü++)
            {
                bool Ʈ = false;
                for (int ƭ = 0; ƭ < Ư.Count(); ƭ++)
                {
                    string Ƭ = Ư[ƭ].ToLower().Trim();
                    if (ū[ü].Ÿ.StartsWith(Ƭ))
                    {
                        Ʈ = true;
                        break;
                    }
                    if (Ƭ == "all")
                    {
                        Ʈ = true;
                        break;
                    }
                    if ((Ƭ == "ore") && (ü >= (int)Ə.ƌ))
                    {
                        Ʈ = true;
                        break;
                    }
                }
                ū[ü].Ť = Ʈ;
            }
        }
    }
    class ƫ
    {
        public static void ƪ(List<IMyGyro> Ö, IMyTerminalBlock Õ, Vector3D Ʃ)
        {
            foreach (IMyGyro Ú in Ö)
            {
                Base6Directions.Direction ƨ = Ú.Orientation.TransformDirectionInverse(Õ.Orientation.Forward);
                Base6Directions.Direction Ƨ = Ú.Orientation.TransformDirectionInverse(Õ.Orientation.Left);
                Base6Directions.Direction Ʀ = Ú.Orientation.TransformDirectionInverse(Õ.Orientation.Up);
                Vector3D ƥ = Vector3D.Zero;
                ä.ª(Ƨ, Ʃ.X, ref ƥ);
                ä.ª(Ʀ, -Ʃ.Y, ref ƥ);
                ä.ª(ƨ, Ʃ.Z, ref ƥ);
                Ú.Yaw = (float)ƥ.X;
                Ú.Pitch = (float)ƥ.Y;
                Ú.Roll = (float)ƥ.Z;
                Ú.GyroOverride = true;
            }
        }
        static Vector3D Ƥ(Vector3D Ð, Vector3D Û, double Ì, ref string Ó, double Ò = 0.000001)
        {
            Vector3D Ù = Vector3D.Cross(Ð, Vector3D.Normalize(Û));
            Vector3D Ý = Ù;
            double Í = Ý.Normalize();
            Ó += "SinA " + Í.ToString();
            Ó += "\nDotA " + Ì.ToString();
            double Ë = Math.Asin(ä.Î(Í));
            Ó += "\nAngle " + Ë.ToString();
            if (Ì < 0)
            {
                Ë = (Math.PI - Math.Abs(Ë)) * Math.Sign(Ë);
            }
            Ó += "\nAngleFull " + Ë.ToString();
            Ó += "\nAngleFull3 " + (Ë * 3).ToString();
            Ù = Ý * Ë * 3.0;
            Ó += "\nRV.x " + Ù.X.ToString();
            Ó += "\nRV.y " + Ù.Y.ToString();
            Ó += "\nRV.z " + Ù.Z.ToString();
            return Ù;
        }
        public static bool Ü(List<IMyGyro> Ö, IMyTerminalBlock Õ, Vector3D Ô, ref string Ó, double Ò = 0.000001)
        {
            MatrixD Ñ = MatrixD.Transpose(Õ.WorldMatrix);
            Vector3D Ð = new Vector3D(0, 0, -1);
            Vector3D Û = Vector3D.TransformNormal(Ô, Ñ);
            double Ì = Vector3D.Dot(Ð, Û);
            if ((Ð == Û))
            {
                foreach (IMyGyro Ú in Ö)
                {
                    Ú.Yaw = 0;
                    Ú.Pitch = 0;
                    Ú.Roll = 0;
                }
                return true;
            }
            Vector3D Ù = Ƥ(Ð, Û, Ì, ref Ó, Ò);
            ƪ(Ö, Õ, Ù);
            return false;
        }
        public static bool Ø(List<IMyGyro> Ö, IMyTerminalBlock Õ, Vector3D Ô, Vector3D t, ref string Ó, double Ò = 0.000001)
        {
            MatrixD Ñ = MatrixD.Transpose(Õ.WorldMatrix);
            Vector3D Ð = new Vector3D(0, 0, -1);
            Vector3D Ï = new Vector3D(0, 1, 0);
            Vector3D Û = Vector3D.TransformNormal(Ô, Ñ);
            Vector3D ß = Vector3D.TransformNormal(t, Ñ);
            double Ì = Vector3D.Dot(Ð, Û);
            if ((Ð == Û))
            {
                foreach (IMyGyro Ú in Ö)
                {
                    Ú.Yaw = 0;
                    Ú.Pitch = 0;
                    Ú.Roll = 0;
                }
                return true;
            }
            Vector3D Ù = Ƥ(Ð, Û, Ì, ref Ó, Ò);
            Vector3D ò = Vector3D.Normalize(ä.ã(ß, Ð));
            double ñ = ä.C(ò, Ï, Ð);
            Ó += "\nUpAngle " + ñ.ToString();
            Ù.Z = ñ * 3;
            ƪ(Ö, Õ, Ù);
            return false;
        }
        public static bool ð(List<IMyGyro> Ö, IMyTerminalBlock Õ, Vector3D ï, Vector3D t, double Ò = 0.000001)
        {
            Vector3D î = Õ.WorldMatrix.Forward;
            Vector3D Ç = Õ.WorldMatrix.Up;
            Vector3D Á = Õ.WorldMatrix.Right;
            Vector3D í = Vector3D.Normalize(Vector3D.Cross(ï, t));
            Vector3D ì = -í;
            Vector3D ë = Vector3D.Normalize(Vector3D.Cross(í, ï));
            double ê = ä.m(ref î, ref ï, ref ë, ref í);
            MatrixD é = MatrixD.CreateFromAxisAngle(ë, -ê);
            î = Vector3D.TransformNormal(î, é);
            î = Vector3D.Normalize(Vector3D.ProjectOnPlane(ref î, ref í));
            î = Vector3D.ProjectOnPlane(ref î, ref í);
            double è = ä.m(ref î, ref ï, ref í, ref ë);
            MatrixD ç = MatrixD.CreateFromAxisAngle(í, -è);
            MatrixD æ = é * ç;
            Ç = Vector3D.TransformNormal(Ç, æ);
            double å = ä.m(ref Ç, ref ë, ref ï, ref ì);
            ƪ(Ö, Õ, new Vector3D(è, -ê, å) * 2);
            return false;
        }
    }
    public class ä
    {
        public static Vector3D ã(Vector3D á, Vector3D G)
        {
            return á - G * Vector3D.Dot(á, G);
        }
        public static Vector3D â(Vector3D á, Vector3D à)
        {
            return à * Vector3D.Dot(á, à);
        }
        public static double Î(double o)
        {
            return Math.Max(-1.0, Math.Min(1.0, o));
        }
        public static Vector3I A(Vector3I f, Vector3I e)
        {
            Vector3I d = e - f;
            d.X = d.X == 0 ? 0 : d.X / Math.Abs(d.X);
            d.Y = d.Y == 0 ? 0 : d.Y / Math.Abs(d.Y);
            d.Z = d.Z == 0 ? 0 : d.Z / Math.Abs(d.Z);
            return d;
        }
        public static double b(Vector3D K, Vector3D J, Vector3D I)
        {
            Vector3D G = Vector3D.Normalize(J - K);
            Vector3D F = I - K;
            Vector3D E = Vector3D.Dot(F, G) * G;
            return Vector3D.Distance(E, F);
        }
        public static double a(Vector3D Z, Vector3D U, double R, double Q)
        {
            double P = Vector3D.Dot(U, Z);
            double O = 4.0 * Q * Q * (P * P - 1.0) + 4 * R * R;
            double N = 0.5 * (Math.Sqrt(O) - 2.0 * Q * P);
            return N;
        }
        public static Vector3D M(Vector3D K, Vector3D J, Vector3D I)
        {
            Vector3D G = Vector3D.Normalize(J - K);
            Vector3D F = I - K;
            Vector3D E = Vector3D.Dot(F, G) * G;
            return E + K;
        }
        public static double C(Vector3D B, Vector3D g, Vector3D k)
        {
            double À = Vector3D.Dot(B, g);
            Vector3D Í = Vector3D.Cross(B, g);
            double Ì = Vector3D.Dot(k, Í);
            double Ë = Math.Acos(MathHelper.Clamp(À, -1, 1));
            return Ë * Math.Sign(Ì);
        }
        public static Vector3D Ê(Vector3D É, Vector3D È, Vector3D Ç, double Æ)
        {
            Vector3D Å = É - È;
            Vector3D Ä = Å;
            double Ã = Ä.Normalize();
            double Â = Math.Sqrt(Ã * Ã - Æ * Æ);
            Vector3D Á = Vector3D.Normalize(Vector3D.Cross(Ä, Ç));
            double À = Â / Ã;
            double º = Æ * À;
            Vector3D µ = Vector3D.Normalize(Å) * À * Â;
            return È + µ + Á * º;
        }
        public static void ª(Base6Directions.Direction z, double y, ref Vector3D x)
        {
            switch (z)
            {
                case Base6Directions.Direction.Up: x.X = y; break;
                case Base6Directions.Direction.Down: x.X = -y; break;
                case Base6Directions.Direction.Right: x.Y = y; break;
                case Base6Directions.Direction.Left: x.Y = -y; break;
                case Base6Directions.Direction.Forward: x.Z = -y; break;
                case Base6Directions.Direction.Backward: x.Z = y; break;
            }
        }
        public static void v(Vector3D u, Vector3D t, out Vector3D r, out Vector3D q)
        {
            q = Vector3D.Normalize(Vector3D.Cross(u, t));
            r = Vector3D.Normalize(Vector3D.Cross(q, u));
        }
        public static double p(double o, double n, double j)
        {
            return Math.Min(j, Math.Max(n, o));
        }
        public static double m(ref Vector3D ó, ref Vector3D î, ref Vector3D Ç, ref Vector3D Á)
        {
            Vector3D Ĩ = Vector3D.ProjectOnPlane(ref ó, ref Ç);
            Vector3D ħ = Ĩ;
            double Ħ = ħ.Normalize();
            if (Ħ < 0.00001) ħ = î;
            double ĥ = Math.Acos(p(Vector3D.Dot(ħ, î), -1, 1));
            if (Vector3D.Dot(ħ, Á) > 0) ĥ *= -1;
            return ĥ;
        }
    }
    public class Ĥ
    {
        MySprite ģ;
        MySprite Ģ;
        MySprite ġ;
        MySprite Ġ;
        MySprite ğ;
        MySprite Ğ;
        MySprite ĝ;
        MySprite Ĝ;
        public Ĥ()
        {
            ģ = new MySprite(SpriteType.TEXTURE, "IconEnergy", new Vector2(0, 50), new Vector2(22f, 22f), new Color(128, 255, 128, 255), "Monospace", TextAlignment.LEFT);
            ġ = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(150, 150), new Vector2(100f, 100f), new Color(0, 255, 255, 255), "Monospace", TextAlignment.LEFT);
            Ġ = new MySprite(SpriteType.TEXTURE, "SquareHollow", new Vector2(150, 150), new Vector2(100f, 100f), new Color(0, 255, 255, 255), "Monospace", TextAlignment.LEFT);
            Ģ = MySprite.CreateText("шаблон", "Monospace", new Color(128, 255, 128, 255), 0.8f, TextAlignment.LEFT);
            ğ = new MySprite(SpriteType.TEXTURE, "CircleHollow", new Vector2(0, 256), new Vector2(512f, 512f), new Color(128, 255, 128, 255), "Monospace", TextAlignment.LEFT);
            Ğ = new MySprite(SpriteType.TEXTURE, "SemiCircle", new Vector2(0, 256), new Vector2(512f, 512f), new Color(128, 255, 128, 255), "Monospace", TextAlignment.LEFT);
            ĝ = new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(0, 256), new Vector2(512f, 512f), new Color(128, 255, 128, 255), "Monospace", TextAlignment.LEFT);
            Ĝ = new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(0, 256), new Vector2(512f, 512f), new Color(128, 255, 128, 255), "Monospace", TextAlignment.LEFT);
        }
        public void ě(MySpriteDrawFrame ù, float ö, float õ, float Æ, Color č)
        {
            ğ.Position = new Vector2(ö - Æ, õ);
            ğ.Size = new Vector2(Æ * 2, Æ * 2);
            ğ.Color = č; ù.Add(ğ);
        }
        public void Ě(MySpriteDrawFrame ù, float ö, float õ, float Æ, float ĩ, Color č)
        {
            Ğ.Position = new Vector2(ö - Æ, õ);
            Ğ.Size = new Vector2(Æ * 2, Æ * 2);
            Ğ.Color = č;
            ù.Add(Ğ);
            float ď = Æ - ĩ;
            Ğ.Position = new Vector2(ö - ď, õ);
            Ğ.Size = new Vector2(ď * 2, ď * 2);
            Ğ.Color = new Color(0, 0, 0, 255);
            ù.Add(Ğ);
        }
        public void ĺ(MySpriteDrawFrame ù, float ķ, float Ķ, float ĵ, float Ĵ, Color č)
        {
            ġ.Position = new Vector2(ķ, Ķ + Ĵ * 0.5f);
            ġ.Size = new Vector2(ĵ, Ĵ);
            ġ.Color = č;
            ġ.RotationOrScale = 0;
            ù.Add(ġ);
        }
        public void ĸ(MySpriteDrawFrame ù, float ķ, float Ķ, float ĵ, float Ĵ, Color č)
        {
            Ġ.Position = new Vector2(ķ, Ķ + Ĵ * 0.5f);
            Ġ.Size = new Vector2(ĵ, Ĵ);
            Ġ.Color = č;
            Ġ.RotationOrScale = 0;
            ù.Add(Ġ);
        }
        public void Ĺ(MySpriteDrawFrame ù, float ĳ, float Ĳ, float ı, float İ, float ĩ, Color č)
        {
            float į = ı - ĳ;
            float Į = İ - Ĳ;
            float ĭ = (float)Math.Sqrt(į * į + Į * Į);
            float Ĭ = ĭ * 0.5f;
            float Í = Į / ĭ;
            ġ.RotationOrScale = (float)Math.Acos(į / ĭ) * Math.Sign(Í);
            ġ.Position = new Vector2((ĳ + ı) * 0.5f - Ĭ, (Ĳ + İ) * 0.5f);
            ġ.Size = new Vector2(ĭ, ĩ);
            ġ.Color = č;
            ù.Add(ġ);
        }
        public void ī(MySpriteDrawFrame ù, string Ī, float ę, float Ċ, Color ô, float Ĉ = 0, TextAlignment ć = TextAlignment.LEFT)
        {
            Ģ.Position = new Vector2(ę, Ċ);
            Ģ.Data = Ī;
            Ģ.Color = ô;
            Ģ.Alignment = ć;
            if (Ĉ != 0) Ģ.RotationOrScale = Ĉ;
            ù.Add(Ģ);
        }
    }
    class Ć
    {
        static void ą(MySpriteDrawFrame ù, Ĥ ø, float ö, float õ, float Æ, float Ą, float ă, float Ă, float ā, int Ā, Color ÿ)
        {
            float Ë = Ă;
            float þ = (ā - Ă) / Ā;
            Vector2 d = new Vector2();
            Vector2 ý = new Vector2(ö, õ);
            for (int ü = 0; ü < Ā + 1; ü++)
            {
                d.X = (float)Math.Cos(Ë);
                d.Y = (float)Math.Sin(Ë);
                Vector2 Þ = ý + d * (1 - Ą) * Æ;
                Vector2 û = ý + d * Æ;
                ø.Ĺ(ù, Þ.X, Þ.Y, û.X, û.Y, ă, ÿ);
                Ë += þ;
            }
        }
        public static void ú(MySpriteDrawFrame ù, Ĥ ø, float ö, float õ, float ĉ, float ċ, float Ĉ, string đ, string Đ = null)
        {
            float Æ = (ĉ - 20) * 0.5f;
            float Ę = (float)Math.Cos(ċ - Math.PI);
            float ė = (float)Math.Sin(ċ - Math.PI);
            Vector2 ý = new Vector2(ö + 10 + Æ, õ + 10 + Æ);
            Vector2 Ė = new Vector2(Ę, ė) * (Æ * 0.3f);
            Vector2 ĕ = new Vector2(Ę, ė) * (Æ - 2);
            Vector2 Þ = ý + Ė;
            Vector2 û = ý + ĕ;
            Color č = new Color(128, 255, 128);
            ą(ù, ø, ý.X, ý.Y, Æ, 0.1f, 2, (float)-Math.PI, 0, 12, č);
            ø.Ĺ(ù, Þ.X, Þ.Y, û.X, û.Y, 4, Color.Yellow);
            ø.ī(ù, đ, ý.X, ý.Y + 2, Color.White, Ĉ, TextAlignment.CENTER);
            if (Đ != null) ø.ī(ù, Đ, ý.X, ý.Y + 22, Color.White, Ĉ, TextAlignment.CENTER);
        }
        public static void Ĕ(MySpriteDrawFrame ù, Ĥ ø, float ö, float õ, float ĉ, float ē, float Ē, float Ĉ, string đ, string Đ = null)
        {
            float Æ = (ĉ - 20) * 0.5f;
            float ď = Æ * 0.6f;
            Vector2 Ď = new Vector2((float)Math.Cos(ē - Math.PI), (float)Math.Sin(ē - Math.PI));
            Vector2 ý = new Vector2(ö + 10 + Æ, õ + 10 + Æ);
            Color č = new Color(128, 255, 128);
            Color Č = new Color(64, 128, 64);
            ø.Ě(ù, ý.X, ý.Y, ď, 2, Č);
            ą(ù, ø, ý.X, ý.Y, ď + 6, 0.2f, 2, (float)-Math.PI, 0, 8, č);
            Vector2 Þ = ý + Ď * (ď + 0.2f * Æ);
            Vector2 û = ý + Ď * ď;
            ø.Ĺ(ù, Þ.X, Þ.Y, û.X, û.Y, 3, Color.Yellow);
            Ď.X = (float)Math.Cos(Ē - Math.PI);
            Ď.Y = (float)Math.Sin(Ē - Math.PI);
            Þ = ý + Ď * (ď - 0.2f * Æ);
            û = ý + Ď * ď;
            ø.Ĺ(ù, Þ.X, Þ.Y, û.X, û.Y, 3, Color.Red);
            ø.ī(ù, đ, ý.X, ý.Y + 2, Color.White, Ĉ, TextAlignment.CENTER);
            if (Đ != null) ø.ī(ù, Đ, ý.X, ý.Y + 22, Color.White, Ĉ, TextAlignment.CENTER);
        }

        //------------END--------------
    }
}