using System;
using System.Collections.Generic;
using System.Text;

using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.ModAPI;
using Digi;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Weapons;
using Sandbox.Game.Weapons;
using VRage.Game.Entity;
using Scripts.Specials.Messaging;

namespace ServerMod {
    static class Relations {

        public const int MEMBERSHIP_NO_FACTION = -2;
        public const int MEMBERSHIP_NOT_MEMBER = -1;
        public const int MEMBERSHIP_APPLICANT = 0;
        public const int MEMBERSHIP_MEMBER = 1;
        public const int MEMBERSHIP_LEADER = 2;
        public const int MEMBERSHIP_FOUNDER = 3;

        public static int GetRelation(this IMySlimBlock block, long userId) {
            return MyIDModule.GetRelationPlayerBlock(userId, block.OwnerId, MyOwnershipShareModeEnum.Faction).AsNumber();
        }

        
        public static bool isOwnedByFactionLeader (this IMyCubeBlock block) {
            if (block.OwnerId == block.BuiltBy()) {
                var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction (block.BuiltBy());
                if (faction != null) {
                    return faction.FounderId == block.BuiltBy();
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }


        public static Dictionary<IMyPlayer, int> getOnlinePlayers (this IMyFaction faction, Dictionary<IMyPlayer, int> set,  List<IMyPlayer> pl = null) {
            if (pl == null) {
                pl = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers (pl, null);
            }

            foreach (IMyPlayer x in pl) {
                foreach (var y in faction.Members) {
                    if (y.Key == x.IdentityId) {
                        set.Set (x, y.Value.IsFounder ? 2 : y.Value.IsLeader ? 1 : 0);
                    }
                }
            } 

            return set;
        }

        public static bool isFactionLeaderOrFounder (long user) {
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction (user);
            if (faction != null) {
                return faction.getMemberShip (user) > 1;
            }
            return false;
        }

        public static int getFactionMemberShip (long user) {
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction (user);
            if (faction != null) {
                return faction.getMemberShip (user);
            }
            return MEMBERSHIP_NO_FACTION;
        }
        public static int getMemberShip (this IMyFaction faction, long user) {
            if (faction.FounderId == user) return MEMBERSHIP_FOUNDER;
            foreach (var x in faction.Members) {
                if (x.Key == user) {
                    if (x.Value.IsLeader) return MEMBERSHIP_LEADER;
                    return 1;
                }
            }

            if (faction.JoinRequests.ContainsKey(user)) return MEMBERSHIP_APPLICANT;
            return MEMBERSHIP_NOT_MEMBER;
        }


        public static IMyFaction getBuilderFaction (this IMyCubeBlock block) {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction (block.BuiltBy());
        }

        public static IMyFaction getFaction (this IMyPlayer pl) {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction (pl.IdentityId);
        }
        public static IMyFaction getFaction (long pl) {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction (pl);
        }
        public static IMyFaction getFaction (IMyIdentity pl) {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction (pl.IdentityId);
        }

        public static int GetRelation(long u1, long u2) {
            return MyIDModule.GetRelationPlayerPlayer(u1, u2).AsNumber();
        }

        public static int GetRelationToBuilder(this IMySlimBlock block, long userId) {
            return MyIDModule.GetRelationPlayerBlock(userId, block.BuiltBy, MyOwnershipShareModeEnum.Faction).AsNumber();
        }

        public static int GetRelationToOwnerOrBuilder(this IMySlimBlock block, long userId) {
            return MyIDModule.GetRelationPlayerBlock(userId,  block.OwnerId != 0 ? block.OwnerId : block.BuiltBy, MyOwnershipShareModeEnum.Faction).AsNumber();
        }

        public static int GetRelationToOwnerOrBuilder(this IMyCubeBlock block, long userId) {
            return MyIDModule.GetRelationPlayerBlock(userId,  block.OwnerId != 0 ? block.OwnerId : block.BuiltBy(), MyOwnershipShareModeEnum.Faction).AsNumber();
        }

        public static long GetOwnerOrBuilder (this IMySlimBlock block) {
            return block.OwnerId != 0 ? block.OwnerId : block.BuiltBy;
        }

        public static long GetOwnerOrBuilder (this IMyCubeBlock block) {
            return block.OwnerId != 0 ? block.OwnerId : block.BuiltBy();
        }

        public static int GetRelation(this IMyCubeGrid cubeGrid, long userId) {
            return GetUserRelation(cubeGrid, userId).AsNumber();
        }

        public static int AsNumber(this MyRelationsBetweenPlayerAndBlock relation) {
            if (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare) {
                return 1;
            } else if (relation == MyRelationsBetweenPlayerAndBlock.Enemies) {
                return -1;
            } else return 0;
        }

        public static int AsNumber(this MyRelationsBetweenPlayers relation) {
            if (relation == MyRelationsBetweenPlayers.Self) return 2;
            if (relation == MyRelationsBetweenPlayers.Allies) return 1;
            if (relation == MyRelationsBetweenPlayers.Enemies) return -1;
            return 0;
        }



        public static bool isEnemy(this IMyCubeGrid grid, long userId) {
            return grid.GetUserRelation(userId) == MyRelationsBetweenPlayerAndBlock.Enemies;
        }

        public static bool isEnemy(this IMyCharacter u, long userId) {
            return MyIDModule.GetRelationPlayerBlock(u.EntityId, userId, MyOwnershipShareModeEnum.Faction) == MyRelationsBetweenPlayerAndBlock.Enemies;
        }
        
        public static MyRelationsBetweenPlayerAndBlock GetUserRelation(this IMyCubeGrid cubeGrid, long userId) {
            var enemies = false;
            var neutral = false;
            try {
                foreach (var key in cubeGrid.BigOwners) {

                    var owner = MyAPIGateway.Entities.GetEntityById(key);
                    //Log.Info("Owner:" + owner);

                    var relation = MyIDModule.GetRelationPlayerBlock(key, userId, MyOwnershipShareModeEnum.Faction);
                    if (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.FactionShare) {
                        return relation;
                    } else if (relation == MyRelationsBetweenPlayerAndBlock.Enemies) {
                        enemies = true;
                    } else if (relation == MyRelationsBetweenPlayerAndBlock.Neutral) {
                        neutral = true;
                    }
                }
            } catch {
                //The list BigOwners could change while iterating -> a silent catch
            }
            if (enemies) return MyRelationsBetweenPlayerAndBlock.Enemies;
            if (neutral) return MyRelationsBetweenPlayerAndBlock.Neutral;
            return MyRelationsBetweenPlayerAndBlock.NoOwnership;
        }

        public static bool isAdminGrid(this IMyCubeGrid grid) {
            var y = MyAPIGateway.Multiplayer.Players;
            var pl = new List<IMyPlayer>();
            y.GetPlayers(pl, x => x.IsAdmin && grid.BigOwners.Contains(x.IdentityId));
            return pl.Count > 0;
        }

        public static long FindBot(String name) {
            var y = MyAPIGateway.Multiplayer.Players;
            var pl = new List<IMyIdentity>();
            y.GetAllIdentites(pl, x =>{ return x.DisplayName.Equals(name); });
            if (pl.Count == 1) {
                return pl[0].IdentityId;
            } else return 0L;
        }

        public static bool isFriend(this MyDamageInformation damage, IMySlimBlock block) {
            return damage.getRelation (block) == 1;
        }
        
        public static int getRelation(this MyDamageInformation damage, IMySlimBlock block) {
            var dealer = getDamageDealer (damage);
            if (dealer == 0) return 0;

            if (block.OwnerId != 0) return block.CubeGrid.GetRelation(dealer);
            else return block.GetRelationToBuilder(dealer);
        }

        public static bool isByHandGrinder (this MyDamageInformation damage) {
            var attacker = MyAPIGateway.Entities.GetEntityById(damage.AttackerId);
            var hnd = attacker as IMyEngineerToolBase;
            if (hnd != null) return true;
            var hnd2 = attacker as IMyHandheldGunObject<MyDeviceBase>;
            if (hnd2 != null) return true;

            return false;
        }

        public static long getDamageDealer (this MyDamageInformation damage) {
            var attacker = MyAPIGateway.Entities.GetEntityById(damage.AttackerId);
            var hnd = attacker as IMyEngineerToolBase;
            if (hnd != null) return hnd.GetToolOwner();
            var hnd2 = attacker as IMyHandheldGunObject<MyDeviceBase>;
            if (hnd2 != null) return hnd2.GetToolOwner();
            var pl = attacker as IMyCharacter;
            if (pl != null)  return Other.FindPlayerByCharacterId(pl.EntityId);
            var cb = attacker as IMySlimBlock;
            if (cb != null) return cb.OwnerId != 0 ? cb.OwnerId : cb.BuiltBy;
            var cb2 = attacker as IMyCubeBlock;
            if (cb2 != null) return cb2.OwnerId != 0 ? cb2.OwnerId : cb2.BuiltBy();

            return 0;
        }
        
        

        public static void OwnGrid(this IMyCubeGrid y, long transferTo, MyOwnershipShareModeEnum shareOptions) {
            y.ChangeGridOwnership(transferTo, shareOptions);
        }

        public static void OwnBlocks(this IMyCubeGrid y, long transferTo, MyOwnershipShareModeEnum shareOptions, Func<IMySlimBlock, bool> apply) { //MyOwnershipShareModeEnum.None
            var blocks = new List<IMySlimBlock>();
            y.GetBlocks(blocks);

            Log.Info("OwnBlocks:" + y.DisplayName + " : " + blocks.Count);

            foreach (var b in blocks) {
                var fat = b.FatBlock;
                if (fat != null) {
                    var own = apply(b);
                    if (own) {
                        if (fat is MyCubeBlock) {
                            (fat as MyCubeBlock).ChangeOwner(transferTo, shareOptions);
                            Log.Info("Change ownership:" + blocks.Count + " " + b.OwnerId + " " + " " + b.GetType().Name + " " + fat.GetType().Name + " " + (b is MyCubeBlock));
                        }
                    }
                }
            }
        }

        public static void OwnBlock(this IMySlimBlock b, long transferTo, MyOwnershipShareModeEnum shareOptions) {
            var fat = b.FatBlock;
            if (fat != null && fat is MyCubeBlock) {
                (fat as MyCubeBlock).ChangeOwner(transferTo, shareOptions);
            }
        }
    }
}
