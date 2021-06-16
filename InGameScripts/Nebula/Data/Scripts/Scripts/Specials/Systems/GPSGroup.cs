using Digi;
using Sandbox.Game;
using Sandbox.ModAPI;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VRageMath;

namespace Scripts.Specials.GPS {
    class GPSGroup {
        private static Regex cockpitRegex = new Regex("\\[GPS:([\\w,]+)\\]");
        private static Regex gpsRegex = new Regex("\\[GROUPS:([\\w,]+)\\]");
        private static Regex colorRegex = new Regex("\\[COLOR:([\\w,]+)\\]");
        private static Dictionary<String, Color> KnownColors = new Dictionary<String, Color> ();

        public static void Init () {
            if (MyAPIGateway.Session.isTorchServer()) return;
            MyVisualScriptLogicProvider.PlayerLeftCockpit += LeaveCockpit;
            MyVisualScriptLogicProvider.PlayerEnteredCockpit += EnterCockpit;
        }

        public static void Close () {
            MyVisualScriptLogicProvider.PlayerLeftCockpit -= LeaveCockpit;
            MyVisualScriptLogicProvider.PlayerEnteredCockpit -= EnterCockpit;
        }

        public static void AddColor (String s, Color c) {
            KnownColors.Add (s.ToLower(), c);
        }

        public static void EnterCockpit (String name, long player, String gridname) {
            try {
                var pp = MyAPIGateway.Session.Player;
                if (pp.IdentityId != player) return;

                var groups = new List<String>();
                groups.Add ("IN");

                try {
                    var cockpit = pp.Controller.ControlledEntity as IMyCockpit;
                    if (cockpit != null) {
                        var cname = cockpit.CustomName;
                        groups = ExtractGroups (cname, cockpitRegex, groups);
                    } else {
                        Log.Error (name + "/" + gridname);
                    }
                    
                } catch (Exception e) {
                    Log.Error (e);    
                }
                
                var ss = "";
                foreach (var x in groups) {
                    ss +="," + x;
                }

                FilterGPS (groups);
            } catch (Exception e) {
                Log.Error(e);
            }
        }

        public static void LeaveCockpit (String name, long player, String gridname) {
            try {
                if (MyAPIGateway.Session.Player.IdentityId != player) return;
                var groups = new List<String> ();
                groups.Add ("OUT");
                FilterGPS (groups);
            } catch (Exception e) {
                Log.Error(e);
            }
        }

        public static void FilterGPS (List<String> groups) {
            var pl = MyAPIGateway.Session.Player.IdentityId;
            var buffer = new List<String>();
            var gps = MyAPIGateway.Session.GPS.GetGpsList (pl);
            foreach (var g in gps) {
                buffer.Clear();
                ExtractGroups (g.Description, gpsRegex, buffer);

                if (buffer.Count == 0 || groups.Count == 0) { continue; } 

                bool showOnHud = false;
                foreach (String x in groups) {
                    if (buffer.Contains (x)) {
                        showOnHud = true;
                        break;
                    }
                }

                if (g.ShowOnHud != showOnHud) {
                    g.ShowOnHud = showOnHud;
                    MyAPIGateway.Session.GPS.SetShowOnHud (pl, g, showOnHud);
                    //MyAPIGateway.Session.GPS.ModifyGps (pl, g, showOnHud);
                }
            }
        }

        public static List<String> ExtractGroups (String str, Regex r, List<String> groups = null) {
            if (groups == null) groups = new List<String>();
            foreach (Match m in r.Matches (str)) {
                var data = m.Groups[1].Value;
                foreach (var t in data.Split (',')) {
                    groups.Add(t);
                }
            }
            return groups;
        }
    }
}