using Sandbox.Game.Entities;
using Scripts.Specials.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace Scripts.Specials.Hologram.Data {
     public class HoloTarget {
        public int method = 0;
        public long entityId = 0; 
        public HoloTarget() { }
        public HoloTarget (IMyCubeBlock block) {
            this.method = 0;
            this.entityId = block.EntityId;
        }

        internal IMySlimBlock Find(HologramProjector hologramProjector) {
            MyEntity myEntity;
            if (MyEntities.TryGetEntityById (entityId, out myEntity)) { 
                if (myEntity.Closed || myEntity.MarkedForClose) {
                    Common.SendChatMessage ("Closed:" + entityId);
                    return null;
                }

                if (myEntity is IMyCubeBlock) {
                    return (myEntity as IMyCubeBlock).SlimBlock;
                }

                if (myEntity is IMySlimBlock) {
                    return (myEntity as IMySlimBlock);
                }

                Common.SendChatMessage ("Other type:" + myEntity.GetType());

                return null;
            }

            Common.SendChatMessage ("!TryGetEntityById:" + entityId);

            return null;
        }
    }
    
}
