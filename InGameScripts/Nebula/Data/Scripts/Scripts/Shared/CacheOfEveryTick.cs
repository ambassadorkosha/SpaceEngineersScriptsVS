using System;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Scripts.Shared
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class CacheOfEveryTick : MySessionComponentBase
    {
        public static double ScrollСache;
        private DateTime Offset = DateTime.Now;

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            UpdateScroll();
        }

        public void UpdateScroll()
        {
            var value = MyAPIGateway.Input.DeltaMouseScrollWheelValue() / 120d;
            if (value != 0)
            {
                ScrollСache += value;
                Offset = DateTime.Now + TimeSpan.FromSeconds(0.5);
            }
            if (Offset < DateTime.Now) ScrollСache = 0;
        }
    }
}