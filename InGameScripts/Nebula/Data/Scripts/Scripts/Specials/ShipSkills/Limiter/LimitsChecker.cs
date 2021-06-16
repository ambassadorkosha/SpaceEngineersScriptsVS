using System;
using System.Collections.Generic;
using ServerMod;
using Slime;
using VRage.Game.ModAPI;
using Digi;
using Sandbox.ModAPI;

namespace Scripts.Specials
{
    public class LimitsChecker {
        //GatlingGun
        public const int TYPE_WEAPONPOINTS = 0;
        //public const int TYPE_RADAR = 1;
        public const int TYPE_SAFEZONE = 1;
        public const int TYPE_TURRETS = 2;
        public const int TYPE_ROCKETS = 3;
        public const int TYPE_JUMPDRIVES = 4;
        
        //public const int TYPE_ANTIRADAR = 6;
        public const int TYPE_WELDERS = 5;
        public const int TYPE_NANOBOTS = 6;
        public const int TYPE_POI = 7;
        public const int TYPE_AFTERBURNER = 8;
        public const int TYPE_ARMOR_MODULES = 9;
        public const int TYPE_TORPEDO = 10;

        public static HashSet<int> allKeys = new HashSet<int>() {TYPE_WEAPONPOINTS, TYPE_SAFEZONE, TYPE_TURRETS, TYPE_ROCKETS, TYPE_JUMPDRIVES, TYPE_WELDERS, TYPE_NANOBOTS, TYPE_POI, TYPE_AFTERBURNER, TYPE_ARMOR_MODULES, TYPE_TORPEDO };
        public static Dictionary<int, int> buffer = From();
        public static Dictionary<int, int> bufferTotal = From();
        public static Dictionary<int, int> EMPTY_LIMITS = From();
       
        
        public static List<SpecBlock> bufferProducers = new List<SpecBlock>(10);
        public static List<LimitedBlock> bufferConsumers = new List<LimitedBlock>(100);

        public static Dictionary<int, int> From(params int[] data) {
            var map = new Dictionary<int, int>();
            foreach (var x in allKeys) {
                map.Add(x, 0);
            }
            for (int x = 0; x < data.Length-1; x+=2) { map[data[x]] = data[x + 1]; }

            return map;
        }

        public static string GetTypeDesciption(int type) {
            switch (type) {
                case TYPE_WEAPONPOINTS : return "WEAPON POINTS";
                case TYPE_SAFEZONE : return "SAFEZONE";
                case TYPE_TURRETS : return "TURRET LIMIT";
                case TYPE_ROCKETS : return "ROCKET WEAPONS";
                case TYPE_JUMPDRIVES : return "JUMPDRIVE";
                case TYPE_WELDERS : return "WELDERS";
                case TYPE_NANOBOTS : return "NANOBOTS";
                case TYPE_POI : return "SPECIAL ASSEMBLER";
                case TYPE_AFTERBURNER : return "AFTERBURNER";
                case TYPE_ARMOR_MODULES: return "ARMOR MODULES";
                case TYPE_TORPEDO: return "TORPEDOES";
                default : return "UNKNOWN ("+type+")";
            }
        }
        
        
        private static List<IMyCubeGrid> gridBuffer = new List<IMyCubeGrid>();

        public static SpecBlock GetMainCore (Ship sh)
        {
            SpecBlock best = null;
            foreach (var g in sh.connectedGrids)
            {
                var ship = g.GetShip();
                if (ship == null) continue;

                foreach (var x in ship.limitsProducer)
                {
                    if ((x.GetPriority() > (best?.GetPriority() ?? -9999)) && x.CanBeApplied(sh.connectedGrids))
                    {
                        best = x;
                    }
                }
            }

            return best;
        }

        public static void CheckLimitsInGrid(IMyCubeGrid grid) {
            try {
                foreach (var x in allKeys) { buffer[x] = 0; bufferTotal[x] = 0; }

                var grids = grid.GetConnectedGrids(GridLinkTypeEnum.Physical, gridBuffer, true);
                if (grids == null) return;
                
                bufferProducers.Clear();
                bufferConsumers.Clear();
                
                foreach (var g in grids) {
                    var ship = g.GetShip();
                    if (ship == null) continue;
                    ship.limitsLastChecked.reset(); //Reseting checks    

                    foreach (var x in ship.limitedBlocks) {
                        bufferTotal.Plus(x.GetLimits());
                        if (!x.IsDrainingPoints()) continue;
                        bufferConsumers.Add(x);
                        buffer.Plus(x.GetLimits());
                    }

                    foreach (var x in ship.limitsProducer) {
                        if (x.CanBeApplied(grids)) {
                            bufferProducers.Add(x);
                        } 
                    }
                }

                var maxLimits = EMPTY_LIMITS;
                SpecBlock producer = null;
                if (bufferProducers.Count > 0) {
                    bufferProducers.Sort((a,b)=>b.GetPriority() - a.GetPriority());
                    foreach (var x in bufferProducers) { x.activationError = SpecBlockActivationError.ANOTHER_CORE; }
                    producer = bufferProducers[0];
                    producer.foundLimits.Clear();
                    producer.foundLimits.Plus(bufferTotal);
                    maxLimits = producer.GetLimits();
                    producer.activationError = SpecBlockActivationError.NONE;
                }

                foreach (var x in bufferConsumers)
                {
                    if (!x.CheckConditions(producer))
                    {
                        x.Disable();
                    }
                }

                if (!buffer.IsOneKeyMoreThan(maxLimits)) //Exceeding limits
                {
                    bufferConsumers.Clear();
                    bufferProducers.Clear();
                    return;
                }

                

                bufferConsumers.Sort((a, b) => (int) (a.block.EntityId - b.block.EntityId));
                
                foreach (var x in allKeys) { buffer[x] = 0; }

                foreach (var x in bufferConsumers) {
                    var l = x.GetLimits();
                    buffer.Plus(l);

                    if (!x.CanBeDisabled())
                    {
                        continue;
                    }

                    if (buffer.IsOneKeyMoreThan(maxLimits)) {
                        x.Disable();
                        buffer.Minus(l);
                    }
                }

                //Still violating;
                if (buffer.IsOneKeyMoreThan(maxLimits))
                {
                    foreach (var x in bufferConsumers)
                    {
                        x.Disable();
                    }
                }

                bufferConsumers.Clear();
                bufferProducers.Clear();
            } catch (Exception e) {
                Log.ChatError("Checker:"+e.ToString());
            }
        }

        public static void OnGridSplit(IMyCubeGrid arg1, IMyCubeGrid arg2) {
            if (MyAPIGateway.Session.IsServer) { return; }
            if (!arg1.InScene) { return; }
            CheckLimitsInGrid(arg1);
            if (!arg2.InScene) { return; }
            CheckLimitsInGrid(arg2);
        }
    }
}