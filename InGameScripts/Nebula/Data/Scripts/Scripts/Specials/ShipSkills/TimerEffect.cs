using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Scripts
{

    abstract public class TimerEffect : MyGameLogicComponent {

        public int duration = 0;
        private bool targetState = false;

        public bool isTicking { get { return duration > 0; } }

        public virtual void AddTimer(int time) {
            duration += time;
        }

        public virtual void OnTimerTick() { }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            NeedsUpdate |= GetUpdate();
        }

        public virtual MyEntityUpdateEnum GetUpdate() { return MyEntityUpdateEnum.EACH_10TH_FRAME; }


        public override void UpdateBeforeSimulation() { if (GetUpdate() == MyEntityUpdateEnum.EACH_FRAME) Update(1); }
        public override void UpdateBeforeSimulation10() { if (GetUpdate() == MyEntityUpdateEnum.EACH_10TH_FRAME) Update(10); }
        public override void UpdateBeforeSimulation100() { if (GetUpdate() == MyEntityUpdateEnum.EACH_100TH_FRAME) Update(100); }

        protected void Update(int time) {
            if (duration > 0) {
                duration -= time;

                OnTimerTick();

                if (duration <= 0) {
                    duration = 0;
                    OnTimerFinished();
                }
            }
        }
        public virtual bool CheckCanTickTimer () { return true; }
        public virtual void OnTimerFinished() { }
    }
}