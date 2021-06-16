using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Shared;
using Slime;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace ServerMod.Specials {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MergeBlock), false, new string[] {
         "LargeShipThrowBlockT1","LargeShipThrowBlockT2","LargeShipThrowBlockT3","LargeShipThrowBlockT4","LargeShipThrowBlockT5",
         "SmallShipThrowBlockT1","SmallShipThrowBlockT2","SmallShipThrowBlockT3","SmallShipThrowBlockT4","SmallShipThrowBlockT5"
     })]
    public class ThrowBlock : MyGameLogicComponent {
        public static ushort THROW_BLOCK_PORT = 23333;

        public static void Init() {
            if (MyAPIGateway.Session.IsServer) {
                MyAPIGateway.Multiplayer.RegisterMessageHandler(THROW_BLOCK_PORT, HandleMessage);
            }
        }
        
        public static void Close() {
            if (MyAPIGateway.Session.IsServer) {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(THROW_BLOCK_PORT, HandleMessage);
            }
        }

        public static void ThrowBlockRequest(IMyTerminalBlock b, bool raze) {
            var data = new byte[9];
            data.Pack(0, b.EntityId);
            data[8] = (byte)(raze ? 1 : 0);
            MyAPIGateway.Multiplayer.SendMessageToServer(THROW_BLOCK_PORT, data);
        }

        
        
        private const double K = 1000;
        private const double MAX = K*K*K*100f;
        private Dictionary<String, double> DETACH_FORCES = new Dictionary<string, double>() {
            { "LargeShipThrowBlockT1", K*K*100d },
            { "LargeShipThrowBlockT2", K*K*300d },
            { "LargeShipThrowBlockT3", K*K*900d },
            { "LargeShipThrowBlockT4", K*K*2700d },
            { "LargeShipThrowBlockT5", K*K*8100d },
            { "SmallShipThrowBlockT1", K*K*10d },
            { "SmallShipThrowBlockT2", K*K*30d },
            { "SmallShipThrowBlockT3", K*K*90d },
            { "SmallShipThrowBlockT4", K*K*270d },
            { "SmallShipThrowBlockT5", K*K*810d }
        };
        
        private static bool inited = false;
        private IMyShipMergeBlock block;
        private bool isSmallBlock;
        private bool razeOnDetach;
        private double detachForce = 0;
        private double maxDetachForce = 10000;
        
        public static void HandleMessage(byte[] data) {
            try
            {
                var id = data.Long(0);
                var raze = data[8] == 1;
                var block = id.As<IMyTerminalBlock>();
                if (block == null) return;
                if (block.GetAs<ThrowBlock>() == null) return;
                ThrowImpl(block, raze);
            } catch (Exception e)
            {

            }
        }
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            block = (Entity as IMyShipMergeBlock);

            try {
                maxDetachForce = DETACH_FORCES.GetValueOrDefault(block.SubtypeName(), 0);
            } catch (Exception e) {
                maxDetachForce = 0;
            }
            
            isSmallBlock = (block.CubeGrid as MyCubeGrid).GridSizeEnum == MyCubeSize.Small;
            block.CustomDataChanged += BlockOnCustomDataChanged;
            BlockOnCustomDataChanged(block);
            
            if (!inited) {
                inited = true;
                InitActions();
            }
        }
      
        private void Save (double force, bool razeBlock)
        {

        }

        private void Parse ()
        {

        }

        private void BlockOnCustomDataChanged(IMyTerminalBlock obj) {
            double result;
            try {
                if (double.TryParse(block.CustomData, out result)) {
                    detachForce = MathHelper.Clamp(result, 0, maxDetachForce);
                }
            } catch (Exception e) {
                Log.Error(e);   
            }
        }

        private static void InitActions () {
            var throwAction = MyAPIGateway.TerminalControls.CreateAction<IMyShipMergeBlock>("Throw");
            throwAction.Name = new StringBuilder("Throw");
            throwAction.Action = (b) => {
                try {
                    if (MyAPIGateway.Session.IsServer) { ThrowImpl(b, false); } else { ThrowBlockRequest(b, false); }
                } catch (Exception e) { }
            };
            throwAction.Writer = (b, t) => { };
            throwAction.Enabled = (b) => { return b.GetAs<ThrowBlock>() != null; };
            MyAPIGateway.TerminalControls.AddAction<IMyShipMergeBlock> (throwAction);


            var throwAction2 = MyAPIGateway.TerminalControls.CreateAction<IMyShipMergeBlock>("ThrowAndRaze");
            throwAction2.Name = new StringBuilder("Throw And Raze block");
            throwAction2.Action = (b) => {
                try
                {
                    if (MyAPIGateway.Session.IsServer) { ThrowImpl(b, true); } else { ThrowBlockRequest(b, true); }
                }
                catch (Exception e) { }
            };
            throwAction2.Writer = (b, t) => { };
            throwAction2.Enabled = (b) => { return b.GetAs<ThrowBlock>() != null; };
            MyAPIGateway.TerminalControls.AddAction<IMyShipMergeBlock>(throwAction2);



            var ForceControl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyShipMergeBlock>("MergeThrowForce");
            ForceControl.Title = MyStringId.GetOrCompute("Throw Force");
            ForceControl.Tooltip = MyStringId.GetOrCompute("Throw Force");
            
            ForceControl.SetLimits(0f, 1f);
            ForceControl.Writer = (b, t) => t.Append((b.GetAs<ThrowBlock>()?.detachForce ?? 0d).toPhysicQuantity("N"));
            ForceControl.Getter = (b) => {
                try {
                    var sm = b.GetAs<ThrowBlock>();
                    if (sm == null) return 0.5f;
                    return (float) (sm.detachForce / sm.maxDetachForce);
                } catch (Exception e) { return 0f; }
            };
            ForceControl.Setter = (b, v) => {
                try {
                    var sm = b.GetAs<ThrowBlock>();
                    if (sm == null) return;
                    sm.detachForce = v * sm.maxDetachForce;
                    b.CustomData = sm.detachForce.ToString(CultureInfo.InvariantCulture);
                } catch (Exception e) { }
            };
            ForceControl.Enabled = (b) => b.GetAs<ThrowBlock>() != null;
            ForceControl.Visible = (b) => b.GetAs<ThrowBlock>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyShipMergeBlock> (ForceControl);
        }

        public static void ThrowImpl(IMyTerminalBlock b, bool raze) {
            try {
                var m1 = (b as IMyShipMergeBlock);
                var m2 = m1.Other;
                if (m2 == null) return;
                var x2 = m2.GetAs<ThrowBlock>();
                if (x2 == null) return;
                var x1 = b.GetAs<ThrowBlock>();
            
                var f = Math.Min(x1.detachForce, x2.maxDetachForce);
                m1.Enabled = false;
                FrameExecutor.addDelayedLogic(2, new ThrowBlockAction(f, m1, m2, !x1.isSmallBlock, true, raze));
            } catch (Exception e) { }
        } 
    }
    
    public class ThrowBlockAction : Action1<long> {
        private IMyShipMergeBlock m1, m2;
        private double force;
        private bool dir;
        private bool razeM1, razeM2;

        public ThrowBlockAction(double force, IMyShipMergeBlock m1, IMyShipMergeBlock m2, bool dir, bool razeM1, bool razeM2) { 
            this.m1 = m1; 
            this.m2 = m2; 
            this.force = force;
            this.dir = dir;
            this.razeM1 = razeM1;
            this.razeM2 = razeM2;
        }
        
        public void run(long k) {
            if (m1.CubeGrid.Physics != null) {
                m1.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, (dir ? m1.WorldMatrix.Left : -m1.WorldMatrix.Forward) * force, m1.CubeGrid.Physics.CenterOfMassWorld, null, null, true);
            }
            
            if (m2.CubeGrid.Physics != null) {
                m2.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, (dir ? m2.WorldMatrix.Left : -m2.WorldMatrix.Forward) * force, m2.CubeGrid.Physics.CenterOfMassWorld, null, null, true);
            }

            if (razeM1) {
                m1.CubeGrid.RazeBlock (m1.Position);
            }

            if (razeM2)
            {
                m2.CubeGrid.RazeBlock(m2.Position);
            }
        }
    }
}