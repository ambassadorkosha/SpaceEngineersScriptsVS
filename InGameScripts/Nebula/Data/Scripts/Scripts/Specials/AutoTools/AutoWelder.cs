using Digi;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using Scripts.Base;
using Scripts.Shared;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using Slime;

namespace Scripts.Specials.AutoTools {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Welder), true, new String []{ "Welder","Welder2","Welder3","Welder4" })]
    public class AutoWelder : MyGameLogicComponent {
        static List<Tripple<long, uint, int>> drains = new List<Tripple<long, uint, int>>();
        static Dictionary<string, int> neededComps = new Dictionary<string, int>();
        static AutoTimer timer = new AutoTimer (6);

        List<MyMouseButtonsEnum> mouse = new List<MyMouseButtonsEnum>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            if (MyAPIGateway.Session.LocalHumanPlayer != null) return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false) {
            return Container.Entity.GetObjectBuilder(copy);
        }

        public float ContainerRange () {
            var name = (Container.Entity as IMyHandheldGunObject<Sandbox.Game.Weapons.MyToolBase>).DefinitionId.SubtypeName;
            return AutoTools.containerRadiuses.GetOr (name, -1);
        }
        
        static DateTime lastTook = DateTime.Now;
        
        public override void UpdateBeforeSimulation10() {
            try {
                if (MyAPIGateway.Session.Player != null) {
                    var tool =(Container.Entity as IMyHandheldGunObject<Sandbox.Game.Weapons.MyToolBase>);
                    if (!tool.IsMyTool()) return;

                    mouse.Clear();
                    
                    MyAPIGateway.Input.GetListOfPressedMouseButtons(mouse);

                    if (mouse.Contains (MyMouseButtonsEnum.Middle) && mouse.Count == 1 && (DateTime.Now - lastTook).TotalMilliseconds >= 400) {
                        lastTook = DateTime.Now;
                        AutoTools.TryPullInContructions(ContainerRange ());
                    } else {
                        if (tool.IsShooting) { 

                            var caster = (tool as IMyEngineerToolBase).Components.Get<MyCasterComponent>();
                            var block = (caster.HitBlock as IMySlimBlock);

                            CheckWelding(block, ContainerRange ());

                            //SplashWelding (MyAPIGateway.Session.Player.Character.WorldMatrix.Translation, 15f);
                            
                            AutoTools.CollectOrDeleteItemsNear (4f, ContainerRange (), false, 
                                (x)=>{ return x.Item.Content.TypeId != AutoTools.Ore; }, 
                                (x)=>{ return false; }
                            );
                        }   
                    }

                    UpgraderTool.UpgradeOrDowngrade (tool);
                }

                //if (MyAPIGateway.Session.IsServer) {
                //    SplashWeldingServer ();
                //}
            } catch(Exception ex) {
                Log.Error (ex);
            }
        }

        public static Dictionary<MyDefinitionId, int> calculateNeededComponents (Vector3D position, float radius) {
            var sphere = new BoundingSphereD (position, radius);
            var list = MyEntities.GetEntitiesInSphere (ref sphere);
            
            var leftNeeded = new Dictionary<MyDefinitionId, int>();
            var temp = new Dictionary<MyDefinitionId, int>();


            foreach (var x in list) {
                var slim = x as IMySlimBlock;
                if (slim == null) {
                    var fat = x as IMyCubeBlock;
                    if (fat != null) {
                        slim = fat.SlimBlock;
                    }
                }

                if (slim != null) {
                    slim.GetBlockLeftNeededComponents(leftNeeded, temp);
                }
            }

            var s = "";
            foreach (var x in leftNeeded) {
                s+= x.Key + ":" + x.Value + " ";
            }

            Common.SendChatMessage ("Need: " + s);

            return leftNeeded;
        }

        
        

        public static void CheckWelding(IMySlimBlock block, float radius) {
            neededComps.Clear();
            drains.Clear();
            
            if (block == null) { return; }

            block.GetMissingComponents (neededComps);
            if (neededComps.Count == 0) return;

            var sphere = new BoundingSphereD (MyAPIGateway.Session.Player.GetPosition(), radius);
            var cargos = new List<MyEntity>();

            InventoryUtils.GetAllCargosInRange (ref sphere, cargos, AutoTools.IgnoreTake, true,  (x) => {
                var inv = x.GetInventory(x.InventoryCount-1);
                for (int i = 0; i < inv.ItemCount; i++) {
                    var item = inv.GetItemAt(i);

                    if (item.HasValue) {
                        var itemV = item.Value;
                        if (itemV.Type.TypeId.EndsWith("Component") && neededComps.ContainsKey(itemV.Type.SubtypeId)) { return true; }
                    }
                }

                return false;
            }, (a,b)=>b.GetInventory().CurrentVolume >= a.GetInventory().CurrentVolume ? 1 : -1, 1);
            
            eatComponents (cargos, neededComps, drains); 

            if (drains.Count == 0) return;

            var data = new byte[(8+8+4+4+4)+(8+4+4) * drains.Count];
            var o = 0;

            o += Bytes.Pack(data, o, block.CubeGrid.EntityId);
            o += Bytes.Pack(data, o, MyAPIGateway.Session.Player.IdentityId);
            o += Bytes.Pack(data, o, block.Position.X);
            o += Bytes.Pack(data, o, block.Position.Y);
            o += Bytes.Pack(data, o, block.Position.Z);

            foreach (var x in drains) {
                o += Bytes.Pack(data, o, x.k);
                o += Bytes.Pack(data, o, x.v);
                o += Bytes.Pack(data, o, x.t);
            }

            MyAPIGateway.Multiplayer.SendMessageToServer (AutoTools.CODE_FILLSTOCKPILE, data);
        }

        public static void eatComponents (List<MyEntity> cargos, Dictionary<string, int> data, List<Tripple<long, uint, int>> drain) {
            foreach (var cargo in cargos) {

                for (var i=0; i<cargo.InventoryCount; i++) {
                    var inv = cargo.GetInventory (i);
                    var itms = inv.GetItems();

                    foreach (var y in itms) {
                        if (y.Content.TypeId != AutoTools.Component) continue;
                        var id = y.Content.SubtypeName;
                        if (!data.ContainsKey(id)) continue;

                        var amountNeeded = (long)data[id];
                        var was = amountNeeded;
                        amountNeeded -= ((long)y.Amount);
                        var took =  (int)(was - Math.Max(0, amountNeeded));

                        if (amountNeeded <= 0) {
                            data.Remove (id);
                        } else {
                            data[id] = (int)amountNeeded;
                        }

                        drain.Add (new Tripple<long, uint, int>(cargo.EntityId, y.ItemId, took));
                    }
                }

            }
        }
    }
}
