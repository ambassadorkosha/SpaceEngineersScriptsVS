using System;
using System.Text;
using Digi;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using ServerMod;

namespace Digi2.AeroWings {


    interface IWing
    {
        double GetLiftForce ();
        Vector3D GetUpVector ();
        Vector3D GetForwardVector ();
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false,
			"aero-wing_1x5x1_rounded_edge_Small",
            "aero-wing_2x5x1_rounded_edge_Small",
            "aero-wing_3x5x1_rounded_edge_Small",
            "aero-wing_4x5x1_rounded_edge_Small",
            "aero-wing_5x5x1_rounded_edge_Small",
            "aero-wing_6x5x1_rounded_edge_Small",
            "aero-wing_5x3x1_rounded_edge_Small",
            "aero-wing_5x2x1_rounded_edge_Small",
            "aero-wing_5x1x1_rounded_edge_Small",
			"aero-wing_2x5x1_pointed_edge_Small",
			"aero-wing_3x5x1_pointed_edge_Small",
			"aero-wing_4x5x1_pointed_edge_Small",
			"aero-wing_5x5x1_pointed_edge_Small",
			"aero-wing_6x5x1_pointed_edge_Small",
			"aero-wing_7x5x1_pointed_edge_Small",
			"aero-wing_2x1x1_pointed_edge_Small",
			"aero-wing_1x5x1_rounded_edge_Large",
            "aero-wing_2x5x1_rounded_edge_Large",
            "aero-wing_3x5x1_rounded_edge_Large",
            "aero-wing_4x5x1_rounded_edge_Large",
            "aero-wing_5x5x1_rounded_edge_Large",
            "aero-wing_6x5x1_rounded_edge_Large",
            "aero-wing_5x3x1_rounded_edge_Large",
            "aero-wing_5x2x1_rounded_edge_Large",
            "aero-wing_5x1x1_rounded_edge_Large",
            "aero-wing_2x5x1_pointed_edge_Large",
            "aero-wing_3x5x1_pointed_edge_Large",
            "aero-wing_4x5x1_pointed_edge_Large",
            "aero-wing_5x5x1_pointed_edge_Large",
            "aero-wing_6x5x1_pointed_edge_Large",
            "aero-wing_7x5x1_pointed_edge_Large",
            "aero-wing_2x1x1_pointed_edge_Large",

			"aero-wingT2_1x5x1_rounded_edge_Small",
			"aero-wingT2_2x5x1_rounded_edge_Small",
			"aero-wingT2_3x5x1_rounded_edge_Small",
			"aero-wingT2_4x5x1_rounded_edge_Small",
			"aero-wingT2_5x5x1_rounded_edge_Small",
			"aero-wingT2_6x5x1_rounded_edge_Small",
			"aero-wingT2_5x3x1_rounded_edge_Small",
			"aero-wingT2_5x2x1_rounded_edge_Small",
			"aero-wingT2_5x1x1_rounded_edge_Small",
			"aero-wingT2_2x5x1_pointed_edge_Small",
			"aero-wingT2_3x5x1_pointed_edge_Small",
			"aero-wingT2_4x5x1_pointed_edge_Small",
			"aero-wingT2_5x5x1_pointed_edge_Small",
			"aero-wingT2_6x5x1_pointed_edge_Small",
			"aero-wingT2_7x5x1_pointed_edge_Small",
			"aero-wingT2_2x1x1_pointed_edge_Small",
			"aero-wingT2_1x5x1_rounded_edge_Large",
			"aero-wingT2_2x5x1_rounded_edge_Large",
			"aero-wingT2_3x5x1_rounded_edge_Large",
			"aero-wingT2_4x5x1_rounded_edge_Large",
			"aero-wingT2_5x5x1_rounded_edge_Large",
			"aero-wingT2_6x5x1_rounded_edge_Large",
			"aero-wingT2_5x3x1_rounded_edge_Large",
			"aero-wingT2_5x2x1_rounded_edge_Large",
			"aero-wingT2_5x1x1_rounded_edge_Large",
			"aero-wingT2_2x5x1_pointed_edge_Large",
			"aero-wingT2_3x5x1_pointed_edge_Large",
			"aero-wingT2_4x5x1_pointed_edge_Large",
			"aero-wingT2_5x5x1_pointed_edge_Large",
			"aero-wingT2_6x5x1_pointed_edge_Large",
			"aero-wingT2_7x5x1_pointed_edge_Large",
			"aero-wingT2_2x1x1_pointed_edge_Large",

			"aero-wingT3_1x5x1_rounded_edge_Small",
			"aero-wingT3_2x5x1_rounded_edge_Small",
			"aero-wingT3_3x5x1_rounded_edge_Small",
			"aero-wingT3_4x5x1_rounded_edge_Small",
			"aero-wingT3_5x5x1_rounded_edge_Small",
			"aero-wingT3_6x5x1_rounded_edge_Small",
			"aero-wingT3_5x3x1_rounded_edge_Small",
			"aero-wingT3_5x2x1_rounded_edge_Small",
			"aero-wingT3_5x1x1_rounded_edge_Small",
			"aero-wingT3_2x5x1_pointed_edge_Small",
			"aero-wingT3_3x5x1_pointed_edge_Small",
			"aero-wingT3_4x5x1_pointed_edge_Small",
			"aero-wingT3_5x5x1_pointed_edge_Small",
			"aero-wingT3_6x5x1_pointed_edge_Small",
			"aero-wingT3_7x5x1_pointed_edge_Small",
			"aero-wingT3_2x1x1_pointed_edge_Small",
			"aero-wingT3_1x5x1_rounded_edge_Large",
			"aero-wingT3_2x5x1_rounded_edge_Large",
			"aero-wingT3_3x5x1_rounded_edge_Large",
			"aero-wingT3_4x5x1_rounded_edge_Large",
			"aero-wingT3_5x5x1_rounded_edge_Large",
			"aero-wingT3_6x5x1_rounded_edge_Large",
			"aero-wingT3_5x3x1_rounded_edge_Large",
			"aero-wingT3_5x2x1_rounded_edge_Large",
			"aero-wingT3_5x1x1_rounded_edge_Large",
			"aero-wingT3_2x5x1_pointed_edge_Large",
			"aero-wingT3_3x5x1_pointed_edge_Large",
			"aero-wingT3_4x5x1_pointed_edge_Large",
			"aero-wingT3_5x5x1_pointed_edge_Large",
			"aero-wingT3_6x5x1_pointed_edge_Large",
			"aero-wingT3_7x5x1_pointed_edge_Large",
			"aero-wingT3_2x1x1_pointed_edge_Large",

