using Digi;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Scripts.Base;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using Item = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace Scripts.Specials.Automation {
    partial class BaseGraph {
        static double FILL_REF_SECONDS = 20;
        static double MIN_REF_SECONDS = 10;

        private Dictionary<MyDefinitionId, MyFixedPoint> allItems = new Dictionary<MyDefinitionId, MyFixedPoint>(); //Reusable
        private Dictionary<MyDefinitionId, MyFixedPoint> prevAllItems = new Dictionary<MyDefinitionId, MyFixedPoint>(); //Reusable
        long lastCalculatedAllItems = -1;
        
        private Dictionary<MyDefinitionId, ProductionTarget> productionTargets;
        private Dictionary<MyDefinitionId, double> refineQue = new Dictionary<MyDefinitionId, double>();
        private Dictionary<MyDefinitionId, double> assembleQue = new Dictionary<MyDefinitionId, double>();
        private List<Tripple<double, double, ProductionTarget>> priorities = new List<Tripple<double, double, ProductionTarget>>();

        public void CalculateAllItems () {
           
            allItems.Clear();
            foreach (var x in allContainers) {
                x.CountItems (allItems);
            }

            try {
                if (assemblers==null) { Print (ERROR, "Assemblers == null CalculateAllItems"); }
                if (refs==null) { Print (ERROR, "refs == null CalculateAllItems"); }
                foreach (var x in assemblers) { x.Key.GetInventory(1).CountItems (allItems); }
                foreach (var x in refs) { x.Key.GetInventory(1).CountItems (allItems); }
            } catch (Exception e) {
                Log.Error(e);
            }
            
            lastCalculatedAllItems = SharpUtils.msTimeStamp();
        }

        public void CalculateRefineryQue () {
            refineQue.Clear();

            Print (PRODBLOCKS, " refs : " + refs.Count);

            foreach (var x in refs) {
                var rf = x.Key;
                var opt = x.Value;

                double speed,yield,power,ingots;
                rf.GetRefinerySpeedAndYield (out speed, out yield, out power, out ingots);

                temp_items.Clear();
                rf.InputInventory.GetItems(temp_items);
                foreach (var y in temp_items) {
                    var refines = opt.GetAvailibleRecipesForOre(y.Type);
                    if (refines == null || refines.Count == 0) {
                        Print (PRODBLOCKS, "Refinery " + x.Key.CustomName + " contains wrong Item: " + y.Type.TypeId+" "+ y.Type.SubtypeId);
                        continue;
                    }
                    var refine = refines[0];
                    var bp = refine.v.Blueprint;

                    var amount = (double)y.Amount;
                    var time = amount / (double)bp.Prerequisites[0].Amount * bp.BaseProductionTimeInSeconds / speed;

                    Print (PRODBLOCKS, "time:" + time + "(" +amount + " * " +bp.BaseProductionTimeInSeconds +" / " +speed+")");

                    foreach (var z in bp.Results) {
                        var ingot = z;
                        var ratio =(double)z.Amount / (double)bp.Prerequisites[0].Amount;
                        var outamount = amount * ratio * yield;

                        refineQue.Sum (ingot.Id, outamount);

                        Print (PRODBLOCKS, "+" + ingot.Id.toHumanString() +"/" +outamount +" ("+ amount + "*"+ratio +"*"+yield+")");
                    }
                }
            }
        }

        public double CalculateRefineryQueTime (IMyRefinery rf, ProductionVariants opt) {
            var rfOres = rf.InputInventory;
            var time = 0.0d;
            double speed,yield,power,ingots;
            rf.GetRefinerySpeedAndYield (out speed, out yield, out power, out ingots);

            temp_items.Clear();
            rfOres.GetItems(temp_items);
            foreach (var y in temp_items) {
                var refines = opt.GetAvailibleRecipesForOre(y.Type);
                if (refines == null || refines.Count == 0) { Print (ERROR, "Refinery " + rf.CustomName + " contains wrong Item: " + y.Type.SubtypeId); continue; }
                var refine = refines[0];
                var bp = refine.v.Blueprint;
                var amount = (double)y.Amount;
                time += amount / (double)bp.Prerequisites[0].Amount * bp.BaseProductionTimeInSeconds / speed;
            }

            return time;
        }

        public double CalculateAssemblerQueTime (IMyAssembler rf, ProductionVariants opt) {
            var time = 0.0d;
            double speed,power;
            rf.GetAssemblerSpeedAndPower (out speed, out power);

            var que = rf.GetQueue ();

            var neededIngots = new Dictionary<MyDefinitionId, double>();
            var leftIngots = rf.InputInventory.CountItems();

            Print (PRODBLOCKS, " que : " + que.Count);

            foreach (var z in que) {
                var bp = MyDefinitionManager.Static.GetBlueprintDefinition(z.Blueprint.Id);

                if (bp == null) {
                    Print (PRODBLOCKS, " bp is null for: " + z.Blueprint.Id);
                    throw new Exception ("No Such bp : " + z.Blueprint.Id);
                }


                var canProduce = bp.CanProduce (leftIngots);

                //((int)canProduce)*bp.BaseProductionTimeInSeconds / speed;
                    
                foreach (var w in bp.Prerequisites) {
                    var need = (w.Amount * z.Amount);
                    leftIngots.Sum(w.Id, -need);
                }

                foreach (var w in bp.Results) {
                    Print (PRODBLOCKS, "+ " + w.Id.toHumanString() + " " + (double)(w.Amount * z.Amount));
                    assembleQue.Sum (w.Id, (double)(w.Amount * z.Amount));
                }
            }

            return time;
        }
       
        public void CalculateAssembleQue () {
            assembleQue.Clear();
            foreach (var x in assemblers) {
                var rf = x.Key;
                var opt = x.Value;
                var rfOres = rf.GetInventory(0);
                var def = (rf.SlimBlock.BlockDefinition as MyAssemblerDefinition);
                var speed = (1+rf.UpgradeValues["Productivity"]);

                var que = x.Key.GetQueue ();

                var neededIngots = new Dictionary<MyDefinitionId, double>();
                var leftIngots = x.Key.InputInventory.CountItems();

                Print (PRODBLOCKS, " que : " + que.Count);

                foreach (var z in que) {
                    var bp = MyDefinitionManager.Static.GetBlueprintDefinition(z.Blueprint.Id);

                    if (bp == null) {
                        Print (PRODBLOCKS, " bp is null for: " + z.Blueprint.Id);
                        throw new Exception ("No Such bp : " + z.Blueprint.Id);
                    }
                    
                    foreach (var w in bp.Prerequisites) {
                        var need = (w.Amount * z.Amount);
                        leftIngots.Sum(w.Id, -need);
                    }

                    foreach (var w in bp.Results) {
                        Print (PRODBLOCKS, "+ " + w.Id.toHumanString() + " " + (double)(w.Amount * z.Amount));
                        assembleQue.Sum (w.Id, (double)(w.Amount * z.Amount));
                    }
                }


                var request  = dictIdFixed.get();
                var pullOut  = dictIdFixed.get();

                foreach (var y in leftIngots) {
                    if (y.Value < 0) {
                        Print (PRODBLOCKS, "Fix assembler: Pull " + y.Key + " : " + y.Value);
                        request.Add (y.Key, -y.Value);
                    } else if(y.Value > 0) {
                        Print (PRODBLOCKS, "Fix assembler: Push " + y.Key + " : " + y.Value);
                        pullOut.Add (y.Key, y.Value);
                    }
                }
                
                x.Key.InputInventory.PullRequest (allContainers, request, true);
                x.Key.InputInventory.PushRequest (allContainers, pullOut, true);

                dictIdFixed.put(request);
                dictIdFixed.put(pullOut);
            }
        }

        public void CalculatePriorities () {
            priorities.Clear();

            foreach (var x in productionTargets) { 
                double amount = (double)allItems.GetOr(x.Value.target, MyFixedPoint.Zero) + refineQue.GetOr (x.Value.target, 0) + assembleQue.GetOr (x.Value.target, 0);
                var comparedRatio = amount / x.Value.ratio;
                var minimumFilledRatio = amount / x.Value.minimum;
                priorities.Add( new Tripple<double, double, ProductionTarget>(comparedRatio, minimumFilledRatio, x.Value));
            }

            priorities.Sort((a, b) => {
                if (a.v < 1 || b.v < 1) return a.v < b.v? -1 : 1;
                return a.k < b.k? -1 : 1;
            });
        }

        public void FillRefs () {
            foreach (var x in refs) { FillOneRef (x.Key, x.Value); }
        }

        public void FillAss () {
            foreach (var x in assemblers) { FillOneAss (x.Key, x.Value); }
        }

        public void FillOneRef (IMyRefinery refinery, ProductionVariants variants) {
            var inv = refinery.GetInventory(0);
            var percent = inv.GetFilledRatio();
            double speed, power, yield, ingotsPerSec;

            refinery.GetRefinerySpeedAndYield(out speed, out yield, out power, out ingotsPerSec);

            var que = CalculateRefineryQueTime (refinery, variants);
             Print (FILLREF, "Que Time:" + que + " " + ((que > MIN_REF_SECONDS) ? " SKIP" : "FILL REF"));
            if (que > MIN_REF_SECONDS) {
                return;
            }

            var totalTime = FILL_REF_SECONDS - que;

            double time;
            double times;

            foreach (var y in priorities) {
                if (y.t.target.TypeId != AutoTools.AutoTools.Ingot) continue;

                var recipes = variants.GetAvailibleRecipes (y.t.target);
                if (recipes == null) {
                    Print (FILLREF, "Cant Produce:" + y.t.target.toHumanString() + ". No such recipe / Blocked Recipe");
                    return;
                }
                
                foreach (var z in recipes) {
                    if (tryFillWithNeedComponents (allItems, z.v.Blueprint, y.t.target, speed, refinery, totalTime, out time, out times)) {
                        totalTime -= time;
                        if (totalTime <= 0) {
                            return;
                        }
                    }
                }
            }
        }

        public void FillOneAss (IMyAssembler assembler, ProductionVariants variants) {
            var inv = assembler.GetInventory(0);
            var percent = inv.GetFilledRatio();
            double speed, power;
            assembler.GetAssemblerSpeedAndPower(out speed, out power);

            var totalTime = FILL_REF_SECONDS;
            double time;
            double times;
            foreach (var y in priorities) {
                if (percent > 0.1) break;
                if (y.t.target.TypeId != AutoTools.AutoTools.Component) continue;

                var recipes = variants.GetAvailibleRecipes (y.t.target);
                if (recipes == null) {
                    Print (FILLREF, "Cant Produce:" + y.t.target.toHumanString() + ". No such recipe / Blocked Recipe");
                    return;
                }
                
                foreach (var z in recipes) {
                    if (tryFillWithNeedComponents (allItems, z.v.Blueprint, y.t.target, speed, assembler, totalTime, out time, out times)) {
                        assembler.AddQueueItem (z.k, (MyFixedPoint)times);
                        totalTime -= time;
                        if (totalTime <= 1) {
                            return;
                        }
                    }
                }
            }
        }
    }
}
