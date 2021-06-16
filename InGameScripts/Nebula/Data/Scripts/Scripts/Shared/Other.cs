using System;
using System.Collections.Generic;
using System.Text;

using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.ModAPI;
using Digi;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Weapons;
using Sandbox.Game.Weapons;
using System.IO;
using System.Linq;
using Slime;

namespace ServerMod {
    static class Players {
        private static List<IMyPlayer> playersCache = new List<IMyPlayer>();
        private static long lastTime = 0;
        public static List<IMyPlayer> GetCachedPlayers(this IMyPlayerCollection collection, Func<IMyPlayer, bool> filter) {
            if (SharpUtils.msTimeStamp() - lastTime > 1000) {
                MyAPIGateway.Players.GetPlayers(playersCache, (x) => true);
                lastTime = SharpUtils.msTimeStamp();
            }
            return new List<IMyPlayer>(playersCache.Where(filter));
        } 
    }
    
    static class Other {
        public static long FindPlayerByCharacterId(long charId) {
            return charId.As<IMyCharacter>()?.GetPlayer().IdentityId ?? 0;
        }
        
        public static IMyPlayer GetPlayer (long player) {
            var players = MyAPIGateway.Players.GetCachedPlayers(x => x.IdentityId == player);
            return players.Count > 0 ? players[0] : null;
        }
        
        public static bool PlayerIsOnline (long player) {
            var players = MyAPIGateway.Players.GetCachedPlayers(x => x.IdentityId == player);
            return players.Count > 0 ? true : false;
        }

        public static IMyPlayer findPlayerByName(string name) {
            var players = MyAPIGateway.Players.GetCachedPlayers((x)=>x.DisplayName.Equals(name));
            if (players.Count > 0) return players[0];
            else return null;
        }
        
        public static long GetPlayerByCharacter(long p) {
            var aa = MyAPIGateway.Entities.GetEntityById(p);
            if (aa is IMyCharacter) {
                var player = Other.findPlayerByName(aa.DisplayName);
                return player==null ? 0 : player.PlayerID;
            } else if (aa is IMyPlayer) {
                Log.Info("GetPlayerByCharacter. It is player, not character");
                return p;
            } else {
                Log.Error("Not player:" +(aa==null ? "null" : aa.ToString()) + " id:" + p + " " + MyAPIGateway.Session.Player.PlayerID );
                return 0;
            }
        }

        public static long BuiltBy(this IMyCubeBlock block) {
            return (block as MyCubeBlock).BuiltBy;
        }

        public static long BuiltBy(this IMySlimBlock block) {
            if (block.FatBlock != null)
            {
                return block.FatBlock.BuiltBy();
            }
            return block.BuiltBy;
        }

        public static bool BuilderIsOnline (this IMyCubeBlock block) {
            return PlayerIsOnline(block.BuiltBy());
        }

        public static List<IMyFaction> GetFactionsWithOnlinePlayers() {
            List<IMyFaction> factions = new List<IMyFaction>();
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, x=> !x.IsBot);
            HashSet<long> playersIds = new HashSet<long>();

            foreach (var i in players) {
                playersIds.Add(i.PlayerID);
            }

            var total = 0;
            foreach (var x in MyAPIGateway.Session.Factions.Factions.Values) {
                bool found = false;

                foreach (var y in x.Members) {
                    var pid = y.Value.PlayerId;
                    if (playersIds.Contains(pid)) {
                        if (!found) {
                            factions.Add(x);
                        }
                        found = true;
                        playersIds.Remove(pid);
                    }
                }
            }
           
            return factions;
        }

        public static void Copy (this MatrixD m, ref Vector3D vec) {
            vec.X = m.M41;
            vec.Y = m.M42;
            vec.Z = m.M43;
        }
        
        public static long GetToolOwner (this IMyEngineerToolBase hnd) {
                if (hnd != null && hnd.OwnerId != 0) {
                    return GetPlayerByCharacter(hnd.OwnerId);
                } else {
                    return 0L;
                }
            }

            public static long GetToolOwner(this IMyHandheldGunObject<MyDeviceBase> hnd) {
                if (hnd != null && hnd.OwnerId != 0) {
                    return GetPlayerByCharacter(hnd.OwnerId);
                } else {
                    return 0L;
                }
            }


            public static IMyCharacter GetCharacter(this IMyHandheldGunObject<MyDeviceBase> hnd) {
                if (hnd != null && hnd.OwnerId != 0) {
                    var ch = MyEntities.GetEntityByIdOrDefault (hnd.OwnerId, null);
                    return ch as IMyCharacter;
                } else {
                    return null;
                }
            }

            public static bool IsMyTool(this IMyHandheldGunObject<MyDeviceBase> hnd) {
                var pl =  MyAPIGateway.Session.Player;
            
                if (pl==null) return false;
                return hnd.GetToolOwner() == pl.IdentityId;
            }

        public static bool isTorchServer (this IMySession session) {
            return MyAPIGateway.Utilities.IsDedicated;
        }
        public static string SubtypeName (this IMyCubeBlock block) {
            return block.SlimBlock.BlockDefinition.Id.SubtypeName;
        }

