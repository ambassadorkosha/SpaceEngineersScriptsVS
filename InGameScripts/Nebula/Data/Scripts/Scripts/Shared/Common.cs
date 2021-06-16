using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Digi;
using Slime;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials.Messaging {
    public static class Common {

       public static void SendChatMessage(string message, string author = "", long playerId = 0L, string font = "Blue") {
            MyVisualScriptLogicProvider.SendChatMessage (message, author, playerId, font);
        }

        public static void SendChatMessageToMe(string message, string author = "", string font = "Blue") {
            if (MyAPIGateway.Session.Player != null) {
                MyVisualScriptLogicProvider.SendChatMessage (message, author, MyAPIGateway.Session.Player.IdentityId, font);
            }
        }

        public static void ShowNotification(string message, int disappearTimeMs, string font = "White", long playerId = 0L) {
            MyVisualScriptLogicProvider.ShowNotification (message, disappearTimeMs, font, playerId);
        }

        public static void ShowNotificationForAllInRange(string message, int disappearTimeMs, Vector3D pos, float r, string font = "White") {
            var pl = GetOnlinePlayersInRange (pos, r);
            foreach (var x in pl) {
                MyVisualScriptLogicProvider.ShowNotification (message, disappearTimeMs, font, x.IdentityId);
            }
        }
        
        
        public static void ShowNotificationForMeInGrid(IMyCubeGrid grid, string message, int disappearTimeMs, string font = "White") {
            if (MyAPIGateway.Session.isTorchServer()) return;
            try {
                var cock = MyAPIGateway.Session.Player.Controller.ControlledEntity as IMyCockpit;
                if (cock == null) return;
                if (cock.CubeGrid != grid) {
                    return;
                }
                MyVisualScriptLogicProvider.ShowNotificationLocal(message, disappearTimeMs, font);
            } catch (Exception e) { }
        }
        
       public static void ShowNotificationForAllInGrid(IMyCubeGrid grid, string message, int disappearTimeMs, string font = "White") {
           var pl = GetOnlinePlayersInShip (grid);
           foreach (var x in pl) {
               MyVisualScriptLogicProvider.ShowNotification (message, disappearTimeMs, font, x.IdentityId);
           }
       }
       
       public static List<IMyPlayer> GetOnlinePlayersInShip (IMyCubeGrid grid) {
            var players = new List<IMyPlayer>();
            try {
                foreach (var x in grid.GetConnectedGrids(GridLinkTypeEnum.Physical)) {
                    var el = x.GetShip().Cockpits;
                    foreach (var y in el) {
                        var p = y.GetPlayer();
                        if (p != null) {
                            players.Add(p);
                        }
                    }
                }
            } catch (Exception e) { Log.Error(e); }
            return players;
       }

        public static List<IMyPlayer> GetOnlinePlayersInRange (Vector3D pos, float r) {
            List<IMyPlayer> players = new List<IMyPlayer>();
            r = r*r;
            MyAPIGateway.Multiplayer.Players.GetPlayers(players, (x)=>{ 
                var ch = x.Character;
                if (ch != null) {
                    return (ch.WorldMatrix.Translation - pos).LengthSquared() < r;
                }
                return false;
            });
            return players;
        }

        


        
        public static String getPlayerName (long id) {
            var p = getPlayer (id);
            return p ==null ? "UnknownP" : p.DisplayName;
        }

        public static IMyPlayer getPlayer (long id) {
            var ind = new List<IMyPlayer>();
           
            MyAPIGateway.Players.GetPlayers (ind,  (x) => { return x.IdentityId == id; });
            return ind.FirstOrDefault(null) as IMyPlayer;
        }
        //public static bool isBot (long id) {
        //    var ind = new List<IMyIdentity>();
        //    MyAPIGateway.Players.GetAllIdentites (ind,  (x) => { return x.IdentityId == id; });
        //    
        //    if (ind.Count == 1) {
        //        ind[0].
        //    }
        //}

        //public static void ShowNotificationToAll(string message, int disappearTimeMs, string font = "White") {
        //    MyVisualScriptLogicProvider.ShowNotificationToAll (message, disappearTimeMs, font);
        //}
        //
        //public static void ShowSystemMessage(string from, string text, long player) {
        //    //MyAPIGateway.Utilities.ShowMessage("System", "Killed by : [" +killer.DisplayName + "] Sent to him: [" + (-took)+"] credits");
        //
        //}
    }
}
