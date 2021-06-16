using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials.Hologram.Data {
    public class HoloGroup {
        public List<HoloParams> holos;
        public HoloTarget target;

        public HoloGroup() { }
        public HoloGroup (HoloTarget target, List<HoloParams> holos) {
            this.target = target;
            this.holos = holos;
        }

        public void Apply (IMyCubeBlock block, Vector3 offset) {
            foreach (var x in holos) {
                x.Apply (block, offset);
            }
        }

        public void Spawn (IMyCubeBlock block) {
            foreach (var x in holos) {
                if (x.entity == null) x.Spawn ();
            }
        }

        public void Close () {
            foreach (var x in holos) {
                x.DestroyEntity ();
            }
        }
    }
}
