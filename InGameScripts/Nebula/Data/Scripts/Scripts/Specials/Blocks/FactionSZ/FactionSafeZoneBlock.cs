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
            Common.ShowNotificationForAllInRange(text, 10000, pos, 10000, "Red");
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
                Settings.SafezoneIsSphere ? MySafeZoneShape.Sphere : MySafeZoneShape.Box, 
                MySafeZoneAccess.Whitelist, 
                new long[0], new long[] { faction.FactionId }, 
                Settings.Upgrades.GetSZRadius(), true, 
                safeZoneBlockId: block.EntityId);

            ent.Name = "FSZB_"+ block.EntityId;
            
            m_safeZone = ent;
            Settings.SafezoneId = ent.EntityId;
            Settings.Enabled = true;
            Settings.CreatedAt = pos;
            
            NotifyAll("Created");
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
