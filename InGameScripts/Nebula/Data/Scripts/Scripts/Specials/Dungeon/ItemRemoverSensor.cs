using Digi;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
//using Digi;

namespace Scripts {

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SensorBlock), true, new string[] { "ItemRemoverSensor", "ItemRemoverSensorSmall" })]
    public class ItemRemoverSensor: MyGameLogicComponent {
        public IMySensorBlock block;
        private List<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo> buffer = new List<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo>();
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            block = (Entity as IMySensorBlock);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateBeforeSimulation() {
            base.UpdateBeforeSimulation();
            try {
                buffer.Clear();
                block.DetectedEntities (buffer);

                foreach (var x in buffer) {
                    var e = MyEntities.GetEntityById (x.EntityId);
                    if (e.HasInventory) {
                        var inv = e.GetInventory ();
                    
                        inv.RemoveAmount (KeyCardDoors.RRED_CARD, 9999999);
                        inv.RemoveAmount (KeyCardDoors.GGREEN_CARD, 9999999);
                        inv.RemoveAmount (KeyCardDoors.BBLUE_CARD, 9999999);
                    }
                } 
            } catch (Exception e) {
                
            }
        }
    }


}
