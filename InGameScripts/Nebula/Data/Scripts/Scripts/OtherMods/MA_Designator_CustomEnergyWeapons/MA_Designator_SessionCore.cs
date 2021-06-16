using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace MA_Designator_CustomEnergyWeapons{
	
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	
	public class MA_Designator_SessionCore : MySessionComponentBase{
		
		
		//Configuration
		public static Guid MyModStorageGuid = new Guid("DE3ED11E-441D-490F-9849-9527564C8C7D"); //Change This To A Unique GUID For Your Mod
		
		/*
		
		For Color References, Please Visit Link Below:
		
		https://github.com/KeenSoftwareHouse/SpaceEngineers/blob/a109106fc0ded66bdd5da70e099646203c56550f/Sources/VRage.Math/Color.cs
		
		*/
		
		//Do Not Edit Values Below Here
		
		public static bool SetupComplete = false;
		public int tickTimer = 0;
		
		public override void UpdateBeforeSimulation(){
			
		}
		
		protected override void UnloadData(){
			
			
			
		}
		
	}
	
}