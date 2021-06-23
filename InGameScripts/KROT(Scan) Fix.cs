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


namespace KROT_Scan_Fix
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------

        const float max_speed_flight = 39.0f; //макс скорость полета
        const float max_speed_drill = 3f; //макс скорость проходки
        const float max_hover_h = 25.0f; //макс высота ховера
        const float min_hover_h = 0.0f; // мин высота ховера

        const float h_step_drill = 0.1f; //шаг изменения высоты в режиме бурения
        const float h_step_flight = 1.0f; //шаг изменения высоты в режиме полета
        const float f_step_drill = 0.05f; //наклон кнопками WS в режиме бурения без круиз-контроля
        const float f_step_flight = 0.05f; //наклон кнопками WS в режиме полета без круиз-контроля
        const float r_decl_drill = 0.0f; //наклон кнопками QE в режиме бурения
        const float r_decl_flight = 0.25f; //наклон кнопками QE в режиме полета
        const float cruise_step_drill = 0.1f; //шаг изменения скорости в режиме бурения (круизконтроль)
        const float cruise_step_flight = 1.0f; //шаг изменения скорости в режиме полета (круизконтроль)

        const int Hot_Cold = 1; // На каком блоке запущен скрипт: Hot = 10, Cold = 1

        const float k_decl_flight = 0.02f;
        const float k_decl_drill = 0.1f;
        const float max_decl_flight = 0.3f;
        const float max_decl_drill = 0.1f;

        const float GyroMult = 1f;

        bool drill = false;
        bool dir_con = true;
        bool keepASL = false;

        bool parking = false;

        int cnt = 0;

        IMyCockpit Controller;
        List<IMyTerminalBlock> TempList;
        List<IMyThrust> HoverList;
        List<IMyThrust> RCS;
        List<IMyGyro> gyros;
        IMyTextSurface LCD;
        IMyShipConnector connector;

        public Program()
        {
            TempList = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(TempList, (b) => (b.IsSameConstructAs(Me) && b.CustomName.Contains("Industrial Cockpit")));
            if (TempList.Count > 0)
            {
                Controller = TempList[0] as IMyCockpit;
                LCD = Controller.GetSurface(2);
                if (LCD == null)
                    LCD = Controller.GetSurface(1);
            }
            else
                Echo("No Main Cockpit");

            HoverList = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(HoverList, (b) =>
            (b.IsSameConstructAs(Me) &&
            (b.Orientation.Forward == Base6Directions.GetOppositeDirection(Controller.Orientation.Up))));

            RCS = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(RCS, (b) =>
            (b.IsSameConstructAs(Me) &&
            (b.Orientation.Forward != Base6Directions.GetOppositeDirection(Controller.Orientation.Up))));

            gyros = new List<IMyGyro>();

            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros, (b) =>
             (b.IsSameConstructAs(Me)));

            //-----
            Controller.DampenersOverride = true;
            foreach (IMyThrust rcs in RCS)
            {
                rcs.Enabled = false;
            }
            GyroOver(true);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            Hover();
            List<IMyShipConnector> cList = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(cList, (b) =>
            (b.IsSameConstructAs(Me)));
            connector = cList[0];
            //-----
        }

        public void Main(string argument, UpdateType uType)
        {
            if (uType == UpdateType.Update1)
            {
                cnt++;
                if (cnt % Hot_Cold == 0)
                    if (parking) Parking();
                    else Hover();
            }
            else
            {
                switch (argument)
                {

                    case "SetPark":
                        //Здесь мы записываем координаты стыковочной точки и направления стыковки в кастом дата прог блока
                        if (connector.Status > 0)
                        {
                            ParkMatrix = connector.WorldMatrix;
                            Me.CustomData = ParkMatrix.Translation.ToString() + '\n' + ParkMatrix.Forward.ToString() + '\n';
                        }
                        break;
                    case "Park":
                        //Восстанавливаем стыковочную матрицу из кастом дата npor блока
                        string[] v = Me.CustomData.Split('\n');
                        Vector3D t = new Vector3D();
                        Vector3D f = new Vector3D();
                        Vector3D p = new Vector3D();
                        if (Vector3D.TryParse(v[0], out t) && Vector3D.TryParse(v[1], out f) && Controller.TryGetPlanetPosition(out p))
                        {
                            ParkMatrix.Translation = t;
                            ParkMatrix.Forward = f;
                            ParkMatrix.Up = Vector3D.Normalize(t - p);
                            ParkMatrix.Right = ParkMatrix.Forward.Cross(ParkMatrix.Up);
                        }

                        //Запускаем стыковочную процедуру
                        Controller.DampenersOverride = true;
                        foreach (IMyThrust rcs in RCS)
                        {
                            rcs.Enabled = false;
                        }
                        GyroOver(true);
                        Runtime.UpdateFrequency = UpdateFrequency.Update1;
                        parking = true;
                        Parking();
                        break;
                    case "StopPark":
                        parking = false;
                        ForwardSpeed = 0f;
                        break;
                    case "Start":
                        Controller.DampenersOverride = true;
                        foreach (IMyThrust rcs in RCS)
                        {
                            rcs.Enabled = false;
                        }
                        GyroOver(true);
                        Runtime.UpdateFrequency = UpdateFrequency.Update1;
                        Hover();
                        break;

                    case "Stop":
                        GyroOver(false);
                        Controller.DampenersOverride = true;
                        foreach (IMyThrust rcs in RCS)
                        {
                            rcs.Enabled = true;
                        }
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        break;

                    case "ASL":
                        keepASL = !keepASL;
                        Controller.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out D_H_ASL);
                        break;

                    case "Drill":
                        drill = !drill;
                        if (drill)
                        {
                            h_step = h_step_drill;
                            f_step = f_step_drill;
                            cruise_step = cruise_step_drill;
                            r_decl = r_decl_drill;
                            k_d = k_decl_drill;
                            max_decl = max_decl_drill;
                        }
                        else
                        {
                            h_step = h_step_flight;
                            f_step = f_step_flight;
                            cruise_step = cruise_step_flight;
                            r_decl = r_decl_flight;
                            k_d = k_decl_flight;
                            max_decl = max_decl_flight;
                        }
                        break;

                    case "Zero":
                        ForwardSpeed = 0f;
                        break;

                    case "Dir":
                        dir_con = !dir_con;
                        break;

                    default:
                        break;
                }
            }
        }


        Vector3D loc_up;
        Vector3D fwd_hor;
        Vector3D right_hor;
        Vector3D my_hor_velocity;
        Vector3D stop_vec;
        Vector3D balance_vec;
        Vector3D input_F;
        Vector3D input_R;

        float h_step = 1.0f;
        float f_step = 0.25f;
        float r_decl = 0.25f;
        float cruise_step = 1.0f;
        double k_d = 0.1;
        double max_decl = 0.3;

        double HoverHeight = 0;
        double ForwardSpeed = 0;
        double VS = 0;
        double VS2 = 0;
        double D_H_ASL = 0;
        double C_H_ASL = 0;
        double P_H_ASL = 0;
        double Ang = 0;

        MatrixD ParkMatrix;
        public void Parking()
        {
            //Задаем базовые вектора относительно стыковочного порта корабля
            loc_up = -Vector3D.Normalize(Controller.GetNaturalGravity());
            fwd_hor = Vector3D.Normalize(Vector3D.Reject(connector.WorldMatrix.Forward, loc_up));
            right_hor = -loc_up.Cross(fwd_hor);

            //Считаем направление на точку стыковки по горизонтали
            Vector3D des_fwd_vec_hor = Vector3D.Normalize(Vector3D.Reject(ParkMatrix.Translation - connector.GetPosition(), loc_up));

            //Вычисляем текущую горизонтальную скорость
            my_hor_velocity = Vector3D.Reject(Controller.GetShipVelocities().LinearVelocity, loc_up);
            //из нее формируем "стоп-вектор"
            stop_vec = -my_hor_velocity * k_d;

            //Получаем расстояния от коннектора корабля до точки стыковки в координатах, привязанных к стыковочному положению
            Vector3D dst = ParkMatrix.Translation - connector.GetPosition();
            double DeltaH = (dst).Dot(ParkMatrix.Up);
            double DeltaR = (dst).Dot(ParkMatrix.Right);
            double DeltaF = (dst).Dot(ParkMatrix.Forward);

            //Высоту слегка подшаманиваем, чтобы спускаться по "глиссаде" к коннектору базы
            DeltaH = Math.Min(DeltaH + dst.Length() * 0.5, max_hover_h);

            //Считаем угол доворота по рысканию к направлению на точку стыковки
            double YawAngle = right_hor.Dot(des_fwd_vec_hor);
            if (fwd_hor.Dot(des_fwd_vec_hor) < 0)
            {
                YawAngle = Math.Sign(YawAngle);
            }

            //Опрашиваем ховер-движки о текущей высоте, усредняем ее.
            double DistanceToSurface = 0;
            foreach (IMyThrust H in HoverList)
            {
                DistanceToSurface += H.GetValue<double>("DistanceToSurface");
            }
            DistanceToSurface /= HoverList.Count;

            //Устанавливаем желаемую высоту для передачи в ховеры
            HoverHeight = DistanceToSurface + DeltaH;
            HoverHeight = Math.Max(Math.Min(HoverHeight, max_hover_h), min_hover_h);

            //Считаем желаемую скорость вперед относительно коннектора корабля
            ForwardSpeed = 0;
            if (Math.Abs(DeltaH) / Math.Max(Math.Abs(DeltaF), 1) < 1)
            //if (Math.Abs(DeltaR) / Math.Max(Math.Abs(DeltaF), 1) < 1)
            {
                ForwardSpeed = -Math.Min(Math.Max(DeltaF / 10, 1), 10);
            }
            //Считаем желаемую скорость вправо/влево для выхода на ось стыковки
            double RightSpeed = DeltaR / 10;

            //Считаем поправки к "стоп-вектору". Они дадут нам желаемую скорость для выхода на ось стыковки и сближения с портом базы
            input_F = fwd_hor * ForwardSpeed * k_d;
            input_R = right_hor * RightSpeed * k_d;
            //Задаем ось выравнивания для ховеров "balance_vec".
            balance_vec = stop_vec + input_R - input_F;
            if (balance_vec.Length() > max_decl) balance_vec = Vector3D.Normalize(balance_vec) * max_decl;
            balance_vec += loc_up;

            //A теперь из оси выравнивания ховеров и оси "вверх" КОКПита получаем кроссом вектор сигналов для гироскопов
            Vector3D rot = -Controller.WorldMatrix.Up.Cross(balance_vec); // + Controller.GetShipVelorities().AngularVelority * 2) * 10;

            //Добавляем к оси сигналов рыскание
            rot += Controller.WorldMatrix.Up * YawAngle;
            rot *= GyroMult;

            //Передаем сигналы иа гироскопы
            foreach (IMyGyro gyro in gyros)
            {
                gyro.Yaw = -(float)rot.Dot(gyro.WorldMatrix.Down);
                gyro.Pitch = (float)rot.Dot(gyro.WorldMatrix.Right);
                gyro.Roll = (float)rot.Dot(gyro.WorldMatrix.Backward);
            }

            //Устанавливаем высоту ховерам
            SetH((float)HoverHeight);

            //Проверяем условия окончания процедуры стыковки
            if (DeltaF < 0.5)
            {
                if (connector.Status == MyShipConnectorStatus.Connectable) connector.Connect();
                if (connector.Status == MyShipConnectorStatus.Connected) parking = false;
            }
        }
        public void Hover()
        {
            loc_up = -Vector3D.Normalize(Controller.GetNaturalGravity());
            fwd_hor = Vector3D.Normalize(Vector3D.Reject(Controller.WorldMatrix.Forward, loc_up));



            //my_vert_velocity = Controller.GetShipVelocities().LinearVelocity.Dot(loc_up);
            my_hor_velocity = Vector3D.Reject(Controller.GetShipVelocities().LinearVelocity, loc_up);
            stop_vec = -my_hor_velocity * k_d;


            double DistanceToSurface = 0;
            foreach (IMyThrust H in HoverList)
            {
                DistanceToSurface += H.GetValue<double>("DistanceToSurface");
            }
            DistanceToSurface /= HoverList.Count;


            if (keepASL)
            {
                D_H_ASL += Controller.MoveIndicator.Y * h_step;
                if (drill) D_H_ASL -= Controller.RollIndicator * h_step;
                Controller.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out C_H_ASL);
                HoverHeight = DistanceToSurface + (D_H_ASL - C_H_ASL);
                //double HR = P_H_ASL - C_H_ASL;
                //HoverHeight += HR;
            }
            else
            {
                HoverHeight += Controller.MoveIndicator.Y * h_step;
                if (drill) HoverHeight -= Controller.RollIndicator * h_step;
            }
            HoverHeight = Math.Max(Math.Min(HoverHeight, max_hover_h), min_hover_h);
            if (dir_con)
            {
                input_F = fwd_hor * Controller.MoveIndicator.Z * f_step;
                input_R = fwd_hor.Cross(loc_up) * Controller.RollIndicator * r_decl;
            }
            else
            {
                ForwardSpeed += Controller.MoveIndicator.Z * cruise_step;
                if (drill) ForwardSpeed = Math.Max(Math.Min(ForwardSpeed, max_speed_drill), -max_speed_drill);
                else ForwardSpeed = Math.Max(Math.Min(ForwardSpeed, max_speed_flight), -max_speed_flight);
                input_F = fwd_hor * ForwardSpeed * k_d;
                input_R = fwd_hor.Cross(loc_up) * Controller.RollIndicator * r_decl;
            }
            balance_vec = stop_vec + input_R - input_F;
            if (balance_vec.Length() > max_decl) balance_vec = Vector3D.Normalize(balance_vec) * max_decl;

            Rotate(loc_up + balance_vec);

            Controller.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out C_H_ASL);

            VS = VS * 0.9 + (C_H_ASL - P_H_ASL) * 6 * 0.1;
            VS2 = VS2 * 0.99 + (C_H_ASL - P_H_ASL) * 6 * 0.01;
            P_H_ASL = C_H_ASL;

            Ang = Math.Atan2(VS, my_hor_velocity.Length());


            double ZeroAccel = (Controller.GetNaturalGravity().Dot(Controller.WorldMatrix.Down));
            SetH((float)HoverHeight);
            LCD.WriteText(" Forward: " + Math.Round(-ForwardSpeed, 2)
                + "\n Altitude: " + Math.Round(HoverHeight, 2)
                + "\n [3] Drill: " + drill
                + "\n [4] Direct: " + dir_con
                + "\n [7] KeepASL: " + keepASL
                + "\n VS: " + Math.Round(VS, 2) + " | Ang: " + Math.Round(Ang * 180 / Math.PI, 2), false);
        }


        void Rotate(Vector3D target_vec)
        {
            Vector3D rot = -Controller.WorldMatrix.Up.Cross(target_vec); // + Controller.GetShipVelocities().AngularVelocity * 2) * 10;
            rot += Controller.WorldMatrix.Up * Controller.MoveIndicator.X;
            rot *= GyroMult;
            foreach (IMyGyro gyro in gyros)
            {
                gyro.Yaw = -(float)rot.Dot(gyro.WorldMatrix.Down);
                gyro.Pitch = (float)rot.Dot(gyro.WorldMatrix.Right);
                gyro.Roll = (float)rot.Dot(gyro.WorldMatrix.Backward);
            }
        }

        public void GyroOver(bool over)
        {
            foreach (IMyGyro gyro in gyros)
            {
                if (over) gyro.GyroPower = 1; else gyro.GyroPower = 0.1f;
                gyro.GyroOverride = over;
            }
        }

        public void SetH(float h)
        {
            foreach (IMyTerminalBlock hover in HoverList)
            {
                hover.SetValueFloat("Hover_MinHeight", h);
            }
        }
        //------------END--------------
    }
}