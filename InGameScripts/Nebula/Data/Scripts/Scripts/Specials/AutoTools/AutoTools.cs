using Digi;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using Scripts.Base;
using Scripts.Shared;
using Scripts.Specials.Messaging;
using ServerMod;
using Slime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Specials.AutoTools {


    class AutoToolsServer {
        public static MyInventory TempInventory = new MyInventory (float.MaxValue, new Vector3(1000d, 10000d, 10000d), MyInventoryFlags.CanSend);
       
        public static void Init () {
            if (MyAPIGateway.Multiplayer.IsServer) {
                MyAPIGateway.Multiplayer.RegisterMessageHandler (AutoTools.CODE_DRILL, DeleteFloatings);
                MyAPIGateway.Multiplayer.RegisterMessageHandler (AutoTools.CODE_FILLSTOCKPILE, FillStockpile);
                MyAPIGateway.Multiplayer.RegisterMessageHandler (AutoTools.CODE_COLLECT_FLOATINGS, CollectFloatings);
                MyAPIGateway.Multiplayer.RegisterMessageHandler (AutoTools.CODE_MOVE_ITEMS, MoveItemsServer);
                MyAPIGateway.Multiplayer.RegisterMessageHandler (AutoTools.CODE_FREESTOCKPILE, PullItemsFromStockpile);
            }
        }
        public static void Unload () {
            if (MyAPIGateway.Multiplayer.IsServer) { 
                MyAPIGateway.Multiplayer.UnregisterMessageHandler (AutoTools.CODE_DRILL, DeleteFloatings);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler (AutoTools.CODE_FILLSTOCKPILE, FillStockpile);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler (AutoTools.CODE_COLLECT_FLOATINGS, CollectFloatings);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler (AutoTools.CODE_MOVE_ITEMS, MoveItemsServer);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler (AutoTools.CODE_FREESTOCKPILE, PullItemsFromStockpile);
            }
        }

        protected static void PullItemsFromStockpile (byte[] data) {
            try {
                //var data = new byte[8+8+1+1+4+8];
                var playerId = BitConverter.ToInt64(data, 0);
                var gridId = BitConverter.ToInt64(data, 8);
                var whereId = BitConverter.ToInt64(data, 28);

                var player = Other.GetPlayer(playerId);
                if (player == null) return;

                if (player.Character == null) return;
                var grid = MyEntities.GetEntityById (gridId) as IMyCubeGrid;
                if (grid == null) return;
                var where = MyEntities.GetEntityById (whereId) as IMyTerminalBlock; 
                if (where == null) return;

                if (!where.HasInventory) return;
                if (!where.IsFunctional) return;
                if (!where.HasPlayerAccess (playerId)) return;
                if ((where.WorldMatrix.Translation - player.Character.WorldMatrix.Translation).Length() > 400) return;

                var x = BitConverter.ToInt32(data, 16);
                var y = BitConverter.ToInt32(data, 20);
                var z = BitConverter.ToInt32(data, 24);

                var block = grid.GetCubeBlock (new Vector3I (x,y,z));

                if ((block.GetWorldPosition() - player.Character.WorldMatrix.Translation).Length() > 15) return;
                block.MoveItemsFromConstructionStockpile (where.GetInventory());
            } catch (Exception e){
                Log.Error (e);
            }
        }

        protected static void MoveItemsServer (byte[] data) {
            try {
                //var data = new byte[8+8+1+1+4+8];
                var fromId = BitConverter.ToInt64(data, 0);
                var toId = BitConverter.ToInt64(data, 8);
                var fromInv = data[16];
                var toInv = data[17];
            
                var itemId = BitConverter.ToUInt32(data, 18);
                var amount = BitConverter.ToInt64(data, 22);
          
                var from = MyEntities.GetEntityById (fromId);
                if (from == null) {  Log.ChatError("from Null:" + fromId); return; }
                var to = MyEntities.GetEntityById (toId);
                if (to == null)  {  Log.ChatError("to Null:" + toId); return; }

                var fromI = from.GetInventory(fromInv);
                var toI = to.GetInventory(toInv);

                if (fromI == null)  {  Log.ChatError("fromI Null"); return; }
                if (toI == null)  {  Log.ChatError("toI Null"); return; }

                var am2 = new MyFixedPoint {RawValue = amount};

                var am = am2 <= MyFixedPoint.Zero ? (MyFixedPoint?)null : am2;
                 
                var index = fromI.GetItemIndexByID (itemId);
                if (index >= 0) {
                    var rest = ((IMyInventory) fromI).TransferItemTo (toI, index, checkConnection:false, amount:am );
                }
            } catch (Exception e){
                Log.Error ("Error in MoveItemsServer:" + data[16] + " " + data[17]);
                Log.Error ("Error in MoveItemsServer:" + e.Message + " " + e.StackTrace);
            }
        }

        public static void DeleteFloatings (byte[] bytes) {
            var ids = MyAPIGateway.Utilities.SerializeFromBinary<List<long>>(bytes);
            foreach (var x in ids) {
                IMyEntity ent;
                if(MyAPIGateway.Entities.TryGetEntityById(x, out ent)) {
                    var floaing = ent as MyFloatingObject;
                    if (floaing != null && floaing.Item.Content.TypeId == AutoTools.Ore) {
                        floaing.Close();
                    }
                }
            }
        }

        public static void CollectFloatings (byte[] bytes) {
            var player = BitConverter.ToInt64(bytes, 0);
            var takeEntity = BitConverter.ToInt64(bytes, 8);
            IMyEntity takeEnt;
            IMyEntity floatEnt;

            if (!MyAPIGateway.Entities.TryGetEntityById(takeEntity, out takeEnt)) { return; }


            var maxD = AutoTools.MAX_PICKUP_DISTANCE * AutoTools.MAX_PICKUP_DISTANCE;
            for (var x=16; x<bytes.Length-7; x+=8){
                var entId = BitConverter.ToInt64(bytes,x);
                if(MyAPIGateway.Entities.TryGetEntityById(entId, out floatEnt)) {
                    var floating = floatEnt as MyFloatingObject;
                    if (floating != null && (floating.WorldMatrix.Translation - takeEnt.WorldMatrix.Translation).LengthSquared() < maxD) { (takeEnt.GetInventory() as MyInventory).TakeFloatingObject(floating); }
                }
            }
        }

        public static void FillStockpile (byte[] bytes) {
            try {
                var g = BitConverter.ToInt64(bytes,0);
                var player = BitConverter.ToInt64(bytes,8);
                var pX = BitConverter.ToInt32(bytes,16);
                var pY = BitConverter.ToInt32(bytes,20);
                var pZ = BitConverter.ToInt32(bytes,24);

                var ship = GameBase.instance.gridToShip.GetOr(g, null);
                var grid = ship.grid;
                IMySlimBlock block = null;

                if (grid != null) {
                    block = grid.GetCubeBlock (new Vector3I (pX, pY, pZ));
                    if (block == null) { return; }
                }

                for (var x=28; x+15<bytes.Length; x+= 16) {
                    try {
                        var entId = BitConverter.ToInt64(bytes, x);
                        var itemId = BitConverter.ToUInt32(bytes, x+8);
                        var amount = BitConverter.ToInt32(bytes, x+12);

                        IMyEntity ent;
                        MyAPIGateway.Entities.TryGetEntityById (entId, out ent);
                        if (ent != null) {
                            for (var i=0; i<ent.InventoryCount; i++) {
                                var inv = ent.GetInventory(i);
                                var index = inv.GetItemIndexByID (itemId);
                                if (index >= 0) {
                                    inv.TransferItemTo (TempInventory, index, amount: amount, checkConnection:false);
                                    break;
                                }
                            }
                        } 
                    } catch (Exception e) {
                        //Common.ShowNotification ("E1", 2000);
                        Log.Error (e, "Couldn't move stockpile 1");
                    }
                }
                //Common.ShowNotification ("Took needed components", 2000, playerId: player);
                block.MoveItemsToConstructionStockpile (TempInventory);

                if (TempInventory.ItemCount > 0) {
                    MoveBack (bytes);
                }
                TempInventory.ClearItems();
            } catch (Exception ee) {
                Log.Error (ee);
                //Common.ShowNotification ("E2" + ee.Message +" "+ ee.StackTrace, 2000);
                //Log.Error (ee, "Couldn't move stockpile 2");
            }
        }

        private static void MoveBack (byte[] bytes) {
            for (var x=28; x+15<bytes.Length; x+= 16) {
                try {
                    var entId = BitConverter.ToInt64(bytes, x);
                    IMyEntity ent;
                    MyAPIGateway.Entities.TryGetEntityById (entId, out ent);
                    if (ent == null) continue;
                    var cargo = ent as IMyCargoContainer;
                    if (cargo == null) continue;
                    var inv = ent.GetInventory();
                    inv.MoveAllItemsFrom(TempInventory);
                    if (TempInventory.ItemCount == 0) return;
                } catch (Exception e) {
                    Log.Error (e);
                }
            }
         }

    }

    class AutoTools {
        public static Dictionary<string, float> containerRadiuses = new Dictionary<string, float>() {
            { "Welder", 18f },{ "Welder2", 25f },{ "Welder3", 32f },{ "Welder4", 40f },
            { "AngleGrinder", 18f  },{ "AngleGrinder2", 25f },{ "AngleGrinder3",  32f },{ "AngleGrinder4", 40f },
            { "HandDrill", 28f  },{ "HandDrill2", 36f },{ "HandDrill3",  44f }//,{ "HandDrill4", 32f }
        };

        public static ushort CODE_DRILL = 34524;
        public static ushort CODE_FILLSTOCKPILE = 34525;
        public static ushort CODE_COLLECT_FLOATINGS = 34526;
        public static ushort CODE_MOVE_ITEMS = 34527;
        public static ushort CODE_BIND_INVENTORY = 34528;
        public static ushort CODE_UNBIND_INVENTORY = 34529;
        public static ushort CODE_FREESTOCKPILE = 34530;

        public static double MAX_PICKUP_DISTANCE = 150; // 44f + 6f - distance from player
        public static double MAX_BIND_DISTANCE = 42;

        public static MyObjectBuilderType Component = MyObjectBuilderType.Parse ("MyObjectBuilder_Component");
        public static MyObjectBuilderType Ore = MyObjectBuilderType.Parse ("MyObjectBuilder_Ore");
        public static MyObjectBuilderType Ingot = MyObjectBuilderType.Parse ("MyObjectBuilder_Ingot");

        public static String sComponent = "MyObjectBuilder_Component";
        public static String sOre = "MyObjectBuilder_Ore";
        public static String sIngot = "MyObjectBuilder_Ingot";

        public static String IgnoreTake = "[AUTOTAKE:IGNORE]";
        public static String IgnorePut = "[AUTOPUT:IGNORE]";

        public const String STEEL_PLATE = "SteelPlate";
        public const String INTERRIOR_PLATE = "InteriorPlate";

        public const int STEEL_PLATE_KEEP = 50;
        public const int INTERRIOR_PLATE_KEEP = 20;

        public static MyDefinitionId STEEL_PLATE_ID = MyDefinitionId.Parse ("MyObjectBuilder_Component/SteelPlate");
        public static MyDefinitionId INTERRIOR_PLATE_ID = MyDefinitionId.Parse ("MyObjectBuilder_Component/InteriorPlate");


        public static void Init () {
            AutoToolsServer.Init();
        }
        public static void Unload () {
            AutoToolsServer.Unload();
        }

        public static void TeleportItemsRequest (MyInventory from, MyInventory to, uint itemId, MyFixedPoint? amount = null) {
            var data = new byte[8+8+1+1+4+8];
            var o = 0;
            o += Bytes.Pack(data, o, from.Owner.EntityId);
            o += Bytes.Pack(data, o, to.Owner.EntityId);
            o += Bytes.Pack(data, o, from.InventoryIdx);
            o += Bytes.Pack(data, o, to.InventoryIdx);
            o += Bytes.Pack(data, o, itemId);
            o += Bytes.Pack(data, o, amount == null ? MyFixedPoint.MinValue.RawValue : amount.Value.RawValue);
            MyAPIGateway.Multiplayer.SendMessageToServer (CODE_MOVE_ITEMS, data);
        }

        public static void CollectOrDeleteItemsNear(float radius, float cargoRadius, bool delete, Func<MyFloatingObject, bool> filterCollect, Func<MyFloatingObject, bool> filterRemove) {
            List<MyEntity> ores = new List<MyEntity>();
            List<MyEntity> cargos = new List<MyEntity>();
            List<MyEntity> data = new List<MyEntity>();

            var pos = MyAPIGateway.Session.Player.GetPosition();
            var sphere = new BoundingSphereD (pos, radius);
            var sphere2 = new BoundingSphereD (pos, cargoRadius);
            var invOwn = MyAPIGateway.Session.Player.Character.GetInventory() as MyInventory;
            

            try {
                var deleteList = new List<long>();

                sphere.Radius = radius;
                data = MyEntities.GetTopMostEntitiesInSphere(ref sphere);

                
                ores.AddList (data);
                ores.RemoveAll((x) => !(x is IMyFloatingObject));
                data.Clear();
                
                
                if (ores.Count == 0) return;

                InventoryUtils.GetAllCargosInRange (ref sphere2, cargos, AutoTools.IgnorePut, false, (x) => x.GetInventory(0).GetLeftVolumeInLiters() > 200 && (x.WorldMatrix.Translation - pos).Length() < AutoTools.MAX_PICKUP_DISTANCE - 5f, (a,b)=>b.GetInventory().CurrentVolume  >= a.GetInventory().CurrentVolume ? 1 : -1, 1);

                var oreList = new List<Pair<long, long>>();
                foreach (var ore in ores) {
                    var ore2 = ore as MyFloatingObject;
                    if (delete) {
                        if (filterRemove(ore2)) {
                            deleteList.Add (ore.EntityId);
                        }
                    } else {
                        if (filterCollect(ore2)) { //
                            if (cargos.Count > 0 && cargos[0].GetInventory(0).GetLeftVolumeInLiters () > 20) {
                                oreList.Add (new Pair<long, long>(ore2.EntityId, cargos[0].EntityId));
                            } else {
                                if (invOwn.GetLeftVolumeInLiters () > 20) {
                                    invOwn.AddEntity(ore);
                                }
                            }
                        }
                    }
                }

                

                if (deleteList.Count > 0) {
                    MyAPIGateway.Multiplayer.SendMessageToServer (AutoTools.CODE_DRILL, MyAPIGateway.Utilities.SerializeToBinary(deleteList));//deleteList.toBytes());
                }

                if (oreList.Count > 0) {
                    var bytes = new byte[16+oreList.Count*8]; 
                    var o = 0;
                    o += Bytes.Pack (bytes, o, MyAPIGateway.Session.Player.IdentityId);
                    o += Bytes.Pack (bytes, o, cargos[0].EntityId);

                        
                    foreach (var x in oreList) {
                        o += Bytes.Pack (bytes, o, x.k);
                    }
                    MyAPIGateway.Multiplayer.SendMessageToServer (AutoTools.CODE_COLLECT_FLOATINGS, bytes);//deleteList.toBytes());
                }
            }
            catch(Exception ex) { 
                Log.Error(ex);    
            }
        }

        public static bool hasItemsToPullOut(IMyInventory inv) {
            var transfer = inv.CountItems(new Dictionary<MyDefinitionId, MyFixedPoint>());
            foreach (var x in transfer) {
                if (x.Key.TypeId == Ore || x.Key.TypeId == Ingot) return true;
                if (x.Key.TypeId == Component) {
                    if (x.Key.SubtypeId == STEEL_PLATE_ID.SubtypeId && x.Value > STEEL_PLATE_KEEP) { return true; } 
                    if (x.Key.SubtypeId == INTERRIOR_PLATE_ID.SubtypeId && x.Value > INTERRIOR_PLATE_KEEP) { return true; } 
                    return true;
                }
            }

            return false;
        }
        
        public static void TryPullOut (float radius) {
            var cha = MyAPIGateway.Session.Player.Character;
            var inv = cha.GetInventory ();

            if (!hasItemsToPullOut(inv)) {
                return;
            }
            
            var sphere = new BoundingSphereD (MyAPIGateway.Session.Player.GetPosition(), radius);
            var cargos = new List<MyEntity>();

            InventoryUtils.GetAllCargosInRange (ref sphere, cargos, AutoTools.IgnorePut, true, (x)=>x.GetInventory(0).GetLeftVolumeInLiters() > 200, sort : (a,b)=>b.GetInventory().CurrentVolume  >= a.GetInventory().CurrentVolume ? 1 : -1, maxTake: 1);
            
            if (cargos.Count == 0) return;
            

            foreach (var cargo in cargos) {
                cargo.GetInventory().TeleportAllItemsFromRequest (inv, (x)=>{ 
                    if (x.Type.TypeId == AutoTools.sOre || x.Type.TypeId == AutoTools.sIngot) {
                        return x.Amount;
                    } else if (x.Type.TypeId == AutoTools.sComponent) {
                        if (x.Type.SubtypeId == STEEL_PLATE) return x.Amount - STEEL_PLATE_KEEP;
                        if (x.Type.SubtypeId == INTERRIOR_PLATE) return x.Amount - INTERRIOR_PLATE_KEEP;
                        return x.Amount;
                    }
                    return -1;
                });
            }
        }

        public static void PullOutContructionsRequest (long grid, Vector3I position, long targetEntity) {
            var user = MyAPIGateway.Session.Player.IdentityId;
            var data = new byte[8 + 8 + 12 + 8];
            var pos = Bytes.Pack (data, 0, user);
            pos += Bytes.Pack (data, pos, grid);
            pos += Bytes.Pack (data, pos, position.X);
            pos += Bytes.Pack (data, pos, position.Y);
            pos += Bytes.Pack (data, pos, position.Z);
            pos += Bytes.Pack (data, pos, targetEntity);

            MyAPIGateway.Multiplayer.SendMessageToServer (AutoTools.CODE_FREESTOCKPILE, data);
        }


        public static void TryPullInContructions (float radius) {
            var cha = MyAPIGateway.Session.Player.Character;
            var chainv = cha.GetInventory();
            if (chainv.GetLeftVolumeInLiters() < 30) {
                return;
            }

            var leftSteelPlates = STEEL_PLATE_KEEP; 
            var leftInterPlates = INTERRIOR_PLATE_KEEP; 

            var data = chainv.CountItems();
            leftSteelPlates -= (int)data.GetOr (STEEL_PLATE_ID, 0);
            leftInterPlates -= (int)data.GetOr (INTERRIOR_PLATE_ID, 0);

            if (leftSteelPlates <= 0 && leftInterPlates <= 0) {
                return;
            }

            var sphere = new BoundingSphereD (MyAPIGateway.Session.Player.GetPosition(), radius);
            var cargos = new List<MyEntity>();
            
            var neededComps = new Dictionary<String, int>(); 
            neededComps.Add(STEEL_PLATE, leftSteelPlates);
            neededComps.Add(INTERRIOR_PLATE, leftInterPlates);
            
            
            InventoryUtils.GetAllCargosInRange (ref sphere, cargos, AutoTools.IgnoreTake, true, (x) => {
                var inv = x.GetInventory();
                for (int i = 0; i < inv.ItemCount; i++) {
                    var item = inv.GetItemAt(i);

                    if (item.HasValue) {
                        var itemV = item.Value;
                        if (itemV.Type.TypeId.EndsWith("Component") && neededComps.ContainsKey(itemV.Type.SubtypeId)) { return true; }
                    }
                }
                return false;
            }, (a,b)=>b.GetInventory().CurrentVolume >= a.GetInventory().CurrentVolume ? 1 : -1, maxTake:1);
            if (cargos.Count == 0) return;

            cargos.Sort ();

            foreach (var cargo in cargos) {
                chainv.TeleportAllItemsFromRequest (cargo.GetInventory(), (x)=> { 
                    if (x.Type.TypeId == AutoTools.sComponent) {
                        if (x.Type.SubtypeId == STEEL_PLATE) {
                            var can = Math.Min(leftSteelPlates, (int)((double)x.Amount));
                            leftSteelPlates -= can;
                            return can;
                        }
                        if (x.Type.SubtypeId == INTERRIOR_PLATE) {
                            var can = Math.Min(leftInterPlates, (int)((double)x.Amount));
                            leftInterPlates -= can;
                            return can;
                        }
                    }
                    return -1;
                });

                if (leftSteelPlates <= 0 && leftInterPlates <= 0) {
                    break;
                }
            }
        }
    }
}


