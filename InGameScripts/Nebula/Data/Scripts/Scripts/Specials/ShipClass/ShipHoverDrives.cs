using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using Digi;
using ServerMod;
using Shame.HoverEngine;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials.ShipClass
{ 
	public class ShipHoverDrives		
	{
		OrientationOrderedBlocks Hovers;
		public ShipHoverDrives()
		{
			Hovers = new OrientationOrderedBlocks();
			Hovers.CompareMethod = CompareHovers;
		}
		public void Add(IMyTerminalBlock InBlock)
		{
			Hovers.Add(InBlock);
		}
		public void Remove(IMyTerminalBlock InBlock)
		{
			Hovers.Remove(InBlock);
		}
		static public bool IsHover(IMyCubeBlock InBlock)
		{
			return InBlock is IMyThrust && (InBlock.BlockDefinition.SubtypeId.StartsWith("SmallHover") || InBlock.BlockDefinition.SubtypeId.StartsWith("LargeHover"));
		}
		public bool CompareHovers(MyBlockOrientation O1, MyBlockOrientation O2)
		{
			return O1.Forward == O2.Forward;
		}
		
		public void UpdateHovers(MyPlanet ClosestPlanet, IMyCubeGrid CurrentGrid, Ship ship) {
			if (ClosestPlanet == null) return;
			if (CurrentGrid == null) return;
			if (CurrentGrid.Physics == null) return;

			Vector3D Gravity = CurrentGrid.Physics.Gravity;

			if (Gravity == Vector3D.Zero || CurrentGrid.IsStatic)
			{
				foreach (OrientationOrderedBlocks.Blocks B in Hovers.OrientedBlocks){
					foreach (IMyTerminalBlock T in B.BlocksList){
						HoverEngine HE = T?.GameLogic?.GetAs<HoverEngine>();
						if (HE != null && HE.block != null){
							if (HE.block.ThrustMultiplier != 0) { 
								HE.block.ThrustOverride = 0;
							}
							HE.SetThustMultiplier (0);
							
						}
					}
				}
				return;
			}

			Vector3D NDown = Gravity;
			double G = NDown.Normalize();
			Vector3D NUp = -NDown;

			Vector3D Velocity = CurrentGrid.Physics.LinearVelocity;
			double VScalar = Velocity.Length();

			double VerticalVelocityScalar = Vector3D.Dot(Velocity, NUp);

			double GridMass = -1;
			
			foreach (OrientationOrderedBlocks.Blocks B in Hovers.OrientedBlocks){
				if (B != null && B.BlocksList.Count > 0){
					HashSet<IMyTerminalBlock>.Enumerator BlockEnum = B.BlocksList.GetEnumerator();
					if (BlockEnum.MoveNext())	{						
						IMyTerminalBlock Block = BlockEnum.Current;
						Vector3D ThrustDir = Block.WorldMatrix.Forward;						
						double CosAngle = Vector3D.Dot(ThrustDir, NDown);
						// Let's set the angle between the thruster forward vector and the gravity "down" vector
						if (CosAngle > HoverEngine.MaxOperationalAngleCos){
							float CommonThrustScale = 0;
							double MeanTerrainElevationGain = 0;	// Terrain elevation gain is calculated usgin two raycast points: previous and current, so we can take terrain change under the grid into account

							int ActiveEngineCount = 0;							
							double FullEffectiveThrust = 0;	// All the thrust available for automatic control
							foreach (IMyTerminalBlock T in B.BlocksList) {
								HoverEngine HE = T?.GetAs<HoverEngine>();
								if (HE != null && HE.block != null && HE.block.IsWorking)	{
									HE.CheckDistanceToClosestSurface(ref NDown, ref Velocity);
									if (HE.distanceToSurfaceCached > 0)	{
										ActiveEngineCount++;
										float ThrustScale = (float)MathHelper.Clamp(HE.maxAltitude / HE.distanceToSurfaceCached, 0, 1.0);
                                        // Square proportion for distance to surface below
                                        CommonThrustScale = Math.Max(CommonThrustScale, ThrustScale * ThrustScale);

										// Get terrain elevation change direction
										Vector3D TerrainDirection = HE.TryGetRaycastElevationDifference();
										// Use ship velocity as a scaling factor and project it onto vertical gravity vector
										MeanTerrainElevationGain += Vector3D.Dot(TerrainDirection * VScalar, NUp);
										//MeanAltitude += HE.distanceToSurfaceCached;
									}
								}
							}

							int ControllableEngineCount = 0;
							foreach (IMyTerminalBlock T in B.BlocksList){
								HoverEngine HE = T?.GameLogic?.GetAs<HoverEngine>();//
								if (HE != null && HE.block != null)	{
                                    if (HE.distanceToSurfaceCached > 0)
                                    {
                                        HE.SetThustMultiplier(CommonThrustScale);

                                        if (HE.autoHoverEnabled && HE.block.IsWorking && HE.rayHitHappened)
                                        {
                                            ControllableEngineCount++;
                                            FullEffectiveThrust += HE.block.MaxEffectiveThrust;
                                        }
                                    } else
                                    {
                                        HE.SetThustMultiplier(0);
                                    }
                                    
								}
							}
							
							MeanTerrainElevationGain /= ActiveEngineCount;

							if (GridMass < 0) {
								GridMass = ship.massCache.shipMass;
							}
							double MaxAcceleration = FullEffectiveThrust / Math.Max(0.00001, GridMass);
							double AccelerationToGScale = G / MaxAcceleration;

							foreach (IMyTerminalBlock T in B.BlocksList){
								HoverEngine HE = T?.GameLogic?.GetAs<HoverEngine>();
								if (HE != null && HE.block != null && T.IsWorking){
									if (HE.autoHoverEnabled){
										if (HE.rayHitHappened)	{
											double HeightDelta = MathHelper.Clamp(HE.heightTargetMax - HE.distanceToSurfaceCached, -1, 1);	// Height proportional regulation
											
											Vector3D HeightDeltaCorrection = NUp * HeightDelta * MaxAcceleration * AccelerationToGScale;
											double VerticalVelocityFactor = MeanTerrainElevationGain - VerticalVelocityScalar;				// If we are flying, take terrain elevation change into account when calculating vertical velocity

											Vector3D VelocityCorrection = NUp * (MathHelper.Clamp(VerticalVelocityFactor * AccelerationToGScale * 0.5, -1, 1) * MaxAcceleration);
											
											// Calculate resulting thrust vector we need
											Vector3D DesiredAcceleation =
												HeightDeltaCorrection		// this is height correction vector, if we are higher than the height we set in the drive properties, go down (and vise versa)
												- Gravity					// Just gravity, we need to compensate it
												+ VelocityCorrection;		// Vertical velocity component: if we are falling too fast, adjust the thrust to compensate speed. This is here to defeat hover fluctuations.
											double DotAcc = Vector3D.Dot(NUp, DesiredAcceleation);
											double GravityCompensation = Math.Max(0, DesiredAcceleation.Length() * Math.Sign(DotAcc));

											float OverrideP = (float)(GravityCompensation * GridMass / FullEffectiveThrust);
											HE.block.ThrustOverridePercentage = MathHelper.Clamp(OverrideP, 0.0001f, 1.0f);
										}
										else{
											HE.block.ThrustOverride = 0.0f;
										}										
									}																	
								}
							}
						}
						else
						{
							foreach (IMyTerminalBlock T in B.BlocksList)
							{
								HoverEngine HE = T?.GameLogic?.GetAs<HoverEngine>();
								if (HE != null && HE.block != null)
								{
									if (HE.block.ThrustMultiplier != 0) {
										HE.block.ThrustOverride = 0;
									}
									HE.SetThustMultiplier(0);
								}
							}
						}
					}
				}
			}
		}
	}
}
