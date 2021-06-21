using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using ServerMod;
using VRage.Game;
using VRageMath;

namespace Scripts.Specials.Dungeon.AirDrops
{
    public class AirDropSettings
    {
        [XmlIgnore]
        public AutoTimer SpawnTimer = new AutoTimer(15 * 60 * 60, 15 * 60 * 60);

        public long Ownership;
        
        public int MinTriggerIntervalInSeconds = 60 * 3;
        public int MaxTriggerIntervalInSeconds = 60 * 6;

        public int MinFirstTriggerDelayInSeconds = 120;
        public int MaxFirstTriggerDelayInSeconds = 240;

        public int BeforeSpawnDelayInSeconds = 60 * 5;
        public int AfterSpawnDelayInSeconds = 60 * 5;

        public AirDropGps Gps;
        public SpawnOptions SpawnOptions = new SpawnOptions();
        public List<AirDropVariant> Prefabs = new List<AirDropVariant>(3);
        public List<LootGroup> Loot = new List<LootGroup>(3);

        
    }

    public class SpawnOptions
    {
        public List<string> Planets = new List<string>();
        public List<SpawnPoint> SpawnPoints = new List<SpawnPoint>();

        public void GetAllSpawnPoints (List<SpawnPoint> buffer)
        {
            foreach (var x in Planets)
            {
                var pl = GameBase.GetPlanetWithGeneratorId (x);
                if (pl != null)
                {
                    var SpawnPoint = new SpawnPoint()
                    {
                        Center = pl.WorldMatrix.Translation,
                        Radius = pl.MaximumRadius,
                        IsPlanetOnly = true
                    };
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

    public class AirDropGps
    {
        public string GPSName = "";
        public Color GPSColor = Color.Aqua;
    }
    public class AirDropGPSItem
    {
        public string GPSName;
        public string GPSDescription;
        public DateTime SpawnTime;
        public int AfterSpawnRemoveDelay;
        public Color Color;
        public Vector3D Position;
    }

    public class AirDropVariant
    {
        public string Prefab;
        public float Chance;

        public float ChanceSpawnUnderVoxels = 0;
        public float ChanceSpawnAboveVoxels = 0;
        public float MinBelowVoxels = 0;
        public float MaxBelowVoxels = 0;
        public float MinAboveVoxels = 0;
        public float MaxAboveVoxels = 0;

        public float MinSpawnPlanetRadius = 0;
        public float MaxSpawnPlanetRadius = 2;

        public bool CanSpawnOnPlanets = true;
        public bool CanSpawnInSpace = true;

        public List<LootGroup> SpecificLoot = new List<LootGroup>(3);
    }

    public class LootVaraint //static Regex regex = new Regex("\\([\\w]+\\/[\\w]+ [\\d.,]+ [\\d.,]+\\)"); //(O/Iron 1000000 10000000)
    {
        public string Id;
        public int Min;
        public int Max;

        [XmlIgnore]
        public MyDefinitionId Definition {
            get {
                return MyDefinitionId.Parse("MyObjectBuilder_" + Id);
            }
        }
    }
    

    public class LootGroup
    {
        public float Chance = 0f;
        public List<LootVaraint> Loot = new List<LootVaraint>(3);
    }
}
