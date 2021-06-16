using VRage.Game.ModAPI;
using VRageMath;
using Digi;
using Sandbox.ModAPI;
using System;
using VRage.Utils;
using VRage.Game;
using Sandbox.Definitions;
using Sandbox.Game;
using System.Collections.Generic;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Scripts.Shared;
using VRage.ObjectBuilders;
using Scripts.Specials;
using Scripts.Specials.Blocks;

namespace ServerMod {

    public interface IGridProtector {
        /// <summary>
        /// May be called async. Returns true, if default protection not needed
        /// </summary>
        /// <returns></returns>
        bool InterceptDamage (IMyCubeGrid grid, IMySlimBlock block, ref MyDamageInformation damage);
    }

    public static class CustomDamageSystem {
        public static Dictionary<long, IGridProtector> protectedGrids = new Dictionary<long, IGridProtector>();
        public static object lockObject = new object();
        public static void Init() {
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(10, HandleDamage);
        }

        

        private static void HandleDamage(object target, ref MyDamageInformation damage)
        { //Paralell
            try
            {
                var slimBlock = target as IMySlimBlock;
                if (slimBlock != null)
                {
                    if (VoxelDamageProtection(ref damage, slimBlock)) return;
                    if (DisableRammingAgainsStatic(ref damage, slimBlock)) return;
                    if (ProtectFromBowling (ref damage)) return;

                    ProtectArmor(ref damage, slimBlock);

                    var cubeGrid = slimBlock.CubeGrid;
                    var ship = cubeGrid.GetShip();
                    if (ship == null)
                    {
                        //Log.Error("Ship is null for " + cubeGrid);
                        return;
                    }
                    damage.Amount *= ship.damageReduction;
                    handleBlockDamage(slimBlock, ref damage, ship);
                }
            }
            catch (Exception e)
            {
                Log.ChatError("CustomDamageSystem:", e);
            }
        }


        private static bool ProtectFromBowling (ref MyDamageInformation damage)
        {
            var attacker = MyAPIGateway.Entities.GetEntityById(damage.AttackerId);
            var bl = (attacker as IMyCubeBlock);
            if (bl != null)
            {
                if (bl.SlimBlock.Integrity < bl.SlimBlock.MaxIntegrity * 0.2)
                {
                    damage.Amount = 0;
                    damage.IsDeformation = false;
                    return true;
                }
            }

            return false;
        }

        private static bool DisableRammingAgainsStatic(ref MyDamageInformation damage, IMySlimBlock slimBlock)
        {
            if (slimBlock != null && (damage.Type == MyDamageType.Fall || damage.Type == MyDamageType.Deformation) && slimBlock.CubeGrid.IsStatic)
            {
                damage.Amount = 0;
                damage.IsDeformation = false;
                return true;
            }
            return false;
        }

