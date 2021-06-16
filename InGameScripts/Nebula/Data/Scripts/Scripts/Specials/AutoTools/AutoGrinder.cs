using Digi;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using ServerMod;
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.Input;

namespace Scripts.Specials.AutoTools
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AngleGrinder), true, new String []{ "AngleGrinder","AngleGrinder2","AngleGrinder3","AngleGrinder4" })]
    public class AutoGrinder : MyGameLogicComponent {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            if (MyAPIGateway.Session.LocalHumanPlayer != null) return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }
        public float ContainerRange () {
            var name = (Container.Entity as IMyHandheldGunObject<Sandbox.Game.Weapons.MyToolBase>).DefinitionId.SubtypeName;
            return AutoTools.containerRadiuses.GetOr (name, -1);
        }
        
        public override void UpdateBeforeSimulation10() {
            try {
                if (MyAPIGateway.Session.Player != null) {
                    var tool =(Container.Entity as IMyHandheldGunObject<Sandbox.Game.Weapons.MyToolBase>);
                    if (!tool.IsMyTool()) return;
                    if (tool.IsShooting && tool.IsMyTool()) { 
                        PullFromStockpile ();
                        AutoTools.TryPullOut (ContainerRange ());// CheckWelding(tool as IMyEngineerToolBase, 15f);

                        AutoTools.CollectOrDeleteItemsNear (4f, ContainerRange (), false, 
                            (x)=>{ return x.Item.Content.TypeId != AutoTools.Ore; }, 
                            (x)=>{ return false; }
                        );
                    }
                }
            } catch(Exception ex) {
                Log.Error (ex);    
            }
        }
        
        static MyInventory temp = new MyInventory(4000f, Vector3D.One, MyInventoryFlags.CanReceive | MyInventoryFlags.CanSend);

        public void PullFromStockpile () {
            var tool = (Container.Entity as IMyHandheldGunObject<Sandbox.Game.Weapons.MyToolBase>);
            var caster = (tool as IMyEngineerToolBase).Components.Get<MyCasterComponent>();
            var block = (caster.HitBlock as IMySlimBlock);
            if (block == null) return;

            block.MoveItemsFromConstructionStockpile(temp);
            if (temp.ItemCount == 0) return;
            block.MoveItemsToConstructionStockpile(temp);

            List<MyEntity> cargos = new List<MyEntity>();
            var sphere2 = new BoundingSphereD (MyAPIGateway.Session.Player.GetPosition(), ContainerRange());
            InventoryUtils.GetAllCargosInRange(ref sphere2, cargos, AutoTools.IgnorePut, false, (term) => term.GetInventory(0).GetLeftVolumeInLiters() > 200, (a,b)=>b.GetInventory().CurrentVolume >= a.GetInventory().CurrentVolume ? 1 : -1, maxTake:1);
           
            if (cargos.Count == 0) return;
            
            AutoTools.PullOutContructionsRequest (block.CubeGrid.EntityId, block.Position, cargos[0].EntityId);
        }

    }
}
