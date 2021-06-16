using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using VRage;
using VRage.Game.ModAPI;
using Item = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace Scripts.Specials.Automation.Graphs {
    public class Transaction {
        Dictionary<IMyInventory, Dictionary<Item, MyFixedPoint>> cargoItems = new Dictionary<IMyInventory, Dictionary<Item, MyFixedPoint>>();
        public void AddTransaction (IMyInventory from, IMyInventory to, Item item, MyFixedPoint amount) {

        }
    }
}
