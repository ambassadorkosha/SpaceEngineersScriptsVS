using Digi;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials.Safezones;
using ServerMod;
using Slime;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Linq;
using VRage.Game.ModAPI;

namespace Scripts.Specials
{
    class RefillEnergy : Action1<long> {
        AutoTimer at = new AutoTimer(30); //frames

        public static void Init () {
            if (!MyAPIGateway.Session.isTorchServer()) {
                FrameExecutor.addFrameLogic (new RefillEnergy());
            }

            if (MyAPIGateway.Session.IsServer) {
                MyAPIGateway.Multiplayer.RegisterMessageHandler (23499, HandleRefillEnergyRequest);
                MyAPIGateway.Multiplayer.RegisterMessageHandler (23498, HandleForbiddenHydrogenRequest);
            }
        }

        public void run (long t) {
            var myPlayer = MyAPIGateway.Session.Player;
            if (myPlayer == null || myPlayer.Character == null) return;

            

            if (at.tick()) {
                var canRefill = myPlayer.PromoteLevel > MyPromoteLevel.Scripter && MyAPIGateway.Session.EnableCopyPaste;

                if (!canRefill) { 
                    foreach (var sf in MySessionComponentSafeZones.SafeZones) {
                        if (sf.Enabled && sf.SafeZoneBlockId != 0 && sf.Contains (myPlayer.Character.WorldMatrix.Translation)) {

                            var safezoneBlock = MyEntities.GetEntityByIdOrDefault(sf.SafeZoneBlockId) as IMySafeZoneBlock;
                            if (safezoneBlock == null) continue;
                        
                            var gl = safezoneBlock.GetAs<DungeonSafeZone>();
                            if (gl != null && !gl.canUseJetPack()) { //TODO Optimize
                                var data = new byte[16];
                                Bytes.Pack (data, 0, myPlayer.IdentityId);
                                Bytes.Pack (data, 8, sf.EntityId);
                                MyAPIGateway.Multiplayer.SendMessageToServer (23498, data);
                                return;
                            }
                        }
                    }
                }
                
                if (MyVisualScriptLogicProvider.GetPlayersEnergyLevel() < 0.5f || 
                    MyVisualScriptLogicProvider.GetPlayersOxygenLevel() < 0.6f || 
                    MyVisualScriptLogicProvider.GetPlayersHydrogenLevel() < 0.35f || 
                    HasBottlesWithHydroLevelLess(myPlayer.Character.GetInventory(), 1f)) {
                    long sfId = 0;
                    if (!canRefill) {
                        foreach (var sf in MySessionComponentSafeZones.SafeZones) {
                            if (sf.Enabled && sf.Contains (myPlayer.Character.WorldMatrix.Translation)) {
                                canRefill = true;
                                sfId = sf.EntityId;
                                break;
                            }
                        }
                    }

                    if (canRefill) {
                        var data = new byte[16];
                        Bytes.Pack (data, 0, myPlayer.IdentityId);
                        Bytes.Pack (data, 8, sfId);
                        SetBottlesLevel(myPlayer.Character.GetInventory(), 1f, true);
                        MyAPIGateway.Multiplayer.SendMessageToServer (23499, data);
                    }
                }
            }
        }

        public static void Close () {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler (23499, HandleRefillEnergyRequest);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler (23498, HandleForbiddenHydrogenRequest);
        }

        public static void HandleRefillEnergyRequest(byte[] data) {
            try {
                long playerId = BitConverter.ToInt64 (data, 0);
                long sfId = BitConverter.ToInt64 (data, 8);
                var pl = Other.GetPlayer (playerId);

                if (pl == null) {
                    Log.ChatError("Pl:" + sfId + " " + playerId);
                    return;
                }
                
                if (pl.PromoteLevel > MyPromoteLevel.Scripter) {
                    MyVisualScriptLogicProvider.SetPlayersEnergyLevel (playerId, 1f);
                    MyVisualScriptLogicProvider.SetPlayersHydrogenLevel (playerId, 1f);
                    MyVisualScriptLogicProvider.SetPlayersOxygenLevel (playerId, 1f);
                } else {    
                    var ch = pl.Character;
                    if (ch != null) { 
                        var sf = MyEntities.GetEntityById (sfId) as MySafeZone;
                        if (sf != null && sf.Enabled && sf.Contains(ch.WorldMatrix.Translation)) {
                            MyVisualScriptLogicProvider.SetPlayersEnergyLevel (playerId, 1f);
                            MyVisualScriptLogicProvider.SetPlayersHydrogenLevel (playerId, 1f);
                            MyVisualScriptLogicProvider.SetPlayersOxygenLevel (playerId, 1f);
                            SetBottlesLevel(ch.GetInventory(), 1f, true);
                        }
                    }
                }
            } catch (Exception e) {
                Log.Error(e);
            }
        }

        public static bool SetBottlesLevel(IMyInventory inventory, float level, bool firstOnly = true) {
            var items = inventory.GetItems(); //.GetItemAt(0).Value
            bool found = false;
            foreach (var x in items) {
                var bottle = x.Content as MyObjectBuilder_GasContainerObject;
                if (bottle == null) continue;
                var myOxygenContainerDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition(bottle) as MyOxygenContainerDefinition;
                if (myOxygenContainerDefinition != null && myOxygenContainerDefinition.StoredGasId.SubtypeName == "Hydrogen") {
                    bottle.GasLevel = level;
                    if (firstOnly) { return true; }
                    found = true;
                }
            }
            return found;
        }
        
        
        public static bool HasBottlesWithHydroLevelLess(IMyInventory inventory, float level, bool firstOnly = true) {
            var items = inventory.GetItems(); //.GetItemAt(0).Value
            foreach (var x in items) {
                var bottle = x.Content as MyObjectBuilder_GasContainerObject;
                if (bottle == null) continue;
                var myOxygenContainerDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition(bottle) as MyOxygenContainerDefinition;
                if (myOxygenContainerDefinition != null && myOxygenContainerDefinition.StoredGasId.SubtypeName == "Hydrogen") {
                    if (bottle.GasLevel < level) {
                        return true;
                    }
                    if (firstOnly) {
                        return false;
                    }
                }
            }
            return false;
        }
        

        public static void HandleForbiddenHydrogenRequest(byte[] data) {
            try { 
                var playerId = BitConverter.ToInt64 (data, 0);
                var sfId = BitConverter.ToInt64 (data, 8);
                var pl = Other.GetPlayer (playerId);
                var ch = pl.Character;
                if (ch == null) return;
                var sf = MyEntities.GetEntityById (sfId) as MySafeZone;
                if (sf == null || !sf.Enabled || !sf.Contains(ch.WorldMatrix.Translation)) return;
                MyVisualScriptLogicProvider.SetPlayersHydrogenLevel (playerId, 0f);
                SetBottlesLevel(ch.GetInventory(), 0f,false);
            } catch (Exception e) {
                Log.Error(e);
            }
        }
    }
}
