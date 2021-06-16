using ParallelTasks;
using System;
using System.Collections.Generic;
using ProtoBuf;
using VRageMath;

namespace Scripts.Specials.Radar {

    [ProtoContract]
    public class ShipInfo {
         [ProtoMember(1)] public Vector3D position;
         [ProtoMember(2)] public Vector3 speed;
         [ProtoMember(3)] public int times;
         [ProtoMember(4)] public float size;
         [ProtoMember(5)] public float mass;
         [ProtoMember(6)] public string name;
         [ProtoMember(7)] public List<long> owners;
         [ProtoMember(8)] public int totalBlocks;
         [ProtoMember(9)] public float electricity;
         [ProtoMember(10)] public double height;

        public ShipInfo Init () {
            speed = new Vector3();
            return this;
        }

        public void Reset () {
            position.X = 0;
            position.Y = 0;
            position.Z = 0;
            speed.X = 0;
            speed.Y = 0;
            speed.Z = 0;
            size = 0;
            mass = 0;
            times = 0;
            name = null;
            owners = null;
            owners = null;
            totalBlocks = 0;
            electricity = 0;
        }

        public override string ToString() {
            return String.Format ("SI s/m/t spd={0} mass={1} elc={2} : {3} : {4}", speed, mass, electricity, times, name);
        }
    }

    [ProtoContract]
    public class PlayerInfo {
        [ProtoMember(1)] public Vector3D position;
        [ProtoMember(2)] public long id;
    }

    [ProtoContract]
    public class RadarInfo {
        [ProtoMember(1)] public List<ShipInfo> ships;
        [ProtoMember(2)] public List<PlayerInfo> player;
    }

    public class Spotted {
        public Vector3D position;
        public String name;
        public String desc;

        public Spotted (Vector3D position, String name, String desc) {
            this.position = position;
            this.name = name;
            this.desc = desc;
        }
    }

    class GPSTaskData : WorkData {
        public RadarInfo ri;
        public byte[] bytes;
        public List<Spotted> spotted;
    }
}
