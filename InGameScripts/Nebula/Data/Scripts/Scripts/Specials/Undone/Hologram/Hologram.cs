using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Game.ModAPI;
using Digi;
using Scripts.Specials.Messaging;
using ServerMod;
using Sandbox.Common.ObjectBuilders;
using VRage.Utils;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Shared;
using Scripts.Specials.Hologram.Data;

namespace Scripts.Specials.Hologram {


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] { "Hologram" })]
    public class Hologram : MyGameLogicComponent, Action1<long> {
        private static bool initControls = false;
        private static Func<IMyTerminalBlock, bool> showControl = (b)=>b.BlockDefinition.SubtypeId.Equals ("Hologram");

        IMyTerminalBlock projector;
        public HoloParams decorated = new HoloParams ();
        public HoloParams current = new HoloParams ();
        public List<HoloParams> all = new List<HoloParams>() { };

        long lastChanged = long.MaxValue;
        long lastSended = long.MinValue;

        bool ignoreCustom = false;

        private Vector3 offsetPoint = new Vector3 (10,0,0);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);

            if (MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated) return;

            projector = Entity as IMyTerminalBlock;
           
            all.Add (current);

            if (!initControls) {
                CreateControls ();
                initControls = true;
            }

            parseApply (projector.CustomData);
            projector.CustomDataChanged += Projector_CustomDataChanged;
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

            FrameExecutor.addFrameLogic (this);
            projector.OnMarkForClose += Projector_OnMarkForClose;
        }

        private void Projector_OnMarkForClose(IMyEntity obj) {
            FrameExecutor.removeFrameLogic (this);
        }

        public override void UpdateAfterSimulation() {
            base.UpdateAfterSimulation();
            var p = projector.GetPosition() + projector.WorldMatrix.Forward * 10;

            MyTransparentGeometry.AddPointBillboard(MyStringId.GetOrCompute("Square"), Color.Blue * 0.5f, p, 0.1f, 0f);

            decorated.Apply (projector, offsetPoint);

            foreach (var x in all) {
                x.Apply (projector, offsetPoint);
            }
        }
        
        public void Changed () {
            lastChanged = SharpUtils.msTimeStamp();
        }
        private void Projector_CustomDataChanged(IMyTerminalBlock obj) {
            if (ignoreCustom) {
                return;
            }
            parseApply (obj.CustomData);
        }
        public void parseApply (String s) {
            try {
                HoloPlaner planer;
                if (s.Length != 0) { 
                    planer = MyAPIGateway.Utilities.SerializeFromXML<HoloPlaner>(s);
                } else {
                    planer = new HoloPlaner();
                }
                
                decorated.SetExactModel (planer.model);
                foreach (var x in all) { x.DestroyEntity(); }
                all.Clear();
                foreach (var x in planer.holos) { all.Add(x); }

                Common.SendChatMessage ("AppliedCustom");

            } catch (Exception e) {
                Log.Error (e);
            }
        }

        public void run(long k) {
            if (lastChanged > lastSended && SharpUtils.msTimeStamp() - lastChanged > 1000) {
                lastSended = SharpUtils.msTimeStamp();

                var planer = new HoloPlaner();
                planer.model = decorated.info.model; 
                planer.holos = all; 

                var xml = MyAPIGateway.Utilities.SerializeToXML(planer);
                ignoreCustom = true;
                projector.CustomData = xml;
                ignoreCustom = false;
            }
        }

        public void DecoratedModelUpdated (String newOne) { decorated.SetExactModel(newOne); Changed(); }
        public void CurrentModelUpdated (String newOne) { current.SetExactModel(newOne); Changed(); }

        public static void CreateControls () {
            var XControl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyUpgradeModule>("Hologram_Name1");
            var XControl2 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyUpgradeModule>("Hologram_Name2");
            var XControl3 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyUpgradeModule>("Hologram_Name3");
            var XControl4 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>("Hologram_Name4");
            var XControl5 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>("Hologram_Name5");

            var slider1 = createSlider ("X", -15, 15, x=>x.current.info.offsetX, (x,y)=>y.Append(x.current.info.offsetX), (x,y)=>{ x.current.info.offsetX = y; x.Changed(); });
            var slider2 = createSlider ("Y", -15, 15, x=>x.current.info.offsetY, (x,y)=>y.Append(x.current.info.offsetY), (x,y)=>{ x.current.info.offsetY = y; x.Changed(); });
            var slider3 = createSlider ("Z", -15, 15, x=>x.current.info.offsetZ, (x,y)=>y.Append(x.current.info.offsetZ), (x,y)=>{ x.current.info.offsetZ = y; x.Changed(); });
            var slider4 = createSlider ("RX", -180, 180, x=>x.current.info.rotX, (x,y)=>y.Append(x.current.info.rotX), (x,y)=>{ x.current.info.rotX = y; x.Changed(); });
            var slider5 = createSlider ("RY", -180, 180, x=>x.current.info.rotY, (x,y)=>y.Append(x.current.info.rotY), (x,y)=>{ x.current.info.rotY = y; x.Changed(); });
            var slider6 = createSlider ("RZ", -180, 180, x=>x.current.info.rotZ, (x,y)=>y.Append(x.current.info.rotZ), (x,y)=>{ x.current.info.rotZ = y; x.Changed(); });


            var allControlls = new List<IMyTerminalControl>();

            allControlls.Add (XControl);
            allControlls.Add (XControl2);
            allControlls.Add (XControl3);
            allControlls.Add (XControl4);
            allControlls.Add (slider1);
            allControlls.Add (slider2);
            allControlls.Add (slider3);
            allControlls.Add (slider4);
            allControlls.Add (slider5);
            allControlls.Add (slider6);
             
             XControl.Title = MyStringId.GetOrCompute("Target Model");
             XControl.Tooltip = MyStringId.GetOrCompute("Model that you want to decorate");
             XControl.Enabled = showControl;
             XControl.Visible = showControl;

             XControl.Getter = (b) => new StringBuilder().Append(b.GameLogic.GetAs<Hologram>().decorated.info.model);
             //XControl.Writer = (b, t) => t.Append("ZZZ");
             XControl.Setter = (b, v) => b.GameLogic.GetAs<Hologram>().DecoratedModelUpdated (v.ToString());
             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(XControl);

             
            
             XControl2.Title = MyStringId.GetOrCompute("Decorated with Model");
             XControl2.Tooltip = MyStringId.GetOrCompute("Model that you want to decorate");
             XControl2.Enabled = showControl;
             XControl2.Visible = showControl;

             XControl2.Getter = (b) => new StringBuilder().Append(b.GameLogic.GetAs<Hologram>().current.info.model);
             //XControl.Writer = (b, t) => t.Append("ZZZ");
             XControl2.Setter = (b, v) => b.GameLogic.GetAs<Hologram>().CurrentModelUpdated (v.ToString());
             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(XControl2);

             
             XControl3.Title = MyStringId.GetOrCompute("Decorations");
             XControl3.Tooltip = MyStringId.GetOrCompute("Model that you want to decorate");
             XControl3.Enabled = showControl;
             XControl3.Visible = showControl;

             XControl3.Multiselect = false;
             XControl3.VisibleRowsCount = 10;

             XControl3.ListContent = (x, l, l2) => {
                 var h = x.GameLogic.GetAs<Hologram>();
                 foreach (var y in h.all) {
                     l.Add (new MyTerminalControlListBoxItem (MyStringId.GetOrCompute (y.info.model), MyStringId.GetOrCompute (y.info.model), y));
                 }
             };
             XControl3.ItemSelected = (x, l) => {
                 var h = x.GameLogic.GetAs<Hologram>();
                 h.current = l.First ().UserData as HoloParams;

                 slider1.UpdateVisual();
                 slider2.UpdateVisual();
                 slider3.UpdateVisual();
                 slider4.UpdateVisual();
                 slider5.UpdateVisual();
                 slider6.UpdateVisual();
             };
             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(XControl3);


             
             XControl4.Title = MyStringId.GetOrCompute("Add");
             XControl4.Tooltip = MyStringId.GetOrCompute("Model that you want to decorate");
             XControl4.Enabled = showControl;
             XControl4.Visible = showControl;
             XControl4.Action  = (b) => {
                 var h = b.GameLogic.GetAs<Hologram>();
                 var current = new HoloParams();
                 h.all.Add (current);

                 XControl3.UpdateVisual();
                 slider1.UpdateVisual();
                 slider2.UpdateVisual();
                 slider3.UpdateVisual();
                 slider4.UpdateVisual();
                 slider5.UpdateVisual();
                 slider6.UpdateVisual();
             };

             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(XControl4);

             XControl5.Title = MyStringId.GetOrCompute("Remove");
             XControl5.Tooltip = MyStringId.GetOrCompute("Model that you want to decorate");
             XControl5.Enabled = showControl;
             XControl5.Visible = showControl;
             XControl5.Action  = (b) => {
                 var h = b.GameLogic.GetAs<Hologram>();
                 h.all.Remove (h.current);

                 XControl3.UpdateVisual();
                 slider1.UpdateVisual();
                 slider2.UpdateVisual();
                 slider3.UpdateVisual();
                 slider4.UpdateVisual();
                 slider5.UpdateVisual();
                 slider6.UpdateVisual();
             };

             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(XControl5);
             

             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(slider1);
             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(slider2);
             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(slider3);
             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(slider4);
             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(slider5);
             MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(slider6);
        }

         public static IMyTerminalControlSlider createSlider (String id, float min, float max, Func<Hologram, float> getter, Action<Hologram, StringBuilder> writer, Action<Hologram, float> setter) {
             var XControl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>("Hologram_"+id);
             XControl.Title = MyStringId.GetOrCompute(id);
             XControl.Tooltip = MyStringId.GetOrCompute(id);
             XControl.SetLimits(min, max);
             XControl.Enabled = showControl;
             XControl.Visible = showControl;

             XControl.Getter = (b) => getter.Invoke (b.GameLogic.GetAs<Hologram>());
             XControl.Writer = (b, t) => writer.Invoke (b.GameLogic.GetAs<Hologram>(), t);
             XControl.Setter = (b, v) => setter.Invoke (b.GameLogic.GetAs<Hologram>(), v);

            return XControl;
        }

    }
}


