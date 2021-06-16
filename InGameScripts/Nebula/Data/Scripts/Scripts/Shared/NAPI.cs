using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
 using System.Globalization;
 using System.Linq;
using System.Text;
using System.Threading.Tasks;
 using Sandbox.Game.EntityComponents;
 using Scripts.Shared.Serialization;
 using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using Digi;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Collections;

namespace Slime {
    public static class NAPI {
        private const int FREEZE_FLAG = 4;
        public static bool isFrozen(this IMyEntity grid) { return ((int)grid.Flags | FREEZE_FLAG) == (int)grid.Flags; }
        public static void setFrozen(this IMyEntity grid) { grid.Flags = grid.Flags | (EntityFlags)FREEZE_FLAG; }
        public static void setUnFrozen(this IMyEntity e) { e.Flags &= ~(EntityFlags)FREEZE_FLAG; }

        public static T As<T>(this long entityId) {
            IMyEntity entity;
            if (!MyAPIGateway.Entities.TryGetEntityById(entityId, out entity)) return default(T);
            if (entity is T) { return (T) entity; } else { return default(T); }
        }

        public static void FindFatBlocks<T>(this IMyCubeGrid grid, List<T> blocks, Func<IMyCubeBlock, bool> filter) {
            var gg = grid as MyCubeGrid;
            var ff = gg.GetFatBlocks();
            foreach (var x in ff) {
                var fat = (IMyCubeBlock) x;
                if (filter(fat)) { blocks.Add((T) fat); }
            }
        }

        public static void OverFatBlocks(this IMyCubeGrid grid, Action<IMyCubeBlock> action) {
            var gg = grid as MyCubeGrid;
            var ff = gg.GetFatBlocks();
            foreach (var x in ff) {
                var fat = (IMyCubeBlock) x;
                action(fat);
            }
        }
        
        public static List<IMyCubeGrid> GetConnectedGrids(this IMyCubeGrid grid, GridLinkTypeEnum with, List<IMyCubeGrid> list = null, bool clear = false) { 
            if (list == null) list = new List<IMyCubeGrid>();
            if (clear) list.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, with, list);
            return list;
        }

