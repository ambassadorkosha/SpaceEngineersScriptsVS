using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Digi;
using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Scripts.Specials.Messaging;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using ObjectBuilders.SafeZone;
using SpaceEngineers.Game.ModAPI;
using ServerMod;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Shared;
using Sandbox.Game.Entities.Cube;
using Scripts.Specials.POI;

namespace Scripts.Specials.Safezones
{
    /*
    Тублер AutoEnable: проверяет свое состояние раз в CHECK_INTERVAL, и всегда включается если соответсвует условиям:
    Условия:
    ForbiddenPlaces : Dictionary <Vector3, float (distance)> - список запрещённых мест с расстояниями в радиусе которых нельзя активировать
    Minimal distance: Скан в радиусе на наличие живых игроков (так же в кокпитах) (бегай по всем игрокам)
    Minimal other safezones: Скан в радиусе на наличие других активных сейфзон
    */
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SafeZoneBlock), false, new string[] { "SafeZone", "SafeZoneT1", "SafeZoneT2", "SafeZoneT3", "SafeZoneT4", "SafeZoneT5", "SafeZoneT6", "SafeZoneT7","FactionSafeZoneBlock" })]
    class CustomSafeZoneLogic : MyGameLogicComponent
    {
        private static bool m_controlsCreated;
        private static readonly int MIN_FRIEND_SF_DISTANCE = 3000;
        private static readonly int MIN_OTHER_ENEMY_SF_DISTANCE = 6000;
        private static readonly int MIN_ENEMY_PLAYER_DISTANCE = 5000;
        private static readonly ulong CHECK_RATIO = 15; //x15 slowdown
        private static readonly int m_distance_friend_sf_sqr = MIN_FRIEND_SF_DISTANCE * MIN_FRIEND_SF_DISTANCE;
        private static readonly int m_distance_other_enemy_sf_sqr = MIN_OTHER_ENEMY_SF_DISTANCE * MIN_OTHER_ENEMY_SF_DISTANCE;
        private static readonly int m_distance_enemy_player__sqr = MIN_ENEMY_PLAYER_DISTANCE * MIN_ENEMY_PLAYER_DISTANCE;
        private static readonly List<string> AFFECTED_SUBTYPES = new List<string>() { "SafeZone", "SafeZoneT1", "SafeZoneT2", "SafeZoneT3", "SafeZoneT4", "SafeZoneT5", "SafeZoneT6", "SafeZoneT7","FactionSafeZoneBlock" };
        private static readonly List<RestictedArea> RESTRICTED_AREAS = new List<RestictedArea>() { new RestictedArea(new Vector3D(0, 0, 0), "AdminZone", 15000f * 15000f) };

        private IMySafeZoneBlock m_block;
        private bool m_lastDetectedState;
        private ulong m_timer;
        private SafeZoneCheckResult m_tmpResult;
        private List<IMyPlayer> m_tempPlayersList = new List<IMyPlayer>();

        public bool IsAutoEnable;
        public static Sync<bool, CustomSafeZoneLogic> sync;
        #region overrides
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (sync == null)
            {
                sync = new Sync<bool, CustomSafeZoneLogic>(55516, sz => sz.IsAutoEnable, (sz, newsettings, PlayerSteamId, isFromServer) =>
                {
                    // if (MyAPIGateway.Multiplayer.IsServer) return;
                    //Log.Info($"Sync handler hit, is Server: {MyAPIGateway.Multiplayer.IsServer}");
                    sz.IsAutoEnable = newsettings;
                    //sz.UpdateGUI();
                });
            }

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                // base.UpdateOnceBeforeFrame();
                m_block = Entity as IMySafeZoneBlock;
                // bool enableAllowed = CanBeEnabled(out m_tmpResult);
                // TryDisableSafeZone(enableAllowed);
                m_lastDetectedState = m_block.IsSafeZoneEnabled();
                NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
            else
            {
                if (!m_controlsCreated)
                {
                    CustomSaveZoneControls.CreateControls();
                    m_controlsCreated = true;
                }
                sync.RequestData(Entity.EntityId);
            }
        }

        public override void UpdateAfterSimulation100()
        {
            bool currentState = m_block.IsSafeZoneEnabled();
            bool enableAllowed = CanBeEnabled(out m_tmpResult);
            if (currentState && !m_lastDetectedState)
            {
                m_lastDetectedState = !TryDisableSafeZone();
                return;
            }
            else
            {
                if ((m_timer++ % CHECK_RATIO) == 0)
                {
                    if (!currentState && IsAutoEnable && enableAllowed)
                    {
                        Log.Info($"CustomSafeZoneLogic.UpdateAfterSimulation100() zone auto enabled!");
                        m_block.EnableSafeZone(true);
                    }
                }
            }

            m_lastDetectedState = currentState;
        }
        #endregion
        /// <summary>
        /// Returns true if disabled
        /// </summary>
        /// <returns></returns>
        private bool TryDisableSafeZone()
        {
            switch (m_tmpResult)
            {
                case SafeZoneCheckResult.EnemyPlayer:
                    Common.ShowNotificationForAllInRange("You trying to activate safezone close to enemy. Minimal distance : " + MIN_ENEMY_PLAYER_DISTANCE + " meters.", 5000, m_block.GetPosition(), 5000, "Red");
                    break;
                case SafeZoneCheckResult.EnemySafeZone:
                    Common.ShowNotificationForAllInRange("Conflicting enemy safezones. Minimal distance : " + MIN_OTHER_ENEMY_SF_DISTANCE + " meters.", 5000, m_block.GetPosition(), 5000, "Red");
                    break;
                case SafeZoneCheckResult.FriendSafeZone:
                    Common.ShowNotificationForAllInRange("Conflicting friendly safezones. Minimal distance : " + MIN_FRIEND_SF_DISTANCE + " meters.", 5000, m_block.GetPosition(), 5000, "Red");
                    break;
                case SafeZoneCheckResult.RestrictedArea:
                    Common.ShowNotificationForAllInRange($"You trying to activate safezone in restricted zone.", 5000, m_block.GetPosition(), 5000, "Red");
                    break;
                case SafeZoneCheckResult.Ok:
                    Common.ShowNotificationForAllInRange($"Safe zone will be enabled.", 5000, m_block.GetPosition(), 5000, "Red");
                    return false;
                default:
                    break;
            }
            m_block.EnableSafeZone(false); // works lol
            m_block.Enabled = false;
            return true;
        }

        /// <summary>
        /// Check rules for enable here
        /// </summary>
        private bool CanBeEnabled(out SafeZoneCheckResult checkResult)
        {
            var block_pos = m_block.GetPosition();
            long thisblockid = m_block.EntityId;
            long ownerofblock = Relations.GetOwnerOrBuilder(m_block);
            IMyFaction myFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerofblock);
            //restricted area check
            foreach (RestictedArea rzone in RESTRICTED_AREAS)
            {
                if ((rzone.Center - block_pos).LengthSquared() < rzone.RadiusSqr)
                {
                    checkResult = SafeZoneCheckResult.RestrictedArea;
                    return false;
                }
            }
            //players check
            m_tempPlayersList.Clear();
            MyAPIGateway.Multiplayer.Players.GetPlayers(m_tempPlayersList);
            foreach (var p in m_tempPlayersList) //maybe parallel?
            {
                if (p.PromoteLevel >= MyPromoteLevel.SpaceMaster) continue; //ignore admins.

                if (MyIDModule.GetRelationPlayerPlayer(p.Identity.IdentityId, ownerofblock) == VRage.Game.Entity.MyRelationsBetweenPlayers.Enemies)
                {
                    if ((p.GetPosition() - block_pos).LengthSquared() < m_distance_enemy_player__sqr)
                    {
                        checkResult = SafeZoneCheckResult.EnemyPlayer;
                        return false;
                    }
                }
            }
            //other safezone check
            foreach (MySafeZone sf in MySessionComponentSafeZones.SafeZones)
            {
                if (sf == null || sf.MarkedForClose || !sf.Enabled || sf.SafeZoneBlockId == thisblockid) continue;

                var d = (block_pos - sf.PositionComp.GetPosition()).LengthSquared();
                if (IsSafeZoneFriendly(m_block.OwnerId, sf))
                {
                    if (d < m_distance_friend_sf_sqr)
                    {
                        checkResult = SafeZoneCheckResult.FriendSafeZone;
                        return false;
                    }
                }
                else
                {
                    if (d < m_distance_other_enemy_sf_sqr)
                    {
                        checkResult = SafeZoneCheckResult.EnemySafeZone;
                        return false;
                    }
                }
            }

            if (!POICore.CanEnableSafezone(block_pos))
            {
                checkResult = SafeZoneCheckResult.RestrictedArea;
                return false;
            }

            checkResult = SafeZoneCheckResult.Ok;
            return true;
        }

        #region helpers
        public bool CheckBoxEnabled(IMyTerminalBlock block)
        {
            if (block.BuiltBy() == MyAPIGateway.Session.Player.IdentityId) return true;

            return false;
        }

        /// <summary>
        /// Draw my controls only on my block
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public bool Visibility(IMyTerminalControl control, IMyTerminalBlock block)
        {
            try
            {
                if (!CustomSafeZoneLogic.AFFECTED_SUBTYPES.Contains(block.BlockDefinition.SubtypeId))
                {
                    return false; //if not our block hide all
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.ChatError($"Visibility() [controls] Exception: \n{ex}");
                return true;
            }
        }

        private bool IsSafeZoneFriendly(long player, MySafeZone zone)
        {
            if (zone.SafeZoneBlockId == 0) return false;
            IMyEntity sfblock;
            if (MyAPIGateway.Entities.TryGetEntityById(zone.SafeZoneBlockId, out sfblock))
            {
                var block = sfblock as IMySafeZoneBlock;
                if (Relations.GetRelationToBuilder(block.SlimBlock, player) == 4)
                {
                    // 4 - enemies
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
}
