using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Digi;
using VRageMath;
using Sandbox.Game.Entities;
using ParallelTasks;
using System;
using VRage.Game;
using Sandbox.Game.EntityComponents;
using Scripts.Specials.Radar;
using Scripts;

namespace ServerMod.Radar {

   
    class RadarServer : Action2<GameBase, long> {
        static internal readonly MyDefinitionId Electricity = MyResourceDistributorComponent.ElectricityId;

        AutoTimer sendtimer = new AutoTimer(1800);
        AutoTimer collectorTimer = new AutoTimer(60);

        public RadarServer () { }
        public void run(GameBase t, long k) {


            try { 
                if (collectorTimer.tick()) {
                    foreach (var ship in GameBase.instance.gridToShip.Values) {

                        if (!ship.active) continue;

                        var ri = ship.radarInfo;
                        var ph = ship.grid.Physics;
                        ri.times++;
                        if (ph != null) {

                            if (ph.Mass <= 0) {
                                ri.mass += RadarConsts.MASS_PENALTY;
                            } else {
                                ri.mass += ph.Mass;
                            }

                            ship.updateClosestPlanet(); //force recalculate
                            if(ship.closestPlanet != null) {
                                ri.height += RadarConsts.calculateHeightPoints(ship.closestPlanet, ship.getElevation());
                            } else {
                                ri.height += RadarConsts.HEIGHT_MAX_POINTS;
                            }

                            ri.speed += ph.LinearVelocity;
                        }
                    
                        if (ship.Cockpits.Count == 0) {
                            ri.electricity += RadarConsts.ENERGY_PENALTY;
                            //Log.Info("No ship.electricity");
                        } else {
                            var v = ship.Cockpits.FirstElement().GridResourceDistributor.TotalRequiredInputByType(Electricity);
                            //Log.Info("Electricity:" + v);
                            ri.electricity += v;
                        }
                    }
                }

                if (sendtimer.tick()) {
                    Process();
                }
            } catch (Exception e) {
                Log.Error (e);
            }

            
        }

        private static void Process() {
            try {
                if (GameBase.instance == null ) {
                    Log.Error("WTF? GameBase.instance");
                    return;
                }

                if (GameBase.instance.gridToShip == null) {
                    Log.Error("WTF? GameBase.instance.gridToShip");
                    return;
                }

                var i = 0;
                var grids = GameBase.instance.gridToShip;
                var list = new List<ShipInfo>(grids.Count);//MemorySaver.getShipInfoBuffer(grids.Count);
                for (var x=0; x<grids.Count; x++) {
                    list.Add (new ShipInfo());
                }

                

                var shipsStart = DateTime.Now;
               

                foreach (var x in grids.Values) {
                    try {
                        if (!x.active) continue;

                        var s = list[i++];
                        var g = x.grid;

                        var ri = x.radarInfo;
                        s.speed = ri.speed;
                        s.mass = ri.mass;
                        s.electricity = ri.electricity;
                        s.times = ri.times;

                        s.position = g.WorldAABB.Center;
                        s.size = g.GridSize;
                        s.name = g.DisplayName;
                        s.owners = g.BigOwners;
                        s.totalBlocks = (x.grid as MyCubeGrid).BlocksCount;

                        ri.Reset();
                    } catch (Exception e) {
                        Log.Error(e, "Radar Server Collect Ships");
                    }
                }

                var shipsEnd = DateTime.Now;


                try {
                    var playersStart = DateTime.Now;

                    List<IMyPlayer> players = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(players, x => !x.IsBot);
                    var list2 = new List<PlayerInfo>(players.Count);//MemorySaver.getShipInfoBuffer(grids.Count);
                    for (var x=0; x<players.Count; x++) {
                        list2.Add (new PlayerInfo());
                    }


                    var i2 = 0;

                    foreach (var player in players) {
                        try {
                            var p = list2[i2++];
                            p.position = player.GetPosition();
                            p.id = player.PlayerID;
                        } catch (Exception e) {
                            Log.Error(e, "Radar Server Collect Chars");
                        }
                    }

                    var playersEnd = DateTime.Now;

                    var ri = new RadarInfo();
                    ri.ships = list;
                    ri.player = list2;

                    var data = new GPSTaskData();
                    data.ri = ri;

                    //Log.Info("RADAR Time: ships/plyers=" + (shipsStart - shipsEnd).TotalMilliseconds +"/"+(playersStart - playersEnd).TotalMilliseconds + " " + ri.ships.Count + "/" + ri.player.Count);

                    MyAPIGateway.Parallel.Start(DoProcess, DoneProcess, data);

                } catch (Exception e) {
                    Log.Error(e, "Radar Server Collect Chars");
                }
            } catch (Exception e) {
                Log.Error(e, "Radar Server");
            }
        }

        public static void DoProcess(WorkData workData) {
            try {
                //Log.Info("RADAR: Time to serialize ");
                var d = workData as GPSTaskData;
                var ri = d.ri;
                var st = DateTime.Now;

                d.bytes = MyAPIGateway.Utilities.SerializeToBinary<RadarInfo>(ri);
                var stend = DateTime.Now;
                //Log.Info("RADAR: Time to serialize " + (stend - st));

                if (MyAPIGateway.Utilities.IsDedicated) {
                    MyAPIGateway.Parallel.Start(DoSend, DoneSend, d);
                } else {
                    RadarClient.handleMessage(d.bytes);
                }
            } catch (Exception e) {
                Log.Error(e, "RADAR: Async");
            }
        }

        public static void DoneProcess(WorkData workData) {
            //Log.Info("RADAR: DoneProcess In");
            var d = workData as GPSTaskData;
            RadarClient.handleMessage(d.bytes);
        }

        public static void DoSend(WorkData workData) {
            var d = workData as GPSTaskData;
            //Log.Info("RADAR: Send data");
            MyAPIGateway.Multiplayer.SendMessageToOthers(999, d.bytes);
            //Log.Info("RADAR: Data sended:"+ d.ri.ships);
        }

        public static void DoneSend(WorkData workData) {
            //Log.Info("RADAR: DoneProcess In");
            var d = workData as GPSTaskData;
            RadarClient.handleMessage(d.bytes);
        }
    }
}