			"aero-wingT4_1x5x1_rounded_edge_Small",
			"aero-wingT4_2x5x1_rounded_edge_Small",
			"aero-wingT4_3x5x1_rounded_edge_Small",
			"aero-wingT4_4x5x1_rounded_edge_Small",
			"aero-wingT4_5x5x1_rounded_edge_Small",
			"aero-wingT4_6x5x1_rounded_edge_Small",
			"aero-wingT4_5x3x1_rounded_edge_Small",
			"aero-wingT4_5x2x1_rounded_edge_Small",
			"aero-wingT4_5x1x1_rounded_edge_Small",
			"aero-wingT4_2x5x1_pointed_edge_Small",
			"aero-wingT4_3x5x1_pointed_edge_Small",
			"aero-wingT4_4x5x1_pointed_edge_Small",
			"aero-wingT4_5x5x1_pointed_edge_Small",
			"aero-wingT4_6x5x1_pointed_edge_Small",
			"aero-wingT4_7x5x1_pointed_edge_Small",
			"aero-wingT4_2x1x1_pointed_edge_Small",
			"aero-wingT4_1x5x1_rounded_edge_Large",
			"aero-wingT4_2x5x1_rounded_edge_Large",
			"aero-wingT4_3x5x1_rounded_edge_Large",
			"aero-wingT4_4x5x1_rounded_edge_Large",
			"aero-wingT4_5x5x1_rounded_edge_Large",
			"aero-wingT4_6x5x1_rounded_edge_Large",
			"aero-wingT4_5x3x1_rounded_edge_Large",
			"aero-wingT4_5x2x1_rounded_edge_Large",
			"aero-wingT4_5x1x1_rounded_edge_Large",
			"aero-wingT4_2x5x1_pointed_edge_Large",
			"aero-wingT4_3x5x1_pointed_edge_Large",
			"aero-wingT4_4x5x1_pointed_edge_Large",
			"aero-wingT4_5x5x1_pointed_edge_Large",
			"aero-wingT4_6x5x1_pointed_edge_Large",
			"aero-wingT4_7x5x1_pointed_edge_Large",
			"aero-wingT4_2x1x1_pointed_edge_Large"
		)]
    public class WingTN : MyGameLogicComponent, IWing
    {
        private IMyTerminalBlock block;

        public double _forceMlt = 0.75d;
		public double _fw = 0f;

        public static double BREAK_FORCE = 0.25;
		

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
			block = (IMyTerminalBlock)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            initBlockParams();
		}

        public override void UpdateOnceBeforeFrame() {
            try {
                if (MyAPIGateway.Session.IsServer) {
                    block.ShowInTerminal = false;
                    block.ShowInToolbarConfig = false;
                }
            } catch (Exception e) {
                Log.Error(e, $"UpdateOnceBeforeFrame error {e.Message}, blockId={Entity?.EntityId}");
            }
        }

        public double GetLiftForce()
        {
            return _forceMlt;
        }

        public Vector3D GetUpVector()
        {
            return block.WorldMatrix.Up;
        }

        public Vector3D GetForwardVector()
        {
            return Vector3D.Normalize(block.WorldMatrix.Left + block.WorldMatrix.Forward * _fw);
        }


        public void initBlockParams() {
			var s = block.BlockDefinition.SubtypeId;

			if (s.Contains ("2x5x1_r") || s.Contains("5x2x1_r") || s.Contains("3x5x1_p")) 
			{
				_forceMlt = 1.0;
				_fw = 0.15;
			}
			else if (s.Contains("3x5x1_r") || s.Contains("5x3x1_r") || s.Contains("4x5x1_p"))
			{
				_forceMlt = 1.25;
				_fw = 0.25;
			}
			else if (s.Contains("4x5x1_r") || s.Contains("5x5x1_p"))
			{
				_forceMlt = 1.5;
				_fw = 0.35;
			}
			else if (s.Contains("5x5x1_r") || s.Contains("6x5x1_p"))
			{
				_forceMlt = 1.75;
				_fw = 0.45;
			} 
			else if (s.Contains("6x5x1_r") || s.Contains("7x5x1_p"))
			{
				_forceMlt = 2.0;
				_fw = 0.55;
			}
			

			if (block.CubeGrid.GridSizeEnum == MyCubeSize.Large)
			{
				_forceMlt *=10;
			}

			var lvl = 1;
			if (s.Contains("T2")) lvl = 2;
			else if (s.Contains("T3")) lvl = 3;
			else if (s.Contains("T4")) lvl = 4;

			_forceMlt *= Math.Pow (1.3, lvl-1);
		}

        
    }
}
