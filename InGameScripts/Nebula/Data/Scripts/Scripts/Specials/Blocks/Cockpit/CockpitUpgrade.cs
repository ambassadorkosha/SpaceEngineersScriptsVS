using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Digi;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Shared;
using Scripts.Specials.ShipClass;
using ServerMod;
using Shame.HoverEngine;
using Slime;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Specials.Blocks.Cockpit
{
    [ProtoContract]
    public class CockpitUpgradeParams
    {
        [ProtoMember(1)] public bool Align;
        [ProtoMember(2)] public bool AlignToSky;
        [ProtoMember(3)] public bool IsCruiseDamping;
        [ProtoMember(4)] public double GyroMultiplier = 1;
        [ProtoMember(5)] public bool DisableAfterAlign;
        [ProtoMember(6)] public bool AlignOnLeave;
        [ProtoMember(7)] public bool Break;
        [ProtoMember(8)] public bool RotateByAD;
        [ProtoMember(9)] public bool ControlHovers;
        [ProtoMember(10)] public bool Hold;
        [ProtoMember(11)] public double DesiredSpeed;
        [ProtoMember(12)] public bool Heli;
        [ProtoMember(13)] public bool HoldElevation;
        [ProtoMember(14)] public float f_step = 30f;
        [ProtoMember(15)] public float r_step = 30f;
        [ProtoMember(16)] public double max_decl = 0.3;
        [ProtoMember(17)] public bool TurnOffHold;
        [ProtoMember(18)] public bool TurnOffAlign;
        [ProtoMember(19)] public bool TurnOffHeli;
        [ProtoMember(20)] public bool TurnOffHeliEliv;
        [ProtoMember(21)] public bool TurnOffVecBrake;
        [ProtoMember(22)] public bool AutoMin;
        [ProtoMember(23)] public long LastMainController;
    }
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), true)]
    public class CockpitUpgrade_Cockpits : CockpitUpgrade { }
