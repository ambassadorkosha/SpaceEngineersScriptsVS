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
using Scripts.Base;
using VRage.Game.ModAPI;
using Scripts.Specials.Messaging;
using VRage.Game;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Scripts.Shared;
using Slime;

namespace ServerMod.Specials {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MedicalRoom), true, new string[] { "CharacterTeleporter" })]
    public class CharacterTeleporter : MyGameLogicComponent  {

        public static ushort CODE = 12996;
        public static Dictionary<string, CharacterTeleporter> teleportPoints = new Dictionary<string, CharacterTeleporter>();
        public static void Init () {
            if (MyAPIGateway.Session.IsServer) {
                MyAPIGateway.Multiplayer.RegisterMessageHandler (CODE, OnTeleportRequest);
            }
            if (!MyAPIGateway.Session.isTorchServer()) {
                MyAPIGateway.Utilities.MessageEntered += MessageEntered;
            }
        }

        public static void SendTeleportCommand (string teleport) {
            try { 
                var data = Encoding.ASCII.GetBytes(teleport);
                var data2 = new byte[data.Length + 12];
                Bytes.Pack (data2, 0, MyAPIGateway.Session.Player.IdentityId);
                Bytes.Pack (data2, 8, data.Length);
                for (var x=0; x<data.Length; x++) {
                    data2[12+x] = data[x];
                }
                MyAPIGateway.Multiplayer.SendMessageToServer (CODE, data2);
            } catch (Exception e) {
                Log.Error (e);
            }
        }
        private static void OnTeleportRequest(byte[] obj) {
            try {
                var playerId = BitConverter.ToInt64 (obj, 0);
                var leng = BitConverter.ToInt32 (obj, 8);
                var name = Encoding.ASCII.GetString(obj, 12, leng);

                var pl = Other.GetPlayer (playerId);
                if (pl==null) {
                    return;
                }

                var character = pl.Character;

                if (character == null) {
                    Common.SendChatMessage ("You can't teleport. You are dead", "Operator", playerId);
                    return;
                }

                if (character.GetInventory ().GetItems().Count > 0) {
                    Common.SendChatMessage ("You can't teleport with items", "Operator", playerId);
                    return;
                }

                if (pl.Controller != null && pl.Controller.ControlledEntity != character) {
                    Common.SendChatMessage ("You can't teleport while piloting", "Operator", playerId);
                    return;
                }
              
                if (!teleportPoints.ContainsKey(name)) {
                    Common.SendChatMessage ("No teleport with name " + name, "Operator", playerId);
                    return;
                }

                var beacon = teleportPoints[name];
                var m  = beacon.block.WorldMatrix;
                var placetoSpawn = MyAPIGateway.Entities.FindFreePlace(m.Translation, 2f) ?? Vector3D.Zero;
                if (placetoSpawn == Vector3D.Zero) {
                    Common.SendChatMessage ("Cant find free space near teleporter" + name, "Operator", playerId);
                    return;
                }
                character.WorldMatrix = MatrixD.CreateWorld (placetoSpawn, m.Forward, m.Up);
            } catch (Exception e) {
                Log.Error (e);
            }
            
        }

        public static void Close() {
            if (MyAPIGateway.Session.IsServer) {
                MyAPIGateway.Multiplayer.RegisterMessageHandler (CODE, OnTeleportRequest);
            }
            if (!MyAPIGateway.Session.isTorchServer()) {
                MyAPIGateway.Utilities.MessageEntered -= MessageEntered;
            }
        }

        

        private static void MessageEntered(string messageText, ref bool sendToOthers) {
            if (messageText.StartsWith("/teleport ")) {
                sendToOthers = false;
                var where = messageText.Substring("/teleport ".Length);
                var character = MyAPIGateway.Session.Player.Character;
                if (character == null) {
                    Common.SendChatMessageToMe ("You can't teleport. You are dead", "Operator");
                    return;
                }

                if (character.GetInventory ().GetItems().Count > 0) {
                    Common.SendChatMessageToMe ("You can't teleport with items", "Operator");
                    return;
                }

                if (MyAPIGateway.Session.Player.Controller.ControlledEntity != character) {
                    Common.SendChatMessageToMe ("You can't teleport while piloting", "Operator");
                    return;
                }
                
                Common.SendChatMessageToMe ("You now teleporting", "Operator");
                SendTeleportCommand (where);
            }
        }

        string lastKey = null;
        IMyFunctionalBlock block;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init (objectBuilder);
            if (MyAPIGateway.Multiplayer.IsServer) {
                block = (Entity as IMyFunctionalBlock);
                block.CustomNameChanged += Changed;
                block.EnabledChanged += Changed;
                block.OnMarkForClose += Block_OnMarkForClose;
                Changed (block);
            }
        }

        private void Block_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
            block.CustomNameChanged -= Changed;
            block.EnabledChanged -= Changed;
            block.OnMarkForClose -= Block_OnMarkForClose;
        }

        private void Changed(IMyTerminalBlock obj) {
            if (lastKey != null) {
                 teleportPoints.Remove (lastKey);
            }

            var name = obj.CustomName;

            if (block.Enabled) {
                if (teleportPoints.ContainsKey(name)) {
                    teleportPoints.Remove (name);
                }
                lastKey = name;
                teleportPoints.Add (name, this);
            }
        }
    }

}
