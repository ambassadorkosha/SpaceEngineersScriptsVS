using System;
using System.Collections.Generic;
using System.Linq;
using Digi;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using DrawSprites;
using VRage.ModAPI;
using VRage.Input;
using Rectangle = VRageMath.Rectangle;
using RectangleF = VRageMath.RectangleF;

namespace Scripts.Shared.GUI
{
    public static class LCDGuiHelper {
        public static bool ExtractCameraOriginAndDirection(out Vector3D OutOrigin, out Vector3D OutDirection)
        {
            OutOrigin = Vector3D.NegativeInfinity;
            OutDirection = Vector3D.NegativeInfinity;
            if (MyAPIGateway.Session == null) return false;
            if (MyAPIGateway.Session.Camera == null) return false;
            IMyCamera Cam = MyAPIGateway.Session.Camera;
            OutOrigin = Cam.Position;
            OutDirection = Cam.WorldMatrix.Forward;
            return true;
        }
        public static Vector2 ProjectRay(MatrixD InWorldMatrix, BoundingBox InLocalAABB, Vector3D InRayDirection, Vector3D InRayOrigin, Vector2 InSurfaceSize, float InGridSize)
        {
            Vector3D Translation = InWorldMatrix.Translation;
            MatrixD CameraToBody = MatrixD.Transpose(InWorldMatrix);
            Vector3D BodyRayDirection = Vector3D.TransformNormal(InRayDirection, CameraToBody);
            Vector3D BodyRayOrigin = Vector3D.TransformNormal(InRayOrigin - Translation, CameraToBody);
            double DistanceToSurface = -BodyRayOrigin.Z + InLocalAABB.Depth - InGridSize * 0.5;
            Vector3D IntersectionWorld = BodyRayOrigin;
            if (BodyRayOrigin.Z != 0) IntersectionWorld = BodyRayDirection / BodyRayDirection.Z * DistanceToSurface + BodyRayOrigin;

            return new Vector2(
                (float)((IntersectionWorld.X + InLocalAABB.Width * 0.5) / InLocalAABB.Width * InSurfaceSize.X),
                (float)((InLocalAABB.Height * 0.5 - IntersectionWorld.Y) / InLocalAABB.Height * InSurfaceSize.Y));
        }

        public static bool isRayOnScreen(this Vector2 Ray, Vector2 InScreen)
        {
	        if ((double)Ray.X < 0 || (double)Ray.Y < 0) return false;
	        if ((double)Ray.X > InScreen.X || (double)Ray.Y > InScreen.Y) return false;
	        return true;
        }
	}

	public class GUIBase
	{
		public enum ButtonState
		{
			bs_none,
			bs_hover,
			bs_down
		}

		public HashSet<Control> Children = new HashSet<Control>();
		public HashSet<Control> ChildrenModal = new HashSet<Control>();
		public HashSet<CRadioButton> ChildRadioButtons = new HashSet<CRadioButton>();

		bool ButtonPressed = false;
		public Control PreviousControl = null;

		public GUIBase()
		{

		}

		public void AddControl(Control InNewControl)
		{
			try
			{
				CDropDownList DDL = InNewControl as CDropDownList;
				if (DDL != null)
				{
					AddControl(DDL.DropList);
				}

				CRadioButton RB = InNewControl as CRadioButton;
				if (RB != null)
				{
					RB.SameLevelRadioButtons = ChildRadioButtons;
					ChildRadioButtons.Add(RB);
				}

				if (InNewControl is CModalControl)
					ChildrenModal.Add(InNewControl as CModalControl);
				else
					Children.Add(InNewControl);
			}
			catch
			{
			}
		}

		public void RemoveControl(Control InControl)
		{
			try
			{
				if (InControl is CModalControl)
					ChildrenModal.Remove(InControl as CModalControl);
				else
					Children.Remove(InControl);
			}
			catch
			{
			}
		}

		public void ClearControls()
		{
			ChildrenModal.Clear();
			Children.Clear();
		}


		public void DrawInto(MyTextSurfaceScriptBase lcdScript, Vector2 screenSize)
		{
			Vector3D Origin;
			Vector3D Direction;
			var block = ((IMyCubeBlock) lcdScript.Block);
			if (LCDGuiHelper.ExtractCameraOriginAndDirection(out Origin, out Direction))
			{
				Vector2 ProjectedPoint = LCDGuiHelper.ProjectRay(block.WorldMatrix, block.LocalAABB, Direction, Origin, screenSize, lcdScript.Block.CubeGrid.GridSize);
				
				if (ProjectedPoint.Between(ref Vector2.Zero, ref screenSize)) OnMouseMove(ProjectedPoint);
				using (var frame = lcdScript.Surface.DrawFrame())
				{
					Draw(frame);
					if (ProjectedPoint.isRayOnScreen(screenSize))
					{
						SpriteGI.DrawVerticalLine(frame, ProjectedPoint.X, ProjectedPoint.Y - 5.0f, ProjectedPoint.Y + 5.0f, 1.0f, Color.Red);
						SpriteGI.DrawHorizontalLine(frame, ProjectedPoint.X - 5.0f, ProjectedPoint.X + 5.0f, ProjectedPoint.Y, 1.0f, Color.Red);
					}
				}
			}
		}

