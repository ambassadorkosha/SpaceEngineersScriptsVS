using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using ServerMod;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Scripts.Specials.Blocks.ShipSkills
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] { "NebArmorModule" })]
    public class ArmorModule : LimitedOnOffBlock
    {
        static float BONUS = 0.9f;
        private static Dictionary<int, int> MODULE = LimitsChecker.From(LimitsChecker.TYPE_ARMOR_MODULES, 1, LimitsChecker.TYPE_WEAPONPOINTS, 8);
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) { 
            base.Init(objectBuilder);
            SetOptions (MODULE);
        }

        public float GetBonus ()
        {
            if (!fblock.Enabled) return 1f;
            if (!fblock.IsFunctional) return 1f;
            return BONUS;
        }

        internal static void Logic(Ship from, List<IMyCubeGrid> connected)
        {
            float v = 1;
            foreach (var x in connected)
            {
                var sh = x.GetShip();
                if (sh == null) continue;
                foreach (var y in sh.armorModules)
                {
                    v *= y.GetBonus();
                }
            }

            foreach (var x in connected)
            {
                var sh = x.GetShip();
                if (sh == null) continue;
                sh.damageReduction = v;
                if (sh.damageReduction != v)
                {
                    if (x.Name == null)
                    {
                        x.Name = "Grid_" + x.EntityId;
                    }
                    MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(x.Name, v);
                }
            }
        }
    }
}
