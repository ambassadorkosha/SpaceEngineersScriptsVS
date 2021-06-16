using Sandbox.Definitions;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using VRage.ModAPI;

namespace Scripts.Specials.Undone {
    public class SolarPanelAllwaysMaxOutput { //INIT NOT CALLED....WHY?
        private static Random r = new Random();
        public SolarPanelAllwaysMaxOutput (IMySolarPanel solar) {
            (solar as IMyTerminalBlock).PropertiesChanged += SolarPanelThruster_PropertiesChanged;
            solar.OnMarkForClose += MySolar_OnMarkForClose;
        }

        private void MySolar_OnMarkForClose(IMyEntity obj) {
            var solar = obj as IMySolarPanel;
            (solar as IMyTerminalBlock).PropertiesChanged += SolarPanelThruster_PropertiesChanged;
            solar.OnMarkForClose -= MySolar_OnMarkForClose;
        }

        private bool ignore = false;
        private void SolarPanelThruster_PropertiesChanged(IMyTerminalBlock obj) {
            var mySolar =  (IMySolarPanel)obj;
            var max = (mySolar.SlimBlock.BlockDefinition as MySolarPanelDefinition).MaxPowerOutput;
            var threstood= max*0.001f;
            if (ignore) return;
            ignore = true;
            if (MyAPIGateway.Multiplayer.IsServer) {
                if (mySolar.MaxOutput > threstood) {
                    mySolar.SourceComp.SetMaxOutput(max);
                    mySolar.SourceComp.SetOutput(max);
                } else {
                    mySolar.SourceComp.SetMaxOutput(0);
                    mySolar.SourceComp.SetOutput(0);
               }
            } else {
                if (mySolar.MaxOutput > threstood) {
                    mySolar.SourceComp.SetMaxOutput(max+max*0.001f*((float)r.NextDouble()));
                    mySolar.SourceComp.SetOutput(max+max*0.001f*((float)r.NextDouble()));
                } else {
                    mySolar.SourceComp.SetMaxOutput(0.001f*max*((float)r.NextDouble()));
                    mySolar.SourceComp.SetOutput(0.001f*max*((float)r.NextDouble()));
                }
            }
            ignore = false;
        }
    }
}
