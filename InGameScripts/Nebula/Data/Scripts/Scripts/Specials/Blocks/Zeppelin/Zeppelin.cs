using Digi;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials.ExtraInfo;
using ServerMod;
using System;
using System.Text;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Slime {

    [ProtoContract]
    public class ZeppelinSettings
    {
        [ProtoMember (1)]
        public float DesiredLiftMlt;
        [ProtoMember (2)]
        public float CurrentLiftMlt;
    }

    public class ZeppelinBlockSettings
    {
        public float Lift;
        public float FillSpeed;
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenTank), false, new string [] {"Zeppelin", "ZeppelinT2", "ZeppelinT3", "ZeppelinT4" })]
    public class Zeppelin : GameLogicWithSyncAndSettings <ZeppelinSettings, ZeppelinBlockSettings, Zeppelin> {
        private static readonly Guid guid = new Guid("54faef65-a5ec-4a7f-8646-e619363b67aa"); //SERVER ONLY
        private static Sync<ZeppelinSettings, Zeppelin> sync;


        public static void Init ()
        {
            sync = new Sync<ZeppelinSettings, Zeppelin>(23361, (x)=>x.Settings, Handler);
        }

        private MyObjectBuilder_EntityBase builder;
        public IMyGasTank baloon;

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false) { return builder; }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            baloon = Entity as IMyGasTank;
            builder = objectBuilder;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            base.Init(objectBuilder);
        }

        public override void InitControls()
        {
            base.InitControls();
            var add5 = MyAPIGateway.TerminalControls.CreateAction<IMyGasTank>("Zeppelin_Add_5");
            add5.Action = (b) => AddToTargetThrust(b.GetAs<Zeppelin>(), 0.05f);
            add5.Name = new StringBuilder("+5%");
            add5.Writer = (b, sb) => GetTargetThrustInfo(b.GetAs<Zeppelin>(), sb);
            add5.Enabled = (b)=>b.GetAs<Zeppelin>() != null;
            MyAPIGateway.TerminalControls.AddAction<IMyGasTank>(add5);

            var remove5 = MyAPIGateway.TerminalControls.CreateAction<IMyGasTank>("Zeppelin_Remove_5");
            remove5.Action = (b) => AddToTargetThrust(b.GetAs<Zeppelin>(), -0.05f);
            remove5.Name = new StringBuilder("-5%");
            remove5.Writer = (b, sb) => GetTargetThrustInfo(b.GetAs<Zeppelin>(), sb);
            remove5.Enabled = (b) => b.GetAs<Zeppelin>() != null;
            MyAPIGateway.TerminalControls.AddAction<IMyGasTank>(remove5);

        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (needSave)
            {
                SaveSettings();
            }
        }

        public static void AddToTargetThrust (Zeppelin z, float p)
        {
            var v = z.Settings.DesiredLiftMlt;

            var v2 = MathHelper.Clamp(v+p, 0, 1);
            if (v != v2)
            {
                z.Settings.DesiredLiftMlt = v2;
                z.NotifyAndSave();
            }
        }

        public static void GetTargetThrustInfo(Zeppelin z, StringBuilder p)
        {
            p.Append((int)(z.Settings.CurrentLiftMlt * 100)).Append("/").Append ((int)(z.Settings.DesiredLiftMlt*100)).Append("/").Append((int)(100*z.baloon.FilledRatio));
        }

        public bool needSave = false;

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            if (Settings.CurrentLiftMlt != Settings.DesiredLiftMlt)
            {
                needSave = true;
                var value = SharpUtils.Lerp2 (Settings.CurrentLiftMlt, Settings.DesiredLiftMlt, BlockSettings.FillSpeed);
                Settings.CurrentLiftMlt = value;
                
                if (value != Settings.DesiredLiftMlt && MyAPIGateway.Session.IsServer) //Sending to clients current state
                {
                    SaveSettings();
                    sync.SendMessageToOthers(Entity.EntityId, Settings, true);
                }
            }
        }

        public override Guid GetGuid() { return guid; }
        public override Sync<ZeppelinSettings, Zeppelin> GetSync() { return sync; }
        public override ZeppelinSettings GetDefaultSettings() { return new ZeppelinSettings(); }
        public override void ApplyDataFromClient(ZeppelinSettings arrivedSettings,ulong userSteamId, byte type)
        {
            this.Settings.DesiredLiftMlt = MathHelper.Clamp(arrivedSettings.DesiredLiftMlt, 0f, 1f);
        }

        public override ZeppelinBlockSettings InitBlockSettings()
        {
            var s = baloon.BlockDefinition.SubtypeId;
            var lvl = 1f;
            if (s.Contains("T2")) lvl = 2f;
            else if (s.Contains("T3")) lvl = 3f;
            else if (s.Contains("T4")) lvl = 4.5f;

            float liftForce = 120000000f;
            liftForce *= (float)Math.Pow(2, lvl - 1);

            return new ZeppelinBlockSettings ()
            {
                Lift = liftForce,
                FillSpeed = 1f/(30*60)
            };
        }
    }

    public static class ZeppelinLogic
    {
        private const float COUNT_MLT = 0.1f;
        private const float CASUALITY = 2f;

        public static float Multiplier (Ship ship)
        {
            var planet = ship.closestPlanet;
            if (planet == null) return 0f;
            if (!planet.HasAtmosphere) return 0f;

            return Multiplier (ship, planet);
        }

        public static float Multiplier(Ship ship, MyPlanet planet)
        {
            return MathHelper.Clamp(planet.GetAirDensity(ship.grid.WorldMatrix.Translation) - 0.85f, 0f, 0.10f) * 10;
        }

        public static void UpdateBeforeSimulation(Ship ship, Vector3D massCenter, ref Vector3D TorqueSum)
        {
            var planet = ship.closestPlanet;
            if (planet == null) return;
            var phys = ship.grid.Physics;
            var target = ship.grid.Physics.Gravity;
            if (target.Length() <= 0.01) return;

            float atmo = 0;
            var pos = ship.grid.WorldMatrix.Translation;
            if (!planet.HasAtmosphere) return;
            atmo = Multiplier (ship, planet);

            var speed = phys.LinearVelocity;
            var mass = ship.massCache.shipMass;


            var leftForce = 1f;
            var finalForce = 0f;
            foreach (var g in ship.connectedGrids)
            {
                var sh = g.GetShip();
                if (sh == null) continue;
                if (g.Physics == null) continue;
                if (sh.zeppelin.Count == 0) continue;

                for (var x = 0; x < sh.zeppelin.Count; x++)
                {
                    var add = leftForce * COUNT_MLT;
                    finalForce += add;
                    leftForce -= add;
                }

                sh.extraFriction += speed.Length() * (float)(mass * atmo * finalForce);
                //Log.ChatError ("Ex:" + sh.extraFriction); 
                var sumForce = Vector3.Zero;
                foreach (var x in sh.zeppelin)
                {
                    var mlt = x.BlockSettings.Lift * x.Settings.CurrentLiftMlt * atmo * x.baloon.FilledRatio / 9.8;
                    var force = -target * (float)mlt;
                    sumForce += force;

                    g.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, null, null);//PoO - point of origin
                    var force3d = (Vector3D)(force / CASUALITY);
                    var position = x.Entity.GetPosition();
                    
                    sh.realisticTorque.AddRealisticTorque (ref position, ref force3d, ref massCenter, ref TorqueSum);
                }
                //Log.ChatError("Zepp:" + sh.zeppelin);

                if (!MyAPIGateway.Session.isTorchServer())
                {
                    var l = sumForce.Length();
                    if (sh.extraFriction > 0 && l > 0)
                    {
                        var cock = MyAPIGateway.Session.Player.Controller.ControlledEntity as IMyCockpit;
                        if (cock == null) return;
                        if (cock.CubeGrid != sh.grid && cock.Pilot == MyAPIGateway.Session.Player) return;

                        ShowInfo.Extra.Append("Zeppelin lift:").AppendLine(((double)l).toPhysicQuantity("N"));
                    }

                    ShowInfo.Extra.Append("Zeppelin lift mlt:").AppendLine(""+atmo);
                }
            }
        }
    }
}