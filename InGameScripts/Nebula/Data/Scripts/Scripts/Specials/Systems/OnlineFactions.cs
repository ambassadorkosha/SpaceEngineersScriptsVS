using System;
using System.Collections.Generic;
using System.Linq;
using Digi;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using Scripts.Base;
using Scripts;

//using Digi;

namespace ServerMod {
    public class OnlineFactions : Action1<long> {
        private static Dictionary<long, Pair<IMyFaction, long>> onlineFactions = new Dictionary<long, Pair<IMyFaction, long>>();
        private static Dictionary<long, Pair<IMyPlayer, long>> onlinePlayers = new Dictionary<long, Pair<IMyPlayer, long>>();
        private static readonly int TIMES = 10;
        private static readonly Object lockObject = new Object();
        
        private AutoTimer timeout = new AutoTimer(60);
        
        public void run(long tick) {
            if (!timeout.tick()) return;

            var factions = Other.GetFactionsWithOnlinePlayers();
            
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, x=> !x.IsBot);
            
            lock (lockObject) {
                
                var newOnlinePlayers = onlinePlayers.ToDictionary(entry => entry.Key, entry => {
                    var p = entry.Value.k;
                    if (players.Contains(p)) {
                        return new Pair<IMyPlayer, long>(p, 0);
                    } else {
                        return new Pair<IMyPlayer, long>(p, entry.Value.v+1);
                    }
                });


                foreach (var p in players) {
                    if (!newOnlinePlayers.ContainsKey(p.PlayerID)) {
                        newOnlinePlayers.Add(p.PlayerID, new Pair<IMyPlayer, long>(p, 0));
                    }
                }
                
                var newOnlineFactions = onlineFactions.ToDictionary(entry => entry.Key, entry => {
                    var f = entry.Value.k;
                    if (factions.Contains(f)) {
                        return new Pair<IMyFaction, long>(f, 0);
                    } else {
                        return new Pair<IMyFaction, long>(f, entry.Value.v+1);
                    }
                });

                
                foreach (var p in factions) {
                    if (!newOnlineFactions.ContainsKey(p.FactionId)) {
                        newOnlineFactions.Add(p.FactionId, new Pair<IMyFaction, long>(p, 0));
                    }
                }
                
                onlinePlayers = newOnlinePlayers;
                onlineFactions = newOnlineFactions;
            }
        }


        public static bool isOnlineUser(long playerId) {
            try {
                if (playerId == 0) {
                    Log.Error ("isOnlineFaction: playerId == 0");
                    return true;
                }

                lock (lockObject) {
                    if (onlinePlayers.ContainsKey(playerId)) {
                        var info = onlinePlayers[playerId];
                        //Log.Error ("isOnlineFaction (player): pl" + info.v + " | " + playerId +"  (t="  + info.v + " / " + TIMES + ")");
                        return info.v < TIMES;
                    } else {
                        //Log.Error ("isOnlineFaction (player): pl" + playerId +" fact:"+faction.FactionId +" "+ faction.Name);
                        return false;
                    }
                }
            } catch (Exception e) {
                Log.Error(e);
                return false;
            }
        }

        public static bool isOnlineFaction(long playerId) {
            try {
                if (playerId == 0) {
                    Log.Error ("isOnlineFaction: playerId == 0");
                    return true;
                }

                //foreach (var x in onlineFactions) {
                //    Log.Info ("FAC:" + x.Key + " / " + x.Value.k + " / " + x.Value.v);
                //}
                //
                //foreach (var x in onlinePlayers) {
                //    Log.Info ("PLA:" + x.Key + " / " + x.Value.k + " / " + x.Value.v);
                //}
                
                
                var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
                if (faction != null)  {
                    if (faction.Tag.Equals ("SCAV")) {
                        return true;
                    }

                    lock (lockObject) {
                        if (onlineFactions.ContainsKey(faction.FactionId)) {
                            var info = onlineFactions[faction.FactionId];
                            //Log.Info ("isOnlineFaction  (fact): pl" + playerId +" fact:"+faction.FactionId +" "+ faction.Name+ " / (t="  + info.v + " / " + TIMES + ")");
                            return info.v < TIMES;
                        } else {

                            //Log.Info ("isOnlineFaction (fact): pl" + playerId +" fact:"+faction.FactionId +" "+ faction.Name);
                            return false;
                        }
                    }
                } else {
                    lock (lockObject) {
                        
                        if (onlinePlayers.ContainsKey(playerId)) {
                            var info = onlinePlayers[playerId];
                            //Log.Error ("isOnlineFaction (player): pl" + info.v + " | " + playerId +"  (t="  + info.v + " / " + TIMES + ")");
                            return info.v < TIMES;
                        } else {
                            //Log.Error ("isOnlineFaction (player): pl" + playerId +" fact:"+faction.FactionId +" "+ faction.Name);
                            return false;
                        }
                    }
                }
            } catch (Exception e) {
                Log.Error(e);
                return false;
            }
        }

    }
}