        private static bool VoxelDamageProtection(ref MyDamageInformation damage, IMySlimBlock slimBlock)
        {
            var atk = MyAPIGateway.Entities.GetEntityById(damage.AttackerId);
            if (slimBlock != null && damage.Type == MyDamageType.Deformation)
            {
                var ph = slimBlock.CubeGrid.Physics;
                if (ph != null && ph.LinearVelocity.LengthSquared() < 30 * 30)
                { //900 = 30*30 ms
                    if (atk == null || atk is MyVoxelBase)
                    { //by voxel
                        damage.Amount = 0;
                        damage.IsDeformation = false;
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool ProtectArmor(ref MyDamageInformation damage, IMySlimBlock slimBlock)
        {
            if (damage.Type == MyDamageType.Explosion && slimBlock != null && slimBlock.FatBlock != null)
            {
                //if (slimBlock.BlockDefinition.Id.TypeId == ArmorRebalance.ARMOR_BLOCK)
                {
                    damage.Amount /= 7; //All fatblock recieve x7 damage from rockets
                    return true;
                }
            }
            return false;
        }


        public static float calculateDamage (IMySlimBlock block, float mlt) {
            return block.MaxIntegrity * mlt * (block.BlockDefinition as MyCubeBlockDefinition).GeneralDamageMultiplier;
        }

        public static void AddEntityProtection (long gridId, IGridProtector protector) {
            lock (lockObject) {
                if (protectedGrids.ContainsKey (gridId)) {
                    if (protectedGrids[gridId] != protector) {
                        Log.Error("Protector already exists");
                    }
                } else {
                   protectedGrids.Add (gridId, protector);
                }
            }
        }

        public static void RemoveEntityProtection (long gridId) {
            lock (lockObject) {
                protectedGrids.Remove (gridId);
            }
        }

        public static bool isProtected (long gridId, long entity, MyStringHash damage) {
            if (gridId == 0 && entity == 0) return false; 
            lock (lockObject) {
                if (gridId != 0 && protectedGrids.ContainsKey (gridId)) {
                    return true;
                }

                if (entity != 0 && protectedGrids.ContainsKey (entity)) {
                    return true;
                }
            }

            return false;
        }

        public static IGridProtector GetGridProtector(long gridId) {
            lock (lockObject) {
                if (gridId != 0 && protectedGrids.ContainsKey (gridId)) {
                    return protectedGrids[gridId];
                }
            }

            return null;
        }

        public static void handleBlockDamage(IMySlimBlock block, ref MyDamageInformation damage, Ship ship) {
            var protector = GetGridProtector (block.CubeGrid.EntityId);
            if (protector != null) {
                if (protector.InterceptDamage (block.CubeGrid, block, ref damage)) {
                    return;
                }
            }

            if (FactionSafeZoneBlock.Protect (block, ref damage))
            {
                return;
            }

            if (ship.protection.protectedUntill > FrameExecutor.currentFrame) {
                if (damage.Type != MyDamageType.Grind) {
                    damage.Amount =0;
                    damage.IsDeformation = false;
                    return;
                } else {
                    if (!damage.isFriend(block)) {
                        damage.Amount = 0;
                        damage.IsDeformation = false;
                        return;
                    }
                }
            }
        }


        public static void checkExpload (IMySlimBlock block, ref MyDamageInformation info) {
            var fat = block.FatBlock;
            if (fat == null) return;
            if (!MyAPIGateway.Session.IsServer) return;

            var def = block.BlockDefinition as MyCubeBlockDefinition;
            if (!(block.Integrity - info.Amount < block.MaxIntegrity * def.CriticalIntegrityRatio)) return;
            
            var reactor = fat as IMyReactor;
            if(reactor != null && reactor.IsWorking && reactor.Enabled){
                float dmg = (float)Math.Pow (reactor.CurrentOutput, 1.6);
                var radius =  block.FatBlock.Model.BoundingSphere.Radius * 2.1f;
                info.Amount = block.MaxIntegrity;
                Explode (fat, dmg, radius, false);
                return;
            }

            var gasBlock = fat as IMyGasTank;
            if (fat is IMyGasTank){
                if ((block.BlockDefinition as MyGasTankDefinition).StoredGasId.SubtypeName == "Oxygen") return;
                float dmg = (float)Math.Pow(gasBlock.Capacity * (float)gasBlock.FilledRatio, 0.65d);
                if (dmg > 50) {
                    var radius =  block.FatBlock.Model.BoundingSphere.Radius * 1.85f;
                    info.Amount = block.MaxIntegrity;
                    Explode (gasBlock, dmg, radius, false);
                }
            }

            var jumpBlock = fat as IMyJumpDrive;
            if (jumpBlock != null){
                float dmg = (float)Math.Pow(jumpBlock.CurrentStoredPower * 10000, 0.85d);
                if (dmg > 50) {
                    var radius =  block.FatBlock.Model.BoundingSphere.Radius * 2.5f;
                    info.Amount = block.MaxIntegrity;
                    Explode (jumpBlock, dmg, radius, false);
                }
            }

        }


        public static void Explode (IMyCubeBlock block, float damage, float radius, bool effects) {
            var pos = block.GetPosition();
            radius = Math.Min(35, radius);
            MyAPIGateway.Utilities.InvokeOnGameThread (() => {
                if (effects) {
                    MyParticleEffect explosionEffect = null;
                    MyParticlesManager.TryCreateParticleEffect(1047, out explosionEffect, false);
                    if (explosionEffect != null){
					    explosionEffect.WorldMatrix = block.WorldMatrix;
					    explosionEffect.UserScale = radius / 8f;
                    }
                }

                BoundingSphereD sphere = new BoundingSphereD(pos, radius);
                MyExplosionInfo bomb = new MyExplosionInfo(damage, damage, sphere, MyExplosionTypeEnum.BOMB_EXPLOSION, true, true);
                MyExplosions.AddExplosion(ref bomb, true);
            });
            
        }
    }
}

/*
 * static MyInventory tempInventory = new MyInventory(4000f, Vector3D.One, MyInventoryFlags.CanReceive | MyInventoryFlags.CanSend);
 * public static void protectFromFallingComponents (IMySlimBlock block, ref MyDamageInformation damage, IMyEntity attacker) {
if (damage.Type != MyDamageType.Grind) return;
//if (!damage.isFriend(block)) return;

var bl = (attacker as IMyCubeBlock);
if (bl != null) {

} else {
    var player = damage.getDamageDealer();
    var pp = Other.GetPlayer (player);
    if (pp != null && pp.Character != null) {
        var ch = pp.Character;
        var mlt = (block.BlockDefinition  as MyCubeBlockDefinition).IntegrityPointsPerSec;
        var realDmg = mlt * damage.Amount*3;

        if (block.Integrity - realDmg < 0) {
            block.MoveItemsFromConstructionStockpile (tempInventory);
            var left = ch.GetInventory().MaxVolume - ch.GetInventory().CurrentVolume;
            if (left - tempInventory.CurrentVolume < 0) {
                    damage.Amount = damage.Amount / 20;
                             
            }
            block.MoveItemsToConstructionStockpile (tempInventory);
        }
    } 
}

}*/