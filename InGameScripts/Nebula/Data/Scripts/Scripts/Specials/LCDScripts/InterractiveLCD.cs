using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using Digi;
using Sandbox.Definitions;
using Scripts.Shared;
using Slime;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Specials.LCDScripts {
    //[MyTextSurfaceScript("InterractiveLCD", "InterractiveLCD")]
    public class InterractiveImpl : InterractiveLCD {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;
        public InterractiveImpl (IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size) {
            
        }
        
        List<Vector2> clicks = new List<Vector2>();
        public override void Run() {
            
            using (MySpriteDrawFrame frame = m_surface.DrawFrame()) {
                var sprite3 = MySprite.CreateText("X", "Monospace", m_foregroundColor, 2, TextAlignment.CENTER);
                sprite3.Position = new Vector2(m_surface.SurfaceSize.X,0);
                sprite3.Size = new Vector2(20f,20f);
                frame.Add(sprite3);
            }
        }

        public override void Click(Vector3 vector) {
            Log.ChatError("Click" + vector);
            var b = (m_block as IMyCubeBlock).SlimBlock.BlockDefinition as MyCubeBlockDefinition;
            var s = b.Size;

            var m = m_block.WorldMatrix;
            var position = m.Translation;


            var c = position- (m.Backward * 0.45 * 2.5f);

            var w = s.X * 2.5f / 2f;
            var h = s.Y * 2.5f / 2f;
            PhysicsHelper.Draw(Color.Red, c, m.Backward*5);
            PhysicsHelper.Draw(Color.Green, c+w*m.Left, m.Backward*5);
            PhysicsHelper.Draw(Color.Blue, c+h*m.Up, m.Backward*5);
        }
    }
    
    
    public abstract class InterractiveLCD : MyTSSCommon {
        public static Dictionary<long, InterractiveLCD> scripts = new Dictionary<long, InterractiveLCD>();

        public static void Click(Vector3 vector, IMyCubeBlock block) {
            if (!scripts.ContainsKey(block.EntityId)) {
                return;
            }

            var script = scripts[block.EntityId];
            script.Click(vector);
        }
        
        public InterractiveLCD(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size) {
            scripts.Add(block.EntityId, this);
        }

        public override void Dispose() {
            base.Dispose();
            scripts.Remove(m_block.EntityId);
        }

        public abstract void Click(Vector3 vector);
    }
}