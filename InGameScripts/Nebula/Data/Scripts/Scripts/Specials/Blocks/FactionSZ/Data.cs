using ProtoBuf;
using System;
using VRageMath;

namespace Scripts.Specials.Blocks
{

    public class FactionSZBlockSettings { }

    [ProtoContract]
    public class FactionSZSettings
    {
        [ProtoMember(1)] public long SafezoneId = 0;
        [ProtoMember(2)] public SZUpgrades Upgrades = new SZUpgrades();
        [ProtoMember(3)] public bool Enabled = false;
        [ProtoMember(4)] public Vector3 CreatedAt = new Vector3(float.NaN, float.NaN, float.NaN);

        [ProtoMember(5)] public string SafezoneTexture;
        [ProtoMember(6)] public Color SafezoneColor;
        [ProtoMember(7)] public bool SafezoneIsSphere = true;



        public override string ToString()
        {
            return $"Id={SafezoneId} Enabled={Enabled} | Sphere={SafezoneIsSphere} Texture={SafezoneTexture} CreatedAt={CreatedAt}";
        }

        public FactionSZSettings Clone()
        {
            var newOne = new FactionSZSettings();
            newOne.SafezoneId = SafezoneId;
            newOne.Upgrades = Upgrades.Clone();
            newOne.Enabled = Enabled;
            newOne.CreatedAt = CreatedAt;

            return newOne;
        }
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

        [ProtoMember(1)] public int SZRadius = 1;
        [ProtoMember(2)] public int RefineSpeed = 1;
        [ProtoMember(3)] public int RefineYeild = 1;
        [ProtoMember(4)] public int EnergyBoost = 1;
        [ProtoMember(5)] public int PCUPerPlayer = 1;
        [ProtoMember(6)] public int PCUPerBase = 1;
        [ProtoMember(7)] public int ZoneProtection = 1;
        [ProtoMember(8)] public int PassiveOreProduction = 1;

        public SZUpgrades Clone()
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

        public int GetUpgradeLvl(int type)
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
            return GetBonus(30, 500, SZRadius);
        }

        public float GetProtectionRadius()
        {
            return GetBonus(1000, 20000, SZRadius);
        }

        private float GetBonus(float min, float max, int lvl)
        {
            return min + ((max - min) / 12f * lvl);
        }
    }
}
