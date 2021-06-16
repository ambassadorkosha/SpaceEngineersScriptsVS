using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using VRage.Game.ModAPI;
using Sandbox.Game.Entities;
using Digi;
using ObjectBuilders.SafeZone;
using SpaceEngineers.Game.ModAPI;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using Sandbox.Game.EntityComponents;
using Scripts.Specials.Messaging;
using Scripts.Specials.Faction;
using ServerMod;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;
using VRage.Game.ObjectBuilders.Components;
using Sandbox.ModAPI.Interfaces;
using Scripts.Shared;
using VRage.Game.Entity;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI.Weapons;
using Sandbox.Game.Components;
using VRage.Utils;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.Entities.Blocks;
using Scripts.Specials.Hologram.Data;
using VRage.Input;

namespace Scripts.Specials.Hologram {


    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Welder), new String []{ "Welder" })]
    public class HologramTool : MyGameLogicComponent {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            if (MyAPIGateway.Session.LocalHumanPlayer != null) return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        Random r = new Random ();

        public IMyCubeBlock getTargetBlock () {
            var tool = (Container.Entity as IMyEngineerToolBase);
            var owner = MyAPIGateway.Entities.GetEntityById (tool.OwnerId) as IMyCharacter;
            var sphere = new BoundingSphereD(owner.GetPosition(), 8f);
            var ents = MyAPIGateway.Entities.GetEntitiesInSphere (ref sphere);
            foreach (var x in ents) {
                if (x is IMyCubeBlock) {
                    return x as IMyCubeBlock;
                }
                if (x is IMySlimBlock && (x as IMySlimBlock).FatBlock != null) {
                    return (x as IMySlimBlock).FatBlock;
                }
            }

            return null;
        }

        public Hologram getHologram () {
            var tool = (Container.Entity as IMyEngineerToolBase);
            var owner = MyAPIGateway.Entities.GetEntityById (tool.OwnerId) as IMyCharacter;
            var sphere = new BoundingSphereD(owner.GetPosition(), 50f);
            var ents = MyAPIGateway.Entities.GetEntitiesInSphere (ref sphere);
            foreach (var x in ents) {
                if (x.GameLogic != null) {
                    var h = x.GameLogic.GetAs<Hologram>();
                    if (h != null) {
                        return h;
                    }
                }
            }

            return null;
        }

        public HologramProjector getHologramBlock () {
            var tool = (Container.Entity as IMyEngineerToolBase);
            var owner = MyAPIGateway.Entities.GetEntityById (tool.OwnerId) as IMyCharacter;
            var sphere = new BoundingSphereD(owner.GetPosition(), 50f);
            var ents = MyAPIGateway.Entities.GetEntitiesInSphere (ref sphere);
            foreach (var x in ents) {
                if (x.GameLogic != null) {
                    var h = x.GameLogic.GetAs<HologramProjector>();
                    if (h != null) {
                        return h;
                    }
                }
            }

            return null;
        }

        public override void UpdateAfterSimulation10() {
            base.UpdateAfterSimulation10();
            
            if (MyAPIGateway.Session.isTorchServer()) return;

            try {
                var tool = (Container.Entity as IMyEngineerToolBase);
                var caster = tool.Components.Get<MyCasterComponent>();
                var block = (caster.HitBlock as IMySlimBlock);
                var hologram = getHologram ();
                var hologramProjector = getHologramBlock ();


                if (block != null && block.FatBlock != null && hologram != null  && hologramProjector != null) {
                    List<MyMouseButtonsEnum> mouse = new List<MyMouseButtonsEnum>();
                    MyAPIGateway.Input.GetListOfPressedMouseButtons(mouse);

                    if (mouse.Contains (MyMouseButtonsEnum.Left) || mouse.Contains (MyMouseButtonsEnum.Right)) {
                         hologramProjector.Clear (block);

                        if (mouse.Contains (MyMouseButtonsEnum.Left)) {
                            var all = new List<HoloParams>();
                            var target = new HoloTarget (block.FatBlock);

                            foreach (var x in hologram.all) {
                               all.Add (x.Copy());
                            }

                            hologramProjector.Add (block, new HoloGroup(target, all));
                        }


                        hologramProjector.Save();
                    }

                }
            } catch (Exception e) {
                Log.Error (e);
            }

           
        }
    }
}
