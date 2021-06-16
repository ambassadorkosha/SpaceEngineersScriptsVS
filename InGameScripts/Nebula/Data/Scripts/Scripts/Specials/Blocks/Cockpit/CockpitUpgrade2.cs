using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Digi;
using Digi2.AeroWings;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Shared.Serialization;
using Scripts.Specials.Blocks.Reactions;
using Scripts.Specials.Messaging;
using ServerMod;
using Slime;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;


namespace Scripts.Specials.Blocks
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
	public class CockpitUpgrade2 : MyGameLogicComponent
	{
		private Ship Ship;
		private IMyCockpit Controller;
		private List<MyKeys> keys = new List<MyKeys>();

		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{ //REAL INIT
			if (Controller == null) Controller = Entity as IMyCockpit;
			if (MyAPIGateway.Session.isTorchServer()) return;

			NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
		}

        private static MoveIndicator MoveIndicator1 = new MoveIndicator () {  Mechanics = MechanicsAxis.Roll, Threshold=0 };
        private static MoveIndicator MoveIndicator2 = new MoveIndicator () {  Mechanics = MechanicsAxis.Pitch, Threshold=0 };
        private static MoveIndicator MoveIndicator3 = new MoveIndicator () {  Mechanics = MechanicsAxis.InvertedYaw, Threshold=0 };

        public override void UpdateBeforeSimulation()
		{
			if (MyAPIGateway.Session.isTorchServer()) return;
			if (Controller.Pilot != MyAPIGateway.Session.Player.Character) return;
			Ship = Controller.CubeGrid.GetShip();
			if (Ship == null) return;
			keys.Clear();
			MyAPIGateway.Input.GetListOfPressedKeys(keys);

            var dict = new Dictionary<MechanicsAxis, MoveIndicator>();


            MoveIndicator1.Threshold = Math.Abs(Ship.PilotActions.X);
            MoveIndicator2.Threshold = Math.Abs(Ship.PilotActions.Y);
            MoveIndicator3.Threshold = Math.Abs(Ship.PilotActions.Z);

            MoveIndicator1.Mechanics = Ship.PilotActions.X > 0 ? MechanicsAxis.Pitch : MechanicsAxis.InvertedPitch;
            MoveIndicator2.Mechanics = Ship.PilotActions.Y > 0 ? MechanicsAxis.Yaw : MechanicsAxis.InvertedYaw;
            MoveIndicator3.Mechanics = Ship.PilotActions.Z > 0 ? MechanicsAxis.Roll : MechanicsAxis.InvertedRoll;

            dict.Add (MoveIndicator1.Mechanics, MoveIndicator1);
            dict.Add (MoveIndicator2.Mechanics, MoveIndicator2);
            dict.Add (MoveIndicator3.Mechanics, MoveIndicator3);

            //Common.SendChatMessage (Ship.PilotActions + " " + MoveIndicator1.Mechanics + " "  + MoveIndicator2.Mechanics + " " + MoveIndicator3.Mechanics +" | " + MoveIndicator1.Threshold + " " + MoveIndicator2.Threshold + " " + MoveIndicator3.Threshold);

            foreach (var g in Ship.grid.GetConnectedGrids(GridLinkTypeEnum.Logical))
			{
				var ship = g.GetShip();
				if (ship == null) continue;
				foreach (var x in ship.BlocksWithReactions)
				{
					if (x.keyReactions != null)
					{
						x.keyReactions.React(keys, dict);
					}
				}
			}	
		}
	}
}
