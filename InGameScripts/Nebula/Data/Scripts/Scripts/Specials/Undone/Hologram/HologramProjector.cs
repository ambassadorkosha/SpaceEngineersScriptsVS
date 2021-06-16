using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.Game.ModAPI;
using Digi;
using Scripts.Specials.Messaging;
using Sandbox.Common.ObjectBuilders;
using Scripts.Specials.Hologram.Data;

namespace Scripts.Specials.Hologram {


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] { "HologramProjector" })]
    public class HologramProjector : MyGameLogicComponent {
        private IMyTerminalBlock projector;
        private Dictionary<IMySlimBlock, List<HoloGroup>> toPreview = new Dictionary<IMySlimBlock, List<HoloGroup>>();
        private bool ignoreCustom = false;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);

            if (MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated) return;

            projector = Entity as IMyTerminalBlock;
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            
            projector.CustomDataChanged += Projector_CustomDataChanged;
        }

        public override void UpdateOnceBeforeFrame() {
            base.UpdateOnceBeforeFrame();

            parseApply (projector.CustomData);
        }

        public void Add (IMySlimBlock block, HoloGroup pars) {
            if (!toPreview.ContainsKey(block)) {
                toPreview.Add(block, new List<HoloGroup>());
            }

            toPreview[block].Add (pars);
        }

        public void Clear (IMySlimBlock block) {
            if (toPreview.ContainsKey(block)) {
                foreach (var x in toPreview[block]) {
                    x.Close();
                }
                toPreview[block].Clear();
            }
        }

        public void Save () {
            var data = new List<HoloGroup>();

            foreach ( var x in toPreview) {
                foreach (var y in x.Value) {
                    data.Add (y);
                }
            }

            var xml = MyAPIGateway.Utilities.SerializeToXML(data);
            ignoreCustom = true;
            projector.CustomData = xml;
            ignoreCustom = false;
            Common.SendChatMessage ("SavedCustom");
        }

        private void Projector_CustomDataChanged(IMyTerminalBlock obj) {
            if (ignoreCustom) {
                return;
            }
            parseApply (obj.CustomData);
        }
        public void parseApply (String s) {
            try {
                List<HoloGroup> planer;
                if (s.Length != 0) { 
                    planer = MyAPIGateway.Utilities.SerializeFromXML<List<HoloGroup>>(s);
                } else {
                    planer = new List<HoloGroup>();
                }

                foreach (var x in toPreview) {
                    foreach (var y in x.Value) {
                        y.Close();
                    }
                }

                toPreview.Clear();

                foreach (var x in planer) {
                    var target = x.target.Find(this);
                    if (target != null) {
                        Add (target, x);
                    } else {
                        Common.SendChatMessage ("Target not found");
                    }
                }
                
            } catch (Exception e) {
                Log.Error (e);
            }
        }



        

        

        public override void UpdateAfterSimulation() {
            base.UpdateAfterSimulation();

            foreach (var x in toPreview) {
                var slim = x.Key;
                var fat = x.Key.FatBlock;

                if (slim.CubeGrid != null) {
                    foreach (var y in x.Value) {
                        y.Apply (fat, Vector3.Zero);
                    }
                } else {
                    foreach (var y in x.Value) {
                        y.Close ();
                    }
                }
            }
        }
    }
}