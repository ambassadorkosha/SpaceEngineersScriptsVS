using Digi;
using Sandbox.Game;
using Sandbox.ModAPI;
using Scripts.Base;
using Scripts.Specials;
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts {

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenGenerator), true, new string[] { 
		"OilRefinerySmallT01",
		"OilRefinerySmallT02",
		"OilRefinerySmallT03",
		"OilRefinerySmallT04",
		"OilRefinerySmallT05",
		"OilRefinerySmallT06",
		"OilRefinerySmallT07",
		"OilRefinerySmallT08",
		"OilRefinerySmallT09",
		"OilRefinerySmallT10",
		"OilRefinerySmallT11",
		"OilRefinerySmallT12",
        "OilRefineryLargeT01",
        "OilRefineryLargeT02",
        "OilRefineryLargeT03",
        "OilRefineryLargeT04",
        "OilRefineryLargeT05",
        "OilRefineryLargeT06",
        "OilRefineryLargeT07",
        "OilRefineryLargeT08",
        "OilRefineryLargeT09",
        "OilRefineryLargeT10",
        "OilRefineryLargeT11",
        "OilRefineryLargeT12"
        })]
    public class OilRefinery : MyGameLogicComponent {
        public static MyDefinitionId ice = MyDefinitionId.Parse ("MyObjectBuilder_Ore/Ice");
        public static MyDefinitionId coal = MyDefinitionId.Parse ("MyObjectBuilder_Ore/FrozenOil");

        IMyGasGenerator gasgen;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            gasgen = (Entity as IMyGasGenerator);
            gasgen.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame() {
            base.UpdateOnceBeforeFrame();
        
            try {
                var inv =  gasgen.GetInventory() as MyInventory; 
                inv.Constraint.Add (coal);
                inv.Constraint.Remove (ice);
                //TorchConnection.FixOilGenerator (gasgen, coal);
            } catch (Exception e){
                Log.Error (e);
                gasgen.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }
    }
}
