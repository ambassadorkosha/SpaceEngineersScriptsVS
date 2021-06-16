using System.Collections.Generic;
using System.Text;
using Digi;
using Sandbox.ModAPI;
using ServerMod;
using Slime;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;


namespace Scripts.Specials {

    public abstract class LimitedOnOffBlock : LimitedBlock {
        public IMyFunctionalBlock fblock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) { base.Init(objectBuilder); fblock = (Entity as IMyFunctionalBlock); }

        public override bool IsDrainingPoints() { return fblock.Enabled || !canBeDisabled; }

        public override void Disable() {
            fblock.Enabled = false;
        }
    }

    public abstract class LimitedForeverBlock : LimitedBlock
    {
        public override bool IsDrainingPoints() { return true; }
        public override bool CanBeDisabled() { return false; }
        public override void Disable() { }
    }

    public abstract class DisabledOnSubpartsLimitedOnOffBlock : LimitedOnOffBlock
    {
        bool CanBeEnabled = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            CanBeEnabled = true;
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            CanBeEnabled = isMain();
            if (!CanBeEnabled)
            {
                fblock.Enabled = false;
            }
        }

        public override bool CanBeActivated()
        {
            return CanBeEnabled && base.CanBeActivated();
        }

        private bool isMain()
        {
            var ship = block.CubeGrid.GetShip();
            if (ship != null)
            {
                foreach (var x in ship.Cockpits)
                {
                    if (x.IsMainControlledCockpit()) return true;
                }
            }

            return false;
        }
    }

    public abstract class LimitedBlock : MyGameLogicComponent {
        public IMyTerminalBlock block;
        protected bool canBeDisabled;
        protected Dictionary<int, int> limits;
        public Dictionary<int, int> GetLimits() { return limits; }

        public abstract bool IsDrainingPoints();
        public abstract void Disable();

        public virtual bool CanBeDisabled()
        {
            return canBeDisabled;
        }

        protected void SetOptions(Dictionary<int, int> limits, bool canBeDisabled = true) {
            this.limits = limits;
            this.canBeDisabled = canBeDisabled;
        }

        public virtual bool CanBeActivated() { //TODO Optimize
            LimitsChecker.CheckLimitsInGrid(block.CubeGrid);
            return IsDrainingPoints();
        }

        public virtual bool CheckConditions (SpecBlock specblock)
        {
            return true;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            block = (Entity as IMyTerminalBlock);
            block.OnMarkForClose += BlockOnOnMarkForClose;
            if (!MyAPIGateway.Session.isTorchServer()) {
                block.AppendingCustomInfo += BlockOnAppendingCustomInfo;
                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame() {
            base.UpdateOnceBeforeFrame();
            block.RefreshCustomInfo();
        }

        private void BlockOnAppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder arg2) { 
            arg2.Append("\r\n>>> Limits: <<<\r\n");
            foreach (var x in limits) {
                if (x.Value > 0) {
                    arg2.Append(LimitsChecker.GetTypeDesciption(x.Key)).Append(" : ").Append(x.Value).Append("\r\n");
                }
            }
        }

        private void BlockOnOnMarkForClose(IMyEntity obj) {
            block.OnMarkForClose -= BlockOnOnMarkForClose;
            if (!MyAPIGateway.Session.isTorchServer()) {
                block.AppendingCustomInfo -= BlockOnAppendingCustomInfo;
            }
        }
    }
}