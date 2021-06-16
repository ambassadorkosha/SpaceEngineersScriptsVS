using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using VRage.Game.ModAPI;
using VRageMath;

namespace Slime {
    public class TorchConnection {

		public static bool IsLimitedBlock(string blockPairName)
		{
			if (MyAPIGateway.Session.isTorchServer()) Common.SendChatMessage("CreateNPC");
			return false;
		}

		public static long CreateNPC(IMyFaction faction)
		{
			if (MyAPIGateway.Session.isTorchServer()) Common.SendChatMessage("CreateNPC");
			return 0;
			//DONT WORK WITHOUT PLUGIN
		}

		public static void TransferPCU(List<IMySlimBlock> blocks, long newOwner)
		{
			if (MyAPIGateway.Session.isTorchServer()) Common.SendChatMessage("FreezerLockGrid");
			//DONT WORK WITHOUT PLUGIN
		}

		public static void FreezerLockGrid(IMyCubeGrid g1) {
            if (MyAPIGateway.Session.isTorchServer()) Common.SendChatMessage("FreezerLockGrid");
            //DONT WORK WITHOUT PLUGIN
        }

		public static void FreezerUnlockGrid(IMyCubeGrid g2) {
            if (MyAPIGateway.Session.isTorchServer()) Common.SendChatMessage("FreezerUnlockGrid");
            //DONT WORK WITHOUT PLUGIN
        }

        public static IMyCharacter CreateCharacter(MatrixD position, Vector3 velocity, string name, string model, Vector3? colormask) {
            if (MyAPIGateway.Session.isTorchServer()) Common.SendChatMessage("CreateCharacter Mod:" + name + " " + model);
            return null; //RETURNS CAN JUMP OR NOT
        }
        
        public static void SetSafezoneOptions (MySafeZone zone, float radius, bool factionsBlackList, bool entitiesBlackList, bool playersBlackList, List<long> factions, List<long> entities, List<long> players, int allowedActions) {
            //SPP CODE
        }

        public static Dictionary<long, HashSet<string>> GetSpecialPlayerAbilities() {
            if (MyAPIGateway.Session.isTorchServer()) Common.SendChatMessage("GetSpecialPlayerAbilities");
            //DONT WORK WITHOUT PLUGIN
            return new Dictionary<long, HashSet<string>>();
        }
        
        //CALLBACK
        public static bool AllowJumpDrive(MyCubeGrid grid, Vector3D where, long user) {
            if (MyAPIGateway.Session.isTorchServer()) 
            {
               // Common.SendChatMessage("AllowJumpDrive");
            }
            return true;//JumpdriveSystem.AllowJumpDrive (grid, where, user);
        }
        public static void SafeZoneActions (MySafeZone zone, int allowedActions) 
        {
            
        }
        public static void SafeZoneColor (MySafeZone zone, Color color) 
        {
            
        }

		public static void SafeZoneTexture (MySafeZone zone, string texture)
        {
            
        }
        public static void SafeZonePlayersInGrids (MySafeZone zone, List<IMyPlayer> players)
        {
            
        }
        public static void SafeZoneFactions (MySafeZone zone, bool WhiteList, List<IMyFaction> factions)
        {
            
        }
    }
}