//    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RemoteControl), true)]
//    public class CockpitUpgrade_RemoteControls : CockpitUpgrade { }
    public class CockpitUpgrade : MyGameLogicComponent
    {
        private static Connection<Converter> connection;
        [ProtoContract]
        private class Converter
        {
            [ProtoMember(1)] public long CockpitId;
            [ProtoMember(2)] public bool TargetState;
        }
        private static Sync<CockpitUpgradeParams, CockpitUpgrade> Sync;
        private static readonly Guid guid = new Guid("5eef512e-b69e-4040-96d5-d367fc57ec8b");
        private CockpitUpgradeParams Set;
        
        private static bool INITIATED_CONTROLS;
        private bool INITIATED;

        private bool Sorted;
        
        private Ship Ship;
        private IMyShipController Controller;
        private HashSet<MyShipController> Cockpits;
        private HashSet<IMyThrust> ThrusterList;
        private readonly HashSet<IMyThrust> ThrustFW = new HashSet<IMyThrust>();
        private readonly HashSet<IMyThrust> SecThrustFW = new HashSet<IMyThrust>();
        private readonly HashSet<IMyThrust> ThrustUP = new HashSet<IMyThrust>();
        private readonly HashSet<IMyThrust> SecThrustUP = new HashSet<IMyThrust>();
        
        private HashSet<IMyGyro> Gyros;
        private Vector3D Rotation = Vector3D.Zero;
        
        private bool StopCalcHold;
        private bool StopCalcAlign;
        private bool StopCalcHeli;
        private static bool CaughtError;
        
        private const float Max_Speed = 300f;

        public static void Init()
        {
            Sync = new Sync<CockpitUpgradeParams, CockpitUpgrade>(38456, sz => sz.Set, (sz, NewSettings, PlayerSteamId, isFromServer) =>
                {
                    if (MyAPIGateway.Session.IsServer)
                    {
                        sz.Set = NewSettings;
                        sz.Entity.SetStorageData(guid, sz.Set);
                    }
                    else
                    {
                        sz.Set = NewSettings;
                    }
                });
            connection = new Connection<Converter>(38457, ConvertHandler);
        }
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (Controller == null) Controller = Entity as IMyShipController;
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            
            if (INITIATED_CONTROLS) return;
            INITIATED_CONTROLS = true;
            
            InitControls<IMyCockpit>();
            InitActions<IMyCockpit>();
            InitRemoveActions<IMyCockpit>();
            
//            InitControls<IMyRemoteControl>();
//            InitActions<IMyRemoteControl>();
        }
        private void SendState(bool Share = true)
        {
            if (MyAPIGateway.Multiplayer.IsServer) Entity.SetStorageData(guid, Set);
            else Sync.SendMessageToOthers(Entity.EntityId, Set);
            if (Share) ShareToAllCockpitsOnGrid();
        }
        private static void ConvertHandler(Converter request, ulong SteamUserId, bool isFromServer)
        {
            try
            {
                var ent = request.CockpitId.As<IMyEntity>();
                if (ent == null) return;

                var Cockpit = ent as IMyShipController;
                if (Cockpit == null) return;

                if (isFromServer)
                {
                    if (request.TargetState) ((MyCubeGrid) Cockpit.CubeGrid).ConvertToStatic();
                    else                     ((MyCubeGrid) Cockpit.CubeGrid).OnConvertToDynamic();
                }
                else
                {
                    var Identity = SteamUserId.Identity();
                    if (Identity == null) return;
                    
                    if (!Cockpit.HasPlayerAccess(Identity.IdentityId)) return;
                    if (request.TargetState) ((MyCubeGrid) Cockpit.CubeGrid).ConvertToStatic();
                    else                     ((MyCubeGrid) Cockpit.CubeGrid).OnConvertToDynamic();
                    connection.SendMessageToOthers(new Converter {CockpitId = Cockpit.EntityId, TargetState = Cockpit.CubeGrid.IsStatic});
                }
            }
            catch (Exception e)
            {
                Log.ChatError("CockpitUpgrade::HandleConvert Error: " + e);
            }
        }
        public override void UpdateOnceBeforeFrame()
        {
            if (INITIATED) return;
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                if (!Entity.TryGetStorageData(guid, out Set)) Set = new CockpitUpgradeParams();
            }
            else
            {
                Set = Set ?? new CockpitUpgradeParams();
                Sync.RequestData(Entity.EntityId);
            }
            if (Controller != null) INITIATED = true;
        }
        public override void UpdateBeforeSimulation()
        {
            try { logic(); }
            catch (Exception e) { Log.ChatError("CockpitUpgrade::logic(): " + e); }
        }
        private void logic()
        {
            if (Controller.CubeGrid.Physics == null || !Controller.CubeGrid.Physics.Enabled) return;

            if (GameBase.instance?.gridToShip == null) { Log.Error("CockpitUpgrade::Logic(): Instance or gridToShip null"); return; }

            Ship = Controller.CubeGrid.GetShip();
            if (Ship == null) return;

            if (isMain()) Set.LastMainController = Controller.EntityId;
            
            Gyros = Ship.gyros;
            ThrusterList = Ship.thrusters;
            Cockpits = Ship.Cockpits;

            if (Set.LastMainController != Controller.EntityId || !Controller.IsFunctional || !Controller.IsWorking) return;

            if (Ship.SortThrusters || !Sorted) SortThrusters();

            if (Gyros != null)
            {
                AlignToHorizon();
                VectorBreak();
                AutoHeli();
            }
            
            if (StopCalcHold && Controller.Pilot != null) StopCalcHold = false;
            
            if (!StopCalcHold) HoldSpeed();
            if (Ship.Hovers.Count != 0 && Controller.Pilot != null) SetHoverHeight();

            if (!CaughtError) return;
            Set.Hold = false;
            Set.HoldElevation = false;
            CaughtError = false;
            SetAcc(ThrusterList);
        }
        private void AlignToHorizon()
        {
            if (Set.TurnOffAlign)
            {
                AlignGyros(Vector3D.Zero);
                Set.Align = false;
                Set.TurnOffAlign = false;
                return;
            }
            if (Controller.Pilot == null && Set.AlignOnLeave) Set.Align = true;

            if (!Set.Align) return;

            var Gravity = Vector3.Normalize(Controller.CubeGrid.Physics.Gravity);

            if (!Gravity.IsValid())
            {
                Set.TurnOffAlign = true;
                return;
            }

            var Direction = !Set.AlignToSky ? Controller.WorldMatrix.Up : Controller.WorldMatrix.Forward;
            Rotation = Direction.Cross(Gravity);

            if (StopCalcAlign && Rotation.Length() > 0.002f) StopCalcAlign = false;
            if (StopCalcAlign) return;
            
            if (Controller.Pilot == null && Rotation.Length() < 0.002f)
            {
                if (Set.AlignOnLeave) Set.TurnOffAlign = true;
                else
                {
                    AlignGyros(Vector3D.Zero);
                    StopCalcAlign = true;
                }
                return;
            }

            if (Set.DisableAfterAlign && Rotation.Length() < 0.002f)
            {
                Set.TurnOffAlign = true;
                return;
            }
            
            if (Controller.Pilot != null) Rotation += !Set.AlignToSky ? Controller.WorldMatrix.Up * Controller.RotationIndicator.Y : Vector3D.Zero;
            
            AlignGyros(Rotation, true);
        }
        private void VectorBreak()
        {
            if (Set.TurnOffVecBrake || Set.Break && Controller.GetShipSpeed() <= 0.001f)
            {
                AlignGyros(Vector3D.Zero);
                Controller.DampenersOverride = true;
                Set.Break = false;
                Set.TurnOffVecBrake = false;
                return;
            }
            if (!Set.Break) return;
            
            Rotation = Controller.WorldMatrix.Forward.Cross(Vector3D.Normalize(Controller.CubeGrid.Physics.LinearVelocity));
            AlignGyros(Rotation, true);
            if(Rotation.Length() <= 0.002f && Controller.CubeGrid.Physics.AngularVelocity.Length() < 0.001f) Controller.DampenersOverride = true;
        }
        private void HoldSpeed()
        {
            if (Set.TurnOffHold)
            {
                SetAcc(ThrustFW,SecThrustFW);
                Set.TurnOffHold = false;
                Set.Hold = false;
                return;
            }
            
            if (!Set.Hold || Set.Heli) return;
            
            if (Controller.Pilot == null)
            {
                SetAcc(ThrustFW,SecThrustFW);
                StopCalcHold = true;
                return;
            }
            
            var elevation = Ship.getElevation2();
            if (elevation < 0) { elevation = 9999999d; }
            var IsLarge = Controller.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
            var min = AirFriction.getFrictionStart(IsLarge, AirFriction.getElevation(elevation));
            double GravityProjection = 0;
            if (Controller.CubeGrid.Physics.Gravity.IsValid()) GravityProjection = Controller.WorldMatrix.Backward.Dot(Controller.CubeGrid.Physics.Gravity);
            var DesSpeed = Set.AutoMin ? min : Set.DesiredSpeed;
            var acceleration = (DesSpeed - 0.1) - Controller.WorldMatrix.Forward.Dot(Controller.CubeGrid.Physics.LinearVelocity);
            var Friction = Controller.WorldMatrix.Forward.Dot(Ship.CurrentCockpitWingStopForce) + Controller.WorldMatrix.Forward.Dot(-Ship.CurrentAirFriction);
            var Force = Ship.massCache.shipMass * (GravityProjection + acceleration) + Friction;
            
            SetAcc(ThrustFW,SecThrustFW, Force, Set.IsCruiseDamping);
        }
        private void SortThrusters()
        {
            ThrustFW.Clear();
            SecThrustFW.Clear();
            ThrustUP.Clear();
            SecThrustUP.Clear();
            
            foreach (var t in ThrusterList)
            {
                if (t.WorldMatrix.Forward.Dot(Controller.WorldMatrix.Backward) > 0.99) {
                    var id = t.BlockDefinition.SubtypeName;
                    if (id.Contains("Hydrogen") || id.Contains("Kerosene")) SecThrustFW.Add(t);
                    else ThrustFW.Add(t);
                } 
                else if (t.WorldMatrix.Forward.Dot(Controller.WorldMatrix.Down) > 0.99)
                {
                    if (!ShipHoverDrives.IsHover(t))
                    {
                        var id = t.BlockDefinition.SubtypeName;
                        if (id.Contains("Hydrogen") || id.Contains("Kerosene")) SecThrustUP.Add(t);
                        else ThrustUP.Add(t);
                    }
                }
            }
            Ship.SortThrusters = false;
            Sorted = true;
        }
        private void SetHoverHeight()
        {
            if (!Set.ControlHovers) return;
            foreach (var hover in Ship.Hovers.Select(_hover => _hover.GameLogic.GetAs<HoverEngine>()))
            {
                hover.SetHeight(hover.heightTargetMax + (float) (Controller.MoveIndicator.Y * 0.04), null);
            }
        }
        private void AlignGyros(Vector3D a, bool over = false)
        {
            try
            {
                if (Gyros == null) return;
                
                if (Set.RotateByAD) a += Controller.WorldMatrix.Up * Controller.MoveIndicator.X;
                a *= Set.GyroMultiplier;

                foreach (var gyro in Gyros.Where(gyro => !gyro.CustomName.Contains("CUIgnore")))
                {
                    if (over) gyro.GyroOverride = true;

                    var m = gyro.WorldMatrix;

                    gyro.Yaw = -(float) a.Dot(m.Down);
                    gyro.Pitch = (float) a.Dot(m.Right);
                    gyro.Roll = (float) a.Dot(m.Backward);

                    if (!over) gyro.GyroOverride = false;
                }
            }
            catch (Exception e)
            {
                Log.ChatError("CockpitUpgrade::AlignGyros: " + e);
            }
        }
        private void AutoHeli()
        {
            if (Set.TurnOffHeli)
            {
                AlignGyros(Vector3D.Zero);
                SetAcc(ThrustUP,SecThrustUP);
                Set.Heli = false;
                Set.TurnOffHeli = false;
                return;
            }

            if (!Set.Heli) return;
            
            var TempRot = Controller.WorldMatrix.Up.Cross(Vector3D.Normalize(Controller.CubeGrid.Physics.Gravity));
            if (Controller.GetShipSpeed() > 1f || TempRot.Length() > 0.002f) StopCalcHeli = false;
            if (StopCalcHeli) return;
            
            if (Controller.Pilot == null && Controller.GetShipSpeed() < 0.1f && Rotation.Length() <= 0.002f)
            {
                AlignGyros(Vector3D.Zero);
                SetAcc(ThrustUP,SecThrustUP);
                StopCalcHeli = true;
                return;
            }

            if (!Controller.CubeGrid.Physics.Gravity.IsValid())
            {
                Set.TurnOffHeli = true;
                return;
            }
            AutoHeliF();
        }
        private void AutoHeliF()
        {
            const double k_d = 0.1;

            var controllerMatrix = Controller.WorldMatrix;

            var loc_up = -Vector3D.Normalize(Controller.CubeGrid.Physics.Gravity);
            var fwd_hor = Vector3D.Normalize(Vector3D.Reject(controllerMatrix.Forward, loc_up));
            var Speed = Controller.CubeGrid.Physics.LinearVelocity;
            if (Math.Abs(Set.f_step) <= 0 || Math.Abs(Set.r_step) <= 0)
            {
                var ch = false;
                if (Math.Abs(Set.f_step) <= 0 && Math.Abs(Set.r_step) <= 0)
                {
                    Speed = Vector3D.Zero;
                    ch = true;
                }
                if (!ch && Math.Abs(Set.f_step) <= 0) Speed = controllerMatrix.Right * controllerMatrix.Right.Dot(Speed);
                if (!ch && Math.Abs(Set.r_step) <= 0) Speed = controllerMatrix.Forward * controllerMatrix.Forward.Dot(Speed);
            }

            var my_hor_velocity = Vector3D.Reject(Speed, loc_up);
            double MoveIndicator;
            if (!Set.Hold) MoveIndicator = Controller.MoveIndicator.Z;
            else MoveIndicator = -1;
            
            var input_F = fwd_hor * MoveIndicator * Set.f_step / 10;
            var input_R = fwd_hor.Cross(loc_up) * Controller.RollIndicator * Set.r_step / 10;
            var stop_vec = -my_hor_velocity * k_d;
            var balance_vec = stop_vec + input_R - input_F;
            if (balance_vec.Length() > Set.max_decl) balance_vec = Vector3D.Normalize(balance_vec) * Set.max_decl;

            Rotation = -controllerMatrix.Up.Cross(loc_up + balance_vec);
            Rotation += controllerMatrix.Up * Controller.RotationIndicator.Y;
            Rotation += controllerMatrix.Right * Controller.RotationIndicator.X;

            AlignGyros(Rotation, true);

            if (Set.TurnOffHeliEliv)
            {
                SetAcc(ThrustUP, SecThrustUP);
                Set.HoldElevation = false;
                Set.TurnOffHeliEliv = false;
                return;
            }
            if (!Set.HoldElevation) return;
            if (Math.Abs(Controller.MoveIndicator.Y) > 0)
            {
                SetAcc(ThrustUP, SecThrustUP);
                return;
            }
            var ZeroAcceleration = controllerMatrix.Down.Dot(Controller.CubeGrid.Physics.Gravity);
            var HoverCorrection = Vector3D.Normalize(Controller.CubeGrid.Physics.Gravity).Dot(Controller.CubeGrid.Physics.LinearVelocity) * Controller.CubeGrid.Physics.LinearVelocity.Length();

            var acc = ZeroAcceleration + HoverCorrection;
            var Force = (float) acc * Ship.massCache.shipMass;
            SetAcc(ThrustUP, SecThrustUP, Force);
        }
        private static void SetAcc(HashSet<IMyThrust> MainThrustList = null,HashSet<IMyThrust> SecThrustList = null, double Force = 0, bool IsDamping = false)
        {
            try
            {
                if (CaughtError) return;
                var Thrust_percentage = 0f;
                var MainThrustFWEffSum = 0f;
                
                if (MainThrustList != null)
                {
                    var EnabledMainThrustList = new List<IMyThrust>();
                    foreach (var Thruster in MainThrustList.TakeWhile(t => t != null && !t.CustomName.Contains("CUIgnore")))
                    {
                        if (!Thruster.IsFunctional) continue;
                        if (Thruster.Enabled)
                        {
                            if (Thruster.MaxEffectiveThrust <= 0)
                            {
                                Thruster.ThrustOverridePercentage = 0;
                                continue;
                            }
                            MainThrustFWEffSum += Thruster.MaxEffectiveThrust;
                            EnabledMainThrustList.Add(Thruster);
                        }
                        else
                        {
                            Thruster.ThrustOverridePercentage = 0;
                        }
                    }

                    if (MainThrustFWEffSum > 0f)
                    {
                        if (Force > 0.0 && EnabledMainThrustList.Count != 0)
                        {
                            Thrust_percentage = (float) Force / MainThrustFWEffSum;
                            if (Thrust_percentage > 1.0f) Thrust_percentage = 1.0f;
                        }
                        if (!IsDamping && Force < 0f) Thrust_percentage = 0.0000001f;
                        foreach (var t in EnabledMainThrustList)
                        {
                            t.ThrustOverridePercentage = Thrust_percentage;
                        }
                    }
                }
                
                if (Thrust_percentage > 0f && Thrust_percentage < 1.0f) return;
                
                if (SecThrustList != null)
                {
                    var EnabledSecThrustList = new List<IMyThrust>();
                    var SecThrust_Percentage = 0f;
                    var SecThrustFWEffSum = 0f;

                    foreach (var Thruster in SecThrustList.TakeWhile(t => t != null && !t.CustomName.Contains("CUIgnore")))
                    {
                        if (!Thruster.IsFunctional) continue;
                        if (Thruster.Enabled)
                        {
                            SecThrustFWEffSum += Thruster.MaxEffectiveThrust;
                            EnabledSecThrustList.Add(Thruster);
                        }
                        else
                        {
                            Thruster.ThrustOverridePercentage = 0;
                        }
                    }
                    if (Force > 0.0 && EnabledSecThrustList.Count != 0)
                    {
                        SecThrust_Percentage = (float) (Force - MainThrustFWEffSum) / SecThrustFWEffSum;
                        if (SecThrust_Percentage > 1.0f) SecThrust_Percentage = 1.0f;
                    }
                    foreach (var t in EnabledSecThrustList)
                    {
                        t.ThrustOverridePercentage = SecThrust_Percentage;
                    }
                }
            }
            catch (Exception e)
            {
                Log.ChatError("Thrusters override disabled on error " + "CockpitUpgrade::SetAcc: " + e);
                CaughtError = true;
            }
        }
        private static void GridConvert(MyCubeGrid Grid, IMyTerminalBlock block)
        {
            try
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    if (Grid.IsStatic) Grid.OnConvertToDynamic();
                    else Grid.ConvertToStatic();
                    connection.SendMessageToOthers(new Converter {CockpitId = block.EntityId, TargetState = Grid.IsStatic});
                }
                else connection.SendMessageToServer(new Converter {CockpitId = block.EntityId, TargetState = !Grid.IsStatic});
            }
            catch (Exception e)
            {
                Log.ChatError("CockpitUpgrade::GridConvert: " + e);
                throw;
            }
        }
        private bool isMain()
        {
            return Controller.IsFunctional && Controller.ControllerInfo?.Controller?.ControlledEntity != null;
        }

        private void ShareToAllCockpitsOnGrid()
        {
            foreach (var _c in Cockpits.Where(c => c != Controller))
            {
                var c = _c as IMyCockpit;
                if (c == null) continue;
                c.GetAs<CockpitUpgrade>().Set = Set;
                c.GetAs<CockpitUpgrade>().SendState(false);
            }
        }

        private static void InitControls<IMy>()
        {
            PlaceSeparateArea<IMy>();

            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_Align", "Align", "Выравнивание", (b) => b.Set.Align,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.Align = v;
                    if (b.Set.Align)
                    {
                        if(b.Set.Break)b.Set.TurnOffVecBrake = true;
                        if(b.Set.Heli)b.Set.TurnOffHeli = true;
                    }
                    else
                    {
                        b.Set.TurnOffAlign = true;
                    }
                    b.SendState(); },update:true);

            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_AlignToSky","Align To Horizon or Sky","Выровнять по горизонту или небу",(b) => b.Set.AlignToSky,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.AlignToSky = v;
                    b.SendState(); },update:true);

            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_DisableAfterAlign","Disable Align after alignment","Отключить выравнивание после того как выравнивнит",(b) => b.Set.DisableAfterAlign,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.DisableAfterAlign = v;
                    b.SendState(); },update:true);
            
            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_AlignOnLeave","Align when leaving the cab","Выровнять когда покину кабину",(b) => b.Set.AlignOnLeave,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.AlignOnLeave = v;
                    b.SendState(); },update:true);
            
            PlaceSeparateArea<IMy>();
            
            MyAPIGateway.TerminalControls.CreateSlider<CockpitUpgrade, IMy>("Cockpit_GyroMultiplier","Align and Helicopter mode mult.","Мультипликатор гироскопов для выравнивание и вертолетного режима", 0.1f,5f,
                (b) => (float) b.Set.GyroMultiplier,
                (b, t) => t.Append(b.Set.GyroMultiplier).Append(""),
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.GyroMultiplier = Math.Round(v, 2);
                    b.SendState();},update:true);
            
            PlaceSeparateArea<IMy>();

            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_VectorBreak","Velocity damping by speed vector","Гашение скорости по вектору скорости",(b) => b.Set.Break,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.Break = v;
                    if (b.Set.Break)
                    {
                        b.Controller.DampenersOverride = false;
                        if (b.Set.Align) b.Set.TurnOffAlign = true;
                        if (b.Set.Hold) b.Set.TurnOffHold = true;
                    }
                    else b.Set.TurnOffVecBrake = true;
                    b.SendState(); },update:true);
            
            PlaceSeparateArea<IMy>();

            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_ControlHovers", "Control hovers height by keyboard", "Контроль высоты зависания с помощью клавиатуры", (b) => b.Set.ControlHovers,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.ControlHovers = v;
                    b.SendState(); },update:true);
            
            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_ChangeRotationToAD","Change Rotation To 'A' 'D'","Сменит повороты на кнопки 'A' 'D'",(b) => b.Set.RotateByAD,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.RotateByAD = v;
                    b.SendState();},update:true);
            
            PlaceSeparateArea<IMy>();
            
            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_HoldSpeed","Cruise control","Круиз контроль",(b) => b.Set.Hold,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.Hold = v;
                    if (!b.Set.Hold)
                    {
                        b.Set.TurnOffVecBrake = true;
                        b.Set.TurnOffHold = true;
                    }
                    b.SendState();},update:true);
            
            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_AutoMin","Cruise Auto optimal speed","Авто оптимальная скорость",(b) => b.Set.AutoMin,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.AutoMin = v;
                    b.SendState();},update:true);
            
            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_CruiseDamp","Cruise Dampeners","Гасители круиза",(b) => b.Set.IsCruiseDamping,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.IsCruiseDamping = v;
                    b.SendState();},update:true);
            
            MyAPIGateway.TerminalControls.CreateSlider<CockpitUpgrade, IMy>("Cockpit_MaxSpeed","Desired Speed","Желаемая скорость",0f,Max_Speed,
                (b) => (float) b.Set.DesiredSpeed,
                (b, t) => t.Append(b.Set.DesiredSpeed).Append(""),
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.DesiredSpeed = Math.Round(v, 2);
                    b.SendState();},update:true);
            
            PlaceSeparateArea<IMy>();

            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_AutoHelicopter","Helicopter mode","Вертолетный режим",(b) => b.Set.Heli,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.Heli = v;
                    if (b.Set.Heli)
                    {
                        if (b.Set.Align) b.Set.TurnOffAlign = true;
                        if (b.Set.Hold) b.Set.TurnOffHold = true;
                    }
                    else
                    {
                        b.Set.TurnOffHeli = true;
                        b.Set.TurnOffHold = true;
                    }
                    b.SendState();},update:true);

            MyAPIGateway.TerminalControls.CreateCheckbox<CockpitUpgrade, IMy>("Cockpit_HoldElevation",
                "Helicopter Altitude Support", "Поддержка высоты для вертолетного режима", (b) => b.Set.HoldElevation,
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.HoldElevation = v;
                    if (!b.Set.HoldElevation) b.Set.TurnOffHeliEliv = true;
                    b.SendState();},update:true);
            
            MyAPIGateway.TerminalControls.CreateSlider<CockpitUpgrade, IMy>("HeliCockpit_Kv","Auto Heli Max forward speed","Максимальная скорость движения вперед для вертолетного режима",0f,Max_Speed,
                (b) => b.Set.f_step,
                (b, t) => {
                    if (Math.Abs(b.Set.f_step) <= 0) t.Append("Off").Append("");
                    else  t.Append(b.Set.f_step).Append(""); },
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.f_step = (float) Math.Round(v, 2);
                    b.SendState(); },update:true);

            MyAPIGateway.TerminalControls.CreateSlider<CockpitUpgrade, IMy>("HeliCockpit_Ka","Auto Heli Max rear speed","Максимальная боковая скорость движения для вертолетного режима",0,Max_Speed,
                (b) => b.Set.r_step,
                (b, t) => {
                    if (Math.Abs(b.Set.r_step) <= 0) t.Append("Off").Append("");
                    else t.Append(b.Set.r_step).Append(""); },
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.r_step = (float) Math.Round(v, 2);
                    b.SendState();},update:true);
            
            MyAPIGateway.TerminalControls.CreateSlider<CockpitUpgrade, IMy>("HeliCockpit_MaxDecl","Auto Heli Max Declination","Максимальный наклон для вертолетного режима",0.1f, 0.7f,
                (b) => (float) b.Set.max_decl,
                (b, t) => t.Append(b.Set.max_decl).Append(""),
                (b, v) => {
                    if (!b.isMain()) return;
                    b.Set.max_decl = Math.Round(v, 2);
                    b.SendState();},update:true);
        }
        private static void InitActions<IMy>()
        {
            var ToStationAndBack = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ToStationAndBack");
            var AlignOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionAlign_OnOff");
            var AlignToSkyOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionAlignToSky_OnOff");
            var CruiseDampenersOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionCruiseDampenersOnOff");
            var DisableAfterAlignOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionDisableAfterAlign_OnOff");
            var AlignOnLeaveOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionAlignOnLeave_OnOff");
            var BreakOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionBreak_OnOff");
            var ControlHoversOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionControlHovers_OnOff");
            var ChangeRotationToAD = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ChangeRotationToAD_OnOff");
            var HoldSpeedOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionHoldSpeed_OnOff");
            var AutoMinOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionAutoMin_OnOff");
            var AutoHelicopterOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionAutoHelicopterOnOff");
            var HoldElevationOnOff = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ActionHoldElevationOnOff");
            var DesiredSpeed_Inc = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_DesiredSpeedIncrease");
            var DesiredSpeed_Dec = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_DesiredSpeedDecrease");
            var ThrustOverToZero = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_ThrustOverToZero");
            var GyroOverToZero = MyAPIGateway.TerminalControls.CreateAction<IMy>("Cockpit_GyroOverToZero");
            
            ToStationAndBack.Action = (b) =>
            {
                if (!b.GetAs<CockpitUpgrade>().isMain()) return;
                GridConvert((MyCubeGrid) b.CubeGrid, b);
            };
            ToStationAndBack.Name = new StringBuilder("Ship/Station");
            ToStationAndBack.Icon = @"Textures\GUI\Icons\Actions\LargeShipToggle.dds";
            ToStationAndBack.Writer = (b, sb) =>
            {
                var gl = b.GetAs<CockpitUpgrade>().Controller.CubeGrid;
                sb.Append(gl.IsStatic ? "Station" : gl.Physics.IsStatic ? "Parked" : "Ship");
            };
            ToStationAndBack.ValidForGroups = true;
            ToStationAndBack.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            AlignOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.Align = !gl.Set.Align;
                if (gl.Set.Align)
                {
                    if(gl.Set.Break)gl.Set.TurnOffVecBrake = true;
                    if(gl.Set.Heli)gl.Set.TurnOffHeli = true;
                }
                else
                {
                    gl.Set.TurnOffAlign = true;
                }
                gl.SendState();
            };
            AlignOnOff.Name = new StringBuilder("Align On/Off");
            AlignOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            AlignOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.Align ? "Align On" : "Align Off");
            AlignOnOff.ValidForGroups = true;
            AlignOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            AlignToSkyOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.AlignToSky = !gl.Set.AlignToSky;
                gl.SendState();
            };
            AlignToSkyOnOff.Name = new StringBuilder("Align To Horizon/Sky On/Off");
            AlignToSkyOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            AlignToSkyOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.AlignToSky ? "Sky" : "Horizon");
            AlignToSkyOnOff.ValidForGroups = true;
            AlignToSkyOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            DisableAfterAlignOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.DisableAfterAlign = !gl.Set.DisableAfterAlign;
                gl.SendState();
            };
            DisableAfterAlignOnOff.Name = new StringBuilder("Disable Align after alignment On/Off");
            DisableAfterAlignOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            DisableAfterAlignOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.DisableAfterAlign ? "DaA On" : "DaA Off");
            DisableAfterAlignOnOff.ValidForGroups = true;
            DisableAfterAlignOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            AlignOnLeaveOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.AlignOnLeave = !gl.Set.AlignOnLeave;
                gl.SendState();
            };
            AlignOnLeaveOnOff.Name = new StringBuilder("Align when leaving the cab On/Off");
            AlignOnLeaveOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            AlignOnLeaveOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.AlignOnLeave ? "AoL On" : "AoL Off");
            AlignOnLeaveOnOff.ValidForGroups = true;
            AlignOnLeaveOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            BreakOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.Break = !gl.Set.Break;
                if (gl.Set.Break)
                {
                    gl.Controller.DampenersOverride = false;
                    if (gl.Set.Align) gl.Set.TurnOffAlign = true;
                    if (gl.Set.Hold) gl.Set.TurnOffHold = true;
                }
                else gl.Set.TurnOffVecBrake = true;
                gl.SendState();
            };
            BreakOnOff.Name = new StringBuilder("Velocity damping by speed vector On/Off");
            BreakOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            BreakOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.Break ? "VB On" : "VB Off");
            BreakOnOff.ValidForGroups = true;
            BreakOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            ControlHoversOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.ControlHovers = !gl.Set.ControlHovers;
                gl.SendState();
            };
            ControlHoversOnOff.Name = new StringBuilder("Control hovers height by keyboard On/Off");
            ControlHoversOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            ControlHoversOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.ControlHovers ? "CH On" : "CH Off");
            ControlHoversOnOff.ValidForGroups = true;
            ControlHoversOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            ChangeRotationToAD.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.RotateByAD = !gl.Set.RotateByAD;
                gl.SendState();
            };
            ChangeRotationToAD.Name = new StringBuilder("Change Rotation To 'A' 'D' On/Off");
            ChangeRotationToAD.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            ChangeRotationToAD.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.RotateByAD ? "Rot: A D" : "Rot: < >");
            ChangeRotationToAD.ValidForGroups = true;
            ChangeRotationToAD.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            HoldSpeedOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.Hold = !gl.Set.Hold;
                if (!gl.Set.Hold) gl.Set.TurnOffHold = true;
                gl.SendState();
            };
            HoldSpeedOnOff.Name = new StringBuilder("Cruise control On/Off");
            HoldSpeedOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            HoldSpeedOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.Hold ? "HS On" : "HS Off");
            HoldSpeedOnOff.ValidForGroups = true;
            HoldSpeedOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;
            
            CruiseDampenersOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.IsCruiseDamping = !gl.Set.IsCruiseDamping;
                gl.SendState();
            };
            CruiseDampenersOnOff.Name = new StringBuilder("Cruise Dampeners On/Off");
            CruiseDampenersOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            CruiseDampenersOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.IsCruiseDamping ? "CD On" : "CD Off");
            CruiseDampenersOnOff.ValidForGroups = true;
            CruiseDampenersOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            AutoHelicopterOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.Heli = !gl.Set.Heli;
                if (gl.Set.Heli)
                {
                    gl.Set.TurnOffAlign = true;
                    gl.Set.TurnOffHold = true;
                }
                else
                {
                    gl.Set.TurnOffHeli = true;
                    gl.Set.TurnOffHold = true;
                }
                gl.SendState();
            };
            AutoHelicopterOnOff.Name = new StringBuilder("Helicopter mode On/Off");
            AutoHelicopterOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            AutoHelicopterOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.Heli ? "Heli On" : "Heli Off");
            AutoHelicopterOnOff.ValidForGroups = true;
            AutoHelicopterOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            HoldElevationOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.HoldElevation = !gl.Set.HoldElevation;
                if(!gl.Set.HoldElevation)
                {
                    gl.Set.TurnOffHeliEliv = true;
                }
                gl.SendState();
            };
            HoldElevationOnOff.Name = new StringBuilder("Helicopter Altitude Support On/Off");
            HoldElevationOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            HoldElevationOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.HoldElevation ? "HeAS On" : "HeAS Off");
            HoldElevationOnOff.ValidForGroups = true;
            HoldElevationOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;
            
            AutoMinOnOff.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.AutoMin = !gl.Set.AutoMin;
                gl.SendState();
            };
            AutoMinOnOff.Name = new StringBuilder("Cruise Auto optimal speed");
            AutoMinOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            AutoMinOnOff.Writer = (b, sb) => sb.Append(b.GetAs<CockpitUpgrade>().Set.AutoMin ? "Auto On" : "Auto Off");
            AutoMinOnOff.ValidForGroups = true;
            AutoMinOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            DesiredSpeed_Inc.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.DesiredSpeed += 10;
                gl.Set.DesiredSpeed = MathHelper.Clamp(gl.Set.DesiredSpeed, 0, Max_Speed);
                gl.SendState();
            };
            DesiredSpeed_Inc.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            DesiredSpeed_Inc.Writer = (b, sb) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (gl.Set.Heli) sb.Append("HELI");
                else if (gl.Set.AutoMin) sb.Append("Auto");
                else sb.Append(gl.Set.DesiredSpeed.toHumanQuantity() + " m");
            };
            DesiredSpeed_Inc.Name = new StringBuilder("Increase of desired speed ");
            DesiredSpeed_Inc.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            DesiredSpeed_Dec.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                gl.Set.DesiredSpeed -= 10;
                gl.Set.DesiredSpeed = MathHelper.Clamp(gl.Set.DesiredSpeed, 0, Max_Speed);
                gl.SendState();
            };
            DesiredSpeed_Dec.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            DesiredSpeed_Dec.Name = new StringBuilder("Decrease of desired speed");
            DesiredSpeed_Dec.Writer = (b, sb) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (gl.Set.Heli) sb.Append("HELI");
                else if (gl.Set.AutoMin) sb.Append("Auto");
                else sb.Append(gl.Set.DesiredSpeed.toHumanQuantity() + " m");
            };
            DesiredSpeed_Dec.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            ThrustOverToZero.Action = (b) =>
            {
                var gl = b.GetAs<CockpitUpgrade>();
                if (!gl.isMain()) return;
                SetAcc(gl.ThrusterList);
            };
            ThrustOverToZero.Name = new StringBuilder("Restore thrust override");
            ThrustOverToZero.Icon = @"Textures\GUI\Icons\Actions\Reset.dds";
            ThrustOverToZero.Writer = (b, sb) => sb.Append("Restore\noverride");
            ThrustOverToZero.ValidForGroups = true;
            ThrustOverToZero.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            GyroOverToZero.Action = (b) => b.GetAs<CockpitUpgrade>().AlignGyros(Vector3D.Zero);
            GyroOverToZero.Name = new StringBuilder("Restore gyros override");
            GyroOverToZero.Icon = @"Textures\GUI\Icons\Actions\Reset.dds";
            GyroOverToZero.Writer = (b, sb) => sb.Append("Restore\noverride");
            GyroOverToZero.ValidForGroups = true;
            GyroOverToZero.Enabled = (b) => b.GetAs<CockpitUpgrade>() != null;

            MyAPIGateway.TerminalControls.AddAction<IMy>(AlignOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(AlignToSkyOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(DisableAfterAlignOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(AlignOnLeaveOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(BreakOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(AutoHelicopterOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(HoldElevationOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(ControlHoversOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(ChangeRotationToAD);
            MyAPIGateway.TerminalControls.AddAction<IMy>(HoldSpeedOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(AutoMinOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(CruiseDampenersOnOff);
            MyAPIGateway.TerminalControls.AddAction<IMy>(DesiredSpeed_Inc);
            MyAPIGateway.TerminalControls.AddAction<IMy>(DesiredSpeed_Dec);
            MyAPIGateway.TerminalControls.AddAction<IMy>(ThrustOverToZero);
            MyAPIGateway.TerminalControls.AddAction<IMy>(GyroOverToZero);
            MyAPIGateway.TerminalControls.AddAction<IMy>(ToStationAndBack);
        }
        private static void InitRemoveActions<IMy>()
        {
            try
            {
                List<IMyTerminalAction> actions;
                MyAPIGateway.TerminalControls.GetActions<IMy>(out actions);
                foreach (var action in actions.Where(action => action.Id == "ShowOnHUD" ||
                                                               action.Id == "ShowOnHUD_On" ||
                                                               action.Id == "ShowOnHUD_Off" ||
                                                               action.Id == "HorizonIndicator" ||
                                                               action.Id == "IncreaseFontSize" ||
                                                               action.Id == "DecreaseFontSize" ||
                                                               action.Id == "IncreaseTextPaddingSlider" ||
                                                               action.Id == "DecreaseTextPaddingSlider" ||
                                                               action.Id == "IncreaseChangeIntervalSlider" ||
                                                               action.Id == "DecreaseChangeIntervalSlider" ||
                                                               action.Id == "PreserveAspectRatio"))
                {
                    MyAPIGateway.TerminalControls.RemoveAction<IMy>(action);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        private static void PlaceSeparateArea<IMy>()
        {
            var separateArea = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator,IMy>("SeparateArea");
            separateArea.Visible = (b) => b.GetAs<CockpitUpgrade>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMy>(separateArea);
        }
    }
}