///var trans = block.WorldMatrix.Translation;
///var d = new Vector3(x,y,z);
///d+=block.WorldMatrix.Translation;
///
///
///
///var rotated = block.WorldMatrix * Matrix.CreateRotationX(rx) * Matrix.CreateRotationX(ry) * Matrix.CreateRotationX(rz);
///rotated *= Matrix.CreateTranslation(rotated.Translation-d);
///
///ent.WorldMatrix = rotated; //block.WorldMatrix * Matrix.CreateRotationX(rx) * Matrix.CreateRotationX(ry) * Matrix.CreateRotationX(rz) * Matrix.CreateTranslation (x, y, z) ;
///
///
///
///
///ent.GameLogic = new DzenBalls(character);
///ent.GameLogic.NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
///ent.NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
///ent.Flags |= EntityFlags.NeedsUpdate;
///ent.Render.ColorMaskHsv = block.Render.ColorMaskHsv;
///
///Common.SendChatMessage ("Test:" + ent.WorldMatrix.Translation);
/////foreach (var xx in block.Render.TextureChanges) {
/////    Common.SendChatMessage (xx.Key + " -> " +xx.Value);
/////}
///
///ent.Render.TextureChanges = block.Render.TextureChanges;
///ent.Render.MetalnessColorable = block.Render.MetalnessColorable;
///
/////ent.Render.TextureChanges = 
/////ent.Render.MetalnessColorable
/////ent.Render.ColorMaskHsv 
///
///MyGameLogic.RegisterForUpdate (ent.GameLogic);