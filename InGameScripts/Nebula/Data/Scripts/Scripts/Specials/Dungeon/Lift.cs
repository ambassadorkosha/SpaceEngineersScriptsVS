using Digi;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
//using Digi;

namespace Scripts {

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_PistonBase), true, new string[] { "LiftPiston"})]
    public class LiftPiston : MyGameLogicComponent {
        private static Regex regex = new Regex("\\[FLOORS:([\\d\\.,]+)\\]");
        private static bool inited = false;

        public IMyPistonBase door;
        private bool patched = false;
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            door = (Entity as IMyPistonBase);

            if (!inited) {
                inited = true;
                InitActions();
            }

			if (MyAPIGateway.Session.isTorchServer()) return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
           
        }

        private void InitActions () {
            var upFloor = MyAPIGateway.TerminalControls.CreateAction<IMyPistonBase>("UpperFloor");
            upFloor.Name = new StringBuilder("Upper Floor");
            upFloor.Action = (b) => ChangeFloor (b, 1);
            upFloor.Writer = (b, t) => { };
            upFloor.Enabled = (b) => { return b.BlockDefinition.SubtypeName.Contains ("LiftPiston"); };

            MyAPIGateway.TerminalControls.AddAction<IMyPistonBase> (upFloor);

            var downFloor = MyAPIGateway.TerminalControls.CreateAction<IMyPistonBase>("DownFloor");
            downFloor.Name = new StringBuilder("Down Floor");
            downFloor.Action = (b) => ChangeFloor (b, -1);
            downFloor.Writer = (b, t) => { };
            downFloor.Enabled = (b) => { return b.BlockDefinition.SubtypeName.Contains ("LiftPiston"); };


            MyAPIGateway.TerminalControls.AddAction<IMyPistonBase> (downFloor);

            for (var x = 1; x<=10; x++) {
                AddFloorAction (x);
            }
        }

        void AddFloorAction (int floor) {
            var ff = MyAPIGateway.TerminalControls.CreateAction<IMyPistonBase>("Floor_" + floor);
            ff.Name = new StringBuilder("To " + (floor) +" floor");
            ff.Action = (b) => NavigateToFloor (b, floor);
            ff.Writer = (b, t) => { };
            ff.Enabled = (b) => { return b.BlockDefinition.SubtypeName.Contains ("LiftPiston"); };
            MyAPIGateway.TerminalControls.AddAction<IMyPistonBase> (ff);
        }

        

        public static List<float> ParseFloors (String s) {
           
            var match = regex.Match (s);
            var list = new List<float>();
            if (!match.Success) {   
                Log.ChatError ("Имя заданно неверно\n" +
                               " Правильный формат: PistonName [FLOORS:X,Y,Z]\n" +
                               "где X,Y,Z и тд это задается высота для каждого этажа\n"); 
                list.Add (0); 
                return list;}
            
            var data = match.Groups[1].Value;
            var floors = data.Split (',');

           

            foreach (var x in floors) {
                float floor;
                if (float.TryParse (x, out floor)) {
                    list.Add (floor);
                }
            }

            list.Sort ();
            return list;
        }

        public void NavigateToFloor (IMyTerminalBlock block, int floor) {
            try
            {
                floor = floor - 1;
                var lift = (block as IMyPistonBase);
                if (lift == null) return;
                if (lift.Velocity == 0)
                {
                    Log.Error("Скорость пистона должна быть больше 0 м/с");
                    return;
                }
                
                var floors = ParseFloors(lift.CustomName);
                
                if (!floors.IsValidIndex(floor) || floors.Count <= 1 )
                {
                    Log.Error ("Этаж: [" + floor +"] не указан! Всего этажей: " + (floors.Count ) ); 
                    return;
                }
                
                var cfloor = CurrentFloor(lift, floors);

                Log.ChatError((floor+1) +" <-- "+ (cfloor + 1) + " / " + (floors.Count));

                ChangeFloor(block, floor - cfloor);
            }
            catch (Exception e)
            {
                Log.Error (e,"[Lift Piston] " ); 
            }
          
        }

        private int CurrentFloor (IMyPistonBase lift, List<float> floors) {
            int currentFloor = -1;
            for (var x=0; x< floors.Count; x++) {
                if (floors[x] <= lift.CurrentPosition) {
                    currentFloor ++;
                }
            }
            return currentFloor;
        }

        public void ChangeFloor (IMyTerminalBlock block, int amount) {
             try { 

                if (amount == 0) return;
                var lift = (block as IMyPistonBase);
                if (lift == null) return;
                if (lift.Velocity == 0) return;
              

                var floors = ParseFloors (lift.CustomName);
                if (floors == null) return;

                int currentFloor = CurrentFloor(lift, floors);
            
                if (amount > 0) {
                     if (lift.Velocity < 0) {
                        lift.Velocity *= -1;
                     }
                     if (currentFloor+amount > floors.Count-1) return;
                     lift.MaxLimit = floors [currentFloor + amount];
                     lift.MinLimit = 0;
                } else {
                     if (lift.Velocity > 0) {
                        lift.Velocity *= -1;
                     }
                     if (currentFloor <= 0) return;

                     lift.MinLimit = floors [Math.Max(currentFloor+amount, 0)];
                     lift.MaxLimit = 999999;
                }
                
            } catch (Exception e) {
                Log.ChatError ("ChangeFloor", e);
            }
        }

        

        public override void UpdateBeforeSimulation() {
            base.UpdateBeforeSimulation();
            try {
                if (!patched) {
                    MyEntitySubpart sub1;
                    MyEntitySubpart sub2;
                    MyEntitySubpart sub3;
                    if (door.TryGetSubpart ("PistonSubpart1", out sub1)) {
                        if (sub1.TryGetSubpart ("PistonSubpart2", out sub2)) {
                            if (sub2.TryGetSubpart ("PistonSubpart3", out sub3)) {
                                sub1.Render.Visible = true;
                                sub2.Render.Visible = true;
                                sub3.Render.Visible = true;
                                patched = true;
                                //NeedsUpdate = MyEntityUpdateEnum.NONE;
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Log.Error (e);
            }
        }
    }
}
