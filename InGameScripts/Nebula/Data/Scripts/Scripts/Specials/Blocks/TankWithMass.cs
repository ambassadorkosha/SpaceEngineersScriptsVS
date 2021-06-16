using Digi;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace ServerMod
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenTank), true)]
    public class TanksWithMass : MyGameLogicComponent
    {
        public static readonly double ICE_LITER_TO_KG = 1;
        public static readonly double OXY_LITER_TO_KG = 2;
        public static readonly double OIL_LITER_TO_KG = 0.05;
        public static readonly double KERO_LITER_TO_KG = 0.95;

        public static bool CustomControlsInit = false;
        private IMyOxygenTank container;
        private double ratio;

        private double bonus;

        SpecialInventory inventory;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            container = (IMyOxygenTank)Entity;

            var sn = container.BlockDefinition.SubtypeName;


            ratio = sn.Contains("Kerosene") ? KERO_LITER_TO_KG : sn.Contains("Oil") ? OIL_LITER_TO_KG : sn.Contains("Oxygen") ? ICE_LITER_TO_KG : ICE_LITER_TO_KG;

            inventory = new SpecialInventory(0, new Vector3(0d, 0d, 0d), MyInventoryFlags.CanSend);
            Entity.Components.Add<MyInventoryBase>(inventory);

            var level = 1;
            if (sn.Contains("T2")) level = 2;
            else if (sn.Contains("T3")) level = 3;
            else if (sn.Contains("T4")) level = 4;
            else if (sn.Contains("T5")) level = 5;
            else if (sn.Contains("T6")) level = 6;
            else if (sn.Contains("T7")) level = 7;
            else if (sn.Contains("T8")) level = 8;
            else if (sn.Contains("T9")) level = 9;
            else if (sn.Contains("T10")) level = 10;
            else if (sn.Contains("T11")) level = 11;
            else if (sn.Contains("T12")) level = 12;

            bonus = Math.Pow(1.05, level - 1);
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            inventory.specialMass = (container.FilledRatio * container.Capacity) * ratio / bonus;
            inventory.Refresh();
        }

        class SpecialInventory : MyInventory
        {
            public double specialMass = 0;
            public override MyFixedPoint CurrentMass => base.CurrentMass + (MyFixedPoint)specialMass;
            public SpecialInventory() : base() { }
            public SpecialInventory(MyObjectBuilder_InventoryDefinition definition, MyInventoryFlags flags) : base(definition, flags) { }
            public SpecialInventory(float maxVolume, Vector3 size, MyInventoryFlags flags) : base(maxVolume, size, flags) { }
            public SpecialInventory(float maxVolume, float maxMass, Vector3 size, MyInventoryFlags flags) : base(maxVolume, maxMass, size, flags) { }
            public SpecialInventory(MyFixedPoint maxVolume, MyFixedPoint maxMass, Vector3 size, MyInventoryFlags flags) : base(maxVolume, maxMass, size, flags) { }
        }
    }
}