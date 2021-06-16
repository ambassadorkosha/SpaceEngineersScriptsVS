using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using ServerMod;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;

namespace Scripts.Specials.Blocks.StackableMultipliers
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), true)]
    public class ThrusterWith2Gases : MyGameLogicComponent
    {
        public static MyDefinitionId OXYGEN = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");
        MyResourceSinkComponent sink;
        IMyThrust thruster;

        static bool initedStatic;
        bool inited = false;

        bool is2 = false;

        float MAX = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
            thruster = (Entity as IMyThrust);
            var sn = thruster.BlockDefinition.SubtypeName.ToLower();
            if (sn.Contains ("heli") || sn.Contains("atmo") || sn.Contains("vert") || sn.Contains("ion") || sn.Contains("hover"))
            {
                return;
            }
            is2 = true;
            var def = (thruster.SlimBlock.BlockDefinition as MyThrustDefinition);
            if (def.FuelConverter == null) {
                return;
            }


            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_10TH_FRAME;
            MAX = def.ForceMagnitude;
            if (!MyAPIGateway.Session.isTorchServer())
            {                                                                                                                
                thruster.AppendingCustomInfo += Thruster_AppendingCustomInfo;
            }
        }

        public void InitSink()
        {
            if (!is2) return;
            if (!inited)
            {
                inited = true;
                foreach (var th in thruster.Components)
                {
                    if (th is MyResourceSinkComponent)
                    {
                        sink = (th as MyResourceSinkComponent);
                        var resource = new MyResourceSinkInfo();
                        resource.MaxRequiredInput = 10000f;
                        resource.RequiredInputFunc = GetConsumption;
                        resource.ResourceTypeId = OXYGEN;
                        sink.AddType(ref resource);
                    }
                }
            }
        }

        public void UpdateMultiplierLogic ()
        {
            if (!is2) return;
            if (sink == null) return;
            var input = sink.CurrentInputByType(OXYGEN);
            var need = sink.RequiredInputByType(OXYGEN);
            if (need != 0)
            {
                var p = input / need;
                thruster.ThrustMultiplier = p;
            }
            else
            {
                thruster.ThrustMultiplier = 1;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            if (!is2) return;

            if (!inited)
            {
                InitSink();
            } else
            {
                UpdateMultiplierLogic();
            }
            base.UpdateAfterSimulation();
        }

        public float GetConsumption ()
        {
            var v = thruster.Enabled ? 1000f * (thruster.CurrentThrust / MAX) : 0;
            return v;
        }

        private void Thruster_AppendingCustomInfo(IMyTerminalBlock arg1, System.Text.StringBuilder arg2)
        {
            if (!is2) return;
            if (thruster == null) return;

            //TODO Possible nullpointer crash

            arg2.AppendLine("Current multiplier:" + thruster.ThrustMultiplier);
            var input = sink.CurrentInputByType(OXYGEN);
            var need = sink.RequiredInputByType(OXYGEN);
            arg2.Append("O2 Consumption:" + input + "/" + need + " L");
        }
    }
}