		public void Draw(MySpriteDrawFrame InFrame)
		{
			foreach (Control C in Children)
				if (C.IsVisible)
					C.DrawChildren(InFrame, Vector2.Zero);

			foreach (Control C in ChildrenModal)
				if (C.IsVisible)
					C.DrawChildren(InFrame, Vector2.Zero);
		}

		bool OnMouseMoverInternal(Vector2 InCoord, ButtonState InButtonState, HashSet<Control> InControls)
		{
			foreach (Control C in InControls)
			{
				Control Activated = C.FindControlByCoordinates(InCoord);
				if (Activated != null && !Activated.IsVisible) continue;

				if (PreviousControl != Activated)
				{
					if (PreviousControl != null)
						PreviousControl.OnControlLeftByCursor(InCoord);
					PreviousControl = Activated;
				}

				if (Activated != null)
				{
					switch (InButtonState)
					{
						case ButtonState.bs_none:
							Activated.OnButtonUp(InCoord);
							break;
						case ButtonState.bs_hover:
							Activated.OnHover(InCoord);
							break;
						case ButtonState.bs_down:
							Activated.OnButtonDown(InCoord);
							break;
					}

					return true;
				}
			}

			return false;
		}

		public void OnMouseMove(Vector2 InCoord)
		{
			ButtonState BState = ButtonState.bs_hover;
			bool LButton = MyAPIGateway.Input.IsMousePressed(MyMouseButtonsEnum.Middle);
			if (ButtonPressed && !LButton)
				BState = ButtonState.bs_none;
			if (!ButtonPressed && LButton)
				BState = ButtonState.bs_down;

			ButtonPressed = LButton;
			if (OnMouseMoverInternal(InCoord, BState, ChildrenModal)) return;
			if (OnMouseMoverInternal(InCoord, BState, Children)) return;
		}

		public void ShowInGrid<T>(List<T> data, Func<int, RectangleF, T, Control> createDelegate, int cols,
			Rectangle fit, int rowHeight = int.MinValue, int columnInterval = 0, int rowOffset = 0)
		{

			int ox = fit.X;
			int oy = fit.Y;
			int w = (fit.Width - (cols - 1) * columnInterval) / cols;

			if (rowHeight == int.MinValue)
			{
				rowHeight = w;
			}

			int i = 0;
			foreach (var t in data)
			{
				var x = i % cols;
				var y = i / cols;
				var control = createDelegate(i,
					new RectangleF(ox + x * (w + columnInterval), oy + y * (rowHeight + rowOffset), w, rowHeight), t);
				if (control != null)
				{
					AddControl(control);
				}

				i++;
			}
		}
	}

	public abstract class Control
	{
		public Control Parent { get; private set; }

		List<Control> Children = new List<Control>();
		HashSet<CRadioButton> ChildRadioButtons = new HashSet<CRadioButton>();

		public RectangleF Bounding;
		public Color BorderColor = new Color(128, 255, 128);
		public Color FillColor = new Color(0, 0, 0);
		public bool IsVisible = true;

		public object UserData;

		public Control(RectangleF InBounding)
		{
			Bounding = InBounding;
		}

		public virtual void DrawChildren(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			Draw(InFrame, TopLeft);
			foreach (Control C in Children)
				if (C.IsVisible)
					C.DrawChildren(InFrame, TopLeft + Bounding.Position);
		}

		protected virtual void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			SpriteGI.DrawHollowSquare(InFrame, Bounding.X + TopLeft.X, Bounding.Y + TopLeft.Y, Bounding.Width,
				Bounding.Height, BorderColor);
		}

		public void AddChild(Control InNewControl)
		{
			try
			{
				CRadioButton RB = InNewControl as CRadioButton;
				if (RB != null)
				{
					RB.SameLevelRadioButtons = ChildRadioButtons;
					ChildRadioButtons.Add(RB);
				}

				Children.Add(InNewControl);
				InNewControl.Parent = this;
			}
			catch
			{
			}
		}

		public void RemoveChild(Control InControl)
		{
			try
			{
				Children.Remove(InControl);
				InControl.Parent = null;
			}
			catch
			{
			}
		}

