using Digi;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRageMath;

namespace Scripts.Specials
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class AlienEnvironment : MySessionComponentBase
    {
        MyPlanet planet = null;

        public void SetFog()
        {
            if (planet == null) return;
            var dd = (MyAPIGateway.Session.Camera.Position - planet.WorldMatrix.Translation).Length();
            var d = dd;

            var mlt = 0f;
            if (d < planet.AverageRadius * 1.1f) mlt = 1f;
            else
            {
                d -= planet.AverageRadius * 1.1f;
                mlt = 1f - (float)(d / (3 * planet.AverageRadius));
                mlt = Math.Max(0, mlt);
            }

            //MyVisualScriptLogicProvider.FogSetAll(mlt, 1f * mlt, VRageMath.Color.Black * mlt, mlt, mlt);
            MyAPIGateway.Session.WeatherEffects.FogDensityOverride = mlt;
            MyAPIGateway.Session.WeatherEffects.FogColorOverride = VRageMath.Color.Black * mlt;
            MyAPIGateway.Session.WeatherEffects.FogMultiplierOverride = 1f * mlt;
            MyAPIGateway.Session.WeatherEffects.FogSkyboxOverride = mlt;
            MyAPIGateway.Session.WeatherEffects.FogAtmoOverride = mlt;

        }

        public void FindPlanet()
        {
            if (planet != null && !planet.Closed)
            {
                return;
            }

            foreach (var p in GameBase.instance.planets)
            {
                if (p.Value.Generator.Id.SubtypeName == "Alien2")
                {
                    planet = p.Value;
                    break;
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (MyAPIGateway.Utilities.IsDedicated) return;
                FindPlanet();
                if (planet == null) return;

                var list = new List<int>();
                foreach (var x in MyAPIGateway.Session.GPS.GetGpsList(MyAPIGateway.Session.Player.IdentityId))
                {
                    var bb = new BoundingBoxD(x.Coords, x.Coords);
                    if (planet.IntersectsWithGravityFast(ref bb))
                    {
                        list.Add(x.Hash);
                    }
                }

                /*foreach (var x in list)
                {
                    MyAPIGateway.Session.GPS.RemoveGps(MyAPIGateway.Session.Player.IdentityId, x);
                }
                SetFog();*/
                base.UpdateAfterSimulation();
            }
            catch { }
        }
    }
}
