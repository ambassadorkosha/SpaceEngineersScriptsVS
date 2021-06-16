using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials.Messaging;
using Slime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Scripts.Specials.Systems
{

    public class HGPacket { 
        public const int REQUEST_JOIN = 1;
        public const int REQUEST_SYNC = 2;

        public int type = 0;
        public byte[] data;
    }

    


    public static partial class HungerGames
    {
        const ushort PORT = 55102;

        private static Connection<HGPacket> Connection;
        private static Dictionary<int, Action<byte[], ulong, bool>> Handlers = new Dictionary<int, Action<byte[], ulong, bool>>();
        private static void InitNetwork ()
        {
            Connection = new Connection<HGPacket>(55102, HandleMessage);
        }


        private static void HandleMessage(HGPacket packet, ulong steamID, bool isFromServer)
        {
            if (Handlers.ContainsKey (packet.type))
            {
                Handlers[packet.type].Invoke(packet.data, steamID, isFromServer);
            } else
            {
                Common.SendChatMessage ($"NetworkHandler not found {packet.type}", "HungerGames:"+ (MyAPIGateway.Session.IsServer ? "Server" : "Client"));
            }
        }

        private static void HandleSync (byte[] data, ulong steamID, bool isFromServer)
        {
            var Identity = steamID.Identity();
            if (Identity == null) return;

            var p = Identity.GetPlayer();
            if (p.PromoteLevel < MyPromoteLevel.Scripter)
            {
                Common.SendChatMessage("You don't have rights to use this command", "HungerGames");
                return;
            }

            var settings = MyAPIGateway.Utilities.SerializeFromBinary<HungerGameSettings>(data);
        }
    }
}
