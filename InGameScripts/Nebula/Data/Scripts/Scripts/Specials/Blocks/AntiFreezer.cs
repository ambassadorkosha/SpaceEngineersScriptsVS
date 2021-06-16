using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Specials.Blocks
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RadioAntenna), true, "AntiFreezeBlockLarge", "AntiFreezeBlockSmall")]
    public class AntiFreezer : MyGameLogicComponent
    {
        private IMyFunctionalBlock Block;
        private MyEntitySubpart subpart;
        private Matrix StartPosition;
        private const float Min = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            Block = Entity as IMyFunctionalBlock;
            Block.OnMarkForClose += OnOnMarkForClose;
            Block.IsWorkingChanged += UpdateEmissive;
        }
        private void OnOnMarkForClose(IMyEntity obj)
        {
            Block.IsWorkingChanged -= UpdateEmissive;
            Block.OnMarkForClose -= OnOnMarkForClose;
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            
            if (subpart == null || subpart.MarkedForClose || subpart.Closed)
            {
                if(Entity.TryGetSubpart("Arrow", out subpart)) StartPosition = subpart.PositionComp.LocalMatrixRef;
            }
            
            var def = (MyCubeBlockDefinition)Block.SlimBlock.BlockDefinition;
            var CriticalIntegrity = def.CriticalIntegrityRatio * Block.SlimBlock.MaxIntegrity;
            var IntegrityPercentage = Math.Max(Min, (Block.SlimBlock.Integrity - CriticalIntegrity) / (Block.SlimBlock.MaxIntegrity - CriticalIntegrity));
            var Angle = MathHelper.ToRadians(-270f * IntegrityPercentage);

            if (subpart != null) subpart.PositionComp.LocalMatrix = StartPosition * Matrix.CreateRotationY(Angle);
        }

        private void UpdateEmissive(IMyEntity block)
        {
            Block.SetEmissiveParts("Emissive", new Color(0, 0, 20), Block.IsWorking ? 5000f : 0f);
        }
    }
}