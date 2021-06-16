using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;
using ProtoBuf;
using Sandbox.ModAPI;
using Scripts.Shared;
using ServerMod;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Scripts.Specials.SlimGarage
{
    public class Communication
    {
        public const ushort NETWORK_ID = 10666;

        public Communication()
        {
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(NETWORK_ID, MessageHandler);
            SlimGarage.WriteToLogDbg("Register handlers");
        }

        public void UnregisterHandlers()
        {
            SlimGarage.WriteToLogDbg($"UnRegister handlers");
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(NETWORK_ID, MessageHandler);
        }

        private void MessageHandler(byte[] bytes)
        {
            try
            {
                var type = (MessageType)bytes[0];
                //SlimGarage.WriteToLogDbg($"Received message: {bytes[0]}: {type}");

                var data = new byte[bytes.Length - 1];
                Array.Copy(bytes, 1, data, 0, data.Length);

                switch (type)
                {
                    case MessageType.Notificaion:
                        OnNotificaion(data);
                        break;
                    case MessageType.ClientSettings:
                        OnClientSettings(data);
                        break;
                    case MessageType.SetBlockStatus:
                        OnSetBlockStatus(data);
                        break;
                    default:
                        return;
                }
            }
            catch (Exception ex)
            {
                SlimGarage.WriteToLogDbg($"Error during message handle! {ex}");
            }
        }

        #region Receive
        private void OnNotificaion(byte[] data)
        {
            var notificaion = MyAPIGateway.Utilities.SerializeFromBinary<Notification>(data);
            MyAPIGateway.Utilities.ShowNotification(notificaion.Message, notificaion.TimeoutMs, notificaion.Font);
        }

        private void OnSoundMessage(byte[] data)
        {
            //play sound on load/save
        }

        private void OnClientSettings(byte[] data)
        {
            // TODO secret key here
            var newsettings = MyAPIGateway.Utilities.SerializeFromBinary<ClientSettings>(data);
            //SlimGarage.WriteToLogDbg("OnClientSettings Works");
        }

        public delegate void OnBlockStatusHandler(BlockStatus s);

        public event OnBlockStatusHandler OnBlockStatusEvent;
        private void OnSetBlockStatus(byte[] data)
        {
            try
            {
                OnBlockStatusHandler handler = OnBlockStatusEvent;
                handler?.Invoke(MyAPIGateway.Utilities.SerializeFromBinary<BlockStatus>(data));
                //OnBlockStatusEvent(MyAPIGateway.Utilities.SerializeFromBinary<BlockStatus>(data));
            }
            catch (Exception ex)
            {
                SlimGarage.WriteToLogDbg("OnSetBlockStatus Exception " + ex.ToString());
            }
        }

#endregion
        #region Send
        public void SendSaveReq(ulong steamid, long entitiyid, long block_entid)
        {
            var msg = new ClientReq
            {
                GridId = entitiyid,
                SteamId = steamid,
                SecretKey = 1234,
                BLOCK_entityId = block_entid

            };
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(msg);
            SendToServer(Communication.MessageType.SaveGridReq, data);
        }

        public void SendLoadReq(ulong steamid, long entitiyid, long block_entid, MatrixD POSs, BoundingBoxD waabb)
        {
            var msg = new ClientReq
            {
                GridId = entitiyid,
                BLOCK_entityId = block_entid,
                SteamId = steamid,
                SecretKey = 1234,
                Pos = POSs.Translation,
                r_forward = POSs.Forward,
                r_up = POSs.Up,
                WAABB = waabb
            };
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(msg);
            SendToServer(Communication.MessageType.LoadGridReq, data);
        }
        public void SendClearReq(ulong steamid, long block_entid)
        {
            var msg = new ClientReq
            {
                BLOCK_entityId = block_entid,
                SteamId = steamid,
                SecretKey = 1234
            };
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(msg);
            SendToServer(Communication.MessageType.ClearGridReq, data);
        }
        public void SendShowReq(ulong steamid, long block_entid)
        {
            var msg = new ClientReq
            {
                BLOCK_entityId = block_entid,
                SteamId = steamid,
                SecretKey = 1234
            };
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(msg);
            SendToServer(Communication.MessageType.ShowGridReq, data);
        }
        public void SendSetStatusReq(ulong steamid, long block_entid, bool sharebuiler)
        {
            var msg = new BlockStatus
            {
                SteamId = steamid,
                BlockEntId = block_entid,
                ShareBuilder = sharebuiler,
                State = BlockState.None,
                Cooldowns = new Dictionary<string, DateTime>(),
                InvAccesGet = -1,
                InvAccesPut = -1
            };
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(msg);
            SendToServer(Communication.MessageType.SetBlockStatus, data);
        }

        public void SendGetSettingsFromServerReq()
        {
            try
            {
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(new ClientSettings
                {
                    SteamId = MyAPIGateway.Session.Player.SteamUserId,
                    Req = true
                });
                SendToServer(Communication.MessageType.ClientSettings, data); //settings req
            }
            catch (Exception e)
            {
                FrameExecutor.addDelayedLogic(20, (x) => SendGetSettingsFromServerReq());
            }
        }

        public void SendGetStatusFromServerReq(long blockid)
        {
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(new BlockStatus
            {
                SteamId = MyAPIGateway.Session.Player.SteamUserId,
                BlockEntId = blockid,
                State = BlockState.None,
                Cooldowns = new Dictionary<string, DateTime>(),
                InvAccesGet = -1,
                InvAccesPut = -1,
                ShareBuilder = false
            });
            SendToServer(Communication.MessageType.GetBlockStatus, data); //settings req
        }
        #endregion
        #region Helpers
        private void SendToServer(MessageType type, byte[] data)
        {
            var newData = new byte[data.Length + 1];
            newData[0] = (byte)type;
            data.CopyTo(newData, 1);
            //SlimGarage.WriteToLogDbg($"Sending message to server: {type}");
            MyAPIGateway.Utilities.InvokeOnGameThread(() => { MyAPIGateway.Multiplayer.SendMessageToServer(NETWORK_ID, newData); });
        }
        [ProtoContract]
        public enum MessageType : byte
        {
            ClientSettings,
            SaveGridReq,
            LoadGridReq,
            ClearGridReq,
            ShowGridReq,
            GetBlockStatus,
            SetBlockStatus,
            Notificaion,
            SoundMessage
        }
        #endregion
    }

    [ProtoContract]
    public struct Notification
    {
        [ProtoMember(1)]
        public int TimeoutMs;
        [ProtoMember(2)]
        public string Message;
        [ProtoMember(3)]
        public string Font;
    }

    [ProtoContract]
    public struct ClientReq
    {
        [ProtoMember(1)]
        public ulong SecretKey;
        [ProtoMember(2)]
        public long GridId;
        [ProtoMember(3)]
        public ulong SteamId;
        [ProtoMember(4)]
        public long BLOCK_entityId;
        [ProtoMember(5)]
        public Vector3D Pos;
        [ProtoMember(6)]
        public Vector3D r_forward;
        [ProtoMember(7)]
        public Vector3D r_up;
        [ProtoMember(8)]
        public BoundingBoxD WAABB;
    }
    [ProtoContract]
    public struct ClientSettings
    {
        [ProtoMember(1)]
        public ulong SecretKey;
        [ProtoMember(2)]
        public ulong SteamId;
        [ProtoMember(3)]
        public bool Req;
    }

    [ProtoContract]
    public struct BlockStatus
    {
        [ProtoMember(1)]
        public ulong SteamId;
        [ProtoMember(2)]
        public long BlockEntId;
        [ProtoMember(3)]
        public Dictionary<string, DateTime> Cooldowns;
        [ProtoMember(4, IsRequired = true), System.ComponentModel.DefaultValue(BlockState.None)]
        public BlockState State;
        [ProtoMember(5)]
        public bool ShareBuilder;
        [ProtoMember(6)]
        public int InvAccesPut; //MyOwnershipShareModeEnum
        [ProtoMember(7)]
        public int InvAccesGet;//MyOwnershipShareModeEnum
        [ProtoMember(8)]
        public string SavedShipInfo; //>Name:[]Blocks:[] PCU:[] Mass:[] kg<
    }
}
