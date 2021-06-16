using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Sandbox.Common.ObjectBuilders;
using SpaceEngineers.Game.ModAPI;
using System.Collections.Generic;

namespace ServerMod.Specials
{
	/*[MyEntityComponentDescriptor(typeof(MyObjectBuilder_MedicalRoom), true)]
    public class RespawnSystem : MyGameLogicComponent  {
        private IMyMedicalRoom myBlock;


		public static Dictionary<long, HashSet<IMyMedicalRoom>> dictionary = new Dictionary<long, HashSet<IMyMedicalRoom>>();

        
        private int state = 0; 


		public static void Register (IMyMedicalRoom medical)
		{
			var player = medical.OwnerId;
			HashSet<IMyMedicalRoom> set = new HashSet<IMyMedicalRoom>();
			if (!dictionary.ContainsKey(player))
			{
				set = new HashSet<IMyMedicalRoom>();
				dictionary.Add (player, set);
			}
		}

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            myBlock = (Entity as IMyMedicalRoom);

			dictionary.Add(myBlock.OwnerId, nwe Listr)

			myBlock.PropertiesChanged += MyBlock_PropertiesChanged;
            myBlock.OnMarkForClose += MyBlock_OnMarkForClose;

        }

        private void MyBlock_PropertiesChanged(IMyTerminalBlock obj) {
            myBlock.DetectAsteroids = false;
        }

        private void MyBlock_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
             myBlock.OnMarkForClose -= MyBlock_OnMarkForClose;
             myBlock.PropertiesChanged -= MyBlock_OnMarkForClose;
        }
    }*/
}
