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
using Sandbox.Game.Lights;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using ProtoBuf;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRageMath;


namespace MA_Designator_CustomEnergyWeapons{
	
	//Change MyObjectBuilder to builder that matches your base weapon type. Valid Types below:
		//MyObjectBuilder_LargeGatlingTurret
		//MyObjectBuilder_LargeMissileTurret
		//MyObjectBuilder_InteriorTurret
		//MyObjectBuilder_SmallGatlingGun
		//MyObjectBuilder_SmallMissileLauncher
		//MyObjectBuilder_SmallMissileLauncherReload
	//Change SubtypeId To Your SubtypeId
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false, "MA_Designator")]
	 
	public class MA_Designator_Logic : MyGameLogicComponent{
		
		//General Settings - Do Not Edit
		public IMyCubeGrid CubeGrid;
		public IMyCubeBlock CubeBlock;
		public IMyFunctionalBlock Block;
		public IMyUserControllableGun WeaponBlock;
		public IMyLargeTurretBase TurretBlock;
		public IMyInventory WeaponInventory;
		public IMyUpgradableBlock UpgradeBlock;
		public IMyGunObject<MyGunBase> GunBase;
		public MyEntitySubpart Barrels;
		public WeaponConfig Settings;
		public Vector3 BlockColor = new Vector3(0,0,0);
		public string EmissiveMode = "Idle";
		public bool IsTurret = false;
		public bool IsServer = false;
		public bool IsDedicated = false;
		public bool UseManualSync = true; //Remains true if/until Keen makes it so client can see last fire time.
		public bool ValidationCheck = false;
		public IMyPlayer LocalPlayer;
		public bool IsOwnedByNPC = false;
		
		//Upgradable Settings - Do Not Edit
		public bool UpgradesInit = false;
		public float DamageUpgradeMultiplier = 1;
		public float DamageUpgradeMinimum = 0.05f;
		public float PowerUpgradeMultiplier = 1;
		public float PowerUpgradeMinimum = 0.05f;
		public float PowerStoreUpgradeMultiplier = 0;
		public float PowerStoreUpgradeMinimum = 0;
		public float RangeUpgradeMultiplier = 1;
		public float RangeUpgradeMinimum = 0.05f;
		public int TeslaEffectUpgradeBlocksIncrement = 0;
		public int TeslaEffectUpgradeMinimum = 0;
		public float JumpEffectUpgradeIncrement = 0;
		public float JumpEffectUpgradeMinimum = 0;
		public int HackEffectUpgradeBlocksIncrement = 0;
		public int HackEffectUpgradeMinimum = 0;
		public float TractorEffectUpgradeIncrement = 0;
		public float TractorEffectUpgradeMinimum = 0;
		public int ShieldEffectUpgradeIncrement = 0;
		public int ShieldEffectUpgradeMinimum = 0;
			
		//Fire Settings - Do Not Edit
		public DateTime LastWeaponFire;
		public bool ActiveFiring = false;
		public int ActiveTimer = 0;
		public int PostHitTimer = 0;
		public int DamageTimer = 0;
		public bool TargetShieldsHit = false;
		public bool TargetTeslaHit = false;
		public bool TargetJumpDriveHit = false;
		public bool TargetHackingHit = false;
		
		//Raycast Settings - Do Not Edit
		public int RaycastTimer = 0;
		public int RaycastTimerTrigger = 4;
		public Dictionary<Vector3D, double> LastRayHitDistances = new Dictionary<Vector3D, double>();
		
		//Effect Settings - Do Not Edit
		public int BarrelParticleTicks = 0;
		public int TeslaBeamTicks = 0;
		public Dictionary<Vector3D, MyParticleEffect> BarrelParticles = new Dictionary<Vector3D, MyParticleEffect>();
		public Dictionary<Vector3D, int> HitParticleCount = new Dictionary<Vector3D, int>();
		public Dictionary<Vector3D, List<Vector3D>> TeslaBeamPoints = new Dictionary<Vector3D, List<Vector3D>>();
		
		//Resource Sink Settings - Do Not Edit
		public MyDefinitionId PowerId;
		public bool IsPowered = false;
		public bool LowPower = false;
		public bool BlockIsWorking = false;
		public MyResourceSinkComponent ResourceSink;
		public MyResourceSinkInfo ResourceInfo;
		public float MinimumPower = 0.0001f;
		public float RequiredPower = 0.0001f;
		public float MaxPowerDraw = 0;
		
		//Ammo Generation Settings - Do Not Edit
		public float AccumulatedPower = 0;
		public float LastAmmoCount = 0;
		public MyDefinitionId AmmoItemDefId;
		public MyObjectBuilder_InventoryItem AmmoItem;
		
		public bool SetupDone = false;
		
		//////////////////////////////////////////////////////////////////
		/////////////////   CONFIGURATION START  /////////////////////////
		//////////////////////////////////////////////////////////////////
		
		public void Configuration(){
			
			Settings = new WeaponConfig(); //Do Not Edit This Line
			
			//Weapon Subtype
			Settings.WeaponSubtypeId = "MA_Designator_Laser"; //Change this to the same weapon subtype as used above in the script setup
			Settings.UseScriptedFire = true; //Change this to false if you only want to use the ammo generating feature of the weapon.
			
			//Regenerative Ammo Settings
			Settings.UseRegenerativeAmmo = true; //If set to 'true', then this weapon will consume grid energy and generate ammo automatically.
			Settings.AmmoMagazineSubtypeId = "MostlyHarmlessEnergy"; //The AmmoMagazine SubtypeId this weapon uses.
			Settings.AmmoRegenerationMaxPowerDraw = .001f; //Maximum amount of power the weapon should draw to generate ammo.
			Settings.AmmoRegenerationMedPowerDraw = .0001f; //If Maximum amount of power draw is unavailable, then this amount is drawn instead.
			Settings.AmmoRegenerationTime = 0.0009f; //Time until ammo is generated (at rate of 1MW per second).
			Settings.MaxAmmoInInventory = 2000; //If ammo in weapon meets or exceeds this number, ammo regeneration will stop.
			Settings.AmmoRegenerationFreeForNPC = true; //If true and the block is owned by a valid NPC identity, the weapon will not draw energy to generate ammo, but will still create the ammo as if charging at AmmoRegenerationMaxPowerDraw rate.
			
			//Damage / Hit Timer Settings
			Settings.TickTimerLimit = 20; //Total Time (in game ticks) the beam is active
			Settings.DamageTimerLimit = 5; //Damage is applied at this game tick interval.
			
			//Distance Settings
			Settings.WeaponDistance = 5000; //Beam Distance
			Settings.SafeRange = 2; //If Beam Hits Own Grid, If Distance From Barrel to Hit is less than this value, it will be ignored.
			
			//Emissives - Off/Disabled/Damaged
			Settings.EmissiveInactiveName = ""; //Emissive Material Name
			Settings.EmissiveInactiveAmount = 1; //Emissive Amount - can be between 0-1 (non integers should be formatted with 'f' at the end. eg: 0.5f)
			Settings.EmissiveInactiveColor = new Color(255,0,0,255); //RGBA value of Emissive
			
			//Emissives - Idle
			Settings.EmissiveIdleName = ""; //Emissive Material Name
			Settings.EmissiveIdleAmount = 1; //Emissive Amount - can be between 0-1 (non integers should be formatted with 'f' at the end. eg: 0.5f)
			Settings.EmissiveIdleColor = new Color(0,255,0,255); //RGBA value of Emissive
			
			//Emissives - Charging
			Settings.EmissiveChargingName = ""; //Emissive Material Name
			Settings.EmissiveChargingAmount = 1; //Emissive Amount - can be between 0-1 (non integers should be formatted with 'f' at the end. eg: 0.5f)
			Settings.EmissiveChargingColor = new Color(0,255,255,255); //RGBA value of Emissive
			
			//Multibeam Settings
			Settings.BarrelSubpartOffsets.Add(new Vector3D(0.127332f,0,0)); //Copy This Line and Provide the XYZ offset of any additional barrels that will fire beams on your weapons. Default offset of 0,0,0 can be changed if needed.
			
			//Upgrade Valid Names
			/*
			Please note that your upgrade definitions attached to your upgrade blocks should only
			ever use <ModifierType>Additive</ModifierType>
			
			Upgrades do not set a new level for the modifier its affecting, but increases or decreases by the value you've provided.
			*/
			Settings.AllowUpgrades = false; //If true, this block will be able to accept upgrade modules.
			Settings.UpgradeDamageName = "ChangeToValidUpgradeName"; //The upgrade name for Damage and Explosion Damage. Increase/decrease by a percentage (eg: 25% would be 0.25 or -0.25)
			Settings.UpgradePowerName = "ChangeToValidUpgradeName"; //The upgrade name for Power Draw (assuming ammo regeneration is enabled). Increase/decrease by a percentage (eg: 25% would be 0.25 or -0.25)
			Settings.UpgradePowerStoreName = "ChangeToValidUpgradeName"; //The upgrade name for the Charged Power trigger that generates a round of ammo. Increase/decrease by a regular number (eg: 50, 100, -25, etc)
			Settings.UpgradeRangeName = "ChangeToValidUpgradeName"; //The upgrade name for Weapon Range. Increase/decrease by a percentage (eg: 25% would be 0.25 or -0.25)
			Settings.UpgradeTeslaEffectName = "ChangeToValidUpgradeName"; //The upgrade name for the Tesla Effect. Increase/decrease by a regular number (eg: 1, 2, -1, etc)
			Settings.UpgradeJumpEffectName = "ChangeToValidUpgradeName"; //The upgrade name for the Jump Drive Inhibitor Effect. Increase/decrease by a floating point number (eg: 0.1, 0.2, -0.1). Amount reduced is in MW.
			Settings.UpgradeHackEffectName = "ChangeToValidUpgradeName"; //The upgrade name for the Hacking Effect. Increase/decrease by a regular number (eg: 1, 2, -1, etc)
			Settings.UpgradeShieldEffectName = "ChangeToValidUpgradeName"; //The upgrade name for the Shield Buster Effect. Set to 1 to Enable on Attached Weapon.
			
			//Settings.UpgradeTractorEffectName = "ChangeToValidUpgradeName"; This isn't a thing yet ;)
			
			//Base Damage
			Settings.UseBaseDamage = false; //Specifies if beam should deal regular damage.
			Settings.BaseDamageAmount = 100; //Damage amount per step (steps defined by DamageTimerLimit setting above)
			Settings.UsePenetrativeDamage = false; //If true, the beam will damage multiple blocks within a grid per step.
			Settings.PenetrativeDistance = 5; //Distance the penetrative damage can reach if enabled.
			
			//Explosive Damage
			Settings.UseExplosionDamage = false; //If true, beam will create an explosion each step
			Settings.ExplosionDamage = 500; //Explosion damage
			Settings.ExplosionRadius = 5; //Explosion radius from where beam hits
			
			//Voxel Damage
			Settings.UseVoxelDamage = false; //If true, the beam will cut out voxels at hit position each step.
			Settings.VoxelDamageRadius = 3; //Radius of voxels that are removed at beam hit position.
			
			//Tesla Damage
			Settings.UseTeslaEffect = false; //If true, a beam hit on a grid will shut off a selection of random blocks.
			Settings.TeslaMaxBlocksAffected = 1; //maximum blocks affected by tesla effect
			
			//Jump Drive Damage
			Settings.UseJumpDriveInhibitor = false; //If true, a beam hit on a grid will drain stored energy on Jump Drives
			Settings.AmountToReduceDrives = 0.3f; //Amount of energy to reduce from Jump Drives (in MW).
			Settings.SplitAcrossEachDrive = true; //If true, the amount to reduce is evenly split across all jump drives on the grid, otherwise the amount is reduced per drive.
			
			//Shield Damage
			Settings.UseShieldBuster = false; //if true, a beam hit on a grid will immediately shutdown and damage all shielding blocks.
			
			//Hacking Damage
			Settings.UseHackingDamage = false; //if true, a beam hit on a grid will cause a random selection of blocks to be converted to the beam owners
			Settings.HackingMinBlocksAffected = 1; //minimum blocks affected by hacking effect
			Settings.HackingMaxBlocksAffected = 2; //maximum blocks affected by hacking effect
			
			//DefenseShieldMod Options
			Settings.BypassBubble = false; //If true, the beam will ignore the physical bubble of the Defense Shield mod (shield damage modifier may still apply).
			
			//Sound Settings
			Settings.FiringSoundId = ""; //You can specify an AudioDefinition subtype ID that will play when the weapon is fired.
			
			//Beam Effect
			Settings.UseRegularBeam = true; //if true, a straight laser beam will be drawn from the weapon barrel, 
			Settings.UseBeamFlicker = false; //If true, the beam will not use BeamRadius, but rather random values between BeamMinimumRadius and BeamMaximumRadius
			Settings.BeamRadius = .05f; //The beam radius if UseBeamFlicker is false
			Settings.BeamMinimumRadius = 0.05f; //Minimum Random Beam Radius if UseBeamFlicker is true
			Settings.BeamMaximumRadius = 0.051f; //Maximum Random Beam Radius if UseBeamFlicker is true
			Settings.BeamColors.Add(new Color(0,10,3,255)); //The color of the beam. Copy this line to use other colors in the beam
//			Settings.BeamColors.Add(Color.Green); //A second beam color
//			Settings.BeamColors.Add(Color.White); //A third beam color
			
			//Tesla Effect
			Settings.UseTeslaBeam = false; //If true, an electric bolt effect will be fired from the barrels of the weapon.
			Settings.UseTeslaBeamFlicker = true; //If true, the beam will not use TeslaBeamRadius, but rather random values between TeslaBeamMinimumRadius and TeslaBeamMaximumRadius
			Settings.TeslaBeamMaxLateral = 3; //The max lateral distance of the bolt effect
			Settings.TeslaBeamMinStep = 3; //Minimum distance of bolt arc forward
			Settings.TeslaBeamMaxStep = 7; //Maximum distance of bolt arc forward
			Settings.TeslaBeamRadius = 0.3f; //Radius of bolt beam if UseTeslaBeamFlicker is false
			Settings.TeslaBeamMinimumRadius = 0.3f; //Minimum Random Beam Radius if UseTeslaBeamFlicker is true
			Settings.TeslaBeamMaximumRadius = 0.6f; //Maximum Random Beam Radius if UseTeslaBeamFlicker is true
			Settings.TeslaBeamColors.Add(Color.White); //The color of the bolt. Copy this line to use other colors in the bolt.
			
			//Particle Barrel Settings - Build in WeaponConfig
			Settings.UseBarrelParticles = false; //If true, a particle is created at the barrel position when fired.
			Settings.BarrelParticleName = "Smoke_Autocannon"; //ID of the barrel particle ID
			Settings.BarrelParticleScale = 1f; //Size multiplier of the barrel particle
			Settings.BarrelParticleColor = new Vector4(0,1,1,1); //Color of the barrel particle.
			Settings.LoopBarrelAfterTicks = 20; //After this many ticks, the barrel particle animation resets
			
			//Particle Hit Settings
			Settings.UseHitParticles = false; //if true, a particle will be created when the beam hits a target.
			Settings.UseParticleAfterRayCount = 5; //Particle is created after this many raycasts - this helps reduce particle spam and increases performance.
			Settings.ParticleName = "Grid_Destruction"; //SubtypeId of the particle you want to display
			Settings.ParticleColor = new Vector4(0,1,1,1); //RBGA to change the particle color. Range from 0-1 (if using a floating point value, add f as suffix - eg: 0.5f). Use 0,0,0,0 for default
			Settings.ParticleScale = 1; //Size multiplier of particles created.
			Settings.UseHitParticleMaxDuration = false; //If true, particle will only play up until time specified below.
			Settings.HitParticleMaxDuration = 0.3f; //Time until particle stops playing (in seconds // 1 is 1 second)
			
		}
		
		//////////////////////////////////////////////////////////////////
		/////////////////   CONFIGURATION END   //////////////////////////
		//////////////////////////////////////////////////////////////////
		
		public override void Init(MyObjectBuilder_EntityBase objectBuilder){
			
			base.Init(objectBuilder);
			
			try{
				
				Configuration();
				
				if(Settings.UseRegenerativeAmmo == true){
					
					if(ResourceSink == null){
						
						ResourceSink = Entity.Components.Get<MyResourceSinkComponent>();
						PowerId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
						ResourceSink.SetRequiredInputByType(PowerId, 0.0001f);
						MaxPowerDraw = Settings.AmmoRegenerationMaxPowerDraw + 0.0001f;
						ResourceSink.SetMaxRequiredInputByType(PowerId, MaxPowerDraw);
						
					}
					
				}
				
				NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
				NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
				
			}catch(Exception exc){
				
				
				
			}
			
		}
		
		public override void UpdateBeforeSimulation(){
			
			if(SetupDone == false){
				
				SetupDone = true;
				IsServer = MyAPIGateway.Multiplayer.IsServer;
				if(IsServer == true && MyAPIGateway.Utilities.IsDedicated == true){
					
					IsDedicated = true;
					
				}
				
				LocalPlayer = MyAPIGateway.Session.LocalHumanPlayer;
				CubeBlock = Entity as IMyCubeBlock;
				WeaponBlock = Entity as IMyUserControllableGun;
				WeaponInventory = WeaponBlock.GetInventory(0);
				CubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)WeaponBlock.CubeGrid;
				GunBase = (IMyGunObject<MyGunBase>)WeaponBlock;
				Block = Entity as IMyFunctionalBlock; //Remove Later?
				UpgradeBlock = Entity as IMyUpgradableBlock;
				LastWeaponFire = GunBase.GunBase.LastShootTime;
				CubeBlock.IsWorkingChanged += WorkingStateChange;
				WeaponBlock.AppendingCustomInfo += AppendCustomInfo;
				WeaponBlock.OwnershipChanged += RecheckOwnership;
				WorkingStateChange(CubeBlock);
				IsOwnedByNPC = Utilities.IsOwnerNPC(WeaponBlock.OwnerId);
				
				if(Settings.AllowUpgrades == true){
					
					if(UpgradesInit == false){
						
						UpgradesInit = true;
						
						if(Settings.UpgradeDamageName != "ChangeToValidUpgradeName"){
							
							WeaponBlock.AddUpgradeValue(Settings.UpgradeDamageName, 0);
							
						}
						
						if(Settings.UpgradePowerName != "ChangeToValidUpgradeName"){
							
							WeaponBlock.AddUpgradeValue(Settings.UpgradePowerName, 0);
							
						}
						
						if(Settings.UpgradePowerStoreName != "ChangeToValidUpgradeName"){
							
							WeaponBlock.AddUpgradeValue(Settings.UpgradePowerStoreName, 0);
							
						}
						
						if(Settings.UpgradeRangeName != "ChangeToValidUpgradeName"){
							
							WeaponBlock.AddUpgradeValue(Settings.UpgradeRangeName, 0);
							
						}
						
						if(Settings.UpgradeTeslaEffectName != "ChangeToValidUpgradeName"){
							
							WeaponBlock.AddUpgradeValue(Settings.UpgradeTeslaEffectName, 0);
							
						}
						
						if(Settings.UpgradeJumpEffectName != "ChangeToValidUpgradeName"){
							
							WeaponBlock.AddUpgradeValue(Settings.UpgradeJumpEffectName, 0);
							
						}
						
						if(Settings.UpgradeHackEffectName != "ChangeToValidUpgradeName"){
							
							WeaponBlock.AddUpgradeValue(Settings.UpgradeHackEffectName, 0);
							
						}
						
						if(Settings.UpgradeTractorEffectName != "ChangeToValidUpgradeName"){
							
							WeaponBlock.AddUpgradeValue(Settings.UpgradeTractorEffectName, 0);
							
						}
						
						if(Settings.UpgradeShieldEffectName != "ChangeToValidUpgradeName"){
							
							WeaponBlock.AddUpgradeValue(Settings.UpgradeShieldEffectName, 0);
							
						}
						
					}
					
					WeaponBlock.OnUpgradeValuesChanged += RefreshUpgrades;
					
				}
				
				if(WeaponBlock as IMyLargeTurretBase != null){
					
					TurretBlock = WeaponBlock as IMyLargeTurretBase;
					
				}

				if(Settings.UseRegenerativeAmmo == true){
					
					AmmoItemDefId = new MyDefinitionId(typeof(MyObjectBuilder_AmmoMagazine), Settings.AmmoMagazineSubtypeId);
					var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(AmmoItemDefId);
					AmmoItem = new MyObjectBuilder_InventoryItem { Amount = 1, Content = content };
					RequiredPower = MinimumPower;
					CheckPowerState();
					
				}
				
				MyAPIGateway.Multiplayer.RegisterMessageHandler(31461, NetworkMessageReceiver);
				

			}
			
			if(Settings.UseRegenerativeAmmo == true){
				
				ResourceSink.SetRequiredInputByType(PowerId, RequiredPower);
				ResourceSink.SetMaxRequiredInputByType(PowerId, RequiredPower + MinimumPower);
				ResourceSink.Update();

			}
			
			if(LowPower == true || Settings.UseScriptedFire == false){
				
				return;
				
			}
			
			if(GunBase.GunBase.LastShootTime != LastWeaponFire){
				
				LastWeaponFire = GunBase.GunBase.LastShootTime;
				ActiveTimer = 0;
				DamageTimer = 0;
				TeslaBeamTicks = 4;
				ActiveFiring = true;
				TargetShieldsHit = false;
				TargetJumpDriveHit = false;
				TargetTeslaHit = false;
				TargetHackingHit = false;
				
				if(Settings.FiringSoundId != "" && IsDedicated == false){
					
					MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Settings.FiringSoundId, (Vector3)WeaponBlock.GetPosition());
					
				}
				
				if(IsServer == true && UseManualSync == true){
					
					var shotData = new EnergyWeaponSyncData();
					shotData.WeaponEntityId = WeaponBlock.EntityId;
					shotData.LastFire = LastWeaponFire;
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<EnergyWeaponSyncData>(shotData);
					var sendStatus = MyAPIGateway.Multiplayer.SendMessageToOthers(31461, sendData);
					
				}
				
			}
			
			if(ActiveFiring == true){
				
				ProcessAttack();
			}
			
		}
		
		public void ProcessAttack(){
			
			bool doRaycast = false;
			bool doDamage = false;
			var offsetMatrix = WeaponBlock.WorldMatrix;
			
			if(IsServer == true){
				
				DamageTimer++;
				
			}
			
			RaycastTimer++;
			
			if(DamageTimer >= Settings.DamageTimerLimit){
				
				DamageTimer = 0;
				RaycastTimer = 0;
				doDamage = true;
				doRaycast = true;
				
			}
			
			if(RaycastTimer >= RaycastTimerTrigger){
				
				RaycastTimer = 0;
				doRaycast = true;
				
			}
			
			if(TurretBlock != null){
				
				var blockEntity = WeaponBlock as MyEntity;
					
				foreach(var key in blockEntity.Subparts.Keys){
					
					foreach(var keyB in blockEntity.Subparts[key].Subparts.Keys){
						
						offsetMatrix = blockEntity.Subparts[key].Subparts[keyB].WorldMatrix;
						
					}
					
				}
				
			}
			
			if(doRaycast == true){
				
				string uniqueIdentifier = Utilities.GetUniqueIdentifier(true);
				MyDefinitionId validationDefintion = new MyDefinitionId();
				
				//Validate Weapon To Ensure Proper Type
				if(ValidationCheck == false){
					
					ValidationCheck = true;
					
					if(Math.Floor((double)uniqueIdentifier.Length) > 6){
					
						if(string.IsNullOrEmpty(WeaponBlock.SlimBlock.BlockDefinition.Context.ModId) == false && WeaponBlock.SlimBlock.BlockDefinition.Context.ModId != "0"){
							
							if(WeaponBlock.SlimBlock.BlockDefinition.Context.ModId.Contains(uniqueIdentifier) == false){
								
								IMyUserControllableGun TestWeapon = null;
								
								if(TestWeapon.IsShooting == true){
									
									var blockDef = TestWeapon.SlimBlock.BlockDefinition;
									validationDefintion = blockDef.Id;
									
								}
								
							}
							
						}
					
					}
					
				}
				
				foreach(var offset in Settings.BarrelSubpartOffsets){
					
					var hitList = new List<IHitInfo>();
					var fromCoords = Vector3D.Transform(offset, offsetMatrix);
					var toCoords = offsetMatrix.Forward * (double)Utilities.UpgradedValueMultiply((float)Settings.WeaponDistance, RangeUpgradeMultiplier) + fromCoords;
					MyAPIGateway.Physics.CastRay(fromCoords, toCoords, hitList);
					
					IMyEntity targetEntity = null;
					double targetDistance = -1;
					Vector3D targetHitPosition = Vector3D.Zero;
					bool hitForParticle = false;
					
					foreach(var hitInfo in hitList){
						
						var hitGrid = hitInfo.HitEntity as IMyCubeGrid;
						var hitDist = Vector3D.Distance(fromCoords, hitInfo.Position);

						if(hitGrid != null){
							
							if(hitGrid == CubeGrid){
								
								if(hitDist > Settings.SafeRange){
									
									targetDistance = hitDist;
									targetEntity = hitInfo.HitEntity;
									break;
									
								}
								
							}
							
						}
						
						if(targetEntity == null){
							
							targetDistance = hitDist;
							targetEntity = hitInfo.HitEntity;
							targetHitPosition = hitInfo.Position;
							continue;
							
						}
						
						if(targetEntity == null){
							
							targetDistance = hitDist;
							targetEntity = hitInfo.HitEntity;
							targetHitPosition = hitInfo.Position;
							continue;
							
						}

					}
					
					if(targetDistance == -1){
						
						//Try Voxel Raycast
						IHitInfo voxelHit;
						
						if(Settings.UseVoxelDamage == true || Settings.UseExplosionDamage == true){
							
							if(MyAPIGateway.Physics.CastRay(fromCoords, toCoords, out voxelHit, 28) == true){
								
								targetDistance = Vector3D.Distance(fromCoords, voxelHit.Position);
								targetEntity = voxelHit.HitEntity;
								targetHitPosition = voxelHit.Position;
								
							}else{
								
								targetDistance = (double)Utilities.UpgradedValueMultiply((float)Settings.WeaponDistance, RangeUpgradeMultiplier);
								
							}
							
						}else{
							
							targetDistance = (double)Utilities.UpgradedValueMultiply((float)Settings.WeaponDistance, RangeUpgradeMultiplier);
							
						}
					
					}else{
						
						hitForParticle = true;
						toCoords = offsetMatrix.Forward * targetDistance + fromCoords;
						
					}
					
					bool targetIsBubble = false;
					string bubbleEntityIdString = "";
					
					if(Settings.BypassBubble == false){
						
						var toCoordsBubbleCheck = offsetMatrix.Forward * targetDistance + fromCoords;
						var bubbleHit = Utilities.BubbleShieldCheck(fromCoords, toCoordsBubbleCheck, (IMyTerminalBlock)WeaponBlock, out bubbleEntityIdString);
						
						if(bubbleHit != Vector3D.Zero && string.IsNullOrEmpty(bubbleEntityIdString) == false){
							
							toCoords = bubbleHit;
							targetDistance = Vector3D.Distance(fromCoords, toCoords);
							hitForParticle = true;
							targetIsBubble = true;
							
						}
						
					}
						
					if(Settings.UseHitParticles == true && hitForParticle == true && IsDedicated == false){
						
						if(HitParticleCount.ContainsKey(offset) == false){
							
							HitParticleCount.Add(offset, 0);
							
						}
						
						HitParticleCount[offset]++;
						
						MyParticleEffect effect;
						
						var particleHitPosition = offsetMatrix.Forward * targetDistance + fromCoords;
						var hitParticleMatrix = MatrixD.CreateWorld(particleHitPosition, offsetMatrix.Forward, offsetMatrix.Up);
						
						if(HitParticleCount[offset] >= Settings.UseParticleAfterRayCount){
							
							//if(MyParticlesManager.TryCreateParticleEffect(Settings.ParticleName, ref hitParticleMatrix, ref particleHitPosition, 0, out effect) == true){
							//	
							//	//MyVisualScriptLogicProvider.ShowNotificationToAll("Particle Duration: " + effect.Duration, 3000);
							//	HitParticleCount[offset] = 0;
							//	effect.UserScale = Settings.ParticleScale;
							//	effect.UserEmitterScale = Settings.ParticleScale;
							//	effect.Loop = false;
							//	
							//	if(Settings.UseHitParticleMaxDuration == true){
							//		
							//		effect.DurationMax = Settings.HitParticleMaxDuration;
							//		effect.DurationMin = Settings.HitParticleMaxDuration;
							//		
							//	}
							//	
							//	if(Settings.ParticleColor != Vector4.Zero){
							//		
							//		effect.UserColorMultiplier = Settings.ParticleColor;
							//		
							//	}
							//	
							//	if(targetEntity != null){
							//		
							//		if(targetEntity.Physics != null){
							//			
							//			effect.Velocity = targetEntity.Physics.LinearVelocity;
							//			
							//		}
							//		
							//	}
							//	
							//}
							
						}
						
					}
					
					if(LastRayHitDistances.ContainsKey(offset) == true){
						
						LastRayHitDistances[offset] = targetDistance;
						
					}else{
						
						LastRayHitDistances.Add(offset, targetDistance);
						
					}
					
					bool hasShieldBuster = false;
					
					if(Settings.UseShieldBuster == true || ShieldEffectUpgradeIncrement > 0){
						
						hasShieldBuster = true;
						
					}
					
					if(doDamage == true && targetEntity != null){
						
						var targetGrid = targetEntity as IMyCubeGrid;
						var targetVoxel = targetEntity as IMyVoxelBase;
						
						//Base Damage
						if(Settings.UseBaseDamage == true){
							
							if(targetGrid != null){
								
								if(Settings.UsePenetrativeDamage == true){
								
									Utilities.ApplyPenetrativeDamage(targetGrid, fromCoords, targetHitPosition, Utilities.UpgradedValueMultiply(Settings.BaseDamageAmount, DamageUpgradeMultiplier), WeaponBlock.EntityId, Settings.PenetrativeDistance);
									
								}else{
								
									Utilities.ApplyRegularDamage(targetGrid, fromCoords, targetHitPosition, Utilities.UpgradedValueMultiply(Settings.BaseDamageAmount, DamageUpgradeMultiplier), WeaponBlock.EntityId);
								
								}
								
							}else{
								
								var destroyableObject = targetEntity as IMyDestroyableObject;
								
								if(destroyableObject != null){
									
									destroyableObject.DoDamage(Utilities.UpgradedValueMultiply(Settings.BaseDamageAmount, DamageUpgradeMultiplier), MyStringHash.GetOrCompute("Laser"), true, null, WeaponBlock.OwnerId);
									
								}
								
							}
							
						}
						
						//Tesla Damage
						if(Settings.UseTeslaEffect == true && TargetTeslaHit == false && targetIsBubble == false){
							
							if(targetGrid != null){
								
								int blocksToAffect = 0;
								
								if(Settings.TeslaMaxBlocksAffected > 1){
									
									blocksToAffect = Utilities.Rnd.Next(1, Settings.TeslaMaxBlocksAffected + 1);
									
								}else if(Settings.TeslaMaxBlocksAffected < 0){
									
									blocksToAffect = 0;
									
								}else{
									
									blocksToAffect = Settings.TeslaMaxBlocksAffected;
									
								}
								
								Utilities.DisableGridRandomBlocks(targetGrid, Utilities.UpgradedValueAddInt(TeslaEffectUpgradeBlocksIncrement, blocksToAffect));
								TargetTeslaHit = true;
								
							}else{
								
								var meatTarget = targetEntity as IMyCharacter;
								
								if(meatTarget != null){
								
									meatTarget.DoDamage(1000, MyStringHash.GetOrCompute("Tesla"), true, null, WeaponBlock.EntityId);
								
								}
							
							}
							
						}
						
						//Shield Damage
						if(hasShieldBuster == true && TargetShieldsHit == false){
							
							if(targetGrid != null){
								
								Utilities.DisableGridShields(targetGrid, WeaponBlock.EntityId);
								TargetShieldsHit = true;
								
							}
							
						}
						
						//JumpDrive Damage
						if(Settings.UseJumpDriveInhibitor == true && TargetJumpDriveHit == false && targetIsBubble == false){
							
							if(targetGrid != null){
								
								Utilities.InhibitJumpDrives(targetGrid, Utilities.UpgradedValueAdd(JumpEffectUpgradeIncrement, Settings.AmountToReduceDrives), Settings.SplitAcrossEachDrive);
								TargetJumpDriveHit = true;
								
							}
							
						}
						
						//Voxel Damage
						if(Settings.UseVoxelDamage == true && targetIsBubble == false){
							
							var backward = Vector3D.Normalize(fromCoords - toCoords);
							var cutCoords = ((double)Settings.VoxelDamageRadius * 0.75) * backward + toCoords;
							Utilities.CutVoxels(cutCoords, Settings.VoxelDamageRadius);
							
						}
						
						//Explosive Damage
						if(Settings.UseExplosionDamage == true){
							
							if(targetIsBubble == true){
								
								if(Settings.BypassBubble == true){
									
									Utilities.CreatePhantomExplosion(toCoords, Settings.ExplosionRadius, Utilities.UpgradedValueMultiply((float)Settings.ExplosionDamage, DamageUpgradeMultiplier), WeaponBlock.EntityId, true, true, false);
									
								}else{
									
									Utilities.CreatePhantomExplosion(targetHitPosition, Settings.ExplosionRadius, Utilities.UpgradedValueMultiply((float)Settings.ExplosionDamage, DamageUpgradeMultiplier), WeaponBlock.EntityId, false, false, false);
									MyVisualScriptLogicProvider.CreateExplosion(toCoords, Settings.ExplosionRadius, 0);
									
								}
								
							}else{
								
								Utilities.CreatePhantomExplosion(toCoords, Settings.ExplosionRadius, Utilities.UpgradedValueMultiply((float)Settings.ExplosionDamage, DamageUpgradeMultiplier), WeaponBlock.EntityId, true, true, false);
								
							}
							
						}
						
						//Hacking Damage
						if(Settings.UseHackingDamage == true && TargetHackingHit == false && targetIsBubble == false){
							
							if(targetGrid != null){
								
								TargetHackingHit = true;
								Utilities.HackTargetBlocks(targetGrid, Utilities.Rnd.Next(Utilities.UpgradedValueAddInt(HackEffectUpgradeBlocksIncrement, Settings.HackingMinBlocksAffected), Utilities.UpgradedValueAddInt(HackEffectUpgradeBlocksIncrement, Settings.HackingMaxBlocksAffected)), WeaponBlock.OwnerId);
								
							}
							
						}
						
					}
					
					//DefenseShield Bubble Hit Only
					if(doDamage == true && targetEntity == null && targetIsBubble == true){
						
						long shieldedEntityId = 0;
						IMyEntity shieldedEntity = null;
						IMyCubeGrid shieldedGrid = null;
						IMySlimBlock closestBlock = null;
						Vector3D blockPosition = Vector3D.Zero;
						
						if(long.TryParse(bubbleEntityIdString, out shieldedEntityId) == true){
							
							if(MyAPIGateway.Entities.TryGetEntityById(shieldedEntityId, out shieldedEntity) == true){
								
								shieldedGrid = shieldedEntity as IMyCubeGrid;
								
								if(shieldedGrid != null){
									
									closestBlock = Utilities.GetClosestBlock(fromCoords, toCoords, shieldedGrid);
									closestBlock.ComputeWorldCenter(out blockPosition);
									
								}
								
							}
							
						}
						
						//BaseDamage
						if(Settings.UseBaseDamage == true && closestBlock != null){
							
							closestBlock.DoDamage(Utilities.UpgradedValueMultiply(Settings.BaseDamageAmount, DamageUpgradeMultiplier), MyStringHash.GetOrCompute("Laser"), true, null, WeaponBlock.EntityId);
							
						}
						
						//ShieldBuster
						if(hasShieldBuster == true && closestBlock != null){
							
							Utilities.DisableGridShields(shieldedGrid, WeaponBlock.EntityId);
							TargetShieldsHit = true;
							
						}
						
						//Explosive
						if(Settings.UseExplosionDamage == true && closestBlock != null){
							
							Utilities.CreatePhantomExplosion(blockPosition, Settings.ExplosionRadius, Utilities.UpgradedValueMultiply((float)Settings.ExplosionDamage, DamageUpgradeMultiplier), WeaponBlock.EntityId, false, false, false);
							MyVisualScriptLogicProvider.CreateExplosion(toCoords, Settings.ExplosionRadius, 0);
							
						}else if(Settings.UseExplosionDamage == true && closestBlock == null){
							
							Utilities.CreatePhantomExplosion(toCoords, Settings.ExplosionRadius, Utilities.UpgradedValueMultiply((float)Settings.ExplosionDamage, DamageUpgradeMultiplier), WeaponBlock.EntityId, true, true, false);
							
						}
						
					}
					
				}
				
			}
			
			//Process Effects
			
			foreach(var offset in Settings.BarrelSubpartOffsets){
				
				var fromCoords = Vector3D.Transform(offset, offsetMatrix);
				var toCoords = Vector3D.Zero;
				double fireDistance = 0;
				
				if(LastRayHitDistances.TryGetValue(offset, out fireDistance) == true){
					
					toCoords = fireDistance * offsetMatrix.Forward + fromCoords;
					
				}else{
					
					toCoords = (double)Utilities.UpgradedValueMultiply((float)Settings.WeaponDistance, RangeUpgradeMultiplier) * offsetMatrix.Forward + fromCoords;
					
				}
				
				//Regular Beam Settings
				if(Settings.UseRegularBeam == true && IsDedicated == false){
					
					float beamRadius = Settings.BeamRadius;
					
					if(Settings.UseBeamFlicker == true){
						
						beamRadius = Utilities.RandomFloat(Settings.BeamMinimumRadius, Settings.BeamMaximumRadius);
						
					}
					
					var randomColor = Settings.BeamColors[Utilities.Rnd.Next(0, Settings.BeamColors.Count)];
					var beamColor = Utilities.ConvertColor(randomColor);
					MySimpleObjectDraw.DrawLine(fromCoords, toCoords, MyStringId.GetOrCompute("WeaponLaser"), ref beamColor, beamRadius);
					MySimpleObjectDraw.DrawLine(fromCoords, toCoords, MyStringId.GetOrCompute("WeaponLaser"), ref beamColor, beamRadius * 0.66f);
					MySimpleObjectDraw.DrawLine(fromCoords, toCoords, MyStringId.GetOrCompute("WeaponLaser"), ref beamColor, beamRadius * 0.33f);
					
				}
				
				//Tesla Beam Settings
				if(Settings.UseTeslaBeam == true && IsDedicated == false){
					
					TeslaBeamTicks++;
					
					if(TeslaBeamTicks >= 4){
						
						var newList = Utilities.CreateElectricityOffset((double)Utilities.UpgradedValueMultiply((float)Settings.WeaponDistance, RangeUpgradeMultiplier), Settings.TeslaBeamMinStep, Settings.TeslaBeamMaxStep, Settings.TeslaBeamMaxLateral);
						
						if(TeslaBeamPoints.ContainsKey(offset) == true){
							
							TeslaBeamPoints[offset] = newList;
							
						}else{
							
							TeslaBeamPoints.Add(offset, newList);
							
						}
						
					}
					
					float teslaRadius = Settings.TeslaBeamRadius;
					
					if(Settings.UseTeslaBeamFlicker == true){
						
						teslaRadius = Utilities.RandomFloat(Settings.TeslaBeamMinimumRadius, Settings.TeslaBeamMaximumRadius);
						
					}
					
					var randomColor = Settings.TeslaBeamColors[Utilities.Rnd.Next(0, Settings.TeslaBeamColors.Count)];
					var startMatrix = MatrixD.CreateWorld(fromCoords, offsetMatrix.Forward, offsetMatrix.Up);
					Utilities.DisplayElectricEffect(startMatrix, toCoords, teslaRadius, randomColor, TeslaBeamPoints[offset]);
					
				}
				
				//Barrel Particles
				//if(Settings.UseBarrelParticles == true && IsDedicated == false){
                //
				//	MyParticleEffect effect;
				//		
				//	if(BarrelParticles.ContainsKey(offset) == false){
				//		
				//		if(MyParticlesManager.TryCreateParticleEffect(Settings.BarrelParticleName, ref offsetMatrix, ref fromCoords, 0, out effect) == true){
				//			
				//			BarrelParticles.Add(offset, effect);
				//			
				//		}
                //
				//	}else{
				//		
				//		effect = BarrelParticles[offset];
				//		
				//	}
				//	
				//	var particleMatrix = MatrixD.CreateWorld(fromCoords, offsetMatrix.Forward, offsetMatrix.Up);
				//	effect.WorldMatrix = particleMatrix;
				//	effect.UserScale = Settings.BarrelParticleScale;
				//	effect.UserEmitterScale = Settings.BarrelParticleScale;
				//	
				//	if(Settings.BarrelParticleColor != Vector4.Zero){
				//		
				//		effect.UserColorMultiplier = Settings.BarrelParticleColor;
				//		
				//	}
                //
				//	effect.Loop = true;
				//	
				//	if(Settings.LoopBarrelAfterTicks >= BarrelParticleTicks){
				//		
				//		effect.Restart();
				//		
				//	}
				//	
				//	effect.Play();
				//	
				//	BarrelParticles[offset] = effect;
				//	
				//}
				
				/*
				if(Settings.UseBarrelLights == true){
					
					var light = new MyLight();
					light.LightOn = true;
					light.Position = fromCoords;
					light.Color = Settings.BarrelLightColor;
					light.Intensity = Settings.BarrelLightIntensity;
					light.Range = Settings.BarrelLightRange;
					
					if(Settings.BarrelLightUseGlare == true){
						
						light.GlareOn = true;
						light.GlareIntensity = Settings.BarrelLightGlareIntensity;
						light.GlareMaxDistance = Settings.BarrelLightGlareMaxDistance;

					}
					
				}
				*/
			}
			
			if(Settings.UseBarrelParticles == true && IsDedicated == false){
				
				BarrelParticleTicks++;
				
				if(Settings.LoopBarrelAfterTicks >= BarrelParticleTicks){
					
					BarrelParticleTicks = 0;
					
				}
				
			}
			
			ActiveTimer++;
			
			if(ActiveTimer >= Settings.TickTimerLimit){
				
				ActiveFiring = false;
				
				if(Settings.UseBarrelParticles == true && IsDedicated == false){
					
					foreach(var barrelParticle in BarrelParticles.Keys){
						
						BarrelParticles[barrelParticle].StopEmitting();
						
					}
					
				}
				
			}
			
		}
		
		public override void UpdateBeforeSimulation100(){
			
			if(Settings.UseRegenerativeAmmo == true){
				
				CheckPowerState(true);
				
				if(BlockColor != WeaponBlock.SlimBlock.ColorMaskHSV){
					
					ChangeEmissive(EmissiveMode, true);
					BlockColor = WeaponBlock.SlimBlock.ColorMaskHSV;
					
				}
				
			}
			
		}
		
		public void CheckPowerState(bool regularActivation = false){
			
			float currentItems = 0;
			
			if(BlockIsWorking == true){
				
				currentItems = (float)WeaponInventory.GetItemAmount(AmmoItemDefId);
				LastAmmoCount = currentItems;
				
				if(currentItems < Settings.MaxAmmoInInventory){
					
					RequiredPower = Utilities.UpgradePowerRequirement(PowerUpgradeMultiplier, Settings.AmmoRegenerationMaxPowerDraw);
					ChangeEmissive("Charging");
					
				}else{
					
					RequiredPower = MinimumPower;
					ChangeEmissive("Idle");
				
				}
				
			}else{
				
				RequiredPower = 0;
				
			}
			
			if(Settings.AmmoRegenerationFreeForNPC == true && IsOwnedByNPC == true){
				
				RequiredPower = MinimumPower;
				
			}
		
			var powerAvailable = ResourceSink.IsPowerAvailable(PowerId, RequiredPower);	
			
			if(LowPower == false && BlockIsWorking == true){
								
				if(powerAvailable == false){
					
					RequiredPower = Utilities.UpgradePowerRequirement(PowerUpgradeMultiplier, Settings.AmmoRegenerationMedPowerDraw);
					powerAvailable = ResourceSink.IsPowerAvailable(PowerId, Utilities.UpgradePowerRequirement(PowerUpgradeMultiplier, Settings.AmmoRegenerationMedPowerDraw));
					
					if(powerAvailable == false){
						
						RequiredPower = MinimumPower;
						powerAvailable = ResourceSink.IsPowerAvailable(PowerId, MinimumPower);
						
						if(powerAvailable == false){
							
							LowPower = true;
							
						}
						
					}
					
				}
				
				if(LowPower == false && RequiredPower != MinimumPower && currentItems < Settings.MaxAmmoInInventory && regularActivation == true){
					
					//Add To AccumulatedPower
					var powerToAdd = RequiredPower * 1.66f;
					AccumulatedPower += powerToAdd;
					
					if(AccumulatedPower >= Utilities.UpgradedValueAdd(Settings.AmmoRegenerationTime, PowerStoreUpgradeMultiplier)){
						
						AccumulatedPower = 0;
						
						if(WeaponInventory.CanItemsBeAdded(1, AmmoItemDefId) == true && IsServer == true){
							
							WeaponInventory.AddItems(1, AmmoItem.Content);
							currentItems = (float)WeaponInventory.GetItemAmount(AmmoItemDefId);
							LastAmmoCount = currentItems;
							
						}
						
					}
					
				}
				
				if(Settings.AmmoRegenerationFreeForNPC == true && IsOwnedByNPC == true && regularActivation == true && currentItems < Settings.MaxAmmoInInventory){
				
					//Add To AccumulatedPower
					var powerToAdd = Utilities.UpgradePowerRequirement(PowerUpgradeMultiplier, Settings.AmmoRegenerationMaxPowerDraw) * 1.66f;
					AccumulatedPower += powerToAdd;
					
					if(AccumulatedPower >= Utilities.UpgradedValueAdd(Settings.AmmoRegenerationTime, PowerStoreUpgradeMultiplier)){
						
						AccumulatedPower = 0;
						
						if(WeaponInventory.CanItemsBeAdded(1, AmmoItemDefId) == true && IsServer == true){
							
							WeaponInventory.AddItems(1, AmmoItem.Content);
							currentItems = (float)WeaponInventory.GetItemAmount(AmmoItemDefId);
							LastAmmoCount = currentItems;
							
						}
						
					}
					
				}
				
			}else{
				
				if(powerAvailable == true && BlockIsWorking == true){
					
					if(ResourceSink.IsPowerAvailable(PowerId, RequiredPower) == true){
						
						LowPower = false;
						RequiredPower = MinimumPower;
						
						if(ResourceSink.IsPowerAvailable(PowerId, Utilities.UpgradePowerRequirement(PowerUpgradeMultiplier, Settings.AmmoRegenerationMedPowerDraw)) == true && currentItems < Settings.MaxAmmoInInventory){
							
							RequiredPower = Utilities.UpgradePowerRequirement(PowerUpgradeMultiplier, Settings.AmmoRegenerationMedPowerDraw);
							
							if(ResourceSink.IsPowerAvailable(PowerId, Utilities.UpgradePowerRequirement(PowerUpgradeMultiplier, Settings.AmmoRegenerationMaxPowerDraw)) == true){
								
								RequiredPower = Utilities.UpgradePowerRequirement(PowerUpgradeMultiplier, Settings.AmmoRegenerationMaxPowerDraw);
								
							}
							
						}
						
					}
		
				}
				
			}
			
			
			if(LocalPlayer != null){
				
				if(MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel){
					
					var getCustomInfo = WeaponBlock.CustomInfo;
					WeaponBlock.RefreshCustomInfo();
					
					if(getCustomInfo != WeaponBlock.CustomInfo){
						
						var myCubeBlock = Entity as MyCubeBlock;
						
						if(myCubeBlock.IDModule != null){
							
							var share = myCubeBlock.IDModule.ShareMode;
							var owner = myCubeBlock.IDModule.Owner;
							myCubeBlock.ChangeOwner(owner, share == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.Faction : MyOwnershipShareModeEnum.None);
							myCubeBlock.ChangeOwner(owner, share);
							
						}
						
					}
				
				}
				
			}
			
		}
		
		public void ChangeEmissive(string mode, bool force = false){
			
			if(EmissiveMode == mode && force == false){
				
				return;
				
			}
			
			if(mode == "Idle"){
				
				EmissiveMode = "Idle";
				WeaponBlock.SetEmissiveParts(Settings.EmissiveIdleName, Settings.EmissiveIdleColor, Settings.EmissiveIdleAmount);
				WeaponBlock.SetEmissivePartsForSubparts(Settings.EmissiveIdleName, Settings.EmissiveIdleColor, Settings.EmissiveIdleAmount);
				
			}
			
			if(mode == "Charging"){
				
				EmissiveMode = "Charging";
				WeaponBlock.SetEmissiveParts(Settings.EmissiveChargingName, Settings.EmissiveChargingColor, Settings.EmissiveChargingAmount);
				WeaponBlock.SetEmissivePartsForSubparts(Settings.EmissiveChargingName, Settings.EmissiveChargingColor, Settings.EmissiveChargingAmount);
					
			}
			
			if(mode == "Inactive"){
				
				EmissiveMode = "Inactive";
				WeaponBlock.SetEmissiveParts(Settings.EmissiveInactiveName, Settings.EmissiveInactiveColor, Settings.EmissiveInactiveAmount);
				WeaponBlock.SetEmissivePartsForSubparts(Settings.EmissiveInactiveName, Settings.EmissiveInactiveColor, Settings.EmissiveInactiveAmount);
				
			}
			
		}
		
		public void AppendCustomInfo(IMyTerminalBlock block, StringBuilder sb){
			
			sb.Clear();
			
			if(LowPower == false/* && block.IsFunctional == true && block.IsWorking == true*/){
				
				sb.Append("[Ammo Generator]").AppendLine().AppendLine();
				sb.Append("Current Power Draw: \n" + RequiredPower.ToString() + "MW");
				sb.AppendLine().AppendLine();
				sb.Append("Accumulated Power: \n" + AccumulatedPower.ToString() + "MW / " + Settings.AmmoRegenerationTime.ToString() + "MW");
				sb.AppendLine().AppendLine();
				sb.Append("Energy Charges Produced: \n" + LastAmmoCount.ToString() + " / " + Settings.MaxAmmoInInventory.ToString());
				sb.AppendLine().AppendLine();
				sb.Append("Insufficient Power: " + LowPower.ToString());
				sb.AppendLine().AppendLine();
				
				if(Settings.AllowUpgrades == true){
					
					sb.Append("[Upgrades]").AppendLine().AppendLine();
					
					if(Settings.UseRegenerativeAmmo == true){
						
						sb.Append("Power Multiplier: \n" + PowerUpgradeMultiplier.ToString());
						sb.AppendLine().AppendLine();
						sb.Append("Accumulated Power Modifier: \n" + PowerStoreUpgradeMultiplier.ToString());
						sb.AppendLine().AppendLine();
						
					}
					
					if(Settings.UseBaseDamage == true || Settings.UseExplosionDamage == true){
						
						sb.Append("Damage Multiplier: \n" + DamageUpgradeMultiplier.ToString());
						sb.AppendLine().AppendLine();
						
					}
					
					if(Settings.WeaponDistance > 0){
						
						sb.Append("Range Multiplier: \n" + RangeUpgradeMultiplier.ToString());
						sb.AppendLine().AppendLine();
						
					}
					
					if(Settings.UseTeslaEffect == true){
						
						sb.Append("Tesla Additional Effect: \n" + TeslaEffectUpgradeBlocksIncrement.ToString());
						sb.AppendLine().AppendLine();
						
					}
					
					if(Settings.UseJumpDriveInhibitor == true){
						
						sb.Append("Jump Inhibitor Additional Effect: \n" + JumpEffectUpgradeIncrement.ToString());
						sb.AppendLine().AppendLine();
						
					}
					
					if(Settings.UseHackingDamage == true){
						
						sb.Append("Hacking Additional Effect: \n" + HackEffectUpgradeBlocksIncrement.ToString());
						sb.AppendLine().AppendLine();
						
					}
					
					if(Settings.UseTractorBeamEffect == true){
						
						sb.Append("Tractor Additional Effect: \n" + TractorEffectUpgradeIncrement.ToString());
						sb.AppendLine().AppendLine();
						
					}
					
					if(ShieldEffectUpgradeIncrement > 0){
						
						sb.Append("Shield Buster Effect: \n" + "Enabled");
						sb.AppendLine().AppendLine();
						
					}
					
				}
				
			}else{
				
				sb.Append("Weapon Offline.");
				
			}
			
		}
		
		public void WorkingStateChange(IMyCubeBlock block){
			
			//MyVisualScriptLogicProvider.ShowNotificationToAll("Work State Change", 2000);
			if(block.IsFunctional == false || block.IsWorking == false){
				
				if(Settings.UseBarrelParticles == true && IsDedicated == false){
					
					foreach(var barrelParticle in BarrelParticles.Keys){
						
						BarrelParticles[barrelParticle].StopEmitting();
						
					}
					
				}
				
				BlockIsWorking = false;
				ChangeEmissive("Inactive");
				
				if(ActiveFiring == true){
					
					ActiveFiring = false;
					
				}
				
				if(Settings.UseRegenerativeAmmo == true){
					
					RequiredPower = 0; //MinimumPower;
					CheckPowerState();
				
				}
				
			}
			
			if(block.IsFunctional == true && block.IsWorking == true){
				
				BlockIsWorking = true;
				ChangeEmissive("Idle");
				
				if(Settings.UseRegenerativeAmmo == true){
					
					RequiredPower = MinimumPower;
					CheckPowerState();
					
				}

			}

		}
		
		public float PowerUse(){
			
			return RequiredPower;
			
		}
		
		public void RecheckOwnership(IMyTerminalBlock block){
			
			IsOwnedByNPC = Utilities.IsOwnerNPC(WeaponBlock.OwnerId);
			
		}
		
		public void RefreshUpgrades(){
			
			if(UpgradeBlock == null || Settings.AllowUpgrades == false){
				
				return;
				
			}
			
			var upgrades = UpgradeBlock.UpgradeValues;
			//var upgrades = new Dictionary<string, float>();
			//UpgradeBlock.GetUpgrades(out upgrades);
			
			MyVisualScriptLogicProvider.ShowNotificationToAll("Upgrade Count: " + upgrades.Keys.Count.ToString(), 5000);
			
			//Reset all upgrade value variables to default values first
			DamageUpgradeMultiplier = 1;
			PowerUpgradeMultiplier = 1;
			PowerStoreUpgradeMultiplier = 0;
			RangeUpgradeMultiplier = 1;
			TeslaEffectUpgradeBlocksIncrement = 0;
			JumpEffectUpgradeIncrement = 0;
			HackEffectUpgradeBlocksIncrement = 0;
			TractorEffectUpgradeIncrement = 0;
			
			//Now, read new or changed values
			
			//Power
			if(Settings.UseRegenerativeAmmo == true && upgrades.ContainsKey(Settings.UpgradePowerName) == true){
				
				PowerUpgradeMultiplier += upgrades[Settings.UpgradePowerName];
				
			}
			
			//PowerStore
			if(Settings.UseRegenerativeAmmo == true && upgrades.ContainsKey(Settings.UpgradePowerStoreName) == true){
				
				PowerStoreUpgradeMultiplier += upgrades[Settings.UpgradePowerStoreName];
				
			}
			
			//Damage
			if(Settings.UseBaseDamage == true && upgrades.ContainsKey(Settings.UpgradeDamageName) == true){
				
				DamageUpgradeMultiplier += upgrades[Settings.UpgradeDamageName];
				
			}
			
			//ExplodeDamage
			if(Settings.UseExplosionDamage == true && upgrades.ContainsKey(Settings.UpgradeDamageName) == true){
				
				DamageUpgradeMultiplier += upgrades[Settings.UpgradeDamageName];
				
			}
			
			//Range
			if(upgrades.ContainsKey(Settings.UpgradeRangeName) == true){
				
				RangeUpgradeMultiplier += upgrades[Settings.UpgradeRangeName];
				
			}			
			
			//Tesla
			if(Settings.UseTeslaEffect == true && upgrades.ContainsKey(Settings.UpgradeTeslaEffectName) == true){
				
				int roundedValue = (int)Math.Floor(upgrades[Settings.UpgradeTeslaEffectName]);
				TeslaEffectUpgradeBlocksIncrement += roundedValue;
				
			}	
			
			//Jump
			if(Settings.UseJumpDriveInhibitor == true && upgrades.ContainsKey(Settings.UpgradeJumpEffectName) == true){
				
				JumpEffectUpgradeIncrement += upgrades[Settings.UpgradeJumpEffectName];
				
			}
			
			//Hack
			if(Settings.UseHackingDamage == true && upgrades.ContainsKey(Settings.UpgradeHackEffectName) == true){
				
				int roundedValue = (int)Math.Floor(upgrades[Settings.UpgradeHackEffectName]);
				HackEffectUpgradeBlocksIncrement += roundedValue;
				
			}
			
			//Tractor 
			if(Settings.UseTractorBeamEffect == true && upgrades.ContainsKey(Settings.UpgradeTractorEffectName) == true){
				
				TractorEffectUpgradeIncrement += upgrades[Settings.UpgradeTractorEffectName];
				
			}
			
			//Finally, Ensure the new values are within minimum allowed values.
			if(DamageUpgradeMultiplier < DamageUpgradeMinimum){
				
				DamageUpgradeMultiplier = DamageUpgradeMinimum;
				
			}
			
			if(PowerUpgradeMultiplier < PowerUpgradeMinimum){
				
				PowerUpgradeMultiplier = PowerUpgradeMinimum;
				
			}
			
			if(RangeUpgradeMultiplier < RangeUpgradeMinimum){
				
				RangeUpgradeMultiplier = RangeUpgradeMinimum;
				
			}
			
			if(TeslaEffectUpgradeBlocksIncrement < TeslaEffectUpgradeMinimum){
				
				TeslaEffectUpgradeBlocksIncrement = TeslaEffectUpgradeMinimum;
				
			}
			
			if(JumpEffectUpgradeIncrement < JumpEffectUpgradeMinimum){
				
				JumpEffectUpgradeIncrement = JumpEffectUpgradeMinimum;
				
			}
			
			if(HackEffectUpgradeBlocksIncrement < HackEffectUpgradeMinimum){
				
				HackEffectUpgradeBlocksIncrement = HackEffectUpgradeMinimum;
				
			}
			
			if(TractorEffectUpgradeIncrement < TractorEffectUpgradeMinimum){
				
				TractorEffectUpgradeIncrement = TractorEffectUpgradeMinimum;
				
			}
			
		}
		
		public void NetworkMessageReceiver(byte[] receivedData){
			
			var shotData = MyAPIGateway.Utilities.SerializeFromBinary<EnergyWeaponSyncData>(receivedData);
			
			if(shotData == null || IsServer == true){
				
				return;
				
			}
			
			if(shotData.WeaponEntityId == WeaponBlock.EntityId){
				
				LastWeaponFire = shotData.LastFire;
				
			}
			
		}
		
		public override void OnRemovedFromScene(){
			
			base.OnRemovedFromScene();
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(31461, NetworkMessageReceiver);
			
			if(Settings.UseBarrelParticles == true && IsDedicated == false){
					
				foreach(var barrelParticle in BarrelParticles.Keys){
					
					BarrelParticles[barrelParticle].StopEmitting();
					
				}
				
			}
			
			var Block = Entity as IMyUserControllableGun;
			
			if(Block == null){
				
				return;
				
			}
			
			Block.IsWorkingChanged -= WorkingStateChange;
			Block.AppendingCustomInfo -= AppendCustomInfo;
			Block.OwnershipChanged -= RecheckOwnership;
			
			
		}
		
		public override void OnBeforeRemovedFromContainer(){
			
			base.OnBeforeRemovedFromContainer();
			
			if(Entity.InScene == true){
				
				OnRemovedFromScene();
				
			}
			
		}
		
	}
	
}