using Digi;
using ServerMod;
using System;
using VRage.Game.ModAPI;

namespace Scripts.Shared {
    public class GrindBlock : Action1<long> {
        Timer time;
        IMySlimBlock block;

        public static GrindBlock AddEffect (IMySlimBlock block, int time = 10) {
            var eff = new GrindBlock (block, time);
            FrameExecutor.addFrameLogic (eff);
            return eff;
        }

        public GrindBlock (IMySlimBlock block, int time=10) {
            this.block = block;
            this.time = new Timer (time);
        }

        public void run(long k) {
            if (time.tick()) {
                try {
                    block.CubeGrid.RemoveBlock (block);
                } catch (Exception e) {
                    Log.Error (e);
                }
                FrameExecutor.removeFrameLogic (this);
            }
        }
    }
}
