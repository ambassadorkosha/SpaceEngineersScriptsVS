using ProtoBuf;
using Sandbox.ModAPI;

namespace Scripts.Shared.Serialization
{
    [ProtoContract]
    public class IdData
    {
        [ProtoMember(1)] public long Id;
    }
}