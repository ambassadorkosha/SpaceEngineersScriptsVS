using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;
using Digi;
using Scripts;
using System;
using System.Text.RegularExpressions;
using Sandbox.Common.ObjectBuilders;
using SpaceEngineers.Game.ModAPI;
using Scripts.Specials.Messaging;

namespace ServerMod.Specials {
    
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), true, new string[] { "DefensiveTurret",
	//"CompactLargeGatlingTurret", "CompactLargeGatlingTurretT02", "CompactLargeGatlingTurretT03", "CompactLargeGatlingTurretT04",
	//"", "LargeGatlingTurretT02", "LargeGatlingTurretT03", "LargeGatlingTurretT04",
	//"SmallGatlingTurret", "SmallGatlingTurretT02", "SmallGatlingTurretT03", "SmallGatlingTurretT04"  })]
    public class FixGatlingTurrets : FixTurret  { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), true, new string[] {
	"", "LargeMissileTurretT02", "LargeMissileTurretT03", "LargeMissileTurretT04",
	"CompactLargeMissileTurret", "CompactLargeMissileTurretT02", "CompactLargeMissileTurretT03", "CompactLargeMissileTurretT04",
	"SmallMissileTurret", "SmallMissileTurret", "SmallMissileTurret", "SmallMissileTurret" })]
    public class FixMissleTurrets : FixTurret  { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), true, new string[] { "LargeInteriorTurret" })]
    public class FixInteriorTurret : FixTurret  { }
    
    public class FixTurret : MyGameLogicComponent  {
        private IMyLargeTurretBase myBlock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            myBlock = (Entity as IMyLargeTurretBase);
            myBlock.PropertiesChanged += MyBlock_PropertiesChanged;
            myBlock.OnMarkForClose += MyBlock_OnMarkForClose;
        }

        private void MyBlock_PropertiesChanged(IMyTerminalBlock obj) {
            myBlock.EnableIdleRotation = false;
        }

        private void MyBlock_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
             myBlock.OnMarkForClose -= MyBlock_OnMarkForClose;
             myBlock.PropertiesChanged -= MyBlock_OnMarkForClose;
        }
    }
}
