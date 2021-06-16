using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials
{
	[MyTextSurfaceScriptAttribute("LCDTest", "LCDTest")]
	class TestingLCDScript : MyTSSCommon
	{
		MySprite SpriteSquareHollow;

		bool NoText;
		float Rotation;
		public TestingLCDScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
		{
			NoText = false;
			Rotation = 0;
			SpriteSquareHollow = new MySprite(SpriteType.TEXTURE, "SquareHollow", new Vector2(250, 250), new Vector2(100f, 100f), new Color(0, 255, 255, 255), "Monospace", TextAlignment.LEFT);
			this.m_surface.ScriptBackgroundColor = new Color(0, 0, 0, 129);
			this.m_surface.ScriptForegroundColor = new Color(128, 255, 128);
		}
		public override void Run()
		{
			base.Run();

			if (NoText)
				this.m_surface.ContentType = ContentType.SCRIPT;
			else
			{
				this.m_surface.ContentType = ContentType.NONE;
				using (MySpriteDrawFrame frame = this.m_surface.DrawFrame())
				{
					SpriteSquareHollow.RotationOrScale = Rotation;
					Rotation += 0.1f;
					frame.Add(SpriteSquareHollow);
				}
				this.m_surface.ContentType = ContentType.SCRIPT;
			}
			NoText = !NoText;
		}

		public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;
	}
}
