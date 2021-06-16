using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using ServerMod;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Scripts.Specials.SlimGarage
{
    public class InterModComms
    {
        private const long NETWORK_ID = 10666;
        public Func<MyObjectBuilder_CubeGrid[], KeyValuePair<bool,Dictionary<long, Dictionary<string, int>>>> Garage_CheckLimits_future;
        /// <summary>
        /// reg handlers for messaging
        /// </summary>
        public InterModComms()
        {
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(NETWORK_ID, RegisterComponents);
            SlimGarage.WriteToLogDbg("InterModComms register handlers");
        }

        public void UnregisterHandlers()
        {
            SlimGarage.WriteToLogDbg($"InterModComms unregister handlers");
            MyAPIGateway.Utilities.UnregisterMessageHandler(NETWORK_ID, RegisterComponents);
        }

        public void SendFuncToServer()
        {
            var myobj = new Dictionary<string, object>();
            Func<MyObjectBuilder_CubeGrid[], long, KeyValuePair<bool,Dictionary<long, Dictionary<string, int>>>> func = ModLoadChecks;
            myobj["Garage_Mod_LoadChecks"] = func;
            Func<IMyCubeGrid[], long, bool> func2 = ModSaveChecks;
            myobj["Garage_Mod_SaveChecks"] = func2;
            MyAPIGateway.Utilities.SendModMessage(NETWORK_ID, myobj);
            SlimGarage.WriteToLogDbg("SendModMessage CheckLimits_future sended.");
        }

        private void RegisterComponents(object data)
        {
            if (data is Dictionary<string, object>)
            {
                var dict = (data as Dictionary<string, object>);
                if (dict.ContainsKey("Garage_CheckLimits_future"))
                {
                    Garage_CheckLimits_future = (Func<MyObjectBuilder_CubeGrid[], KeyValuePair<bool,Dictionary<long, Dictionary<string, int>>>>)dict["Garage_CheckLimits_future"];
                    SlimGarage.WriteToLogDbg($"Garage_CheckLimits_future received to mod");
                    SendFuncToServer();
                }
            }
        }

        public KeyValuePair<bool,Dictionary<long, Dictionary<string, int>>> ModLoadChecks(MyObjectBuilder_CubeGrid[] cubegrids, long playerid) //CALL THIS FROM PLUGIN
        {
            // IsFreePlaceForPlacement();
            return Garage_CheckLimits_future.Invoke(cubegrids);
        }

        public bool ModSaveChecks(IMyCubeGrid[] grids, long playerid) //CALL THIS FROM PLUGIN
        {
            // IsFreePlaceForPlacement();
            return IsNoEnemySpecBlocks(grids, playerid);
        }

        /// <summary>
        /// true is good
        /// </summary>
        /// <param name="grids"></param>
        /// <param name="playerid"></param>
        /// <returns></returns>
        private bool IsNoEnemySpecBlocks(IMyCubeGrid[] grids, long playerid)
        {
            bool found = true;
            List<long> members = new List<long>();
            var fac = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerid);
            if (fac != null)
            {
                var tfacMem = fac.Members;
                foreach (var mem in tfacMem)
                {
                    members.Add(mem.Key);
                }
            }

            members.Add(playerid);
            foreach (var g in grids)
            {
                var ship = g.GetShip();
                if (ship == null) continue;
                if (ship.limitsProducer == null) continue;
                foreach (var l in ship.limitsProducer)
                {
                    if (!members.Contains(l.block.OwnerId))
                    {
                        found = false;
                        return found;
                    }
                }
            }
            return found;
        }
    }
}
