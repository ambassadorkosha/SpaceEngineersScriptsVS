using Digi;
using Sandbox.Common.ObjectBuilders;
using Scripts;
using Scripts.Shared;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace ServerMod.Specials {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), true, new string[] { "DefensiveTurret",
		"", "LargeGatlingTurretT02", "LargeGatlingTurretT03", "LargeGatlingTurretT04",
		"SmallGatlingTurret", "SmallGatlingTurretT02", "SmallGatlingTurretT03", "SmallGatlingTurretT04",
		"CompactLargeGatlingTurret", "CompactLargeGatlingTurretT02", "CompactLargeGatlingTurretT03", "CompactLargeGatlingTurretT04"})]
    public class GatlingEMP : TurretEMP { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), true, new string[] {"LargeInteriorTurret"})]
    public class InterriorTurretEMP : TurretEMP { }
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), true, new string[] {"MA_Designator_sm", "MA_Designator",
	"CompactLargeMissileTurret", "CompactLargeMissileTurretT02", "CompactLargeMissileTurretT03", "Compact LargeMissileTurretT04",
	"", "LargeMissileTurretT02", "LargeMissileTurretT03", "LargeMissileTurretT04",
	"SmallMissileTurret", "SmallMissileTurretT02", "SmallMissileTurretT03", "SmallMissileTurretT04"})]
    public class MissileLauncherEMP : TurretEMP { }
    
    
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), true, new string[] { "SmallCameraRaycast", "LargeCameraRaycast" })]
    public class CameraEMP : TurretEMP {  }

    
    public class TurretEMP : EMPEffectOnOff, Action1<long> {
        bool added = false;
		
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (!added){
                FrameExecutor.addFrameLogic(this);
                added = true;
            }
        }

        public override MyEntityUpdateEnum GetUpdate() { return MyEntityUpdateEnum.BEFORE_NEXT_FRAME; }


        public void run(long t) {
            if (myBlock == null || myBlock.Closed || myBlock.MarkedForClose) {
                FrameExecutor.removeFrameLogic (this);
                return;
            }
            Update(1);
        }
    }
    
}
