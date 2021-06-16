using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;
using Digi;
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Scripts;
using Scripts.Specials;
using Scripts.Specials.Messaging;
using Slime;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using IMyBatteryBlock = Sandbox.ModAPI.IMyBatteryBlock;
using IMyFunctionalBlock = Sandbox.ModAPI.IMyFunctionalBlock;
using IMyJumpDrive = Sandbox.ModAPI.IMyJumpDrive;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

namespace ServerMod.Specials
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), true, new string[] { "EMPSmall", "EMPLarge" })]
    public class EMP2 : LimitedOnOffBlock {
        private static Dictionary<int, int> EMP = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 15);
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            SetOptions(EMP);
        }
    }
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), true, new string[] { "EMPSmall", "EMPLarge" })]
    public class EMP : EMPEffectOnOff  {
        public const ushort HANDLER = 31203;
        private static bool INITED = false;

        private const int DISABLE_FOR = 8; //s
        private const int DISABLE_EMP_FOR = 40; //s

        private IMyBatteryBlock myBlock;
        //protected override void InitDuration() { cooldownDuration = 6000; }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            myBlock = Entity as IMyBatteryBlock;
            if (!INITED) { 
                InitControls();
                INITED = true;
            }
            (myBlock as IMyFunctionalBlock).PropertiesChanged += OnPropertiesChanged;
            myBlock.OnMarkForClose += OnMarkForClose;
        }

        private void OnPropertiesChanged(IMyTerminalBlock obj) {
            if (myBlock.ChargeMode != ChargeMode.Recharge) {
                myBlock.ChargeMode = ChargeMode.Recharge;
            }
        }

        private void OnMarkForClose(IMyEntity obj) {
            (myBlock as IMyFunctionalBlock).PropertiesChanged -= OnPropertiesChanged;
            myBlock.OnMarkForClose -= OnMarkForClose;
        }


        public static void Init() {
            if (MyAPIGateway.Session.IsServer) {
                MyAPIGateway.Multiplayer.RegisterMessageHandler(HANDLER, HandleMessage);
            }
        }
        
        public static void Close() {
            if (MyAPIGateway.Session.IsServer) {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(HANDLER, HandleMessage);
            }
        }

        public static void HandleMessage(byte[] data) {
            var id = data.Long(0);
            var block = id.As<IMyTerminalBlock>();
            if (block == null) return;
            if (block.GetAs<EMP>() == null) return;
            EmpImpl(block);
        }


        public static void EmpRequest(IMyTerminalBlock b) {
            var data = new byte[8];
            data.Pack(0, b.EntityId);
            MyAPIGateway.Multiplayer.SendMessageToServer(HANDLER, data);
        }

        private void InitControls() {
            var throwAction = MyAPIGateway.TerminalControls.CreateAction<IMyBatteryBlock>("Impulse");
            throwAction.Name = new StringBuilder("Impulse");
            throwAction.Action = (b) => {
                try {
                    if (MyAPIGateway.Session.IsServer) { EmpImpl(b); } else { EmpRequest(b); }
                } catch (Exception e) { }
            };
            throwAction.Writer = (b, t) => { };
            throwAction.Enabled = (b) => { return b.GetAs<EMP>() != null; };
            MyAPIGateway.TerminalControls.AddAction<IMyBatteryBlock> (throwAction);
        }

        public static void EmpImpl(IMyTerminalBlock b) {
            try {
                var block = b.GetAs<EMP>().myBlock;
                if (!block.Enabled) {
                    Common.ShowNotificationForAllInRange("Your block must be enabled", 5000,  block.WorldMatrix.Translation, 1000, "Red");
                    return;
                }

                if (Math.Abs(block.CurrentStoredPower - block.MaxStoredPower) > 0.001f) {
                    Common.ShowNotificationForAllInRange("Your EMP is charged only for " + (100d*block.CurrentStoredPower / block.MaxStoredPower).toHumanQuantity() + "%", 5000,  block.WorldMatrix.Translation, 1000, "Red");
                    return;
                }
                
                

                var sink = block.Components.Get<MyResourceSourceComponent>();
                sink.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, 0.1f);
                sink.Enabled = true;
                //sink.SetMaxOutputByType(MyResourceDistributorComponent.ElectricityId, (block.SlimBlock.BlockDefinition as MyBatteryBlockDefinition).MaxPowerOutput);

                var emp = block.GetAs<EMPEffect>();
                if (emp != null && emp.isTicking) {
                    Common.ShowNotificationForAllInRange("Your block was disabled by EM-Impulse for " + (emp.duration / 60) + " s", 1000, block.WorldMatrix.Translation, 5000f, "Red");
                    return;
                }
                
                var limited = block.GetAs<LimitedBlock>();
                if (limited != null && !limited.CanBeActivated()) {
                    Common.ShowNotificationForAllInRange("Your block doesn't match the limits", 1000, block.WorldMatrix.Translation, 5000f, "Red");
                    return;
                }

                var v = new Vector3D();
                block.SlimBlock.ComputeWorldCenter(out v);
                var sphere = new BoundingSphereD(v, 6000d);
                var enities = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);
                var set = new HashSet<IMyCubeGrid>();

                foreach (var x in enities) {
                    var g = x as IMyCubeGrid;
                    if (g != null) {
                        set.Add(g);
                    }
                }

                foreach (var g in set) {
                    foreach (var y in g.GetShip().empEffects) {
                        

                        var empLogic = y as EMP;
                        if (empLogic != null)
                        {
                            y.AddTimer(DISABLE_EMP_FOR * 60);
                        } else
                        {
                            y.AddTimer(DISABLE_FOR * 60);
                        }

                        var jd = y as Warpdrive2;
                        if (jd != null) {
                            var jdd = (jd.Entity as IMyJumpDrive);
                            jdd.CurrentStoredPower *= 0.90f;
                        }
                    }
                }
                
                //b.GetAs<EMP>().AddTimer(84 * 60); // add 90 sec to emp
                
                
                var list = new List<Particle>(5);
                for (int i = 0; i < 5; i++) {
                    list.Add(new Particle() {
                        effectId = "Damage_Electrical_Damaged_EMP2",//"Damage_GravGen_Damaged",
                        translation = block.WorldMatrix.Translation,
                        forward = block.WorldMatrix.Forward,
                        up = block.WorldMatrix.Up,
                        time = 60*6,
                        scale = 35f,
                        position = Vector3D.Zero
                    });
                }
                
                ParticleDispatcher.AddEffect(list);
                ParticleDispatcher.AddEffectToOthers(list);
            } catch (Exception e) {
                Log.Error(e);
            }
        }
    }
}
