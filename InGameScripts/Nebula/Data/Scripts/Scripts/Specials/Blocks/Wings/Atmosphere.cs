using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Digi2.AeroWings;
using VRageMath;
using VRage.Game.ModAPI;
using ServerMod;

namespace NebulaPhysics
{
	public class Atmosphere {
		double atmosphereDensity = 0;
        int atmospheres = 0;
        double wingValue;

        private const float MIN_ATMOSPHERE = 0.4f;
        private const float MAX_ATMOSPHERE = 0.7f;

        public double getAtmosphereDensity()
        {
            return atmosphereDensity;
        }

        public double getWingValue()
        {
            return wingValue;
        }

        public void updateAtmosphere(IMyCubeGrid grid) {
			if (grid == null || grid.Physics == null || grid.Physics.IsStatic) return;

			var gridCenter = grid.Physics.CenterOfMassWorld;
			var planets = GameBase.instance.planets;

			atmospheres = 0;
			atmosphereDensity = 0;
            wingValue = 0;

            foreach (var pp in planets) {
                var planet = pp.Value;
				if (Vector3D.DistanceSquared(gridCenter, planet.WorldMatrix.Translation) < (planet.AtmosphereRadius * planet.AtmosphereRadius)) {
					atmosphereDensity += planet.GetAirDensity(gridCenter);
					atmospheres++;
				}
			}

			if (atmospheres > 0) {
				atmosphereDensity /= atmospheres;
                wingValue = MathHelper.Clamp((atmosphereDensity - MIN_ATMOSPHERE) / (MAX_ATMOSPHERE - MIN_ATMOSPHERE), 0f, 1f);
			}
		}
		
		
	}
}
