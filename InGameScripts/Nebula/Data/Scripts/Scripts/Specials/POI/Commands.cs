using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Scripts.Base;
using ServerMod;
using VRageMath;

namespace Scripts.Specials.POI
{
    /*
       !poi create here -1 -1 -1 Name can be with spaces
       !poi create 1|-1|1 -1 -1 -1
       !poi remove 19999 (id)
       !poi removeAll
       !poi setGps 19999 <Name> <Description> <Vector>
       !poi list
       */
    public static class Commands
    {
        private readonly static string CMD_KEY = "!poi";
        private readonly static string CHAT_NAME = "POI";
        private static Dictionary<string, Pair<Action<string[]>, string>> m_cmdReactions = new Dictionary<string, Pair<Action<string[]>, string>>();
        public static void Init()
        {
            MyAPIGateway.Utilities.MessageEntered += UtilitiesMessageEntered;
            GameBase.AddUnloadAction(() => { MyAPIGateway.Utilities.MessageEntered -= UtilitiesMessageEntered; });
            m_cmdReactions.Add("create", new Pair<Action<string[]>, string>(Create, "(<X> <Y> <Z>) | (here) <CantMineDistance> <CantEnableSafezone> <CantEnableBaseCores> <CantJumpHere> <Name>"));
            m_cmdReactions.Add("remove", new Pair<Action<string[]>, string>(Remove, "(<Name>) | (<id>)"));
            m_cmdReactions.Add("removeall", new Pair<Action<string[]>, string>(RemoveAll, ""));
            m_cmdReactions.Add("setgps", new Pair<Action<string[]>, string>(SetGps, "<POIid> <X> <Y> <Z> <Description>"));
            m_cmdReactions.Add("list", new Pair<Action<string[]>, string>(ShowList, ""));
        }

        private static void UtilitiesMessageEntered(string messageText, ref bool sendToOthers)
        {
            try
            {
                if (string.IsNullOrEmpty(messageText)) return;
                var cmd = messageText.ToLower();
                if (cmd.StartsWith(CMD_KEY))
                {
                    Respond($"{messageText}");
                    if (MyAPIGateway.Session.Player.PromoteLevel < VRage.Game.ModAPI.MyPromoteLevel.Admin)
                    {
                        Respond($"Only admins can use this command.");
                        return;
                    }

                    if (cmd.Trim() == CMD_KEY)
                    {
                        Respond($"Please specify command or use {CMD_KEY} help");
                    }
                    else
                    {
                        var args = cmd.Remove(0, CMD_KEY.Length).Trim().Replace("   ", " ").Replace("  ", " ").Split(' ');
                        if (args.Length > 0)
                        {
                            var commandAlias = args[0].Trim();
                            if (m_cmdReactions.ContainsKey(commandAlias))
                            {
                                m_cmdReactions[commandAlias].k.Invoke(args);
                            }
                            else
                            {
                                MyAPIGateway.Utilities.ShowMissionScreen("POI command list", "Help", "", GetHelpText());
                            }
                        }
                    }
                    sendToOthers = false;
                }
            }
            catch (Exception ex)
            {
                Respond($"Exception in command: {ex.ToString()} {ex.StackTrace}");
            }
        }

