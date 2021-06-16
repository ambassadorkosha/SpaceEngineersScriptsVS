using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Shared;
using ServerMod;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials.ShipClass {
    public class MassCache {
        private int calculated;
        private Vector3D position;
        private double mass;
        private Ship ship;

        public Vector3D centerOfMass { get { updateMass(false); return position; }}
        public double shipMass { get { updateMass(false); return mass; }}

        public MassCache(Ship ship) { this.ship = ship; }

        public void dropCache() { calculated = -1; }
            
        public void updateMass(bool force) {
            if (FrameExecutor.currentFrame == calculated || force) return;
            if (ship.grid == null) return;
            if (ship.grid.Physics == null) return;

            calculated = FrameExecutor.currentFrame;
                
            var grid = ship.grid;
                
            var COM_ship = grid.Physics.CenterOfMassWorld;
            float grid_mass = grid.Physics.Mass;

            var subgrids = MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Logical);
            if (subgrids.Count > 1) {
                foreach (MyCubeGrid subgrid in subgrids) {
                    if (subgrid != grid && subgrid.Physics != null) {
                        COM_ship = COM_ship + (subgrid.Physics.CenterOfMassWorld - COM_ship) * (subgrid.Physics.Mass / (grid_mass + subgrid.Physics.Mass));
                        grid_mass = grid_mass + subgrid.Physics.Mass;
                    }
                }
                
                foreach (MyCubeGrid subgrid in subgrids) {
                    var ship = subgrid.GetShip();
                    ship.massCache.position = COM_ship;
                    ship.massCache.mass = grid_mass;
                    ship.massCache.calculated = calculated;
                }
                
                position = COM_ship;
                mass = grid_mass;
            } else {
                mass = grid.Physics.Mass;
                position = grid.Physics.CenterOfMassWorld;
            }				 
        }
    }
}