using System;
using System.Text;
using Digi;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;


    /** SUPPORTED LANGUAGES
     English,
    Czech,
    Slovak,
    German,
    Russian,
    Spanish_Spain,
    French,
    Italian,
    Danish,
    Dutch,
    Icelandic,
    Polish,
    Finnish,
    Hungarian,
    Portuguese_Brazil,
    Estonian,
    Norwegian,
    Spanish_HispanicAmerica,
    Swedish,
    Catalan,
    Croatian,
    Romanian,
    Ukrainian,
    Turkish,
    Latvian,
    ChineseChina,
     */


namespace Scripts.Specials {
    //AUTHOR: Stridemann
    [MyTextSurfaceScript("MultilanguagePanel", "Multi-language Panel")]
    public class MultiLanguagePanel : MyTSSCommon {
        private const string LANG_KEYWORD = "LANG-";
        private const string SETTING_TEXT_COLOR = "TextColor";
        private const string SETTING_TEXT_FONT = "TextFont";
        private const string SETTING_TEXT_SCALE = "TextScale";
        private const string SETTING_TEXT_MARGIN_TOP = "TextMarginTop";
        private const string SETTING_TEXT_MARGIN_LEFT = "TextMarginLeft";
        private readonly Vector2 _screenSize;
        private readonly IMyTextSurface _surface;
        private string _displayText;
        private Color _textColor = Color.White;
        private string _textFont = "Monospace";
        private float _textMarginLeft = 10;
        private float _textMarginTop = 10;
        private float _textScale = 1;
        private IMyCubeBlock _block;
        public MultiLanguagePanel(IMyTextSurface surface, IMyCubeBlock block, Vector2 screenSize) : base(surface, block, screenSize) {
            _screenSize = screenSize;
            _surface = surface;
            _block = block;
            (_block as IMyTerminalBlock).CustomDataChanged += OnCustomDataChanged;
            try {
                Initialize(block);
                DrawText();
            } catch (Exception e) { Log.Error(e); }
        }

        private void OnCustomDataChanged(IMyTerminalBlock obj) {
            Initialize(obj);
        }

        public override void Dispose() {
            base.Dispose();
            (_block as IMyTerminalBlock).CustomDataChanged -= OnCustomDataChanged;
            _block = null;
        }

