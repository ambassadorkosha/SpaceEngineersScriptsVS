using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Scripts.Specials.ItemSpawner {
   public class SpawnedItem {
        internal MyDefinitionId id;
        internal double amount;
        internal double max = -1;
        internal bool reactOnConseal = false;

        public bool spawn (double mlt, IMyInventory inventory, Dictionary<MyDefinitionId, MyFixedPoint> items = null) {
            var total = mlt * amount;


            if (total < 0) {

                if (items.ContainsKey (id)) {
                    var amount = (double)items[id];
                    if (amount > max) {
                        var targetLeft = Math.Max(amount + total, max);
                        var changed = amount - targetLeft;
                        inventory.RemoveAmount (id, changed);
                    }
                }
            } else {
                if (max > 0 && items != null) {
                    if (items.ContainsKey(id)) {
                        total = Math.Min(max - (float)items[id], total);
                        if (total <= 0) {
                            return false;
                        }
                    } else {
                        total = Math.Min(max, total);
                    }
                }
                inventory.AddItem(id, total);
            }

            
            return true;
        }
    }
}
