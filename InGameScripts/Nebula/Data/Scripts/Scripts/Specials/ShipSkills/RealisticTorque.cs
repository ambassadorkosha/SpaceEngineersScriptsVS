using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;
using Scripts.Base;
using ServerMod;
using Scripts.Shared;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using Scripts.Specials.ExtraInfo;
using Slime;
using Digi;

namespace Scripts.Specials.ShipSkills
{
    public class RealisticTorque
    {
        //private Vector3D TorqueSum = Vector3D.Zero;
        //private Vector3D CenterMass = Vector3D.Zero;

        private List <Pair<Vector3D, Vector3D>> forces = new List<Pair<Vector3D, Vector3D>>();
        private int wasApplied;

        public void AddRealisticTorque (ref Vector3D Position, ref Vector3D ThrustForce)
        {
            forces.Add (new Pair<Vector3D, Vector3D>(Position, ThrustForce));
        }

        public void AddRealisticTorque(ref Vector3D Position, ref Vector3D ThrustForce, ref Vector3D CenterMass, ref Vector3D TorqueSum)
        {
            TorqueSum += Vector3D.Cross(Position - CenterMass, ThrustForce);
        }

        public void ApplyRealisticTorque(Ship ship)
        {
            if (wasApplied == FrameExecutor.currentFrame) return;
            IMyCubeGrid grid = ship.grid;

            var real = new List<RealisticThruster>();
            var current = FrameExecutor.currentFrame;

            var biggestGrid = grid;
            var gg = MyGridPhysicalGroupData.GetGroupSharedProperties((MyCubeGrid)grid);
            var center = gg.CoMWorld;
            var mass = gg.Mass;

            foreach (var x in ship.connectedGrids)
            {
                var sh = x.GetShip();
                sh.realisticTorque.wasApplied = current;
                real.AddRange(x.GetShip().realistic);
                if (x.Physics != null && x.Physics.Mass > biggestGrid.Physics.Mass)
                {
                    biggestGrid = x;
                }
            }

            Vector3D TorqueSum = ApplyTorque(center, 4, mass, ship.connectedGrids);
            if (TorqueSum != Vector3D.Zero)
            {
                MatrixD WorldToBody = MatrixD.Transpose(biggestGrid.WorldMatrix);
                Vector3D BodyTorqueSum = Vector3D.TransformNormal(TorqueSum, WorldToBody);
                biggestGrid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, null, null, BodyTorqueSum);
            }
        }

        

        public Vector3D ApplyTorque (Vector3D CenterMass, double threshold, float totalMass, List<IMyCubeGrid> connectedShips)
        {
            var TorqueSum = Vector3D.Zero;
            Ship ship = null;
            foreach (var g in connectedShips)
            {
                var sh = g.GetShip();
                if (sh == null) continue;

                ship = sh;

                foreach (var x in sh.realisticTorque.forces)
                {
                    TorqueSum += Vector3D.Cross(x.k - CenterMass, x.v);
                }
                forces.Clear();

                foreach (RealisticThruster T in sh.realistic)
                {
                    if (!T.RealisticMode) continue;
                    IMyThrust TBlock = T.Block;
                    if (TBlock != null)
                    {
                        TorqueSum += Vector3D.Cross(TBlock.GetPosition() - CenterMass, TBlock.CurrentThrust * TBlock.WorldMatrix.Backward);
                    }
                }
            }

            //Log.ChatError("ApplyTorque:");
            if (ship != null)
            {
                ZeppelinLogic.UpdateBeforeSimulation(ship, CenterMass, ref TorqueSum);
            }

            

            var s = TorqueSum.Length() / totalMass;
            if (s < threshold)
            {
                return Vector3D.Zero;
            }
            else
            {
                //PhysicsHelper.Draw(Color.Red, CenterMass, TorqueSum);
                var norm = TorqueSum;
                norm.Normalize();
                return TorqueSum - norm * threshold;
            }
        }
    }
}
