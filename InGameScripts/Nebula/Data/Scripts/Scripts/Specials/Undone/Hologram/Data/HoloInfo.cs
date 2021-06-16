using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Specials.Hologram.Data {
    public class HoloInfo {
        public float offsetX = 0f, offsetY = 0f, offsetZ = 0f;
        public float rotX = 0f, rotY = 0f, rotZ  = 0f;
        public float scale = 1f;
        public String model = "GeneratorSmall.mwm";

        public HoloInfo() { }

        public HoloInfo Copy () {
            var p = new HoloInfo();

            p.offsetX = offsetX;
            p.offsetY = offsetY;
            p.offsetZ = offsetZ;
            p.rotX = rotX;
            p.rotY = rotY;
            p.rotZ = rotZ;
            p.scale = scale;
            p.model = model;

            return p;
        }
    }
}
