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

namespace ServerMod {

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), true, new string[] { 
        "TreasureChestLargeGridLarge",  
        "TreasureChestLargeGridSmall",  
        "TreasureChestSmallGridLarge",  
        "TreasureChestSmallGridMedium",  
        "TreasureChestSmallGridSmall"
        })]

    public class TreasureChest : Spawner {
        bool force = false;
        String data;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            if (MyAPIGateway.Multiplayer.IsServer) {
                force = true;
                container.CustomDataChanged += Container_CustomDataChanged;
                force = false;
                var data = container.CustomData;
                parse(data);
           }
            
        }

        private void Container_CustomDataChanged(IMyTerminalBlock obj) {
            try {  
                if (force) return;

                var player = Other.GetNearestPlayer(container.GetPosition());
                if (player.IsPromoted) {
                    data = obj.CustomData;
                    parse(data);
                } else {
                    force = true;
                    container.CustomData = data; //return prev
                    force = false;
                }
            } catch (Exception e)  {
                Log.Error (e);
            }
            
        }
    }
}
