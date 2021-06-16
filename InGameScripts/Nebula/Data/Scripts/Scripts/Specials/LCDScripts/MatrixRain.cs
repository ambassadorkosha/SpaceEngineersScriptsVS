using System;
using System.Collections.Generic;
using System.Linq;
using DrawSprites;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials.LCDScripts
{
    [MyTextSurfaceScript("MatrixRain", "Matrix Rain")]
    public class MatrixRain : MyTSSCommon
    {
        private readonly Vector2 _size;
        private static readonly Random rand = new Random();
        private readonly int MaxColumns;
        private readonly bool isWide;
        private readonly Vector2 CharSize;

        private static readonly string[] chars;
        private const string afb = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        static MatrixRain()
        {
            chars = new string[afb.Length];
            for (var i = 0; i < afb.Length; i++)
            {
                chars[i] = "" + afb[i];
            }
        }

        private class CharColumn
        {
            public int ColumnNum;
            public int startLine;
            public readonly List<string> Chras = new List<string>();
            public void nxt()
            {
                if (Chras.Count >= 15)
                {
                    Chras.RemoveAt(0);
                    startLine++;
                }
                Chras.Add(chars[rand.Next(chars.Length)]);
            }
        }
        private readonly List<CharColumn> charColumns = new List<CharColumn>();
        
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;
        public MatrixRain(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            var viewport = new RectangleF((size - surface.SurfaceSize) / 2, surface.SurfaceSize);
            _size = viewport.Size;
            if (surface.SurfaceSize.X > surface.SurfaceSize.Y) isWide = true;
            CharSize = new Vector2(SpriteGI.TextWidth, SpriteGI.TextHeight)/2;
            MaxColumns = (int)(_size.X / CharSize.X) + 1;
        }

        public override void Run()
        {
            base.Run();
            for(var i =0;i < (isWide ? 2:1) ;i++)
            {
                var newColumnNum = rand.Next(MaxColumns);
                while (charColumns.Where(c=> c.ColumnNum == newColumnNum).Any(c => c.Chras.Count < 8 )) {newColumnNum = rand.Next(MaxColumns);}
                charColumns.Add(new CharColumn {ColumnNum = newColumnNum});
            }

            using (var frame = Surface.DrawFrame())
            {
                charColumns.RemoveAll(CCol => CCol.startLine * CharSize.Y > _size.Y);
                foreach (var CCol in charColumns)
                {
                    DrawColumn(frame, CCol.ColumnNum * CharSize.X, CCol.startLine, CCol.Chras, m_foregroundColor, CharSize.Y);
                    CCol.nxt();
                }
            }
        }
        
        private void DrawColumn(MySpriteDrawFrame frame, float x, int y, List<string> strings, Color inColor, float inCharSize)
        {
            var C = inColor.ToVector3();
            for (var z = 0; z < strings.Count; z++)
            {
                var dif = 255 / strings.Count * (strings.Count - (z + 1))/10;
                inColor = new Color(C.X / dif,C.Y / dif,C.Z / dif);
                var str = strings[z];
                frame.DrawText(str, x, (y+z) * inCharSize, inColor, inCharSize);
            }
        }
    }
}