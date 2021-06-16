using Digi;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials.Hologram.Data {
    public class HoloParams {
            public HoloInfo info = new HoloInfo();
            [XmlIgnoreAttribute]
            public Vector3D offsetBuffer = new Vector3D ();//TODO!!!

            [XmlIgnoreAttribute]
            public MyEntity entity;
            public HoloParams() { }

            public MyEntity Spawn () {
                try {
                    var ent = new MyEntity();
                    var displayName = new StringBuilder().Append ("MyAPIGateway"); 
                    ent.Init (displayName, info.model, null, info.scale, null);
                    MyAPIGateway.Entities.AddEntity(ent);

                    //var turretsub = (Entity as MyEntity).Subparts["MissileTurretBase1"]?.Subparts["MissileTurretBarrels"];
                    //Sandbox.Engine.Physics.MyPhysicsHelper.InitModelPhysics((Entity as MyEntity).Subparts["MissileTurretBase1"]);
                    //var r = Sandbox.Engine.Physics.MyPhysicsHelper.InitModelPhysics(ent);
     
                    entity = ent;

                    entity.InitComponents();

                    //Sandbox.Engine.Physics.MyPhysicsHelper.InitModelPhysics(entity, VRage.Game.Components.RigidBodyFlag.RBF_STATIC);
                    //entity.Physics.
                    
                    //entity.Physics.BreakableBody.
                    //Sandbox.Engine.Physics.MyPhysicsHelper.
                    //Sandbox.Engine.Physics.MyPhysicsHelper.InitModelPhysics(turretsub);

                    return ent;
                } catch (Exception e) {
                    Log.Error(e);
                    return null;
                }
            }


            public void Apply (IMyCubeBlock baseBlock, Vector3 offset) {
                if (entity == null) { Spawn (); }
                
                var toPi = Math.PI / 180;

                if (baseBlock == null) {
                    Log.Error("baseBlock == null");
                    return;
                }
                if (baseBlock.WorldMatrix == null) {
                    Log.Error("World matrix == null");
                    return;
                }

                var m = baseBlock.WorldMatrix;

                entity.WorldMatrix = m * MatrixD.CreateTranslation (-m.Translation) *
                    MatrixD.CreateFromAxisAngle(m.Forward, (float) (toPi * info.rotX)) *
                    MatrixD.CreateFromAxisAngle(m.Right, (float) (toPi * info.rotY)) *
                    MatrixD.CreateFromAxisAngle(m.Up, (float) (toPi * info.rotZ)) 
                    
                    * MatrixD.CreateTranslation (m.Translation) 
                
                    * MatrixD.CreateTranslation (m.Forward * (offset.X + info.offsetX))
                    * MatrixD.CreateTranslation (m.Right * (offset.Y + info.offsetY))
                    * MatrixD.CreateTranslation (m.Up * (offset.Z + info.offsetZ));
                
            }


        internal void DestroyEntity() {
            if (entity != null) {
                entity.Close();
                entity = null;
            }
        }

        internal HoloParams Copy() {
            var p = new HoloParams ();
            p.info = info.Copy();
            return p;
        }

        public void SetModel(string model) {
            if (this.info.model != model) {
                DestroyEntity();
                this.info.model = "Models\\Cubes\\Large\\"+model;
            }
        }

        public void SetExactModel(string model) {
            if (this.info.model != model) {
                DestroyEntity();
                this.info.model = model;
            }
        }
    }
}
