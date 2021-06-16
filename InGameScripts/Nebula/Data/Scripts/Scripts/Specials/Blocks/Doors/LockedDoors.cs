using Sandbox.ModAPI;
using Scripts.Specials.Doors;
using ServerMod;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;

//using Digi;

namespace Scripts {

    public static class KeyCardDoors {
        public static MyItemType RRED_CARD = MyDefinitionId.Parse ("MyObjectBuilder_Component/CardRed");
        public static MyItemType GGREEN_CARD = MyDefinitionId.Parse ("MyObjectBuilder_Component/CardGreen");
        public static MyItemType BBLUE_CARD = MyDefinitionId.Parse ("MyObjectBuilder_Component/CardBlue");
        public static Dictionary<string, MyItemType> IDS = new Dictionary<string, MyItemType>() { { "[CARD:RED]", MyDefinitionId.Parse ("MyObjectBuilder_Component/CardRed")}, { "[CARD:GREEN]", MyDefinitionId.Parse ("MyObjectBuilder_Component/CardGreen")},  { "[CARD:BLUE]",  MyDefinitionId.Parse ("MyObjectBuilder_Component/CardBlue")} };
    }
	
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Door), true, new string[] { "LockedDoor1" })]
    public class LockedDoors1: LockedDoorsBase {  }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirtightSlideDoor), true, new string[] { "LockedDoor2"})]
    public class LockedDoors2: LockedDoorsBase {  }

    public abstract class LockedDoorsBase : MyGameLogicComponent {
        public IMyDoor door;
        bool autoclose = true;
        bool autoopen = true;

        int needClose = 1;
        int needOpen = 1;

       

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            door = (Entity as IMyDoor);
			if (MyAPIGateway.Multiplayer.IsServer) {
				door.OnDoorStateChanged += Door_OnDoorStateChanged;	
            }
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }


        public static bool CanOpenDoor (IMyDoor door, IMyPlayer player, float minDistance) {
            var ch = player.Character;
            if (ch == null) {
                return false;
            }
            var d = ch.WorldMatrix.Translation - door.WorldMatrix.Translation;
            if (d.Length() > minDistance) {
                return false;
            }

            var inv = ch.GetInventory().CountItems();
            var cn = door.CustomName;

            var canOpen = true;
            foreach (var x in KeyCardDoors.IDS) {
                if (cn.Contains(x.Key) && !inv.ContainsKey(x.Value)) {
                    canOpen = false;
                }
            }

            return canOpen;
        }

        

        private void Door_OnDoorStateChanged(IMyDoor arg1, bool arg2) {
            if (autoclose) {
                if (arg2) {
                    needClose = 24;
                }
            }
        }

        public void closeLogic () {
            if (needClose == 0) {
                door.CloseDoor();
                needClose = -1;
            } else if (needClose > 0){ 
                needClose--;
            }
        }

        public void openLogic () {
            if (!door.IsWorking) return;
            if (door.Status == Sandbox.ModAPI.Ingame.DoorStatus.Open || door.Status == Sandbox.ModAPI.Ingame.DoorStatus.Opening) return;
            if (needOpen <= 0) {

                var pl = MyAPIGateway.Session.Player;
                if (pl == null) return;

                needOpen = 6;
                if (CanOpenDoor (door, pl, 4f)) {
                    DoorsAPI.SendRequest (door, true);
                    needOpen = 20;
                }
            } else {
                needOpen--;
            }
        }

        public override void UpdateBeforeSimulation10() {
            base.UpdateBeforeSimulation10();
            if (MyAPIGateway.Multiplayer.IsServer) {
                if (autoclose) { 
                   closeLogic();
                }
            }

            if (autoopen && MyAPIGateway.Session.Player != null) {
                openLogic();
            }
        }
    }

}
