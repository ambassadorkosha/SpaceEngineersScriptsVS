/*using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;
using Digi;
using Scripts;
using System;
using Sandbox.Game.Components;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using Scripts.Base;
using VRage.Game.ModAPI;
using Scripts.Specials.Messaging;
using VRage.Game;

namespace ServerMod.Specials {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] { "TowerDefence" })]
    public class TowerDefence : MyGameLogicComponent, IGridProtector  {
        IMyFunctionalBlock block;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init (objectBuilder);
            if (!MyAPIGateway.Multiplayer.IsServer) return;

            block = (Entity as IMyFunctionalBlock);
            block.EnabledChanged += EnabledChanged;
            block.OnMarkForClose += OnMarkForClose;
            block.CubeGrid.OnBlockAdded += OnBlockAdded;
            block.CubeGrid.OnBlockRemoved += OnBlockRemoved;

            NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME | VRage.ModAPI.MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private void OnBlockAdded(IMySlimBlock obj) { 
            lock (locker) {
                TestBlock(obj); 
            }
        }

        public void TestBlock (IMySlimBlock x) {
            if (x.FatBlock is IMyUserControllableGun) {
                if (!lastTakenDamage.ContainsKey(x.FatBlock)) {
                    lastTakenDamage.Add (x.FatBlock, SharpUtils.msTimeStamp());
                }
            }
        }


        public override void UpdateOnceBeforeFrame() {
            base.UpdateOnceBeforeFrame();
            Init ();
        }

        public override void UpdateAfterSimulation10() {
            base.UpdateAfterSimulation10();

            if (!timer.tick()) return;

            try {
                var now = SharpUtils.msTimeStamp();
                lock (locker) {
                    foreach (var x in lastTakenDamage) {
                        if (x.Key.MarkedForClose) { closedBuffer.Add(x.Key); continue; }
                        if (now - x.Value > REPAIR_TIME) {
                            var rt =  x.Key.SlimBlock;
                            if (rt.Integrity != rt.MaxIntegrity) {
                                repair.Add(x.Key); 
                            }
                        }

                        if (x.Key.GetInventory().GetFilledRatio() < 0.60) {
                            refill.Add (x.Key);
                        }
                    }

                    foreach (var x in closedBuffer) {
                        lastTakenDamage.Remove (x);
                    }
                }

                foreach (var x in repair) {
                    FullRepair(x.SlimBlock);
                }
            
                foreach (var x in refill) {
                    var id = x.SlimBlock.BlockDefinition.Id;
                    if (!loadTurrets.ContainsKey(id)) {
                        continue;
                    }
                    x.GetInventory().AddItem (loadTurrets[id], 1d);
                }
                repair.Clear();
                closedBuffer.Clear();
                refill.Clear();
            } catch (Exception e) {
                Log.Error (e);
            }
        }

        public bool InterceptDamage(IMyCubeGrid grid, IMySlimBlock block, ref MyDamageInformation damage) {
            damage.IsDeformation = false;
            if (block.FatBlock is IMyUserControllableGun) {
                 lock (locker) {
                    var faaat = block.FatBlock;
                    var ms = SharpUtils.msTimeStamp();
                    if (!block.FatBlock.IsFunctional) {
                        damage.Amount = 0;
                    } else {
                        if (lastTakenDamage.ContainsKey(faaat)) {
                           lastTakenDamage[faaat] = ms;
                        }
                    }
                }
            } else {
                damage.Amount = 0;
            }
            return true;
        }

        
        

        public void Init () { 
            var connections = MyAPIGateway.GridGroups.GetGroup (block.CubeGrid, GridLinkTypeEnum.Physical);
            lock (locker) {
                foreach (var y in connections) {
                    block.CubeGrid.FindBlocks((x)=>TestBlock(x)); 
                    CustomDamageSystem.AddEntityProtection (y.EntityId, this);
                    protectedGrids.Add (y.EntityId);
                }
            }
        }


        

        private void OnBlockRemoved(IMySlimBlock obj) {
            lock (locker) {
                if (obj.FatBlock != null) {
                    lastTakenDamage.Remove (obj.FatBlock);
                }
            }
        }

        private void OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
            block.EnabledChanged -= EnabledChanged;
            block.OnMarkForClose -= OnMarkForClose;
            block.CubeGrid.OnBlockAdded -= OnBlockAdded;
            block.CubeGrid.OnBlockRemoved -= OnBlockRemoved;
            lock (locker) {
                foreach (var x in protectedGrids) {
                    CustomDamageSystem.RemoveEntityProtection (x);
                }
            }
        }

        private void EnabledChanged(IMyTerminalBlock obj) { }

        private void FullRepair (IMySlimBlock block) {
            var def = (block.BlockDefinition as MyCubeBlockDefinition);
            var bs = def.MaxIntegrity / def.IntegrityPointsPerSec;
            block.IncreaseMountLevel (bs, block.BuiltBy());
        }
    }

}
*/