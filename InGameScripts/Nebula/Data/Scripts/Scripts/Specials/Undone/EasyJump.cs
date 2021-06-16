using Digi;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials;
using Scripts.Specials.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRageMath;

namespace Scripts.Main {

    /*[MyEntityComponentDescriptor(typeof(MyObjectBuilder_JumpDrive), true, new string[] { "LargeJumpDrive", "LargeJumpDriveMK1" })] //add here more
    public class EasyJump : MyGameLogicComponent {
        public static void Init () {
            if (MyAPIGateway.Multiplayer.IsServer) {
                 Common.SendChatMessage ("EasyJump Init");
                MyAPIGateway.Multiplayer.RegisterMessageHandler (34563, HandleJumpRequest);
            }
        }

        public static void Unload () {
            if (MyAPIGateway.Multiplayer.IsServer) {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler (34563, HandleJumpRequest);
            }
        }

         public static void SendRequest (IMyTerminalBlock block) {
            var target = block.WorldMatrix.Translation + block.WorldMatrix.Forward * 50*1000;
            var data = new byte[24 + 8 + 8];
            var o = 0;
            o += Bytes.Pack (ref data, o, target.X);
            o += Bytes.Pack (ref data, o, target.Y);
            o += Bytes.Pack (ref data, o, target.Z);
            o += Bytes.Pack (ref data, o, block.CubeGrid.EntityId);
            o += Bytes.Pack (ref data, o, MyAPIGateway.Session.Player.IdentityId);

            Common.SendChatMessage (block.CubeGrid.EntityId + " / " + MyAPIGateway.Session.Player.IdentityId);

            MyAPIGateway.Multiplayer.SendMessageToServer (34563, data);
        }

        private static void HandleJumpRequest(byte[] obj) {
            try {
               var x = BitConverter.ToDouble (obj, 0);
               var y = BitConverter.ToDouble (obj, 8);
               var z = BitConverter.ToDouble (obj, 16);
               var grid = BitConverter.ToInt64 (obj, 24);
               var who = BitConverter.ToInt64 (obj, 32);
               var gps = new Vector3D (x,y,z);

               JumpdriveSystem.TryJump (grid, who, gps);
               Common.SendChatMessage ("HandleJumpRequest:" + x + y + z);
            } catch (Exception e) {
                Common.SendChatMessage ("HandleJumpRequest:Error:" + e.Message);
            }
        }


        //================================================================

        public static bool initedControls = false;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            Common.SendChatMessage ("Init!");
             if (!initedControls) {// && !MyAPIGateway.Multiplayer.IsServer) {
                 initedControls = true;
                 CreateControls();
             }
        }

        public static void CreateControls () {
            Common.SendChatMessage ("CreateControls!");

            var jumpControl = MyAPIGateway.TerminalControls.CreateAction<IMyJumpDrive>("EasyJump");
            jumpControl.Icon = @"Data\Blocks\CoalReactor\Icons\CoalReactor.dds";
            jumpControl.Name = new StringBuilder("Easy jump");
            jumpControl.Action = (b) => SendRequest (b);
            jumpControl.Writer = (b, t) => { };
            jumpControl.Enabled = (b) => { return b.BlockDefinition.SubtypeName.Contains ("JumpDrive"); };

            MyAPIGateway.TerminalControls.AddAction<IMyJumpDrive> (jumpControl);
        }
    }*/
}