		public Control FindControlByCoordinates(Vector2 InPoint)
		{
			if (Bounding.Contains(InPoint))
			{
				Control ControlUnderCursor;
				foreach (Control C in Children)
				{
					ControlUnderCursor = C.FindControlByCoordinates(InPoint - Bounding.Position);
					if (ControlUnderCursor != null)
						return ControlUnderCursor;
				}

				return this;
			}

			return null;
		}

		public Vector2 GetAbsoluteTopLeftPosition()
		{
			if (Parent == null)
				return Bounding.Position;
			else
				return Parent.GetAbsoluteTopLeftPosition() + Bounding.Position;
		}

		public virtual void OnHover(Vector2 Coord)
		{

		}

		public virtual void OnButtonDown(Vector2 Coord)
		{

		}

		public virtual void OnButtonUp(Vector2 Coord)
		{

		}

		public virtual void OnControlLeftByCursor(Vector2 Coord)
		{

		}
	}

	public class InternalText
	{
		private string _Title = "";
		private float _TextSize = 12.0f;

		public string Title
		{
			get { return _Title; }
			set
			{
				if (value == null)
				{
					Log.ChatError($"Text with: {_Title} will get NULL value");
				}

				_Title = value;
				TextWidth = SpriteGI.MeasureText(Title, _TextSize).X;
			}
		}

		public float TextSize
		{
			get { return _TextSize; }
			set
			{
				_TextSize = value;
				TextWidth = SpriteGI.MeasureText(Title, _TextSize).X;
			}
		}

		public float TextWidth { get; protected set; }

		public void Draw(MySpriteDrawFrame InFrame, RectangleF InBounding, Color InColor)
		{
			float X = (float) Math.Max(InBounding.X + (InBounding.Width - TextWidth) * 0.5, 0.0);
			float Y = (float) Math.Max(InBounding.Y + (InBounding.Height - TextSize) * 0.5, 0.0);
			
			SpriteGI.DrawText(InFrame, _Title, X, Y, InColor, TextSize);
		}
	}

	public abstract class InternalControlWithText : Control
	{
		public InternalText Text = new InternalText();
		public Color TextColor = new Color(128, 255, 128);

		public InternalControlWithText(RectangleF InBounding, string InTitle, Color InTextColor) : base(InBounding)
		{
			Text.Title = InTitle;
			Text.TextSize = Bounding.Height * 0.7f;
			TextColor = InTextColor;
		}
	}

	public class CImage : Control
	{
		public delegate void OnClick(Control control);

		public GUIBase.ButtonState State { get; private set; }
		public bool IsDown { get; private set; }

		public OnClick OnClickDelegate;
		public OnClick OnDownDelegate;
		private string Id;
		private MySprite Sprite;

		public CImage(RectangleF InBounding, string Id, object UserData = null, OnClick OnClick = null) : base(
			InBounding)
		{
			IsDown = false;
			this.Id = Id;
			this.UserData = UserData;
			this.OnClickDelegate = OnClick;
			Sprite = new MySprite(SpriteType.TEXTURE, Id);
		}

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			Color Background = FillColor;
			base.Draw(InFrame, TopLeft);
			Sprite.Position = new Vector2(Bounding.X + Bounding.Width / 2, Bounding.Y + Bounding.Height / 2);
			Sprite.Size = new Vector2(Bounding.Width, Bounding.Height);
			Sprite.RotationOrScale = 0;
			InFrame.Add(Sprite);
		}

		public override void OnHover(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_hover;
		}

		public override void OnButtonDown(Vector2 Coord)
		{
			IsDown = true;
			State = GUIBase.ButtonState.bs_down;
			OnDownDelegate?.Invoke(this);
		}

		public override void OnButtonUp(Vector2 Coord)
		{
			IsDown = false;
			State = GUIBase.ButtonState.bs_hover;
			OnClickDelegate?.Invoke(this);
		}

		public override void OnControlLeftByCursor(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_none;
		}
	}
	
	public class CDoubleImage : Control
	{
		public delegate void OnClick(Control control);

		public GUIBase.ButtonState State { get; private set; }
		public bool IsDown { get; private set; }

		public OnClick OnClickDelegate;
		public OnClick OnDownDelegate;
		private MySprite Sprite1;
		private MySprite Sprite2;

		public CDoubleImage(RectangleF InBounding, string Id1, string Id2, object UserData = null,
			OnClick OnClick = null) : base(InBounding)
		{
			IsDown = false;
			this.UserData = UserData;
			this.OnClickDelegate = OnClick;
			Sprite1 = new MySprite(SpriteType.TEXTURE, Id1);
			Sprite2 = new MySprite(SpriteType.TEXTURE, Id2);
		}

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			var b = BorderColor;
			if (State == GUIBase.ButtonState.bs_hover)
			{
				BorderColor = BorderColor * 0.5f;
			}


			base.Draw(InFrame, TopLeft);


			BorderColor = b;

			Sprite1.Position = new Vector2(Bounding.X + Bounding.Width / 2, Bounding.Y + Bounding.Height / 2);
			Sprite1.Size = new Vector2(Bounding.Width, Bounding.Height);
			Sprite1.RotationOrScale = 0;


			Sprite2.Position = new Vector2(Bounding.X + 3 * Bounding.Width / 4, Bounding.Y + 3 * Bounding.Height / 4);
			Sprite2.Size = new Vector2(Bounding.Width / 2, Bounding.Height / 2);
			Sprite2.RotationOrScale = 0;

			InFrame.Add(Sprite1);
			InFrame.Add(Sprite2);
		}

		public override void OnHover(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_hover;
		}

		public override void OnButtonDown(Vector2 Coord)
		{
			IsDown = true;
			State = GUIBase.ButtonState.bs_down;
			OnDownDelegate?.Invoke(this);
		}

		public override void OnButtonUp(Vector2 Coord)
		{
			IsDown = false;
			State = GUIBase.ButtonState.bs_hover;
			OnClickDelegate?.Invoke(this);
		}

		public override void OnControlLeftByCursor(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_none;
		}
	}

	public class CButton : InternalControlWithText
	{
		public delegate void OnClick(Control control);

		public GUIBase.ButtonState State { get; private set; }
		public Color HoverColor = new Color(8, 16, 8);
		public Color PressedFillColor = new Color(128, 255, 128);
		public Color PressedTextColor = new Color(0, 0, 0);
		public bool IsDown { get; private set; }

		public OnClick OnClickDelegate;
		public OnClick OnDownDelegate;

		public CButton(RectangleF InBounding, string InTitle, Color InTextColor, object UserData = null, OnClick OnClick = null) : base(InBounding, InTitle, InTextColor)
		{
			IsDown = false;
			this.UserData = UserData;
			this.OnClickDelegate = OnClick;
		}

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			Color Background = FillColor;
			Color TColor = TextColor;
			if (State == GUIBase.ButtonState.bs_hover)
				Background = HoverColor;

			if (IsDown)
			{
				Background = PressedFillColor;
				TColor = PressedTextColor;
			}

			base.Draw(InFrame, TopLeft);
			SpriteGI.DrawSquare(InFrame, Bounding.X + TopLeft.X + 1.5f, Bounding.Y + TopLeft.Y + 1.5f,
				Bounding.Width - 2.0f, Bounding.Height - 2.0f, Background);
			Text.Draw(InFrame, new RectangleF(TopLeft + Bounding.Position, Bounding.Size), TColor);
		}

		public override void OnHover(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_hover;
		}

		public override void OnButtonDown(Vector2 Coord)
		{
			IsDown = true;
			State = GUIBase.ButtonState.bs_down;
			OnDownDelegate?.Invoke(this);
		}

		public override void OnButtonUp(Vector2 Coord)
		{
			IsDown = false;
			State = GUIBase.ButtonState.bs_hover;
			OnClickDelegate?.Invoke(this);
		}

		public override void OnControlLeftByCursor(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_none;
		}
	}

	public class CStaticText : InternalControlWithText
	{
		private TextAlignment _Alignment;
		private bool _default;
		public CStaticText(RectangleF InBounding, string InTitle, Color InTextColor, bool InDefault = false, TextAlignment InAlignment = TextAlignment.CENTER) : base(InBounding, InTitle, InTextColor)
		{
			_Alignment = InAlignment;
			_default = InDefault;
		}

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			
			if(!_default) Text.Draw(InFrame, new RectangleF(TopLeft + Bounding.Position, Bounding.Size), TextColor);
			else
			{
				var X = TopLeft.X + Bounding.Position.X;
				var Y = (float) Math.Max(TopLeft.Y + Bounding.Y + (Bounding.Height - Text.TextSize) * 0.5, 0.0);
				switch (_Alignment)
				{
					case (TextAlignment.LEFT):
						break;
					case (TextAlignment.CENTER):
						X += Bounding.Size.X / 2;
						break;
					case (TextAlignment.RIGHT):
						X += Bounding.Size.X;
						break;
				}
				InFrame.DrawText(Text.Title, X, Y, TextColor, Text.TextSize, _Alignment);
			}
		}
	}

	public class CDropDownList : InternalControlWithText
	{
		public delegate void DelItemSelected(int SelectedItem);
		public delegate void OnClick(Control control);

		public List<string> Items
		{
			get { return DropList.Items; }
			private set { }
		}

		public int SelectedItem
		{
			get { return DropList.SelectedItem; }
			set { DropList.SelectedItem = MathHelper.Clamp(value, -1, DropList.Items.Count - 1); }
		}
		
		public CDropDownModalList DropList = new CDropDownModalList(new RectangleF(0, 0, 1.0f, 1.0f), 16.0f);
		public GUIBase.ButtonState State { get; private set; }
		public DelItemSelected OnItemSelected;
		public OnClick OnClickDelegate;

		public CDropDownList(RectangleF InBounding, string InTitle, Color InTextColor, OnClick OnClick = null) : base(InBounding, InTitle, InTextColor)
		{
			Bounding.Height = 30.0f;
			DropList.IsVisible = false;
			DropList.OnItemSelected += OnItemSelectedMethod;
			DropList.UpdateDimenstions(Bounding);
			Bounding.Height = DropList.ItemHeight;
			this.OnClickDelegate = OnClick;
		}

		public void AddItem(string InItem)
		{
			DropList.Items.Add(InItem);
			DropList.UpdateDimenstions(Bounding);
		}

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			base.Draw(InFrame, TopLeft);
			SpriteGI.DrawSquare(InFrame, Bounding.X + TopLeft.X + 1.5f, Bounding.Y + TopLeft.Y + 1.5f, Bounding.Width - 2.0f, Bounding.Height - 2.0f, FillColor);
			string Title = DropList.SelectedItem == -1 ? Text.Title : DropList.Items[DropList.SelectedItem];
			float Y = (float) Math.Max(Bounding.Y + (Bounding.Height - Text.TextSize) * 0.5, 0.0);
			SpriteGI.DrawText(InFrame, Title, TopLeft.X + Bounding.Right - 10.0f - Bounding.Height, Y, TextColor, Text.TextSize, TextAlignment.RIGHT);

			RectangleF ButtonBounding = new RectangleF(Bounding.Right - Bounding.Height + TopLeft.X, Bounding.Y + TopLeft.Y, Bounding.Height, Bounding.Height);
			float Border = ButtonBounding.Width * 0.3f;
			float Width = ButtonBounding.Width * 0.4f;

			SpriteGI.DrawHollowSquare(InFrame, ButtonBounding.X, ButtonBounding.Y, ButtonBounding.Width, ButtonBounding.Height, BorderColor);
			Color SQColor = State == GUIBase.ButtonState.bs_hover ? DropList.ColorHoveredBackground : FillColor;
			Color ArrowColor = TextColor;
			if (DropList.IsVisible)
			{
				SQColor = DropList.ColorSelectedBackground;
				ArrowColor = DropList.ColorSelectedItem;
			}

			SpriteGI.DrawSquare(InFrame, ButtonBounding.X + 1.5f, ButtonBounding.Y + 1.5f, ButtonBounding.Width - 2.0f, ButtonBounding.Height - 2.0f, SQColor);
			SpriteGI.DrawRightTriangle(InFrame, ButtonBounding.X + Border, ButtonBounding.Y + Border - 2.0f, Width, Width, -(float) (Math.PI * 0.25), ArrowColor);
		}

		public override void OnHover(Vector2 Coord)
		{
			RectangleF ButtonBounding = new RectangleF(Bounding.Right - Bounding.Height, Bounding.Y, Bounding.Height, Bounding.Height);
			if (ButtonBounding.Contains(Coord)) State = GUIBase.ButtonState.bs_hover;
		}

		public override void OnButtonDown(Vector2 Coord)
		{
			RectangleF ButtonBounding = new RectangleF(Bounding.Right - Bounding.Height, Bounding.Y, Bounding.Height, Bounding.Height);
			if (ButtonBounding.Contains(Coord))
			{
				DropList.IsVisible = !DropList.IsVisible;
				State = GUIBase.ButtonState.bs_down;
			}
		}

		public override void OnButtonUp(Vector2 Coord)
		{
			RectangleF ButtonBounding = new RectangleF(Bounding.Right - Bounding.Height, Bounding.Y, Bounding.Height, Bounding.Height);
			if (ButtonBounding.Contains(Coord))
			{
				State = GUIBase.ButtonState.bs_hover;
			}
		}

		public override void OnControlLeftByCursor(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_none;
		}

		public void OnItemSelectedMethod(int InItem)
		{
			State = GUIBase.ButtonState.bs_down;
			OnItemSelected?.Invoke(InItem);
			OnClickDelegate?.Invoke(this);
		}
		

		public class CDropDownModalList : CModalControl
		{
			public List<string> Items = new List<string>();
			public int SelectedItem = -1;
			public Color ColorTextItem = new Color(128, 255, 128);
			public Color ColorSelectedItem = new Color(0, 0, 0);
			public Color ColorSelectedBackground = new Color(128, 255, 128);
			public Color ColorHoveredBackground = new Color(8, 16, 8);
			public float TextSize;
			public DelItemSelected OnItemSelected;

			int HoveredItem = -1;
			public float ItemHeight { get; private set; }


			public CDropDownModalList(RectangleF InBounding, float InTextSize) : base(InBounding)
			{
				ItemHeight = InTextSize * 1.5f;
				TextSize = InTextSize;
			}

			public void UpdateDimenstions(RectangleF InBounding)
			{
				ItemHeight = TextSize * 1.5f;
				float MaxLength = 0;
				foreach (string S in Items)
				{
					MaxLength = Math.Max(MaxLength, SpriteGI.MeasureText(S, TextSize).X + 10.0f);
				}

				Bounding.Width = Math.Max(MaxLength, InBounding.Width);
				Bounding.Height = ItemHeight * Items.Count;
				Bounding.X = InBounding.Right - Bounding.Width;
				Bounding.Y = InBounding.Bottom;
			}

			protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
			{
				base.Draw(InFrame, TopLeft);

				float OffsetY = Bounding.Y + TopLeft.Y + 5.0f;
				float OffsetX = Bounding.Right + TopLeft.X - 5.0f;
				float OffsetSelectionX = Bounding.X + TopLeft.X + 1.5f;
				for (int i = 0; i < Items.Count; i++)
				{
					if (SelectedItem == i)
					{
						SpriteGI.DrawSquare(InFrame, OffsetSelectionX, OffsetY - 3.0f, Bounding.Width - 2.0f, ItemHeight - 2.5f, ColorSelectedBackground);
						SpriteGI.DrawText(InFrame, Items[i], OffsetX, OffsetY, ColorSelectedItem, TextSize, TextAlignment.RIGHT);
					}
					else
					{
						if (HoveredItem == i) SpriteGI.DrawSquare(InFrame, OffsetSelectionX, OffsetY - 3.0f, Bounding.Width - 2.0f, ItemHeight - 2.5f, ColorHoveredBackground);
						SpriteGI.DrawText(InFrame, Items[i], OffsetX, OffsetY, ColorTextItem, TextSize, TextAlignment.RIGHT);
					}

					OffsetY += ItemHeight;
				}
			}

			public override void OnHover(Vector2 Coord)
			{
				if (ItemHeight == 0)
					HoveredItem = -1;
				else
					HoveredItem = (int) ((Coord.Y - Bounding.Y) / ItemHeight);

				if (HoveredItem < -1) HoveredItem = -1;
				if (HoveredItem >= Items.Count) HoveredItem = -1;
			}

			public override void OnButtonDown(Vector2 Coord)
			{
				if (ItemHeight == 0)
					SelectedItem = -1;
				else
				{
					SelectedItem = (int) ((Coord.Y - Bounding.Y) / ItemHeight);

					if (SelectedItem < -1) SelectedItem = -1;
					if (SelectedItem >= Items.Count) SelectedItem = -1;
					IsVisible = false;
					OnItemSelected?.Invoke(SelectedItem);
					//OnClickDelegate?.Invoke(this);
				}
			}

			public override void OnButtonUp(Vector2 Coord)
			{

			}

			public override void OnControlLeftByCursor(Vector2 Coord)
			{
				HoveredItem = -1;
			}
		}
	}

	public class CCheckBox : InternalControlWithText
	{
		public bool Checked;
		int HoveredItem = -1;
		public GUIBase.ButtonState State { get; private set; }

		public delegate void DelChecked(bool InChecked);

		public delegate void OnClick(Control control);
		public OnClick OnClickDelegate;

		public DelChecked OnCheck;
		public Color ColorHoveredBackground = new Color(8, 16, 8);

		public CCheckBox(RectangleF InBounding, string InTitle, Color InTextColor, OnClick OnClick = null) : base(InBounding, InTitle, InTextColor)
		{
			this.OnClickDelegate = OnClick;
		}

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			RectangleF BB = new RectangleF(TopLeft + Bounding.Position, Bounding.Size);
			RectangleF InternalBB = BB;
			BB.Y += 0.1f * BB.Height;
			BB.Height = 0.8f * BB.Height;
			BB.Width = BB.Height;

			SpriteGI.DrawHollowSquare(InFrame, BB.X, BB.Y, BB.Width, BB.Height, BorderColor);
			if (State == GUIBase.ButtonState.bs_hover)
			{
				SpriteGI.DrawSquare(InFrame, BB.X + 1.5f, BB.Y + 1.5f, BB.Width - 2.0f, BB.Height - 2.0f,
					ColorHoveredBackground);
			}

			if (Checked)
			{
				SpriteGI.DrawSquare(InFrame, BB.X + 3.0f, BB.Y + 3.0f, BB.Width - 6.0f, BB.Height - 6.0f, TextColor);
			}

			InternalBB.X += InternalBB.Height * 1.1f;

			float X = (float) Math.Max(InternalBB.X, 0.0);
			float Y = (float) Math.Max(InternalBB.Y + (InternalBB.Height - Text.TextSize) * 0.5, 0.0);
			if (Text.Title.Length > 0)
				SpriteGI.DrawText(InFrame, Text.Title, X, Y, TextColor, Text.TextSize);
		}

		public override void OnHover(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_hover;
		}

		public override void OnButtonDown(Vector2 Coord)
		{
			Checked = !Checked;
			State = GUIBase.ButtonState.bs_down;
			OnCheck?.Invoke(Checked);
			OnClickDelegate?.Invoke(this);
		}

		public override void OnButtonUp(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_hover;
		}

		public override void OnControlLeftByCursor(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_none;
		}
	}

	public class CRadioButton : InternalControlWithText
	{
		public bool Checked;
		int HoveredItem = -1;
		public GUIBase.ButtonState State { get; private set; }

		public delegate void DelChecked(bool InChecked);

		public DelChecked OnCheck;
		public Color ColorHoveredBackground = new Color(8, 16, 8);
		public HashSet<CRadioButton> SameLevelRadioButtons;

		public CRadioButton(RectangleF InBounding, string InTitle, Color InTextColor) : base(InBounding, InTitle, InTextColor)
		{
		}

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			RectangleF BB = new RectangleF(TopLeft + Bounding.Position, Bounding.Size);
			RectangleF InternalBB = BB;
			BB.Y += 0.1f * BB.Height;
			BB.Height = 0.8f * BB.Height;
			BB.Width = BB.Height;
			float Radius = BB.Height * 0.5f;
			SpriteGI.DrawHollowCircle(InFrame, BB.X + Radius, BB.Y + Radius, Radius, BorderColor);
			if (State == GUIBase.ButtonState.bs_hover)
			{
				SpriteGI.DrawCircle(InFrame, BB.X + Radius, BB.Y + Radius, Radius - 2.0f, ColorHoveredBackground);
			}

			if (Checked)
			{
				SpriteGI.DrawCircle(InFrame, BB.X + Radius, BB.Y + Radius, Radius - 4.0f, TextColor);
			}

			InternalBB.X += InternalBB.Height * 1.1f;

			float X = (float) Math.Max(InternalBB.X, 0.0);
			float Y = (float) Math.Max(InternalBB.Y + (InternalBB.Height - Text.TextSize) * 0.5, 0.0);
			if (Text.Title.Length > 0)
				SpriteGI.DrawText(InFrame, Text.Title, X, Y, TextColor, Text.TextSize);
		}

		public override void OnHover(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_hover;
		}

		public override void OnButtonDown(Vector2 Coord)
		{
			if (SameLevelRadioButtons != null)
			{
				foreach (CRadioButton RB in SameLevelRadioButtons)
				{
					if (RB.Checked)
					{
						RB.OnCheck?.Invoke(false);
					}

					RB.Checked = false;
				}
			}

			Checked = !Checked;
			State = GUIBase.ButtonState.bs_down;
			OnCheck?.Invoke(Checked);
		}

		public override void OnButtonUp(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_hover;
		}

		public override void OnControlLeftByCursor(Vector2 Coord)
		{
			State = GUIBase.ButtonState.bs_none;
		}
	}

	public class CProgressBar : InternalControlWithText
	{
		private float _Percent = 0;

		public float Percent
		{
			get { return _Percent; }
			set { _Percent = MathHelper.Clamp(value, 0.0f, 100.0f); }
		}

		public Color BarColor = Color.Blue;

		public CProgressBar(RectangleF InBounding, string InTitle, Color InTextColor) : base(InBounding, InTitle, InTextColor)
		{
		}

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			base.Draw(InFrame, TopLeft);
			RectangleF BB = new RectangleF(TopLeft + Bounding.Position, Bounding.Size);
			SpriteGI.DrawSquare(InFrame, BB.X + 1.5f, BB.Y + 1.5f, BB.Width - 2.0f, BB.Height - 2.0f, FillColor);
			SpriteGI.DrawSquare(InFrame, BB.X + 1.5f, BB.Y + 1.5f, BB.Width * _Percent * 0.01f - 2.0f, BB.Height - 2.0f,
				BarColor);
			Text.Title = _Percent.ToString("F00") + "%";
			Text.Draw(InFrame, BB, TextColor);
		}
	}

	public class CTrackBar : InternalControlWithText
	{
		public CTrackBar(RectangleF InBounding, string InTitle, Color InTextColor) : base(InBounding, InTitle, InTextColor)
		{
		}
	}

	public class CModalControl : Control
	{
		public CModalControl(RectangleF InBounding) : base(InBounding)
		{
		}

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			base.Draw(InFrame, TopLeft);
			SpriteGI.DrawSquare(InFrame, Bounding.Position.X + TopLeft.X + 1.5f, Bounding.Position.Y + TopLeft.Y + 1.5f,
				Bounding.Width - 2.0f, Bounding.Height - 2.0f, FillColor);
		}
	}

	public class CModalWindow : CModalControl
	{
		public InternalText Text = new InternalText();
		public Color TopBarColor = new Color(8, 16, 8);
		public Color TopDragBarColor = new Color(16, 32, 16);

		public CModalWindow(RectangleF InBounding, string InTitle) : base(InBounding)
		{
		}

		private bool IsDragged = false;
		private Vector2 DragOffset = Vector2.Zero;

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			base.Draw(InFrame, TopLeft);
			RectangleF TopLine = Bounding;
			TopLine.Position += TopLeft;
			TopLine.Height = Text.TextSize * 1.5f;

			SpriteGI.DrawSquare(InFrame, TopLine.X + 1.5f, TopLine.Y + 1.5f, TopLine.Width - 2.0f, TopLine.Height,
				IsDragged ? TopDragBarColor : TopBarColor);

			Text.Draw(InFrame, TopLine, this.BorderColor);
		}

		public override void OnHover(Vector2 Coord)
		{
			if (IsDragged)
			{
			}
		}

		public override void OnButtonDown(Vector2 Coord)
		{
			if (IsDragged) return;
			RectangleF TopLine = Bounding;
			TopLine.Height = Text.TextSize * 1.5f;
			IsDragged = TopLine.Contains(Coord);
			if (IsDragged) DragOffset = Coord - Bounding.Position;
		}

		public override void OnButtonUp(Vector2 Coord)
		{
			IsDragged = false;
		}

		public override void OnControlLeftByCursor(Vector2 Coord)
		{

		}
	}

	public class CCalculatorText : InternalControlWithText
	{
		public List<string> data;
		private RectangleF _viewport;
		private Vector2 _size;
		public Color color = Color.White;
		public bool makeColumns = false;
		
		public CCalculatorText(RectangleF InBounding, Vector2 InSize, string InTitle, Color InTextColor) : base(InBounding, InTitle, InTextColor)
		{
			_viewport = InBounding;
			_size = InSize;
		}

		protected override void Draw(MySpriteDrawFrame InFrame, Vector2 TopLeft)
		{
			var longest = data.Aggregate("", (max, current) => max.Length > current.Length ? max : current);

			const float charLen = 14.5f;
			const float lineLen = 28.5f;
			var xLen = longest.Length * charLen;
			var yLen = data.Count * lineLen;

			var columns = (int) Math.Round(Math.Sqrt(yLen / xLen));
			if (makeColumns && columns > 1)
			{
				var amount = data.Count / columns + 1;
				var scale = Math.Min(_size.Y / (amount * lineLen), _size.X / (longest.Length * charLen * columns)) - 0.01f;
				var step = new Vector2(_size.X / columns, 0);
				
				var i = 0;
				
				foreach (var list in SplitList(data, amount))
				{
					var sprite = new MySprite()
					{
						Type = SpriteType.TEXT,
						Data = string.Join("\n", list),
						Alignment = TextAlignment.LEFT,
						RotationOrScale = scale,
						Position = _viewport.Position + i * step,
						Color = color
					};
					InFrame.Add(sprite);
					i++;
				}
			}
			else
			{
				var scale = Math.Min(_size.Y / yLen, _size.X / xLen) - 0.01f;
				var sprite = new MySprite()
				{
					Type = SpriteType.TEXT,
					Data = string.Join("\n", data),
					Alignment = TextAlignment.LEFT,
					RotationOrScale = scale,
					Position = _viewport.Position,
					Color = color
				};
				InFrame.Add(sprite);
			}
		}
		private static List<List<T>> SplitList<T>(List<T> list, int size) 
		{
			var result = new List<List<T>>();
			for (var i = 0; i < list.Count; i += size) { result.Add(list.GetRange(i, Math.Min(size, list.Count - i))); }
			return result;
		}
	}
}
