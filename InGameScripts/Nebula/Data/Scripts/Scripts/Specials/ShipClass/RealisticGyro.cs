using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using ServerMod;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using System;
using Digi;

namespace Scripts
{

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Gyro), true)]//LargeBlockMediumAtmosphericThrustT04
	public class RealisticGyro : MyGameLogicComponent
	{
		public IMyGyro Block;
		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			Block = (IMyGyro)Entity;
			Block.GyroStrengthMultiplier = 1;
			NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
		}

		public override void UpdateBeforeSimulation100()
		{
			try
			{
				var ship = Block.CubeGrid.GetShip();
				if (ship == null) return;
				var atmo = ship.AtmosphereProperties;
				atmo.updateAtmosphere(Block.CubeGrid);
				var dens = atmo.getWingValue();
				Block.GyroStrengthMultiplier = (float)MathHelper.Clamp(dens, 0.30, 1);
			} catch (Exception e)
			{
				Log.ChatError(e);
			}
			
		}
	}
}
