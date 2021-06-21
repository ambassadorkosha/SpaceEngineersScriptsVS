using System;
using System.Collections.Generic;
using Scripts.Base;
using ServerMod;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using static Scripts.Specials.LCDScripts.AutoLCDBuffer;

namespace DrawSprites
{

	public static class SpriteGI
	{
		public static MySprite SpriteText;
		public static MySprite SpriteSquare;
		public static MySprite HollowSquare;
		public static MySprite CircleHollow;
		public static MySprite Circle;
		public static MySprite HalfCircle;
		public static MySprite SpriteTriangle;
		public static MySprite SpriteRightTriangle;
		
		public const float TextHeight = 28.7f;
		public const float TextWidth = 19.35f;


		static SpriteGI()
		{
            SpriteSquare = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(150, 150), new Vector2(100f, 100f), new Color(0, 255, 255, 255), "Monospace", TextAlignment.LEFT);
            HollowSquare = new MySprite(SpriteType.TEXTURE, "SquareHollow", new Vector2(150, 150), new Vector2(100f, 100f), new Color(0, 255, 255, 255), "Monospace", TextAlignment.LEFT);
            CircleHollow = new MySprite(SpriteType.TEXTURE, "CircleHollow", new Vector2(0, 256), new Vector2(512f, 512f), Color.White, "Monospace", TextAlignment.LEFT);
            Circle = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(0, 256), new Vector2(512f, 512f), Color.White, "Monospace", TextAlignment.LEFT);
            HalfCircle = new MySprite(SpriteType.TEXTURE, "SemiCircle", new Vector2(0, 256), new Vector2(512f, 512f), Color.White, "Monospace", TextAlignment.LEFT);
            SpriteTriangle = new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(0, 256), new Vector2(512f, 512f), Color.White, "Monospace", TextAlignment.LEFT);
            SpriteRightTriangle = new MySprite(SpriteType.TEXTURE, "RightTriangle", new Vector2(0, 256), new Vector2(512f, 512f), Color.White, "Monospace", TextAlignment.LEFT);
			SpriteText = MySprite.CreateText("шаблон", "Monospace", new Color(128, 255, 128, 255), 0.8f, TextAlignment.LEFT);
		}

		public static void DrawHollowCircle(this MySpriteDrawFrame frame, float X, float Y, float Radius, Color c)
		{
			CircleHollow.Position = new Vector2(X - Radius, Y);
			CircleHollow.Size = new Vector2(Radius * 2, Radius * 2);
			CircleHollow.Color = c;
			frame.Add(CircleHollow);
		}
		public static void DrawCircle(this MySpriteDrawFrame frame, float X, float Y, float Radius, Color c)
		{
			Circle.Position = new Vector2(X - Radius, Y);
			Circle.Size = new Vector2(Radius * 2, Radius * 2);
			Circle.Color = c;
			frame.Add(Circle);
		}
		
		public static void DrawEllipse(this MySpriteDrawFrame frame, float X1, float X2, float Y1, float Y2, Color c)
		{
			float Y = (Y2 + Y1) * 0.5f;
			Circle.Position = new Vector2(X1, Y);
			Circle.Size = new Vector2(Math.Abs(X2 - X1), Math.Abs(Y2 - Y1));
			Circle.Color = c;
			frame.Add(Circle);
		}
		public static void DrawHollowEllipse(this MySpriteDrawFrame frame, float X1, float X2, float Y1, float Y2, Color c)
		{
			float Y = (Y2 + Y1) * 0.5f;
			CircleHollow.Position = new Vector2(X1, Y);
			CircleHollow.Size = new Vector2(Math.Abs(X2 - X1), Math.Abs(Y2 - Y1));
			CircleHollow.Color = c;
			frame.Add(CircleHollow);
		}
		public static void DrawHollowEllipseLines(this MySpriteDrawFrame frame, float X1, float X2, float Y1, float Y2, Color c, float Thickness, int LineCount)
		{
			float dX = Math.Abs(X2 - X1);
			float dY = Math.Abs(Y2 - Y1);
			float cx = (X2 + X1) * 0.5f;
			float cy = (Y2 + Y1) * 0.5f;

			Vector2 P1 = new Vector2(cx + dX * 0.5f, cy);
			Vector2 P2 = P1;

			double AngleOffset = Math.PI * 2.0 / LineCount;
			double Angle = AngleOffset;
			for (int i=0; i<LineCount; i++)
			{
				P1.X = cx + dX * 0.5f * (float)Math.Cos(Angle);
				P1.Y = cy + dY * 0.5f * (float)Math.Sin(Angle);
				DrawLine(frame, P1.X, P1.Y, P2.X, P2.Y, Thickness, c);
				P2 = P1;
				Angle += AngleOffset;
			}
		}
		public static void DrawHollowHalfCircle(this MySpriteDrawFrame frame, float X, float Y, float Radius, float Thickness, Color c)
		{
			HalfCircle.Position = new Vector2(X - Radius, Y);
			HalfCircle.Size = new Vector2(Radius * 2, Radius * 2);
			HalfCircle.Color = c;
			frame.Add(HalfCircle);

			float SmallRadius = Radius - Thickness;
			HalfCircle.Position = new Vector2(X - SmallRadius, Y);
			HalfCircle.Size = new Vector2(SmallRadius * 2, SmallRadius * 2);
			HalfCircle.Color = new Color(0, 0, 0, 255);
			frame.Add(HalfCircle);
		}
		public static void DrawSquare(this MySpriteDrawFrame frame, float left, float top, float width, float height, Color c)
		{
			SpriteSquare.Position = new Vector2(left, top + height * 0.5f);
			SpriteSquare.Size = new Vector2(width, height);
			SpriteSquare.Color = c;			
			SpriteSquare.RotationOrScale = 0;
			frame.Add(SpriteSquare);
		}
		public static void DrawSquareN(this MySpriteDrawFrame frame, float left, float top, float width, float height, Color c)
		{
			frame.DrawSquare(left, top, width,1, c);
			frame.DrawSquare(left + width, top, 1, height+1, c);
			frame.DrawSquare(left, top + height, width, 1, c);
			frame.DrawSquare(left, top, 1, height, c);
		}
		public static void DrawTriangle(this MySpriteDrawFrame frame, float left, float top, float width, float height, float rotation, Color c)
		{
			SpriteTriangle.Position = new Vector2(left, top + height * 0.5f);
			SpriteTriangle.Size = new Vector2(width, height);
			SpriteTriangle.Color = c;
			SpriteTriangle.RotationOrScale = rotation;
			frame.Add(SpriteTriangle);
		}
		public static void DrawRightTriangle(this MySpriteDrawFrame frame, float left, float top, float width, float height, float rotation, Color c)
		{
			SpriteRightTriangle.Position = new Vector2(left, top + height * 0.5f);
			SpriteRightTriangle.Size = new Vector2(width, height);
			SpriteRightTriangle.Color = c;
			SpriteRightTriangle.RotationOrScale = rotation;
			frame.Add(SpriteRightTriangle);
		}
		public static void DrawHollowSquare(this MySpriteDrawFrame frame, float left, float top, float width, float height, Color c)
		{
			HollowSquare.Position = new Vector2(left, top + height * 0.5f);
			HollowSquare.Size = new Vector2(width, height);
			HollowSquare.Color = c;
			HollowSquare.RotationOrScale = 0;
			frame.Add(HollowSquare);
		}
		public static void DrawPointingTriangle(this MySpriteDrawFrame frame, float X1, float Y1, float X2, float Y2, float Thickness, Color c)
		{
			float w = X2 - X1;
			float h = Y2 - Y1;
			float l = (float)Math.Sqrt(w * w + h * h);
			float hl = l * 0.5f;
			float SinA = h / l;
			SpriteTriangle.RotationOrScale = (float)Math.Acos(w / l) * Math.Sign(SinA) - (float)(Math.PI/2.0);
			SpriteTriangle.Position = new Vector2((X1 + X2 - Thickness) * 0.5f, (Y1 + Y2) * 0.5f);
			SpriteTriangle.Size = new Vector2(Thickness, l);
			SpriteTriangle.Color = c;
			frame.Add(SpriteTriangle);
		}
		public static void DrawLine(this MySpriteDrawFrame frame, float X1, float Y1, float X2, float Y2, float Thickness, Color c)
		{
			float w = X2 - X1;
			float h = Y2 - Y1;
			float l = (float)Math.Sqrt(w * w + h * h);
			float hl = l * 0.5f;
			float SinA = h / l;
			SpriteSquare.RotationOrScale = (float)Math.Acos(w / l) * Math.Sign(SinA);
			SpriteSquare.Position = new Vector2((X1 + X2) * 0.5f - hl, (Y1 + Y2) * 0.5f);
			SpriteSquare.Size = new Vector2(l, Thickness);
			SpriteSquare.Color = c;
			frame.Add(SpriteSquare);
		}
		public static void DrawVerticalLine(this MySpriteDrawFrame frame, float X, float Y1, float Y2, float Thickness, Color c)
		{
			SpriteSquare.RotationOrScale = 0;
			SpriteSquare.Position = new Vector2(X, (Y1 + Y2) * 0.5f);
			SpriteSquare.Size = new Vector2(Thickness, Math.Abs(Y2 - Y1));
			SpriteSquare.Color = c;
			frame.Add(SpriteSquare);
		}
		public static void DrawHorizontalLine(this MySpriteDrawFrame frame, float X1, float X2, float Y, float Thickness, Color c)
		{
			SpriteSquare.RotationOrScale = 0;
			SpriteSquare.Position = new Vector2(Math.Min(X1, X2), Y);
			SpriteSquare.Size = new Vector2(Math.Abs(X2 - X1), Thickness);
			SpriteSquare.Color = c;
			frame.Add(SpriteSquare);
		}
		public static void DrawText(this MySpriteDrawFrame frame, string InText, float Left, float Top, Color InColor, float TextSize = 0, TextAlignment TAlignment = TextAlignment.LEFT, string InFontId = "Monospace")
		{
			SpriteText.Position = new Vector2(Left, Top);
			SpriteText.Data = InText;
			SpriteText.Color = InColor;
			SpriteText.Alignment = TAlignment;
			SpriteText.FontId = InFontId;
			if (TextSize != 0)
			{
				SpriteText.RotationOrScale = TextSize / TextHeight;
			}
			frame.Add(SpriteText);
		}
		public static int DrawItemAmountTextAUTOLCD(this MySpriteDrawFrame InFrame, IEnumerable<Pair<string, double[]>> InItems, int InLastLine, string InName , Vector2 InPos, Color InColor,float InTextSize, float InlineHeight, float InLeftOffset, float InRightOffset, float InDivisionsWidth, bool InBarVisibility, ShowType InType = ShowType.Default)
		{
			var line = InPos.Y;
			var barSize = InType == ShowType.OnlyExactVolume ? 0 :(InPos.X - InLeftOffset) / 5;
			
			InFrame.DrawText(InName.Replace("_"," "),  (barSize + InLeftOffset *2)/2,line, InColor,InTextSize * TextHeight, InFontId:"Debug");
			InFrame.DrawText("Qty  /  ", InPos.X - InRightOffset,line, InColor,InTextSize * TextHeight, TextAlignment.RIGHT, "Debug");
			InFrame.DrawText("Quota",InPos.X,line, InColor,InTextSize * TextHeight, TextAlignment.RIGHT, "Debug");
			line += InlineHeight;
			InLastLine++;
			
			foreach (var item in InItems)
			{
				if (InType != ShowType.OnlyExactVolume && item.v[1] > 0) InFrame.DrawProgressBarAUTOLCD(item.v,new Vector2(InLeftOffset*2,line), InColor, InlineHeight, barSize, InDivisionsWidth, InBarVisibility);
				InFrame.DrawText(item.k + ":", item.v[1] <= 0 ? InLeftOffset : barSize + InLeftOffset *3,line, InColor,InTextSize * TextHeight, InFontId:"Debug");
				InFrame.DrawText(item.v[0].toHumanQuantity() + "  /  ",InPos.X - InRightOffset,line, InColor,InTextSize * TextHeight, TextAlignment.RIGHT, "Debug");
				if(item.v[1] > 0) InFrame.DrawText(item.v[1].toHumanQuantity(),InPos.X,line, InColor,InTextSize * TextHeight,TextAlignment.RIGHT, "Debug");
				line += InlineHeight;
				InLastLine++;
			}

			return InLastLine;
		}
		
		public static int DrawDefaultAUTOLCD(this MySpriteDrawFrame InFrame, IEnumerable<Pair<string, double[]>> InCargo, int InLastLine, Vector2 InPos, Color InColor,float InTextSize, float InlineHeight, float InLeftOffset, float InDivisionsWidth, bool InBarCubeVisibility, float InPercentageOffset = 0, ShowType InType = ShowType.Default, AutoLCDInfoType inALIT = AutoLCDInfoType.Volume)
		{
			var line = InPos.Y;
			foreach (var item in InCargo)
			{
				var Percentage = item.v[0] / item.v[1];
				if (item.v[1] <= 0) Percentage = 0;
				
				if (InType != ShowType.OnlyProgressBar)
				{
					InFrame.DrawText(item.k + ":", InLeftOffset * 2, line, InColor, InTextSize * TextHeight, InFontId: "Debug");

					var Value1 = inALIT != AutoLCDInfoType.Volume ? inALIT != AutoLCDInfoType.PowerUsing ? inALIT != AutoLCDInfoType.Weight ? item.v[0].toHumanQuantityEnergy() + "h" : item.v[0].toHumanWeight() : item.v[0].toHumanQuantityEnergy() : item.v[0].toHumanQuantityVolume();
					var Value2 = inALIT != AutoLCDInfoType.Volume ? inALIT != AutoLCDInfoType.PowerUsing ? inALIT != AutoLCDInfoType.Weight ? item.v[1].toHumanQuantityEnergy() + "h" : item.v[1].toHumanWeight() : item.v[1].toHumanQuantityEnergy() : item.v[1].toHumanQuantityVolume();

					var txt = inALIT != AutoLCDInfoType.Time ? inALIT != AutoLCDInfoType.Weight && item.v[1] > 0 ?  Value1 + "  /  " + Value2 : Value1 : ((int)item.v[2]).toHumanTime2();
					if (inALIT == AutoLCDInfoType.Time && (int) item.v[2] <= 0) txt = "-";

					if (InType == ShowType.Default || InType == ShowType.OnlyExactVolume) InFrame.DrawText(txt, InPos.X - InLeftOffset, line, InColor, InTextSize * TextHeight, TextAlignment.RIGHT, "Debug");
					else InFrame.DrawText((Percentage).ToString("0.0%"), InPos.X - InLeftOffset, line, InColor, InTextSize * TextHeight, TextAlignment.RIGHT, "Debug");

					line += InlineHeight;
					InLastLine++;
				}

				if (InType == ShowType.OnlyExactVolume || InType == ShowType.OnlyPercentage) continue;
				if (InType == ShowType.Default)
				{
					InFrame.DrawText((Percentage).ToString("0.0%"), InPos.X - InLeftOffset, line, InColor, InTextSize * TextHeight, TextAlignment.RIGHT, "Debug");
					InFrame.DrawProgressBarAUTOLCD(item.v, new Vector2(InLeftOffset*2, line), InColor, InlineHeight, InPos.X - InLeftOffset*2 - InPercentageOffset, InDivisionsWidth, InBarCubeVisibility);
				}
				else InFrame.DrawProgressBarAUTOLCD(item.v, new Vector2(InLeftOffset*2, line), InColor, InlineHeight, InPos.X - InLeftOffset*2, InDivisionsWidth, InBarCubeVisibility);
				line += InlineHeight;
				InLastLine++;
			}
			return InLastLine;
		}
		public static int DrawSimpleAUTOLCD(this MySpriteDrawFrame InFrame, IEnumerable<Pair<string, string>> InPairs, int InLastLine, Vector2 InPos, Color InColor,float InTextSize, float InlineHeight, float InLeftOffset)
		{
			var line = InPos.Y;
			foreach (var pair in InPairs)
			{
				InFrame.DrawText(pair.k + ":", InLeftOffset , line, InColor, InTextSize * TextHeight, InFontId: "Debug");
				InFrame.DrawText(pair.v, InPos.X - InLeftOffset, line, InColor, InTextSize * TextHeight, InFontId: "Debug",TAlignment: TextAlignment.RIGHT);
				line += InlineHeight;
				InLastLine++;
			}
			return InLastLine;
		}
		public static int DrawSimpleAUTOLCD(this MySpriteDrawFrame InFrame, Dictionary<string, int[]> InPairs, int InLastLine, Vector2 InPos, Color InColor,float InTextSize, float InlineHeight, float InLeftOffset)
		{
			var line = InPos.Y;
			foreach (var pair in InPairs)
			{
				InFrame.DrawText(pair.Key + ":", InLeftOffset , line, InColor, InTextSize * TextHeight, InFontId: "Debug");

				string txt;
				if (pair.Value.Length > 1) txt = pair.Value[1] + " / " + pair.Value[0];
				else txt = pair.Value[0].ToString();
				
				InFrame.DrawText(txt, InPos.X - InLeftOffset, line, InColor, InTextSize * TextHeight, InFontId: "Debug",TAlignment: TextAlignment.RIGHT);
				line += InlineHeight;
				InLastLine++;
			}
			return InLastLine;
		}
		public static void DrawProgressBarAUTOLCD(this MySpriteDrawFrame InFrame, double[] Qty_Quota, Vector2 InPos, Color InColor, float InBarHeight, float InBarWidth, float InDivisionsWidth, bool InBarVisibility)
		{
			var Percentage = Math.Min(Qty_Quota[0] / Qty_Quota[1], 100);
			var aLines = InBarWidth / InDivisionsWidth -1;
			InBarWidth = (int)(aLines + 1) * InDivisionsWidth;
			for (var i = 0; i <= aLines ; i += 1)
			{
				if(i / aLines >= Percentage) break;
				InFrame.DrawEllipse( InPos.X + i * InDivisionsWidth,InPos.X + i * InDivisionsWidth + InDivisionsWidth, InPos.Y, InPos.Y + InBarHeight/1.2f, InColor);
			}
			if(InBarVisibility) InFrame.DrawSquareN(InPos.X,InPos.Y,InBarWidth,InBarHeight/1.2f,InColor);
		}

		public static Vector2 MeasureText(this string InText, float TextSize)
		{
            if (InText == null)
            {
                return new Vector2(0f,0f);
            }
			float TextScale = TextSize / TextHeight;
			return new Vector2(TextWidth * InText.Length * TextScale, TextScale * TextHeight);
		}
		public static float GetTextHeightForString(this string InText, float BoundingWidth)
		{
            if (InText == null)
            {
                return 0f;
            }
            float Scale = BoundingWidth / (TextWidth * InText.Length);
			return TextHeight * Scale;
		}
	}
}
