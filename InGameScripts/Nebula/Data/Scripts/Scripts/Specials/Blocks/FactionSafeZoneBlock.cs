using Digi;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials.Messaging;
using Scripts.Specials.POI;
using ServerMod;
using Slime;
using System;
using System.Collections.Generic;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Specials.Blocks
{
    [ProtoContract]
    public class FactionSZSettings
    {
        [ProtoMember (1)] public long SafezoneId = 0;
        [ProtoMember (2)] public SZUpgrades Upgrades = new SZUpgrades();
        [ProtoMember (3)] public bool Enabled = false;
        [ProtoMember (4)] public Vector3 CreatedAt = new Vector3 (float.NaN, float.NaN, float.NaN);
    }

    [ProtoContract]
    public class SZUpgrades
    {
        public const int TYPE_RADIUS = 1;
        public const int TYPE_SPEED = 2;
        public const int TYPE_YEILD = 3;
        public const int TYPE_EBOOST = 4;
        public const int TYPE_PCUPERPLAYER = 5;
        public const int TYPE_PCUPERBASE = 6;
        public const int TYPE_ZONEPROTECTION = 7;
        public const int TYPE_OREPRODUCTION = 8;

        [ProtoMember (1)] public int SZRadius = 1;
        [ProtoMember (2)] public int RefineSpeed = 1;
        [ProtoMember (3)] public int RefineYeild = 1;
        [ProtoMember (4)] public int EnergyBoost = 1;
        [ProtoMember (5)] public int PCUPerPlayer = 1;
        [ProtoMember (6)] public int PCUPerBase = 1;
        [ProtoMember (7)] public int ZoneProtection = 1;
        [ProtoMember (8)] public int PassiveOreProduction = 1;
   
        public SZUpgrades Clone ()
        {
            var szu = new SZUpgrades();

            szu.SZRadius = SZRadius;
            szu.RefineSpeed = RefineSpeed;
            szu.RefineYeild = RefineYeild;
            szu.EnergyBoost = EnergyBoost;
            szu.PCUPerPlayer = PCUPerPlayer;
            szu.PCUPerBase = PCUPerBase;
            szu.ZoneProtection = ZoneProtection;
            szu.PassiveOreProduction = PassiveOreProduction;

            return szu;
        }

        public int GetUpgradeLvl (int type)
        {
            switch (type)
            {
                case TYPE_RADIUS: return SZRadius;
                case TYPE_SPEED: return RefineSpeed;
                case TYPE_YEILD: return RefineYeild;
                case TYPE_EBOOST: return EnergyBoost;
                case TYPE_PCUPERPLAYER: return PCUPerPlayer;
                case TYPE_PCUPERBASE: return PCUPerBase;
                case TYPE_ZONEPROTECTION: return ZoneProtection;
                case TYPE_OREPRODUCTION: return PassiveOreProduction;
                default: throw new Exception("No such upgrade type");
            }
        }

        public void SetUpgradeLvl(int type, int level)
        {
            switch (type)
            {
                case TYPE_RADIUS: SZRadius = level; break;
                case TYPE_SPEED: RefineSpeed = level; break;
                case TYPE_YEILD: RefineYeild = level; break;
                case TYPE_EBOOST: EnergyBoost = level; break;
                case TYPE_PCUPERPLAYER: PCUPerPlayer = level; break;
                case TYPE_PCUPERBASE: PCUPerBase = level; break;
                case TYPE_ZONEPROTECTION: ZoneProtection = level; break;
                case TYPE_OREPRODUCTION: PassiveOreProduction = level; break;
                default: throw new Exception("No such upgrade type");
            }
        }


        public float GetSZRadius()
        {
            return GetBonus (30, 500, SZRadius);
        }

        public float GetProtectionRadius()
        {
            return GetBonus(30, 500, SZRadius);
        }

        private float GetBonus (float min, float max, int lvl)
        {
            return min + ((max - min) / 12f * lvl);
        }
    }

    public class FactionSZBlockSettings { }


    partial class FactionSafeZoneBlock {

        const byte TYPE_CREATE_SZ = 3; 
        const byte TYPE_UPGRADE = 4; 
        
        public override void ApplyDataFromClient(FactionSZSettings arrivedSettings, ulong playerId, byte type)
        {
            var identity = playerId.Identity();
            var builtBy  = block.BuiltBy();

            var f = builtBy.Faction();
            var f2 = identity.IdentityId.Faction();

            if (f != f2) return;


            if (!f.IsLeader(identity.IdentityId))
            {
                Common.SendChatMessage ("Only leaders can access faction core", "Faction Core", identity.IdentityId);
                return;
            }

            var u = arrivedSettings.Upgrades;
            switch (type)
            {
                case TYPE_CREATE_SZ: 
                    CreateSZ();
                    break;
                case TYPE_UPGRADE:
                    TryUpgrade (Settings.Upgrades, u, playerId);
                    break;
            }
        }

        public void TryUpgrade (SZUpgrades now, SZUpgrades nnew, ulong player)
        {
            var identity = player.Identity();
            if (identity == null) return;

            if (nnew.SZRadius > now.SZRadius && now.SZRadius + 1 <= 12)
            {
                var cost = GetUpgradeCost (now.SZRadius + 1, SZUpgrades.TYPE_RADIUS);
                if (!TryUpgrade(cost, player)) return;

                now.SZRadius++;

                var sz = SafeZone;
                if (sz != null)
                {
                    var ob = (MyObjectBuilder_SafeZone)sz.GetObjectBuilder();
                    ob.Radius = now.GetSZRadius();
                    ob.Size = new Vector3(now.GetSZRadius(), now.GetSZRadius(), now.GetSZRadius());
                    MySessionComponentSafeZones.UpdateSafeZone (ob, true);
                }
                
                NotifyAndSave();
                Common.SendChatMessage ("Radius has been upgraded!", "FactionSZ", identity.IdentityId);
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
        }

        private bool TryUpgrade (Dictionary<MyDefinitionId, double> cost, ulong player)
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
                x.RemoveAmount (cost);
            }

            return true;
        }

        public Dictionary<MyDefinitionId, double> GetUpgradeCost (int lvl, int type)
        {
            var id = MyDefinitionId.Parse("MyObjectBuilder_Component/CompT" + (lvl < 10 ? "0" : "") + lvl);
            var dict = new Dictionary<MyDefinitionId, double>();
            dict.Add (id, 400);
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

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] { "FactionSafeZoneBlock22" })]
    partial class FactionSafeZoneBlock : GameLogicWithSyncAndSettings<FactionSZSettings, FactionSZBlockSettings, FactionSafeZoneBlock>
    {
        private static Sync<FactionSZSettings, FactionSafeZoneBlock> Sync;
        private static Guid Guid = new Guid("ee351845-e23e-4c96-84eb-c845c4a99c24");
        private static MyConcurrentList<FactionSafeZoneBlock> AllBlocks = new MyConcurrentList<FactionSafeZoneBlock>();

        private IMyFunctionalBlock block;
        private MySafeZone m_safeZone;

        private MySafeZone SafeZone {
            get {
                if (m_safeZone != null)
                {
                    return m_safeZone;
                }

                if (Settings.SafezoneId == 0) return null;

                m_safeZone = Settings.SafezoneId.As<MySafeZone>();
                return m_safeZone;
            }
            set {
                m_safeZone = value;
            }
        }

        public static void Init()
        {
            Sync = new Sync<FactionSZSettings, FactionSafeZoneBlock>(48821, (x)=>x.Settings, Handler);
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = Entity as IMyFunctionalBlock;
            NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            block.OnMarkForClose += Block_OnMarkForClose;
            
            if (MyAPIGateway.Session.IsServer)
            {
                block.CubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;
            }
        }

        private void CubeGrid_OnPhysicsChanged(VRage.ModAPI.IMyEntity obj)
        {
            if (!block.CubeGrid.IsStatic)
            {
                OnBlockDestroyed();
            }
        }

        private void Block_OnMarkForClose(VRage.ModAPI.IMyEntity obj)
        {
            var sz = SafeZone;
            if (sz != null)
            {
                sz.Close();
            }
            AllBlocks.Remove(this);
        }

        public override void UpdateOnceBeforeFrame()
        {
            CreateSZ();

            base.UpdateOnceBeforeFrame();
        }

        


        private bool CanPlaceNewSafezoneHere (Vector3 pos, FactionSafeZoneBlock exclude)
        {
            foreach (var x in AllBlocks)
            {
                if (x == exclude) continue;
                if (!x.Settings.Enabled) continue;

                var r = x.Settings.Upgrades.GetProtectionRadius();
                if ((x.block.WorldMatrix.Translation - pos).LengthSquared() < r * r)
                {
                    return false;
                }
            }

            foreach (var x in MySessionComponentSafeZones.SafeZones)
            {
                if (!x.Enabled) continue;

                if ((x.WorldMatrix.Translation - pos).LengthSquared() < 500*500)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Protect (IMySlimBlock block, ref MyDamageInformation info)
        {
            var fat = block.FatBlock;
            if (fat == null) return false;

            var fszb = fat.GetAs<FactionSafeZoneBlock>();
            if (fszb == null) return false;


            if (!fszb.IsGrantingProtection()) return false;
            if (!info.isByHandGrinder()) return false;

            var f = fat.SlimBlock.BuiltBy.Faction();
            if (f == null) return false;
            if (info.getDamageDealer() == f.FounderId) return false;

            info.Amount = 0;
            info.IsDeformation = false;
            return true;
        }


        public bool IsGrantingProtection ()
        {
            if (Settings.SafezoneId == 0) return false;
            if (!Settings.Enabled) return false;
            return true;
        }

        private void NotifyAll (string text)
        {
            var pos = block.WorldMatrix.Translation;
            Common.ShowNotificationForAllInRange(text, 5000, pos, 10000, "Red");
        }

        public void CreateSZ ()
        {
            if (!MyAPIGateway.Session.IsServer)
            {
                return;
            }
            if (((MyCubeBlock)block).IsPreview)
            {
                return;
            }

            var pos = block.WorldMatrix.Translation;
            var builtBy = block.SlimBlock.BuiltBy;
            var faction = builtBy.PlayerFaction();

            if (!block.CubeGrid.IsStatic)
            {
                NotifyAll ("You can't enable grid on dynamic grid");
                return;
            }

            if (faction == null)
            {
                NotifyAll("You can't use that block without faction");
                return;
            }

            if (faction.FounderId == builtBy)
            {
                NotifyAll("Only Founder can place this block");
                return;
            }

            if (!CanPlaceNewSafezoneHere(block.WorldMatrix.Translation, this))
            {
                NotifyAll("Current zone is protected by other block");
                return;
            }

            if (!POICore.CanEnableSafezone(pos, 500f))
            {
                NotifyAll("Current zone is protected by administration");
                return;
            }

            var ent = (MySafeZone)MySessionComponentSafeZones.CrateSafeZone(block.WorldMatrix, 
                MySafeZoneShape.Sphere, 
                MySafeZoneAccess.Whitelist, 
                new long[0], new long[] { faction.FactionId }, 
                Settings.Upgrades.GetSZRadius(), true, 
                safeZoneBlockId: block.EntityId);

            ent.Name = "FSZB_"+ block.EntityId;
            
            m_safeZone = ent;
            Settings.SafezoneId = ent.EntityId;
            Settings.Enabled = true;
            Settings.CreatedAt = pos;

            NotifyAndSave();
        }

        public void OnBlockDestroyed ()
        {
            
            if (Settings.SafezoneId != 0)
            {
                var sz = Settings.SafezoneId.As<MySafeZone>();
                if (sz != null)
                {
                    MySessionComponentSafeZones.RemoveSafeZone(sz);
                }
            }

            Settings.Upgrades = new SZUpgrades();
            Settings.Enabled = false;
            Settings.CreatedAt = new Vector3 (float.NaN, float.NaN, float.NaN);

            NotifyAndSave();
        }


        public override void InitControls()
        {
            base.InitControls();
            MyAPIGateway.TerminalControls.CreateSlider<FactionSafeZoneBlock, IMyUpgradeModule> ("FactionSafezoneBlock_Upgrade_Radius", "Radius", "Upgrade radius", 0, 13,
                (x) => x.Settings.Upgrades.SZRadius, 
                (x, sb) => sb.Append(x.Settings.Upgrades.SZRadius).Append(" lvl"),
                (x, y) => GUIRequestUpdate (x,y, SZUpgrades.TYPE_RADIUS));
        }

        public static void GUIRequestUpdate (FactionSafeZoneBlock block, float value, int type)
        {
            value = MathHelper.Clamp (value, 1, 12);
            var lvl = block.Settings.Upgrades.GetUpgradeLvl(type);
            if (lvl < value)
            {

            }
        }


        public override void Close()
        {
            OnBlockDestroyed();
            base.Close();
        }

        public override FactionSZSettings GetDefaultSettings() { return new FactionSZSettings(); }

        public override Guid GetGuid() { return Guid; }

        public override Sync<FactionSZSettings, FactionSafeZoneBlock> GetSync() { return Sync; }

        public override FactionSZBlockSettings InitBlockSettings() {  return new FactionSZBlockSettings(); }
    }
}
