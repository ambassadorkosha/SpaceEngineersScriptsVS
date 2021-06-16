using VRage.Game.Components;
using ObjectBuilders.SafeZone;
using SpaceEngineers.Game.ModAPI;

namespace Scripts.Specials.Safezones {

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SafeZoneBlock), true, new string[] { "DungeonSafezone" })]
    public class DungeonSafeZone : MyGameLogicComponent {
        public bool canUseJetPack () {
            var custom = (Entity as IMySafeZoneBlock).CustomData;
            if (custom.Contains ("NO-JETPACK")) {
                return false;
            }
            return true;
        }
    }

}
