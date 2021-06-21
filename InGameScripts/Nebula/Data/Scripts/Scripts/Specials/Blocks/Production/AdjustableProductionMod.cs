using Digi;
using Sandbox.ModAPI;
using ServerMod;
using Slime;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace Scripts.Specials.Production
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class AdjustableProductionMod : MySessionComponentBase {
        public static HashSet<String> IDs = new HashSet<string> () { "LargeAssembler", "LargeAssemblerMK", "LargeAssemblerADV", "LargeAssemblerULT", "LargeRefinery", "LargeRefineryVanilla" };

        public static Guid Guid1 = new Guid("bf0916ef-73c0-4044-a0ac-00b41c6d5e9e");
		public static Guid Guid2 = new Guid("fe5fdfe2-6287-4a26-b026-c194a1cc6a68");
		public static Guid Guid3 = new Guid("8f4f6ff4-5406-4f02-ae73-161458198ac8");
		public static Guid Guid4 = new Guid("1fca62a2-5f0c-4fce-b485-fbfb9cb8161e");


        public static ushort MSGID_CHANGE_VALUES = 52345;
        public static ushort MSGID_CHANGE_VALUES_CLIENT = 52346;
        public static ushort MSGID_REQUEST_VALUES = 52347;
        public static ushort MSGID_RECIEVE_VALUES = 52348;

        public static long MODCONNECTION = 82733000;


        public static void RefreshHandler (object obj)
        {
            try
            {
                var refOrAss = obj as IMyProductionBlock;
                if (refOrAss != null)
                {
                    var adjustable = refOrAss.GetAs<Adjustable>();
                    if (adjustable != null)
                    {
                        adjustable.UpdatePoints();
                    }
                }
            } catch (Exception e)
            {
                MyLog.Default.Info($"[AdjustableProductionMod.RefreshHandler] : {e}");
            }
            
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent) {
            base.Init(sessionComponent);

            
             try {
                MyAPIGateway.Utilities.RegisterMessageHandler(82733000, RefreshHandler);
                if (MyAPIGateway.Session.IsServer) {
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_CHANGE_VALUES, SyncModDataRequestReceived);
                    if (!MyAPIGateway.Utilities.IsDedicated) MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_CHANGE_VALUES_CLIENT, SyncModDataRequestReceivedClient);
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_REQUEST_VALUES, GetValuesRequestReceived);
                } else {
                    //MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_CHANGE_VALUES, SyncModDataRequestReceived);
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_CHANGE_VALUES_CLIENT, SyncModDataRequestReceivedClient);
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_RECIEVE_VALUES, GetValuesReceivedClient);
                }
            } catch (Exception e) {
                Log.Error (e);
            }
        }

        protected override void UnloadData() {
            base.UnloadData();
            if (MyAPIGateway.Session.IsServer) {
                MyAPIGateway.Utilities.UnregisterMessageHandler(82733000, RefreshHandler);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_CHANGE_VALUES, SyncModDataRequestReceived);
                if (!MyAPIGateway.Utilities.IsDedicated) MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_CHANGE_VALUES_CLIENT, SyncModDataRequestReceivedClient);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_REQUEST_VALUES, GetValuesRequestReceived);
            } else {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_CHANGE_VALUES_CLIENT, SyncModDataRequestReceivedClient);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_RECIEVE_VALUES, GetValuesReceivedClient);
            }
        }

        public static void SyncModDataRequestReceived(byte[] obj) {
            Helper.DispatchMessage (obj, "ReceivedServer", (entityId, speed, power, yeild, pp)=>{
                pp.SetExactPoints (speed, power, yeild, true, true);
				var data = new byte[8 + 4 + 4 + 4];
				Bytes.Pack(data, 0, entityId);
				Bytes.Pack(data, 8, pp.pspeed);
				Bytes.Pack(data, 12, pp.ppower);
				Bytes.Pack(data, 16, pp.pyeild);
				MyAPIGateway.Multiplayer.SendMessageToOthers (MSGID_RECIEVE_VALUES, data, true);
            });
        }

        public static void SyncModDataRequestReceivedClient(byte[] obj) {
            Helper.DispatchMessage (obj, "ReceivedClient", (entityId, speed, power, yeild, pp)=>{ pp.SetExactPoints (speed, power, yeild, true, false); });
        }

        public static void GetValuesReceivedClient(byte[] obj) {
            Helper.DispatchMessage (obj, "ReceivedClientRawValues", (entityId, speed, power, yeild, pp)=>{ pp.SetExactPoints (speed, power, yeild, true, false); });
        }

        public static void GetValuesRequestReceived(byte[] obj) {
            try {
                var entityId = BitConverter.ToInt64(obj, 0);
                var playerId = BitConverter.ToUInt64(obj, 8);

                if (!AdjustableRef.allRefs.ContainsKey(entityId)) {
                    Log.Info ("AdjustableRefs->No Such ID on server:"+entityId);
                    return;
                }

                var pp = AdjustableRef.allRefs[entityId];
				var data = new byte[8+4+4+4];
				Bytes.Pack(data, 0, entityId);
				Bytes.Pack(data, 8, pp.pspeed);
				Bytes.Pack(data, 12, pp.ppower);
				Bytes.Pack(data, 16, pp.pyeild);
				MyAPIGateway.Multiplayer.SendMessageTo (MSGID_RECIEVE_VALUES, data, playerId,true);
            } catch (Exception e){
                Log.Error (e, "AdjustableRefs->Set on server");
            }
        }

        public static void SendValuesToServer (long entity, int speed, int power, int yeild) {
            try {
				var data = new byte[8 + 4 + 4 + 4];
				Bytes.Pack(data, 0, entity);
				Bytes.Pack(data, 8, speed);
				Bytes.Pack(data, 12, power);
				Bytes.Pack(data, 16, yeild);
				MyAPIGateway.Multiplayer.SendMessageToServer (MSGID_CHANGE_VALUES, data, true);
            } catch (Exception e) {
                Log.Error (e);
            }
        }

        public static void RequestValuesFromServer (long entity, ulong playerId) {
            try {
				var data = new byte[8 + 8];
				Bytes.Pack(data, 0, entity);
				Bytes.Pack(data, 8, playerId);
				MyAPIGateway.Multiplayer.SendMessageToServer (MSGID_REQUEST_VALUES, data, true);
            } catch (Exception e) {
                Log.Error (e);
            }
        }


        
    public static class Helper {
            public static void DispatchMessage(byte[] obj, String tag,  Action<long, int, int, int, Adjustable> todo) {
                try {
                    var entityId = BitConverter.ToInt64(obj, 0);
                    if (!Adjustable.allRefs.ContainsKey(entityId))
                    {
                        return;
                    }

                    var speed = BitConverter.ToInt32(obj, 8);
                    var power = BitConverter.ToInt32(obj, 12);
                    var yeild = BitConverter.ToInt32(obj, 16);
				
                    

                    var pp = Adjustable.allRefs[entityId];
                
                    todo.Invoke (entityId, speed, power, yeild, pp);
                } catch (Exception e){
                    Log.Error (e, "AdjustableRefs->"+tag);
                }
            }
        }
    }
}