        public override void Run() {
            Initialize(_block);
            DrawText();
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;

        private void Initialize(IMyCubeBlock block) {
            var terminalBlock = block as IMyTerminalBlock;

            if (terminalBlock != null) {
                var customData = terminalBlock.CustomData;

                if (!string.IsNullOrEmpty(customData)) {
                    var userLanguage = MyAPIGateway.Session.Config.Language.ToString();

                    string parseError;
                    ParseSettings(customData, out parseError);

                    if (string.IsNullOrEmpty(parseError)) _displayText = GetLanguageText(customData, userLanguage);
                    else _displayText = parseError;
                } else {
                    terminalBlock.CustomData = GenerateDefaultCustomData();
                    _displayText = "Test text";
                }
            } else _displayText = $"Expecting {nameof(IMyTerminalBlock)} : " + block;
        }

        private string GenerateDefaultCustomData() {
            var sb = new StringBuilder();
            sb.AppendLine($"[{SETTING_TEXT_COLOR}=255,255,255]");
            sb.AppendLine($"[{SETTING_TEXT_FONT}=Monospace]");
            sb.AppendLine($"[{SETTING_TEXT_SCALE}=1.0]");
            sb.AppendLine($"[{SETTING_TEXT_MARGIN_TOP}=10]");
            sb.AppendLine($"[{SETTING_TEXT_MARGIN_LEFT}=10]");
            sb.AppendLine();
            sb.AppendLine($"{LANG_KEYWORD}{nameof(MyLanguagesEnum.English)}:");
            sb.AppendLine("English text");
            sb.AppendLine();
            sb.AppendLine($"{LANG_KEYWORD}{nameof(MyLanguagesEnum.Russian)}:");
            sb.AppendLine("Russian text");
            sb.AppendLine();
            sb.AppendLine($"{LANG_KEYWORD}{nameof(MyLanguagesEnum.Ukrainian)}:");
            sb.AppendLine("Ukrainian text");
            return sb.ToString();
        }

        private void ParseSettings(string localizationData, out string error) {
            error = string.Empty;
            var configEndTrimIndex = localizationData.IndexOf(LANG_KEYWORD, StringComparison.OrdinalIgnoreCase);

            if (configEndTrimIndex < 2) return;

            var cfgText = localizationData.Substring(0, configEndTrimIndex);
            var splited = cfgText.Replace("\r", string.Empty).Split('\n');

            foreach (var cfgLine in splited) {
                var trimCfgLine = cfgLine.TrimStart('[').TrimEnd(']');
                var paramValue = trimCfgLine.Split('=');

                if (paramValue.Length == 2) {
                    var param = paramValue[0];
                    var value = paramValue[1];

                    if (param == SETTING_TEXT_COLOR) {
                        var colorComps = value.Split(',');

                        if (colorComps.Length == 3) {
                            byte r, g, b;

                            if (byte.TryParse(colorComps[0], out r) && byte.TryParse(colorComps[1], out g) && byte.TryParse(colorComps[2], out b)) _textColor = new Color(r / 255f, g / 255f, b / 255f);
                            else error = "Color parse error";
                        }
                    } else if (param == SETTING_TEXT_FONT) _textFont = value;
                    else if (param == SETTING_TEXT_SCALE) {
                        float scale;

                        if (float.TryParse(value.Replace(",", "."), out scale)) _textScale = scale;
                        else error = "Scale parse error";
                    } else if (param == SETTING_TEXT_MARGIN_TOP) {
                        if (!float.TryParse(value, out _textMarginTop)) error = "MarginTop parse error";
                    } else if (param == SETTING_TEXT_MARGIN_LEFT) {
                        if (!float.TryParse(value, out _textMarginLeft)) error = "MarginLeft parse error";
                    } else error = $"Unknown setting: {param}";
                }
            }
        }

        private string GetLanguageText(string localizationData, string userLanguage) {
            var langCfgLine = $"{LANG_KEYWORD}{userLanguage}:";
            var keywordIndex = localizationData.IndexOf(langCfgLine, StringComparison.OrdinalIgnoreCase);

            if (keywordIndex == -1) //User lang localization not found, try find english
            {
                langCfgLine = $"{LANG_KEYWORD}{nameof(MyLanguagesEnum.English)}:";
                keywordIndex = localizationData.IndexOf(langCfgLine, StringComparison.OrdinalIgnoreCase);
            }

            if (keywordIndex == -1) return "No english text found";

            var startSplitPos = keywordIndex + langCfgLine.Length;

            var endSplitPos = localizationData.IndexOf(LANG_KEYWORD, startSplitPos, StringComparison.OrdinalIgnoreCase); //try search the next lang as end of current lang text

            if (endSplitPos == -1) //this is the last lang
                endSplitPos = localizationData.Length;

            var splitLength = endSplitPos - startSplitPos;
            return localizationData.Substring(startSplitPos, splitLength).Trim('\r').Trim('\n');
        }

        private void DrawText() {
            var minScreenSide = Math.Min(_screenSize.X, _screenSize.Y); //as I tested the X and Y is always equal..
            var viewRectangle = new RectangleF((_surface.TextureSize - _surface.SurfaceSize) / 2 + Vector2.One * 5, _surface.SurfaceSize);

            var textSprite = MySprite.CreateText(_displayText, _textFont, _textColor, minScreenSide / 500 * _textScale, //autoscaling for different screens
                                                 TextAlignment.LEFT);

            textSprite.Size = new Vector2(512f, 80f);
            textSprite.Position = new Vector2(viewRectangle.X + _textMarginLeft, viewRectangle.Y + _textMarginTop);

            using (var frame = _surface.DrawFrame()) { frame.Add(textSprite); }
        }
    }
}