        private static string GetHelpText()
        {
            var sb = new StringBuilder();
            int i = 0;
            foreach (var cmd in m_cmdReactions)
            {
                sb.AppendLine($"{i++}) {CMD_KEY} {cmd.Key} [ {cmd.Value.v} ]");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void Respond(string msg)
        {
            MyVisualScriptLogicProvider.SendChatMessage(message: msg, author: CHAT_NAME, playerId: MyAPIGateway.Session.Player.IdentityId, font: "Blue");
        }

        private static string ParseName(string[] args, int startindex)
        {
            int i = startindex;
            string name = "";
            while (i < args.Length)
            {
                name += args[i] + " ";
                i++;
            }
            name = name.Trim(' ');
            return name;
        }

        #region CommandActions
        /// <summary>
        /// create (<X> <Y> <Z>) | (here) <CantMineDistance> <CantEnableSafezone>
        /// <CantEnableBaseCores> <Name>
        /// create here -1 -1 -1 -1 Name can be with spaces
        /// create 1000 2000 3000 -1 -1 -1 -1 Admin zone #123
        /// </summary>
        /// <param name="args"></param>
        private static void Create(string[] args)
        {
            if (args.Length < 6)
            {
                Respond("This command requires more arguments. See help.");
                return;
            }

            var pos = new Vector3D();
            if (args[1] == "here")
            {
                if (MyAPIGateway.Session.LocalHumanPlayer != null)
                {
                    pos = MyAPIGateway.Session.LocalHumanPlayer.GetPosition();
                }
                else
                {
                    Respond("For 'here' argument u need to be in world.Physicaly.");
                    return;
                }
                CreateInternal(pos, args, 2);
            }
            else
            {
                try
                {
                    pos = new Vector3D(long.Parse(args[1]), long.Parse(args[2]), long.Parse(args[3]));
                }
                catch (Exception ex)
                {
                    Respond("Can't parse position. See help.");
                    return;
                }

                CreateInternal(pos, args, 4);
            }
        }

        private static void CreateInternal(Vector3D pos, string[] args, int index)
        {
            float CantMineDistance = 0;
            float CantEnableSafezone = 0;
            float CantEnableBaseCores = 0;
            float CantJumpHere = 0;
            try
            {
                CantMineDistance = float.Parse(args[index++]);
                CantEnableSafezone = float.Parse(args[index++]);
                CantEnableBaseCores = float.Parse(args[index++]);
                CantJumpHere = float.Parse(args[index++]);

            }
            catch (Exception ex)
            {
                Respond("Can't parse POI rules. See help.");
                return;
            }

            var name = ParseName(args, index);
            if (string.IsNullOrWhiteSpace(name))
            {
                Respond("Can't parse POI name. See help.");
                return;
            }
            else
            {
                POICore.Create(pos, CantMineDistance, CantEnableSafezone, CantEnableBaseCores,CantJumpHere, name);
                Respond("New POI created!");
                return;
            }
        }

        private static void RemoveAll(string[] args)
        {
            if (!POICore.GetPois().Any())
            {
                Respond("POI list already empty!");
                return;
            }

            POICore.RemoveAll();
            Respond("POI list cleared!");
        }

        /// <summary>
        /// remove (<Name>) | (<id>)
        /// remove Admin zone blah blah
        /// remove 777
        /// </summary>
        /// <param name="args"></param>
        private static void Remove(string[] args)
        {
            if (args.Length < 2)
            {
                Respond("This command requires more arguments. See help.");
                return;
            }

            try
            {
                long id = long.Parse(args[1]);
                if (POICore.RemoveById(id))
                {
                    Respond($"POI with id: {id} removed.");
                    return;
                }
            }
            catch { }

            var name = ParseName(args, 1);
            if (string.IsNullOrWhiteSpace(name))
            {
                Respond("POI not found. Specify POI id or name. See help.");
                return;
            }
            else
            {
                POICore.RemoveByName(name);
                Respond($"POI with name: {name} removed.");
                return;
            }
        }

        /// <summary>
        /// setgps <id> <X> <Y> <Z> <color R> <color G> <color B> <Description>
        /// setgps 777 1000 2000 3000 This is admin zone #3        
        /// </summary>
        /// <param name="args"></param>
        private static void SetGps(string[] args)
        {
            if (args.Length < 4)
            {
                Respond("This command requires more arguments. See help.");
                return;
            }

            long id = 0;
            try
            {
                id = long.Parse(args[1]);
            }
            catch (Exception ex)
            {
                Respond("Can't parse POI id. Run list for POI id.");
                return;
            }

            float x = 0;
            float y = 0;
            float z = 0;
            try
            {
                x = float.Parse(args[2]);
                y = float.Parse(args[3]);
                z = float.Parse(args[4]);
            }
            catch (Exception ex)
            {
                Respond("Can't parse GPS position. See help.");
                return;
            }

            float r = 0;
            float g = 0;
            float b = 0;
            try
            {
                r = float.Parse(args[5]);
                g = float.Parse(args[6]);
                b = float.Parse(args[7]);
            }
            catch (Exception ex)
            {
                Respond("Can't parse GPS color. See help.");
                return;
            }

            if (string.IsNullOrWhiteSpace(args[8]))
            {
                Respond("Can't parse GPS name. See help.");
                return;
            }

            var name = ParseName(args, 8);
            POICore.TrySetGPS(id, new Vector3D(x, y, z), new Vector3D(r, g, b), name);
        }

        private static void ShowList(string[] args)
        {
            MyAPIGateway.Utilities.ShowMissionScreen(screenTitle: "POI List:", screenDescription: POICore.GetPOIList(), okButtonCaption: "Close");
        }
        #endregion
    }
}
