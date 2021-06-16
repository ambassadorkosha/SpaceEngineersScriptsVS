using Digi;
using Sandbox.ModAPI;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Text;
using Scripts.Shared;
using VRage.Game.ModAPI;

namespace Scripts {
    class LastLoginTracker : Action1<long> {
        private static LastLoginTracker instance = null;

        private static int ONLINE_CHECK_INTERVAL = 10; 
        private static int LOGIN_TIME_OUT = 60; //TIME IN SEC

        private Dictionary<long, long> players = new Dictionary<long, long>();
        private List<Listener> listeners = new List<Listener>();
        private AutoTimer timer = new AutoTimer (60*ONLINE_CHECK_INTERVAL); //

        public static void Init () { 
            if (MyAPIGateway.Multiplayer.IsServer) {
                instance = new LastLoginTracker (); 
                FrameExecutor.addFrameLogic (instance);
                try {
                    Load ();
                } catch (Exception e) {
                    Log.Error (e, "LastLoginTracker:Load");
                }
            }
        }

        public static void addListener (Listener l) { instance.listeners.Add(l); }
        
        public static void Load () {
            try {
                var ll = Other.LoadWorldFile<String>("last_onlines");
                if (ll != null) {
                    var lines = ll.Split('\n');
                    foreach (var l in lines) {
                        var data = l.Split(',');
                        if (data.Length == 2) {
                            var d1 = long.Parse(data[0]);
                            var d2 = long.Parse(data[1]);
                            instance.players.Add (d1,d2);
                        }
                    }
                }
            } catch (Exception e) {
                Log.Error (e);
            }
        }

        public static void Save () {
            try {
                StringBuilder sb = new StringBuilder();
                foreach (var x in instance.players) {
                    sb.Append(x.Key).Append(",").Append(x.Value).Append("\n");
                }
                Other.SaveWorldFile("last_onlines", sb.ToString());
            } catch (Exception e) {
                Log.Error (e);
            }
        }

        

        public void run(long k) {
            if (!timer.tick()) return;

            var now = SharpUtils.timeStamp();

            var pl = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers (pl, null);
            foreach (IMyPlayer x in pl) {
                if (x.IsBot) continue;

                var id = x.IdentityId;
                long prevSeen = 0;
                if (players.ContainsKey(id)) {
                    prevSeen = players[id];
                    var timePassed = now - prevSeen;
                    if (timePassed > LOGIN_TIME_OUT) {
                        foreach (Listener l in listeners) { l.onConnected (x, timePassed); }
                    }
                    players[id] = now;
                } else {
                    foreach (Listener l in listeners) { l.onNewPlayer (x); }
                    players.Add(id, now);
                }
            }
        }

        public interface Listener {
            void onNewPlayer (IMyPlayer p);
            void onConnected (IMyPlayer p, long timePassedInSec);
        }
    }
}
