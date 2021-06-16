using Sandbox.ModAPI;
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
using Sandbox.Common.ObjectBuilders;
using Scripts.Base;
using VRage.Game.ModAPI;
using Scripts.Specials.Messaging;
using VRage.Game;
using VRage.Utils;
using Sandbox.Game;

namespace ServerMod.Specials {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RadioAntenna), true, new string[] { "EnergyArmorController", "EnergyArmorControllerSmall" })]
    public class EnergyArmorController : MyGameLogicComponent, IGridProtector  {
        const long REPAIR_TIME = 5000;

        static MyObjectBuilderType ARMORTYPE = MyObjectBuilderType.Parse ("MyObjectBuilder_CubeBlock");
        static HashSet<MyStringHash> ARMOR_SUBTYPES = new HashSet<MyStringHash>() { { MyStringHash.TryGet ("LargeEnergyArmor1") } };



        object locker = new object();
        IMyFunctionalBlock block;

        MyInventory inv = new MyInventory(4000f, Vector3D.One, MyInventoryFlags.CanReceive | MyInventoryFlags.CanSend); //temp inventory

        Dictionary<IMySlimBlock, long> lastTakenDamage = new Dictionary<IMySlimBlock, long>();

        HashSet<IMySlimBlock> closedBuffer = new HashSet<IMySlimBlock>();
        HashSet<IMySlimBlock> repair = new HashSet<IMySlimBlock>();
        HashSet<long> protectedGrids = new HashSet<long>();

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

        public override void UpdateOnceBeforeFrame() {
            base.UpdateOnceBeforeFrame();
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            Init ();
        }

        public override void UpdateAfterSimulation10() {
            base.UpdateAfterSimulation10();
            
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            try {
                var now = SharpUtils.msTimeStamp();
                lock (locker) {
                    foreach (var x in lastTakenDamage) {
                        var slim = x.Key;
                        if (x.Key.IsDestroyed) { 
                            closedBuffer.Add(x.Key); 
                            continue; 
                        }
                        if (now - x.Value > REPAIR_TIME && slim.Integrity != slim.MaxIntegrity && slim.BuildLevelRatio > (slim.BlockDefinition as MyCubeBlockDefinition).CriticalIntegrityRatio) {
                            repair.Add(x.Key);
                        }
                    }

                    foreach (var x in closedBuffer) {
                        lastTakenDamage.Remove (x);
                    }
                }

                foreach (var x in repair) {
                    Repair(x);
                }
            
                repair.Clear();
                closedBuffer.Clear();
            } catch (Exception e) {
                Log.Error (e);
            }
        }

        public static bool isEnergyArmor (IMySlimBlock block) {
             return block.BlockDefinition.Id.TypeId == ARMORTYPE && ARMOR_SUBTYPES.Contains(block.BlockDefinition.Id.SubtypeId);
        }

        public bool InterceptDamage(IMyCubeGrid grid, IMySlimBlock block, ref MyDamageInformation damage) {
            if (isEnergyArmor (block)) {
                lock (locker) {
                    if (lastTakenDamage.ContainsKey(block)) {
                        lastTakenDamage[block] = SharpUtils.msTimeStamp();
                    }
                }
            }
            return false;
        }

        public void TestBlock (IMySlimBlock x) {
            if (isEnergyArmor(x)) {
                if (!lastTakenDamage.ContainsKey(x)) {
                    lastTakenDamage.Add (x, SharpUtils.msTimeStamp());
                }
            }
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


        private void OnBlockAdded(IMySlimBlock obj) { 
            lock (locker) {
                TestBlock(obj); 
            }
        }

        private void OnBlockRemoved(IMySlimBlock obj) {
            lock (locker) {
                if (obj.FatBlock != null) {
                    lastTakenDamage.Remove (obj);
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

        private void Repair (IMySlimBlock block) {
            var def = (block.BlockDefinition as MyCubeBlockDefinition);
            var mlt = def.IntegrityPointsPerSec;

            inv.AddItem (MyDefinitionId.Parse ("MyObjectBuilder_Component/EnergyArmorLayer"), 99999d);
            block.MoveItemsToConstructionStockpile (inv);
            block.IncreaseMountLevel (block.MaxIntegrity / 100 / mlt, block.BuiltBy, inv);
        }
    }

}
