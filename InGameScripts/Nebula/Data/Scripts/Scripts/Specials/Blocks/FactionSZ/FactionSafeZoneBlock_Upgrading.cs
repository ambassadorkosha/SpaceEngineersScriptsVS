using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Scripts.Specials.Messaging;
using ServerMod;
using Slime;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials.Blocks
{
    partial class FactionSafeZoneBlock
    {

        const byte TYPE_CREATE_SZ = 3;
        const byte TYPE_UPGRADE = 4;
        const byte TYPE_MODIFY_LOOK = 5;

        public override void ApplyDataFromClient(FactionSZSettings arrivedSettings, ulong playerId, byte type)
        {
            var identity = playerId.Identity();
            if (identity == null) return;

            var ident = identity.IdentityId;
            var builtBy = block.BuiltBy();

            var f = builtBy.PlayerFaction();
            if (f == null)
            {
                Common.SendChatMessage($"Only leaders can access faction core {builtBy} - faction is null", "Faction Core", ident);
                return;
            }

            var f2 = ident.PlayerFaction();
            if (f != f2) {
                Log.ChatError("ApplyDataFromClient:Wrong access error:" + type + " " + playerId);
                return;
            }

            if (!f.IsLeader(ident) && !f.IsFounder (ident))
            {
                Common.SendChatMessage("Only leaders can access faction core", "Faction Core", ident);
                return;
            }

            var u = arrivedSettings.Upgrades;
            switch (type)
            {
                case TYPE_CREATE_SZ:
                    CreateSZ();
                    break;
                case TYPE_UPGRADE:
                    Log.ChatError("ApplyDataFromClient:TryUpgrade:" + type + " " + playerId);
                    TryUpgrade(Settings.Upgrades, u, playerId);
                    break;
                case TYPE_MODIFY_LOOK:
                    ModifyLook (arrivedSettings);
                    break;
            }
        }

        private void ModifyLook (FactionSZSettings arrivedSettings)
        {
            var sz = SafeZone;
            if (sz == null) return;

            var ob = (MyObjectBuilder_SafeZone)sz.GetObjectBuilder();
            var c = arrivedSettings.SafezoneColor;


            ob.Shape = arrivedSettings.SafezoneIsSphere ? MySafeZoneShape.Sphere : MySafeZoneShape.Box;
            ob.Radius = Settings.Upgrades.GetSZRadius();
            ob.Size = new Vector3(Settings.Upgrades.GetSZRadius(), Settings.Upgrades.GetSZRadius(), Settings.Upgrades.GetSZRadius());
            ob.Texture = arrivedSettings.SafezoneTexture;
            ob.ModelColor = new VRage.SerializableVector3(c.R, c.G, c.B);

            MySessionComponentSafeZones.UpdateSafeZone(ob, true);
        }

        public void TryUpgrade(SZUpgrades now, SZUpgrades nnew, ulong player)
        {
            var identity = player.Identity();
            if (identity == null) return;

            Log.ChatError("TryUpgrade:Radius:" + nnew.SZRadius + " " + now.SZRadius + " " + (now.SZRadius + 1 <= 12));

            if (nnew.SZRadius > now.SZRadius && now.SZRadius + 1 <= 12)
            {
                
                var cost = GetUpgradeCost(now.SZRadius + 1, SZUpgrades.TYPE_RADIUS);
                if (!TryUpgrade(cost, player)) return;

                now.SZRadius++;

                var sz = SafeZone;
                if (sz != null)
                {
                    var ob = (MyObjectBuilder_SafeZone)sz.GetObjectBuilder();
                    ob.Radius = now.GetSZRadius();
                    ob.Size = new Vector3(now.GetSZRadius(), now.GetSZRadius(), now.GetSZRadius());
                    MySessionComponentSafeZones.UpdateSafeZone_Implementation(ob);
                }

                NotifyAndSave();
                Common.SendChatMessage("Radius has been upgraded!", "FactionSZ", identity.IdentityId);
                return;
            }

            if (nnew.ZoneProtection > now.ZoneProtection && now.ZoneProtection + 1 <= 12)
            {
                var cost = GetUpgradeCost(now.ZoneProtection + 1, SZUpgrades.TYPE_ZONEPROTECTION);
                if (!TryUpgrade(cost, player)) return;


                now.ZoneProtection++;
                NotifyAndSave();
                Common.SendChatMessage("Zone Protection has been upgraded!", "FactionSZ", identity.IdentityId);
                return;
            }


            if (nnew.PCUPerPlayer > now.PCUPerPlayer && now.PCUPerPlayer + 1 <= 12)
            {
                var cost = GetUpgradeCost(now.PCUPerPlayer + 1, SZUpgrades.TYPE_ZONEPROTECTION);
                if (!TryUpgrade(cost, player)) return;


                now.PCUPerPlayer++;


                var faction = block.BuiltBy().PlayerFaction();
                foreach (var x in faction.Members)
                {
                    var pid = x.Value.PlayerId;
                    var ident = pid.As<IMyIdentity>();
                    ///MyBlockLimits.
                }

                NotifyAndSave();
                Common.SendChatMessage("Zone Protection has been upgraded!", "FactionSZ", identity.IdentityId);
                return;
            }

        }

        private bool TryUpgrade(Dictionary<MyDefinitionId, double> cost, ulong player)
        {
            var identity = player.Identity();
            if (identity == null) return false;

            var inventories = new List<IMyInventory>();
            var hasResouces = new Dictionary<MyDefinitionId, double>();

            foreach (var x in block.SlimBlock.Neighbours)
            {
                var f = x.FatBlock;
                if (f != null && f.GetInventory() != null && f.GetInventory().ItemCount > 0 && !inventories.Contains(f.GetInventory()))
                {
                    f.GetInventory().CountItemsD(hasResouces);
                    inventories.Add(f.GetInventory());
                }
            }

            foreach (var x in cost)
            {
                if (!hasResouces.ContainsKey(x.Key) || hasResouces[x.Key] < cost[x.Key])
                {
                    Common.SendChatMessage($"You dont have enough resouces, to upgrade to next level: {cost[x.Key]} : {x.Key.toHumanString()}", "Safezone", identity.IdentityId);
                    return false;
                }
            }

            foreach (var x in inventories)
            {
                x.RemoveAmount(cost);
            }

            return true;
        }

        public Dictionary<MyDefinitionId, double> GetUpgradeCost(int lvl, int type)
        {
            var id = MyDefinitionId.Parse("MyObjectBuilder_Component/CompT" + (lvl < 10 ? "0" : "") + lvl);
            var dict = new Dictionary<MyDefinitionId, double>();
            dict.Add(id, 400);
            return dict;
        }

        public void OnRadiusUpdated()
        {
            var sz = SafeZone;
            if (sz != null)
            {
                var ob = (MyObjectBuilder_SafeZone)sz.GetObjectBuilder();
                ob.Radius = Settings.Upgrades.GetSZRadius();
                MySessionComponentSafeZones.UpdateSafeZone_Implementation(ob);
            }
        }

    }
}
