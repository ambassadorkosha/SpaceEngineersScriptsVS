using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Digi;
using Sandbox.Game.GameSystems.Chat;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace Scripts.Specials.SlimGarage
{
    public static class SlimGarage
    {
        public static string[] GarageSubtypes = { "LargeGarage" };
        public static MyStringHash[] GarageSubtypesHash =
        { MyStringHash.GetOrCompute("LargeGarage")};
        public static Communication Comms = null;
        public static InterModComms InterModComms = null;

        public static void Init()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                //InterModComms should be only server.
                InterModComms = new InterModComms();
                WriteToLogDbg("Server session mod initialized. ver: 1.2");
                return;
            }
            //only client
            WriteToLogDbg("Client session mod initialized. ver: 1.2");
            Comms = new Communication();
            MyAPIGateway.Session.OnSessionReady += OnSessionReady;
        }
        public static void OnSessionReady()
        {
            Comms.SendGetSettingsFromServerReq();
            MyAPIGateway.Session.OnSessionReady -= OnSessionReady;
        }

        public static void Unload()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                InterModComms.UnregisterHandlers();
                return;
            }
            Comms.UnregisterHandlers();
            WriteToLogDbg($"UnloadData");
        }
        /// <summary>
        /// For track my bad debug messages ;(
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteToLogDbg(string msg)
        {
            MyLog.Default.Info($"[SlimGarage] : {msg}");
        }
    }
}
