using System;
using Digi;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using ServerMod;
using System.Text.RegularExpressions;
using System.Globalization;
using Sandbox.ModAPI.Ingame;
using IMyAirtightHangarDoor = Sandbox.ModAPI.IMyAirtightHangarDoor;
using IMyAirtightSlideDoor = Sandbox.ModAPI.IMyAirtightSlideDoor;
using IMyDoor = Sandbox.ModAPI.IMyDoor;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

namespace Scripts
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirtightSlideDoor), true)]
    public class AutoClosingDoors1 : AutoClosingDoorsBase { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Door), true)]
    public class AutoClosingDoors2 : AutoClosingDoorsBase { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirtightHangarDoor), true)]
    public class AutoClosingDoors3 : AutoClosingDoorsBase { }

    public class AutoClosingDoorsBase : MyGameLogicComponent
    {
        public IMyDoor door;
        private static bool INITIATED;
        bool autoClose;
        bool autoOpen;
        int needClose = 1;
        int needOpen;
        private int timeToClose = 12;
        private int timeToOpen = 20;
        private float distanceToOpen = 5;
        private float distanceToClose = 5;
        const float MIN_DISTANCE = 0.5f;
        const float MAX_DISTANCE = 20f;
        private const float MIN_TIME = 1f;
        private const float MAX_TIME = 100f;

        //const string PATTERN = @"\[OPEN:\d+:\d+\]*\|*|\[*CLOSE:\d+:\d+\]";
        const string PATTERN = @"\[OPEN:\d+:\d+.\d*\]|\[CLOSE:\d+:\d+.\d*\]";

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            
            try
            {
                base.Init(objectBuilder);
                door = (Entity as IMyDoor);

                if (!INITIATED)
                {
                    INITIATED = true;
                    InitControlsDoor();
                    InitControlsSlidingDoor();
                    InitControlsHangarDoor();
                }

                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    door.OnDoorStateChanged += Door_OnDoorStateChanged;
                }
                door.PropertiesChanged += Door_CustomNameChanged;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        public override void Close()
        {
            base.Close();
            door = (Entity as IMyDoor);
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                door.OnDoorStateChanged -= Door_OnDoorStateChanged;
            }

			door.PropertiesChanged -= Door_CustomNameChanged;
		}

        private static void InitControlsDoor()
        {
            var DoorAutoOpen = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyDoor>("Door_AutoOpen");
            DoorAutoOpen.Title = MyStringId.GetOrCompute("Auto-opening doors");
            DoorAutoOpen.Tooltip = MyStringId.GetOrCompute("Auto-opening doors");
            DoorAutoOpen.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().autoOpen;
            DoorAutoOpen.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.autoOpen = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoOpen.Enabled = (b) => true;
            DoorAutoOpen.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyDoor>(DoorAutoOpen);

            var DoorAutoClose = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyDoor>("Door_AutoClose");

            DoorAutoClose.Title = MyStringId.GetOrCompute("Auto-closing doors");
            DoorAutoClose.Tooltip = MyStringId.GetOrCompute("Auto-closing doors");
            DoorAutoClose.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().autoClose;
            DoorAutoClose.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.autoClose = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoClose.Enabled = (b) => true;
            DoorAutoClose.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;

            MyAPIGateway.TerminalControls.AddControl<IMyDoor>(DoorAutoClose);

            var DoorAutoCloseTime = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyDoor>("Door_AutoCloseTime");
            DoorAutoCloseTime.Title = MyStringId.GetOrCompute("Auto-closing time");
            DoorAutoCloseTime.Tooltip = MyStringId.GetOrCompute("Auto-closing time");
            DoorAutoCloseTime.Writer = (b, t) => t.Append(b.GetAs<AutoClosingDoorsBase>().timeToClose).Append("");
            DoorAutoCloseTime.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().timeToClose;
            DoorAutoCloseTime.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.timeToClose = (int)v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoCloseTime.SetLimits(MIN_TIME, MAX_TIME);
            DoorAutoCloseTime.Enabled = (b) => true;
            DoorAutoCloseTime.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyDoor>(DoorAutoCloseTime);

            var DoorAutoOpenTime = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyDoor>("Door_AutoOpenTime");
            DoorAutoOpenTime.Title = MyStringId.GetOrCompute("Auto-open time");
            DoorAutoOpenTime.Tooltip = MyStringId.GetOrCompute("Auto-open time");
            DoorAutoOpenTime.Writer = (b, t) => t.Append(b.GetAs<AutoClosingDoorsBase>().timeToOpen).Append("");
            DoorAutoOpenTime.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().timeToOpen;
            DoorAutoOpenTime.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.timeToOpen = (int)v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoOpenTime.SetLimits(MIN_TIME, MAX_TIME);
            DoorAutoOpenTime.Enabled = (b) => true;
            DoorAutoOpenTime.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyDoor>(DoorAutoOpenTime);

            #region Distance to open/close

            var DoorAutoCloseDistance = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyDoor>("Door_AutoCloseDistance");
            DoorAutoCloseDistance.Title = MyStringId.GetOrCompute($"Auto-closing distance {MIN_DISTANCE} - {MAX_DISTANCE}");
            DoorAutoCloseDistance.Tooltip = MyStringId.GetOrCompute("Auto-closing distance");
            DoorAutoCloseDistance.Writer = (b, t) => t.Append(Math.Round(b.GetAs<AutoClosingDoorsBase>().distanceToClose, 2)).Append("");
            DoorAutoCloseDistance.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().distanceToClose;
            DoorAutoCloseDistance.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.distanceToClose = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoCloseDistance.SetLimits(MIN_DISTANCE, MAX_DISTANCE);
            DoorAutoCloseDistance.Enabled = (b) => true;
            DoorAutoCloseDistance.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyDoor>(DoorAutoCloseDistance);

            var DoorAutoOpenDistance = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyDoor>("Door_AutoOpenDistance");
            DoorAutoOpenDistance.Title = MyStringId.GetOrCompute($"Auto-opening distance {MIN_DISTANCE} - {MAX_DISTANCE}");
            DoorAutoOpenDistance.Tooltip = MyStringId.GetOrCompute("Auto-opening distance");
            DoorAutoOpenDistance.Writer = (b, t) => t.Append(Math.Round(b.GetAs<AutoClosingDoorsBase>().distanceToOpen,  2)).Append("");
            DoorAutoOpenDistance.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().distanceToOpen;
            DoorAutoOpenDistance.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.distanceToOpen = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoOpenDistance.SetLimits(MIN_DISTANCE, MAX_DISTANCE);
            DoorAutoOpenDistance.Enabled = (b) => true;
            DoorAutoOpenDistance.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyDoor>(DoorAutoOpenDistance);

            #endregion

        }
        private static void InitControlsSlidingDoor()
        {
            var DoorAutoOpen = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyAirtightSlideDoor>("Door_AutoOpen");
            DoorAutoOpen.Title = MyStringId.GetOrCompute("Auto-opening doors");
            DoorAutoOpen.Tooltip = MyStringId.GetOrCompute("Auto-opening doors");
            DoorAutoOpen.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().autoOpen;
            DoorAutoOpen.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.autoOpen = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoOpen.Enabled = (b) => true;
            DoorAutoOpen.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightSlideDoor>(DoorAutoOpen);

            var DoorAutoClose = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyAirtightSlideDoor>("Door_AutoClose");
            DoorAutoClose.Title = MyStringId.GetOrCompute("Auto-closing doors");
            DoorAutoClose.Tooltip = MyStringId.GetOrCompute("Auto-closing doors");
            DoorAutoClose.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().autoClose;
            DoorAutoClose.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.autoClose = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoClose.Enabled = (b) => true;
            DoorAutoClose.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightSlideDoor>(DoorAutoClose);

            var DoorAutoCloseTime = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAirtightSlideDoor>("Door_AutoCloseTime");
            DoorAutoCloseTime.Title = MyStringId.GetOrCompute("Auto-closing time");
            DoorAutoCloseTime.Tooltip = MyStringId.GetOrCompute("Auto-closing time");
            DoorAutoCloseTime.Writer = (b, t) => t.Append(b.GetAs<AutoClosingDoorsBase>().timeToClose).Append("");
            DoorAutoCloseTime.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().timeToClose;
            DoorAutoCloseTime.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.timeToClose = (int)v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoCloseTime.SetLimits(MIN_TIME, MAX_TIME);
            DoorAutoCloseTime.Enabled = (b) => true;
            DoorAutoCloseTime.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightSlideDoor>(DoorAutoCloseTime);

            var DoorAutoOpenTime = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAirtightSlideDoor>("Door_AutoOpenTime");
            DoorAutoOpenTime.Title = MyStringId.GetOrCompute("Auto-open time");
            DoorAutoOpenTime.Tooltip = MyStringId.GetOrCompute("Auto-open time");
            DoorAutoOpenTime.Writer = (b, t) => t.Append(b.GetAs<AutoClosingDoorsBase>().timeToOpen).Append("");
            DoorAutoOpenTime.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().timeToOpen;
            DoorAutoOpenTime.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.timeToOpen = (int)v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoOpenTime.SetLimits(MIN_TIME, MAX_TIME);
            DoorAutoOpenTime.Enabled = (b) => true;
            DoorAutoOpenTime.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightSlideDoor>(DoorAutoOpenTime);

            #region Distance to open/close

            var DoorAutoCloseDistance = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAirtightSlideDoor>("Door_AutoCloseDistance");
            DoorAutoCloseDistance.Title = MyStringId.GetOrCompute($"Auto-closing distance {MIN_DISTANCE} - {MAX_DISTANCE}");
            DoorAutoCloseDistance.Tooltip = MyStringId.GetOrCompute("Auto-closing distance");
            DoorAutoCloseDistance.Writer = (b, t) => t.Append(Math.Round(b.GetAs<AutoClosingDoorsBase>().distanceToClose, 2)).Append("");
            DoorAutoCloseDistance.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().distanceToClose;
            DoorAutoCloseDistance.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.distanceToClose = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoCloseDistance.SetLimits(MIN_DISTANCE, MAX_DISTANCE);
            DoorAutoCloseDistance.Enabled = (b) => true;
            DoorAutoCloseDistance.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightSlideDoor>(DoorAutoCloseDistance);

            var DoorAutoOpenDistance = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAirtightSlideDoor>("Door_AutoOpenDistance");
            DoorAutoOpenDistance.Title = MyStringId.GetOrCompute($"Auto-opening distance {MIN_DISTANCE} - {MAX_DISTANCE}");
            DoorAutoOpenDistance.Tooltip = MyStringId.GetOrCompute("Auto-opening distance");
            DoorAutoOpenDistance.Writer = (b, t) => t.Append(Math.Round(b.GetAs<AutoClosingDoorsBase>().distanceToOpen,2)).Append("");
            DoorAutoOpenDistance.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().distanceToOpen;
            DoorAutoOpenDistance.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.distanceToOpen = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoOpenDistance.SetLimits(MIN_DISTANCE, MAX_DISTANCE);
            DoorAutoOpenDistance.Enabled = (b) => true;
            DoorAutoOpenDistance.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightSlideDoor>(DoorAutoOpenDistance);

            #endregion

        }
        private static void InitControlsHangarDoor()
        {
            var DoorAutoOpen = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyAirtightHangarDoor>("Door_AutoOpen");
            DoorAutoOpen.Title = MyStringId.GetOrCompute("Auto-opening doors");
            DoorAutoOpen.Tooltip = MyStringId.GetOrCompute("Auto-opening doors");
            DoorAutoOpen.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().autoOpen;
            DoorAutoOpen.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.autoOpen = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoOpen.Enabled = (b) => true;
            DoorAutoOpen.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightHangarDoor>(DoorAutoOpen);

            var DoorAutoClose = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyAirtightHangarDoor>("Door_AutoClose");
            DoorAutoClose.Title = MyStringId.GetOrCompute("Auto-closing doors");
            DoorAutoClose.Tooltip = MyStringId.GetOrCompute("Auto-closing doors");
            DoorAutoClose.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().autoClose;
            DoorAutoClose.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.autoClose = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoClose.Enabled = (b) => true;
            DoorAutoClose.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightHangarDoor>(DoorAutoClose);

            var DoorAutoCloseTime = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAirtightHangarDoor>("Door_AutoCloseTime");
            DoorAutoCloseTime.Title = MyStringId.GetOrCompute("Auto-closing time");
            DoorAutoCloseTime.Tooltip = MyStringId.GetOrCompute("Auto-closing time");
            DoorAutoCloseTime.Writer = (b, t) => t.Append(b.GetAs<AutoClosingDoorsBase>().timeToClose).Append("");
            DoorAutoCloseTime.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().timeToClose;
            DoorAutoCloseTime.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.timeToClose = (int)v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoCloseTime.SetLimits(MIN_TIME, MAX_TIME);
            DoorAutoCloseTime.Enabled = (b) => true;
            DoorAutoCloseTime.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightHangarDoor>(DoorAutoCloseTime);

            var DoorAutoOpenTime = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAirtightHangarDoor>("Door_AutoOpenTime");
            DoorAutoOpenTime.Title = MyStringId.GetOrCompute("Auto-open time");
            DoorAutoOpenTime.Tooltip = MyStringId.GetOrCompute("Auto-open time");
            DoorAutoOpenTime.Writer = (b, t) => t.Append(b.GetAs<AutoClosingDoorsBase>().timeToOpen).Append("");
            DoorAutoOpenTime.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().timeToOpen;
            DoorAutoOpenTime.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.timeToOpen = (int)v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoOpenTime.SetLimits(MIN_TIME, MAX_TIME);
            DoorAutoOpenTime.Enabled = (b) => true;
            DoorAutoOpenTime.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightHangarDoor>(DoorAutoOpenTime);

            #region Distance to open/close

            var DoorAutoCloseDistance = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAirtightHangarDoor>("Door_AutoCloseDistance");
            DoorAutoCloseDistance.Title = MyStringId.GetOrCompute($"Auto-closing distance {MIN_DISTANCE} - {MAX_DISTANCE}");
            DoorAutoCloseDistance.Tooltip = MyStringId.GetOrCompute("Auto-closing distance");
            DoorAutoCloseDistance.Writer = (b, t) => t.Append(Math.Round(b.GetAs<AutoClosingDoorsBase>().distanceToClose, 2)).Append("");
            DoorAutoCloseDistance.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().distanceToClose;
            DoorAutoCloseDistance.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.distanceToClose = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoCloseDistance.SetLimits(MIN_DISTANCE, MAX_DISTANCE);
            DoorAutoCloseDistance.Enabled = (b) => true;
            DoorAutoCloseDistance.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightHangarDoor>(DoorAutoCloseDistance);

            var DoorAutoOpenDistance = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAirtightHangarDoor>("Door_AutoOpenDistance");
            DoorAutoOpenDistance.Title = MyStringId.GetOrCompute($"Auto-opening distance {MIN_DISTANCE} - {MAX_DISTANCE}");
            DoorAutoOpenDistance.Tooltip = MyStringId.GetOrCompute("Auto-opening distance");
            DoorAutoOpenDistance.Writer = (b, t) => t.Append(Math.Round(b.GetAs<AutoClosingDoorsBase>().distanceToOpen,2)).Append("");
            DoorAutoOpenDistance.Getter = (b) => b.GetAs<AutoClosingDoorsBase>().distanceToOpen;
            DoorAutoOpenDistance.Setter = (b, v) =>
            {
                var autoDoor = b.GetAs<AutoClosingDoorsBase>();
                autoDoor.distanceToOpen = v;
                autoDoor.Door_PropertiesChanged(b);
            };
            DoorAutoOpenDistance.SetLimits(MIN_DISTANCE, MAX_DISTANCE);
            DoorAutoOpenDistance.Enabled = (b) => true;
            DoorAutoOpenDistance.Visible = (b) => b.GetAs<AutoClosingDoorsBase>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAirtightHangarDoor>(DoorAutoOpenDistance);

            #endregion

        }

        private void Door_PropertiesChanged(IMyTerminalBlock obj)
        {
            var s = door.CustomName;

            string newNameOpen = autoOpen ? $"[OPEN:{timeToOpen}:{distanceToOpen:N2}]" : "";
            string newNameClose = autoClose ? $"[CLOSE:{timeToClose}:{distanceToClose:N2}]" : "";
            if (Regex.IsMatch(s, @"\[OPEN:\d+:\d+.\d*\]"))
            {
                s = Regex.Replace(s, @"\[OPEN:\d+:\d+.\d*\]", newNameOpen);
            }
            else
            {
                s = s + $"{newNameOpen}";
            }
            if (Regex.IsMatch(s, @"\[CLOSE:\d+:\d+.\d*\]"))
            {
                s = Regex.Replace(s, @"\[CLOSE:\d+:\d+.\d*\]", newNameClose);
            }
            else
            {
                s = s + $"{newNameClose}";
            }
            door.CustomName = s;
        }

        private void Door_CustomNameChanged(IMyTerminalBlock obj)
        {
            if (door.CustomName == string.Empty)
            {
                return;
            }

            foreach (Match match in Regex.Matches(door.CustomName, PATTERN))
            {
                var s = match.Value.Replace("[", "").Replace("]", "").Replace("|", "").Split(new char[] {':'});
                
                if (s[0] == "OPEN")
                {
                    Int32.TryParse(s[1], out timeToOpen);
                    distanceToOpen = Convert.ToSingle(s[2]);
                    autoOpen = true;
                }
                if (s[0] == "CLOSE")
                {
                    Int32.TryParse(s[1], out timeToClose);
                    distanceToClose = Convert.ToSingle(s[2]);
                    autoClose = true;
                }
            }
        }

        private void Door_OnDoorStateChanged(IMyDoor arg1, bool arg2)
        {
            if (autoClose)
            {
                //Log.Error ("Door:" + arg1.IsFullyClosed + "/ " + arg2);
                if (arg2)
                {
                    needClose = timeToClose;     //12
                    //Log.Error ("Door:needClose" + needClose);
                }
            }
        }

        public void closeLogic()
        {
            if (needClose == 0)
            {
                if (isNear()) return;
                if (door.Status == Sandbox.ModAPI.Ingame.DoorStatus.Open)
                {
                    door.CloseDoor();
                    needClose = -1;
                }
            }
            else if (needClose > 0)
            {
                needClose--;
            }
        }

        public bool haveAccess()
        {
            return door.HasPlayerAccess(MyAPIGateway.Session.Player.IdentityId);
        }

        public bool inWhitelist()
        {
            var pl = MyAPIGateway.Session.Player;
            var steamIds = door.CustomData.Split(',');
            foreach (var id in steamIds)
            {
                if (id == "" + pl.SteamUserId) return true;
            }
            return false;
        }

        public bool isNear()
        {
            var ch = MyAPIGateway.Session.Player?.Character;
            if (ch == null) return false;
            switch (door.Status)
            {
                case DoorStatus.Closed:
                    return (ch.GetPosition() - door.GetPosition()).Length() < distanceToOpen;
                case DoorStatus.Open:
                    return (ch.GetPosition() - door.GetPosition()).Length() < distanceToClose;
                default:
                    return false;
            }
        }

        public void openLogic()
        {
            if (!door.IsWorking) return;
            if (needOpen <= 0)
            {
                if (!isNear()) return;
                if (MyAPIGateway.Session.Player?.Character == null) return;
                if (!haveAccess() && !(door as MyDoorBase).AnyoneCanUse && !inWhitelist())
                {
                    needOpen = 10;
                    return;
                }
                if (!isNear())
                {
                    needOpen = 0;//
                }
                else
                {
                    needOpen = timeToOpen;     //20;
                    door.OpenDoor();
                }
            }
            else
            {
                needOpen--;
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                if (autoClose)
                {
                    closeLogic();
                }
            }
            if (!MyAPIGateway.Session.isTorchServer())
            {
                if (autoOpen && MyAPIGateway.Session.Player != null)
                {
                    openLogic();
                }
            }
            
        }
    }

}
