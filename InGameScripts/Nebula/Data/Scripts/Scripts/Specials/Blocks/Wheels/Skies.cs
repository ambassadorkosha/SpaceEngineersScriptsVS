using Sandbox.ModAPI;
using System;
using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Blocks;
using Scripts.Shared;
using ServerMod;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.Game;
using VRage.Utils;

namespace Scripts.Specials.Wheels {


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Wheel), false,
        "SkiesWheelM_SB", "SkiesWheel_SB"
    )]
    public class SkiesWheel : MyGameLogicComponent
    {
        public IMyWheel Wheel;
        private bool inContact = false;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Wheel = (IMyWheel)Entity;
            
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateAfterSimulation();
            inContact = (Wheel.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) != 0;
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorSuspension), false,
        "SkiesM", "Skies"
    )]

    public class Skies : MyGameLogicComponent {
        public IMyMotorSuspension Suspension;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            Suspension = (IMyMotorSuspension)Entity;
            (Suspension as IMyTerminalBlock).PropertiesChanged += RotatingGyroLarge_PropertiesChanged;
            if (MyAPIGateway.Utilities.IsDedicated) {
                return;
            }
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

        }

        private void RotatingGyroLarge_PropertiesChanged(IMyTerminalBlock obj) { //Disables airshock
            if (Suspension.AirShockEnabled) {
                Suspension.AirShockEnabled = false;
            }
            if (Suspension.Propulsion)
            {
                Suspension.Propulsion = false;
            }
        }

        //int iter = 0;
        public override void UpdateBeforeSimulation() {
            try {
                if (!Suspension.IsFunctional) return;
                if (Suspension.Top == null) return;
                if (Suspension.Top.CubeGrid.Physics == null) return;
                if ((Suspension.Top.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) != 0) return;

                //iter++;
                var skiG = Suspension.Top.CubeGrid;

                var m = Suspension.WorldMatrix;
                var m2 = skiG.WorldMatrix;

                var v1 = m.Left;
                var v2 = m2.Left;
                var degree = Math.Acos (v1.Dot(v2) / (v1.Length() * v2.Length())).toDegree();

                if ((m.Translation - (m2.Translation + m2.Left)).LengthSquared() > (m.Translation - (m2.Translation + m.Left)).LengthSquared())
                {
                    degree *= -1;
                }

                if (Math.Abs(degree) > 30 && skiG.Physics.AngularVelocity.Length () < 7)
                {
                    skiG.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, null, null, new Vector3(0f, Math.Sign(degree) * 10000f, 0f), applyImmediately: false);
                } else
                {
                    skiG.Physics.AngularVelocity = skiG.Physics.AngularVelocity * 0.85f;
                }
                
                //Suspension.Top.CubeGrid.WorldMatrix

            } catch (Exception e)  {
                // This is for your benefit. Remove or change with your logging option before publishing.
                MyAPIGateway.Utilities.ShowNotification("Error: " + e.Message, 16);
            }
        }
    }
}