using System;
using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials.Safezones;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace Scripts.Specials.Blocks.StackableMultipliers
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false)]
	public class ThrusterBase : StackingMultipliersBlock
	{
        static bool INITED = false;

		IMyThrust thruster;
		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			thruster = (Entity as IMyThrust);
            NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;

            InitControls();

        }

        static bool Getter (ThrusterBase x)
        {
            return !x.thruster.CustomName.Contains("[NoAfterburn]");
        }

        static void Setter(ThrusterBase x, bool value)
        {
            if (x == null || x.thruster == null) return;

            var name = x.thruster.CustomName ?? "";
            if (value)
            {
                x.thruster.CustomName = name.Replace("[NoAfterburn]", "");
            }
            else
            {
                x.thruster.CustomName = name + "[NoAfterburn]";
            }
        }

        static void InitControls ()
        {
            if (INITED) return;
            INITED = true;

            MyAPIGateway.TerminalControls.CreateCheckbox<ThrusterBase, IMyThrust>("AfterburnerToggle",
               "React on Afterburner", "Can Afterburner affect on this thruster?", Getter, Setter);
        }

		public override void Apply(float m1, float m2, float m3)
		{
			thruster.ThrustMultiplier = m1;
			thruster.PowerConsumptionMultiplier = m2;
		}

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            Recalculate();
        }
    }
}
