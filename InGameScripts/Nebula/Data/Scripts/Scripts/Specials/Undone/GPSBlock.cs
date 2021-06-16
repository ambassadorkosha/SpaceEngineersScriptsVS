using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Main {
    
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), true, new string[] { "LargeGridCoreNPC"  })]
    class GPSBeacon : GPSBlock { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RadioAntenna), true, new string[] { "LargeBlockRadioAntennaNPC" , "CompactAntennaNPC"  })]
    class GPSRadioAntenna : GPSBlock { }

    abstract class GPSBlock : MyGameLogicComponent {
		public bool triggered = false;
		
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) { 
            if (MyAPIGateway.Multiplayer.IsServer) {
                NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame() {
            base.UpdateOnceBeforeFrame();
            SendGPS();
        }
        public void SendGPS () { 
			if (MyAPIGateway.Multiplayer.IsServer && !triggered) {
				var pos = Entity.WorldMatrix;
				var pos2 = new Vector3D (pos.Translation.X,pos.Translation.Y,pos.Translation.Z);
				MyVisualScriptLogicProvider.AddGPSForAll (name: "Captured signal", description:"Unknown Object - Ship/Station NPC", position:pos2, GPSColor:new Color (r:139,g:0,b:139, a:125), disappearsInS:900); 
				triggered = true;
			}
		}
    }
}