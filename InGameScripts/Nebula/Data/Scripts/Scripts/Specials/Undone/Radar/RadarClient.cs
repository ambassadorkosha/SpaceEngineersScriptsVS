using Sandbox.ModAPI;
using Digi;
using ParallelTasks;
using System;
using Scripts.Shared;
using Scripts.Specials.Radar;
using VRageMath;
using System.Collections.Generic;
using Sandbox.Game.Entities;

namespace ServerMod.Radar {
    class RadarClient {
        public static void Init() { MyAPIGateway.Multiplayer.RegisterMessageHandler(999, handleMessage); }
        public static void Close() { MyAPIGateway.Multiplayer.UnregisterMessageHandler(999, handleMessage); }
        public static void handleMessage(byte[] bytes) {
            //Log.Info("RadarClient : handleMessage");
            //var data = new GPSTaskData();
            //data.bytes = bytes;
            //MyAPIGateway.Parallel.Start(DoProcess, DoneProcess, data);
        }

        public static void DoProcess(WorkData workData) {
            try {
                if (MyAPIGateway.Session.Player == null) return;


                //Log.Info("RadarClient : Do");
                var d = workData as GPSTaskData;

                MyAPIGateway.Session.Player.GetPosition();

                d.ri = MyAPIGateway.Utilities.SerializeFromBinary<RadarInfo>(d.bytes);

                if (d.ri.ships != null) {
                    //Log.Info("RadarClient : Start Spots:" + d.spotted);
                    var myPos = MyAPIGateway.Session.Player.GetPosition();
                    var myPlanet = myPos.GetPlanet();
                    d.spotted = new List<Spotted>();
                    foreach (var x in d.ri.ships) {
                        var spot = RadarConsts.GetSpot(x, myPos, myPlanet);
                        if (spot != null) {
                            d.spotted.Add(spot);
                        }
                    }
                    //Log.Info("RadarClient : Spots:" + d.spotted);
                } else {
                    //Log.Error("RadarClient : Ships null");
                }

                //Log.Info("RadarClient : Do Deserialized");
            } catch (Exception e) {
                Log.Error(e, "RADAR");
            }
        }
        public static void DoneProcess(WorkData workData) {
            try {
                 if (MyAPIGateway.Session.Player == null) return;

                var d = workData as GPSTaskData;
                Gps.RemoveWithDescription("AR:");
                if (d.spotted != null) {
                    var i = 0;
                    foreach (var x in d.spotted) {
                        Gps.AddGps("#"+(i++) + " " +x.name, "AR:" + x.desc, x.position);
                    }
                } else {
                    //Log.Error("RadarClient : No spots???");
                }
            } catch (Exception e) {
                Log.Error(e, "RADAR");
            }
        }
    }
}
