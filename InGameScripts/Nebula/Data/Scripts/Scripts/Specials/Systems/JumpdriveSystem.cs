using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials {
    public class JumpdriveSystem {
        private static Random r = new Random();
        public static bool AllowJumpDrive(MyCubeGrid jgrid, Vector3D where, long user) {
            var u = Other.GetPlayer (user);
            if (u== null) {
                Common.SendChatMessage ("Player zero");
                return false;
            }

            Vector3D pos = Vector3D.Zero;

            if (u.Character != null) {
                pos = u.Character.GetPosition();
            } else {
                var cockpit = u.Controller.ControlledEntity as IMyCockpit;
                if (cockpit != null) {
                    pos = cockpit.GetPosition();
                }
            }

            if (pos == Vector3D.Zero) {
                Common.SendChatMessage ("Vector zero");
                return false;
            }
            var sphere = new BoundingSphereD (pos, 5000);

            var ents = MyEntities.GetTopMostEntitiesInSphere (ref sphere);
            foreach (var e in ents) {
                var grid = e as IMyCubeGrid;
                if (grid != null && grid.GetRelation (user) < 1) {
                    Common.SendChatMessage ("Bad relation:" + grid.CustomName);
                    return false; 
                }
            }

            Common.SendChatMessage ("Ok");
            return true;
        }
    }
}
