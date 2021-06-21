using Digi;
using Sandbox.ModAPI;
using Scripts.Shared;
using VRageMath;

namespace Scripts.Specials.Blocks
{
    partial class FactionSafeZoneBlock
    {

        private static void CreateUpgradeControl (string id, string name, string tooltip, int type)
        {
            MyAPIGateway.TerminalControls.CreateSlider<FactionSafeZoneBlock, IMyUpgradeModule>("FactionSafezoneBlock_"+ id, name, tooltip, 0, 13,
                (x) => x.Settings.Upgrades.GetUpgradeLvl(type),
                (x, sb) => sb.Append(x.Settings.Upgrades.GetUpgradeLvl(type)).Append(" lvl"),
                (x, y) => GUIRequestUpdate(x, y, type));
        }

        public override void InitControls()
        {
            base.InitControls();

            MyAPIGateway.TerminalControls.CreateButton<FactionSafeZoneBlock, IMyUpgradeModule>("FactionSafezoneBlock_SpawnSafezone", "Spawn Safezone", "Spawn Safezone", GUIRequestSpawn);
            MyAPIGateway.TerminalControls.CreateColorPicker<FactionSafeZoneBlock, IMyUpgradeModule>("FactionSafezoneBlock_ChangeSafezoneColor", "Color", "Safezone color", GUIGetColor, GUISetColor);

            CreateUpgradeControl ("Upgrade_Radius", "Radius", "Upgrade radius", SZUpgrades.TYPE_RADIUS);
            CreateUpgradeControl ("Upgrade_Refine Speed", "Refine Speed", "Upgrade Refine Speed", SZUpgrades.TYPE_SPEED);
            CreateUpgradeControl ("Upgrade_Refine Yeild", "Refine Yeild", "Upgrade Refine Yeild", SZUpgrades.TYPE_YEILD);
            CreateUpgradeControl ("Upgrade_Energy_Boost", "Energy boost", "Upgrade Energy boost", SZUpgrades.TYPE_EBOOST);
            CreateUpgradeControl ("Upgrade_PCU_Per_Player", "PCU per player", "Upgrade PCU per player", SZUpgrades.TYPE_PCUPERPLAYER);
            CreateUpgradeControl ("Upgrade_PCU_Per_Base", "PCU per base", "Upgrade PCU per base", SZUpgrades.TYPE_PCUPERBASE);
            CreateUpgradeControl ("Upgrade_Protection_Zone", "Protection zone", "Upgrade Protection zone", SZUpgrades.TYPE_ZONEPROTECTION);
            CreateUpgradeControl ("Upgrade_Passive_Ore_Production", "Passive Ore Production", "Upgrade Passive Ore Production", SZUpgrades.TYPE_OREPRODUCTION);
        }

        public static Color GUIGetColor(FactionSafeZoneBlock block)
        {
            var sz = block.SafeZone;
            if (sz != null) {
                return sz.ModelColor;
            }

            return block.Settings.SafezoneColor;
        }

        public static void GUISetColor(FactionSafeZoneBlock block, Color color)
        {
            var newSettings = block.Settings.Clone();
            newSettings.SafezoneColor = color;
            block.Notify(newSettings, TYPE_MODIFY_LOOK);
        }


        public static void GUIRequestSpawn(FactionSafeZoneBlock block)
        {
            var newSettings = block.Settings.Clone();
            block.Notify(newSettings, TYPE_CREATE_SZ);
        }

        public static void GUIRequestUpdate(FactionSafeZoneBlock block, float value, int type)
        {
            var newvalue = (int)MathHelper.Clamp(value, 1, 12);
            var lvl = block.Settings.Upgrades.GetUpgradeLvl(type);
            if (lvl >= newvalue) return;

            var newSettings = block.Settings.Clone();
            newSettings.Upgrades.SetUpgradeLvl(type, newvalue);

            block.Notify(newSettings, TYPE_UPGRADE);
        }
    }
}