        public static IMyFaction PlayerFaction(this long playerId) { return MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId); }

        public static IMyPlayer GetPlayer(this IMyCharacter character) { return MyAPIGateway.Players.GetPlayerControllingEntity(character); } //CAN BE NULL IF IN COCKPIT
        public static IMyPlayer GetPlayer(this IMyShipController cockpit) { return MyAPIGateway.Players.GetPlayerControllingEntity(cockpit); }
        public static IMyPlayer GetPlayer(this IMyIdentity Identity) { 
            IMyPlayer player = null;
            MyAPIGateway.Players.GetPlayers(null, (x) => {
                if (x.IdentityId == Identity.IdentityId)
                {
                    player = x;
                }
                return false;
            });
            return player;
        }


        public static bool IsOnline (this IMyPlayerCollection players, long identity)
        {
            bool contains = false;
            players.GetPlayers (null, (x)=>{
                if (x.IdentityId == identity)
                {
                    contains = true;
                }
                return false;
            });

            return contains;
        }

        public static bool IsControllingCockpit (this IMyShipController cockpit)
        {
            if (cockpit.IsMainCockpit)
            {
                return true;
            }
            else if (cockpit.ControllerInfo != null && cockpit.ControllerInfo.Controller != null && cockpit.ControllerInfo.Controller.ControlledEntity != null)
            {
                return true;
            }

            return false;
        }

        public static bool IsMainControlledCockpit(this IMyShipController cockpit)
        {
            return cockpit.ControllerInfo != null && cockpit.ControllerInfo.Controller != null && cockpit.ControllerInfo.Controller.ControlledEntity != null;
        }

        public static IMyCubeGrid GetMyControlledGrid(this IMySession session) {
            var cock = MyAPIGateway.Session.Player.Controller.ControlledEntity as IMyCockpit;
            if (cock == null) return null;
            return cock.CubeGrid;
        }

        public static IMyFaction Faction(this long factionId)
        {
            return MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
        }

		public static bool isBot (this IMyIdentity identity)
		{
			return MyAPIGateway.Players.TryGetSteamId(identity.IdentityId) == 0;
		}

        public static bool IsUserAdmin(this ulong SteamUserId)
        {
            return MyAPIGateway.Session.IsUserAdmin(SteamUserId);
        }
        
        public static MyPromoteLevel PromoteLevel (this ulong SteamUserId)
        {
            var PlayersList = new List<IMyPlayer>(); 
            MyAPIGateway.Players.GetPlayers(PlayersList);
            foreach (var Player in PlayersList.Where(Player => Player.SteamUserId == SteamUserId))
            { 
                return Player.PromoteLevel;
            }
            return MyPromoteLevel.None;
        }
        
        public static IMyIdentity Identity (this ulong SteamUserId)
        {
            IMyIdentity identity = null; 
            MyAPIGateway.Multiplayer.Players.GetAllIdentites(null, (x)=>
            {
                if (identity != null) return false;
                var st = MyAPIGateway.Multiplayer.Players.TryGetSteamId(x.IdentityId);
                if (st == SteamUserId)
                {
                    identity = x;
                }
                return false;
            });
            return identity;
        }

        public static BoundingBoxD GetAABB (this List<IMyCubeGrid> grids)
        {
            var aabb1 = grids[0].PositionComp.WorldAABB;
            BoundingBoxD aabb = new BoundingBoxD(aabb1.Min, aabb1.Max);
            for (var x=1; x<grids.Count; x++)
            {
                aabb.Include (grids[x].PositionComp.WorldAABB);
            }
            return aabb;
        }



        public static bool IsSameFaction(this long playerId, long player2Id) {
            if (playerId == player2Id) return true;

            var f1 = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            var f2 = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player2Id);

            if (f1 == f2) { return f1 != null; }

            return false;
        }

        public static Vector3D GetWorldPosition(this IMyCubeBlock slim) { return slim.CubeGrid.GridIntegerToWorld(slim.Position); }

        public static BoundingBoxD GetWorldAABB(this IMyCubeBlock slim) {
            var cb = slim.CubeGrid as MyCubeGrid;
            return new BoundingBoxD(slim.Min * cb.GridSize - cb.GridSizeHalfVector, slim.Max * cb.GridSize + cb.GridSizeHalfVector).TransformFast(cb.PositionComp.WorldMatrix);
        }
        public static MatrixD GetWorldMatrix(this IMyCubeBlock slim) { //TODO: under development
            var cb = slim.CubeGrid as MyCubeGrid;
            return new MatrixD();
        }
        
        public static List<IMyEntity> GetEntitiesInSphere (this IMyEntities entities, Vector3D pos, double radius, Func<IMyEntity, bool> filter = null) {
            var sphere = new BoundingSphereD(pos, radius);
            var list = entities.GetEntitiesInSphere(ref sphere);
            if (filter != null) { list.RemoveAll((x)=>!filter(x)); }
            return list;
        }


        public static void SendMessageToOthersProto(this IMyMultiplayer multi, ushort id, object o, bool reliable = true) {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(o);
            multi.SendMessageToOthers(id, bytes, reliable);
        }

        // Calculates how much components are welded for block
        public static void GetRealComponentsCost(this IMySlimBlock block, Dictionary<string, int> dictionary, Dictionary<string, int> temp = null)
        {
            if (temp == null) temp = new Dictionary<string, int>();
            var components = (block.BlockDefinition as MyCubeBlockDefinition).Components;
            foreach (var component in components)
            {
                string name = component.Definition.Id.SubtypeName;
                int count = component.Count;

                if (dictionary.ContainsKey(name)) dictionary[name] += count;
                else dictionary.Add(name, count);
            }

            temp.Clear();
            block.GetMissingComponents(temp);

            foreach (var component in temp)
            {
                string name = component.Key;
                int count = component.Value;

                if (dictionary.ContainsKey(name)) dictionary[name] -= count;
                else dictionary.Add(name, count);
            }
        }

        public static bool ParseHumanDefinition(string type, string subtype, out MyDefinitionId id)
        {
            if (type == "i" || type == "I")
            {
                type = "Ingot";
            }
            else if (type == "o" || type == "O")
            {
                type = "Ore";
            }
            else if (type == "c" || type == "C")
            {
                type = "Component";
            }
            return MyDefinitionId.TryParse("MyObjectBuilder_" + type + "/" + subtype, out id);
        }

        public static ListReader<MyCubeBlock> GetFatBlocks (this IMyCubeGrid grid)
        {
           return ((MyCubeGrid)grid).GetFatBlocks();
        }
    }

    public static class RandomSugar {
        public static Vector3 NextVector(this Random random, float x, float y, float z)
        {
            x = x * (float)(2*random.NextDouble()-1d);
            y = y * (float)(2*random.NextDouble()-1d);
            z = z * (float)(2*random.NextDouble()-1d);
            return new Vector3(x, y, z);
        }

        public static double NextDouble(this Random random, double min, double max)
        {
            return min + (max - min) * random.NextDouble();
        }

        public static float NextFloat(this Random random, double min, double max)
        {
            return (float)(min + (max - min) * random.NextDouble());
        }

        public static T Next<T> (this Random random, List<T> array)
        {
            if (array.Count == 0) return default (T);
            return array[random.Next()%array.Count];
        }

        public static T NextWithChance<T>(this Random random, List<T> array, Func<T, float> func, bool returnLastAsDefault = false)
        {
            if (array.Count == 0) return default(T);
            for (int x=0; x<array.Count; x++)
            {
                var a = array[x];
                var ch = func(a);
                if (random.NextDouble() <= ch)
                {
                    return a;
                }
            }

            return returnLastAsDefault ? array[array.Count-1] : default (T);
        }
    }
    
    public static class Serialization {
        public static StringBuilder Serialize(this StringBuilder sb, float f) { sb.Append(f.ToString(CultureInfo.InvariantCulture)); return sb; } 
        public static StringBuilder Serialize(this StringBuilder sb, double f) { sb.Append(f.ToString(CultureInfo.InvariantCulture)); return sb; } 
        public static StringBuilder Serialize(this StringBuilder sb, int f) { sb.Append(f.ToString(CultureInfo.InvariantCulture)); return sb; } 
        public static StringBuilder Serialize(this StringBuilder sb, long f) { sb.Append(f.ToString(CultureInfo.InvariantCulture)); return sb; }

        public static MyModStorageComponentBase GetOrCreateStorage(this IMyEntity entity) { return entity.Storage = entity.Storage ?? new MyModStorageComponent(); }
        public static bool HasStorage(this IMyEntity entity) { return entity.Storage != null; }


        public static bool TryGetStorageData<T>(this IMyEntity entity, Guid guid, out T value)
        {
            if (entity.Storage == null)
            {
                entity.Storage = new MyModStorageComponent();
                value = default(T);
                return false;
            }
            else
            {
                var d = entity.GetStorageData(guid);
                if (d == null)
                {
                    value = default(T);
                    return false;
                }

                try
                {
                    value = MyAPIGateway.Utilities.SerializeFromXML<T>(d);
                    return true;
                }
                catch (Exception e)
                {
                    value = default(T);
                    return false;
                }
            }
        }

        public static string GetStorageData(this IMyEntity entity, Guid guid) {
            if (entity.Storage == null) return null;
            string data = null;
            if (entity.Storage.TryGetValue(guid, out data)) {
                return data;
            } else {
                return null;
            }
        }
        
        public static string GetAndSetStorageData(this IMyEntity entity, Guid guid, string newData) {
            var data = GetStorageData(entity, guid);
            SetStorageData(entity, guid, newData);
            return data;
        }
        
        public static void SetStorageData(this IMyEntity entity, Guid guid, String data) {
            if (entity.Storage == null && data == null) {
                return;
            }
            entity.GetOrCreateStorage().SetValue(guid, data);
        }

        public static void SetStorageData<T>(this IMyEntity entity, Guid guid, T data)
        {
            var s = MyAPIGateway.Utilities.SerializeToXML<T>(data);
            SetStorageData(entity, guid, s);
        }
    }
    

    public static class Sharp {
        public static int NextExcept(this Random r, HashSet<int> ignored, int max) {
            while (true) {
                var n = r.Next(max);
                if (!ignored.Contains(n)) { return n; }
            }
        }
    }

    public static class Bytes {
        public static int Pack(this byte[] bytes, int pos, int what) {
            var b1 = BitConverter.GetBytes(what);
            bytes[pos + 0] = b1[0];
            bytes[pos + 1] = b1[1];
            bytes[pos + 2] = b1[2];
            bytes[pos + 3] = b1[3];
            return 4;
        }

        public static int Pack(this byte[] bytes, int pos, byte what) {
            bytes[pos] = what;
            return 1;
        }

        public static int Pack(this byte[] bytes, int pos, uint what) {
            var b1 = BitConverter.GetBytes(what);
            bytes[pos + 0] = b1[0];
            bytes[pos + 1] = b1[1];
            bytes[pos + 2] = b1[2];
            bytes[pos + 3] = b1[3];
            return 4;
        }
		public static int Pack(this byte[] bytes, int pos, float what)
		{
			var b1 = BitConverter.GetBytes(what);
			bytes[pos + 0] = b1[0];
			bytes[pos + 1] = b1[1];
			bytes[pos + 2] = b1[2];
			bytes[pos + 3] = b1[3];
			return 4;
		}

		public static int Pack(this byte[] bytes, int pos, long what) {
            var b1 = BitConverter.GetBytes(what);
            bytes[pos + 0] = b1[0];
            bytes[pos + 1] = b1[1];
            bytes[pos + 2] = b1[2];
            bytes[pos + 3] = b1[3];
            bytes[pos + 4] = b1[4];
            bytes[pos + 5] = b1[5];
            bytes[pos + 6] = b1[6];
            bytes[pos + 7] = b1[7];
            return 8;
        }

        public static int Pack(this byte[] bytes, int pos, ulong what) {
            var b1 = BitConverter.GetBytes(what);
            bytes[pos + 0] = b1[0];
            bytes[pos + 1] = b1[1];
            bytes[pos + 2] = b1[2];
            bytes[pos + 3] = b1[3];
            bytes[pos + 4] = b1[4];
            bytes[pos + 5] = b1[5];
            bytes[pos + 6] = b1[6];
            bytes[pos + 7] = b1[7];
            return 8;
        }

        public static long Long(this byte[] bytes, int pos) { return BitConverter.ToInt64(bytes, pos); }
        public static ulong ULong(this byte[] bytes, int pos) { return BitConverter.ToUInt64(bytes, pos); }
        public static double Double(this byte[] bytes, int pos) { return BitConverter.ToDouble(bytes, pos); }
        public static int Int(this byte[] bytes, int pos) { return BitConverter.ToInt32(bytes, pos); }
        public static uint UInt(this byte[] bytes, int pos) { return BitConverter.ToUInt32(bytes, pos); }
        public static float Float(this byte[] bytes, int pos) { return BitConverter.ToSingle(bytes, pos); }
        public static short Short(this byte[] bytes, int pos) { return BitConverter.ToInt16(bytes, pos); }
        public static ushort UShort(this byte[] bytes, int pos) { return BitConverter.ToUInt16(bytes, pos); }
    }
}