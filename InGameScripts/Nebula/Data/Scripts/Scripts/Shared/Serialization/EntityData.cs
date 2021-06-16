using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace Scripts.Shared.Serialization {

    public class SimpleData {
        public double d1, d2, d3;
        public long l1, l2, l3;
        public string s1, s2, s3;
        
        public String Serialize() {
            return MyAPIGateway.Utilities.SerializeToXML(this);
        }

        public static SimpleData Deserialize(string s) {
            return MyAPIGateway.Utilities.SerializeFromXML<SimpleData>(s);
        }
    }
    
    public class EntityData {
        public long entity;
        public int type;
        public float f1,f2,f3,f4;
        public double d1,d2,d3,d4;
        public long l1,l2,l3,l4;
        public int i1,i2,i3,i4;
        public string s1,s2,s3,s4;

        public EntityData() {
            
        }

        public EntityData(IMyEntity e) { entity = e.EntityId; }
        public EntityData(IMyEntity e, int type) { 
            entity = e.EntityId;
            this.type = type;
        }

        public String Serialize() {
            return MyAPIGateway.Utilities.SerializeToXML(this);
        }

        public static EntityData Deserialize(string s) {
            return MyAPIGateway.Utilities.SerializeFromXML<EntityData>(s);
        }
    }
    
    public class EntityListData {
        public List<EntityData> data = new List<EntityData>();
        
        public String Serialize() {
            return MyAPIGateway.Utilities.SerializeToXML(this);
        }

        public static EntityListData Deserialize(string s) {
            return MyAPIGateway.Utilities.SerializeFromXML<EntityListData>(s);
        }
    }
    
    
}