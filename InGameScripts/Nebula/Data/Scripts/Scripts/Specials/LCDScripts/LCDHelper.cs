using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.ModAPI;
using VRageMath;

namespace Scripts.Specials.LCDScripts {
    public static class LCDHelper {
        
        
        
        private static List<List<T>> SplitList<T>(List<T> list, int size) {
            List<List<T>> result = new List<List<T>>();

            for (int i = 0; i < list.Count; i += size) { result.Add(list.GetRange(i, Math.Min(size, list.Count - i))); }

            return result;
        }
        
        public static void Display(this IMyTextSurface _surface, Vector2 _size, RectangleF _viewport, List<string> data, bool makeColumns, Color color) {
            MySpriteDrawFrame frame = _surface.DrawFrame();

            string longest = data.Aggregate("", (max, current) => max.Length > current.Length ? max : current);

            const float charLen = 14.5f;
            const float lineLen = 28.5f;

            float xLen = longest.Length * charLen;
            float yLen = data.Count * lineLen;

            int columns = (int) Math.Round(Math.Sqrt(yLen / xLen));
            if (makeColumns && columns > 1) {
                int ammount = data.Count / columns + 1;

                float scale = Math.Min(_size.Y / (ammount * lineLen), _size.X / (longest.Length * charLen * columns)) - 0.01f;
                Vector2 step = new Vector2(_size.X / columns, 0);

                List<List<string>> split = SplitList(data, ammount);
                int i = 0;
                foreach (var list in split) {
                    string text = string.Join("\n", list);
                    Vector2 position = _viewport.Position + i * step;

                    var sprite = new MySprite() {
                                                    Type = SpriteType.TEXT,
                                                    Data = text,
                                                    Alignment = TextAlignment.LEFT,
                                                    RotationOrScale = scale,
                                                    Position = position,
                                                    Color = color
                    };

                    frame.Add(sprite);

                    i++;
                }
            } else {
                float scale = Math.Min(_size.Y / yLen, _size.X / xLen) - 0.01f;
                Vector2 position = _viewport.Position;

                var sprite = new MySprite() {
                                                Type = SpriteType.TEXT,
                                                Data = string.Join("\n", data),
                                                Alignment = TextAlignment.LEFT,
                                                RotationOrScale = scale,
                                                Position = position,
                                                Color = color
                                            };

                frame.Add(sprite);
            }

            frame.Dispose();
        }
    }
}