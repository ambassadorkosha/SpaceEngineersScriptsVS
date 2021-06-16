using Digi;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using Slime;
using VRage.ModAPI;

namespace Scripts.Specials.Effects {
    class EnabledIfBuildByFaction {
        public static EnabledIfBuildByFaction AddEffect (IMyFunctionalBlock block) {
            return new EnabledIfBuildByFaction (block);
        }
        
        public EnabledIfBuildByFaction (IMyFunctionalBlock block) {
            block.EnabledChanged += Block_EnabledChanged;
            block.OnMarkForClose += BlockOnOnMarkForClose;
            Block_EnabledChanged(block);
        }

        private void BlockOnOnMarkForClose(IMyEntity obj) {
            (obj as IMyFunctionalBlock).EnabledChanged -= Block_EnabledChanged;
            obj.OnMarkForClose -= BlockOnOnMarkForClose;
        }

        private void Block_EnabledChanged(IMyTerminalBlock obj) {
            var fun = (obj as IMyFunctionalBlock);
            
            if (fun.Enabled) {
                var builtBy = fun.SlimBlock.BuiltBy;
                var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(builtBy);

                if (faction == null) {
                    fun.Enabled = false;
                    return;
                }

                if (!faction.IsFounder(builtBy)) {
                    fun.Enabled = false;
                    return;
                }
                
                //ok
            }
        }
    }
}
