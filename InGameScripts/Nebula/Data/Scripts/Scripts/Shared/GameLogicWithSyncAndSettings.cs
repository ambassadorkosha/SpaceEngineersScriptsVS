using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Slime;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using Digi;
using ProtoBuf;
using VRageMath;
using VRage.Utils;

namespace Scripts.Shared
{
    /*[ProtoContract]
    public class TestSettings
    {
        [ProtoMember(1)]
        public float CurrentThrust;
    }

    public class TestBlockSettings
    {
        public float FlameLength;
        public float MaxThrust;
    }

    
    public class TestGameLogic : GameLogicWithSyncAndSettings<TestSettings, TestBlockSettings, TestGameLogic>
    {
        private static Guid GUID = new Guid();
        private static Sync<TestSettings, TestGameLogic> sync;

        public override TestSettings GetDefaultSettings() { return new TestSettings { CurrentThrust = 0f }; }
        public override Guid GetGuid() { return GUID; }
        public override Sync<TestSettings, TestGameLogic> GetSync() { return sync; }
        public override TestBlockSettings InitBlockSettings() { 
            return new TestBlockSettings() { FlameLength = 5f }; 
        }

        public static void Init ()
        {
            sync = new Sync<TestSettings, TestGameLogic>(53334, (x)=>x.Settings, Handler);
        }

        protected override void OnSettingsChanged()
        {

        }

        public override void ApplyDataFromClient(TestSettings arrivedSettings)
        {
            Settings.CurrentThrust = MathHelper.Clamp(arrivedSettings.CurrentThrust, 0, BlockSettings.MaxThrust);
        }
    }*/

    public abstract class GameLogicWithSyncAndSettings<DynamicSettings, StaticSettings, FinalClass> : MyGameLogicComponent where FinalClass : GameLogicWithSyncAndSettings<DynamicSettings, StaticSettings, FinalClass>
    {
        /// <summary>
        /// Get guid, that belongs to this type of gamelogic. Must be STATIC and UNIQ per each nested class
        /// </summary>
        /// <returns></returns>
        public abstract Guid GetGuid();

        /// <summary>
        /// Get sync, that belongs to this type of gamelogic. Must be STATIC and UNIQ per each nested class
        /// </summary>
        /// <returns></returns>
        public abstract Sync<DynamicSettings, FinalClass> GetSync();

        /// <summary>
        /// Called, when data arrives on server from clients. 
        /// You must apply changes to gameLogic.Settings
        /// </summary>
        /// <param name="arrivedSettings">Data that arrived from client</param>
        public abstract void ApplyDataFromClient (DynamicSettings arrivedSettings,ulong userSteamId, byte type);


        /// <summary>
        /// If new block placed, what settings it will have?
        /// </summary>
        /// <returns></returns>
        public abstract DynamicSettings GetDefaultSettings();

        /// <summary>
        /// When block placed, we should define here static setting.
        /// </summary>
        /// <returns></returns>
        public abstract StaticSettings InitBlockSettings();

        /// <summary>
        /// Data that is automaticly transfered between client and server. It is also stored in settings.
        /// </summary>
        public DynamicSettings Settings;

        /// <summary>
        /// Data that is not changed at all. It is somthing like SBC values
        /// </summary>
        public StaticSettings BlockSettings;

        private static HashSet<Type> InitedControls = new HashSet<Type>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            LoadSettings();
            BlockSettings = InitBlockSettings();

            if (!MyAPIGateway.Session.IsServer)
            {
                GetSync().RequestData(Entity.EntityId);
            }

            //Init controls once;
            bool needInit = false;
            lock (InitedControls)
            {
                needInit = InitedControls.Add(GetType());
            }

            if (needInit)
            {
                InitControls();
            }
        }

        protected virtual void OnSettingsChanged() { }

        public static void Handler (FinalClass block, DynamicSettings settings, byte type,ulong userSteamId, bool isFromServer)
        {
            var tt = (GameLogicWithSyncAndSettings<DynamicSettings, StaticSettings, FinalClass>)block;

            if (isFromServer)
            {
                // Hate C# for bad generics
                tt.Settings = settings;
                tt.OnSettingsChanged();
            }
            else
            {
                tt.ApplyDataFromClient(settings, userSteamId, type);
                tt.NotifyAndSave();
                tt.OnSettingsChanged();
            }
        }

        #region Init Settings

        /// <summary>
        /// Must be called on client side, in Gui elements, or on Server side where data from client is arrived;
        /// </summary>
        public void NotifyAndSave()
        {
            try
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    GetSync().SendMessageToOthers(Entity.EntityId, Settings);
                    SaveSettings();
                }
                else
                {
                    var sync = GetSync();
                    if (sync != null)
                    {
                        sync.SendMessageToServer(Entity.EntityId, Settings);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ChatError("NotifyAndSave Exception " + ex.ToString() + ex.StackTrace);
            }
        }

        public override sealed void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            var st = (Entity as IMyEntity);
            if (st.Storage == null)
            {
                st.Storage = new MyModStorageComponent();
            }
        }

        public void LoadSettings()
        {
            if (!Entity.TryGetStorageData(GetGuid(), out Settings))
            {
                Settings = GetDefaultSettings();
                SaveSettings();
            }
        }
        
        public void SaveSettings()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Entity.SetStorageData(GetGuid(), Settings);
            }
        }

        #endregion

        public virtual void InitControls()
        {

        }
    }
}
