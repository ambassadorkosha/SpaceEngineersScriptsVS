using System;
using System.Collections.Generic;
using Digi;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using DrawSprites;
using System.Text;
using VRage.ModAPI;
using VRage.Utils;
using Scripts.Shared.GUI;

namespace Scripts.Specials
{
	//AUTHOR: Stridemann
	[MyTextSurfaceScriptAttribute("LCDGUI", "GUI test")]
	public class GUITest : MyTSSCommon
	{
		private readonly IMyCubeBlock _cubeBlock;
		private readonly IMyTextSurface _lcd;
		private readonly Vector2 _screenSize;

		public GUIBase GB;
		public CButton TestButton;
		public CStaticText TestTitle;

		public GUITest(IMyTextSurface surface, IMyCubeBlock block, Vector2 screenSize) : base(surface, block, screenSize)
		{
			_screenSize = screenSize;
			_cubeBlock = block;
			_lcd = surface;
			GB = new GUIBase();

			TestButton = new CButton(new RectangleF(20.0f, 70.0f, 200.0f, 30.0f), "Типа кнопка", m_foregroundColor);
			TestButton.Text.TextSize = SpriteGI.GetTextHeightForString(TestButton.Text.Title, TestButton.Bounding.Width - 10.0f);

			TestTitle = new CStaticText(new RectangleF(20.0f, 20.0f, 200.0f, 30.0f), "просто надпись", m_foregroundColor);

			/*CModalWindow Window = new CModalWindow(new RectangleF(20.0f, 150.0f, 300.0f, 200.0f), "Окно");
			CButton WButton = new CButton(new RectangleF(50.0f, 100.0f, 200.0f, 40.0f), "Закрой меня");
			WButton.Text.TextSize = SpriteGI.GetTextHeightForString(WButton.Text.Title, WButton.Bounding.Width + 20.0f);
			Window.AddChild(WButton);*/

			CDropDownList DropDownList = new CDropDownList(new RectangleF(20.0f, 150.0f, 200.0f, 20.0f), "Окно", m_foregroundColor);
			DropDownList.AddItem("1. item");
			DropDownList.AddItem("2. item");
			DropDownList.AddItem("3. item");
			DropDownList.AddItem("4. item");
			DropDownList.AddItem("5. item");

			CProgressBar ProgressBar = new CProgressBar(new RectangleF(20.0f, 190.0f, 200.0f, 20.0f), "", m_foregroundColor);
			ProgressBar.Percent = 24.5f;


			CCheckBox CB = new CCheckBox(new RectangleF(20.0f, 230.0f, 200.0f, 20.0f), "Чек бокс", m_foregroundColor);
			
			CRadioButton RB1 = new CRadioButton(new RectangleF(300.0f, 150.0f, 200.0f, 20.0f), "Радиопочка 1", m_foregroundColor);
			CRadioButton RB2 = new CRadioButton(new RectangleF(300.0f, 190.0f, 200.0f, 20.0f), "Радиопочка 2", m_foregroundColor);
			CRadioButton RB3 = new CRadioButton(new RectangleF(300.0f, 230.0f, 200.0f, 20.0f), "Радиопочка 3", m_foregroundColor);

			GB.AddControl(TestButton);
			GB.AddControl(TestTitle);
			GB.AddControl(DropDownList);
			GB.AddControl(ProgressBar);
			GB.AddControl(CB);
			GB.AddControl(RB1);
			GB.AddControl(RB2);
			GB.AddControl(RB3);
			TestTitle.Text.Title = DropDownList.DropList.Bounding.ToString();
		}

		public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        

		public override void Run()
		{
			base.Run();
			if (m_block == null) return;
			if (MyAPIGateway.Session == null) return;
			if (MyAPIGateway.Session.Camera == null) return;
			if (Vector3D.Distance(m_block.GetPosition(), MyAPIGateway.Session.Camera.Position) > (m_block.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Small ? 5.0 : 15.0)) return;

            GB.DrawInto (this, m_size);
        }
		
	}

	public class GPSCoordinates
	{
		public String Name;
		public Vector3D Coordinates;
		public Color HUDColor = Color.Blue;
		public GPSCoordinates(String Source)
		{
			Name = "";
			Coordinates = Vector3D.Zero;
			TryParse(Source);
		}
		public GPSCoordinates()
		{
			Name = "";
			Coordinates = Vector3D.Zero;
		}
		public GPSCoordinates(Vector3D InPosition, string InName)
		{
			Name = InName;
			Coordinates = InPosition;
		}
		public bool TryParse(String Source)
		{
			string[] contents = Source.Split(':');
			bool result = false;
			if (contents.Length >= 6 && contents[0] == "GPS")
			{
				Coordinates = Vector3D.Zero;
				Name = contents[1];
				double.TryParse(contents[2], out Coordinates.X);
				double.TryParse(contents[3], out Coordinates.Y);
				double.TryParse(contents[4], out Coordinates.Z);
				result = true;
			}
			return result;
		}
		public override String ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("GPS:");
			sb.Append(Name);
			sb.Append(":");
			sb.Append(Coordinates.X.ToString());
			sb.Append(":");
			sb.Append(Coordinates.Y.ToString());
			sb.Append(":");
			sb.Append(Coordinates.Z.ToString());
			sb.Append(":#FF75C9F1:");
			return sb.ToString();
		}
		public static string GetGPSString(string Name, Vector3D Val)//GPS:CM:156990.32:231608.61:-262863.68:#FF75C9F1:
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("GPS:");
			sb.Append(Name);
			sb.Append(":");
			sb.Append(Val.X.ToString());
			sb.Append(":");
			sb.Append(Val.Y.ToString());
			sb.Append(":");
			sb.Append(Val.Z.ToString());
			sb.Append(":#FF75C9F1:");
			return sb.ToString();
		}
		public static GPSCoordinates Create(String Source)
		{
			string[] contents = Source.Split(':');
			if (contents.Length >= 6 && contents[0] == "GPS")
			{
				Vector3D Coordinates = Vector3D.Zero;
				string Name = contents[1];
				if (!double.TryParse(contents[2], out Coordinates.X))
					return null;
				if (!double.TryParse(contents[3], out Coordinates.Y))
					return null;
				if (!double.TryParse(contents[4], out Coordinates.Z))
					return null;

				return new GPSCoordinates(Coordinates, Name);
			}
			return null;
		}
	}
}