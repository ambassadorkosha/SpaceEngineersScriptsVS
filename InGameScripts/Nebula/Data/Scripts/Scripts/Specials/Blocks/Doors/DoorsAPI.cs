using Digi;
using Sandbox.ModAPI;
using Scripts.Shared;
using ServerMod;
using Slime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Specials.Doors {
    class DoorsAPI {
        const int CODE = 23314;
        public static void Init () {
            if (MyAPIGateway.Multiplayer.IsServer) {
                MyAPIGateway.Multiplayer.RegisterMessageHandler (CODE, HandleOpenRequest);
            }
        }
        public static void Unload () {
            if (MyAPIGateway.Multiplayer.IsServer) { 
                MyAPIGateway.Multiplayer.UnregisterMessageHandler (CODE, HandleOpenRequest);
            }
        }

        public static void SendRequest (IMyDoor door, bool open) {
            var bytes = new byte[17];
            var pl = MyAPIGateway.Session.LocalHumanPlayer;

            Bytes.Pack (bytes, 0, pl.IdentityId);
            Bytes.Pack (bytes, 8, door.EntityId);
            bytes[16] = (byte)(open ? 1 : 0);

            MyAPIGateway.Multiplayer.SendMessageToServer (CODE, bytes);
        }

        public static void HandleOpenRequest (byte[] bytes) {
            try {
                var pl = BitConverter.ToInt64 (bytes, 0);
                var dr = BitConverter.ToInt64 (bytes, 8);
                var open = bytes[16] == 1;

                var player = Other.GetPlayer (pl);
                var door = MyAPIGateway.Entities.GetEntityById (dr) as IMyDoor;

                if (door == null || player == null) {
                    Log.Error ("Door or player == null" + door + " " + player);
                    return;
                }

                var lockedDoor = door.GetAs<LockedDoorsBase>();
                if (lockedDoor != null) {
                    if (LockedDoorsBase.CanOpenDoor (door, player, 6f)) {
                        door.OpenDoor();
                    }
                    return;
                }

                var liftDoor = door.GetAs<LiftDoorsBase>();
                if (liftDoor != null) {
                    var state = LiftDoorsBase.CanOpenDoor (door); //open
                    if (state) {
                        door.OpenDoor();
                    } else {
                        door.CloseDoor();
                    }
                    return;
                }


            } catch (Exception e){
                Log.Error (e);
            }
        }
    }
}
