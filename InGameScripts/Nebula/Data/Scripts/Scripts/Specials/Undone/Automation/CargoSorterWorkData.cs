using Digi;
using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Scripts.Base;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Scripts.Specials.Automation {
    public class CargoSorterWorkData : WorkData {
        public static void SearchEntities (IMySlimBlock controller, List<IMySlimBlock> data, Action<CargoSorterWorkData> callback) {
            var dd = new CargoSorterWorkData (controller, data);
            MyAPIGateway.Parallel.Start((workData)=>{
                var ddd = workData as CargoSorterWorkData;
                if (ddd == null) return;
                ddd.BuildGraph();
            }, (workData) => {
                 var ddd = workData as CargoSorterWorkData;
                 if (ddd == null) return;
                 callback.Invoke (ddd);
             }, dd);
        }


        public List<IMySlimBlock> data;
        public IMySlimBlock controller;
        //--------------------------------------
        public HashSet<CargoSorter> otherLogics = new HashSet<CargoSorter>();
        public HashSet<IMyEntity> all = new HashSet<IMyEntity>();

        public Dictionary<IMyRefinery,ProductionVariants> refs = new Dictionary<IMyRefinery,ProductionVariants>();
        public Dictionary<IMyAssembler,ProductionVariants> assemblers = new Dictionary<IMyAssembler,ProductionVariants>();

        public Dictionary<string, List<Pair<IMyInventory, CargoOptions>>> pullCargos = new Dictionary<string, List<Pair<IMyInventory, CargoOptions>>>();
        public List<IMyInventory> drainContainers =new List<IMyInventory>();
        public Dictionary<IMyInventory, CargoOptions> cargoPullOptions = new Dictionary<IMyInventory, CargoOptions>();
        public List<IMyInventory> allContainers =new List<IMyInventory>();

        public Dictionary<string, IMyTextPanel> lcds = new Dictionary<string, IMyTextPanel>();
        public Dictionary<MyDefinitionId, ProductionTarget> productionTargets = new Dictionary<MyDefinitionId, ProductionTarget>();
        
        public CargoSorterWorkData (IMySlimBlock controller, List<IMySlimBlock> data) {
            this.data = data;
            this.controller = controller;
        }

        internal void BuildGraph () {
            try { 
                productionTargets = CargoParser.parsePriorities ((controller.FatBlock as IMyTerminalBlock).CustomData);

                foreach (var x in data) {
                    var fat = x.FatBlock;
                    if (fat == null) continue;

                    var cargo = fat as IMyCargoContainer;
                    if (cargo != null) {
                        if (cargo.CustomName.Contains("(PULL:IGNORE)")) continue;
                            
                        all.Add (cargo);

                        if (!cargo.HasLocalPlayerAccess()) {
                            Log.Info ("Skipped :"+cargo.CustomName + " Dont have access");
                            continue;
                        }
                        allContainers.Add (cargo.GetInventory());

                        CargoOptions opt;
                        if (CargoParser.tryAnalyzeName (cargo, out opt)) {
                            if (opt != null) {
                                cargoPullOptions.Add (cargo.GetInventory(), opt);
                                foreach (var item in opt.pullItems) {
                                    pullCargos.GetOrCreate (item.getString()).Add (new Pair<IMyInventory, CargoOptions>(cargo.GetInventory(), opt));
                                }
                            } else {
                                //PULL:IGNORE
                            }
                        } else {
                            drainContainers.Add (cargo.GetInventory());
                        }
                    }

                    var refinery = fat as IMyRefinery;
                    if (refinery != null) {
                        all.Add (refinery);
                        if (!refinery.HasLocalPlayerAccess()) {
                            Log.Info ("Skipped :" + refinery + " Dont have access");
                            continue;
                        }

                        var opt = parseRefine((refinery.SlimBlock.BlockDefinition as MyRefineryDefinition).BlueprintClasses);

                        refs.Add (refinery, opt);
                        allContainers.Add (refinery.OutputInventory);
                        drainContainers.Add (refinery.OutputInventory);
                        continue;
                    }

                    var assembler = fat as IMyAssembler;
                    if (assembler != null) {
                        all.Add (assembler);
                        if (!assembler.HasLocalPlayerAccess()) {
                            Log.Info ("Skipped :" + assembler + " Dont have access");
                            continue;
                        }

                        var opt = parseRefine((assembler.SlimBlock.BlockDefinition as MyAssemblerDefinition).BlueprintClasses);

                        assemblers.Add (assembler, opt);
                        allContainers.Add (assembler.OutputInventory);
                        drainContainers.Add (assembler.OutputInventory);
                        continue;
                    }


                    var lcd = fat as IMyTextPanel;
                    if (lcd != null) {
                        lcds.Add (lcd.CustomName, lcd);
                        continue;
                    }


                    var logic = fat.GameLogic.GetAs<CargoSorter>();
                    if (logic != null) {
                        otherLogics.Add (logic);
                    }
                }

                foreach (var x in pullCargos) {
                    var y = x.Key.Split('/');
                    var type = y[0];
                    var stype = y.Length == 1? "" : y[1];
                    x.Value.Sort ((a,b)=>a.v.getRawPriority(type, stype)-b.v.getRawPriority(type, stype));
                }

            } catch (Exception e) {
                Log.Error (e);
            }
        }


        public ProductionVariants parseRefine (List<MyBlueprintClassDefinition> list) {
            var temp = new List<MyBlueprintDefinitionBase.ProductionInfo>();
            var ro = new ProductionVariants();

            foreach (var x in list) {
                if (x.Id.SubtypeName.Contains ("Blocks")) {
                    continue;
                }

                foreach (var y in x) {
                    temp.Clear();
                    y.GetBlueprints(temp);
                    
                    //
                    //
                    //
                    //

                    //temp.RemoveAll ((bp)=> { return !bp.Blueprint.Public || !bp.Blueprint.Enabled || !bp.Blueprint.IsPrimary; });

                   if (temp.Count != 1) { 
                       //Common.SendChatMessage ("Skipping : " +y.DisplayNameText +" : "+ temp.Count);
                       continue; 
                   }

                    foreach (var z in temp) {
                        var bp = z.Blueprint;
                        if (!bp.Public || !bp.Enabled) continue;

                        var from = bp.Prerequisites;
                        var to = bp.Results;
                        

                        foreach (var w in to) {
                            ro.produced.GetOrCreate(w.Id).Add (new Pair<MyBlueprintDefinitionBase, MyBlueprintDefinitionBase.ProductionInfo>(y,z));
                        }  
                        foreach (var w in from) {
                            ro.resources.GetOrCreate(w.Id).Add (new Pair<MyBlueprintDefinitionBase, MyBlueprintDefinitionBase.ProductionInfo>(y,z));
                        }  
                    }
                }
            }

            return ro;
        }
    }
}
