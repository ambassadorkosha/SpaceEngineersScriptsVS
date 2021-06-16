using System;
using System.Collections.Generic;

using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRageMath;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using Sandbox.Common.ObjectBuilders.Definitions;
using Digi;
using VRage.Utils;
using Sandbox.Game.Entities;
using Scripts.Specials.ExtraInfo;
using Slime;
using Scripts.Specials;

namespace ServerMod {
    public class AirFriction {
        public static double[] ELEVATIONS = new double [] { 1500, 2500, 3500, 4500, 6000, 8000, 10000};
        public static double[] FRICTION_START_LARGE = new double [] { 80, 100, 120, 140, 160, 180, 200};
        public static double[] FRICTION_START_SMALL = new double [] { 100, 120, 140, 160, 180, 200, 220};
        public static double[] TOP_SPEED_LARGE = new double [] { 120, 140, 160, 180, 200, 220, 240};
        public static double[] TOP_SPEED_SMALL = new double [] { 140, 160, 180, 200, 220, 240, 260};
        
        public static int getElevation (double elevation) {
            if (elevation < ELEVATIONS[0]) return 0;
            if (elevation < ELEVATIONS[1]) return 1;
            if (elevation < ELEVATIONS[2]) return 2;
            if (elevation < ELEVATIONS[3]) return 3;
            if (elevation < ELEVATIONS[4]) return 4;
            if (elevation < ELEVATIONS[5]) return 5;
            return 6;
        }

        public static double getMaxSpeed(bool isLarge, int max) {
            if (isLarge) {
                return TOP_SPEED_LARGE[max];
            } else {
                return TOP_SPEED_SMALL[max];
            }
        }
        
        public static double getFrictionStart(bool isLarge, int max) {
            if (isLarge) {
                return FRICTION_START_LARGE[max];
            } else {
                return FRICTION_START_SMALL[max];
            }
        }
        
        public static void ApplyFriction(Ship ship) {
            var p = ship.grid.Physics;

            if (!ship.active || ship.skipFriction || ship.grid.MarkedForClose || p == null || !p.Enabled || p.IsStatic) return;


            

            var x = p.LinearVelocity;
            var spd = x.Length();

            if (spd <= FRICTION_START_LARGE[0]) {
                if (ship.extraFriction > 0) {
                    var spdVector = p.LinearVelocity;
                    spdVector.Normalize();
                    if (!MyAPIGateway.Session.isTorchServer()) {
                        ShowInfo.Extra.Append("Extra friction:").AppendLine(((double)ship.extraFriction).toPhysicQuantity("N"));
                    }
                    var f = -spdVector * ship.extraFriction;
                    p.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, f, p.CenterOfMassWorld, null);
                    ship.extraFriction = 0;
                    ship.CurrentAirFriction = f;
                } else {
                    ship.CurrentAirFriction = Vector3.Zero;
                }
                return;
            }

            var elevation = ship.getElevation2();
            if (elevation < 0) {
                elevation = 9999999d;
            } 

            bool hardLimit = false;
            if (ship.thrusters.Count == 0 && ship.zeppelin.Count == 0) // gliders have more max speed
            {
                elevation = 9999999d;
            } else
            {
                var gridcore = LimitsChecker.GetMainCore(ship);
                if (gridcore == null)
                {
                    elevation = 0;
                    hardLimit = true;
                }
            }

            var el = getElevation(elevation);
            var isLarge = ship.grid.GridSizeEnum == VRage.Game.MyCubeSize.Large;

            var min = getFrictionStart(isLarge, el);
            if (spd < min) {
                if (ship.extraFriction > 0) {
                    var spdVector = p.LinearVelocity;
                    spdVector.Normalize();
                    if (!MyAPIGateway.Session.isTorchServer()) {
                        ShowInfo.Extra.Append("Extra friction:").AppendLine(((double) ship.extraFriction).toPhysicQuantity("N"));
                    }

                    var f = -spdVector * ship.extraFriction;
                    p.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, f, p.CenterOfMassWorld, null);
                    ship.extraFriction = 0;
                    ship.CurrentAirFriction = f;
                } else {
                    ship.CurrentAirFriction = Vector3.Zero;
                }
                return;
            }
            
            var max = getMaxSpeed(isLarge, el);
            var diff = max - min;
            var nowdiff = spd - min;


            var mlt = nowdiff/diff;
            var power = - MathHelper.Clamp(mlt*mlt*2.5, 0.03f, 1000f) * ship.grid.Physics.Mass * 10 * (hardLimit ? 45 : 1);

            var d = p.LinearVelocity;
            d.Normalize();
            
            d.Multiply((float)power-ship.extraFriction);//(float)power
            ship.extraFriction = 0;
            p.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, d, p.CenterOfMassWorld, null);
            ship.CurrentAirFriction = d;

            if (!MyAPIGateway.Session.isTorchServer()) {
                var grid = MyAPIGateway.Session.GetMyControlledGrid();
                if (grid == ship.grid) {
                    if (ship.extraFriction > 0) {
                        ShowInfo.Extra.Append("Extra friction:").AppendLine(((double)ship.extraFriction).toPhysicQuantity("N"));
                    }
                    ShowInfo.Extra.Append("Env. friction:").AppendLine((-power).toPhysicQuantity("N"));
                }
            }
        }
    }
}