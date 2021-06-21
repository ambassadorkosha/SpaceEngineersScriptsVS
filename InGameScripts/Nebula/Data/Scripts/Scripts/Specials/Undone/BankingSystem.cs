using Sandbox.ModAPI;
using Scripts;
using Scripts.Shared;
using Scripts.Specials.Faction;
using Scripts.Specials.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI;

namespace ServerMod
{
    public class BankingSystem : Action2<GameBase, long>, LastLoginTracker.Listener {
        public static double TAKE_PLAYER = -0.001d;
        public static double TAKE_PLAYER_OFFLINE = -0.001d;

        public static HashSet<String> excludedFactions = new HashSet<string>() { "SCAV", "SPRT" };
        public static String PLAYER_PAY_TEXT = "You recieve bonus ({0:n0} SC) for staying online.";
        public static String FROM = "Banking system";
        
        public static int INTERVAL_SECONDS = 60*30; //30 min
        public static AutoTimer timer = new AutoTimer(60*INTERVAL_SECONDS);

        public static void Init () {
           if (MyAPIGateway.Multiplayer.IsServer) {
                //GameBase.instance.addFrameLogic(new BankingSystem());
            }
        }

        public BankingSystem () {
             LastLoginTracker.addListener (this);
        }

        public void run(GameBase t, long k) {
            if (timer.tick()) {
                rewardOnlineLogic (100);
            }
        }

        public void rewardOnlineLogic (long amount) {
            var pl = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers (pl, null);
            foreach (IMyPlayer x in pl) {
                var took = x.changeMoney (0, amount);
                Common.SendChatMessage (String.Format(PLAYER_PAY_TEXT, amount), FROM, playerId:x.IdentityId, font:"Green");
            }
        }


        // ----------- OLD LOGIC ----------

        public void onConnected(IMyPlayer p, long timePassed) { 
            //oldOfflineLogc (p, timePassed);
        }

        public void onNewPlayer(IMyPlayer p) { }

        

       
        public void oldOfflineLogc (IMyPlayer p, long timePassed) {
            var takeTaxTimes = timePassed / INTERVAL_SECONDS;
            long balance;
            if (p.TryGetBalanceInfo(out balance)) {
                 var tax = Math.Pow (1+TAKE_PLAYER_OFFLINE, takeTaxTimes);
                 var took = p.changeMoney (0, -((long)((1-tax)*balance)));
                Common.SendChatMessage (String.Format(PLAYER_PAY_TEXT, -took), FROM, playerId:p.IdentityId, font:"Green");
            }        
        }

        public void oldLogic () {
            long balance;
            var onlineFactionPlayers = new Dictionary<IMyPlayer, int>();
            var sb = new StringBuilder();
            var pl = new List<IMyPlayer>();
                
            MyAPIGateway.Players.GetPlayers (pl, null);
            foreach (IMyPlayer x in pl) {
                var took = x.changeMoney (TAKE_PLAYER);
                Common.SendChatMessage (String.Format(PLAYER_PAY_TEXT, -took), FROM, playerId:x.IdentityId, font:"Green");
            }
               
            var destroy = new List<IMyFaction>();
            foreach (IMyFaction x in MyAPIGateway.Session.Factions.Factions.Values) {
                if (excludedFactions.Contains (x.Tag)) continue;

                x.TryGetBalanceInfo(out balance);
                var info = Factions.getInfo(x.FactionId);
                    
                x.getOnlinePlayers (onlineFactionPlayers, pl);

                var takeMoney = info.calculatePrice (balance, onlineFactionPlayers.Count > 0 ? sb : null);

                if (onlineFactionPlayers.Count > 0) {
                    var mess = sb.ToString();
                    foreach (var y in onlineFactionPlayers.Keys) {
                        Common.SendChatMessage (mess, FROM, playerId:y.IdentityId, font:"Green");
                    }
                }

                sb.Clear();
                onlineFactionPlayers.Clear();

                x.changeMoney (0, -takeMoney);
                if (x.TryGetBalanceInfo(out balance)) {
                    if (balance <= 0) destroy.Add (x);
                }
            }

            foreach (var x in destroy) {
                MyAPIGateway.Session.Factions.RemoveFaction (x.FactionId);
                Common.SendChatMessage ("Faction " + x.Name + "["+x.Tag+"] has been disformed, as it couldn't pay pax", FROM);
            }
        }
    }
}
