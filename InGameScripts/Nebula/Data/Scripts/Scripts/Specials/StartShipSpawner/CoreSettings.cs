using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using ProtoBuf;
using ServerMod;
using VRageMath;

namespace Scripts.Specials.StartShipSpawner
{
    [ProtoContract]
    public class BlockSettings
    {
        [ProtoMember(1)]
        public string CurrentBehaviorName;
    }

    public class CoreSettings
    {
        public List<SpawnBehaviorVariant> SpawnBehaviors;
        public MessageType MessageType;
        public List<CooldownForXML> SpawnCooldownsXMLList;
        public SpawnPlaces Places;
        [XmlIgnore]
        public Dictionary<long, DateTime> SpawnCooldowns;
    }

    public class SpawnBehaviorVariant
    {
        public Vector3D Position = Vector3D.Zero;
        public string BehaviorName = "replacemepls";
        public string Prefab = "replacemepls";
        public float MinBelowVoxels = 0;
        public float MaxBelowVoxels = 0;
        public float MinAboveVoxels = 0;
        public float MaxAboveVoxels = 0;
        public int CooldownInMinutes = 0;
        public float MinEnemyDistance = 0;
        public bool CanSpawnOnPlanets = true;
        public bool CanSpawnInSpace = true;
        public bool ResetIdentity = false;
        public int MaxPlayerRadius = 0;
    }

    public enum MessageType
    {
        Request,
        Modify
    }

    public class SpawnPlaces
    {
        public List<string> Planets = new List<string>();
        public List<SpawnPoint> SpawnPoints = new List<SpawnPoint>();

        public void GetAllSpawnPoints(List<SpawnPoint> buffer)
        {
            foreach (var x in Planets)
            {
                var pl = GameBase.GetPlanetWithGeneratorId(x);
                if (pl != null)
                {
                    var SpawnPoint = new SpawnPoint()
                    {
                        Center = pl.WorldMatrix.Translation,
                        Radius = pl.MaximumRadius,
                        IsPlanetOnly = true
                    };
                    SpawnerSession.ToLog($"Add planet to spawner at [{pl.WorldMatrix.Translation}] Radius: [{pl.MaximumRadius}] Name: [{x}]");
                    buffer.Add(SpawnPoint);
                }
            }
            buffer.AddList(SpawnPoints);
        }
    }

    public class SpawnPoint
    {
        public Vector3 Center = Vector3.Zero;
        public float Radius = 0;
        public bool IsPlanetOnly = false;
    }

    [XmlType("XmlList")]
    public class CooldownForXML
    {
        public CooldownForXML()
        {
            PlayerID = 0;
            DateTime = DateTime.UtcNow;
        }

        public CooldownForXML(long key, DateTime value)
        {
            PlayerID = key;
            DateTime = value;
        }

        [XmlAttribute("PlayerID")]
        public long PlayerID { get; set; }
        [XmlAttribute("DateTime")]
        public DateTime DateTime { get; set; }
    }
}
