using System;
using System.Text;
using Digi;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using ServerMod;
using Scripts.Specials.Blocks;
using Scripts.Specials.Blocks.Reactions;
using Scripts.Shared;
using VRage.Game.ModAPI;
using ProtoBuf;
using VRage.Game.Entity;
using VRage.Utils;
using System.Collections.Generic;
using Slime;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Digi2.AeroWings
{

    [ProtoContract]
    public class WingSettings
    {
        [ProtoMember(1)] public float rotation;
        [ProtoMember(2)] public float targetRotation = 0;
        [ProtoMember(3)] public bool enabled = true;
        [ProtoMember(4)] public int flags = (int)MechanicsAxis.Pitch;
        [ProtoMember(5)] public float threshold = 0.4f;
        [ProtoMember(6)] public float sensitivity = 0.4f;
        [ProtoMember(7)] public float maxAngle = (float)(Math.PI / 6);
        [ProtoMember(8)] public float trimmer = 0f;

        public override string ToString()
        {
            return $"en={enabled} r={rotation} tr={targetRotation} f={flags} threshold={threshold} sensitivity={sensitivity} maxAngle={maxAngle} trimmer={trimmer}";
        }
        //[ProtoMember(6)] public bool invertedYaw = false;
    }

    public enum MechanicsAxis
    {
        Pitch = 1,
        Yaw = 2,
        Roll = 4,
        InvertedPitch = 8,
        InvertedYaw = 16,
        InvertedRoll = 32
    }

    public class WingBlockSettings
    {
        public float rotationSpeed = (float) Math.PI / 80f;
        public float maxAngle =  (float)Math.PI / 6f;
        public float minAngle = -(float)Math.PI / 6f;

        public double forwardFactor = 0.45f;
        public double forceMultiplier = 0.45f;
        public double aileronMlt = 1f;
        public Vector3 rotateOffset = Vector3.Zero;
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false, new string[]
    {
        "aero-wing_5x2_mechanical_Small",
        "aero-wing_5x3_mechanical_Small",
        "aero-wing_5x4_mechanical_Small",
        "aero-wing_5x5_mechanical_Small",
        "aero-wing_5x6_mechanical_Small",
        "aero-wing_5x7_mechanical_Small",

        "aero-wing_5x2_mechanical_Large",
        "aero-wing_5x3_mechanical_Large",
        "aero-wing_5x4_mechanical_Large",
        "aero-wing_5x5_mechanical_Large",
        "aero-wing_5x6_mechanical_Large",
        "aero-wing_5x7_mechanical_Large",


        "aero-wing_5x2_mechanical_Small_T02",
        "aero-wing_5x3_mechanical_Small_T02",
        "aero-wing_5x4_mechanical_Small_T02",
        "aero-wing_5x5_mechanical_Small_T02",
        "aero-wing_5x6_mechanical_Small_T02",
        "aero-wing_5x7_mechanical_Small_T02",

        "aero-wing_5x2_mechanical_Large_T02",
        "aero-wing_5x3_mechanical_Large_T02",
        "aero-wing_5x4_mechanical_Large_T02",
        "aero-wing_5x5_mechanical_Large_T02",
        "aero-wing_5x6_mechanical_Large_T02",
        "aero-wing_5x7_mechanical_Large_T02",

        "aero-wing_5x2_mechanical_Small_T03",
        "aero-wing_5x3_mechanical_Small_T03",
        "aero-wing_5x4_mechanical_Small_T03",
        "aero-wing_5x5_mechanical_Small_T03",
        "aero-wing_5x6_mechanical_Small_T03",
        "aero-wing_5x7_mechanical_Small_T03",

        "aero-wing_5x2_mechanical_Large_T03",
        "aero-wing_5x3_mechanical_Large_T03",
        "aero-wing_5x4_mechanical_Large_T03",
        "aero-wing_5x5_mechanical_Large_T03",
        "aero-wing_5x6_mechanical_Large_T03",
        "aero-wing_5x7_mechanical_Large_T03",

        "aero-wing_5x2_mechanical_Small_T04",
        "aero-wing_5x3_mechanical_Small_T04",
        "aero-wing_5x4_mechanical_Small_T04",
        "aero-wing_5x5_mechanical_Small_T04",
        "aero-wing_5x6_mechanical_Small_T04",
        "aero-wing_5x7_mechanical_Small_T04",

        "aero-wing_5x2_mechanical_Large_T04",
        "aero-wing_5x3_mechanical_Large_T04",
        "aero-wing_5x4_mechanical_Large_T04",
        "aero-wing_5x5_mechanical_Large_T04",
        "aero-wing_5x6_mechanical_Large_T04",
        "aero-wing_5x7_mechanical_Large_T04",

        "aero-side-wing_3x2x1_mechanical_Small",
        "aero-side-wing_3x2x1_mechanical_Large",

        "aero-side-wing_3x2x1_mechanical_Small_T02",
        "aero-side-wing_3x2x1_mechanical_Large_T02",

        "aero-side-wing_3x2x1_mechanical_Small_T03",
        "aero-side-wing_3x2x1_mechanical_Large_T03",

        "aero-side-wing_3x2x1_mechanical_Small_T04",
        "aero-side-wing_3x2x1_mechanical_Large_T04",
    })]
    public class Eleron : BlockReactionsOnKeys, IWing {
        private static readonly Guid guid = new Guid("e06a78fa-14a9-4880-a417-85df54628e92");
        private static bool INITED = false;

        public static Sync<WingSettings, Eleron> sync;
        public static void Init()
        {
            sync = new Sync<WingSettings, Eleron>(23734, (x) => x.settings, Handle);
        }

        public static void Handle(Eleron eleron, WingSettings settings, ulong PlayerSteamId, bool isFromServer)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                eleron.settings.targetRotation = settings.targetRotation;
                eleron.settings.targetRotation = MathHelper.Clamp(eleron.settings.targetRotation, eleron.blockSettings.minAngle, eleron.blockSettings.maxAngle);
                eleron.settings.flags = settings.flags;
                eleron.settings.enabled = settings.enabled;
                eleron.settings.sensitivity = settings.sensitivity;
            }
            else
            {
                eleron.settings = settings;
            }
        }

        public static void ApplyEleron(Ship ship, Eleron eleron, Vector3 vel, double speedSq)
        {
            var m = eleron.getBlockMatrix();//.GetEleron();
            var fw2 = Vector3D.Normalize(m.Left + m.Forward * eleron.blockSettings.forceMultiplier);
            var speedDir2 = Math.Abs(fw2.Dot(vel));
            var upDir = eleron.block.WorldMatrix.Up;

            if (speedDir2 != 0)
            {
                float CurrentAngle = eleron.GetCurrentRotationAngle();
                if (CurrentAngle != 0)
                {
                    var scalar = (ROTATION_MLT * speedDir2 * speedSq * ship.AtmosphereProperties.getWingValue() * ship.forcesMultiplier);// * (1 + Math.Sqrt(ship.massCache.shipMass) / 25);
                    var forceVector = (upDir * Math.Sin(CurrentAngle) * scalar);
                    ship.grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceVector, m.Translation, null);
                    //PhysicsHelper.Draw (Color.Red, m.Translation, forceVector);
                }
            }
        }
        static float ROTATION_MLT = 8.2f;


        public IMyTerminalBlock block { get; private set; }
        private WingSettings settings;

        private MyEntitySubpart wing;
        private MatrixD wingMatrix;

        public WingBlockSettings blockSettings;

        private bool modelInited = false;

        public MatrixD getBlockMatrix() { return block.WorldMatrix; }



        private Vector3 CalculateOffset (double length)
        {
            return new Vector3(0, 0, block.CubeGrid.GridSize * (length / 2 - 1f / 4f));
        }

        public double GetLiftForce()
        {
            return blockSettings.forceMultiplier;
        }

        public Vector3D GetUpVector()
        {
            return block.WorldMatrix.Up;
        }

        public Vector3D GetForwardVector()
        {
            return Vector3D.Normalize(block.WorldMatrix.Left + block.WorldMatrix.Forward * blockSettings.forwardFactor);
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME;

            block = Entity as IMyTerminalBlock;
            blockSettings = new WingBlockSettings();

            var sn = block.BlockDefinition.SubtypeName;
            if (sn.Contains("5x2"))
            {
                blockSettings.forceMultiplier = 1.0;
                blockSettings.forwardFactor = 0.15;
                blockSettings.rotateOffset = CalculateOffset(2);
            }
            else if (sn.Contains("5x3"))
            {
                blockSettings.forceMultiplier = 1.25;
                blockSettings.forwardFactor = 0.25;
                blockSettings.rotateOffset = CalculateOffset(3);
            }
            else if (sn.Contains("5x4"))
            {
                blockSettings.forceMultiplier = 1.5;
                blockSettings.forwardFactor = 0.35;
                blockSettings.rotateOffset = CalculateOffset(4);
            }
            else if (sn.Contains("5x5"))
            {
                blockSettings.forceMultiplier = 1.75;
                blockSettings.forwardFactor = 0.45;
                blockSettings.rotateOffset = CalculateOffset(5);
            }
            else if (sn.Contains("5x6"))
            {
                blockSettings.forceMultiplier = 2.0;
                blockSettings.forwardFactor = 0.55;
                blockSettings.rotateOffset = CalculateOffset(6);
            }
            else if (sn.Contains("5x7"))
            {
                blockSettings.forceMultiplier = 2.2;
                blockSettings.forwardFactor = 0.55;
                blockSettings.rotateOffset = CalculateOffset(7);
            }
            else if (sn.Contains("side-wing"))
            {
                blockSettings.forceMultiplier = 1.0;
                blockSettings.forwardFactor = 0;
                blockSettings.rotateOffset = Vector3.Zero;
            }

            blockSettings.forceMultiplier *= 0.8;// beacuse it is not wing

            var lvl = 0;
            if (sn.EndsWith("T02")) lvl = 1;
            else if (sn.EndsWith("T03")) lvl = 2;
            else if (sn.EndsWith("T04")) lvl = 3;

            blockSettings.forceMultiplier *= (int)Math.Pow(3, lvl);

            if (block.CubeGrid.GridSizeEnum == MyCubeSize.Large)
            {
                blockSettings.forceMultiplier *= 20;
            }


            if (!INITED)
            {
                INITED = true;
                InitActions();
                InitBlockReactionsActions<Eleron, IMyTerminalBlock>("Eleron");
            }

            if (!Entity.TryGetStorageData<WingSettings>(guid, out settings))
            {
                settings = new WingSettings();
            }
        }

        public void Serialize ()
        {
            Entity.SetStorageData(guid, settings);
        }

        public static void RunDraw(IMyCubeGrid grid)
        {
            var ship = grid.GetShip();
            if (ship == null) return;
            var elerons = ship.elerons;
            if (elerons == null || elerons.Count == 0) return;

            foreach (var x in elerons)
            {
                x.Draw();
            }
        }

        public void Draw ()
        {
            try
            {
                if (wing == null || wing.MarkedForClose || wing.Closed)
                {
                    wing = null;
                    if(!InitModel()) return;
                }
                wing.PositionComp.LocalMatrix = wingMatrix * Matrix.CreateTranslation(blockSettings.rotateOffset) * Matrix.CreateRotationX(settings.rotation) * Matrix.CreateTranslation(-blockSettings.rotateOffset);
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (!modelInited)
            {
                InitModel();
            }
        }

        private bool InitModel()
        {
            var isOut = Entity.TryGetSubpart("Aeliron", out wing);
            if (wing != null)  wingMatrix = wing.PositionComp.LocalMatrixRef;
            return isOut;
        }

        public override void OnOptionsChanged()
        {
            keyReactions = ReactionSet.Parse(reactionString, AddReactions);
        }

        public EleronSetDegree AddReactions(string action, string actionInfo, List<VRage.Input.MyKeys> keys, List<MoveIndicator> indicators)
        {
            float overrideValue;
            if (float.TryParse(action, out overrideValue))
            {
                return new EleronSetDegree()
                {
                    keys = keys,
                    moveIndicators = indicators,
                    degree = (float)(overrideValue * Math.PI / 180.0),
                    thrust = this
                };
            }

            return null;
        }

        public static void AddToTargetDegree(IMyTerminalBlock b, double value)
        {
            var th = b.GetAs<Eleron>();
            if (th != null)
            {
                th.settings.targetRotation += (float)value;
                th.settings.targetRotation = MathHelper.Clamp (th.settings.targetRotation, th.blockSettings.minAngle, th.blockSettings.maxAngle);
                th.Serialize();
                sync.SendMessageToOthers(th.Entity.EntityId, th.settings);
            }
        }

        public static void SetTargetDegree(IMyTerminalBlock b, double value)
        {
            var th = b.GetAs<Eleron>();
            if (th != null)
            {
                th.settings.targetRotation = (float)value;
                th.Serialize();
                sync.SendMessageToOthers(th.Entity.EntityId, th.settings);
            }
        }

        public static void SetRotationScript(Sandbox.ModAPI.Ingame.IMyTerminalBlock b, float val)
        {
            var th = ((IMyTerminalBlock)b).GetAs<Eleron>();
            if (th != null)
            {
                th.settings.trimmer = val;
                sync.SendMessageToOthers(th.Entity.EntityId, th.settings);
            }
        }

        public static void GetTargetRotation(IMyTerminalBlock b, StringBuilder value)
        {
            var th = b.GetAs<Eleron>();
            if (th != null)
            {
                value.AppendFormat("{0:N0}", (th.settings.targetRotation / Math.PI * 180)).Append("*");
            }
        }

        private static void SetON(Eleron th, bool react, MechanicsAxis on)
        {
            bool changed = false;
            if (!react) {
                if (th.settings.flags.HasFlags((int)on))
                {
                    th.settings.flags -= (int)on;
                    changed = true;
                }
            }  else
            {
                if (!th.settings.flags.HasFlags ((int)on))
                {
                    th.settings.flags |= (int)on;
                    changed = true;
                }
            }


            if (changed) {
                th.Serialize();
                sync.SendMessageToOthers(th.Entity.EntityId, th.settings);
            }
        }

        private static void SetThreshold(Eleron th, float val)
        {
            if (th.settings.threshold != val)
            {
                th.settings.threshold = val;
                th.Serialize();
                sync.SendMessageToOthers(th.Entity.EntityId, th.settings);
            }
        }

        private static void SetSensitity(Eleron th, float val)
        {
            if (th.settings.sensitivity != val)
            {
                th.settings.sensitivity = val;
                th.Serialize();
                sync.SendMessageToOthers(th.Entity.EntityId, th.settings);
            }
        }

        private static void SetTrimmer(Eleron th, float val)
        {
            var radians = (float)MathHelper.Clamp(val.toRadian(), th.blockSettings.minAngle, th.blockSettings.maxAngle);
            if (th.settings.trimmer != radians)
            {
                th.settings.trimmer = radians;
                th.Serialize();
                sync.SendMessageToOthers(th.Entity.EntityId, th.settings);
            }
        }

        private static void SetMaxAngle(Eleron th, float val)
        {
            var radians = (float)MathHelper.Clamp(val.toRadian(), 0, th.blockSettings.maxAngle);
            if (th.settings.maxAngle != radians)
            {
                th.settings.maxAngle = radians;
                th.Serialize();
                sync.SendMessageToOthers(th.Entity.EntityId, th.settings);
            }
        }

        private static bool GetON(Eleron th, MechanicsAxis on)
        {
            try
            {
                return (th.settings.flags & (int)on) != 0;
            } catch (Exception e)
            {
                return false;
            }
        }

        public static void InitActions()
        {
            MyAPIGateway.TerminalControls.CreateCheckbox<Eleron, IMyTerminalBlock>("Wing_ReactPitch", "React on Pitch (Left/Right)", "Pick how it will react on your control", (x) => GetON(x, MechanicsAxis.Pitch), (x, v) => SetON(x, v, MechanicsAxis.Pitch));
            MyAPIGateway.TerminalControls.CreateCheckbox<Eleron, IMyTerminalBlock>("Wing_ReactYaw", "React on Yaw (Up/Down)", "Pick how it will react on your control", (x) => GetON(x, MechanicsAxis.Yaw), (x, v) => SetON(x, v, MechanicsAxis.Yaw));
            MyAPIGateway.TerminalControls.CreateCheckbox<Eleron, IMyTerminalBlock>("Wing_ReactRoll", "React on Roll (Q/E)", "Pick how it will react on your control", (x) => GetON(x, MechanicsAxis.Roll), (x, v) => SetON(x, v, MechanicsAxis.Roll));

            MyAPIGateway.TerminalControls.CreateCheckbox<Eleron, IMyTerminalBlock>("Wing_ReactPitchInverted", "Inverted Pitch", "Pick how it will react on your control", (x) => GetON(x, MechanicsAxis.InvertedPitch), (x, v) => SetON(x, v, MechanicsAxis.InvertedPitch));
            MyAPIGateway.TerminalControls.CreateCheckbox<Eleron, IMyTerminalBlock>("Wing_ReactYawInverted", "Inverted Yaw", "Pick how it will react on your control", (x) => GetON(x, MechanicsAxis.InvertedYaw), (x, v) => SetON(x, v, MechanicsAxis.InvertedYaw));
            MyAPIGateway.TerminalControls.CreateCheckbox<Eleron, IMyTerminalBlock>("Wing_ReactRollInverted", "Inverted Roll", "Pick how it will react on your control", (x) => GetON(x, MechanicsAxis.InvertedRoll), (x, v) => SetON(x, v, MechanicsAxis.InvertedRoll));

            MyAPIGateway.TerminalControls.CreateSlider<Eleron, IMyTerminalBlock>("Wing_SensitivityThreshold", "Sensitivity: Threshold", "Minimal amount of ", 0, 0.9f, (x) => x.settings.threshold, (x,v)=>v.Append(x.settings.threshold), SetThreshold);
            MyAPIGateway.TerminalControls.CreateSlider<Eleron, IMyTerminalBlock>("Wing_SensitivityPower", "Sensitivity: Power", "Pick how it will react on your control", 0, 0.9f, (x) => x.settings.sensitivity, (x,v)=>v.Append(x.settings.sensitivity), SetSensitity);

            MyAPIGateway.TerminalControls.CreateSlider<Eleron, IMyTerminalBlock>("Wing_Trimmer", "Trimmer", "Wing will return to this angle value", -30f, 30f, (x) => x.settings.trimmer.toDegree(), (x,v)=>v.Append(x.settings.trimmer.toDegree()), SetTrimmer);
            var property = MyAPIGateway.TerminalControls.CreateProperty<Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>, IMyTerminalBlock>("Wing_Angle_Script");
            property.Getter = block => SetRotationScript;
            MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(property);

            MyAPIGateway.TerminalControls.CreateSlider<Eleron, IMyTerminalBlock>("Wing_MaxAngle", "MaxAngle", "Wing will return to this angle (in degrees) value", 0f, 30f, (x) => x.settings.maxAngle.toDegree(), (x,v)=>v.Append(x.settings.maxAngle.toDegree()), SetMaxAngle);


            var addDegree90 = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("Wing360_Add90");
            addDegree90.Action = (b) => AddToTargetDegree(b, Math.PI / 2);
            addDegree90.Name = new StringBuilder("+90*");
            addDegree90.Writer = (b, sb) => GetTargetRotation(b, sb);
            addDegree90.Enabled = (b) => b.GetAs<Eleron>() != null;
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(addDegree90);

            var addDegree30 = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("Wing360_Add30");
            addDegree30.Action = (b) => AddToTargetDegree(b, Math.PI / 6);
            addDegree30.Name = new StringBuilder("+30*");
            addDegree30.Writer = (b, sb) => GetTargetRotation(b, sb);
            addDegree30.Enabled = (b) => b.GetAs<Eleron>() != null;
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(addDegree30);

            var addDegree45 = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("Wing360_Add15");
            addDegree45.Action = (b) => AddToTargetDegree(b, Math.PI / 12);
            addDegree45.Name = new StringBuilder("+15*");
            addDegree45.Writer = (b, sb) => GetTargetRotation(b, sb);
            addDegree45.Enabled = (b) => b.GetAs<Eleron>() != null;
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(addDegree45);

            var remDegree90 = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("Wing360_Rem90");
            remDegree90.Action = (b) => AddToTargetDegree(b, -Math.PI / 2);
            remDegree90.Name = new StringBuilder("-90*");
            remDegree90.Writer = (b, sb) => GetTargetRotation(b, sb);
            remDegree90.Enabled = (b) => b.GetAs<Eleron>() != null;
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(remDegree90);

            var remDegree30 = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("Wing360_Rem30");
            remDegree30.Action = (b) => AddToTargetDegree(b, -Math.PI / 6);
            remDegree30.Name = new StringBuilder("-30*");
            remDegree30.Writer = (b, sb) => GetTargetRotation(b, sb);
            remDegree30.Enabled = (b) => b.GetAs<Eleron>() != null;
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(remDegree30);

            var remDegree45 = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("Wing360_Rem15");
            remDegree45.Action = (b) => AddToTargetDegree(b, -Math.PI / 12);
            remDegree45.Name = new StringBuilder("-15*");
            remDegree45.Writer = (b, sb) => GetTargetRotation(b, sb);
            remDegree45.Enabled = (b) => b.GetAs<Eleron>() != null;
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(remDegree45);
        }

        private double ReactOnRotation (double rotationValue)
        {
            if (rotationValue < settings.threshold && rotationValue > -settings.threshold)
            {
                return 0;
            }
            else
            {
                return (MathHelper.Clamp(Math.Abs(rotationValue), 0, 0.9) - settings.threshold + settings.sensitivity) / (0.9 - settings.threshold) * Math.Sign(rotationValue);
            }
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            try
            {
                if (settings == null) return;
                var ship = block.CubeGrid.GetShip();
                if (ship == null) return;
                var m = block.CubeGrid.WorldMatrix;
                Vector3D Forward = m.Forward;
                Vector3D Up = m.Up;
                Vector3D Right = m.Right;
                if (ship.PilotCockpit != null)
                {
                    Forward = ship.PilotCockpit.WorldMatrix.Forward;
                    Up = ship.PilotCockpit.WorldMatrix.Up;
                    Right = ship.PilotCockpit.WorldMatrix.Right;
                }

                var tr = settings.targetRotation;
                var r = settings.rotation;

                double rotationValue = 0;

                if (settings.flags.HasFlags((int)MechanicsAxis.Pitch)) //Pitch have higher priority
                {
                    rotationValue = ship.PilotActions.X * (settings.flags.HasFlags((int)MechanicsAxis.InvertedPitch) ? -1d : 1d);
                    rotationValue = ReactOnRotation(rotationValue);
                }

                if (settings.flags.HasFlags((int)MechanicsAxis.Yaw))
                {
                    rotationValue = ship.PilotActions.Y * (settings.flags.HasFlags((int)MechanicsAxis.InvertedYaw) ? -1d : 1d);
                    rotationValue = ReactOnRotation(rotationValue);
                }

                if (settings.flags.HasFlags((int)MechanicsAxis.Roll))
                {
                    rotationValue = ship.PilotActions.Z * (settings.flags.HasFlags((int)MechanicsAxis.InvertedRoll) ? -1d : 1d);
                    rotationValue = ReactOnRotation(rotationValue);
                }

                var clamp1 = MathHelper.Clamp((float)(rotationValue * blockSettings.maxAngle) + settings.trimmer, blockSettings.minAngle, blockSettings.maxAngle);
                settings.targetRotation = MathHelper.Clamp(clamp1, -settings.maxAngle, settings.maxAngle);

                if (settings.rotation != settings.targetRotation)
                {
                    if (settings.rotation < settings.targetRotation)
                    {
                        settings.rotation += blockSettings.rotationSpeed;
                        if (settings.rotation > settings.targetRotation)
                        {
                            settings.rotation = settings.targetRotation;
                        }
                    }
                    else
                    {
                        settings.rotation -= blockSettings.rotationSpeed;
                        if (settings.rotation < settings.targetRotation)
                        {
                            settings.rotation = settings.targetRotation;
                        }
                    }
                }

                if ((tr != settings.targetRotation || r != settings.rotation) && MyAPIGateway.Session.IsServer)
                {
                    sync.SendMessageToOthers(block.EntityId, settings);
                }
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }
        public static double GetDistanceToRay(Vector3D Origin, Vector3D NormalizedDirection, Vector3D Point)
        {
            Vector3D OPoint = Point - Origin;
            Vector3D RayPoint = Vector3D.Dot(OPoint, NormalizedDirection) * NormalizedDirection;
            return Vector3D.Distance(RayPoint, OPoint);
        }
        public static Vector3D ProjectOnPlane(Vector3D Vec, Vector3D Normal)
        {
            return Vec - Normal * Vector3D.Dot(Vec, Normal);
        }
        public MatrixD GetEleron ()
        {
            var m = block.WorldMatrix;
            var t = m.Translation;
            var left = m.Left;
            var gs = block.CubeGrid.GridSize / 2f;
            return m * Matrix.CreateTranslation(-t - left * 3 * gs) * Matrix.CreateFromAxisAngle(m.Forward, settings.rotation) * Matrix.CreateTranslation(t - left * gs * 4.7);
        }
        public float GetCurrentRotationAngle()
        {
            return settings.rotation;
        }

        public override IMyTerminalControlTextbox GetControl()
        {
            return null;
        }
    }
}
