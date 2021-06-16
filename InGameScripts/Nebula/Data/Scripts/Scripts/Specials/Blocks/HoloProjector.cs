using Digi;
using Sandbox.Game;
using Sandbox.ModAPI;
using Scripts.Base;
using Scripts.Specials;
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Scripts.Specials.ExtraInfo;
using ServerMod;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace Scripts {

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), true, new string[] { "HoloProjectorLarge", "HoloProjectorSmall"})]
    public class HoloProjector : MyGameLogicComponent {
        private static List<IMySlimBlock> nullList = new List<IMySlimBlock>();
        private static Vector3 exclude = new Vector3(0.8944445, 0.2, 0.55);//#FF00A1 / 322/100/100
        IMyProjector projector;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            projector = (Entity as IMyProjector);
            if (!MyAPIGateway.Session.isTorchServer()) { NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME; }
            
        }

        public override void UpdateBeforeSimulation() {
            base.UpdateBeforeSimulation();
            Apply();
        }
        public void Apply() {
            var g = projector.ProjectedGrid;
            if (g == null) return;
            if ((g as MyCubeGrid).BlocksCount > 400) return;
            g.GetBlocks(nullList, (x) => {
                if (Math.Abs(x.ColorMaskHSV.X - exclude.X) < 0.01f && Math.Abs(x.ColorMaskHSV.Y  - exclude.Y) < 0.01f && Math.Abs(x.ColorMaskHSV.Z - exclude.Z) < 0.01f) {
                    if (x.Dithering != 1f) x.Dithering = 1f;
                } else {
                    if (x.Dithering != 0f) x.Dithering = 0f;
                }
                
                return false;
            });
        }

        
    }
}
