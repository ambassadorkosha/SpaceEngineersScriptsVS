using ServerMod;
using VRage.Game;
using VRage.Game.Components;

namespace Scripts.Shared
{
    public abstract class SessionComponentWithSettings<T> : MySessionComponentBase
    {
        protected T Settings;
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Settings = Other.LoadWorldFile<T>(GetFileName(), GetDefault);
        }
        public override void SaveData()
        {
            Other.SaveWorldFile(GetFileName(), Settings);
            base.SaveData();
        }

        protected abstract T GetDefault();
        protected abstract string GetFileName();
    }

    public abstract class SessionComponentWithSyncSettings<T> : MySessionComponentBase
    {
        StaticSync<T> Sync;
        T Settings;
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Settings = Other.LoadWorldFile<T>(GetFileName(), GetDefault);
            Sync = new StaticSync<T>(GetPort(), () => Settings, HandleData);
        }

        public override void SaveData()
        {
            base.SaveData();
            Other.SaveWorldFile(GetFileName(), Settings);
        }

        protected abstract void HandleData(T data, byte action, ulong player, bool isFromServer);
        protected abstract T GetDefault();
        protected abstract string GetFileName();
        protected abstract ushort GetPort();
    }
}
