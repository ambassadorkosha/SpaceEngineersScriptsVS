using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game;
using VRage.Common.Utils;
using VRageMath;
using VRage;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Library.Utils;


namespace Takeshi.AirBrakes
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AdvancedDoor), false, "aero-wing-plane-air_brake_double_1x1x1_Small", "aero-wing-plane-air_brake_double_1x1x1_Large", "aero-wing-plane-air_brake_single_1x1x1_Small", "aero-wing-plane-air_brake_single_1x1x1_Large")]
    public class AirBrakes : MyGameLogicComponent {
        private Vector3D COM_offset;
        int tempcount;
        int tempcountmax;

        private MyObjectBuilder_EntityBase objectBuilder;
        private IMyAdvancedDoor block;

        private float drag;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            this.objectBuilder = objectBuilder;
            block = Entity as IMyAdvancedDoor;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            drag = getDragPower ();
        }

        public override void UpdateOnceBeforeFrame() {
            block.DoorStateChanged += DoorStateChanged;
            if (block.Status == Sandbox.ModAPI.Ingame.DoorStatus.Open || block.Status == Sandbox.ModAPI.Ingame.DoorStatus.Opening) {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        private void DoorStateChanged(bool doorstate) {
            if (block.Status == Sandbox.ModAPI.Ingame.DoorStatus.Open || block.Status == Sandbox.ModAPI.Ingame.DoorStatus.Opening) {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            } else {
                NeedsUpdate = MyEntityUpdateEnum.NONE;
            }
        }

        public float getDragPower () {
            if (block.BlockDefinition.SubtypeId == "aero-wing-plane-air_brake_single_1x1x1_Small") return 1000f*2;
            if (block.BlockDefinition.SubtypeId == "aero-wing-plane-air_brake_double_1x1x1_Large") return 27000f*2;
            if (block.BlockDefinition.SubtypeId == "aero-wing-plane-air_brake_single_1x1x1_Large") return 13500f*2;
            return 2000f*2;
        }

        //public override void UpdateAfterSimulation()
        public override void UpdateBeforeSimulation() {
            base.UpdateBeforeSimulation();

            if (block == null || block.MarkedForClose || block.Closed) return;
            var grid = block.CubeGrid as MyCubeGrid;
            if (grid.Physics == null || grid.Physics.IsStatic) return;

            var vel = grid.Physics.GetVelocityAtPoint(block.WorldMatrix.Translation);
            float speed = vel.Length();

            if (speed > 1) {
                //new version: Ship COM
                if (++tempcount > tempcountmax) {
                    tempcount = 0;
                    tempcountmax = 60;

                    var COM_ship = grid.Physics.CenterOfMassWorld;
                    float grid_mass = grid.Physics.Mass;
                    COM_offset = Vector3D.Zero;

                    var subgrids = MyAPIGateway.GridGroups.GetGroup(grid, VRage.Game.ModAPI.GridLinkTypeEnum.Logical);
                    if (subgrids.Count > 1) {
                        foreach (MyCubeGrid subgrid in subgrids) {
                            if (subgrid != grid && subgrid.Physics != null) {
                                COM_ship = COM_ship + (subgrid.Physics.CenterOfMassWorld - COM_ship) * (subgrid.Physics.Mass / (grid_mass + subgrid.Physics.Mass));
                                grid_mass = grid_mass + subgrid.Physics.Mass;
                            }
                        }
                        COM_offset = Vector3D.TransformNormal(COM_ship - grid.Physics.CenterOfMassWorld, MatrixD.Transpose(block.WorldMatrix));
                    }
                }
                
                var shipCenter =  grid.Physics.CenterOfMassWorld + Vector3D.TransformNormal(COM_offset, block.WorldMatrix);
                var grav = grid.Physics.Gravity;
                float gravspeed = grav.Length();
                Vector3D force = -vel * speed / 100f * gravspeed / 9.8f * drag;
                grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, shipCenter, null);
            }
        }

        public void Close()  {
            if(block!=null) {
                block.DoorStateChanged -= DoorStateChanged;
                block = null;
            }
            objectBuilder = null;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false) {
            return copy ? (MyObjectBuilder_EntityBase)objectBuilder.Clone() : objectBuilder;
        }
    }
}
