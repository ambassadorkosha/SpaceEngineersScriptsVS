using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using Slime;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials {

    [MyTextSurfaceScriptAttribute ("DateClock", "DateClock")]
    class DateClockLCDScript : MyTSSCommon {
        private IMyCubeBlock block;
        public DateClockLCDScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size) { this.block = block; }
        public override void Run() {
            base.Run();

            using (MySpriteDrawFrame frame = m_surface.DrawFrame()) {
                var now  =DateTime.Now;
                var fline = now.ToString("dd MMMM ") + (now.Year + 60);
                var sline = now.ToString("HH:mm:ss");

                var sprite2 = MySprite.CreateText(fline, "Monospace", m_foregroundColor, 1.4f, TextAlignment.CENTER);
			    sprite2.Position = new Vector2(256,256-40f + 80);
			    sprite2.Size = new Vector2(512f,80f);

                var sprite3 = MySprite.CreateText(sline, "Monospace", m_foregroundColor, 2, TextAlignment.CENTER);
			    sprite3.Position = new Vector2(256,256-40f - 80);
			    sprite3.Size = new Vector2(512f,80f);

			    frame.Add(sprite2);
			    frame.Add(sprite3);
            }
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;
    }
}
