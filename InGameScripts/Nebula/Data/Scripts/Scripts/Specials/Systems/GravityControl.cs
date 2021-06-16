using Sandbox.Game.Entities;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Scripts.Specials.Systems
{
    static class GravityControl
    {
        public static MyStringHash QUN = MyStringHash.GetOrCompute("Qun"); 
        public static void ApplyForce (List<IMyCubeGrid> grids)
        {
            bool invertGravity = false;
            bool tested = false;
            MyPlanet planet = null;
            foreach (var x in grids)
            {
                if (x.Physics == null) return;
                if (x.Physics.Gravity == Vector3.Zero) return;
                var sh = x.GetShip();
                if (sh == null) return;
                planet = sh.closestPlanet;
                if (planet == null) return;
                if (tested) continue;
                tested = true;
                if (planet.Generator.Id.SubtypeId != QUN) return;

                var p = (planet.WorldMatrix.Translation - x.WorldMatrix.Translation);
                p.Normalize();
                var g = x.Physics.Gravity;
                if ((g + p).LengthSquared() > (g - p).LengthSquared())
                {
                    invertGravity = true;
                }
            }

            if (invertGravity)
            {
                foreach (var x in grids)
                {
                    x.Physics.Gravity = -x.Physics.Gravity;
                }
            }
        }
    }
}