        public static T LoadWorldFile<T>(string file) {
            file += ".xml";
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(file, typeof(T))) {
                try 
                {
                    using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(file, typeof(T)))
                    {
                        return MyAPIGateway.Utilities.SerializeFromXML<T>(reader.ReadToEnd());
                    }
                } catch (Exception exc) {
                    Log.Error(exc);
                    return default(T);
                }
            } else {
                return default(T);
            }
        }

        public static T LoadWorldFile<T>(string file, Func<T> defaultGenerator)
        {
            file += ".xml";
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(file, typeof(T)))
            {
                try
                {
                    using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(file, typeof(T)))
                    {
                        return MyAPIGateway.Utilities.SerializeFromXML<T>(reader.ReadToEnd());
                    }
                }
                catch (Exception exc)
                {
                    Log.Error(exc);
                    return defaultGenerator();
                }
            }
            else
            {
                return defaultGenerator();
            }
        }


        public static String LoadPlainWorldFile<T>(string file) {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(file, typeof(T))) {
                try 
                {
                    using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(file, typeof(T)))
                    {
                        return reader.ReadToEnd();
                    }
                } catch (Exception exc) {
                    Log.Error(exc);
                    return "";
                }
            } else {
                return "";
            }
        }

        public static bool SaveWorldFile<T>(string file, T settings) {
            file += ".xml";
            try {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(file, typeof(T))) {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML<T>(settings));
                }
                return true;
            } catch (Exception exc) {
                Log.Error(exc);
                return false;
            }
        }

        
        public static IMyPlayer GetNearestPlayer(Vector3D checkCoords) {
            IMyPlayer thisPlayer = null;
            double distance = Double.MaxValue;

            var list = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(list); // MES_SessionCore.PlayerList????

            //Log.Info("TotalPlayers:" + list.Count);

            foreach (var player in list) {
                if (player.Character == null || player.IsBot == true) {
                    //Log.Info("Character == null || player.IsBot" + list.Count);
                    continue;
                }

                var currentDist = Vector3D.Distance(player.GetPosition(), checkCoords);


                //Log.Info("Check Character" + currentDist + " " + distance + " " + player);
                if (currentDist < distance) {
                    thisPlayer = player;
                    distance = currentDist;
                }

            }
            return thisPlayer;
        }

        public static bool spawnPrefab(this MyPrefabDefinition prefab, Vector3D pos, Vector3 forward, Vector3 up, long playerId, Action<MyObjectBuilder_EntityBase> beforeCreated, Action<IMyCubeGrid> onCreated, MyOwnershipShareModeEnum share = MyOwnershipShareModeEnum.Faction)
        {
            try
            {
                var gridOB = prefab.CubeGrids[0];
                
                gridOB.PositionAndOrientation = new MyPositionAndOrientation(pos, forward, up);
                MyAPIGateway.Entities.RemapObjectBuilder(gridOB);
                beforeCreated?.Invoke(gridOB);
                MyAPIGateway.Entities.CreateFromObjectBuilderParallel(gridOB, true, (x) =>
                {
                    var grid = x as IMyCubeGrid;
                    if (playerId != 0)
                    {
                        grid.ChangeGridOwnership(playerId, share);
                    }
                    onCreated(grid);
                });
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e);
                return false;
            }
        }

        public static bool spawnPrefab(this MyPrefabDefinition prefab, Vector3D pos, Vector3 forward, Vector3 up, long playerId, Action<IMyCubeGrid> onCreated, MyOwnershipShareModeEnum share = MyOwnershipShareModeEnum.Faction) {
            try {
                var gridOB = prefab.CubeGrids[0];
                var pos2 = MyAPIGateway.Entities.FindFreePlace(pos, prefab.BoundingBox.Size.Max()/2) ?? Vector3.Zero;
                if (pos2 == Vector3.Zero) { 
                    return false;
                }
                gridOB.PositionAndOrientation = new MyPositionAndOrientation(pos2, forward, up);
                MyAPIGateway.Entities.RemapObjectBuilder(gridOB);

                MyAPIGateway.Entities.CreateFromObjectBuilderParallel(gridOB, true, (x)=> {
                    var grid = x as IMyCubeGrid;
                    if (playerId != 0) {
                        grid.ChangeGridOwnership(playerId, share);
                    }
                    onCreated(grid);
                });
                return true;
            } catch (Exception e) {
                Log.Error(e);
                return false;
            }
        }


        public static MyObjectBuilder_CubeGrid projectPrefab(this MyPrefabDefinition prefab, Vector3D pos, Vector3D direction, long playerId, MyOwnershipShareModeEnum share = MyOwnershipShareModeEnum.Faction) {
            try {
                var x = new MyObjectBuilder_CubeGrid();
                var y = new MyCubeGrid();
                y.GetObjectBuilder();

                var gridOB = prefab.CubeGrids[0];
                var pos2 = MyAPIGateway.Entities.FindFreePlace(pos, prefab.BoundingBox.Size.Max());
                if (pos2 == null)
                {
                    return null;
                }
                //
                //gridOB.PositionAndOrientation = new MyPositionAndOrientation(pos2 ?? Vector3D.Zero, Vector3.Forward, Vector3.Up);
                //MyAPIGateway.Entities.RemapObjectBuilder(gridOB);
                return gridOB;
            } catch (Exception e)  {
                Log.Error(e);
                return null;
            }
        }
    }
}
