using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Specials.Security {
    class TerminalBlockOptions {
        public static ushort CMD_ID = 23500;
        public static void Init () {
            MyAPIGateway.TerminalControls.CustomActionGetter += TerminalControls_CustomActionGetter;
            MyAPIGateway.TerminalControls.CustomControlGetter += TerminalControls_CustomControlGetter;
            MyAPIGateway.Multiplayer.RegisterMessageHandler (CMD_ID, UpdatePermissions);
        }

        public static void Unload () {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler (CMD_ID, UpdatePermissions);
        }

        private static void UpdatePermissions(byte[] obj) {

        }

        private static void TerminalControls_CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls) {
            controls.Clear();
        }

        private static void TerminalControls_CustomActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions) {
            actions.Clear();
        }
    }
}
