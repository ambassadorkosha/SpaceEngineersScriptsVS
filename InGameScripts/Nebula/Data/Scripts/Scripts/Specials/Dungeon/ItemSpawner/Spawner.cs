using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using VRage.Game.ModAPI;
using Digi;
using Scripts.Specials.ItemSpawner;
using Scripts.Specials.Messaging;

namespace ServerMod {
    public abstract class Spawner : MyGameLogicComponent {
        protected long lastRun = long.MaxValue;
        protected static long INTERVAL = 1666;
        protected static long CONSEALED = INTERVAL * 10;

        protected ItemSpawnInfo spawner;
        protected IMyTerminalBlock container;

        protected int tick = 0;


        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            if (MyAPIGateway.Multiplayer.IsServer) {
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
                container = (IMyTerminalBlock)Entity;
            }
        }

        public void parse (String s) {
            spawner = new ItemSpawnInfo();
            try {
                var lines = s.Split('\n');
                foreach (var line in lines) {
                    var d = line.Split(' ');
                    var interval = int.Parse(d[0]);
                    var amount = Double.Parse(d[1]);
                    var what = d[2];
                    var max = Double.Parse(d[3]);
                    var conceal = d.Length > 4 ? d[4] : "";
                    spawner.add(interval, amount, what, max, conceal.Equals("true"));
                }
            } catch { }
        }

        public virtual void onChanged () { }
        public override void UpdateBeforeSimulation100() {
            base.UpdateBeforeSimulation100();

            try {
                var now = SharpUtils.msTimeStamp();

                if (now-lastRun > CONSEALED) {
                    var times = (now-lastRun) / INTERVAL;
                    try {
                        tick++;
                        if (spawner.spawn(tick, 1, container.GetInventory(0), container.GetInventory().CountItems(), false)) {
                            onChanged ();
                        }
                        times --;

                        if (spawner.haveReactOnConseal) {
                            var items = container.GetInventory().CountItems();
                            var inv = container.GetInventory(0);
                            for (var x=0; x <= times; x++) {
                                tick++;
                                if (spawner.spawn(tick, 1, inv, items, true)) {
                                    items = container.GetInventory().CountItems(); // need recalculate
                                    onChanged ();
                                }
                            }
                        } else {
                            tick+=(int)times;
                        }
                        
                    } catch { }
                } else {
                    try {
                        tick++;
                        if (spawner.spawn(tick, 1, container.GetInventory(0), container.GetInventory().CountItems(), false)) {
                            onChanged ();
                        }
                    } catch { }
                }

                lastRun = now;
            } catch { }
            
        }
    }
}
