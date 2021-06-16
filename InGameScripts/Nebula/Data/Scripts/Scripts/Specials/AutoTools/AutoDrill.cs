using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.Game.Entities;
using VRage.Input;
using ServerMod;
using Sandbox.Common.ObjectBuilders;

namespace Scripts.Specials.AutoTools
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_HandDrill), true, new[]{ "HandDrill","HandDrill2","HandDrill3","HandDrill4" })]
    public class AutoDrill : MyGameLogicComponent {
        public static List<string> skippedOre = new List<string>(){ "Stone", "Ice"};

        List<MyMouseButtonsEnum> mouse = new List<MyMouseButtonsEnum>();
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            if (MyAPIGateway.Session.LocalHumanPlayer != null) return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false) {
            return Container.Entity.GetObjectBuilder(copy);
        }
        public override void UpdateBeforeSimulation10() {
            try {
                if (MyAPIGateway.Session.Player != null) {
                    if ((Container.Entity as IMyHandheldGunObject<Sandbox.Game.Weapons.MyToolBase>).IsShooting) { 
                        mouse.Clear();
                        MyAPIGateway.Input.GetListOfPressedMouseButtons(mouse);

                        var isRightClick =  mouse.Contains (MyMouseButtonsEnum.Right);
                        
                        AutoTools.CollectOrDeleteItemsNear (4f, ContainerRange (), isRightClick, 
                            (x)=>{ return x.Item.Content.TypeId == AutoTools.Ore && !skippedOre.Contains(x.Item.Content.SubtypeName); }, 
                            (x)=>{ return x.Item.Content.TypeId == AutoTools.Ore; }
                        );

                        if (isRightClick) {
                            AutoTools.TryPullOut (ContainerRange ());
                        }
                    }
                }
            } catch(Exception ex) { }
        }

        public float ContainerRange () {
            var name = (Container.Entity as IMyHandheldGunObject<Sandbox.Game.Weapons.MyToolBase>).DefinitionId.SubtypeName;
            return AutoTools.containerRadiuses.GetOr (name, -1);
        }
        
    }
}
