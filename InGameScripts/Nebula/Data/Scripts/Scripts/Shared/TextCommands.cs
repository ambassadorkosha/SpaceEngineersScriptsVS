using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using VRageMath;
using COMAND = System.Collections.Generic.KeyValuePair<System.Action<string>, string>;

namespace NAPI
{
    public static class TextCommands
    {
        private static Dictionary<string, Dictionary<string, COMAND>> cmdReactions = new Dictionary<string, Dictionary<string, COMAND>>();

        public static void Init()
        {
            MyAPIGateway.Utilities.MessageEntered += UtilitiesMessageEntered;
        }

        public static void AddCommand (string categoryName, string commandName, Action<string> action, string help)
        {
            categoryName = categoryName.ToLower();
            commandName = commandName.ToLower();

            Dictionary <string, COMAND> category = null;
            if (!cmdReactions.ContainsKey(categoryName))
            {
                category = new Dictionary<string, COMAND>();
                cmdReactions.Add (categoryName, category);
                category.Add ("help", new COMAND ((x)=>Help(categoryName), "Helps"));
            } else {
                category = cmdReactions[categoryName];
            }

            category [commandName] = new COMAND(action, help);
        }

        public static void AddCommand(string categoryName, string commandName, string regex, Action<string[]> action, string help)
        {
            Regex reg = new Regex(regex);

            Action<string> _action = (x)=>
            {
                var match = reg.Match(x);
                if (!match.Success)
                {
                    Common.SendChatMessageToMe ("Wrong data. Info: " + cmdReactions[categoryName][commandName].Value);
                    return;
                }

                var arr = new string[match.Groups.Count];
                for (var i=0; i<match.Groups.Count; i++)
                {
                    arr[i] = match.Groups[i].Value;
                }

                action (arr);
            };
        }

        private static void UtilitiesMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("!")) return;
            
            sendToOthers = false;

            var parts = messageText.Split (' ');
            if (parts.Length < 2) return;

            var category = parts[0].Substring(1);
            var command = parts[1].ToLower();

            if (!cmdReactions.ContainsKey (category)) return;

            var cat = cmdReactions[category];

            if (!cat.ContainsKey (command))
            {
                Common.SendChatMessageToMe($"No such command. Use !{category} help for info");
                return;
            }

            var i = messageText.IndexOf (parts[1]);
            var txt = messageText.Substring (i+parts[1].Length);

            var cmd = cat [command].Key;
            try
            {
                cmd (txt);
                Common.SendChatMessageToMe("Command executed!");
            } 
            catch (Exception e)
            {
                Common.SendChatMessageToMe ("Error: "+e);
            }
        }

        private static void Help(string categoryName)
        {
            var sb = new StringBuilder($"List of commands\n");
            foreach (var x in cmdReactions[categoryName])
            {
                sb.Append("!").Append(categoryName).Append(" ").Append(x.Key).Append(" ").Append(x.Value).Append("\n");
            }
            Common.SendChatMessageToMe(sb.ToString());
            return;
        }
    }
}
