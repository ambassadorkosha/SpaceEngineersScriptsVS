using Digi;
using Sandbox.Game;
using Sandbox.ModAPI;
using ServerMod;
using System;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Specials.Dungeon {

    public class AnimalSpawnData { //Botname|OFFSET(:)
        public string bot;
        public Vector3 offset = new Vector3();

        public static AnimalSpawnData parse (String s) {
            try {
                var data = new AnimalSpawnData ();
                var parts = s.Split('|');
                data.bot = parts[0];
                string[] offsets = parts[1].Split(':'); 
                data.offset.X = float.Parse (offsets[0]);
                data.offset.Y = float.Parse (offsets[1]);
                data.offset.Z = float.Parse (offsets[2]);
                return data;
            } catch (Exception e) {
                return null;
            }
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] { "AnimalSpawner" })]
    class AnimalSpawner : MyGameLogicComponent {
        static bool inited = false;

        public IMyUpgradeModule block;
        public bool force = false;
        public String data;
        public AnimalSpawnData spawnData;
        public long lastSpawned = -1;



        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            if (!inited) {
                inited = true;
                InitActions ();
            }

            block = (Entity as IMyUpgradeModule);
            (block as IMyTerminalBlock).CustomDataChanged += CustomDataChanged;
           
            data = block.CustomData;
            spawnData = AnimalSpawnData.parse(data);
        }

        private void CustomDataChanged(IMyTerminalBlock obj) {
            try {  
                if (force) return;
                var player = Other.GetNearestPlayer(block.GetPosition());
                if (player.IsPromoted) {
                    data = obj.CustomData;
                    spawnData = AnimalSpawnData.parse(data);
                } else {
                    force = true;
                    block.CustomData = data; //return prev
                    force = false;
                }
            } catch (Exception e)  {
                Log.Error (e);
            }
        }

        public static void Spawn (IMyTerminalBlock block) { //
            if (!MyAPIGateway.Session.IsServer) { return; }
            var spawner = block.GetAs<AnimalSpawner>();
            var data = spawner.spawnData;
            if (data == null) { return; }



            MyVisualScriptLogicProvider.SpawnBot(data.bot, block.WorldMatrix.Translation + data.offset);
        }


        public static void InitActions () {
            var spawn = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule> ("SpawnAnimal");
            spawn.Action = (b) => { Spawn(b); };
            spawn.Name = new StringBuilder("SpawnAnimal");
            spawn.Enabled =  (b) => { return b.BlockDefinition.SubtypeId.Contains ("AnimalSpawner"); };

            MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule> (spawn);
        }
    }
}
