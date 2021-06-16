using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Scripts.Specials.ItemSpawner {
    public class ItemSpawnInfo {
        internal Dictionary<int, List<SpawnedItem>> all = new Dictionary<int, List<SpawnedItem>>();
        public bool haveReactOnConseal = false;

        public bool spawn (int tick, double mlt, IMyInventory inventory, Dictionary<MyDefinitionId, MyFixedPoint> items = null, bool isFromConseal = false) {
            bool spawned = false;
            foreach (var x in all) {
                if (tick % x.Key  == 0) {
                    foreach (var y in x.Value) {
                        if (!isFromConseal || y.reactOnConseal) {
                            spawned |= y.spawn(mlt, inventory, items);
                        }
                    }
                }
            }
            return spawned;
        }

        public ItemSpawnInfo add (int tick, double amount, string id, double max = -1, bool reactOnConseal = false) {
            var si = new SpawnedItem();
            si.id = MyDefinitionId.Parse("MyObjectBuilder_"+id);
            si.amount = amount;
            si.max = max;
            si.reactOnConseal = reactOnConseal;

            if (reactOnConseal) {
                haveReactOnConseal = reactOnConseal;
            }

            List<SpawnedItem> list = null;

            if (!all.ContainsKey (tick)) {
                list = new List<SpawnedItem>();
                all.Add(tick, list);
            } else {
                list = all[tick];
            }

            list.Add(si);
            return this;
        }
    }
}
