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
namespace ScriptPDT1
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        /* Клан "Андромеда"
         * vk.com/andromeda_se
         *
         * Planetary Down Thrusters Only Script (PDTOS)
         * Версия от 08.09.2019
         *
         * Для работы скрипта кроме пр. блока необходимо:
         * 1.Ускорители
         * 2.Блок управления кораблем
         * 3.Гироскоп
         *
         * В Custom Data (Свои данные) пр. блока в столбик необходимо указать:
         * 1. Имя блока управления кораблем (Можно оставить пустым, тогда нужно установить главный)
         * 2. Имя группы (Можно оставить пустым, тогда будет использовать все что есть)
         * 3. Имя текстовой панели для вывода информации(для указания панели внутри блока - "Блок*Номер")
         * 4. Множитель управления вращением
         * 5. Маленькая дистанция до поверхности (Уменьшение угла)
         * 6. Критическая дистанция до поверхности (Сильное уменьшение угла)
         * 7. Рысканье на Q E (Право-Лево = Q-E)
         * 8. Множитель гироскопа
         * 9. Делитель скорости для контроля наклона (Коэффициент наклона = Скорость / X)
         * 10. Имя таймера перегрузки (Сработает при перегрузке корабля на высоте не выше 3км, Можно оставить пустым)
         * 
         * Аргументы:
         * Переключение режима 'максимального угла' - "ToggleMaxAngle"
         * 
         * Настройки загружаются при компиляции скрипта. При изменении настроек перекомпилируйте скрипт
         * Для выключения гасетеля нажмите Z
         */

        //Глубокая настройка 
        const float ThrustCoeff = 0.5f;//Эффективность ускорителей вниз при которой можно считать что корабль вышел в космос
        const bool Eng = false;//English outputs

        //Пожалуйста не изменяйте ничего ниже
        //Please do not change anything below
        IMyShipController Controller;
        IMyTextSurface Surface;
        IMyTimerBlock OverloadTimer;

        float MaxThrust = 0f;
        float MaxAngle = 1f;
        float AngleCoeff = 0.7f;
        float CocpitMultipler;
        float SmallDistance;
        float CriticalDistance;
        float GyroMultipler;
        float SpeedDevider;
        double SurfaceDistance = 0;

        bool Dampen = true;
        bool UseMaxAngle = false;
        bool QEUSE = false;
        bool UneffectiveThrusts = false;

        string Status; string Warnings;

        public Program()
        {
            string[] StrArg = EasyApi.GetSettings(Me.CustomData, 10);
            string ControllName = StrArg[0];
            string GroupName = StrArg[1];
            string SurfaceName = StrArg[2] == "" ? Me.CustomName + "*0" : StrArg[2];
            float.TryParse(StrArg[3], out CocpitMultipler); if (CocpitMultipler == 0) CocpitMultipler = 0.01f;
            float.TryParse(StrArg[4], out SmallDistance); if (SmallDistance == 0) SmallDistance = 20f;
            float.TryParse(StrArg[5], out CriticalDistance); if (CriticalDistance == 0) CriticalDistance = 10f;
            bool.TryParse(StrArg[6], out QEUSE);
            float.TryParse(StrArg[7], out GyroMultipler); if (GyroMultipler == 0) GyroMultipler = 2f;
            float.TryParse(StrArg[8], out SpeedDevider); if (SpeedDevider == 0) SpeedDevider = 10f;
            string TimerName = StrArg[9];
            List<IMyShipController> tmpControllers = new List<IMyShipController>();
            List<IMyThrust> tmpThrust = new List<IMyThrust>();
            if (GroupName == "")
            {
                GridTerminalSystem.GetBlocksOfType<IMyThrust>(tmpThrust);
                GridTerminalSystem.GetBlocksOfType<IMyGyro>(Gyro);
                GridTerminalSystem.GetBlocksOfType<IMyShipController>(tmpControllers);
            }
            else
            {
                IMyBlockGroup Group = GridTerminalSystem.GetBlockGroupWithName(GroupName);
                if (Group == null) throw new NullReferenceException(Eng ? "Group specified but not found." : "Группа указана, но не найдена.");
                Group.GetBlocksOfType<IMyThrust>(tmpThrust);
                Group.GetBlocksOfType<IMyGyro>(Gyro);
                Group.GetBlocksOfType<IMyShipController>(tmpControllers);
            }
            if (tmpControllers.Count > 0)
            {
                Controller = tmpControllers.Find(x => (x.IsMainCockpit || x.CustomName == ControllName) && x.CubeGrid == Me.CubeGrid);
                if (Controller == null) Controller = tmpControllers.Find(x => x.CubeGrid == Me.CubeGrid);
            }
            else throw new NullReferenceException(Eng ? "Controller doesn't exists on ship." : "Контроллер отсутствует на корабле.");
            if (Controller == null) throw new NullReferenceException(Eng ? "Controller not found." : "Контроллер корабля не найден.");
            tmpThrust.ForEach(x =>
            {
                if (x.CubeGrid == Me.CubeGrid)
                {
                    x.Enabled = true;
                    if (x.IsWorking)
                    {
                        if (x.WorldMatrix.Forward == Controller.WorldMatrix.Down)
                        {
                            Thrusts.Add(x);
                            MaxThrust += x.MaxThrust;
                        }
                        else if (x.WorldMatrix.Forward == Controller.WorldMatrix.Backward)
                            SpaceThrusts.Add(x);
                        else x.Enabled = false;
                    }
                }
            });
            if (!Thrusts.Any() && !SpaceThrusts.Any()) throw new NullReferenceException(Eng ? "Thrusters downward or forward not found." : "Ускорители направленные вниз или вперед не найдены.");
            Gyro = Gyro.FindAll(x => x.CubeGrid == Me.CubeGrid);
            if (!Gyro.Any()) throw new NullReferenceException(Eng ? "Gyros not found." : "Гироскопы не найдены.");
            if (SurfaceName != "-")
            {
                string[] surf = SurfaceName.Split('*');
                int syrfs;
                var prov = (GridTerminalSystem.GetBlockWithName(surf[0]) as IMyTextSurfaceProvider);
                if (prov != null)
                {
                    string ToParse = surf.Length > 1 ? surf[1] : "0";
                    if (int.TryParse(surf[1], out syrfs))
                    {
                        if (syrfs >= 0 && prov.SurfaceCount > syrfs)
                        {
                            Surface = prov.GetSurface(syrfs);
                        }
                        else throw new Exception(Eng ? "Surface number " + syrfs + " in '" + surf[0] + "' doesn't exists." : "Панель номер " + syrfs + " в блоке '" + surf[0] + "' не существует.");
                    }
                    else throw new Exception(Eng ? "Surface number is not Integer." : "Номер панели не является Integer.");
                }
                else throw new Exception(Eng ? "" : "Surface not found.");
            }
            if (TimerName != "")
            {
                OverloadTimer = GridTerminalSystem.GetBlockGroupWithName(TimerName) as IMyTimerBlock;
                if (OverloadTimer == null) throw new NullReferenceException(Eng ? "Overload timer specified but not found." : "Таймер перегрузки указан, но не найден.");
            }
            Surface.ContentType = ContentType.TEXT_AND_IMAGE;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            Me.CustomData = Eng ? "Ship control block name: " + Controller.CustomName +
                        "\r\nGroup name: " + GroupName +
                        "\r\nText panel address: " + SurfaceName +
                        "\r\nYaw Rotation multipler: " + CocpitMultipler +
                        "\r\nSmall altitude: " + SmallDistance +
                        "\r\nCritical altitude: " + CriticalDistance +
                        "\r\nYaw axis control by Q&E: " + QEUSE +
                        "\r\nGyro multiplier: " + GyroMultipler +
                        "\r\nSpeed devider: " + SpeedDevider +
                        "\r\nOverload timer: " + TimerName :
                                "Имя блока управления кораблем: " + Controller.CustomName +
                        "\r\nИмя группы: " + GroupName +
                        "\r\nИмя текстовой панели: " + SurfaceName +
                        "\r\nМножитель управления вращением: " + CocpitMultipler +
                        "\r\nМаленькая дистанция: " + SmallDistance +
                        "\r\nКритическая дистанция: " + CriticalDistance +
                        "\r\nРысканье на Q E: " + QEUSE +
                        "\r\nМножитель гироскопа: " + GyroMultipler +
                        "\r\nДелитель скорости: " + SpeedDevider +
                        "\r\nТаймер перегрузки: " + TimerName;
        }

        public void Main(string arg, UpdateType updSrc)
        {
            Echo($"Клан \"Андромеда\"\r\nvk.com/andromeda_se\r\n\r\nPDTOS\r\n");
            Dampen = Controller.DampenersOverride;
            if ((Thrusts.Any() && Controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out SurfaceDistance) && !UneffectiveThrusts) || !SpaceThrusts.Any())
            {
                ArgParse(arg);
                Status += (Eng ? "Altitude: " : "Высота: ") + SurfaceDistance.ToString("0.0") + "m\n";
                if (SurfaceDistance < CriticalDistance) AngleCoeff = 0.2f;
                else if (SurfaceDistance < SmallDistance) AngleCoeff = 0.5f;
                else if (!UseMaxAngle) AngleCoeff = 0.7f;
                else AngleCoeff = 1;

                Vector3D Grav = Controller.GetNaturalGravity();
                Vector3D GravNorm = Vector3D.Normalize(Grav);
                Vector3D Speed = Controller.GetShipVelocities().LinearVelocity;
                Vector3D Veloc = Vector3D.ProjectOnVector(ref Speed, ref Grav);
                float VertVel = (float)((Veloc.Dot(Grav) > 0) ? (-Veloc.Length()) : Veloc.Length());
                float HorVel;
                Stabilize(GetStablizer(Speed, GravNorm, out HorVel, VertVel));
                SetThrust(GetThrust(Speed, GravNorm, Grav, VertVel));
                Status += (Eng ? " Vert: " : " Верт: ") + VertVel.ToString("0.00m/s\n");
                Status += (Eng ? " Horz: " : " Горз: ") + HorVel.ToString("0.00m/s\n");
                UpdateAngle(Grav);
                Status += (Eng ? "MaxAngle: " : "МаксУгол: ") + UseMaxAngle + "\n";
                SetEnableThrust(false);
            }
            else
            {
                Status += (Eng ? "Space!\n" : "Космос!\n");
                Vector3D Speed = Controller.GetShipVelocities().LinearVelocity;
                Vector3D Grav = Controller.GetNaturalGravity();
                Status += (Eng ? "Speed: " : "Скорость: ") + Speed.Length().ToString("0.00") + "m/s\n";
                if (Controller.RotationIndicator == Vector2.Zero &&
                    Controller.MoveIndicator.Z == 0 &&
                    Controller.DampenersOverride)
                {
                    Vector3D Stabl = Speed + Grav;
                    if (Stabl.Length() > 0.0099)
                    {
                        if (StabilizeSpace(Stabl) < 0.1)
                            SetEnableThrust(true);
                        else SetEnableThrust(false);
                    }
                    else
                    {
                        SetGyro();
                        SetEnableThrust(true);
                    }
                }
                else
                {
                    SetGyro();
                    SetEnableThrust(true);
                }
                if (Thrusts.Any()) UpdateAngle(Grav, false);
            }
            Surface.WriteText("PDTOS\n" + Status + Warnings);
            Status = ""; Warnings = "";
        }

        public void Save()
        {
            SetGyro();
            SetThrust();
        }
        public void ArgParse(string arg)
        {
            string[] argsv = arg.Split(':');
            switch (argsv[0])
            {
                case "ToggleMaxAngle":
                    UseMaxAngle = !UseMaxAngle;
                    break;
            }
        }
        public void UpdateAngle(Vector3D Grav, bool textOut = true)
        {
            float MaxEffTh = MaxThrust * (Thrusts[0].MaxEffectiveThrust / Thrusts[0].MaxThrust);
            MaxAngle = ((float)(Math.Acos((float)Grav.Length() *
                Controller.CalculateShipMass().PhysicalMass / MaxEffTh))) * 0.9f;
            UneffectiveThrusts = false;
            if (float.IsNaN(MaxAngle) || MaxAngle <= 0)
            {
                if (textOut) Warnings += (Eng ? "Overload!\n" : "Перегруз!\n");
                MaxAngle = 0;
                if (MaxEffTh < MaxThrust * ThrustCoeff) UneffectiveThrusts = true;
                if (OverloadTimer != null && SurfaceDistance < 3000) OverloadTimer.Trigger();
            }
            if (MaxAngle > 1.4f)
            {
                MaxAngle = 1.4f;
            }
            if (textOut) Status += (Eng ? "Angle: " : "Угол: ") + ((MaxAngle * AngleCoeff) * 180 / Math.PI).ToString("0") + "°\n";
        }

        public Vector3D GetStablizer(Vector3D Speed, Vector3D GravNorm, out float SpeedH, float VertVel)
        {
            double Spd = Speed.Length();
            Status += (Eng ? "Speed: " : "Скорость: ") + Spd.ToString("0.00") + "m/s\n";
            double StopCtrl = Math.Min(Spd / (SpeedDevider * (4 * AngleCoeff)), 1);
            Vector3D GRejVel = Vector3D.Reject(Speed, GravNorm);
            SpeedH = (float)GRejVel.Length();
            if (!Dampen) GRejVel = Vector3D.Zero;
            if (Controller.MoveIndicator != Vector3D.Zero)
            {
                Vector3D Forward = Vector3D.Normalize(Vector3D.Reject(Controller.WorldMatrix.Forward, GravNorm));
                Vector3D Left = Vector3D.Normalize(Vector3D.Reject(Controller.WorldMatrix.Left, GravNorm));
                Vector3D Acell = Vector3D.Normalize((Forward * Controller.MoveIndicator.Z) + (Left * Controller.MoveIndicator.X));
                GRejVel = Vector3D.Reject(GRejVel, Acell) + Acell * 100;
                StopCtrl = 1;
            }
            double grvel = GRejVel.Length();
            if (grvel > Math.Abs(VertVel) * 2 || grvel > 10)
            {
                double CKat = (Math.Tan(Convert.ToDouble(MaxAngle * AngleCoeff * StopCtrl)));
                return (Vector3D.Normalize(GRejVel) * CKat) + GravNorm;
            }
            else return GravNorm;
        }
        public void Stabilize(Vector3D Stabilizer)
        {
            if (Stabilizer.Length() > 0)
            {
                Vector3D Row = Vector3D.Normalize(Stabilizer);
                float PitchInput = (float)Row.Dot(Controller.WorldMatrix.Forward);
                float RollInput = (float)Row.Dot(Controller.WorldMatrix.Left);
                float RL = 0;
                if (QEUSE) RL = Controller.RollIndicator * 10; else RL = Controller.RotationIndicator.Y;
                SetGyro(PitchInput * GyroMultipler, RL * CocpitMultipler, RollInput * GyroMultipler);
            }
            else SetGyro();
        }
        public float StabilizeSpace(Vector3D Stabilizer)
        {
            if (Stabilizer.Length() > 0)
            {
                Vector3D Row = Vector3D.Normalize(Stabilizer);
                double gF = Row.Dot(Controller.WorldMatrix.Down);
                double gL = Row.Dot(Controller.WorldMatrix.Right);
                double gU = Row.Dot(Controller.WorldMatrix.Forward);
                float YawInput = -(float)Math.Atan2(gL, -gU) * 0.4f;
                float PitchInput = (float)Math.Atan2(gF, -gU) * 0.4f;
                float RL = 0;
                RL = Controller.RollIndicator * 10;
                SetGyro(PitchInput * GyroMultipler, YawInput * GyroMultipler, RL * CocpitMultipler);
                return Math.Abs(YawInput) + Math.Abs(PitchInput);
            }
            else
            {
                SetGyro();
                return 0;
            }
        }
        List<IMyGyro> Gyro = new List<IMyGyro>();
        public void SetGyro(float pitch = 0.0f, float yaw = 0.0f, float roll = 0.0f)
        {
            foreach (IMyGyro Gyr in Gyro)
            {
                if (Gyr.IsWorking)
                {
                    if (pitch == 0.0f && yaw == 0.0f && roll == 0.0f)
                    {
                        Gyr.GyroOverride = false;
                    }
                    else
                    {
                        MatrixD Om = Controller.WorldMatrix; Vector3D axis = Vector3D.TransformNormal(new Vector3D(pitch, -yaw, -roll), (Controller.WorldMatrix));
                        Vector3D axisLocal = Vector3D.TransformNormal(axis, MatrixD.Transpose(Gyr.WorldMatrix));
                        Gyr.GyroOverride = true;
                        Gyr.Pitch = (float)-axisLocal.X;
                        Gyr.Yaw = (float)-axisLocal.Y;
                        Gyr.Roll = (float)-axisLocal.Z;
                    }
                }
                else { Gyr.GyroOverride = false; }
            }
        }

        public float GetThrust(Vector3D Speed, Vector3D GravNorm, Vector3D Grav, float VertVel)
        {
            double Mass = Controller.CalculateShipMass().PhysicalMass;
            Vector3D MDown = Controller.WorldMatrix.Down;
            double mult = 1 / MDown.Dot(GravNorm);
            double VelH = Vector3D.Reject(Speed, GravNorm).Length();
            Vector3D Centor;
            Controller.TryGetPlanetPosition(out Centor);
            double gravForce = Grav.Length() - (Math.Pow(VelH, 2) / (Controller.CenterOfMass - Centor).Length());
            double Thrust = Mass * (gravForce - VertVel * 10) * mult + Controller.MoveIndicator.Y * float.MaxValue;
            if (Thrust >= MaxThrust)
                Thrust = 0;
            return (float)Thrust;
        }
        List<IMyThrust> Thrusts = new List<IMyThrust>();
        public void SetThrust(float thrust = 0.0f, bool procentage = false)
        {

            Thrusts.ForEach(x =>
            {
                if (procentage)
                {
                    x.ThrustOverridePercentage = thrust;
                }
                else
                {
                    x.ThrustOverride = thrust * (float)(Math.Pow(x.MaxThrust, 2) / (x.MaxEffectiveThrust * MaxThrust));
                }
                x.Enabled = true;
            });
        }
        List<IMyThrust> SpaceThrusts = new List<IMyThrust>();
        public void SetEnableThrust(bool Enable)
        {
            if (SpaceThrusts.Any() && SpaceThrusts.First().Enabled != Enable) SpaceThrusts.ForEach(x => x.Enabled = Enable);
        }
        static class EasyApi
        {
            //Без упаковки
            public static string[] GetSettings(string Settings, int Len)
            {
                string[] args = Settings.Split(new string[] { "\n" }, StringSplitOptions.None);
                Array.Resize(ref args, Len);
                for (int i = 0; i < args.Length; i++)
                    if (args[i] != null) args[i] = args[i].Split(':').Last().Trim();
                    else args[i] = "";
                return args;
            }
        }
        //------------END--------------
    }
}