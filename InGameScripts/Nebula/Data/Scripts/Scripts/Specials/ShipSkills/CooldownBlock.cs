using Digi;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Scripts {

    public abstract class CooldownBlockOnOff : CooldownBlock {
        protected IMyFunctionalBlock myBlock;

        protected abstract void InitDuration();
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            myBlock = Entity as IMyFunctionalBlock;
            InitDuration();
            base.Init(objectBuilder);
            myBlock.EnabledChanged += EnabledChanged;
        }
        public override void Close() { myBlock.EnabledChanged -= EnabledChanged; }
        protected override bool GetTargetState() { return myBlock.Enabled; }
        protected override void ApplyState(bool enabled) { myBlock.Enabled = enabled; }
        private void EnabledChanged(IMyTerminalBlock obj) {
            OnStateChanged (GetTargetState());
        }
    }

    public abstract class CooldownActionBlockOnOff : CooldownActionBlock {
        protected IMyFunctionalBlock myBlock;

        protected abstract void InitDuration();
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            myBlock = Entity as IMyFunctionalBlock;
            InitDuration();
            base.Init(objectBuilder);
            myBlock.EnabledChanged += EnabledChanged;
        }
        public override void Close() { myBlock.EnabledChanged -= EnabledChanged; }
        protected override bool GetTargetState() { return myBlock.Enabled; }
        protected override void ApplyState(bool enabled) { myBlock.Enabled = enabled; }
        private void EnabledChanged(IMyTerminalBlock obj) {
            OnStateChanged (GetTargetState());
        }
    }

    public abstract class CooldownActionBlock : TimerEffect {
        private bool trigger = true;
        protected int cooldownDuration = 300;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            SilentSetState(false);
        }

        protected abstract bool GetTargetState();
        protected abstract void ApplyState(bool enabled);

        public void OnStateChanged(bool state) {
            if (!trigger) { return; }
            var emp = Entity.GetAs<EMPEffect>();
            if (emp != null && emp.isTicking) {
                OnCooldown();
                SilentSetState(false);
                OnActivationFailed();
                return;
            }

            if (isTicking && state) {
                SilentSetState(false);  //return it state
                OnActivationFailed();
                return;
            }

            if (!state) { // hm strange
                
            } else {
                OnActivated();
                AddTimer(cooldownDuration);
            }
        }

        public override void OnTimerFinished() {
            OnCooldownEnded();
        }

        private void SilentSetState(bool enabled) {
            trigger = false;
            ApplyState(enabled);
            trigger = true;
        }

        
        public virtual void OnActivated() { }
        public virtual void OnActivationFailed() { Common.ShowNotificationForAllInGrid((Entity as IMyCubeBlock).CubeGrid, "You cant activate block. Cooldown: " + (duration / 60)+ " s.", 1000, "Red");}
        public virtual void OnCooldown() { }
        public virtual void OnCooldownEnded() { }
    }
    
     public abstract class CooldownBlock : TimerEffect { 
        private bool isCooldownTimer = false;
        private bool trigger = true;

        protected int activeDuration = 300;
        protected int cooldownDuration = 300;
        protected IMyCubeBlock myBlock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            myBlock = Entity as IMyCubeBlock;
            SilentSetState(false);
        }

        protected abstract bool GetTargetState();
        protected abstract void ApplyState(bool enabled);

        public void OnStateChanged(bool state) {
            if (!trigger) { return; }

            var emp = myBlock.GetAs<EMPEffect>();
            if (emp != null && emp.isTicking) {
                Log.Error("EMP EFFECT: Setting false");
                OnCooldown();
                SilentSetState(false);
                OnActivationFailed();
                return;
            }

            if (isTicking && state) {
                SilentSetState(false);  //return it state
                OnActivationFailed();
                return;
            }

            if (!state) { // hm strange
                if (!isCooldownTimer) {
                    duration = 0;
                    OnTimerFinished();
                }
            } else {
                OnActivated();
                isCooldownTimer = false;
                AddTimer(activeDuration);
            }
        }

        public override void OnTimerFinished() {
            if (!isCooldownTimer) {
                Log.Info("TimedEffect: Activate ended " + Entity);
                isCooldownTimer = true;
                AddTimer(cooldownDuration);
                SilentSetState(false);
                myBlock.SetDamageEffect(true);
                OnCooldown();
                return;
            } else {
                Log.Info("TimedEffect: Cooldowned " + Entity);
                myBlock.SetDamageEffect(false);
                OnCooldownEnded();
            }
        }

        private void SilentSetState(bool enabled) {
            trigger = false;
            ApplyState(enabled);
            trigger = true;
        }

        
        public virtual void OnActivated() { }
        public virtual void OnActivationFailed() { }
        public virtual void OnCooldown() { }
        public virtual void OnCooldownEnded() { }
    }
}