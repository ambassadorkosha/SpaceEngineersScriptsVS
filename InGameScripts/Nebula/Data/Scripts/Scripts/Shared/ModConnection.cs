﻿using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Utils;
using ServerMod;

namespace Scripts.Shared
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    class ModConnectionComponent : MySessionComponentBase
    {
        public ModConnectionComponent() { }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            MyLog.Default.Error("ModConnectionComponent:MOD Init");
            ModConnection.Init();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            ModConnection.Close();
        }
    }

    public static class ModConnection
    {
        private static string TAG = "MOD";
        private static int PORT1 = 6666666;
        private static int PORT2 = 6666667;
        private static Dictionary<string, object> Data;
        private static Dictionary<string, List<Action<string, object>>> Subscriptions = new Dictionary<string, List<Action<string, object>>>();
        private const bool DEBUGLOG = true;
        private static List<KeyValuePair<string, object>> RegisterQueue = new List<KeyValuePair<string, object>>();

        private static bool IsMain = false;
        public static bool IsInited => ModConnection.Data != null;


        public static void Close()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(PORT1, ConnectionPortHandler);
            MyAPIGateway.Utilities.UnregisterMessageHandler(PORT2, NotifyChannelHandler);
        }

        public static void Init()
        {
            if (IsInited) throw new Exception("ModConnection already inited!");
            Log("ModConnectionComponent:MOD Init");
            MyAPIGateway.Utilities.RegisterMessageHandler(PORT1, ConnectionPortHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(PORT2, NotifyChannelHandler);

            MyAPIGateway.Utilities.SendModMessage(PORT1, null);

            if (Data == null)
            {
                IsMain = true;
                Data = new Dictionary<string, object>();
                foreach (var x in RegisterQueue) //We have to react on own methods too
                {
                    SetValue(x.Key, x.Value, crashOnDuplicate: true);
                }
                //We dont need react, because we are subscribed on NotifyChannel
            }
            else
            {
                foreach (var x in Data)
                {
                    Handle(x.Key, x.Value);
                }
                foreach (var x in RegisterQueue)
                {
                    SetValue(x.Key, x.Value, crashOnDuplicate: true);
                }
            }
        }


        private static void ConnectionPortHandler(object data)
        {
            Log("ConnectionPortHandler");
            if (data == null) //Request data
            {
                if (IsMain && Data != null)
                {
                    Log("Request data !" + TAG);
                    MyAPIGateway.Utilities.SendModMessage(PORT1, Data);
                }
                else
                {
                    Log("Request data Error ! " + TAG + " [" + data + "]");
                    //Ignore we are not Main, or not inited yet.
                }
            }
            else
            {
                var fn = data as Dictionary<string, object>;
                if (fn != null)
                {
                    Log("Arrived data! " + TAG);
                    Data = fn;
                }
                else
                {
                    Log("WTF? Erorr1 ! " + TAG);
                    //possible trash;
                }
            }
        }

        public static void Log(string data)
        {
            if (DEBUGLOG)
            {
                MyLog.Default.Error($"MCon {TAG}: {data}");
            }
        }

        private static void NotifyChannelHandler(object data)
        {
            var pair = data as KeyValuePair<string, object>?;
            if (!pair.HasValue)
            {
                Log("Something wrong");
                return;
            }
            var d = pair.Value;

            if (!Data.ContainsKey(d.Key))
            {
                Log($"Desynchronization [{d.Key}]/[{d.Value}] -> [{d.Key}]/[null]");
            }
            else
            {
                if (Data[d.Key] != d.Value)
                {
                    Log($"Desynchronization [{d.Key}]/[{d.Value}] -> [{d.Key}]/[{Data[d.Key]}]");
                }
            }

            Log($"Registered [{d.Key}]->[{d.Value}]");
            Handle(d.Key, d.Value);
        }

        private static string ALL = "";
        private static void Handle(string Name, object O)
        {
            if (Name != ALL)
            {
                if (Subscriptions.ContainsKey(Name))
                {
                    foreach (var x in Subscriptions[Name])
                    {
                        try
                        {
                            x(Name, O);
                        }
                        catch (Exception e)
                        {
                            Log($"ModConnection: Exception for [{Name}] : {e.ToString()}");
                        }
                    }
                }
            }

            if (Subscriptions.ContainsKey(ALL))
            {
                foreach (var x in Subscriptions[ALL])
                {
                    try
                    {
                        x(Name, O);
                    }
                    catch (Exception e)
                    {
                        Log($"ModConnection: Exception for [{Name}] : {e.ToString()}");
                    }
                }
            }
        }





        public static void SetValue(string Name, object Data, bool crashOnDuplicate = false, bool notify = true)
        {
            if (ModConnection.Data == null)
            {
                RegisterQueue.Add(new KeyValuePair<string, object>(Name, Data));
            }
            else
            {
                if (crashOnDuplicate && ModConnection.Data.ContainsKey(Name))
                {
                    PrintAllData();
                    throw new Exception($"Key already exists {Name} : [{ModConnection.Data[Name]}");
                }

                ModConnection.Data[Name] = Data;
                if (notify) MyAPIGateway.Utilities.SendModMessage(PORT2, new KeyValuePair<string, object>(Name, Data));
            }
        }

        public static T Get<T>(string Name)
        {
            object o;
            if (Data.TryGetValue(Name, out o))
            {
                if (o is T)
                {
                    return (T)o;
                }
            }

            return default(T);
        }

        public static void Subscribe(string Name, Action<string, object> OnDataArrivedOrChanged)
        {
            Subscriptions.GetOrCreate(Name).Add(OnDataArrivedOrChanged);
            if (Data.ContainsKey(Name))
            {
                OnDataArrivedOrChanged(Name, Data[Name]);
            }
        }

        public static void Subscribe<T>(string Name, Action<T> OnDataArrivedOrChanged)
        {
            Subscribe(Name, (name, data) => OnDataArrivedOrChanged((T)data));
        }

        public static void SetValueAndSubscribe<T>(string Name, T Data, Action<T> OnDataArrivedOrChanged, bool crashOnDumplicate = true)
        {
            Subscribe(Name, OnDataArrivedOrChanged);
        }

        public static void SubscribeToAll(Action<string, object> OnDataArrivedOrChanged)
        {
            Subscribe(ALL, OnDataArrivedOrChanged);
        }

        public static void PrintAllData()
        {
            var sb = new StringBuilder("ModConnection:\n");
            foreach (var x in Data)
            {
                sb.AppendLine($"{x.Key} -> {x.Value}");
            }

            Log(sb.ToString());
        }
    }
}