/*
 * SPLASH WELDING DEAD
 *
 *
 *  //public static Dictionary<long, long> playerToCargo = new Dictionary<long, long>();

     //public static void RequestBind (IMyCubeBlock container) {
        //    MyAPIGateway.Multiplayer.SendMessageToServer (CODE_BIND_INVENTORY, Bytes.PackPair (MyAPIGateway.Session.Player.IdentityId, container.EntityId));
        //}
        //
        //public static void RequestUnbind () {
        //    MyAPIGateway.Multiplayer.SendMessageToServer (CODE_UNBIND_INVENTORY, BitConverter.GetBytes (MyAPIGateway.Session.Player.IdentityId));
        //}

    //if (mouse.Contains (MyMouseButtonsEnum.Middle)) {
    //    SplashWeldingClient ();
    //}


    static float SPLASH_RADIUS = 3f;

        public void SplashWeldingServer () {
            var tool =(Container.Entity as IMyHandheldGunObject<Sandbox.Game.Weapons.MyToolBase>);
            if (!tool.IsShooting) return;

            var caster = (tool as IMyEngineerToolBase).Components.Get<MyCasterComponent>();
            var block = (caster.HitBlock as IMySlimBlock);
            if (block == null) return;

            var owner = tool.GetCharacter();
            if (owner == null) return;
            var player = Other.GetPlayerByCharacter (owner.EntityId);
            if (player == 0) return;
            var cargo = AutoToolsServer.playerToCargo.GetValueOrDefault(player, 0);
            if (cargo == 0) return;

            var cargoEnt = MyEntities.GetEntityByIdOrDefault(cargo, null) as IMyTerminalBlock;
            if  (!cargoEnt.HasInventory) return;
            if  (!cargoEnt.HasPlayerAccess (player)) return;
            if  ((cargoEnt.WorldMatrix.Translation - owner.WorldMatrix.Translation).Length() > ContainerRange ()*1.2f) return;

            Vector3D position = block.GetWorldPosition ();
            float radius = 3f;
            float weldSpeed = 1f;

            var sphere = new BoundingSphereD (position, radius);
            var list = MyEntities.GetEntitiesInSphere (ref sphere);
           
            foreach (var x in list) {
                var slim = x as IMySlimBlock;
                if (slim == null) {
                    var fat = x as IMyCubeBlock;
                    if (fat != null) {
                        slim = fat.SlimBlock;
                    }
                }

                if (slim != null) {
                     slim.MoveItemsToConstructionStockpile (cargoEnt.GetInventory());
                     slim.IncreaseMountLevel (weldSpeed, slim.BuiltBy);
                }
            }
            list.Clear();
        }

        public void SplashWeldingClient () {
            var tool =(Container.Entity as IMyHandheldGunObject<Sandbox.Game.Weapons.MyToolBase>);
            if (!tool.IsShooting) return;

            var caster = (tool as IMyEngineerToolBase).Components.Get<MyCasterComponent>();
            var block = (caster.HitBlock as IMySlimBlock);

            var getNeeded = calculateNeededComponents (block.GetWorldPosition(), SPLASH_RADIUS);

            var sphere = new BoundingSphereD (MyAPIGateway.Session.Player.GetPosition(), ContainerRange());
            var cargos = new List<MyEntity>();
            InventoryUtils.GetAllCargosInRange (ref sphere, cargos, AutoTools.IgnoreTake, -1, true);
            foreach (var x in cargos) {
                AutoTools.RequestBind(x as IMyCubeBlock);
                break;
            }
        }
 * 
 * 
 * 
 */
