using Digi;
using Sandbox.ModAPI;
using Scripts.Specials.Automation;
using ServerMod;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Scripts {
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), true, new string[] { "LargeBlockBeacon", "LargeGridCore", "CompactGridCore" })]
    public class CargoSorter : MyGameLogicComponent {
        AutoTimer timer = new AutoTimer(6*10 * 6);//*60);//1 min
        internal bool isWorking = true;

        IMyCubeGrid grid;
        IMyCubeBlock block;

        private List<IMySlimBlock> blocks = new List<IMySlimBlock>();
        private List<IMySlimBlock> blocks2 = new List<IMySlimBlock>();
        private BaseGraph graph = new BaseGraph();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            if (MyAPIGateway.Session.isTorchServer()) return;

            block = (Entity as IMyCubeBlock);
            grid = block.CubeGrid;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }
        
        public override void UpdateBeforeSimulation10() {
            if (MyAPIGateway.Session.Player.Character == null) return;
            var d = MyAPIGateway.Session.Player.Character.GetPosition() - Entity.GetPosition();
            if (d.Length() > 200) { return; }

            if (timer.tick()) { FindCargos (); }
            if (!isWorking) { return; }
            graph.Work ();
        }

        private void FindCargos () {
            blocks.Clear();
            blocks2.Clear();

            var connections = MyAPIGateway.GridGroups.GetGroup (grid, GridLinkTypeEnum.Physical);
            foreach (var x in connections) {
                x.GetBlocks(blocks);//TODO check if need to clear
                blocks2.AddList(blocks);
                blocks.Clear();
            }

            CargoSorterWorkData.SearchEntities (block.SlimBlock, new List<IMySlimBlock>(blocks2), BuildGraphFinished);
        } 

        internal void BuildGraphFinished (CargoSorterWorkData data) {
            var worker = enableOnlyOneCargoSorter (data);
            worker.graph.OnReadyToWork (data);
        }

        public CargoSorter enableOnlyOneCargoSorter (CargoSorterWorkData data) {
            try { 
                bool canWork = true;
                CargoSorter currentWorking = this;


                if (data.otherLogics.Count > 1) { 
                    foreach (var x in data.otherLogics) {
                        if (currentWorking.block.GetRelationToOwnerOrBuilder (x.block.GetOwnerOrBuilder()) == 1) {
                            x.isWorking = false;
                            if (x.Entity.EntityId < currentWorking.Entity.EntityId) {
                                currentWorking.isWorking = false;
                                currentWorking = x;
                            }
                        }
                    }
                }
                currentWorking.isWorking = true;
                return currentWorking;

            } catch (Exception e) {
                Log.Error (e);
                return this;
            }
        }

        
    }
}
