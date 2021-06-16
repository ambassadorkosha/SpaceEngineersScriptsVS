using Digi;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Scripts.Base;
using Scripts.Shared;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using Item = VRage.Game.ModAPI.Ingame.MyInventoryItem;
using FP = VRage.MyFixedPoint;
using ID = VRage.Game.MyDefinitionId;

namespace Scripts.Specials.Automation {
    partial class BaseGraph {
        public static ObjectPool<Dictionary<ID, FP>> dictIdFixed = new ObjectPool<Dictionary<ID, FP>>(()=>new Dictionary<ID, FP>(), (x)=>x.Clear()); 


        public static string GRAPH = "(PULL:GRAPH)";
        public static string PROD = "(PULL:PROD)";
        public static string PRIOR = "(PULL:PRIOR)";
        public static string ITEMS = "(PULL:ITEMS)";
        public static string CHANGE = "(PULL:CHANGE)";
        public static string ERROR = "(PULL:ERROR)";
        public static string FILLREF = "(PULL:FILLREF)";
        public static string TLOGS = "(PULL:TLOGS)";
        public static string PRODBLOCKS = "(PULL:PRODBLOCKS)";

        static float MAX_TIME_TO_EXECUTE = 0.5f;

        protected HashSet<IMyEntity> all;

        protected List<IMyInventory> allContainers;
        protected List<IMyInventory> drainContainers;
        protected Dictionary<IMyInventory, CargoOptions> cargoPullOptions;
        protected Dictionary<String, List<Pair<IMyInventory, CargoOptions>>> pullContainters;
       
        protected Dictionary<IMyRefinery,ProductionVariants> refs;
        protected Dictionary<IMyAssembler,ProductionVariants> assemblers;

       
        protected Dictionary<IMyInventory, Dictionary<String, List<Pair<IMyInventory, CargoTypeOptions>>>> connected = new Dictionary<IMyInventory, Dictionary<String, List<Pair<IMyInventory, CargoTypeOptions>>>>();
        protected Dictionary<IMyInventory, HashSet<IMyInventory>> connectedEnities = new Dictionary<IMyInventory, HashSet<IMyInventory>>();

        
        protected Dictionary<string, IMyTextPanel> lcds = new Dictionary<string, IMyTextPanel>();
        protected Dictionary<string, StringBuilder> lcdOutputs = new Dictionary<string, StringBuilder>();

        protected List<Item> temp_items = new List<Item>();

        public void Print (String key, String what) {
            var lcd = lcds.GetOr(key, null);
            if (lcd == null) return;

            var sb = lcdOutputs.GetOr(key, null);
            if (sb == null) {
                sb = new StringBuilder();
                lcdOutputs.Add (key, sb);
            }
            sb.Append (what).Append("\n");
        }

        public void RPrint (String key) {
            var lcd = lcds.GetOr(key, null);
            if (lcd == null) return;

            var sb = lcdOutputs.GetOr(key, null);
            if (sb == null) {
                sb = new StringBuilder();
                lcdOutputs.Add (key, sb);
            }
            lcd.WritePublicText (sb.ToString(), false);
        }

        public void Clear (String key) {
            var lcd = lcds.GetOr(key, null);
            if (lcd == null) return;

            var sb = lcdOutputs.GetOr(key, null);
            if (sb == null) {
                sb = new StringBuilder();
                lcdOutputs.Add (key, sb);
            }
            sb.Clear();
        }


        protected void calculateGraph () {
            StringBuilder sb = new StringBuilder();

            try { 
                connected = new Dictionary<IMyInventory, Dictionary<String, List<Pair<IMyInventory, CargoTypeOptions>>>>();
                foreach (var x in allContainers) {
                    foreach (var y in pullContainters) {
                         foreach (var z in y.Value) {

                            var cn = x.IsConnectedTo (z.k);

                            sb.Append ((cn ? "CONNECTED " : "NOT CONNECTED ")).Append (x.CustomName()).Append (" -> ").Append (z.k.CustomName()).Append (" | ");
                            foreach (var tt in z.v.pullItems) {
                                sb.Append(tt.ToString()).Append(" ");
                            }
                            sb.Append("\n");

                            //;

                            if (cn) {
                                foreach (var it in z.v.pullItems) {
                                    connected.GetOrCreate(x).GetOrCreate(it.getString()).Add (new Pair<IMyInventory, CargoTypeOptions>(z.k, it));
                                }
                                
                                try {
                                     var available = connected.GetValueOrDefault (x,null);
                                } catch (Exception e){
                                     Print (GRAPH, "GRAPH:" + e.Message + " " + e.StackTrace);
                                }
                                
                            }
                        }
                    }
                }
                foreach (var x in all) { 
                    foreach (var y in all) { 
                        if (x != y) {
                            var cn = x.GetInventory().IsConnectedTo (y.GetInventory());
                            if (cn) {
                                connectedEnities.GetOrCreate (x.GetInventory()).Add (y.GetInventory());
                            }
                        }
                    }
                }

                

            } catch (Exception e) {
                Log.Error (e);
            } finally {
                Print (GRAPH, sb.ToString());
            }
        }


        
        public void tryPush (IMyInventory inv, Stopwatch sw) {
            temp_items.Clear();
            inv.GetItems(temp_items);

            var av = connected.GetValueOrDefault (inv, null);
            if (av == null || av.Count == 0) {
                Print (TLOGS, "TRANSFER: GRAPH ERROR:" + connected.Count + " [" +inv.CustomName()+"]");
                return;
            }

            foreach (var item in temp_items) {
                var t = item.Type;
                var available = av.GetValueOrDefault (item.Type.TypeId,null);
                var available2 = av.GetValueOrDefault (item.Type.TypeId+"/"+item.Type.SubtypeId,null);

                if ((available == null || available.Count == 0) && (available2 == null || available2.Count == 0)) {

                    Print (TLOGS, "TRANSFER: NO CONNECTED CARGOS OF THIS TYPE:" + item.Type.TypeId + " " + inv.CustomName());
                    continue;
                }

                var available3 = new List<Pair<IMyInventory, CargoTypeOptions>>();
                if (available != null) available3.AddRange (available);
                if (available2 != null) available3.AddRange (available2);

                available3.Sort ((a,b)=>a.v.priority - b.v.priority); //TODO REMOVE DUPLICATED

                var mySettings = cargoPullOptions.GetOr(inv, null);

                //TryTransfer (inv, mySettings, item, 0, available3);
                
                if (sw.Elapsed.TotalMilliseconds > MAX_TIME_TO_EXECUTE) {
                    break;
                }
            }
        }


