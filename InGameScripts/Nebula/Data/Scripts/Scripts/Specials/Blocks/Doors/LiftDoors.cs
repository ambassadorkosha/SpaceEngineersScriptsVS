using Sandbox.ModAPI;
using Scripts.Specials.Doors;
using ServerMod;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

//using Digi;

namespace Scripts {
	
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Door), true, new string[] { "LiftDoor1" })]
    public class LiftDoors1: LiftDoorsBase {  }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirtightSlideDoor), true, new string[] { "LiftDoor2"})]
    public class LiftDoors2: LiftDoorsBase {  }

    public abstract class LiftDoorsBase : MyGameLogicComponent {
        public IMyDoor door;
        public int openDelay = 0;
        public int MAX_OPEN_DELAY = 6;

        static HashSet<LiftDoorsBase> AllLiftDoors = new HashSet<LiftDoorsBase>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            door = (Entity as IMyDoor);
            AllLiftDoors.Add(this);
			if (!MyAPIGateway.Session.isTorchServer()) {
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
				//door.OnDoorStateChanged += Door_OnDoorStateChanged;	
            }
            door.OnMarkForClose += Door_OnMarkForClose;
        }

        private void Door_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
            AllLiftDoors.Remove (this);
        }

        public static bool CanOpenDoor (IMyDoor door) {
            foreach (var x in AllLiftDoors) {
                if (door == x.door) continue;
                if ((door.WorldMatrix.Translation - x.door.WorldMatrix.Translation).Length() < 2.6f) {
                    return true;
                }
            }

            return false;
        }

        
        public override void UpdateBeforeSimulation10() {
            base.UpdateBeforeSimulation10();

            if (MyAPIGateway.Session.isTorchServer()) { return; }

            var targetState = CanOpenDoor(door);

            //Log.Error (targetState + " / " + door.IsFullyClosed);
            if (door.IsFullyClosed == targetState && (door.OpenRatio == 0 || door.OpenRatio == 1)) { 
                if (!targetState) {
                     DoorsAPI.SendRequest(door, false);
                } else {
                    openDelay++;
                    if (openDelay >= MAX_OPEN_DELAY) {
                        DoorsAPI.SendRequest(door, false);
                        openDelay = 0;
                    }
                }
            } else {
                openDelay = 0;
            }
        }
    }

}
