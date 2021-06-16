using Digi;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Scripts.Specials {

    /*[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipConnector), true, new string[] { "Connector", "ConnectorMedium" })]
    class EasyConnector : MyGameLogicComponent{
        private IMyFunctionalBlock myBlock;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            myBlock = (Entity as IMyFunctionalBlock);
            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation() {
            base.UpdateAfterSimulation();

           
            var p = myBlock.CubeGrid.Physics;
            if (p == null || p.IsStatic) return;

            var a = p.LinearAcceleration;
            var v = p.LinearVelocity;

            var ax = a.X;
            var ay = a.Y;
            var az = a.Z;

            var vx = v.X;
            var vy = v.Y;
            var vz = v.Z;

            var tx = vx / ax;
            var ty = vy / ay;
            var tz = vz / az;

           



            


            if (tx < 0 && ty < 0 && tz <0) {
                var sx =(ax*tx*tx / 2);// vx*tx + 
                var sy =(ay*ty*ty / 2);// vy*ty + 
                var sz =(az*tz*tz / 2);// vz*tz + pp

                Draw (Color.Red, myBlock.WorldMatrix.Translation+myBlock.WorldMatrix.Forward*0.4, -new Vector3D (sx, sy, sz));
            }

            

            


             //Log.Error (a + "\n" + vx + "\n" + ax +"\n" +tx+ "\n" + sx);
             //Log.Error (tx + "\n" + ty + "\n" + tz);

           

        }

        public static void Draw (Color c, Vector3D start, Vector3D vec, float thick = 0.05f) {
            MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("Square"), c*0.75f, start, vec, 1, thick);
        }
    }*/
}
