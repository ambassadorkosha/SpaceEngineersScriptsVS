using ProtoBuf;
using System.Collections.Generic;
using VRageMath;

namespace Scripts.Specials.Systems
{
    public enum HungerGameState
    {
        WRONG_STATE,
        WAITING_FOR_START,
        INITING,
        RUNNING_SPHERE_STATIC,
        RUNNING_SPHERE_RESIZING
    }

    public class HungerGameSettings
    {
        [ProtoMember(1)] public string Name = null; //seconds from 1970
        [ProtoMember(10)] public bool Enabled = false;
        [ProtoMember(2)] public long FirstCapture = -1; //seconds from 1970
        [ProtoMember(3)] public long WaitingForPeopleTime = 60 * 2;
        [ProtoMember(4)] public long EventDuration = 60 * 3;
        [ProtoMember(5)] public long EventInterval = 60 * 10;
        [ProtoMember(6)] public long MinimumPlayers = 1;
        [ProtoMember(7)] public long MaximumPlayers = 16;


        [ProtoMember(8)] public BoundingSphereD GameSphere;

        public override string ToString()
        {
            return $"[FC={FirstCapture} WT={WaitingForPeopleTime} ED={EventDuration} EI={EventInterval} M={MinimumPlayers}|{MaximumPlayers} S=({GameSphere.Radius}:{(int)GameSphere.Center.X} {(int)GameSphere.Center.Y} {(int)GameSphere.Center.Z})]";
        }

    }

    public class HungerGamesSettings
    {
        [ProtoMember(1)] public int Quality = 60;
        [ProtoMember(2)] public int DrawDistance = 10000;
        //[ProtoMember(2)] public HungerGameState PrevState;
        //[ProtoMember(2)] public HungerGameState PrevState;
    }

    public class HungerGameParams
    {
        [ProtoMember(1)] public HungerGameState State = HungerGameState.WRONG_STATE;
        [ProtoMember(2)] public HungerGameState PrevState = HungerGameState.WRONG_STATE;

        [ProtoMember(3)] public BoundingSphereD NoneDamageSphere = new BoundingSphereD();
        [ProtoMember(4)] public BoundingSphereD DamageSphere = new BoundingSphereD();

        //TempForDrawing
        [ProtoMember(5)] public BoundingSphereD PrevDamageSphere = new BoundingSphereD();

        [ProtoMember(6)] public List<long> PlayingPlayers = new List<long>();
        [ProtoMember(7)] public Dictionary<long, long> PlayerToTeam = new Dictionary<long, long>();



        public float transformProgress;
        public float transformTime;


        public int GetCurrentZone()
        {
            return 1;
        }

        public float GetZoneTransformation()
        {
            return 0.2f;
        }

        //public float GetNextZoneTransition()
        //{
        //
        //}

    }
}
