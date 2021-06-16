using ProtoBuf;
using Sandbox.ModAPI;

namespace Scripts.Shared.Serialization
{
    [ProtoContract]
    public class StringData
    {
        [ProtoMember(1)] public long Id;
        [ProtoMember(2)] public string Data;
            
        public byte[] Serialize()
        {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }

        public static StringData DeSerialize(byte[] data)
        {
            return MyAPIGateway.Utilities.SerializeFromBinary<StringData>(data);
        }
    }
}