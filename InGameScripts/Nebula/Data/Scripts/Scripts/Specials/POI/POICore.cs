using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using VRageMath;
using Scripts.Shared;
using VRage.Utils;
using VRage.Game.ModAPI;
using ServerMod;
using Digi;

namespace Scripts.Specials.POI
{
    /// <summary>
    /* POI
    - нет возможности включить сейфзону
    - запрет модификации вокселей через проброс в плагин(там написано) свзязку дописать
    - Список должен отображать GPS  у игроков, всегда при заходе выслать.(нельзя удалить)
    блок базы не должен работать в радиусе от POI(внести IF в блок базы)
    gps может не быть
    */
    /// </summary>
    public static class POICore
    {
        public const bool LOGS = true;
        public const ushort NETWORK_ID = 10668;
        private const int M_MAXID = 99;
        private static List<POI> m_pois = new List<POI>();
        private static Connection<Settings> m_connection = new Connection<Settings>(NETWORK_ID, HandleMessage);
        private static Random m_rand = new Random();

        public static void Init()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                LoadSettings();
            }
            else
            {
                m_connection.SendMessageToServer(new Settings() { MessageType = MessageType.Request });
            }

            if (!MyAPIGateway.Session.isTorchServer())
            {
                Commands.Init();
            }
        }

        public static void Close ()
        {
            
        }

        public static List<POI> GetPois()
        {
            return m_pois;
        }

        public static void Save()
        {
            using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("POISettings.xml", typeof(POICore)))
            {
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(POICore.m_pois));
            }
        }

        private static void HandleMessage(Settings data, ulong steamId, bool isFromServer)
        {
            if (!MyAPIGateway.Session.IsServer) //self sending
            {
                OnNewSettings(data.PoiList);
            } else {
                switch (data.MessageType)
                {
                    case MessageType.Request:
                        m_connection.SendMessageTo(new Settings() { PoiList = m_pois }, steamId);
                        break;
                    case MessageType.Create:
                        CreateServer(data);
                        break;
                    case MessageType.Delete:
                        RemoveServer(data);
                        break;
                    case MessageType.Modify:
                        ModifyServer(data);
                        break;
                    default:
                        return;
                }
            }
        }

        #region PublicChecks
        public static bool CanEnableSafezone(Vector3 Position, float distance = 0)
        {
            foreach (var x in m_pois)
            {
                if (x.CantEnableSafezone > 0 && (x.Position - Position).LengthSquared() + distance * distance < x.CantEnableSafezone * x.CantEnableSafezone)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CanCoreWork(Vector3 Position, float distance = 0)
        {
            foreach (var x in m_pois)
            {
                if (x.CantEnableBaseCores > 0 && (x.Position - Position).LengthSquared() + distance * distance < x.CantEnableBaseCores * x.CantEnableBaseCores)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CanMine(Vector3 Position, float distance = 0)
        {
            foreach (var x in m_pois)
            {
                if (x.CantMineDistance > 0 && (x.Position - Position).LengthSquared() + distance * distance < x.CantMineDistance * x.CantMineDistance)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool CanJumpHere(Vector3 position)
        {
            foreach (var x in m_pois)
            {
                if (x.CantJumpHere > 0 && ((x.Position - position).LengthSquared() < x.CantJumpHere * x.CantJumpHere))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region OnlyClient
        private static void OnNewSettings(List<POI> newpoilist)
        {
            if (newpoilist != null)
            {
                m_pois = newpoilist;
            }

            var playersGPSCache = new List<IMyPlayer>();
            Gps.RemoveWithDescription("~POI:");
            foreach (var poi in m_pois)
            {
                if (poi.Gps == null) continue;
                var gpsname = poi.Gps.GPSName;
                string gpsdesc = "~POI: " + poi.Id.ToString();
                Vector3D gpspos = poi.Gps.GPSPosition;
                Vector3D gpscolor = poi.Gps.GPSColor;
                Gps.AddGpsColored(gpsname, gpsdesc, gpspos, gpscolor);
            }
        }

        public static void RemoveAll()
        {
            m_connection.SendMessageToServer(new Settings() { MessageType = MessageType.Delete, PoiList = m_pois });
        }

        public static bool RemoveById(long id)
        {
            var filter = m_pois.Where(x => (x.Id == id));
            if (filter.Any())
            {
                var poi = filter.First();
                m_connection.SendMessageToServer(new Settings() { MessageType = MessageType.Delete, PoiList = new List<POI>() { poi } });
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool RemoveByName(string name)
        {
            var filter = m_pois.Where(x => x.Name.ToLower() == name.ToLower());
            if (filter.Any())
            {
                var poi = filter.First();
                m_connection.SendMessageToServer(new Settings() { MessageType = MessageType.Delete, PoiList = new List<POI>() { poi } });
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void Create(Vector3 Position, float CantMineDistance, float CantEnableSafezone, float CantEnableBaseCores, float CantJumpHere, string name)
        {
            var poi = new POI();
            poi.Position = Position;
            poi.CantMineDistance = CantMineDistance;
            poi.CantEnableSafezone = CantEnableSafezone;
            poi.CantEnableBaseCores = CantEnableBaseCores;
            poi.CantJumpHere = CantJumpHere;
            poi.Name = name;
            poi.Id = GenUnicId();
            m_connection.SendMessageToServer(new Settings() { PoiList = new List<POI>() { poi }, MessageType = MessageType.Create });
        }

        public static bool TrySetGPS(long id, Vector3D pos, Vector3D color, string name)
        {
            var result = m_pois.Where(x => x.Id == id);
            if (result.Count() < 1) return false;

            var poi = result.First();
            poi.Gps = new GPS() { GPSPosition = pos, GPSColor = color, GPSName = name };
            m_pois.RemoveAll(x => x.Id == id);
            m_pois.Add(poi);
            m_connection.SendMessageToServer(new Settings() { PoiList = new List<POI>() { poi }, MessageType = MessageType.Modify });
            return true;
        }

        public static string GetPOIList()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~POI's~~~~~~~~~~~~~~~~~~~~~~~~~~");
            int i = 1;
            foreach (var poi in m_pois)
            {
                sb.Append($"{i} :").AppendLine(poi.ToString());
            }

            return sb.ToString();
        }

        #endregion
        #region OnlyServer

        private static void CreateServer(Settings data)
        {
            var poi = data.PoiList.FirstOrDefault();
            if (poi != null)
            {
                m_pois.Add(poi);
            }
            BroadcastToAll();
        }

        private static void RemoveServer(Settings data)
        {
            foreach (var poi in data.PoiList)
            {
                m_pois.RemoveAll(x => x.Id == poi.Id);
            }
            BroadcastToAll();
        }

        private static void ModifyServer(Settings data)
        {
            if (data.PoiList.Count > 0)
            {
                var poi = data.PoiList[0];
                m_pois.RemoveAll(x => x.Id == poi.Id);
                m_pois.Add(poi);
            }

            BroadcastToAll();
        }

        private static void BroadcastToAll()
        {
            m_connection.SendMessageToOthers(new Settings() { PoiList = m_pois });
        }

        private static void LoadSettings()
        {
            try
            {
                m_pois = new List<POI>();
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage("POISettings.xml", typeof(POICore)))
                {
                    using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("POISettings.xml", typeof(POICore)))
                    {
                        m_pois = MyAPIGateway.Utilities.SerializeFromXML<List<POI>>(reader.ReadToEnd());
                    }
                }
                else
                {
                    Save();
                }
            }
            catch (Exception ex)
            {
                Log.ChatError("Exception while loading ModSettings, using default." + ex.ToString());
            }
        }

        #endregion

        private static long GenUnicId()
        {
            //with 100 poi player gps will die i think
            if (m_pois.Count >= M_MAXID) return -1;
            var id = m_rand.Next(1, M_MAXID);
            while (m_pois.Where(x => x.Id == id).Any())
            {
                id = m_rand.Next(1, M_MAXID);
            }

            return id;
        }
    }
}