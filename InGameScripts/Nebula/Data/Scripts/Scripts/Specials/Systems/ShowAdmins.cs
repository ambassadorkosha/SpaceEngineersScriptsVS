using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Text;
using Digi;
using Slime;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using static Draygo.API.HudAPIv2;

namespace Scripts.Specials {

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class ShowAdmins : MySessionComponentBase {
        static Dictionary<long, SpaceMessage> messages = new Dictionary<long, SpaceMessage>();
        static bool IsDedicated;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent) {
            IsDedicated = MyAPIGateway.Session.isTorchServer();
        }

        private AutoTimer timer = new AutoTimer(3, 3);
        private static StringBuilder admin = new StringBuilder("Admin");
        
        public override void Draw() {
            try {
                if (IsDedicated) return;
                if (GameBase.HudAPI == null || !GameBase.HudAPI.Heartbeat || MyAPIGateway.Session.Player == null || MyAPIGateway.Session.Player.Character == null) {
                    foreach (var x in messages) { x.Value.DeleteMessage(); }
                    messages.Clear();
                    return;
                }

                timer.tick();

                foreach (var x in messages) { x.Value.DeleteMessage(); }
                messages.Clear();

                var ch = MyAPIGateway.Session.Player.Character;
                var sp = new BoundingSphereD(ch.WorldMatrix.Translation, 200);
                var ents = MyEntities.GetTopMostEntitiesInSphere(ref sp);
                ents.Remove(ch as MyEntity);
                
                foreach (var x in ents) {
                    var d = (x as IMyCharacter);
                    if (d == null) continue;
                    var player = d.GetPlayer ();
                    if (player == null) continue;
                    if (player.PromoteLevel <= MyPromoteLevel.Scripter) continue;
                    if (!messages.ContainsKey(player.IdentityId)) {
                        var m = new SpaceMessage(admin, d.WorldMatrix.Translation + d.WorldMatrix.Up * 3, MyAPIGateway.Session.Camera.WorldMatrix.Up, MyAPIGateway.Session.Camera.WorldMatrix.Left, 1.2, TxtOrientation: TextOrientation.center);
                        messages.Add(player.IdentityId, m);
                    }
                }
            } catch (Exception e) {
                Log.Error(e);
            }
        }
    }
}
