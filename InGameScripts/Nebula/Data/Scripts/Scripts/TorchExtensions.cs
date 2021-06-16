using Scripts.Shared;
using System;
using VRageMath;
using Digi;

using Invoke = System.Func<object, object[], object>;
using Getter = System.Func<object, object>;
using Setter = System.Action<object, object>;
using Sandbox.ModAPI;
using Scripts.Specials.POI;

namespace Scripts
{
    public static class TorchExtensions
    {
        public static Func<string, string, Func<object, object[], object>> invokeFabric;
        public static Func<string, string, Getter> getterFabric;
        public static Func<string, string, Setter> setterFabric;
        public static Getter SimulationFrameCounter;
        //functions from plugin
        public static Action<long, string, string, Vector3, Color, TimeSpan?, bool, bool, bool, bool> AddGPS =
            (identity, name, description, coords, color, discardAt, alwaysVisible, showOnHud, isObjective, playSound) => { };
        //mod functions for send to plugin
        public static Func<Vector3, bool> CanMineHere = (position) =>
        {
            // ModConnection.Log("MOD CanMineHere called!");
            return POICore.CanMine(position);
        };
        public static Func<Vector3, bool> CanJumpHere = (position) =>
        {
            //  ModConnection.Log("MOD CanJumpHere called!");
            return POICore.CanJumpHere(position);
        };


        public static void Init()
        {
            ModConnection.SetValue("MIG.VoxelProtector.CanMineHere", CanMineHere); //"send" func to plugin
            ModConnection.SetValue("MIG.APIExtender.CanJumpHere", CanJumpHere);
            ModConnection.Subscribe<Action<long, string, string, Vector3, Color, TimeSpan?, bool, bool, bool, bool>>("MIG.APIExtender.AddGPS", (impl) => { AddGPS = impl; });
            ModConnection.Subscribe<Func<string, string, Invoke>>("MIG.APIExtender.InvokeFabric", (impl) => { invokeFabric = impl; OnReflectionUtilsReady(); });
            ModConnection.Subscribe<Func<string, string, Getter>>("MIG.APIExtender.GetterFabric", (impl) => { getterFabric = impl; OnReflectionUtilsReady(); });
            ModConnection.Subscribe<Func<string, string, Setter>>("MIG.APIExtender.SetterFabric", (impl) => { setterFabric = impl; OnReflectionUtilsReady(); });
        }

        public static void OnReflectionUtilsReady()
        {
            //Log.ChatError("OnReflectionUtilsReady:TST");
            if (invokeFabric != null && getterFabric != null && setterFabric != null)
            {
                //Log.ChatError("OnReflectionUtilsReady:INIT");
                SimulationFrameCounter = getterFabric("Sandbox.Engine.Platform.Game", "SimulationFrameCounter");
                //ur functions here

                //
            }
        }

        public static ulong CurrentFrame()
        {
            if (SimulationFrameCounter == null)
            {
                return 666;
            }
            return (ulong)SimulationFrameCounter(MyAPIGateway.Session);
        }
    }
}
