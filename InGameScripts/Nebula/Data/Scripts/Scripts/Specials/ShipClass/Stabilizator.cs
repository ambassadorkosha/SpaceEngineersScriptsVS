using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using ServerMod;
using VRage.Game.Components;
using VRageMath;
using System;
using Sandbox.Definitions;
using VRage.ObjectBuilders;

namespace Scripts
{

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, new string[] { 
        "LGStabilizator", "LGStabilizatorT02", "LGStabilizatorT03", "LGStabilizatorT04", "LGStabilizatorT05", "LGStabilizatorT06", "LGStabilizatorT07", "LGStabilizatorT08", "LGStabilizatorT09", "LGStabilizatorT10", "LGStabilizatorT11", "LGStabilizatorT12", 
        "SGStabilizator", "SGStabilizatorT02", "SGStabilizatorT03", "SGStabilizatorT04", "SGStabilizatorT05", "SGStabilizatorT06", "SGStabilizatorT07", "SGStabilizatorT08", "SGStabilizatorT09", "SGStabilizatorT10", "SGStabilizatorT11", "SGStabilizatorT12" 
        })]

    public class Stabilizator : MyGameLogicComponent
	{

        //class Settings {
        //    float Force = 200;
        //}

        //private static Sync<Settings, Stabilizator> Sync;

        float Force = 200;
        IMyUpgradeModule block;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
            block = (IMyUpgradeModule)Entity;
            var def = (block.SlimBlock.BlockDefinition as MyUpgradeModuleDefinition);
            var sn = def.Id.SubtypeName;

            int lvl = 1;
            if (sn.EndsWith("T02")) lvl = 2;
            else if (sn.EndsWith("T03")) lvl = 3;
            else if (sn.EndsWith("T04")) lvl = 4;
            else if (sn.EndsWith("T05")) lvl = 5;
            else if (sn.EndsWith("T06")) lvl = 6;
            else if (sn.EndsWith("T07")) lvl = 7;
            else if (sn.EndsWith("T08")) lvl = 8;
            else if (sn.EndsWith("T09")) lvl = 9;
            else if (sn.EndsWith("T10")) lvl = 10;
            else if (sn.EndsWith("T11")) lvl = 11;
            else if (sn.EndsWith("T12")) lvl = 12;

            Force *= (float)Math.Pow (1.5+0.2, lvl-1);
            if (def.CubeSize == VRage.Game.MyCubeSize.Small)
            {
                Force /= 20;
            }
        }

        //public static void InitControls ()
        //{
        //    Sync = new Sync<Settings, Stabilizator>();
        //}

        public static void Logic(Ship ship)
        {
            if (ship.Stabilizators.Count == 0) return;
            var ph = ship.grid.Physics;
            if (ph == null || ph.AngularVelocity == Vector3.Zero) return;

            var force = 0f;
            Vector3D actions = Vector3D.Zero;

            if (ship.PilotActions != Vector3D.Zero)
            {
                actions = ship.PilotActions;
            }

            foreach (var y in ship.Stabilizators)
            {
                if (y.block.Enabled)
                {
                    force += y.Force;
                }
            }
            
            var mass = ship.massCache.shipMass;


            var l = ph.AngularVelocity.Length();
            var impulse = (l * mass - force) / mass;
            if (impulse < 0) impulse = 0;
            
            var v = ph.AngularVelocity;
            v.Normalize();
            v *= (float)impulse;
            ph.AngularVelocity = v;
        }
    }
}
