using Digi;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Scripts.Shared;
using ServerMod;
using Slime;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.VisualScripting;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using MyVisualScriptLogicProvider = Sandbox.Game.MyVisualScriptLogicProvider;

namespace Scripts.Specials.Blocks {

    public class BaseSpecCore : AProtectorBlock{
        public override void UpdateAfterSimulation100() {
            base.UpdateAfterSimulation100();
            GrantProtection();
        }
    }
    
    public class AdminProtector : AProtectorBlock {
        public override void UpdateAfterSimulation100() {
            base.UpdateAfterSimulation100();
            GrantProtection();
        }
    }
    
    public abstract class AProtectorBlock : MyGameLogicComponent {
        public static void Init() {
            MyVisualScriptLogicProvider.BlockBuilt += BlockBuilt;
        }

        public static void Unload() {
            MyVisualScriptLogicProvider.BlockBuilt -= BlockBuilt;
        }
        
        private static void BlockBuilt(string typeid, string subtypeid, string gridname, long blockid) {
            //var grid = MyEntities.GetEntityByName(gridname) as MyCubeGrid;
            //if (grid != null) {
            //    var ship = grid.GetShip();
            //    if (ship == null) return;
            //    if (!ship.protection.canPlaceBlocks && ship.protection.protectors.Count > 0) {
            //        
            //    }
            //}
        }

        private IMyFunctionalBlock block;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            block = (Entity as IMyFunctionalBlock);
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public void GrantProtection() {
            if (block.CubeGrid.IsStatic) {
                if (OnlineFactions.isOnlineFaction(block.BuiltBy())) {
                    var now = FrameExecutor.currentFrame;
                    foreach (var x in block.CubeGrid.GetConnectedGrids(GridLinkTypeEnum.Logical)) {
                        
                    }
                }
            }
        }
        
    }
}