using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Linq;
using System.Text;
using VRageMath;

namespace Scripts.Specials.Systems
{
    public static partial class HungerGames
    {

        private static void InitCommands()
        {
            actions.Add("list", CommandList);
            actions.Add("enable", CommandEnable);
            actions.Add("startnow", CommandStartNow);
            actions.Add("delete", CommandDelete);
            actions.Add("create", CommandCreate);
            actions.Add("help", CommandHelp);
            actions.Add("quality", CommandHelp);
            actions.Add("join", CommandJoin);
        }



        private static void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (messageText.StartsWith("!hg "))
            {
                sendToOthers = false;
                var data = messageText.Replace("!hg ", "").Split(' ');
                var cmd = data[0].ToLower();
                if (actions.ContainsKey(cmd))
                {
                    try
                    {
                        actions[cmd].Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Common.SendChatMessageToMe("Error:" + e, "HungerGames");
                    }
                }
                else
                {
                    Common.SendChatMessageToMe("Command not found", "HungerGames");
                    CommandHelp(null);
                }
            }
        }

        private static void CommandJoin(string[] data)
        {
            var game = GetGameByNameOrThrow(data[1]);
            if (game == null)
            {
                Common.SendChatMessageToMe("Game not found", "HungerGames");
            }

        }

        private static void CommandEnable(string[] data)
        {
            var game = GetGameByNameOrThrow(data[1]);
            game.gameSettings.Enabled = true;
        }

        private static void CommandStartNow(string[] data)
        {
            var game = GetGameByNameOrThrow(data[1]);
            game.gameSettings.Enabled = true;
            game.gameSettings.FirstCapture = SharpUtils.timeStamp() + 3;
        }

        private static void CommandDelete(string[] data)
        {
            var name = data[1];
            var deleted = games.RemoveAll((x) => x.gameSettings.Name == name);
            Common.SendChatMessageToMe(deleted > 0 ? "Deleted" : "Not Found game with name " + name, "HungerGames");
        }

        private static void CommandCreate(string[] data)
        {
            var name = data[1];
            var where = data[2]; //here, thisPlanet, x|y|z
            var radius = data[3];
            var fc = data[4]; //first capture


            var fcIn = int.Parse(fc);

            var sphere = new BoundingSphereD();
            sphere.Center = MyAPIGateway.Session.LocalHumanPlayer.Character.WorldMatrix.Translation;
            sphere.Radius = Double.Parse(radius);

            foreach (var x in games)
            {
                if (x.gameSettings.Name == name) throw new Exception("Already exists with same name");
            }


            var game = new HungerGame();
            game.gameSettings.Name = name;
            game.gameSettings.GameSphere = sphere;
            game.gameSettings.FirstCapture = fcIn + SharpUtils.timeStamp();

            game.gameParams.DamageSphere.Center = game.gameSettings.GameSphere.Center;
            game.gameParams.DamageSphere.Radius = game.gameSettings.GameSphere.Radius;

            game.gameParams.NoneDamageSphere.Center = game.gameSettings.GameSphere.Center;
            game.gameParams.NoneDamageSphere.Radius = game.gameSettings.GameSphere.Radius / 3f;

            games.Add(game);
        }

        private static void CommandList(string[] data)
        {
            var sb = new StringBuilder();
            var i = 0;
            foreach (var x in games)
            {
                sb.Append($"{i} : {x.gameSettings.Name} ({x.gameParams.State}) settings={x.gameSettings.ToString()} params={x.gameParams.ToString()}");
            }
        }

        private static void CommandQuality(string[] data)
        {
            settings.Quality = int.Parse(data[1]);
        }

        private static void CommandHelp(string[] data)
        {
            var sb = new StringBuilder();
            var i = 0;
            foreach (var x in actions)
            {
                sb.Append($"!hg {x.Key}\n");
            }
            Common.SendChatMessageToMe(sb.ToString(), "HungerGames");
        }

        private static HungerGame GetGameByNameOrThrow(string name)
        {
            var game = games.First((x) => x.gameSettings.Name == name);
            if (game == null)
            {
                throw new Exception("Game not found");
            }
            return game;
        }
    }
}
