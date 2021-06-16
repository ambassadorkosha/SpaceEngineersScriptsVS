using Sandbox.ModAPI;
using System;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.Game;
using VRage.Utils;

namespace Scripts.Specials.Wheels {

    public class WheelGeometry {
        public float MinHeight = 3.5f; //WHEEL MAX HEIGHT
        public float ZeroDegreeHeight = 1.3f; 
        public float MaxHeight = -2.1f; //WHEEL MAX HEIGHT
        public float MinAngle = 0f; 
        public float MaxAngle = 50f;
        public float SideDistance = 2.5f;
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorSuspension), false,
        //"Fast"
       
        "Suspension3x3V",
        "Suspension3x3T02",
        "Suspension3x3T03",
        "Suspension3x3T04",
        "Suspension3x3T05",
        "Suspension3x3T06",

        "SmallSuspension3x3V",
        "SmallSuspension3x3T02",
        "SmallSuspension3x3T03",
        "SmallSuspension3x3T04",
        "SmallSuspension3x3T05",
        "SmallSuspension3x3T06",

        "Suspension3x3Vmirrored",
        "Suspension3x3mirroredT02",
        "Suspension3x3mirroredT03",
        "Suspension3x3mirroredT04",
        "Suspension3x3mirroredT05",
        "Suspension3x3mirroredT06",

        "SmallSuspension3x3Vmirrored",
        "SmallSuspension3x3mirroredT02",
        "SmallSuspension3x3mirroredT03",
        "SmallSuspension3x3mirroredT04",
        "SmallSuspension3x3mirroredT05",
        "SmallSuspension3x3mirroredT06",

        "Suspension5x5V",
        "Suspension5x5T02",
        "Suspension5x5T03",
        "Suspension5x5T04",
        "Suspension5x5T05",
        "Suspension5x5T06",

        "Suspension5x5Vmirrored",
        "Suspension5x5mirroredT02",
        "Suspension5x5mirroredT03",
        "Suspension5x5mirroredT04",
        "Suspension5x5mirroredT05",
        "Suspension5x5mirroredT06",
       
        "SmallSuspension5x5V",
        "SmallSuspension5x5T02",
        "SmallSuspension5x5T03",
        "SmallSuspension5x5T04",
        "SmallSuspension5x5T05",
        "SmallSuspension5x5T06",

