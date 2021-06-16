using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using VRageMath;

namespace Scripts.Specials.POI
{
    [ProtoContract]
    public class POI
    {
        [ProtoMember(1)]
        public long Id = 0;
        [ProtoMember(2)]
        public Vector3D Position = Vector3D.Zero;
        [ProtoMember(3)]
        public float CantMineDistance = -1f;
        [ProtoMember(4)]
        public float CantEnableSafezone = -1f;
        [ProtoMember(5)]
        public float CantEnableBaseCores = -1f;
        [ProtoMember(6)]
        public string Name = "";
        [ProtoMember(7)]
        public GPS Gps = null;
        [ProtoMember(8)]
        public float CantJumpHere = -1f;

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"[ {Name} ] id:[ {Id} ] [{Position.X},{Position.Y},{Position.Z}]");
            sb.AppendLine($"Rules: cores:[{CantEnableBaseCores}] safezone:[{CantEnableSafezone}] mine:[{CantMineDistance}]");
            if (Gps == null) sb.AppendLine($"GPS: (Not set.)");
            else sb.AppendLine($"GPS: ([{ Gps.GPSName}] [{Gps.GPSDescription}] [{Gps.GPSPosition.X},{Gps.GPSPosition.Y},{Gps.GPSPosition.Z}])");

            return sb.ToString();
        }
    }

    [ProtoContract]
    public class GPS
    {
        [ProtoMember(1)]
        public string GPSName = "";
        [ProtoMember(2)]
        public string GPSDescription = "";
        [ProtoMember(3)]
        public Vector3 GPSColor = Color.Aqua;
        [ProtoMember(4)]
        public Vector3D GPSPosition = Vector3D.Zero;
    }

    [ProtoContract]
    public class Settings
    {
        [ProtoMember(1)]
        public List<POI> PoiList;
        [ProtoMember(2)]
        public MessageType MessageType;
    }

    public enum MessageType
    {
        Create,
        Delete,
        Modify,
        Request
    }
}

