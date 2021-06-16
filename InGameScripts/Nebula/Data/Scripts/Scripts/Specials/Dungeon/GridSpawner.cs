using Digi;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using ServerMod;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using Scripts.Shared;
using VRage.Game;
using VRage.ModAPI;
using System.Text.RegularExpressions;

namespace Scripts.Specials.Dungeon
{
    /*public class GridSpawnData
    {
        
        public int deathtimer;
        public int timeout;
        public int offset;
        public int gpsrad;
        public int count;
        public int gpstime;
        public int tmin;
        public int tmax;
        public string prefab;
        public string ai;
        public string gpsname;
        public string gpsdesc;
        public string itemid;
        public string[] items;
        public string[] containers;
        public bool addgps;
        public bool isplanet = false;
        public bool isstatic = false;
        public Vector3 startpos = new Vector3(0,0,0);
        public Color gpscolor;

        public static GridSpawnData Randomize(GridSpawnData settings)
        {
            var data = settings;
            Random rnd = new Random((int)DateTime.Now.Ticks);

            data.timeout = rnd.Next(data.tmin, data.tmax) * 1000;

            var cnt = 0;
            string item;
            do
            {
                item = data.items[rnd.Next(0, data.items.Count() - 1)].Trim(new Char[] { '\n' });
                cnt++;
            } while (rnd.Next(1, 10) > int.Parse(item.Split('|')[3]) && cnt < 100);

            var EHOT = item.Split('|');
            data.itemid = EHOT[0];
            data.count = rnd.Next(int.Parse(EHOT[1]), int.Parse(EHOT[2]));

            string[] parts;
            string line;
            cnt = 0;
            do
            {
                line = data.containers[rnd.Next(0, data.containers.Count() - 1)].Trim(new Char[] { '\n' });
                parts = line.Split('|');
                cnt++;
            } while (rnd.Next(1, 10) > int.Parse(parts[1]) && cnt < 100);
            data.isplanet = parts[3] == "true";
            data.isstatic = parts[4] == "true";
            data.prefab = parts[0];

            return data;
        }
        
        public static GridSpawnData Parse(String s)
        {
            try
            {
                if (s == "")
                {
                    return null;
                }
                
                var data = new GridSpawnData();
                data.containers = s.Split('$')[1].Split('%');
                var settings = s.Split('$')[0].Trim(new Char[] { '\n' }).Split('|');


                var p1 = settings[0].ToLowerInvariant();
                data.ownership = p1 == "player" ? 2 : p1 == "pirates" ? 1 : 0;

                data.deathtimer = int.Parse(settings[1]) * 60;

                var OwO = settings[2].Split('&');
                data.tmin = int.Parse(OwO[0]);
                data.tmax = int.Parse(OwO[1]);
                

                if (settings[4] == "true")
                {
                    data.addgps = true;
                    data.gpsname = settings[5];
                    data.gpsdesc = settings[6];
                    data.gpstime = int.Parse(settings[10]);
                    var cl = settings[9].Split(':');
                    data.gpscolor = new Color(int.Parse(cl[0]), int.Parse(cl[1]), int.Parse(cl[2]));
                }
                data.offset = int.Parse(settings[3]);
                //data.ai = parts[2];
                data.ai = null;
                data.gpsrad = int.Parse(settings[7]);

                if (settings[8] != "")
                {
                    var coords = settings[8].Split(':');
                    data.startpos = new Vector3(float.Parse(coords[0]), float.Parse(coords[1]), float.Parse(coords[2]));
                }
                
                data.items = s.Split('$')[2].Split('%');
                

                return data;
            }
            catch (Exception e)
            {
                Log.Error(e);
                return null;
            }
        }
    }
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_MedicalRoom), true, new string[] { "GridSpawnerLarge", "GridSpawnerSmall" })]
    class GridSpawner : MyGameLogicComponent
    {
        static bool inited = false;

        public List<IMyCargoContainer> cargos;
        public IMyCubeGrid xx;

        public IMyMedicalRoom block;
        public bool force = false;
        public String data;
        public GridSpawnData spawnData;
        public long lastSpawned = -1;

        public static Vector3 rndInRadius(int maxoffset)
        {
            Random rnd = new Random((int)DateTime.Now.Ticks);
            Vector3D offset = new Vector3D(rnd.Next(2) - 1, rnd.Next(2) - 1, rnd.Next(2) - 1);
            offset.Normalize();
            return offset * rnd.Next(maxoffset);
        }

        public override void UpdateAfterSimulation100()
        {
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            if ((Entity as IMyMedicalRoom).Enabled)
            {
                Spawn((Entity as IMyMedicalRoom));
            }
        }
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            if (!inited)
            {
                inited = true;
                InitActions();
                Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
            block = (Entity as IMyMedicalRoom);
            //(Entity as IMyTerminalBlock).CustomDataChanged += GridSpawner_CustomDataChanged;
        }

        public static void AttachAI(IMyCubeGrid grid, String AI = "Default")
        {
            var b = grid.FindBlock((x) => { return x.FatBlock is IMyRemoteControl; });
            if (b == null) return;

            var fat = b.FatBlock;
            //var targets2 = new List<MyEntity>();
            //targets2.Add(b as MyEntity);

            fat.Name = "SuperDuperAI";
            MyEntities.SetEntityName(fat as MyEntity);
            var r = fat as IMyRemoteControl;
            r.SetAutoPilotEnabled(true);
            MyVisualScriptLogicProvider.SetDroneBehaviourAdvanced(fat.Name, AI);//, targets : targets2);
            MyEntities.RemoveName(fat as MyEntity);
        }

        public static void AddDestroyTimer(IMyCubeGrid grid, int time)
        {
            FrameExecutor.addDelayedLogic(time, new GridRemover(grid));
        }

        public static void AddItems(IMyCubeGrid grid, GridSpawnData data)
        {
            try
            {
                var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                List<IMyCargoContainer> cargos;
                cargos = new List<IMyCargoContainer>();
                gts.GetBlocksOfType<IMyCargoContainer>(cargos);

                foreach (IMyCargoContainer cargo in cargos)
                {

                    cargo.GetInventory().AddItem(MyDefinitionId.Parse(data.itemid), (double)data.count);

                }
            }
            catch (Exception e) { Log.ChatError(e); }

        }

        public static void Spawn(IMyTerminalBlock block)
        { //MyVisualScriptLogicProvider.SpawnBot("Wolf", block.WorldMatrix.Translation);
            GridSpawner spawner = block.GetAs<GridSpawner>();
            if (spawner.data != block.CustomData)
            {
                spawner.spawnData = GridSpawnData.Parse(block.CustomData);
                spawner.data = block.CustomData;
            }
            GridSpawnData data = GridSpawnData.Randomize(spawner.spawnData);
            if (data == null)
            {
                //Common.SendChatMessage ("Data null");
                return;
            }
            if (SharpUtils.msTimeStamp() - spawner.lastSpawned < data.timeout)
            {
                //Common.SendChatMessage ("Spawn in cooldown");
                return;
            }
            if (!MyAPIGateway.Session.IsServer)
            {
                //Common.SendChatMessage ("Not server");
                return;
            }


            spawner.lastSpawned = SharpUtils.msTimeStamp();


            var prefab = MyDefinitionManager.Static.GetPrefabDefinition(data.prefab);

            long playerId = 0;
            switch (data.ownership)
            {
                case 0:
                    {
                        playerId = 0;
                        break;
                    }
                case 1:
                    {
                        playerId = Relations.FindBot("Space Pirates");
                        break;
                    }
                case 2:
                    {
                        var pl = Other.GetNearestPlayer(block.WorldMatrix.Translation);
                        if (pl != null)
                        {
                            playerId = pl.IdentityId;
                        }
                        else
                        {
                            playerId = 0;
                        }
                        break;
                    }
            }


            var up = block.PositionComp.GetOrientation().Up;
            var m = block.WorldMatrix;
            var sp_pos = new Vector3D(0,0,0);
            if (data.startpos != new Vector3(0, 0, 0))
            {
                sp_pos = data.startpos + rndInRadius(data.offset);
            }
            else
            {
                sp_pos = block.GetPosition() + rndInRadius(data.offset);
            }
            if (data.isplanet)
                sp_pos = sp_pos.GetPlanet().GetClosestSurfacePointGlobal(sp_pos);
            var gps_sp_pos = sp_pos + rndInRadius(data.gpsrad);
            if (data.isplanet)
                gps_sp_pos = gps_sp_pos.GetPlanet().GetClosestSurfacePointGlobal(gps_sp_pos);
            try
            {
                prefab.spawnPrefab(sp_pos, m.Forward, m.Up, playerId, (x) =>
                {
                    if (data.ai != null && data.ai.Length > 0) AttachAI(x, data.ai);
                    //if (data.addgps) Gps.AddGps(data.gpsname,data.gpsdesc,sp_pos+rndInRadius(data.gpsrad));
                    if (data.addgps) MyVisualScriptLogicProvider.AddGPSForAll(data.gpsname, data.gpsdesc, gps_sp_pos, data.gpscolor, data.gpstime); ;
                    if (data.deathtimer > 0) AddDestroyTimer(x, data.deathtimer);
                    AddItems(x, data);
                    x.IsStatic = data.isstatic;
                }, VRage.Game.MyOwnershipShareModeEnum.None);
            }
            catch (Exception e) { Log.Error(e); }
        }


        public static void InitActions()
        {
            var spawn = MyAPIGateway.TerminalControls.CreateAction<IMyMedicalRoom>("Spawn");
            spawn.Action = (b) => { Spawn(b); };
            spawn.Name = new StringBuilder("Spawn");
            spawn.Enabled = (b) => { return b.BlockDefinition.SubtypeId.Contains("GridSpawner"); };

            MyAPIGateway.TerminalControls.AddAction<IMyMedicalRoom>(spawn);
        }

        private void Block_OwnershipChanged(IMyTerminalBlock obj)
        {
            var owner = block.OwnerId;
            if (owner != block.BuiltBy())
            {
                ((MyCubeGrid)block.CubeGrid).TransferBlocksBuiltByID(block.BuiltBy(), owner);
            }
        }
    }
    public class GridRemover : Action1<long>
    {
        private IMyCubeGrid grid;
        public GridRemover(IMyCubeGrid grid)
        {
            this.grid = grid;
        }

        public void run(long k)
        {
            if (grid.MarkedForClose || grid.Closed)
            {
                return;
            }
            if (grid.GetShip() == null)
            {
                //Log ("")
                return;
            }
            grid.GetShip().SafeDeleteGrid();
        }
    }*/
}