        "SmallSuspension5x5Vmirrored",
        "SmallSuspension5x5mirroredT02",
        "SmallSuspension5x5mirroredT03",
        "SmallSuspension5x5mirroredT04",
        "SmallSuspension5x5mirroredT05",
        "SmallSuspension5x5mirroredT06"

        )]



    public class RotatingGyroLarge : MyGameLogicComponent {

        public static bool DEBUG = false;
        public static WheelGeometry FAST_LARGE = new WheelGeometry() {
           MinHeight = 0.0f, //WHEEL MAX HEIGHT
           MaxHeight = -2.80f, //WHEEL MAX HEIGHT
           ZeroDegreeHeight = -0.4f, 
           MinAngle = -10f, 
           MaxAngle = 45f,
           SideDistance = 2.5f * 2f
        };

        public static WheelGeometry FAT_LARGE = new WheelGeometry() {
           MinHeight = 0.2f, //WHEEL MAX HEIGHT
           MaxHeight = -9.2f, //WHEEL MAX HEIGHT
           ZeroDegreeHeight = 0.2f, 
           MinAngle = 0f, 
           MaxAngle = 65f,
           SideDistance =  7.5f + 0.7f
        };

        public static WheelGeometry FAT_SMALL = new WheelGeometry() {
           MinHeight = 0.2f, //WHEEL MAX HEIGHT
           MaxHeight = -2.05f, //WHEEL MAX HEIGHT
           ZeroDegreeHeight = 0, 
           MinAngle = -10f, 
           MaxAngle = 60f,
           SideDistance = 1.45f
        };

        
        public static WheelGeometry FAST_SMALL = new WheelGeometry() {
           MinHeight = 0f, //WHEEL MAX HEIGHT
           MaxHeight = -1.35f, //WHEEL MAX HEIGHT
           ZeroDegreeHeight = 0, 
           MinAngle = 0f, 
           MaxAngle = 60f,
           SideDistance = 1f
        };


        public IMyMotorSuspension Suspension;
        public MyEntitySubpart Turret;
        public MyEntitySubpart Head;


        public MyEntitySubpart tire1;
        public MyEntitySubpart tire2;
        
        public bool InitSubpart = true;

        private WheelGeometry geom;
        
        // These are static as their identities shouldnt change across different instances
        private Matrix turretOriginal;
        private Matrix headOriginal;

        private Matrix tire1Original = Matrix.Zero;
        private Matrix tire2Original = Matrix.Zero;
        
        private double TO_RAD = 2 * Math.PI /360;

        Color workingColor = Color.Green;
        Color topSpeed = new Color (0x30, 0xff, 0);
        float topSpeedEmissive = 1000f;

        double maxEmissiveSpeed = 70;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            Suspension = (IMyMotorSuspension)Entity;
            (Suspension as IMyTerminalBlock).PropertiesChanged += RotatingGyroLarge_PropertiesChanged;
            if (MyAPIGateway.Utilities.IsDedicated) {
                return;
            }

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            

            var n = Suspension.BlockDefinition.SubtypeName;
            var large = !n.Contains ("SmallSuspension");
            if (n.Contains ("5x5")) {
                geom = large ? FAT_LARGE : FAT_SMALL;
            } else if (n.Contains ("3x3")) {
                geom = large ? FAST_LARGE : FAST_SMALL;
            } else {
                geom = large ? FAST_LARGE : FAST_SMALL;
            }
        }

        private void RotatingGyroLarge_PropertiesChanged(IMyTerminalBlock obj) { //Disables airshock
            if (Suspension.AirShockEnabled) {
                Suspension.AirShockEnabled = false;
            }
        }

        public override void Close() {
            NeedsUpdate = MyEntityUpdateEnum.NONE;
        }

        private void InitSubParts () {
            if (Suspension.TryGetSubpart("LaserComTurret", out Turret)) {

                MyEntitySubpart hh;

                if (Turret.TryGetSubpart("LaserCom", out hh)) {
                    Head = hh;
                    InitSubpart = false;

                    turretOriginal = Turret.PositionComp.LocalMatrix;
                    headOriginal = Head.PositionComp.LocalMatrix;
                } else if (Turret.TryGetSubpart("LaserCom.002", out hh)) {
                    Head = hh;
                    InitSubpart = false;

                    turretOriginal = Turret.PositionComp.LocalMatrix;
                    headOriginal = Head.PositionComp.LocalMatrix;
                }
            }
        }

        private void InitSubParts2 () {
            if (Suspension.Top != null) { // && (tire1 == null || tire2 == null)) {
                var x = Suspension.Top.TryGetSubpart("M+S", out tire1);
                var x2 = Suspension.Top.TryGetSubpart("4x4", out tire2);
                if (x && tire1Original == Matrix.Zero) tire1Original =  new Matrix (tire1.PositionComp.LocalMatrix);
                if (x2 && tire2Original == Matrix.Zero) tire2Original =   new Matrix (tire2.PositionComp.LocalMatrix);
            }
        }

        public double calculatePositionOfWheel () {
            var m = Suspension.WorldMatrix;
            var offset = (m * Matrix.CreateTranslation (m.Up * geom.SideDistance)) * Matrix.CreateTranslation (m.Forward * (geom.MinHeight-geom.ZeroDegreeHeight));
            var wheelPositionMin  = offset.Translation - Suspension.Top.PositionComp.GetPosition();
            if (DEBUG) { 
                MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("Square"), Color.Blue * 0.5f, Suspension.GetPosition(), offset.Translation - Suspension.GetPosition(), 1, 0.05f);
                MyTransparentGeometry.AddPointBillboard(MyStringId.GetOrCompute("Square"), Color.Red * 0.5f, Suspension.Top.GetPosition(), 0.1f, 0f);
            }
            return wheelPositionMin.Length() / (geom.MinHeight - geom.MaxHeight);
        } 
        public void colorEmissives (Color c, float v) {
            if (Suspension.Top != null) Suspension.Top.SetEmissiveParts ("Emissive", c, v);
            if (Head != null) Head.SetEmissiveParts ("Emissive", c, v);
        }

        public override void UpdateBeforeSimulation() {
            try {
                if (!Suspension.IsFunctional) return;                // Ignore damaged or build progress blocks.
                //if (Suspension.CubeGrid.Physics == null) return;     // Ignore ghost grids (projections).
				
                if (InitSubpart || Turret != null && Head != null && (Head.Closed || Turret.Closed)) {
                    InitSubParts ();
                }
                
                if (Turret != null && Head != null && Suspension.Top != null) {
                    var pos = calculatePositionOfWheel ();

                    Turret.PositionComp.LocalMatrix =  turretOriginal * Matrix.CreateRotationZ((float)(Math.PI-Suspension.SteerAngle/2));
                    var angle = -(pos * (geom.MaxAngle-geom.MinAngle) + geom.MinAngle) * TO_RAD;
                    Head.PositionComp.LocalMatrix = Matrix.CreateRotationX ((float)angle) * Matrix.CreateTranslation (headOriginal.Translation);
                }

                if (!Suspension.IsWorking) {
                    colorEmissives (Color.Transparent, 1);
                } else {
                    var speedMlt = (float)Math.Pow(Math.Min(1, Suspension.CubeGrid.Physics.LinearVelocity.Length() / maxEmissiveSpeed), 2);
                    var c = Color.Lerp (workingColor, topSpeed, speedMlt);
                    colorEmissives (c, topSpeedEmissive*speedMlt);
                }

                InitSubParts2 ();
                if (Suspension.Friction < 25) {
                   if (tire1 != null) tire1.PositionComp.LocalMatrix = tire1Original * Matrix.CreateScale (0f,0f,0f);
                   if (tire2 != null) tire2.PositionComp.LocalMatrix = tire2Original * Matrix.CreateScale (0f,0f,0f);
                } else if (Suspension.Friction < 60) {
                   if (tire1 != null) tire1.PositionComp.LocalMatrix = tire1Original * Matrix.CreateScale (1f,1f,1f);
                   if (tire2 != null) tire2.PositionComp.LocalMatrix = tire2Original * Matrix.CreateScale (0f,0f,0f);
                } else {
                   if (tire1 != null) tire1.PositionComp.LocalMatrix = tire1Original * Matrix.CreateScale (0f,0f,0f);
                   if (tire2 != null) tire2.PositionComp.LocalMatrix = tire2Original * Matrix.CreateScale (1f,1f,1f);
                }
            } catch (Exception e)  {
                // This is for your benefit. Remove or change with your logging option before publishing.
                MyAPIGateway.Utilities.ShowNotification("Error: " + e.Message, 16);
            }
        }
    }
}