        private void TryTransfer (IMyInventory inv, CargoOptions mySettings, double min, double max, Item item, int position, List<Pair<IMyInventory, CargoTypeOptions>> available) {
            try { 
                var o = mySettings.getOptions(item.Type.TypeId, item.Type.SubtypeId);


                var myPriority = o == null ? 11 : o.priority;
                var myMin = o == null ? -1 : o.min;
                var myMax = o == null ? MyFixedPoint.MaxValue : o.max;


                var amount = item.Amount;

                var overMaxAmount = amount - myMax;
                var overMinAmount = amount - myMin;


                Print (TLOGS, "TRY TRANSFER :" + inv.CustomName());
                var was = inv.ItemCount;


                foreach (var z in available) {
                    var targetInv = z.k;
                    var targetOptions = z.v;
                    

                    if (myPriority <= targetOptions.priority) { 
                        if (overMaxAmount > 0) { //force push out
                            
                        } else if (overMinAmount > 0 && targetOptions.min > 0) { // if i have more than needed, and it needs ore;
                            var am = z.k.CountItemType(targetOptions.type, targetOptions.subtype);
                            var needAmount = am - targetOptions.min;
                            if (needAmount > 0) {
                                if (targetInv.IsFull) {  continue; } 
                                inv.TransferItemTo (targetInv, item, needAmount);
                            }
                        }
                        continue; 
                    }
                    
                    if (targetInv.IsFull) {  continue; } 
                    inv.TransferItemTo (targetInv, item, null);
                }
             } catch (Exception e) {
                Log.Error (e);
            }
        }

        public bool tryFillWithNeedComponents (Dictionary<MyDefinitionId, MyFixedPoint> allItems,  MyBlueprintDefinitionBase bp, MyDefinitionId what, double speed, IMyProductionBlock block, double timeLeft, out double productionTime, out double times) {
            var can_produce_times = bp.CanProduce (allItems);
            if (can_produce_times < 1) {
                Print (FILLREF, "Cant Produce:" + what.toHumanString() +" with [" + bp.DisplayNameText + "]. No needed ingridients");
                productionTime = 0;
                times = 0;
                return false;
            }
            var totalTime =  Math.Min(timeLeft, bp.BaseProductionTimeInSeconds * can_produce_times / speed);
            var needP = totalTime * speed / bp.BaseProductionTimeInSeconds;

            if (block is IMyAssembler) {
                needP = (int)needP;
            }

            Print (FILLREF, "Can:" + what.toHumanString() +" with [" + bp.DisplayNameText + "]. Tm: " + can_produce_times.toHumanQuantity() + ". Take " + needP + " " + totalTime + " " +speed+ " " + bp.BaseProductionTimeInSeconds);

            var pull = new Dictionary<MyDefinitionId, MyFixedPoint>();
            foreach (var ore in bp.Prerequisites) {
                var am = ore.Amount * (MyFixedPoint)needP;
                pull.Add (ore.Id, am);
                Print (FILLREF, "Pull:" + ore.Id.toHumanString() + " " + ((double) pull[ore.Id]));//.toHumanQuantity());
            }

            var list = new List<IMyInventory> ();

            foreach (var tt in allContainers) {
                list.Add (tt);
            }

            Print (FILLREF, "Pull:List " + allContainers.Count);
            block.InputInventory.PullRequest (list, pull, alwaysAtEnd:true);
            productionTime = totalTime;
            times = needP;
            return true;
        }
    }
}
