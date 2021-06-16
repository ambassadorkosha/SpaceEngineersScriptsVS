using Scripts.Specials.Blocks.ShipSkills;
using Scripts.Specials.Systems;
using ServerMod;
using Slime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Scripts.Specials.ShipClass
{
    class ShipSubpartsLogic
    {
        public static void BeforeSimulation (Ship from)
        {
            if (!from.isApplied)
            {
                from.isApplied = true;
                from.grid.GetConnectedGrids(GridLinkTypeEnum.Physical, from.connectedGrids, true);
                foreach (var x in from.connectedGrids)
                {
                    if (x == from.grid) continue;
                    var sh = x.GetShip();
                    if (sh != null)
                    {
                        sh.connectedGrids.Clear();
                        sh.connectedGrids.AddList(from.connectedGrids);
                        sh.isApplied = true;
                    }
                }

                BeforeSimulation(from, from.connectedGrids);
            }
        } 

        public static void AfterSimulation (Ship from)
        {
            from.isApplied = false;
        }
        public static void BeforeSimulation (Ship from, List<IMyCubeGrid> connected)
        {
            ArmorModule.Logic(from, connected);
            GravityControl.ApplyForce (connected);
        }
    }
}
