using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Digi;
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Scripts.Specials.Messaging;
using Slime;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using Scripts.Shared;
using System.Text;
using ProtoBuf;
using ServerMod;
using VRageMath;
using Sandbox.Game;
using Scripts.Specials.ExtraInfo;
using Sandbox.Game.Screens.Helpers;

namespace Scripts.Specials.Safezones
{
    public enum MySafeZoneAction
    {
        Damage = 1,
        Shooting = 2,
        Drilling = 4,
        Welding = 8,
        Grinding = 16,
        VoxelHand = 32,
        Building = 64,
        LandingGearLock = 128,
        ConvertToStation = 256,
        AdminIgnore = 382,
        All = 511
    }

    public enum CapturableSafeZoneState
    {
        WRONG_STATE,
        WAITING_START,
        IN_PROGRESS,
        CAPTURED
    }

    [ProtoContract]
    public class CapturableSafeZoneParams
    {
        [ProtoMember(1)] public long FirstCapture = -1; //seconds from 1970
        [ProtoMember(2)] public long LastCapturedIteration = -1;
        [ProtoMember(3)] public long EventDuration = 60 * 2;
        [ProtoMember(4)] public long TimeToCapture = 60; //from 0 to 100%
        [ProtoMember(5)] public long EventInterval = 60 * 3;
        [ProtoMember(6)] public long LastTickIteration = -1;
        [ProtoMember(7)] public CapturableSafeZoneState LastTickState = CapturableSafeZoneState.WRONG_STATE;
        [ProtoMember(8)] public float CaptureProgress;
        [ProtoMember(9)] public long CapturingFaction; //Currently
        [ProtoMember(10)] public long PrevCapturedByFaction;
        [ProtoMember(11)] public long Radius = 50;
        [ProtoMember(12)] public long i;
        [ProtoMember(13)] public string Name = "NoName";
        [ProtoMember(14)] public string Status = "";
        [ProtoMember(15)] public Vector3 Position;
        [ProtoMember(16)] public long EntityId;

        public CapturableSafeZoneState GetState()
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
            {
                Common.SendChatMessage("Error: calling GetState on client", "SaveZone", font: "Red");
                return CapturableSafeZoneState.WRONG_STATE;
            }

            var ServerFirstCapture = FirstCapture - SharpUtils.timeUtcDif();
            if (ServerFirstCapture < 0 || EventDuration > EventInterval) return CapturableSafeZoneState.WRONG_STATE;

            var now = SharpUtils.timeStamp();
            if (now < ServerFirstCapture) return CapturableSafeZoneState.WAITING_START;

            i = (now - ServerFirstCapture) % EventInterval;

            if (LastCapturedIteration == CurrentIteration() || i > EventDuration) return CapturableSafeZoneState.CAPTURED;

            return CapturableSafeZoneState.IN_PROGRESS;
        }

        public long CurrentIteration()
        {
            return Math.Max(0, (SharpUtils.timeStamp() + SharpUtils.timeUtcDif() - FirstCapture) / EventInterval);
        }

        public DateTime GeIterationStartAt(long iter)
        {
            var t = FirstCapture + (iter) * EventInterval;
            return SharpUtils.utcZero.AddSeconds(t).AddSeconds(-SharpUtils.timeUtcDif());
        }

        public override string ToString()
        {
            return $"FC {FirstCapture} | LC {LastCapturedIteration} | ED {EventDuration} TTC {TimeToCapture} Int {EventInterval} Prev = {PrevCapturedByFaction.Faction()?.Tag ?? $"Unknown [{PrevCapturedByFaction}]"} Iter = {LastTickIteration}/{LastTickState}";
        }

