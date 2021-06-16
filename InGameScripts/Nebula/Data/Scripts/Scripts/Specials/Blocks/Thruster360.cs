using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;
using Scripts.Specials.Hologram.Data;
using ServerMod;
using Digi;
using Scripts.Shared;
using System.Text;
using Scripts.Specials.Blocks.Reactions;
using VRage.Utils;
using Sandbox.Game.EntityComponents;
using Slime;
using ProtoBuf;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Scripts.Specials.Blocks
{
    [ProtoContract]
    public class Thruster360Settings {
        [ProtoMember(1)]
        public float rotation;
        [ProtoMember(2)]
        public float targetRotation;
        [ProtoMember(3)]
        public float desiredThrust;
        [ProtoMember(4)]
        public bool clockWiseRotation;

        public override string ToString()
        {
            return $"Th360: {rotation} {targetRotation} {desiredThrust} {clockWiseRotation}";
        }
    }

	public class Thruster360BlockSettings {
		public float maxThrust;
		public float powerUsage;
		public float rotationSpeed;
		public bool mirrored;
		public float flameLength = 10;
	}

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] { 
        "Thruster360", 
        "Thruster360T02", 
        "Thruster360T03", 
        "Thruster360T04", 
        "Thruster360T05", 
        "Thruster360T06", 
        "Thruster360T07", 
        "Thruster360T08", 
        "Thruster360T09", 
        "Thruster360T10", 
        "Thruster360T11", 
        "Thruster360T12",

        "Thruster360_M",
        "Thruster360_MT02",
        "Thruster360_MT03",
        "Thruster360_MT04",
        "Thruster360_MT05",
        "Thruster360_MT06",
        "Thruster360_MT07",
        "Thruster360_MT08",
        "Thruster360_MT09",
        "Thruster360_MT10",
        "Thruster360_MT11",
        "Thruster360_MT12",

        "Thruster360_S",
        "Thruster360_ST02",
        "Thruster360_ST03",
        "Thruster360_ST04",
        "Thruster360_ST05",
        "Thruster360_ST06",
        "Thruster360_ST07",
        "Thruster360_ST08",
        "Thruster360_ST09",
        "Thruster360_ST10",
        "Thruster360_ST11",
        "Thruster360_ST12",

        "Thruster360_S_M",
        "Thruster360_S_MT02",
        "Thruster360_S_MT03",
        "Thruster360_S_MT04",
        "Thruster360_S_MT05",
        "Thruster360_S_MT06",
        "Thruster360_S_MT07",
        "Thruster360_S_MT08",
        "Thruster360_S_MT09",
        "Thruster360_S_MT10",
        "Thruster360_S_MT11",
        "Thruster360_S_MT12" })]
	public class Thruster360 : BlockReactionsOnKeys
	{
        private static readonly Guid guid = new Guid("5369d32e-cc0a-4fe1-b0f4-e6c09c8d9166"); //SERVER ONLY

        public const ushort PORT = 48832;
        static Random random = new Random();
        IMyUpgradeModule block;
		Thruster360Settings settings;
		HoloGroup pars = new HoloGroup();
		Thruster360BlockSettings blockSettings;
		static bool INITED = false;
		private bool modelInited = false;

        static Sync<Thruster360Settings, Thruster360> sync;
        MyResourceSinkComponent ssink;

        public static void Init ()
        {
            sync = new Sync<Thruster360Settings, Thruster360>(PORT, (x)=>x.settings, Handler);
        }

        public static void Handler (Thruster360 thruster, Thruster360Settings settings, ulong from, bool fromServer)
        {
            if (fromServer)
            {
                thruster.settings = settings;
            } 
            else
            {
                thruster.settings.desiredThrust = MathHelper.Clamp(settings.desiredThrust, 0, thruster.blockSettings.maxThrust);
                thruster.settings.targetRotation = settings.targetRotation;
                thruster.settings.clockWiseRotation = settings.clockWiseRotation;
                thruster.NotifyDataChanged();
            }
        }

        public void InitSink ()
        {
            ssink = new MyResourceSinkComponent();
            var resource = new MyResourceSinkInfo();
            resource.MaxRequiredInput = blockSettings.powerUsage;
            resource.RequiredInputFunc = GetConsumption;
            resource.ResourceTypeId = MyResourceDistributorComponent.ElectricityId;
            ssink.Init(MyStringHash.GetOrCompute("Thrust"), resource);
            //ssink.RequiredInputChanged += Ssink_RequiredInputChanged;
            //ssink.IsPoweredChanged += Ssink_IsPoweredChanged;
            //ssink.CurrentInputChanged += Ssink_CurrentInputChanged;
            block.ResourceSink = ssink;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			base.Init(objectBuilder);
			block = Entity as IMyUpgradeModule;


            var sn = block.SubtypeName();
            var mirrored = sn.Contains("_M");
            var small = sn.Contains("_S");

            var thrust = small ? 165000f : 2241000f;
            var power = small ? 1.1f : 14.94f;
            
            var lvl = 1;
            if (sn.Contains ("T02")) lvl = 2;
            else if (sn.Contains ("T03")) lvl = 3;
            else if (sn.Contains ("T04")) lvl = 4;
            else if (sn.Contains ("T05")) lvl = 5;
            else if (sn.Contains ("T06")) lvl = 6;
            else if (sn.Contains ("T07")) lvl = 7;
            else if (sn.Contains ("T08")) lvl = 8;
            else if (sn.Contains ("T09")) lvl = 9;
            else if (sn.Contains ("T10")) lvl = 10;
            else if (sn.Contains ("T11")) lvl = 11;
            else if (sn.Contains ("T12")) lvl = 12;

            thrust *= (float) Math.Pow (1.6d, lvl-1);
            power *= (float) Math.Pow (1.4d, lvl-1);

            blockSettings = new Thruster360BlockSettings {
                maxThrust = thrust,
                powerUsage = power,
                rotationSpeed = (float)Math.PI / 60f / 5f,
                mirrored = mirrored,
                flameLength = 10
            };

            if (MyAPIGateway.Session.IsServer)
            {
                if (!block.TryGetStorageData(guid, out settings))
                {
                    settings = new Thruster360Settings
                    {
                        rotation = 0,
                        targetRotation = 0,
                        desiredThrust = 0
                    };
                }
            } 
            else
            {
                settings = new Thruster360Settings
                {
                    rotation = 0,
                    targetRotation = 0,
                    desiredThrust = 0
                };
                sync.RequestData(block.EntityId);
            }
            

            Log.ChatError ("Settings:" + settings);

            InitSink();

            NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME | VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME;

			if (!INITED)
			{
				INITED = true;
				InitActions<IMyUpgradeModule>();
				InitBlockReactionsActions<Thruster360, IMyUpgradeModule>("Thruster");
			}
		}

        public override sealed void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            if (Entity.Storage == null) { 
                Entity.Storage = new MyModStorageComponent(); 
            }
        }

        public override void UpdateOnceBeforeFrame()
		{
			base.UpdateOnceBeforeFrame();
			if (!modelInited)
			{
				InitModel();
            }
		}

		private void InitModel ()
		{
			var p1 = new HoloParams();
			p1.info.scale = 2;
			p1.SetExactModel("Models\\Cubes\\Small\\AtmosphericThrusterLarge.mwm");
			p1.info.rotX = 0;
			p1.info.rotY = 0;
			p1.info.rotZ = 0;


			var p2 = new HoloParams();
			p2.info.scale = 2;
			p2.SetExactModel("Models\\Cubes\\Large\\SpaceBallBlock.mwm");
			p2.info.rotX = 0;
			p2.info.rotY = 0;
			p2.info.rotZ = 0;

			pars.holos = new List<HoloParams>();
			pars.holos.Add(p1);
			pars.holos.Add(p2);
			pars.target = new HoloTarget(block);

			pars.Spawn(block);
		}


		public static void SetTargetDegree(IMyTerminalBlock b, double value)
		{
			var th = b.GetAs<Thruster360>();
			if (th != null)
			{
				th.settings.targetRotation = (float)value;
			}
		}

		public override void OnOptionsChanged()
		{
			keyReactions = ReactionSet.Parse(reactionString, AddReactions);
		}

        public Thruster360SetDegree AddReactions(string action, string actionInfo, List<VRage.Input.MyKeys> keys, List<MoveIndicator> indicators)
        {
            var reactions = new List<KeysReactions>();
            float overrideValue;
            if (float.TryParse(action, out overrideValue))
            {
                return new Thruster360SetDegree()
                {
                    keys = keys,
                    moveIndicators = indicators,
                    degree = overrideValue,
                    thrust = this
                };
            }

            return null;
        }

		public static void AddToTargetDegree(IMyTerminalBlock b, double value)
		{
			var th = b.GetAs<Thruster360>();
			if (th == null) return;

            th.settings.targetRotation += (float)value;
            th.settings.clockWiseRotation = value > 0;
            th.NotifyDataChanged();
        }

		public static void ChangeTargetThrust(IMyTerminalBlock b, double value)
		{
			var th = b.GetAs<Thruster360>();
			if (th == null) return;

            th.settings.desiredThrust += (float)(th.blockSettings.maxThrust * value);
            th.settings.desiredThrust = MathHelper.Clamp (th.settings.desiredThrust, 0, th.blockSettings.maxThrust);
            th.NotifyDataChanged();
        }

        public void NotifyDataChanged ()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                sync.SendMessageToOthers(block.EntityId, settings);
                block.SetStorageData(guid, settings);
            }
            else
            {
                sync.SendMessageToServer(block.EntityId, settings);
            }
        }
        
        public float GetConsumption()
        {
            var sh = block.CubeGrid.GetShip();
            if (sh == null) return 0;
            sh.AtmosphereProperties.updateAtmosphere(block.CubeGrid);
            var p = settings.desiredThrust / blockSettings.maxThrust * blockSettings.powerUsage * sh.AtmosphereProperties.getWingValue();
            return (float)p;
        }

        public float GetThrust ()
        {
            var availiable = ssink.ResourceAvailableByType(MyResourceDistributorComponent.ElectricityId); //Should be always less than GetConsumption()
            var usage = Math.Min(availiable, GetConsumption());
            return Math.Min(usage, blockSettings.powerUsage) / blockSettings.powerUsage * blockSettings.maxThrust;
        }

        public override void Close()
        {
            base.Close();
            pars.Close();
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            if (!block.Enabled) return;

            ssink.Update();

            try
            {
                DoRotation();
                ApplyForce();
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        private void DoRotation ()
        {
            if (settings.rotation != settings.targetRotation)
            {
                if (settings.rotation < settings.targetRotation)
                {
                    settings.rotation += blockSettings.rotationSpeed;
                    if (settings.rotation > settings.targetRotation)
                    {
                        settings.rotation = settings.targetRotation;
                        block.SetStorageData(guid, settings); //TODO it is too heavy
                    }
                }
                else
                {
                    settings.rotation -= blockSettings.rotationSpeed;
                    if (settings.rotation < settings.targetRotation)
                    {
                        settings.rotation = settings.targetRotation;
                        block.SetStorageData(guid, settings); //TODO it is too heavy
                    }
                }
            }
        }

        private void ApplyForce ()
        {
            var ph = block.CubeGrid.Physics;
            if (ph == null) return;
            if (ph.IsStatic) return;
            var sh = block.CubeGrid.GetShip();
            if (sh == null) return;
            var f = GetForceVector() * GetThrust();

            block.CubeGrid.Physics?.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, f, null, null, null, false, false);
        }

        public Vector3 GetForceVector ()
        {
            var m = MatrixD.CreateWorld(Vector3D.Zero, block.WorldMatrix.Forward, block.WorldMatrix.Up) * Matrix.CreateFromAxisAngle(block.WorldMatrix.Forward, blockSettings.mirrored ? -settings.rotation : settings.rotation);
            return blockSettings.mirrored ? m.Down : m.Up;
        }

        public void DrawFlame()
        {
            var r = random.NextDouble() * 0.2d + 0.8d;
            var r2 = (float)(random.NextDouble() * 0.2d + 0.8d);


            var dir = -GetDirectionVector();
            var f = dir;
            var scale = (GetThrust() / blockSettings.maxThrust);

            f *= scale * blockSettings.flameLength;
            var center = GetCenter();
            PhysicsHelper.Draw(Color.White, center+ dir, f * r, 0.35f, "EngineThrustMiddle");
            PhysicsHelper.DrawPoint(Color.White, center + dir, 3f * r2 * scale, material: "EngineThrustMiddle");
        }

        private void UpdatePosition ()
        {
            if (pars.holos[0] == null || pars.holos[0].entity == null) return;
            var up = block.WorldMatrix.Up;
            var center = GetCenter();

            var mirror = (blockSettings.mirrored ? -1 : 1);
            var m = (MatrixD.CreateWorld(Vector3D.Zero, -mirror * block.WorldMatrix.Up, block.WorldMatrix.Left) * Matrix.CreateFromAxisAngle(block.WorldMatrix.Forward, settings.rotation * mirror));

            pars.holos[0].entity.WorldMatrix = m * Matrix.CreateTranslation(center);// * ;
            pars.holos[1].entity.WorldMatrix = m * Matrix.CreateTranslation(center);// * Matrix.CreateScale (v,v,v);
            var mm = pars.holos[0].entity.WorldMatrix;
        }

        public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation(); 
			if (MyAPIGateway.Session.isTorchServer()) return;

            try
			{
                DrawFlame();
                UpdatePosition();
            } catch (Exception e)
			{
				Log.ChatError (e);
			}
		}

        public Vector3 GetCenter()
        {
            var fw = block.WorldMatrix.Forward;
            return block.WorldMatrix.Translation + fw * 1;
        }

        public Vector3D GetDirectionVector()
        {
            var m = (MatrixD.CreateWorld(Vector3D.Zero, block.WorldMatrix.Forward, block.WorldMatrix.Up) * Matrix.CreateFromAxisAngle(block.WorldMatrix.Forward, blockSettings.mirrored ? -settings.rotation : settings.rotation));
            return blockSettings.mirrored ? m.Down : m.Up;
        }

        private static bool ActionEnabled(IMyTerminalBlock block)
        {
            return block.GetAs<Thruster360>() != null;
        }

        public static void GetTargetThrust(IMyTerminalBlock b, StringBuilder value)
        {
            var th = b.GetAs<Thruster360>();
            if (th != null)
            {
                value.Append((100 * th.settings.desiredThrust / th.blockSettings.maxThrust)).Append("% ");
            }
        }


        public static void GetTargetRotation(IMyTerminalBlock b, StringBuilder value)
        {
            var th = b.GetAs<Thruster360>();
            if (th != null)
            {
                value.AppendFormat("{0:N0}", (th.settings.targetRotation / Math.PI * 180)).Append("*");
            }
        }


        public static void InitActions<T>()
        {
            var addDegree90 = MyAPIGateway.TerminalControls.CreateAction<T>("Thruster360_Add90");
            addDegree90.Action = (b) => AddToTargetDegree(b, Math.PI / 2);
            addDegree90.Name = new StringBuilder("+90*");
            addDegree90.Writer = (b, sb) => GetTargetRotation(b, sb);
            addDegree90.Enabled = ActionEnabled;
            MyAPIGateway.TerminalControls.AddAction<T>(addDegree90);

            var addDegree45 = MyAPIGateway.TerminalControls.CreateAction<T>("Thruster360_Add45");
            addDegree45.Action = (b) => AddToTargetDegree(b, Math.PI / 4);
            addDegree45.Name = new StringBuilder("+45*");
            addDegree45.Writer = (b, sb) => GetTargetRotation(b, sb);
            addDegree45.Enabled = ActionEnabled;
            MyAPIGateway.TerminalControls.AddAction<T>(addDegree45);

            var addDegree30 = MyAPIGateway.TerminalControls.CreateAction<T>("Thruster360_Add30");
            addDegree30.Action = (b) => AddToTargetDegree(b, Math.PI / 6);
            addDegree30.Name = new StringBuilder("+30*");
            addDegree30.Writer = (b, sb) => GetTargetRotation(b, sb);
            addDegree30.Enabled = ActionEnabled;
            MyAPIGateway.TerminalControls.AddAction<T>(addDegree30);

            var remDegree90 = MyAPIGateway.TerminalControls.CreateAction<T>("Thruster360_Rem90");
            remDegree90.Action = (b) => AddToTargetDegree(b, -Math.PI / 2);
            remDegree90.Name = new StringBuilder("-90*");
            remDegree90.Writer = (b, sb) => GetTargetRotation(b, sb);
            remDegree90.Enabled = ActionEnabled;
            MyAPIGateway.TerminalControls.AddAction<T>(remDegree90);

            var remDegree45 = MyAPIGateway.TerminalControls.CreateAction<T>("Thruster360_Rem45");
            remDegree45.Action = (b) => AddToTargetDegree(b, -Math.PI / 4);
            remDegree45.Name = new StringBuilder("-45*");
            remDegree45.Writer = (b, sb) => GetTargetRotation(b, sb);
            remDegree45.Enabled = ActionEnabled;
            MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(remDegree45);

            var remDegree30 = MyAPIGateway.TerminalControls.CreateAction<T>("Thruster360_Rem30");
            remDegree30.Action = (b) => AddToTargetDegree(b, -Math.PI / 6);
            remDegree30.Name = new StringBuilder("-30*");
            remDegree30.Writer = (b, sb) => GetTargetRotation(b, sb);
            remDegree30.Enabled = ActionEnabled;
            MyAPIGateway.TerminalControls.AddAction<T>(remDegree30);

            var addThrust = MyAPIGateway.TerminalControls.CreateAction<T>("Thruster360_AddThrust_010");
            addThrust.Action = (b) => ChangeTargetThrust(b, 0.1f);
            addThrust.Name = new StringBuilder("+10% Thrust");
            addThrust.Writer = (b, sb) => GetTargetThrust(b, sb);
            addThrust.Enabled = ActionEnabled;
            MyAPIGateway.TerminalControls.AddAction<T>(addThrust);

            var remThrust = MyAPIGateway.TerminalControls.CreateAction<T>("Thruster360_RemThrust_010");
            remThrust.Action = (b) => ChangeTargetThrust(b, -0.1f);
            remThrust.Name = new StringBuilder("-10% Thrust");
            remThrust.Writer = (b, sb) => GetTargetThrust(b, sb);
            remThrust.Enabled = ActionEnabled;
            MyAPIGateway.TerminalControls.AddAction<T>(remThrust);
        }

        public override IMyTerminalControlTextbox GetControl() { return null; }
    }
}
