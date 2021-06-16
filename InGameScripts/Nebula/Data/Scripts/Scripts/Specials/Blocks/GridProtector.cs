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

namespace ServerMod.Specials {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), true, new string[] { "AdminGridProtectorLarge", "AdminGridProtectorSmall" })]
    public class GridProtector : MyGameLogicComponent, IGridProtector {
       IMyFunctionalBlock block;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init (objectBuilder);
            //if (!MyAPIGateway.Multiplayer.IsServer) return;
            block = (Entity as IMyFunctionalBlock);
            block.OnMarkForClose += Block_OnMarkForClose;
            if (MyAPIGateway.Multiplayer.IsServer) block.CubeGrid.OnBlockAdded += OnBlockAdded;
            block.EnabledChanged += Block_EnabledChanged;
            NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            Block_EnabledChanged(block);
        }

        private void Block_EnabledChanged(IMyTerminalBlock obj) {
            if (block.Enabled) {
                CustomDamageSystem.AddEntityProtection (block.CubeGrid.EntityId, this);
            } else {
                CustomDamageSystem.RemoveEntityProtection (block.CubeGrid.EntityId);
            }
        }

        private void Block_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
            CustomDamageSystem.RemoveEntityProtection (block.CubeGrid.EntityId);
            block.OnMarkForClose -= Block_OnMarkForClose;
            if (MyAPIGateway.Multiplayer.IsServer) block.CubeGrid.OnBlockAdded -= OnBlockAdded;
        }

        public override void UpdateOnceBeforeFrame() {
            base.UpdateOnceBeforeFrame();
            if (block.Enabled) {
                CustomDamageSystem.AddEntityProtection (block.CubeGrid.EntityId, this);
            }
        }

        public bool InterceptDamage(IMyCubeGrid grid, IMySlimBlock block, ref MyDamageInformation damage) {
            if (damage.Type == MyDamageType.Debug) {
                return false;
            } else {
                if (block.BuiltBy == this.block.SlimBlock.BuiltBy)
                {
                    damage.IsDeformation = false;
                    damage.Amount = 0;
                    return true;
                }
                return true;
            }
        }

        private void OnBlockAdded(IMySlimBlock obj) { 
            //if (block.Enabled) {
            //    if (obj != block.SlimBlock && obj.BuiltBy != block.SlimBlock.BuiltBy) {
            //        obj.DoDamage (obj.MaxIntegrity*100, MyDamageType.Debug, false); 
            //    }
            //}
        }
    }

}