        public float CaptureProgressBy100Ticks()
        {
            return 1f / (TimeToCapture) * (100f / 60f);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RadioAntenna), true, "EventSafeSphereLarge")]
    public class CapturableSafeZone : MyGameLogicComponent
    {
        private static Sync<CapturableSafeZoneParams, CapturableSafeZone> sync;
        public static void Initialize()
        {
            sync = new Sync<CapturableSafeZoneParams, CapturableSafeZone>(55520, sz => sz.settings, (sz, NewSettings, PlayerSteamId, isFromServer) =>
            {
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    if (!IsAdmin(PlayerSteamId)) return;
                }
                sz.settings = NewSettings;
                if (isFromServer)
                {
                    ReactOnStatusUpdate(NewSettings);
                }
                //Log.ChatError ("Arrived: " + NewSettings.ToString());
            }, (NewSettings, PlayerSteamId, isFromServer) =>
            {
                if (isFromServer)
                {
                    ReactOnStatusUpdate(NewSettings);
                }
            });
        }

        private const int SZ_ALLOWED = (int)(MySafeZoneAction.All - MySafeZoneAction.Damage - MySafeZoneAction.Shooting);

        private static readonly Guid guid = new Guid("e06a78fa-14a9-4880-a417-85df54628e92");
        private static bool INITED;

        private CapturableSafeZoneParams settings;

        private readonly List<IMyPlayer> players = new List<IMyPlayer>();
        private readonly HashSet<IMyFaction> charFactions = new HashSet<IMyFaction>();
        private readonly HashSet<IMyFaction> shipFactions = new HashSet<IMyFaction>();
        private readonly HashSet<IMyFaction> factions = new HashSet<IMyFaction>();

        private IMyRadioAntenna antenna;
        private MySafeZone SafeZone;
        private bool inited = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            //if (!MyAPIGateway.Multiplayer.IsServer) sync.RequestData(Entity.EntityId);
            InitControls();
            antenna = (IMyRadioAntenna)Entity;
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            FindSafeZone();
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            Init();
        }

        private void Init()
        {
            if (inited) return;

            try
            {
                base.OnAddedToContainer();
                //Log.ChatError ("Init");
                if (MyAPIGateway.Session.IsServer)
                {
                    if (Entity.TryGetStorageData(guid, out settings))
                    {

                        var state = settings.GetState();
                        if (state == CapturableSafeZoneState.WRONG_STATE)
                        {
                            //Log.ChatError("Wrong state: ");
                            settings = new CapturableSafeZoneParams();
                            settings.EntityId = Entity.EntityId;
                            return;
                        }
                        if (settings.LastTickIteration != settings.CurrentIteration()) //We could have last saved state at half of capture progress
                        {
                            settings.CaptureProgress = 0;
                            settings.CapturingFaction = 0;
                        }


                        settings.EntityId = Entity.EntityId;

                        //Log.ChatError("Loaded: " + settings);
                    }
                    else
                    {
                        //Log.ChatError("Error on parsing!" + Entity.GetStorageData (guid));
                        settings = settings ?? new CapturableSafeZoneParams();
                        settings.EntityId = Entity.EntityId;
                    }
                }
                else
                {
                    //Log.ChatError("RequestData");
                    settings = settings ?? new CapturableSafeZoneParams();
                    settings.EntityId = Entity.EntityId;
                    sync.RequestData(Entity.EntityId);
                }
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        private static bool IsAdmin(ulong PlayerSteamId)
        {
            var PlayersList = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(PlayersList);
            foreach (var Player in PlayersList.Where(Player => Player.SteamUserId == PlayerSteamId))
            {
                if (Player.PromoteLevel >= MyPromoteLevel.Admin) return true;
            }
            return false;
        }

        private void GetFactionsContesting()
        {

            players.Clear();

            charFactions.Clear();
            shipFactions.Clear();
            factions.Clear();

            MyAPIGateway.Players.GetPlayers(players, (p) =>
            {
                var f = p.getFaction();
                if (f == null) return false;
                var ent = p?.Controller?.ControlledEntity;
                if (ent == null) return false;
                var inRange = (p.GetPosition() - antenna.PositionComp.WorldMatrixRef.Translation).LengthSquared() < settings.Radius * settings.Radius;
                if (!inRange) return false;

                if (ent is IMyCharacter) charFactions.Add(f);
                else shipFactions.Add(f);

                factions.Add(f);

                return true;
            });
        }



        public override void UpdateAfterSimulation100()
        {
            //only on server
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            try
            {
                //Log.ChatError ("Update 1");
                var state = settings.GetState();
                var iter = settings.CurrentIteration();
                if (settings.LastTickIteration != iter || state != settings.LastTickState)
                {
                    settings.CaptureProgress = 0;
                    settings.CapturingFaction = 0;

                    SyncStateToSafeZone();
                }

                settings.LastTickIteration = iter;
                settings.LastTickState = state;

                settings.EntityId = Entity.EntityId;
                settings.Position = Entity.WorldMatrix.Translation;
                //Log.ChatError("Update 2");
                if (state == CapturableSafeZoneState.WRONG_STATE)
                {
                    antenna.HudText = $"Wrong data: {settings}";
                    return;
                }
                else
                {
                    antenna.HudText = "";
                }


                if (SafeZone == null)
                {
                    SyncStateToSafeZone();
                }
                else
                {
                    if (SafeZone.Closed || SafeZone.MarkedForClose)
                    {
                        SafeZone = null;
                    }
                }
                //Log.ChatError("Update 3");

                if (state == CapturableSafeZoneState.CAPTURED) iter++;

                if (state == CapturableSafeZoneState.WAITING_START || state == CapturableSafeZoneState.CAPTURED)
                {
                    if (SafeZone != null && !SafeZone.Enabled)
                    {
                        SafeZone.Enabled = true;
                        MySessionComponentSafeZones.UpdateSafeZone((MyObjectBuilder_SafeZone)SafeZone.GetObjectBuilder(), true);
                    }

                    var TimeLeft = (settings.GeIterationStartAt(iter) - DateTime.UtcNow).StripMilliseconds();
                    var f = settings.PrevCapturedByFaction.Faction()?.Tag ?? "Nobody";

                    //if (TimeLeft.Days < 1)
                    //{
                    UpdateStatus($"#{iter + 1} Event in {TimeLeft:dd\\:hh\\:mm\\:ss} Duration={settings.EventDuration} | Owner = {f}");
                    //} else
                    //{
                    //    UpdateStatus(null);
                    //}
                    ReactOnStatusUpdate(settings);
                    sync.SendMessageToOthers(Entity.EntityId, settings);
                    SaveState();
                    return;
                }

                if (state == CapturableSafeZoneState.IN_PROGRESS)
                {
                    if (SafeZone != null && SafeZone.Enabled)
                    {
                        SafeZone.Enabled = false;
                        MySessionComponentSafeZones.UpdateSafeZone((MyObjectBuilder_SafeZone)SafeZone.GetObjectBuilder(), true);
                    }

                    GetFactionsContesting();
                    var EventTimeLeft = (settings.GeIterationStartAt(iter).AddSeconds(settings.EventDuration) - DateTime.UtcNow).StripMilliseconds();



                    var capturingFaction = shipFactions.Count == 1 ? shipFactions.First() : (shipFactions.Count == 0 && charFactions.Count == 1) ? charFactions.First() : null;

                    if (capturingFaction != null)
                    {
                        //Log.ChatError("Update 11");
                        CaptureProcess(iter, capturingFaction, EventTimeLeft);
                        //Log.ChatError("Update 12");
                    }
                    else if (shipFactions.Count == 0 && charFactions.Count == 0)
                    {
                        var f = settings.CapturingFaction.Faction()?.Tag ?? "Nobody";
                        UpdateStatus($"Idle {(settings.CaptureProgress * 100f).fixZero()}% by {f} | Time left: {EventTimeLeft:hh\\:mm\\:ss}");
                    }
                    else
                    {
                        UpdateStatus($"Contesting by {factions.Count} factions | Time left: {EventTimeLeft:hh\\:mm\\:ss}");
                    }
                }

                if (!MyAPIGateway.Session.isTorchServer())
                {
                    ReactOnStatusUpdate(settings);
                }

                sync.SendMessageToOthers(Entity.EntityId, settings);
                SaveState();
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        private static List<IMyPlayer> playersGPSCache = new List<IMyPlayer>();

        private void UpdateStatus(string what, int ttl = 0)
        {
            settings.Status = what;
            //antenna.HudText = what;
            playersGPSCache.Clear();
            MyAPIGateway.Players.GetPlayers(playersGPSCache);
            var blockId = "" + antenna.EntityId;
            foreach (var x in playersGPSCache)
            {
                //var gps = Gps.RemoveWithDescription(blockId, x.IdentityId, false);
                //TorchExtensions.AddGPS(x.IdentityId, what, blockId, Entity.WorldMatrix.Translation, Color.Aqua, new TimeSpan(10), true, gps?.ShowOnHud ?? true, false, false);
                var gps = Gps.GetWithDescription(blockId, x.IdentityId);
                if (gps != null)
                {
                    gps.Name = what;
                    gps.Coords = Entity.WorldMatrix.Translation;
                    MyAPIGateway.Session.GPS.ModifyGps(x.IdentityId, gps);
                }
                else
                {
                    var newgps = MyAPIGateway.Session.GPS.Create(what, blockId, Entity.WorldMatrix.Translation, true);
                    //gps.
                    MyAPIGateway.Session.GPS.AddGps(x.IdentityId, newgps);
                    //TorchExtensions.AddGPS(x.IdentityId, what, blockId, Entity.WorldMatrix.Translation, Color.Aqua, new TimeSpan(10), true, true, false, false);
                }
            }
        }

        private static void ReactOnStatusUpdate(CapturableSafeZoneParams settings)
        {
            try
            {
                if (true) return;
                if (settings.Status == null) return;
                if (MyAPIGateway.Session.isTorchServer()) return;
                if (settings.EntityId == 0) return;

                var myId = MyAPIGateway.Session.Player.IdentityId;
                var list = MyAPIGateway.Session.GPS.GetGpsList(myId);

                ShowInfo.Extra2 = settings.Status ?? "";

                IMyGps gps = null;

                var zoneId = "" + settings.EntityId;


                Log.ChatError("Amount:" + list.Count);
                foreach (var x in list)
                {
                    if (x.Description.Contains(zoneId))
                    {
                        Log.ChatError("Found GPS");
                        gps = x;
                        break;
                    }
                }

                if ((settings.Status == null || settings.Status.Length == 0) && gps != null)
                {
                    Log.ChatError("Wtf? Settings.Status = null Removing GPS");
                    MyAPIGateway.Session.GPS.RemoveGps(myId, gps);
                }
                else
                {
                    if (gps == null)
                    {
                        MyVisualScriptLogicProvider.AddGPSObjective(settings.Status, zoneId, settings.Position, Color.Cyan, 10, myId);
                        Log.ChatError("Adding GPS");
                    }
                    else
                    {
                        gps.Name = settings.Status;
                        gps.Coords = settings.Position;
                        gps.DiscardAt = TimeSpan.FromSeconds(10);
                        MyAPIGateway.Session.GPS.ModifyGps(myId, gps);
                        Log.ChatError("Modify GPS");
                    }
                }
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        public void CaptureProcess(long iter, IMyFaction ff, TimeSpan EventTimeLeft)
        {
            if (settings.CapturingFaction == ff.FactionId)
            {
                settings.CaptureProgress += settings.CaptureProgressBy100Ticks();

                if (settings.CaptureProgress > 1f)
                {
                    settings.CaptureProgress = 1f;
                }

                if (settings.CapturingFaction != settings.PrevCapturedByFaction)
                {
                    if (settings.CaptureProgress < 1f)
                    {
                        UpdateStatus($"Capturing {(settings.CaptureProgress * 100f).fixZero()}% by {ff.Tag} | Time left: {EventTimeLeft:hh\\:mm\\:ss}");
                    }
                    else
                    {
                        Capture(ff, iter);
                    }
                }
                else
                {
                    if (settings.i < settings.EventDuration)
                    {
                        UpdateStatus($"Holding {(settings.CaptureProgress * 100f).fixZero()}% by {ff.Tag} | Time left: {EventTimeLeft:hh\\:mm\\:ss}");
                    }
                    else
                    {
                        Capture(ff, iter);
                    }
                }
            }
            else
            {
                settings.CaptureProgress -= settings.CaptureProgressBy100Ticks();
                if (settings.CaptureProgress < 0)
                {
                    settings.CapturingFaction = ff.FactionId;
                    settings.CaptureProgress *= -1;
                }

                var FirstWord = "Capturing";
                if (settings.CapturingFaction != ff.FactionId) FirstWord = "Recapturing";
                UpdateStatus($"{FirstWord} {(settings.CaptureProgress * 100f).fixZero()}% by {ff.Tag} | Time left: {EventTimeLeft:hh\\:mm\\:ss}");
            }
        }

        private void SaveState()
        {
            settings.LastTickIteration = settings.CurrentIteration();
            settings.LastTickState = settings.GetState();
            Entity.SetStorageData(guid, settings);
        }

        private void Capture(IMyFaction faction, long Iteration)
        {


            if (settings.PrevCapturedByFaction == faction.FactionId)
            {
                var f = settings.PrevCapturedByFaction.Faction()?.Tag ?? "Nobody";
                Common.SendChatMessage($"Event Ended with keeping last owner: {f}", settings.Name, font: "Red");
            }
            else
            {
                settings.PrevCapturedByFaction = faction.FactionId;
                var f = settings.PrevCapturedByFaction.Faction()?.Tag ?? "Nobody";
                Common.SendChatMessage($"Event Ended with capturing SafeZone by: {f}", settings.Name, font: "Red");
            }

            settings.CaptureProgress = 0;
            settings.CapturingFaction = 0;

            settings.LastCapturedIteration = Iteration;
            SyncStateToSafeZone();
            SaveState();
            //DO SAFEZONE SHIT
        }

        private void SendState()
        {
            if (MyAPIGateway.Multiplayer.IsServer) SaveState();
            else sync.SendMessageToServer(Entity.EntityId, settings);
        }

        private static bool IsEnabled(CapturableSafeZone b)
        {
            return true;
        }

        private static bool IsVisible(CapturableSafeZone b)
        {
            return MyAPIGateway.Session.Player.PromoteLevel >= MyPromoteLevel.Admin;
        }

        private void SyncStateToSafeZone()
        {
            var state = settings.GetState();
            if (state == CapturableSafeZoneState.WRONG_STATE)
            {
                if (SafeZone == null) FindSafeZone();
                if (SafeZone != null) MySessionComponentSafeZones.RemoveSafeZone(SafeZone);
                return;
            }

            if (SafeZone == null)
            {
                FindSafeZone();
                if (SafeZone == null)
                {
                    MySessionComponentSafeZones.CrateSafeZone(antenna.WorldMatrix, MySafeZoneShape.Sphere, MySafeZoneAccess.Whitelist, new long[0], new long[0], settings.Radius, true);
                    FindSafeZone();
                }
            }

            var f = new List<long>();
            if (settings.PrevCapturedByFaction != 0)
            {
                f.Add(settings.PrevCapturedByFaction);
            }

            TorchConnection.SetSafezoneOptions(SafeZone, settings.Radius, false, true, false, f, new List<long>(), new List<long>(), SZ_ALLOWED);
        }

        private void FindSafeZone()
        {
            foreach (var x in MySessionComponentSafeZones.SafeZones)
            {
                var d = x.WorldMatrix.Translation - antenna.WorldMatrix.Translation;
                if (d.LengthSquared() < x.Radius * x.Radius)
                {
                    SafeZone = x;
                    return;
                }
            }
        }

        private static void InitControls()
        {
            if (INITED) return;
            INITED = true;

            MyAPIGateway.TerminalControls.CreateTextbox<CapturableSafeZone, IMyRadioAntenna>("EventName",
                "Event Name", "Name of the Event",
                (x) => new StringBuilder(x.settings.Name),
                (x, y) =>
                {
                    x.settings.Name = y.ToString();
                    x.SendState();
                }, IsEnabled, IsVisible);

            MyAPIGateway.TerminalControls.CreateTextbox<CapturableSafeZone, IMyRadioAntenna>("First Start",
               "First Start", "Time in seconds, from 2020 year",
               (x) => new StringBuilder(SharpUtils.utcZero.AddSeconds(x.settings.FirstCapture).ToString("yyyy.MM.dd HH:mm:ss")),
               (x, y) =>
               {
                   DateTime result;
                   if (DateTime.TryParse(y.ToString(), out result))
                   {
                       if (result < SharpUtils.utcZero) return;
                       //var offset = -10800l;//TimeZone.CurrentTimeZone.GetUtcOffset(result);
                       x.settings.FirstCapture = (long)(result - SharpUtils.utcZero).TotalSeconds;
                       x.SendState();
                   }
               }, IsEnabled, IsVisible);

            MyAPIGateway.TerminalControls.CreateSlider<CapturableSafeZone, IMyRadioAntenna>("Interval",
                "Interval", "Time in seconds, how often events are",
                1f, 14 * 86400f,
                (x) => x.settings.EventInterval,
                (x, sb) => sb.Append(TimeSpan.FromSeconds(x.settings.EventInterval)),
                (x, y) =>
                {
                    x.settings.EventInterval = (long)y;
                    x.SendState();
                }, IsEnabled, IsVisible);


            MyAPIGateway.TerminalControls.CreateSlider<CapturableSafeZone, IMyRadioAntenna>("Duration",
                "Duration to capture in seconds", "Duration of event",
                1f, 14 * 86400f,
                (x) => x.settings.EventDuration,
                (x, sb) => sb.Append(x.settings.EventDuration),
                (x, y) =>
                {
                    x.settings.EventDuration = (long)y;
                    x.SendState();
                }, IsEnabled, IsVisible);

            MyAPIGateway.TerminalControls.CreateSlider<CapturableSafeZone, IMyRadioAntenna>("TTC",
                "Time to capture", "Time to capture",
                1f, 14 * 86400f,
                (x) => x.settings.TimeToCapture,
                (x, sb) => sb.Append(x.settings.TimeToCapture),
                (x, y) =>
                {
                    x.settings.TimeToCapture = (long)y;
                    x.SendState();
                }, IsEnabled, IsVisible);

            MyAPIGateway.TerminalControls.CreateSlider<CapturableSafeZone, IMyRadioAntenna>("Radius",
                "SafeZone Radius", "SafeZone Radius",
                1f, 15000f,
                (x) => x.settings.Radius,
                (x, sb) => sb.Append(x.settings.Radius),
                (x, y) =>
                {
                    x.settings.Radius = (long)y;
                    x.SendState();
                }, IsEnabled, IsVisible);

            MyAPIGateway.TerminalControls.CreateTextbox<CapturableSafeZone, IMyRadioAntenna>("CapturedBy",
               "CapturedBy", "",
               (x) =>
               {
                   var fac = MyAPIGateway.Session.Factions.TryGetFactionById(x.settings.PrevCapturedByFaction);
                   if (fac == null) return new StringBuilder();
                   return new StringBuilder(fac.Tag);
               },
               (x, y) =>
               {
                   var faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(y.ToString());
                   if (faction == null)
                   {
                       x.settings.PrevCapturedByFaction = 0;//
                   }
                   else
                   {
                       x.settings.PrevCapturedByFaction = faction.FactionId;
                   }
                   x.SendState();
               }, IsEnabled, IsVisible);
        }
    }
}


