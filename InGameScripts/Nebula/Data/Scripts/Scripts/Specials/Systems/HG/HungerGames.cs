using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using Scripts.Specials.Systems;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Scripts.Specials.Systems
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_RadioAntenna), true, "EventSafeSphereLarge")]


    //!hg create TST here 5000 10

    public static partial class HungerGames
    {
        static HungerGamesSettings settings = new HungerGamesSettings();
        static List<HungerGame> games = new List<HungerGame>();
        static Dictionary<string, Action<string[]>> actions = new Dictionary<string, Action<string[]>>();
        public static void Init()
        {
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
            InitCommands();
        }

        public static void Tick ()
        {
            foreach (var x in games)
            {
                x.Tick();
            }
        }

        public static void Draw()
        {
            foreach (var x in games)
            {
                x.Draw();
            }
        }
    }

    

    
}
