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
using SpaceEngineers.Game.ModAPI;
using ProtoBuf;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace MA_Designator_CustomEnergyWeapons{
	
	public static class Utilities{
		
		public static long CorruptFounderId = 0;
		
		public static Dictionary<string, string> UniqueIdConversionReference = new Dictionary<string, string>();
		
		public static List<string> ShieldBlockSubtypeNames = new List<string>();
		
		public static bool DefenseShieldModCheck = false;
		public static bool DefenseShieldModActive = false;
		
		public static Random Rnd = new Random();
		
		public static void ApplyPenetrativeDamage(IMyCubeGrid cubeGrid, Vector3D startCoords, Vector3D endCoords, float damageAmount, long damageOwner, double penetrateDistance){
			
			var newStartCoords = Vector3D.Normalize(startCoords - endCoords) * 50 + endCoords;
			var newEndCoords = Vector3D.Normalize(endCoords - startCoords) * penetrateDistance + endCoords;
			var cellList = new List<Vector3I>();
			cubeGrid.RayCastCells(newStartCoords, newEndCoords, cellList);
			
			var blockList = new List<IMySlimBlock>();
			
			foreach(var cell in cellList){
				
				var block = cubeGrid.GetCubeBlock(cell);
				
				if(block == null){
					
					continue;
					
				}
				
				var blockPosition = Vector3D.Zero;
				block.ComputeWorldCenter(out blockPosition);
				var thisBlockDist = Vector3D.Distance(blockPosition, endCoords);
				
				if(thisBlockDist < penetrateDistance){
					
					blockList.Add(block);
					
				}
				
			}
			
			foreach(var targetBlock in blockList){
				
				if(targetBlock == null){
					
					continue;
					
				}
				
				targetBlock.DoDamage(damageAmount, MyStringHash.GetOrCompute("Laser"), true, null, damageOwner);
				
			}
			
		}
		
		public static void ApplyRegularDamage(IMyCubeGrid cubeGrid, Vector3D startCoords, Vector3D endCoords, float damageAmount, long damageOwner){
			
			var newStartCoords = Vector3D.Normalize(startCoords - endCoords) * 50 + endCoords;
			var newEndCoords = Vector3D.Normalize(endCoords - startCoords) * 50 + endCoords;
			var cellList = new List<Vector3I>();
			cubeGrid.RayCastCells(newStartCoords, newEndCoords, cellList);
			
			IMySlimBlock closestBlock = null;
			double closestBlockDist = 0;
			
			foreach(var cell in cellList){
				
				var block = cubeGrid.GetCubeBlock(cell);
				
				if(block == null){
					
					continue;
					
				}
				
				var blockPosition = Vector3D.Zero;
				block.ComputeWorldCenter(out blockPosition);
				var thisBlockDist = Vector3D.Distance(blockPosition, endCoords);
				
				if(closestBlock == null){
					
					closestBlock = block;
					closestBlockDist = thisBlockDist;
					
				}
				
				if(thisBlockDist < closestBlockDist){
					
					closestBlock = block;
					closestBlockDist = thisBlockDist;
					
				}
				
			}
			
			if(closestBlock == null){
				
				return;
				
			}
			
			closestBlock.DoDamage(damageAmount, MyStringHash.GetOrCompute("Laser"), true, null, damageOwner);
			
		}
		
		public static Vector3D BubbleShieldCheck(Vector3D fromCoords, Vector3D toCoords, IMyTerminalBlock sourceWeapon, out string bubbleEntityIdString){
			
			bubbleEntityIdString = "";
			
			if(DefenseShieldModCheck == false){
				
				DefenseShieldModCheck = true;
				var shieldBlockDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_UpgradeModule), "EmitterL");
				var shieldBlockDefinition = MyDefinitionManager.Static.GetDefinition(shieldBlockDefinitionId);
				
				if(shieldBlockDefinition == null){
					
					return Vector3D.Zero;
					
				}
				
				DefenseShieldModActive = true;
				
			}
			
			if(DefenseShieldModActive == false){
				
				return Vector3D.Zero;
				
			}
			
			var entityList = new List<MyLineSegmentOverlapResult<MyEntity>>();
			var line = new LineD(fromCoords, toCoords);
			MyGamePruningStructure.GetAllEntitiesInRay(ref line, entityList);
			
			foreach(var listItem in entityList){
				
				if(listItem.Element.Physics == null && listItem.Element.DisplayName == "dShield"){
					
					if(listItem.Element.Render.Visible == false){
						
						continue;
						
					}
					
					long targetGridEntityId = 0;
					IMyEntity targetGridEntity = null;
					
					if(string.IsNullOrEmpty(listItem.Element.Name) == true){
						
						continue;
						
					}
					
					if(long.TryParse(listItem.Element.Name, out targetGridEntityId) == false){
						
						continue;
						
					}
					
					if(MyAPIGateway.Entities.TryGetEntityById(targetGridEntityId, out targetGridEntity) == false){
						
						continue;
						
					}
					
					var targetGrid = targetGridEntity as IMyCubeGrid;
					
					if(targetGrid == null){
						
						continue;
						
					}
					
					bool hostile = false;
					
					foreach(var owner in targetGrid.BigOwners){
						
						if(owner == 0){
							
							continue;
							
						}
						
						var relation = sourceWeapon.GetUserRelationToOwner(owner);
						
						if(relation == MyRelationsBetweenPlayerAndBlock.Enemies || relation == MyRelationsBetweenPlayerAndBlock.Neutral || relation == MyRelationsBetweenPlayerAndBlock.NoOwnership){
							
							hostile = true;
							break;
							
						}
						
					}
					
					if(hostile == false){
						
						continue;
						
					}
					
					bubbleEntityIdString = listItem.Element.Name;
					
					Vector3D returnCoords = Vector3D.Normalize(toCoords - fromCoords) * listItem.Distance + fromCoords;
					var sphereHitCoords = DsRayCast(listItem.Element as IMyEntity, line, sourceWeapon.EntityId, 0, MyStringId.GetOrCompute("Laser"));
					
					if(sphereHitCoords != null){
						
						returnCoords = (Vector3D)sphereHitCoords;
						
					}
					
					return returnCoords;
					
				}
				
			}
			
			return Vector3D.Zero;
			
		}
		
		public static Vector4 ConvertColor(Color color){
			
			return new Vector4(color.X / 10, color.Y / 10, color.Z / 10, 0.1f);
			
		}
		
		public static string GetUniqueIdentifier(bool primary){
			
			if(UniqueIdConversionReference.Keys.Count == 0){
				
				UniqueIdConversionReference.Add("A", "0");
				UniqueIdConversionReference.Add("B", "1");
				UniqueIdConversionReference.Add("C", "2");
				UniqueIdConversionReference.Add("D", "3");
				UniqueIdConversionReference.Add("E", "4");
				UniqueIdConversionReference.Add("F", "5");
				UniqueIdConversionReference.Add("G", "6");
				UniqueIdConversionReference.Add("H", "7");
				UniqueIdConversionReference.Add("I", "8");
				UniqueIdConversionReference.Add("J", "9");
				
			}
			
			var input = "";
			
			if(primary == true){
				
				return input;
				
			}
			
			var result = input;
			
			foreach(var key in UniqueIdConversionReference.Keys){
				
				result = result.Replace(key, UniqueIdConversionReference[key]);
				
			}
			
			return result;
			
		}
		
		public static List<Vector3D> CreateElectricityOffset(double maxRange, double minForwardStep, double maxForwardStep, double maxOffset){
			
			double currentForwardDistance = 0;
			var offsetList = new List<Vector3D>();
			
			while(currentForwardDistance < maxRange){
				
				currentForwardDistance += RandomDouble(minForwardStep, maxForwardStep);
				var lateralXDistance = RandomDouble(maxOffset * -1, maxOffset);
				var lateralYDistance = RandomDouble(maxOffset * -1, maxOffset);
				offsetList.Add(new Vector3D(lateralXDistance, lateralYDistance, currentForwardDistance * -1));
				
			}
			
			return offsetList;
			
		}
		
		public static void CreatePhantomExplosion(Vector3D position, float radius, float damage, long blockEntityId = 0, bool showParticles = false, bool damageVoxels = false, bool createForceImpulse = true){
			
			MyExplosionTypeEnum explosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_50;
			
			if (radius < 2f){
				
				explosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_02;
				
			}else if (radius < 15f){
				
				explosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_15;
				
			}else if (radius < 30f){
				
				explosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_30;
				
			}
			
			MyExplosionInfo myExplosionInfo = default(MyExplosionInfo);
			myExplosionInfo.PlayerDamage = 0f;
			myExplosionInfo.OriginEntity = blockEntityId;
			myExplosionInfo.Damage = damage;
			myExplosionInfo.ExplosionType = explosionType;
			myExplosionInfo.ExplosionSphere = new BoundingSphereD(position, radius);
			myExplosionInfo.LifespanMiliseconds = 700;
			myExplosionInfo.ParticleScale = 1f;
			myExplosionInfo.Direction = Vector3.Down;
			myExplosionInfo.VoxelExplosionCenter = position;
			myExplosionInfo.ExplosionFlags = (MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.CREATE_SHRAPNELS | MyExplosionFlags.APPLY_DEFORMATION);
			myExplosionInfo.VoxelCutoutScale = 1f;
			myExplosionInfo.PlaySound = true;
			myExplosionInfo.ApplyForceAndDamage = true;
			
			if(createForceImpulse == false){
				myExplosionInfo.StrengthImpulse = 0;
			}
			
			myExplosionInfo.ObjectsRemoveDelayInMiliseconds = 40;
			myExplosionInfo.CreateParticleEffect = showParticles;
			myExplosionInfo.AffectVoxels = damageVoxels;
			MyExplosionInfo explosionInfo = myExplosionInfo;
			MyExplosions.AddExplosion(ref explosionInfo);
			
		}
		
		public static void CutVoxels(Vector3D position, float radius){
			
			try{
				
				var voxelShape = MyAPIGateway.Session.VoxelMaps.GetSphereVoxelHand();
				voxelShape.Center = position;
				voxelShape.Radius = radius;
				voxelShape.Transform = MatrixD.CreateWorld(position, Vector3D.Forward, Vector3D.Up);
				var sphere = new BoundingSphereD(position, 500);
				var mapList = new List<IMyVoxelBase>();
				MyAPIGateway.Session.VoxelMaps.GetInstances(mapList);
				
				//Debug:
				//var gps = MyAPIGateway.Session.GPS.Create("Hit", "", position, true);
				//MyAPIGateway.Session.GPS.AddLocalGps(gps);
				
				for(int i = mapList.Count - 1; i >= 0; i--){
					
					if(mapList[i].PositionComp.WorldAABB.Intersects(sphere) == false){
						
						mapList.RemoveAt(i);
						continue;
						
					}
					
					MyAPIGateway.Session.VoxelMaps.CutOutShape(mapList[i], voxelShape);
					
				}
				
			}catch(Exception exc){
				
				MyVisualScriptLogicProvider.ShowNotificationToAll("Cut Voxel Fail", 1000);
				
			}
			
		}
		
		public static void DisplayElectricEffect(MatrixD startMatrix, Vector3D endCoords, float beamRadius, Color color, List<Vector3D> offsetList, bool isDedicated = false){
			
			if(offsetList.Count < 2 || isDedicated == true){
				
				return;
				
			}
			
			var maxDistance = Vector3D.Distance(startMatrix.Translation, endCoords);
			
			for(int i = 0; i < offsetList.Count; i++){
				
				var fromBeam = Vector3D.Zero;
				var toBeam = Vector3D.Zero;
				
				if(i == 0){
					
					fromBeam = startMatrix.Translation;
					toBeam = Vector3D.Transform(offsetList[i], startMatrix);
					
				}else{
					
					fromBeam = Vector3D.Transform(offsetList[i - 1], startMatrix);
					toBeam = Vector3D.Transform(offsetList[i], startMatrix);
					
				}
				
				var vectorColor = color.ToVector4();
				MySimpleObjectDraw.DrawLine(fromBeam, toBeam, MyStringId.GetOrCompute("WeaponLaser"), ref vectorColor, beamRadius);
				
				if(Vector3D.Distance(startMatrix.Translation, toBeam) > maxDistance){
					
					break;
					
				}

			}
			
		}
		
		//Method From DarkStar for Detecting Sphere Intersections in Defense Shield
		public static Vector3D? DsRayCast(IMyEntity shield, LineD line, long attackerId, float damage, MyStringId effect){
			
			var worldSphere = new BoundingSphereD(shield.PositionComp.WorldVolume.Center, shield.PositionComp.LocalAABB.HalfExtents.AbsMax());
			var myObb = MyOrientedBoundingBoxD.Create(shield.PositionComp.LocalAABB, shield.PositionComp.WorldMatrix.GetOrientation());
			myObb.Center = shield.PositionComp.WorldVolume.Center;
			var obbCheck = myObb.Intersects(ref line);

			var testDir = line.From - line.To;
			testDir.Normalize();
			var ray = new RayD(line.From, -testDir);
			var sphereCheck = worldSphere.Intersects(ray);

			var obb = obbCheck ?? 0;
			var sphere = sphereCheck ?? 0;
			double furthestHit;

			if(obb <= 0 && sphere <= 0){
				
				furthestHit = 0;
				
			}else if(obb > sphere){
				
				furthestHit = obb;
				
			}else{
				
				furthestHit = sphere;
				
			}
			var hitPos = line.From + testDir * -furthestHit;
			
			/*
			var parent = MyAPIGateway.Entities.GetEntityById(long.Parse(shield.Name));
			var cubeBlock = (MyCubeBlock)parent;
			var block = (IMySlimBlock)cubeBlock.SlimBlock;

			if(block == null){
				
				return null;
				
			} 
			
			block.DoDamage(damage, MyStringHash.GetOrCompute(effect.ToString()), true, null, attackerId);
			shield.Render.ColorMaskHsv = hitPos;
			
			if(effect.ToString() == "bypass"){
				
				return null;
				
			}
			*/
			return hitPos;
		}
		
		public static float CalculateTractorBeamEffect(IMyCubeGrid cubeGrid, float forceToReduce){
			
			float amountToReduce = 0;
			//Get Grid Group
			
			//Loop Group and Get
			
			return amountToReduce;
			
		}
		
		public static void DisableGridShields(IMyCubeGrid cubeGrid, long owner){
			
			try{
				
				var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
				var blockList = new List<IMyFunctionalBlock>();
				var damageBlockList = new List<IMyFunctionalBlock>();
				gts.GetBlocksOfType<IMyFunctionalBlock>(blockList);
				
				foreach(var targetBlock in blockList){
					
					if(targetBlock.IsFunctional == false){
						
						continue;
						
					}
					
					if(ShieldBlockSubtypeNames.Count == 0){
						
						//Cython's Energy Shields
						ShieldBlockSubtypeNames.Add("LargeShipSmallShieldGeneratorBase");
						ShieldBlockSubtypeNames.Add("LargeShipLargeShieldGeneratorBase");
						ShieldBlockSubtypeNames.Add("SmallShipSmallShieldGeneratorBase");
						ShieldBlockSubtypeNames.Add("SmallShipMicroShieldGeneratorBase");
						ShieldBlockSubtypeNames.Add("ShieldCapacitor");
						ShieldBlockSubtypeNames.Add("ShieldFluxCoil");
						
						//DarkStar's Defense Shields
						ShieldBlockSubtypeNames.Add("DSControlTable");
						ShieldBlockSubtypeNames.Add("DSControlLarge");
						ShieldBlockSubtypeNames.Add("DSControlSmall");
						ShieldBlockSubtypeNames.Add("DSControlLCD");
						ShieldBlockSubtypeNames.Add("DSSupergen");
						ShieldBlockSubtypeNames.Add("EmitterL");
						ShieldBlockSubtypeNames.Add("EmitterS");
						ShieldBlockSubtypeNames.Add("EmitterST");
						ShieldBlockSubtypeNames.Add("LargeShieldModulator");
						ShieldBlockSubtypeNames.Add("SmallShieldModulator");
						ShieldBlockSubtypeNames.Add("LargeEnhancer");
						ShieldBlockSubtypeNames.Add("SmallEnhancer");
						ShieldBlockSubtypeNames.Add("EmitterLA");
						ShieldBlockSubtypeNames.Add("EmitterSA");
						
						var corruptFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("CORRUPT");
						
						if(corruptFaction != null){
							
							CorruptFounderId = corruptFaction.FounderId;
							
						}
						
					}
					
					if(ShieldBlockSubtypeNames.Contains(targetBlock.SlimBlock.BlockDefinition.Id.SubtypeName) == true){
						
						targetBlock.Enabled = false;
						damageBlockList.Add(targetBlock);
						
					}
					
					if(targetBlock as IMyBeacon != null && targetBlock.OwnerId == CorruptFounderId && CorruptFounderId != 0 && targetBlock.CustomName.Contains("Shield") == true){
						
						targetBlock.Enabled = false;
						damageBlockList.Add(targetBlock);
					
					}
					
					if(targetBlock.SlimBlock.BlockDefinition.Id.SubtypeName.StartsWith("PhaseShiftArmor_") == true){
						
						targetBlock.Enabled = false;
						damageBlockList.Add(targetBlock);
						
					}
					
				}
				
				foreach(var targetBlock in damageBlockList){
					
					var tbob = targetBlock.SlimBlock.GetObjectBuilder();
					var damageModifier = tbob.BlockGeneralDamageModifier;
					var damageAmount = targetBlock.SlimBlock.Integrity * 0.8f;
					var finalDamage = damageAmount / damageModifier;
					targetBlock.SlimBlock.DoDamage(finalDamage, MyStringHash.GetOrCompute("bypass"), true, null, owner);
					
				}
				
			}catch(Exception exc){
				
				
				
			}
			
		}
		
		public static void DisableGridRandomBlocks(IMyCubeGrid cubeGrid, int blocksTotal){
			
			if(blocksTotal <= 0){
				
				return;
				
			}
			
			try{
				
				var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
				var blockList = new List<IMyFunctionalBlock>();
				gts.GetBlocksOfType<IMyFunctionalBlock>(blockList);
				
				var totalBlocksAffected = 0;
				
				while(totalBlocksAffected < blocksTotal){
					
					if(blockList.Count == 0){
						
						break;
						
					}
					
					var randIndex = Utilities.Rnd.Next(0, blockList.Count);
					
					if(blockList[randIndex].IsFunctional == false || blockList[randIndex].IsWorking == false){
						
						blockList.RemoveAt(randIndex);
						continue;
						
					}
					
					blockList[randIndex].Enabled = false;
					totalBlocksAffected++;
					blockList.RemoveAt(randIndex);

				}
				
			}catch(Exception exc){
				
				
				
			}
			
		}
		
		public static IMySlimBlock GetClosestBlock(Vector3D fromCoords, Vector3D coords, IMyCubeGrid cubeGrid){
			
			IMySlimBlock closestBlock = null;
			double closestDistance = 0;
			Vector3D closestPosition = Vector3D.Zero;
			
			var directionToGridCenter = Vector3D.Normalize(cubeGrid.GetPosition() - coords);
			var toCoords = directionToGridCenter * 1500 + coords;
			var cellHits = new List<Vector3I>();
			cubeGrid.RayCastCells(coords, toCoords, cellHits);
			
			foreach(var cell in cellHits){
				
				var thisBlock = cubeGrid.GetCubeBlock(cell);
				
				if(thisBlock == null){
					
					continue;
					
				}
				
				var thisBlockPos = Vector3D.Zero;
				thisBlock.ComputeWorldCenter(out thisBlockPos);
				var distance = Vector3D.Distance(coords, thisBlockPos);
				
				if(closestBlock == null){
					
					closestBlock = thisBlock;
					closestDistance = distance;
					closestPosition = thisBlockPos;
					continue;
					
				}
				
				if(distance < closestDistance){
					
					closestBlock = thisBlock;
					closestDistance = distance;
					closestPosition = thisBlockPos;
					
				}
				
			}
			
			if(closestBlock != null){
				
				return closestBlock;
				
			}
			
			var blockList = new List<IMySlimBlock>();
			cubeGrid.GetBlocks(blockList);
			
			foreach(var block in blockList){
				
				var thisBlockPos = Vector3D.Zero;
				block.ComputeWorldCenter(out thisBlockPos);
				var distance = Vector3D.Distance(coords, thisBlockPos);
				
				if(closestBlock == null){
					
					closestBlock = block;
					closestDistance = distance;
					closestPosition = thisBlockPos;
					continue;
					
				}
				
				if(distance < closestDistance){
					
					closestBlock = block;
					closestDistance = distance;
					closestPosition = thisBlockPos;
					
				}
				
			}
			
			return closestBlock;
			
		}
		
		public static void HackTargetBlocks(IMyCubeGrid cubeGrid, int numberOfBlocks, long newOwner){
			
			try{
				
				var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
				var blockList = new List<IMyTerminalBlock>();
				gts.GetBlocksOfType<IMyTerminalBlock>(blockList);
				int blocksConverted = 0;
				
				while(blockList.Count > 0){
					
					var randIndex = Rnd.Next(0, blockList.Count);
					var block = blockList[randIndex];
					blockList.RemoveAt(randIndex);
					
					if(block.OwnerId != 0 && block.OwnerId != newOwner){
						
						var blockEntity = block as IMyEntity;
						var cubeBlock = blockEntity as MyCubeBlock;
						cubeBlock.ChangeBlockOwnerRequest(newOwner, MyOwnershipShareModeEnum.Faction);
						blocksConverted++;
						
					}
					
					if(blocksConverted >= numberOfBlocks){
						
						break;
						
					}
					
				}
				
			}catch(Exception exc){
				
				
				
			}
			
		}
		
		public static void InhibitJumpDrives(IMyCubeGrid cubeGrid, float amountToInhibit, bool splitEvenlyAcrossDrives = true){
			
			try{
				
				var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
				var blockList = new List<IMyJumpDrive>();
				gts.GetBlocksOfType<IMyJumpDrive>(blockList);
				
				if(blockList.Count == 0){
					
					return;
					
				}
				
				for(int i = blockList.Count - 1; i >= 0; i--){
					
					if(blockList[i].IsWorking == false || blockList[i].IsFunctional == false){
						
						blockList.RemoveAt(i);
						continue;
						
					}
					
					if(blockList[i].CurrentStoredPower == 0){
						
						blockList.RemoveAt(i);
						continue;
						
					}
					
				}
				
				if(blockList.Count == 0){
					
					return;
					
				}
				
				float inhibitReducer = amountToInhibit;
				
				if(splitEvenlyAcrossDrives == true){
					
					inhibitReducer = amountToInhibit / (float)blockList.Count;
					
				}
				
				foreach(var block in blockList){
					
					//Test if needs sync...
					block.CurrentStoredPower -= inhibitReducer;
					
					if(block.CurrentStoredPower < 0){
						
						block.CurrentStoredPower = 0;
						
					}
					
				}
				
			}catch(Exception exc){
				
				
				
			}
			
		}
		
		public static bool IsOwnerNPC(long owner){
			
			if(owner == 0){
				
				return false;
				
			}
			
			var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);
			
			if(faction != null){
				
				if(faction.IsEveryoneNpc() == true){
					
					return true;
					
				}else{
					
					return false;
					
				}
				
			}
			
			var checkpoint = MyAPIGateway.Session.GetCheckpoint(MyAPIGateway.Session.Name);
			
			if(checkpoint == null){
				
				return false;
				
			}
			
			if(checkpoint.NonPlayerIdentities.Contains(owner) == true){
				
				return true;
				
			}
			
			return false;
			
		}
		
		public static double RandomDouble(double minValue, double maxValue){
			
			var minInflatedValue = (float)Math.Round(minValue, 3) * 1000;
			var maxInflatedValue = (float)Math.Round(maxValue, 3) * 1000;
			var randomValue = (float)Utilities.Rnd.Next((int)minInflatedValue, (int)maxInflatedValue) / 1000;
			return randomValue;
			
		}
		
		public static float RandomFloat(float minValue, float maxValue){
			
			var minInflatedValue = (float)Math.Round(minValue, 3) * 1000;
			var maxInflatedValue = (float)Math.Round(maxValue, 3) * 1000;
			var randomValue = (float)Utilities.Rnd.Next((int)minInflatedValue, (int)maxInflatedValue) / 1000;
			return randomValue;
			
		}
		
		public static float UpgradePowerRequirement(float upgradeModifier, float powerInput){
			
			return upgradeModifier * powerInput;
			
		}
		
		public static float UpgradedValueMultiply(float upgradeModifier, float powerInput){
			
			return upgradeModifier * powerInput;
			
		}
		
		public static float UpgradedValueAdd(float upgradeModifier, float powerInput){
			
			return upgradeModifier + powerInput;
			
		}
		
		public static int UpgradedValueAddInt(int upgradeModifier, int powerInput){
			
			return upgradeModifier + powerInput;
			
		}
		
		public static int UpgradeExplosionDamageValue(float upgradeModifier, float powerInput){
			
			var roundedValue = (int)Math.Floor(upgradeModifier * powerInput);
			return roundedValue;
			
		}
		
	}
	
}
