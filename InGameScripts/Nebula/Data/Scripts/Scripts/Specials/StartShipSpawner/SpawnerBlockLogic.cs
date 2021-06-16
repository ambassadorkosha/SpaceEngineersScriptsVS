using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Shared;
using ServerMod;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Scripts.Specials.StartShipSpawner
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false, new string[] { "SpawnerControlPanel" })]
    public class SpawnerBlockLogic : GameLogicWithSyncAndSettings<BlockSettings, int, SpawnerBlockLogic>
    {
        public const ushort NETWORK_ID = 10669;
        private static Sync<BlockSettings, SpawnerBlockLogic> m_sync;
        private IMyTerminalBlock m_block;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            m_block = (IMyTerminalBlock)Entity;
        }

        public override Guid GetGuid() { return new Guid("be990249-e5e4-4bcc-bdc4-57190a0efcaf"); }

        public override Sync<BlockSettings, SpawnerBlockLogic> GetSync() { return m_sync; }

        public override void ApplyDataFromClient(BlockSettings arrivedSettings, ulong userSteamId, byte type)
        {

            if (String.IsNullOrWhiteSpace(arrivedSettings.CurrentBehaviorName))
            {
                return;
            }

            if (type == (byte)MessageType.Request)
            {
                SpawnerSession.Instance.TrySpawn(m_block.GetPosition(), arrivedSettings.CurrentBehaviorName);
            }
            else
            {
                var temp = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(temp);
                if (temp.Where(x => x.SteamUserId == userSteamId && x.PromoteLevel >= MyPromoteLevel.Moderator).Any())
                {
                    Settings.CurrentBehaviorName = arrivedSettings.CurrentBehaviorName;
                    NotifyAndSave(); //send to all clients new settings
                }
            }
        }

        public override BlockSettings GetDefaultSettings()
        {
            return new BlockSettings() { CurrentBehaviorName = "Replaceme" };
        }

        public override int InitBlockSettings() { return 0; } //Not used

        public override void InitControls()
        {
            base.InitControls();
            InitMyControls();
        }

        public static void InitNetworking()
        {
            m_sync = new Sync<BlockSettings, SpawnerBlockLogic>(NETWORK_ID, (x) => x.Settings, Handler);
        }

        private static void InitMyControls()
        {
            //ComboboxLater
            var spawnAction = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("StartShipSpawner.Spawn");
            spawnAction.Action = (block) => { block.GetAs<SpawnerBlockLogic>()?.RequestSpawn(block); };
            spawnAction.Name = new StringBuilder("Spawn");
            spawnAction.Enabled = block => CanUse(block);  //For prevent action spam                      
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(spawnAction);

            var behaviorName = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyTerminalBlock>("StartShipSpawner.TextBox");
            //TextBox.Title = MyStringId.GetOrCompute("Grind ship");
            behaviorName.Tooltip = MyStringId.GetOrCompute("Write spawn behavior name here.");
            behaviorName.SupportsMultipleBlocks = false;
            behaviorName.Setter = (block, str) =>
            {
                var l = block.GetAs<SpawnerBlockLogic>();
                if (l != null)
                {
                    l.Settings.CurrentBehaviorName = str.ToString();
                    l.NotifyAndSave();
                }

            };
            behaviorName.Getter = (block) =>
            {
                var l = block.GetAs<SpawnerBlockLogic>();
                if (l != null)
                {
                    return new StringBuilder(l.Settings.CurrentBehaviorName);
                }
                else
                {
                    return new StringBuilder("NULL");
                }
            };
            behaviorName.Enabled = block => IsPromoted(); //only promoted can modify
            behaviorName.Visible = block => block.GetAs<SpawnerBlockLogic>() != null; //visible for all humans
            MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(behaviorName);
        }

        #region GUIControls
        private static bool IsPromoted()
        {
            try
            {
                if (MyAPIGateway.Session.LocalHumanPlayer.PromoteLevel > VRage.Game.ModAPI.MyPromoteLevel.Moderator)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                SpawnerSession.ToLog($"Visibility() [controls] Exception: \n{ex}");
                return true;
            }
        }

        private static bool CanUse(IMyTerminalBlock block)
        {
            try
            {
                var l = block.GetAs<SpawnerBlockLogic>();
                if (l == null)
                {
                    SpawnerSession.ToLog($"CanUse() Logic is null.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                SpawnerSession.ToLog($"CanUse() [controls] Exception: \n{ex}");
                return false;
            }
        }

        private void RequestSpawn(IMyTerminalBlock block)
        {
            try
            {
                var l = block.GetAs<SpawnerBlockLogic>();
                if (l == null)
                {
                    SpawnerSession.ToLog($"RequestSpawn() Logic is null.");
                    return;
                }

                var text = l.Settings.CurrentBehaviorName;
                var isServer = MyAPIGateway.Session.IsServer;
                if (isServer)
                {
                    SpawnerSession.Instance.TrySpawn(m_block.GetPosition(), text);
                }
                else
                {
                    m_sync.SendMessageToServer(m_block.EntityId, l.Settings, true, (byte)MessageType.Request);
                }
            }
            catch (Exception ex)
            {
                SpawnerSession.ToLog($"RequestSpawn failed, exception: [{ex}]");
            }
        }
        #endregion
    }
}