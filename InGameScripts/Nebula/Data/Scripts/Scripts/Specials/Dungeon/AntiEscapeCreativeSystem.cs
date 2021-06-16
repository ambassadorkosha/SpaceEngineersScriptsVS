using Digi;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scripts.Shared;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials {
    public class AntiEscapeCreativeSystem  {
        static Runner runner = new Runner();
        static Dictionary<Vector3D, float> areas = new Dictionary<Vector3D, float>();
        static float SAFE_DISTANCE = 5000;
        

        public static void AddRestrictedArea (Vector3D pos, float distance) {
            areas.Add (pos, distance);
        }
        public static void Init () {
            if (MyAPIGateway.Session.IsServer) {
                FrameExecutor.addFrameLogic (runner);
            }
        }

        public static bool CheckGrid (Vector3 init, Vector3 pos) {
            if (init == Vector3D.Zero) { //IS LOCAL PROJECTION
                return true;
            }
            foreach (var x in areas) {
                var wasOutside = (x.Key - init).Length() > x.Value;
                if (wasOutside) {
                    if ((x.Key - pos).Length() < x.Value+SAFE_DISTANCE)  {
                        Common.SendChatMessage ("Somebody tried to escape museum planet. How naive ... ");
                        return false;
                    }
                } else {
                    if ((x.Key - pos).Length() > x.Value-SAFE_DISTANCE) {
                        Common.SendChatMessage ("Somebody tried to escape museum planet. How naive ... ");
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool CheckCharacter (Vector3 pos) {
            foreach (var x in areas) {
                var p = (x.Key - pos).Length();
                if (p > x.Value-SAFE_DISTANCE && p < x.Value+SAFE_DISTANCE) return false;
            }
            return true;
        }

        static List<IMyPlayer> players_buffer = new List<IMyPlayer>();
        public static void Check () {
            GameBase t = GameBase.instance;
            var l = t.gridToShip.Count;
            if (l > 0) {
                for (var x=0; x<10; x++) {
                    var z = GameBase.r.Next(l);
                    var s = t.gridToShip.Values.ElementAt (z);
                    var g = s.grid;
                    if (!CheckGrid (s.initVector, s.grid.PositionComp.WorldMatrix.Translation)) {
                        MyAPIGateway.Utilities.InvokeOnGameThread(() => { FrameExecutor.addDelayedLogic(1, new GridRemover(s, true)); });
                    }
                }
            }

            players_buffer.Clear();
            MyAPIGateway.Multiplayer.Players.GetPlayers(players_buffer);
            l = players_buffer.Count;
            if (l>0) {
                
                for (var x=0; x<5; x++) {
                     var z = GameBase.r.Next(l);
                     var p = players_buffer[z];
                     var character = p.Character;
                     if (character != null) {
                         if (!CheckCharacter (character.PositionComp.WorldMatrix.Translation)) {
                             MyAPIGateway.Utilities.InvokeOnGameThread(() => { character.Kill(); });
                         }
                     }
                }
            }

        }

        class Runner : Action1<long> {
            public void run(long k) {
                 AntiEscapeCreativeSystem.Check ();
            }
        }
    }
}
