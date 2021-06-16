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
	
	public class WeaponConfig{
		
		//Weapon Subtype
		public string WeaponSubtypeId {get; set;}
		public bool UseScriptedFire {get; set;}
		
		//Regenerative Ammo Settings
		public bool UseRegenerativeAmmo {get; set;}
		public string AmmoMagazineSubtypeId {get; set;}
		public float AmmoRegenerationMaxPowerDraw {get; set;}
		public float AmmoRegenerationMedPowerDraw {get; set;}
		public float AmmoRegenerationLowPowerDraw {get; set;}
		public float AmmoRegenerationTime {get; set;}
		public float MaxAmmoInInventory {get; set;}
		public bool AmmoRegenerationFreeForNPC {get; set;}
		
		//Timer and Distance Settings
		public int PreHitTimerLimit {get; set;}
		public int TickTimerLimit {get; set;}
		public int PostHitTimerLimit {get; set;}
		public int DamageTimerLimit {get; set;}
		
		//Distance Settings
		public double WeaponDistance {get; set;}
		public double SafeRange {get; set;}
				
		//Emissives - Off/Disabled/Damaged
		public string EmissiveInactiveName {get; set;}
		public float EmissiveInactiveAmount {get; set;}
		public Color EmissiveInactiveColor {get; set;}
		
		//Emissives - Idle
		public string EmissiveIdleName {get; set;}
		public float EmissiveIdleAmount {get; set;}
		public Color EmissiveIdleColor {get; set;}
		
		//Emissives - Charging
		public string EmissiveChargingName {get; set;}
		public float EmissiveChargingAmount {get; set;}
		public Color EmissiveChargingColor {get; set;}
		
		//Multibeam Settings
		public string TurretBarrelsSubpartName {get; set;}
		public List<Vector3D> BarrelSubpartOffsets {get; set;}
		
		//Upgrade Settings
		public bool AllowUpgrades {get; set;}
		public string UpgradeDamageName {get; set;}
		public string UpgradePowerName {get; set;}
		public string UpgradePowerStoreName {get; set;}
		public string UpgradeRangeName {get; set;}
		public string UpgradeTeslaEffectName {get; set;}
		public string UpgradeJumpEffectName {get; set;}
		public string UpgradeHackEffectName {get; set;}
		public string UpgradeTractorEffectName {get; set;}
		public string UpgradeShieldEffectName {get; set;}
		
		//Base Damage
		public bool UseBaseDamage {get; set;}
		public float BaseDamageAmount {get; set;}
		public bool UsePenetrativeDamage {get; set;}
		public double PenetrativeDistance {get; set;}
		
		//Explosive Damage
		public bool UseExplosionDamage {get; set;}
		public int ExplosionDamage {get; set;}
		public float ExplosionRadius {get; set;}
		
		//Voxel Damage
		public bool UseVoxelDamage {get; set;}
		public float VoxelDamageRadius {get; set;}
			
		//Tesla Damage
		public bool UseTeslaEffect {get; set;}
		public int TeslaMinBlocksAffected {get; set;}
		public int TeslaMaxBlocksAffected {get; set;}
		
		//Jump Drive Damage
		public bool UseJumpDriveInhibitor {get; set;}
		public float AmountToReduceDrives {get; set;}
		public bool SplitAcrossEachDrive {get; set;}
		
		//Shield Damage
		public bool UseShieldBuster {get; set;}
		
		//Hacking Damage
		public bool UseHackingDamage {get; set;}
		public int HackingMinBlocksAffected {get; set;}
		public int HackingMaxBlocksAffected {get; set;}
		
		//Tractor Beam Effect
		public bool UseTractorBeamEffect {get; set;}
		
		//Defense Shield Specific Settings
		public bool BypassBubble {get; set;}
		
		//Inventory Item
		public MyDefinitionId InventoryItemId {get; set;}
		public MyObjectBuilder_InventoryItem InventoryItem {get; set;}
		
		//Sound Settings
		public string FiringSoundId {get; set;}
		
		//Beam Effect
		public bool UseRegularBeam {get; set;}
		public bool UseBeamFlicker {get; set;}
		public bool FadeBeamEndPoint {get; set;}
		public bool ShrinkBeamEndPoint {get; set;}
		public float BeamRadius {get; set;}
		public float BeamMinimumRadius {get; set;}
		public float BeamMaximumRadius {get; set;}
		public List<Color> BeamColors {get; set;}
		
		//Tesla Effect
		public bool UseTeslaBeam {get; set;}
		public bool UseTeslaBeamFlicker {get; set;}
		public double TeslaBeamMaxLateral {get; set;}
		public double TeslaBeamMinStep {get; set;}
		public double TeslaBeamMaxStep {get; set;}
		public float TeslaBeamRadius {get; set;}
		public float TeslaBeamMinimumRadius {get; set;}
		public float TeslaBeamMaximumRadius {get; set;}
		public List<Color> TeslaBeamColors {get; set;}
		
		//Particle Barrel Settings
		public bool UseBarrelParticles {get; set;}
		public string BarrelParticleName {get; set;}
		public float BarrelParticleScale {get; set;}
		public Vector4 BarrelParticleColor {get; set;}
		public bool LoopBarrelParticle {get; set;}
		public int LoopBarrelAfterTicks {get; set;}
		
		public bool UseBarrelLights {get; set;}
		public Color BarrelLightColor {get; set;}
		public float BarrelLightIntensity {get; set;}
		public float BarrelLightRange {get; set;}
		public bool BarrelLightUseGlare {get; set;}
		public float BarrelLightGlareIntensity {get; set;}
		public float BarrelLightGlareMaxDistance {get; set;}
		
		//Hit Particle Effect
		public bool UseHitParticles {get; set;}
		public int UseParticleAfterRayCount {get; set;}
		public string ParticleName {get; set;}
		public Vector4 ParticleColor {get; set;}
		public float ParticleScale {get; set;}
		public bool UseHitParticleMaxDuration {get; set;}
		public float HitParticleMaxDuration {get; set;}
		
		public WeaponConfig(){
			
			WeaponSubtypeId = "MA_Designator_Laser";
			UseScriptedFire = true;
			
			UseRegenerativeAmmo = true;
			AmmoMagazineSubtypeId = "";
			AmmoRegenerationMaxPowerDraw = 10;
			AmmoRegenerationMedPowerDraw = 5;
			AmmoRegenerationLowPowerDraw = 1;
			AmmoRegenerationTime = 12;
			MaxAmmoInInventory = 5;
			AmmoRegenerationFreeForNPC = true;
			
			PreHitTimerLimit = 0;
			TickTimerLimit = 0;
			PostHitTimerLimit = 0;
			DamageTimerLimit = 0;
			
			WeaponDistance = 800;
			SafeRange = 2;
			
			EmissiveInactiveName = "";
			EmissiveInactiveAmount = 1;
			EmissiveInactiveColor = new Color(0,0,0,255);
			
			EmissiveIdleName = "";
			EmissiveIdleAmount = 1;
			EmissiveIdleColor = new Color(0,0,0,255);
			
			EmissiveChargingName = "";
			EmissiveChargingAmount = 1;
			EmissiveChargingColor = new Color(0,0,0,255);

			TurretBarrelsSubpartName = "";
			BarrelSubpartOffsets = new List<Vector3D>();
			
			AllowUpgrades = false;
			UpgradeDamageName = "ChangeToValidName";
			UpgradePowerName = "ChangeToValidName";
			UpgradePowerStoreName = "ChangeToValidName";
			UpgradeRangeName = "ChangeToValidName";
			UpgradeTeslaEffectName = "ChangeToValidName";
			UpgradeJumpEffectName = "ChangeToValidName";
			UpgradeHackEffectName = "ChangeToValidName";
			UpgradeTractorEffectName = "ChangeToValidName";
			UpgradeShieldEffectName = "ChangeToValidName";
			
			UseBaseDamage = false;
			BaseDamageAmount = 0;
			UsePenetrativeDamage = false;
			PenetrativeDistance = 0;
			
			UseExplosionDamage = false;
			ExplosionDamage = 0;
			ExplosionRadius = 0;
			
			UseVoxelDamage = false;
			VoxelDamageRadius = 0;
			
			UseTeslaEffect = false;
			TeslaMinBlocksAffected = 0;
			TeslaMaxBlocksAffected = 0;
			
			UseJumpDriveInhibitor = false;
			AmountToReduceDrives = 0;
			SplitAcrossEachDrive = false;
			
			UseShieldBuster = false;
			
			UseHackingDamage = false;
			HackingMinBlocksAffected = 1;
			HackingMaxBlocksAffected = 2;
			
			UseTractorBeamEffect = false;
			
			BypassBubble = false;
			
			InventoryItemId = new MyDefinitionId();
			InventoryItem = null;
			
			FiringSoundId = "";
			
			UseRegularBeam = false;
			UseBeamFlicker = false;
			FadeBeamEndPoint = false;
			ShrinkBeamEndPoint = false;
			BeamRadius = 0;
			BeamMinimumRadius = 0;
			BeamMaximumRadius = 0;
			BeamColors = new List<Color>();
			
			UseTeslaBeam = false;
			UseTeslaBeamFlicker = false;
			TeslaBeamMaxLateral = 0;
			TeslaBeamMinStep = 0;
			TeslaBeamMaxStep = 0;
			TeslaBeamRadius = 0;
			TeslaBeamMinimumRadius = 0;
			TeslaBeamMaximumRadius = 0;
			TeslaBeamColors = new List<Color>();
			
			UseBarrelParticles = false;
			BarrelParticleName = "";
			BarrelParticleScale = 1;
			BarrelParticleColor = Vector4.Zero;
			LoopBarrelParticle = false;
			LoopBarrelAfterTicks = 4;
			
			UseBarrelLights = true;
			BarrelLightColor = Color.Cyan;
			BarrelLightIntensity = 10;
			BarrelLightRange = 15;
			BarrelLightUseGlare = true;
			BarrelLightGlareIntensity = 10;
			BarrelLightGlareMaxDistance = 40;
			
			UseHitParticles = false;
			UseParticleAfterRayCount = 3;
			ParticleName = "";
			ParticleColor = new Vector4(0,0,0,0);
			ParticleScale = 1;
			UseHitParticleMaxDuration = false;
			HitParticleMaxDuration = 0;
			
		}
		
	}
	
}