using Digi;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Scripts
{

    public class EMPEffectOnOff : EMPEffect {
        protected IMyFunctionalBlock myBlock;
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            myBlock = Entity as IMyFunctionalBlock;
            myBlock.EnabledChanged += EnabledChanged;
        }
        public override void Close() { myBlock.EnabledChanged -= EnabledChanged; }
        protected override bool GetTargetState() { return myBlock.Enabled; }
        protected override void ApplyState(bool enabled) { myBlock.Enabled = enabled; }
        private void EnabledChanged(IMyTerminalBlock obj) {
            OnStateChanged (GetTargetState());
        }
    }
    
    public abstract class EMPEffect : TimerEffect {
        IMyTerminalBlock myBlock;
        private bool targetState = false;
        private bool trigger = true;
        public bool returnTargetState = true;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            myBlock = Entity as IMyTerminalBlock;
        }

        protected abstract bool GetTargetState();
        protected abstract void ApplyState(bool enabled);
        
        public void OnStateChanged(bool targetState) {
            if (!trigger) {
                Log.Info("TimedEffect Silent Change : " + GetTargetState());
                return;
            }

            if (isTicking) {
                this.targetState = targetState;
                SilentSetState(false);
                Common.ShowNotificationForAllInGrid(myBlock.CubeGrid, "Your block was disabled by EM-Impulse for " + (duration / 60) + " s", 2000, MyFontEnum.Blue);
            }
        }
        
        private void SilentSetState(bool enabled) {
            trigger = false;
            ApplyState(enabled);
            trigger = true;
        }
        
        public override void OnTimerFinished() {
            if (returnTargetState && targetState) {
                SilentSetState(true);
            }

            myBlock.SetDamageEffect(false);
        }

        public override void AddTimer(int time) {
            var was = isTicking;
            base.AddTimer(time);
            if (!was) {
                myBlock.SetDamageEffect(true);
                targetState = GetTargetState();
                SilentSetState(false);
            }
        }
    }
}