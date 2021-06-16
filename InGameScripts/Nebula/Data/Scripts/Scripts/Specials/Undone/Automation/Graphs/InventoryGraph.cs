using Digi;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game;

namespace Scripts.Specials.Automation {
    partial class BaseGraph {
        static Random r = new Random ();
        
        
        int i = 0;

        public void OnReadyToWork (CargoSorterWorkData data) {
            var start = SharpUtils.msTimeStamp();
            data.drainContainers.Sort((x,y) => {  
                var a = x.CurrentVolume;
                var b = y.CurrentVolume;
                return a > b ? 1 : a==b ? 0 : -1; 
            });

            all = data.all;
            drainContainers = data.drainContainers;
            pullContainters = data.pullCargos;
            allContainers = data.allContainers;
            cargoPullOptions = data.cargoPullOptions;
            assemblers = data.assemblers;
            refs = data.refs;
            productionTargets = data.productionTargets;
            lcds = data.lcds;

            Log.Info ("OnReadyToWork:" + drainContainers.Count + " " + pullContainters.Count);
            calculateGraph ();
            var end = SharpUtils.msTimeStamp();

            Common.SendChatMessage("GraphCalculate:"+ (end-start));
        }


        public void Work () {
            try {
                switch (i%16) {
                    case 0: Job0(); break;
                    case 1: Job1(); break;
                    case 2: Job2(); break;
                    case 3: Job3(); break;
                    case 4: Job4(); break;
                    default: JobInventory (); break;
                }

                DisplayChanges ();

                 i++;

                //RPrint (GRAPH);
                //RPrint (ERROR);
            } catch (Exception e) {
                Log.Error (e);
            }
        }


        long lastChangesDisplay = 0;
        public void DisplayChanges () {
            if (SharpUtils.msTimeStamp() - lastChangesDisplay > 30000) {
                var keys = new HashSet<MyDefinitionId>();

                Clear (CHANGE);
                Print (CHANGE, "CHANGE:"+ " (last:" + SharpUtils.msTimeStamp() + ")");

                keys.Clear();
                foreach (var x in allItems.Keys) { keys.Add (x); }
                foreach (var x in prevAllItems.Keys) { keys.Add (x); }

                foreach (var x in keys) {
                    var now = allItems.GetOr (x, MyFixedPoint.Zero);
                    var was = prevAllItems.GetOr (x, MyFixedPoint.Zero);

                    if (was != now) {
                        Print (CHANGE, String.Format ("{0} : {1}", x.toHumanString(), ((double)(now - was)).toHumanQuantity()));
                    }
                }

                lastChangesDisplay = SharpUtils.msTimeStamp();

                prevAllItems.Clear();
                foreach (var x in allItems) {
                    prevAllItems.Add (x.Key, x.Value);
                }
                RPrint (CHANGE);
            }
        }

        public void Job0 () {
            CalculateAllItems ();
            Clear (ITEMS);
            Print (ITEMS, "ITEMS:"+ " (last:" + SharpUtils.msTimeStamp() + ")");
            var keys = new HashSet<MyDefinitionId>();
            
            foreach (var x in allItems.Keys) { keys.Add (x); }
            foreach (var x in refineQue.Keys) { keys.Add (x); }
            foreach (var x in assembleQue.Keys) { keys.Add (x); }

            foreach (var x in keys) {
                var current = allItems.GetOr (x, MyFixedPoint.Zero);
                var future = refineQue.GetOr (x, 0) + assembleQue.GetOr (x, 0);

                Print (ITEMS, String.Format ("{0,-20} {1} | +{2}", x.toHumanString(), ((double)current).toHumanQuantity(), future.toHumanQuantity()  ));
            }

            RPrint (ITEMS);
        }

        public void Job1 () {
            Clear (PRODBLOCKS);

            Print (PRODBLOCKS, "PRODBLOCKS: " + "(last:" + SharpUtils.msTimeStamp() + ")");

            CalculateAssembleQue ();
            CalculateRefineryQue ();
           
            Print (PRODBLOCKS, "SUM:");
            foreach (var x in refineQue) {
                Print (PRODBLOCKS, x.Key.toHumanString() + " : " + x.Value);
            }
            RPrint (PRODBLOCKS);
        }

        public void Job2 () {
            Clear (PRIOR);
            Print (PRIOR, "PRIORS"+ " (last:" + SharpUtils.msTimeStamp() + ")");
            foreach (var x in productionTargets) {
                Print (PRIOR, x.Value.target + " ratio =" + x.Value.ratio + " min:" +x.Value.minimum + " max:"+x.Value.maximum);
            }
            RPrint (PRIOR);


            CalculatePriorities ();
            Clear (PROD);
            Print (PROD, "PROD:" + priorities.Count + "/" + productionTargets.Count+ " (last:" + SharpUtils.msTimeStamp() + ")");
           
            foreach (var x in priorities) {
                var target = productionTargets[x.t.target];
                double amount1 = (double)allItems.GetOr (target.target, MyFixedPoint.Zero);
                double amount2 = refineQue.GetOr (target.target, 0) + assembleQue.GetOr (target.target, 0);
                 
                Print (PROD, x.t.target.toHumanString() + " : over=" + x.k.fixZero() + " min=" + x.v.fixZero() + " ratio=" + target.ratio.fixZero() + " have=" + amount1.toHumanQuantity() + " / queued=" + amount2.toHumanQuantity());
            }
            RPrint (PROD);
        }


        public void Job3 () {
            //Clear (FILLREF);
            //Print (FILLREF, "FILLREF"+ " (last:" + SharpUtils.msTimeStamp() + ")");
            FillRefs();
            RPrint (FILLREF);
        }

        public void Job4 () {
            //Clear (FILLREF);
            //Print (FILLREF, "FILLREF"+ " (last:" + SharpUtils.msTimeStamp() + ")");
            FillAss ();
            RPrint (FILLREF);
        }

        public void JobInventory () {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Clear (TLOGS);
            if (allContainers.Count > 0) {
                while (true) {
                    var x = allContainers[r.Next () % allContainers.Count];
                    tryPush (x, sw);
                    if (sw.Elapsed.TotalMilliseconds > MAX_TIME_TO_EXECUTE) {
                        break;
                    }
                }
            }
            RPrint (TLOGS);
        }

        


    }
}
