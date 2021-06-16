using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using Digi2.AeroWings;
using Scripts.Specials.ExtraInfo;
using ServerMod;
using VRage.Game.Components;
using Digi;
using System;

namespace Scripts.Specials.Wings
{
    public class WingLogic {
        public static int maxSquareWingSpeed = 20000;
        public static double START_FORCE = 15 * 15; //15 m/s
        public const float WING_LIFT_POWER = 3.2f;

        public static void BeforeSimulation(IMyCubeGrid grid, Ship ship) {
            if (grid == null || grid.Physics == null || grid.Physics.IsStatic) return;
            
            var vel = grid.Physics.LinearVelocity;
            double speedSq = MathHelper.Clamp(vel.LengthSquared() * 2, 0, maxSquareWingSpeed);
            
            if (!(speedSq >= START_FORCE)) return;
            
            ship.AtmosphereProperties.updateAtmosphere(grid);
            var wv = ship.AtmosphereProperties.getWingValue();
            if (wv <= 0) return;

            Vector3D CenterOfMass = ship.massCache.centerOfMass;

            Vector3D stopForce = Vector3D.Zero;
            
            Vector3D AngularVelocitySlowdownFactor = Vector3D.Zero;

            

            double forceScale = 0;
            var forceVector = Vector3D.Zero;
            //var upVwector = MatrixD.Zero;

            var stopAngularForce = 0.0;

            foreach (var B in ship.wingList.OrientedBlocks) {
                if (B == null || B.BlocksList.Count <= 0) continue;
                forceScale = 0;
                var upDir = Vector3D.Zero;
                foreach (var T in B.BlocksList) {
                    if (T == null) continue;
                    if (!T.IsFunctional) continue;


                    var W = T.GetAs<WingTN>();
                    if (W == null) {
                        var eleron = T.GetAs<Eleron>();
                        upDir = eleron.GetUpVector();
                        if (eleron != null)
                        {
                            Eleron.ApplyEleron(ship, eleron, vel, speedSq);
                        }
                        var fw = eleron.GetForwardVector();
                        var speedDir = fw.Dot(vel);
                        if (speedDir != 0) { 
                            forceScale += eleron.GetLiftForce();
                        }
                        stopAngularForce += eleron.GetLiftForce();
                    } else
                    {
                        upDir = W.GetUpVector();
                        var fw = W.GetForwardVector();
                        var speedDir = fw.Dot(vel);
                        if (speedDir != 0) {
                            forceScale += W.GetLiftForce();
                        }
                        stopAngularForce += W.GetLiftForce();
                    }
                }

 
                var scalar = WING_LIFT_POWER * forceScale * speedSq * wv;
                var vel2 = new Vector3D(vel.X, vel.Y, vel.Z);
                vel2.Normalize();


                var v = vel2 * (scalar * WingTN.BREAK_FORCE);
                stopForce += v;

                forceVector += (-upDir * upDir.Dot(vel) * scalar) - v;
            }

			MatrixD WorldToBody = MatrixD.Transpose(ship.grid.WorldMatrix);
			Vector3D BodyAngularVelocity = Vector3D.TransformNormal(grid.Physics.AngularVelocity, WorldToBody);



			if (BodyAngularVelocity != Vector3D.Zero && stopAngularForce != 0)
			{
                stopAngularForce *= (wv * ship.forcesMultiplier) / 25d * (1 + Math.Sqrt(ship.massCache.shipMass));
                AngularVelocitySlowdownFactor = new Vector3D(stopAngularForce, stopAngularForce, stopAngularForce) * BodyAngularVelocity;
				grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, null, null, -AngularVelocitySlowdownFactor);
			}

			ship.CurrentCockpitWingSumVector = forceVector;
            ship.CurrentCockpitWingStopForce = stopForce;
            
            if (forceVector != Vector3.Zero) {
				
				grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceVector * ship.forcesMultiplier, ship.massCache.centerOfMass, null);
                //PhysicsHelper.Draw(Color.Red, ship.centerOfMassCache, forceVector);
                if (!MyAPIGateway.Session.isTorchServer()) {
                    var cock = MyAPIGateway.Session.Player.Controller.ControlledEntity as IMyCockpit;
                    if (cock == null) return;
                    if (cock.CubeGrid != grid) return;
                    Vector3D NUp = Vector3D.Normalize(-grid.Physics.Gravity);
                    double LiftForce = Vector3D.Dot(NUp, forceVector);
                    ShowInfo.Extra.Append("Wing Friction:").AppendLine(stopForce.Length().toPhysicQuantity("N"));
                    ShowInfo.Extra.Append("Wing Lift:").AppendLine(LiftForce.toPhysicQuantity("N"));
                    ShowInfo.Extra.Append("Wing A/D:").AppendLine(stopAngularForce.toPhysicQuantity("N"));
                }
            }
        }
    }
}