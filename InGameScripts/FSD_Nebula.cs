using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Slime;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

using MyInventoryItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;
using MyProductionItem = Sandbox.ModAPI.Ingame.MyProductionItem;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;


/*
 *  Extensions are written behind the block- or group name (same line) with a seperator: "," or ":" (both work)
 *  --------------------------------------------------------------------------------------------------------------------------------------------
 *  WideBar           Large bar instead of an icon for each single block               -> Syntax: GroupOrBlockName,Widebar
 *  SmallBar          Small bar instead of an icon for each single block               -> Syntax: GroupOrBlockName,SmallBar
 *  NoIcon              Only a small headline without any graphics                         -> Syntax: GroupOrBlockName,noIcon
 *  (The 'WideBar' 'SmallBar' and 'NoIcon' extensions are mutually exclusive)
 *  --------------------------------------------------------------------------------------------------------------------------------------------
 *  nolinebreak       Each group will occupy only one line, no matter it's size   -> Syntax: GroupOrBlockName,nolinebreak
 *  noscrolling        Headline does not scroll                                                       -> Syntax: GroupOrBlockName,noscrolling
 *  noheadline        Headline not shown                                                              -> Syntax: GroupOrBlockName,noheadline
 *  nosubgrids        Blocks or groups on other subgrids will be ignored            -> Syntax: GroupOrBlockName,nosubgrids
 *  nosubgridLCDs  Reads all blocks on subgrids, but doesn't write to lcds'    -> Syntax: GroupOrBlockName,nosubgridLCDs
 *  optional             switch between e.g. power or inventory                              -> Syntax: GroupOrBlockName,optional
 *  ""         Searches for exact name instead of blocks containing the name     -> Syntax: "GroupOrBlockName"
 *  left                     Left aligned text (standard)                                                   -> Syntax: GroupOrBlockName,left
 *  center                Center aligned text                                                                -> Syntax: GroupOrBlockName,center
 *  right                   Right aligned text                                                                  -> Syntax: GroupOrBlockName,right
 *  noIcons              Text (headline) only - no icons or bars                                 -> Syntax: GroupOrBlockName,noIcons
 *  #                        Shows only 1 icon per #-symbol                                           -> Syntax: GroupOrBlockName###
 *  (Missing Blocks will be replaced with a placeholder ico. Works not with 'LargeBar, 'SmallBar' or 'noIcons'.)
 *  --------------------------------------------------------------------------------------------------------------------------------------------
 *  Text:           Write custom text in front of the headline,             -> Syntax: GroupOrBlockName,Text:CustomText
 *                    or combine it with noheadline,                                -> Syntax: GroupOrBlockName,noheadline,Text:CustomText
 *                    or use without GroupOrBlockName                         -> Syntax: ,Text:CustomText
 *  (The 'Text:' Extension must be the last one in line since everthing between it and the line end will be shown as text)
 *  --------------------------------------------------------------------------------------------------------------------------------------------
 *  Position(x,y)     Choose the draw position yourself                   -> Syntax: GroupOrBlockName,Position(x,y)
 *  (The next item in the display list will be located on the left screen border again.)
 *   --------------------------------------------------------------------------------------------------------------------------------------------
 *  gfxStart(x,y,fontsize)     Insert own monospace graphic, fontsize is optional and 0.1 by default
 *  gfxEnd
 *  -> Syntax: 
 *  ,gfxStart(x,y,0.2)
 *  [MonospaceGraphic]
 *  gfxEnd         
 *  --------------------------------------------------------------------------------------------------------------------------------------------
 *  Different extensions can be combined in the same line.    Example: GroupOrBlockName,Position(x,y) Widebar optional noscrolling text:Exampletext
 *  --------------------------------------------------------------------------------------------------------------------------------------------
 *  You can combine differnt group- or blocknames by using the seperator: "+"     -> Syntax: GroupOrBlockName1 + GroupOrBlockName2 + GroupOrBlockName3
 *  You can separate these differnt groups or blocks by using 'Gap' with a number specifying the with of the gap.
 *  Gap               Insert a gap before/after drawn object(s)                   -> Syntax: Gap 20 + GroupOrBlockName1 + Gap 30 + GroupOrBlockName2
 *  --------------------------------------------------------------------------------------------------------------------------------------------
 *  You can combine this also with WideBar or SmallBar in consecutive lines       -> Syntax: GroupOrBlockName 1, Widebar
 *                                                                                           + Gap 30 + GroupOrBlockName2, Widebar
 *  (Combining SmallBar and WideBar in a single line is not possible.)
 *  
 *  ********************************************************************************************************************************************
 *  Overide options
 *  ********************************************************************************************************************************************
 *  You can overide individual LCD/Cockpit screen settings by using a special keyword line starting with 'FSD options:' in the Custom Data field 
 *  of the Programmable block itself.
 *  All keywords for this override options must be in a single line and this line must be located above an optional 'ShowStats' line or else the 
 *  used keywords affect only the LCD panels of the Programmable block.
 *  Avaible overide option keywords
 *  nolinebreak         applying the noLinebreak option on all controlled LCD screens                   -> Syntax: FSD options:nolinebreak
 *  noscrolling          applying the noScrolling option on all controlled LCD screens                    -> Syntax: FSD options:noscrolling
 *  noheadline          applying the noHeadline option on all controlled LCD screens                     -> Syntax: FSD options:noheadline
 *  nosubgrids          applying the noSubgrid option on all controlled LCD screens                      -> Syntax: FSD options:nosubgrids
 *  optional               applying the optional option on all controlled LCD screens                          -> Syntax: FSD options:optional
 *  verbatim              Searches exact name instead of blocks containing the name                     -> Syntax: FSD options:verbatim
 *  nosubgridLCDs   only LCD Screens on the same subgrid as the PB will be contolled            -> Syntax: FSD options:nosubgridLCDs
 *  
 *  Options can be combined.  Example: FSD options:optional noheadline nolinebreak noscrolling nosubgrids nosubgridLCDs verbatim
 *  (Only 'FSD options:' is case sensitive. All other keywords for the overide options are not case sensitive.)
 *  
 *  ********************************************************************************************************************************************
 *  Example for a cockpit with multiple screens
 *  ********************************************************************************************************************************************
 *  ShowStats
 *  Panel 0
 *  Battery,optional,WideBar
 *  Hydrogen:WideBar
 *  Panel 3
 *  Cargo
 *  Panel 1
 *  Drill + Gap 20 + Connector
 *  Connector
 *  ********************************************************************************************************************************************
 *  Example for a normal screen
 *  ********************************************************************************************************************************************
 *  ShowStats
 *  Battery,SmallBar,Position(150,150)
 *  Hydrogen Engine
 *  Cargo 2
 *  ********************************************************************************************************************************************
 *  Arguments - Run the pb with the following arguments
 *  ********************************************************************************************************************************************
 *  The Programmable block can be controlled by running it with one of these arguments: (not case sensitive)
 *  shutdown        Turns all screens black/off
 *  powerup          Turns all screens back online
 *  refresh             Refreshes all screens - workaround for multiplayer, needs further testing
 *  ********************************************************************************************************************************************
 *  Examples for you to copy to the color-config
 *  ********************************************************************************************************************************************
 *  Color frameColorFunctional = Color.Cyan;          //color if block is not damaged
 *  Color frameColorNotFunctional = Color.Red;      //color if block is damaged
 *  Color frameColorWideBar = Color.White;           //color wide bar
 *  Color headlineColor = Color.White;                    //headline for single blocks
 *  Color headlineColorWideBar = Color.White;      //headline for wide bar
 *  Color pictogramColor = Color.White;                  //symols on single blocks
 *  Color percentDisplayColor = Color.White;         //fillstate percentvalue on single blocks
 *  Color ChargeColorLoaded = Color.Lime;            //color of chargebar 100 to 91% - inverted on cargo with ChargeColorEmpty
 *  Color ChargeColorNormal = Color.Cyan;            //color of chargebar 90 to 26%
 *  Color ChargeColorEmpty = Color.Red;               //color of chargebar 25 to 0% - inverted on cargo with ChargeColorLoaded
 *  
 *  // Colors you could use:
 *  Color.AliceBlue,Color.AntiqueWhite,Color.Aqua,Color.Aquamarine,Color.Azure,Color.Beige,Color.Bisque,
 *  Color.Black,Color.BlanchedAlmond,Color.Blue,Color.BlueViolet,Color.Brown,Color.BurlyWood,Color.CadetBlue,Color.Chartreuse,
 *  Color.Chocolate,Color.Coral,Color.CornflowerBlue,Color.Cornsilk,Color.Crimson,Color.Cyan,Color.DarkBlue,Color.DarkCyan,
 *  Color.DarkGoldenrod,Color.DarkGray,Color.DarkGreen,Color.DarkKhaki,Color.DarkMagenta,Color.DarkOliveGreen,Color.DarkOrange,
 *  Color.DarkOrchid,Color.DarkRed,Color.DarkSalmon,Color.DarkSeaGreen,Color.DarkSlateBlue,Color.DarkSlateGray,
 *  Color.DarkTurquoise,Color.DarkViolet,Color.DeepPink,Color.DeepSkyBlue,Color.DimGray,Color.DodgerBlue,Color.Firebrick,
 *  Color.FloralWhite,Color.ForestGreen,Color.Fuchsia,Color.Gainsboro,Color.GhostWhite,Color.Gold,Color.Goldenrod,Color.Gray,
 *  Color.Green,Color.GreenYellow,Color.Honeydew,Color.HotPink,Color.IndianRed,Color.Indigo,Color.Ivory,Color.Khaki,Color.Lavender,
 *  Color.LavenderBlush,Color.LawnGreen,Color.LemonChiffon,Color.LightBlue,Color.LightCoral,Color.LightCyan,
 *  Color.LightGoldenrodYellow,Color.LightGray,Color.LightGreen,Color.LightPink,Color.LightSalmon,Color.LightSeaGreen,
 *  Color.LightSkyBlue,Color.LightSlateGray,Color.LightSteelBlue,Color.LightYellow,Color.Lime,Color.LimeGreen,Color.Linen,
 *  Color.Magenta,Color.Maroon,Color.MediumAquamarine,Color.MediumBlue,Color.MediumOrchid,Color.MediumPurple,
 *  Color.MediumSeaGreen,Color.MediumSlateBlue,Color.MediumSpringGreen,Color.MediumTurquoise,Color.MediumVioletRed,
 *  Color.MidnightBlue,Color.MintCream,Color.MistyRose,Color.Moccasin,Color.NavajoWhite,Color.Navy,Color.OldLace,Color.Olive,
 *  Color.OliveDrab,Color.Orange,Color.OrangeRed,Color.Orchid,Color.PaleGoldenrod,Color.PaleGreen,Color.PaleTurquoise,
 *  Color.PaleVioletRed,Color.PapayaWhip,Color.PeachPuff,Color.Peru,Color.Pink,Color.Plum,Color.PowderBlue,Color.Purple,
 *  Color.Red,Color.RosyBrown,Color.RoyalBlue,Color.SaddleBrown,Color.Salmon,Color.SandyBrown,Color.SeaGreen,Color.SeaShell,
 *  Color.Sienna,Color.Silver,Color.SkyBlue,Color.SlateBlue,Color.SlateGray,Color.Snow,Color.SpringGreen,Color.SteelBlue,Color.Tan,
 *  Color.Teal,Color.Thistle,Color.Tomato,Color.Transparent,Color.Turquoise,Color.Violet,Color.Wheat,Color.White,Color.WhiteSmoke,
 *  Color.Yellow,Color.YellowGreen,
 */
namespace Scripts.Specials
{

    [MyTextSurfaceScriptAttribute("FancyLCD", "Fancy LCD")]
    class FancyLCDScript : MyTSSCommon
    {
        private IMyCubeBlock ts_block;
        public FancyLCDScript(IMyTextSurface lcd_surface, IMyCubeBlock ts_block, Vector2 size) : base(lcd_surface, ts_block, size)
        {
            this.ts_block = ts_block;
            GeneratePatterns();
        }

        public override void Run()
        {

            try
            {
                base.Run();
                if (MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ts_block.CubeGrid) == null
                  || !((IMyTerminalBlock)ts_block).CustomData.Contains(lcdtag))
                    return;

                int surfaceIndex;
                screenIndex = 0;
                string temp_txt = "";
                temp_txt = getBetween(((IMyTerminalBlock)ts_block).CustomData + "\n", "FSD options:", "\n").ToLower();
                optionalViewDefault = temp_txt.Contains(optionaltag.ToLower());
                noheadlineDefault = temp_txt.Contains(noheadlinetag.ToLower());
                nonameDefault = temp_txt.Contains(nonametag.ToLower());
                nolinebreakDefault = temp_txt.Contains(nolinebreaktag.ToLower());
                noscrollingDefault = temp_txt.Contains(noscrolltag.ToLower());
                verbatimdefault = temp_txt.Contains(verbatimtag.ToLower());
                nosubgridDefault = temp_txt.Contains(nosubgridtag.ToLower());
                nogroupsDefault = temp_txt.Contains(nogrouptag.ToLower());
                nosubgridLCDs = temp_txt.Contains(nosubgridLCDtag.ToLower());
                if (temp_txt.Contains(righttag.ToLower())) { textposdefault = TextAlignment.RIGHT; }
                else if (temp_txt.Contains(centertag.ToLower())) { textposdefault = TextAlignment.CENTER; }
                else { textposdefault = TextAlignment.LEFT; }

                temp_txt = ((IMyTerminalBlock)ts_block).CustomData;

                var provider = ts_block as IMyTextSurfaceProvider;
                Displayname = ((IMyTerminalBlock)ts_block).CustomName; // for Error reporting
                temp_txt = temp_txt.Substring(temp_txt.IndexOf(lcdtag) + lcdtag.Length).Trim();
                if (temp_txt.Contains(endtag)) { temp_txt = temp_txt.Remove(temp_txt.IndexOf(endtag)); }
                string[] sections = ("\n" + temp_txt).Split(new[] { "\nPanel ", "\npanel ", "\nPANEL " }, StringSplitOptions.None).Where(x => !string.IsNullOrEmpty(x)).Select(s => s.Trim()).ToArray();
                for (int i = 0; i < sections.Length; i++)
                {
                    string[] lines = sections[i].Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    if (lines.Length > 0 && int.TryParse(lines.First(), out surfaceIndex))
                    {       //get surface index
                        if (surfaceIndex < provider.SurfaceCount)
                        {     //surface exists?
                            surface = provider.GetSurface(surfaceIndex);
                            sArgs = lines.Skip(1).ToArray();                      //get what to show on screen
                        }
                    }
                    else
                    {  //no surface index found
                        surface = provider.GetSurface(0);
                        sArgs = lines;                      //get what to show on screen
                    }
                    DrawScreen(surface, argument);
                    screenIndex++;
                }
            }
            catch (Exception ex)
            {
                if (surface != null)
                {
                    //surface.ContentType = ContentType.TEXT_AND_IMAGE;
                    surface.WriteText("ERROR DESCRIPTION:\n" + ex);
                }
            }

        }

        //  ****************************************************************************************************************************************
        //  Config
        //  ****************************************************************************************************************************************
        // Tag the script reacts to - write it to the custom data of your screen/cockpit
        const string lcdtag = "ShowStats";

        // Tag to end the command list (optional)
        const string endtag = "EndStats";

        // Tag to display either power or inventory if a block has both (in case of battery it's load percentage or input/output)
        const string optionaltag = "optional";

        // Tag to display the blocks without headline
        const string noheadlinetag = "noheadline";

        //Tag to exclude subgrids
        const string nosubgridtag = "nosubgrids";

        //Tag to exclude subgrid LCDs
        const string nosubgridLCDtag = "nosubgridLCDs";

        //Tag to require exact Name match
        const string verbatimtag = "verbatim";

        //Tag to skip groups in the detection
        const string nogrouptag = "nogroups";

        // Tag to display a large chargebar instead of each block individually
        const string widebartag = "WideBar";

        // Tag to display a small chargebar instead of each block individually
        const string smallbartag = "SmallBar";

        // Tag to display a gap between two blocks
        const string gaptag = "Gap";

        // Tag to position the next blocks
        const string positiontag = "Position";

        // Tag to prevent linebreaks
        const string nolinebreaktag = "nolinebreak";

        // Tag to display a custom text
        const string texttag = "Text:";

        // Tag to display text left aligned
        const string lefttag = "left";

        // Tag to display text center aligned
        const string centertag = "center";

        // Tag to display text right aligned
        const string righttag = "right";

        // no scrolling
        const string noscrolltag = "noscrolling";

        //Tag to display only the headlines without any graphics
        const string noicontag = "noIcons";

        //Tag to display only the headlines without the names
        const string nonametag = "noNames";

        //Tag to indicate the start of a graphic sprite
        const string gfxStart = "gfxStart";

        //Tag to indicate the end of a graphic sprite
        const string gfxEnd = "gfxEnd";

        // How often the script is updating
        const int scriptUpdatesPerMinute = 60;

        // The fancy stuff - color section
        // Any RGB-Color could be used from Black new Color(0, 0, 0) to White new Color(255, 255, 255)

        Color frameColorFunctional = Color.Cyan;          //color if block is not damaged
        Color frameColorNotFunctional = Color.Red;      //color if block is damaged
        Color frameColorWideBar = Color.White;           //color wide bar
        Color headlineColor = Color.White;                    //headline for single blocks
        Color headlineColorWideBar = Color.White;      //headline for wide bar
        Color pictogramColor = Color.White;                  //symols on single blocks
        Color percentDisplayColor = Color.White;         //fillstate percentvalue on single blocks
        Color ChargeColorLoaded = Color.Lime;            //color of chargebar 100 to 91% - inverted on cargo with ChargeColorEmpty
        Color ChargeColorNormal = Color.Cyan;            //color of chargebar 90 to 26%
        Color ChargeColorEmpty = Color.Red;               //color of chargebar 25 to 0% - inverted on cargo with ChargeColorLoaded

        //  ****************************************************************************************************************************************
        //  Config  End - Do not change anything below
        //  ****************************************************************************************************************************************

        int execCounter = 1, containertype, screenIndex, headlineIndex, placehlds, fillLevelLoopcount;
        int[,] scrollpositions = new int[50, 20];
        string[] sArgs;
        string argument, pattern, chargeBar, icon, drawpercent, Displayname, eShieldInput = "", added_txt = "", patternBattery, patternCargo, patternTank, patternConnector, patternTool, patternJumpdrive, patternGasgen, patternShield, patternTurret, patternThruster, patternReactor, patternSolar, patternWindmill, patternNone, patternWideBar, patternSmallBar, patternRefinery, patternAssembler, patternProjector, iconProjector, iconIce, iconReactor, iconOil, iconWindmill, iconSolar, iconThruster, iconCollector, iconEjector, iconCargo, iconConnector, iconDrill, iconWelder, iconGrinder, iconJumpdrive, iconShield, iconBullet, iconRocket, iconRefinery, iconAssembler, chargebarSC, chargebarWide, chargebarSmall, Background, StartSep = "[ ", SepStart = "[ ", StopSep = " ]", SepStop = " ]";
        StringBuilder gfxSprite = new StringBuilder();
        bool get_sprite, invertColor, optionalView, noheadline, noname, nolinebreak, noscrolling, verbatim, nosubgrids, nogroups, optionalViewDefault, noheadlineDefault, nonameDefault, nolinebreakDefault, noscrollingDefault, verbatimdefault, nosubgridDefault, nogroupsDefault, nosubgridLCDs;
        TextAlignment textpos = TextAlignment.LEFT, textposdefault = TextAlignment.LEFT;
        Color fillLevelColor = new Color(), frameColor = new Color();
        //Arrays for sumXY[0](blockcount),sumXY[1](percent), sumXY[2](loadvalue)
        float[] sumBat = new float[3], sumJdrives = new float[3], sumCargo = new float[3], sumHydro = new float[3], sumOxy = new float[3], sumCollectors = new float[3], sumConnectors = new float[3], sumEjectors = new float[3], sumDrills = new float[3], sumGrinder = new float[3], sumWelder = new float[3], sumGasgen = new float[3], sumShield = new float[3], sumGatling = new float[3], sumMissileTurret = new float[3], sumGatlingTurret = new float[3], sumMissileLauncher = new float[3], sumThrust = new float[3], sumReactor = new float[3], sumSolar = new float[3], sumWindmill = new float[3], sumO2Gen = new float[3], sumRefinery = new float[3], sumAssembler = new float[3], sumProjector = new float[3], sumEShield = new float[2], sumOil = new float[3], sumOilRef = new float[3], sumOilEng = new float[3], sumAll = new float[2];
        HashSet<IMyCubeGrid> ignoredGrids = new HashSet<IMyCubeGrid>();
        List<IMyTerminalBlock> surfaceProviders = new List<IMyTerminalBlock>(), Group = new List<IMyTerminalBlock>(), subgroup = new List<IMyTerminalBlock>();
        IMyTextSurface surface;
        RectangleF viewport;
        MySpriteDrawFrame frame = new MySpriteDrawFrame();
        Vector2 chargeBarInitialOffset, chargeBarOffset, LineStartpos, pos;

        public class GasGen
        {
            public static float MaxVolume(IMyTerminalBlock block)
            {
                float val;
                float.TryParse(getBetween(block.DetailedInfo, "L/", "L)"), out val);
                return val;
            }
            public static float CurrentVolume(IMyTerminalBlock block)
            {
                float val;
                float.TryParse(getBetween(block.DetailedInfo, " (", "L/"), out val);
                return val;
            }
        }
        public class EnergyShield
        {
            public static float CurrentHitpoints(IMyTerminalBlock block)
            {
                float val;
                float.TryParse(getBetween(block.CustomName, " (", "/"), out val);
                return val;
            }
            public static float MaxHitpoints(IMyTerminalBlock block)
            {
                float val;
                float.TryParse(getBetween(block.CustomName, "/", ")"), out val);
                return val;
            }
            public static string RequiredInput(IMyTerminalBlock block)
            {
                return getBetween(block.DetailedInfo, "\nRequired Input: ", "\n");
            }
        }
        void DrawScreen(IMyTextSurface surface, string argument)
        {
            viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);   //draw area
            pos = LineStartpos = viewport.Position + new Vector2(7, 0);  //draw starting positon for each screen
            frame = surface.DrawFrame();
            string[] groupAndOptions;
            string c_arg;
            float gfx_res = 0.1f;
            bool continueDrawing = true;
            int lastDraw = Nothing;
            int optionpos = 0;
            int texttagpos = 0;
            headlineIndex = 0;
            //Set up draw surface
            PrepareTextSurface(surface);
            get_sprite = false;
            gfxSprite.Clear();
            foreach (var arg in sArgs)
            {// get what to draw on a specific screen
                if (get_sprite)
                {
                    if (arg.ToLower().TrimStart(new[] { ':', ',' }) == gfxEnd.ToLower())
                    {
                        AddTextSprite(frame, pos, gfxSprite.ToString(), "Monospace", gfx_res, Color.White, TextAlignment.LEFT);
                        get_sprite = false;
                        gfxSprite.Clear();
                    }
                    else
                    {
                        gfxSprite.AppendLine(arg.Trim());
                    }
                    continue;
                }
                LineStartpos.X = viewport.X + 7;   //reset x after used positiontag
                optionpos = arg.IndexOfAny(new char[] { ',', ':' });
                textpos = textposdefault;
                containertype = SingleContainer;
                if (optionpos >= 0)
                {
                    c_arg = arg.ToLower().Substring(optionpos + 1); // separating the options
                    texttagpos = c_arg.IndexOf(texttag.ToLower());
                    if (texttagpos >= 0)
                    { // looking for texttag
                        added_txt = arg.Substring(texttagpos + texttag.Length + optionpos + 1);
                        c_arg = c_arg.Remove(texttagpos);
                    }
                    else
                    {
                        added_txt = "";
                    }
                    if (c_arg.StartsWith(gfxStart.ToLower()))
                    {
                        Vector2 tempvector;
                        if (getVector(c_arg, gfxStart, out tempvector, out gfx_res))
                        {
                            pos = LineStartpos = tempvector + viewport.Position;
                            get_sprite = true;
                            gfxSprite.Clear();
                            continue;
                        }
                    }
                    if (c_arg.Contains(positiontag.ToLower()))
                    { // looking for positiontag
                        Vector2 tempvector;
                        if (getVector(c_arg, positiontag, out tempvector, out gfx_res)) { pos = LineStartpos = tempvector + viewport.Position; lastDraw = Nothing; }
                    }
                    if (c_arg.Contains(righttag.ToLower())) { textpos = TextAlignment.RIGHT; }
                    else if (c_arg.Contains(centertag.ToLower())) { textpos = TextAlignment.CENTER; }
                    else if (c_arg.Contains(lefttag.ToLower())) { textpos = TextAlignment.LEFT; }
                    optionalView = c_arg.Contains(optionaltag.ToLower()) ^ optionalViewDefault;
                    noheadline = c_arg.Contains(noheadlinetag.ToLower()) ^ noheadlineDefault;
                    noname = c_arg.Contains(nonametag.ToLower()) ^ nonameDefault;
                    nolinebreak = c_arg.Contains(nolinebreaktag.ToLower()) ^ nolinebreakDefault;
                    noscrolling = c_arg.Contains(noscrolltag.ToLower()) ^ noscrollingDefault;
                    nosubgrids = c_arg.Contains(nosubgridtag.ToLower()) ^ nosubgridDefault;
                    nogroups = c_arg.Contains(nogrouptag.ToLower()) ^ nogroupsDefault;
                    if (c_arg.Contains(widebartag.ToLower()))
                    { // looking for widebartag
                        if (viewport.Width < 500) { containertype = SmallBar; } else { containertype = WideBar; }
                    }
                    else if (c_arg.Contains(smallbartag.ToLower()))
                    { // looking for smallbartag
                        containertype = SmallBar;
                    }
                    else if (c_arg.Contains(noicontag.ToLower()))
                    { // looking for smallbartag
                        containertype = Textline;
                    }
                    c_arg = arg.Substring(0, optionpos);
                }
                else
                {
                    optionalView = optionalViewDefault;
                    noheadline = noheadlineDefault;
                    noname = nonameDefault;
                    nolinebreak = nolinebreakDefault;
                    noscrolling = noscrollingDefault;
                    nosubgrids = nosubgridDefault;
                    added_txt = "";
                    c_arg = arg;
                }
                groupAndOptions = c_arg.Trim().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                if (continueDrawing)
                {
                    continueDrawing = CycleDrawBlocks(groupAndOptions, lastDraw);     //draw blocks or blockgroups
                    lastDraw = containertype;
                }
            }
            frame.Dispose();
        }
        bool CycleDrawBlocks(string[] groupOrBlocknames, int lastDraw)
        {
            Vector2 posHeadline = new Vector2(0, 0);
            Vector2 posContainerGroup = posHeadline;
            StringBuilder headline = new StringBuilder();
            float percent = 0;
            bool continueDrawing = true;
            ClearGroupArrays();
            Group.Clear();
            switch (lastDraw)
            {
                case WideBar:
                    if (containertype != lastDraw || (pos.X + 508) >= viewport.Width) { LineStartpos.Y += 72; pos = LineStartpos; }
                    break;
                case SmallBar:
                    if (containertype != lastDraw || (pos.X + 256) >= viewport.Width) { LineStartpos.Y += 55; pos = LineStartpos; }
                    break;
                case SingleContainer:
                    LineStartpos.Y += 95; pos = LineStartpos;
                    break;
                case Textline:
                    LineStartpos.Y += 23; pos = LineStartpos;
                    break;
            }
            if (containertype == SingleContainer && (!noheadline || added_txt != ""))
            {
                LineStartpos.Y += 23; pos = LineStartpos;  // leave room for drawing headline above containers
                posContainerGroup = pos;
            }
            foreach (string entry in groupOrBlocknames)
            {
                string name = entry.Trim();
                placehlds = 0;
                if (containertype == SingleContainer && name.EndsWith("#"))
                {
                    placehlds = name.Length - name.TrimEnd('#').Length;
                    name = name.TrimEnd('#');
                }
                verbatim = verbatimdefault || (name.StartsWith("" + '"') && name.EndsWith("" + '"'));
                if (verbatim) { name = name.Trim('"'); }
                var foundgroup = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ts_block.CubeGrid).GetBlockGroupWithName(name);     //search either for blockgroup

                if (foundgroup != null && !nogroups)
                {
                    if (groupOrBlocknames.Length == 1 && added_txt == "" && !noname)
                    {
                        headline.Append(foundgroup.Name + ": ");
                    }
                    foundgroup.GetBlocks(Group, (x => x.CubeGrid == ts_block.CubeGrid || !nosubgrids));
                }
                else
                {
                    Group.Clear();
                }

                if (Group.Count == 0)
                {   //or for blocks with name
                    MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ts_block.CubeGrid).SearchBlocksOfName(name, Group, (x => x.CubeGrid == ts_block.CubeGrid || !nosubgrids));
                }
                for (int i = 0; i < placehlds; i++) { Group.Add(ts_block as IMyTerminalBlock); }
                if (name.ToLower().Contains(gaptag.ToLower()))
                {
                    int gap = 0;
                    if (int.TryParse(name.ToLower().Replace(gaptag.ToLower(), ""), out gap))
                    {
                        pos.X += gap;
                        Group.Clear();
                    }
                }
                else if (Group.Count < 1)
                {
                    headline.Append("No group or block with the name:\"" + name + "\"");
                }
                bool continuegroup = true;
                int place_used = 0;
                foreach (var block in Group)
                {       //loop through Group
                    if (verbatim && block != ts_block && block.CustomName != name) { continue; }
                    if (!GetBlockProperties(block, ref percent)) { continue; }
                    if (placehlds > 0 && place_used >= placehlds) { continue; }
                    if (!continuegroup || containertype != SingleContainer) { continue; }
                    if ((pos.X + 72) > viewport.Width)
                    {   //test if item fits in line
                        if (!nolinebreak)
                        {
                            if ((pos.Y + 170) > viewport.Bottom)
                            {   //test if item fits in next line
                                continuegroup = false;
                            }
                            else
                            {
                                LineStartpos.Y += 95;   //linebreak
                                pos = LineStartpos;
                            }
                        }
                        else
                        {
                            continuegroup = false;
                        }
                    }
                    if (continuegroup)
                    {
                        if (block != ts_block) { GetChargeStateValues(percent, SingleContainer); } else { fillLevelLoopcount = 0; drawpercent = "???"; }
                        DrawSingleContainer(pos, frameColor);   //draw block
                        pos.X += 72;   //position for next item in line
                        place_used++;
                    }
                }
            }
            if (sumAll[0] == 0 && added_txt != "")
            {
                if (containertype == SingleContainer) { LineStartpos.Y -= 23; pos = LineStartpos; }
                containertype = Textline;
            }
            else if (sumAll[0] > 0)
            {
                percent = (int)Math.Truncate(sumAll[1] / sumAll[0]);
            }
            switch (containertype)
            {
                case WideBar:
                    GetChargeStateValues(percent, WideBar);
                    posHeadline = pos + new Vector2(15, 14);  //place headline inside wide bar
                    DrawCargebarWide(pos, Color.White);
                    pos.X += 508;
                    if ((pos.Y + 0) > viewport.Height) { continueDrawing = false; }  // end of screen
                    break;
                case SmallBar:
                    GetChargeStateValues(percent, SmallBar);
                    posHeadline = pos + new Vector2(15, 14);  //place headline inside wide bar
                    DrawCargebarWide(pos, Color.White);
                    pos.X += 256;
                    if ((pos.Y + 0) > viewport.Height) { continueDrawing = false; }  // end of screen
                    break;
                case SingleContainer:
                    posHeadline = posContainerGroup + new Vector2(0, -23);  //place headline above containers
                    if ((pos.Y) > viewport.Height) { continueDrawing = false; }  // end of screen
                    break;
                case Textline:
                    posHeadline = LineStartpos;
                    if ((pos.Y + 23) >= viewport.Height) { continueDrawing = false; }  // end of screen
                    break;
            }
            headline.Append(added_txt);
            if (!noheadline)
            { //   build headline from collected arrays
                if (noname)
                {
                    StopSep = StartSep = "";
                }
                else
                {
                    StartSep = SepStart;
                    StopSep = SepStop;
                }
                if (sumBat[0] > 0)
                {
                    headline.Append(hlstart("Battery", sumBat[0]));
                    if (optionalView) { headline.Append("In " + TruncateUnit(sumBat[1], "watt") + "Out "); }
                    else { headline.Append(f_percent(sumBat)); }
                    headline.Append(TruncateUnit(sumBat[2], "whr") + StopSep);
                }
                if (sumReactor[0] > 0)
                {
                    headline.Append(hlstart("Reactor", sumReactor[0]));
                    if (optionalView) { headline.Append("Output " + Math.Truncate(sumReactor[1] / sumReactor[0]) + "% " + TruncateUnit(sumReactor[2], "watt") + StopSep); }
                    else { headline.Append(TruncateUnit(sumReactor[2], "kg") + "Uranium " + StopSep); }
                }
                if (sumOilEng[0] > 0)
                {
                    headline.Append(hlstart("Oil Engine", sumOilEng[0]));
                    if (optionalView) { headline.Append("Output " + f_percent(sumOilEng) + TruncateUnit(sumOilEng[2], "watt") + StopSep); }
                    else { headline.Append(f_percent(sumOilEng) + TruncateUnit(sumOilEng[2], "lit") + StopSep); }
                }
                if (sumGasgen[0] > 0)
                {
                    headline.Append(hlstart("Hydrogen Engine", sumGasgen[0]));
                    if (optionalView) { headline.Append("Output " + f_percent(sumGasgen) + TruncateUnit(sumGasgen[2], "watt") + StopSep); }
                    else { headline.Append(f_percent(sumGasgen) + TruncateUnit(sumGasgen[2], "lit") + StopSep); }
                }
                if (sumJdrives[0] > 0) { headline.Append(hlstart("Jumpdrive", sumJdrives[0]) + f_percent(sumJdrives) + TruncateUnit(sumJdrives[2], "whr") + StopSep); }
                if (sumCargo[0] > 0) { headline.Append(hlstart("Cargo", sumCargo[0]) + f_percent(sumCargo) + TruncateUnit(sumCargo[2], "c_l") + StopSep); }
                if (sumCollectors[0] > 0) { headline.Append(hlstart("Collector", sumCollectors[0]) + f_percent(sumCollectors) + TruncateUnit(sumCollectors[2], "c_l") + StopSep); }
                if (sumConnectors[0] > 0) { headline.Append(hlstart("Connector", sumConnectors[0]) + f_percent(sumConnectors) + TruncateUnit(sumConnectors[2], "c_l") + StopSep); }
                if (sumEjectors[0] > 0) { headline.Append(hlstart("Ejector", sumEjectors[0]) + f_percent(sumEjectors) + TruncateUnit(sumEjectors[2], "c_l") + StopSep); }
                if (sumDrills[0] > 0) { headline.Append(hlstart("Drill", sumDrills[0]) + f_percent(sumDrills) + TruncateUnit(sumDrills[2], "c_l") + StopSep); }
                if (sumWelder[0] > 0) { headline.Append(hlstart("Welder", sumWelder[0]) + f_percent(sumWelder) + TruncateUnit(sumWelder[2], "c_l") + StopSep); }
                if (sumGrinder[0] > 0) { headline.Append(hlstart("Grinder", sumGrinder[0]) + f_percent(sumGrinder) + TruncateUnit(sumGrinder[2], "c_l") + StopSep); }
                if (sumHydro[0] > 0) { headline.Append(hlstart("Hydrogen Tank", sumHydro[0]) + f_percent(sumHydro) + TruncateUnit(sumHydro[2], "lit") + StopSep); }
                if (sumOxy[0] > 0) { headline.Append(hlstart("Oxygen Tank", sumOxy[0]) + f_percent(sumOxy) + TruncateUnit(sumOxy[2], "lit") + StopSep); }
                if (sumOil[0] > 0) { headline.Append(hlstart("Oil Tank", sumOil[0]) + f_percent(sumOil) + TruncateUnit(sumOil[2], "lit") + StopSep); }
                if (sumOilRef[0] > 0) { headline.Append(hlstart("Oil Refinery", sumOilRef[0]) + f_percent(sumOilRef) + TruncateUnit(sumOilRef[2], "c_l") + StopSep); }
                if (sumEShield[0] > 0) { headline.Append(hlstart("Shield Generator", sumEShield[0]) + f_percent(sumEShield) + "Required Input: " + eShieldInput + " " + StopSep); }
                if (sumShield[0] > 0) { headline.Append(hlstart("Shield Controller", sumShield[0]) + Math.Truncate(sumShield[1]) + "% " + "Overheated: " + Math.Truncate(sumShield[2]) + "% " + StopSep); }
                if (sumMissileTurret[0] > 0) { headline.Append(hlstart("Missile Turret", sumMissileTurret[0]) + f_percent(sumMissileTurret) + plSing(" Missile", sumMissileTurret[2]) + " " + StopSep); }
                if (sumGatlingTurret[0] > 0) { headline.Append(hlstart("Gatling Turret", sumGatlingTurret[0]) + f_percent(sumGatlingTurret) + plSing(" Shell", sumGatlingTurret[2]) + " " + StopSep); }
                if (sumMissileLauncher[0] > 0) { headline.Append(hlstart("Rocket Launcher", sumMissileLauncher[0]) + f_percent(sumMissileLauncher) + plSing(" Rocket", sumMissileLauncher[2]) + " " + StopSep); }
                if (sumGatling[0] > 0) { headline.Append(hlstart("Gatling Gun", sumGatling[0]) + f_percent(sumGatling) + plSing(" Shell", sumGatling[2]) + " " + StopSep); }
                if (sumThrust[0] > 0) { headline.Append(hlstart("Thruster", sumThrust[0]) + f_percent(sumThrust) + "Eff. " + Math.Truncate(sumThrust[2] * 100 / sumThrust[0]) + "% Override" + StopSep); }
                if (sumSolar[0] > 0) { headline.Append(hlstart("Solar Panel", sumSolar[0]) + "Output " + f_percent(sumSolar) + TruncateUnit(sumSolar[2], "watt") + StopSep); }
                if (sumWindmill[0] > 0) { headline.Append(hlstart("Wind Turbine", sumWindmill[0]) + "Output " + f_percent(sumWindmill) + TruncateUnit(sumWindmill[2], "watt") + StopSep); }
                if (sumO2Gen[0] > 0) { headline.Append(hlstart("O2H2 Generator", sumO2Gen[0]) + f_percent(sumO2Gen) + TruncateUnit(sumO2Gen[2], "c_l") + StopSep); }
                if (sumRefinery[0] > 0)
                {
                    headline.Append(hlstart("Refinery", sumRefinery[0]));
                    if (optionalView) { headline.Append("Ingots " + f_percent(sumRefinery) + TruncateUnit(sumRefinery[2], "c_l") + StopSep); } else { headline.Append("Ores " + f_percent(sumRefinery) + TruncateUnit(sumRefinery[2], "c_l") + StopSep); }
                }
                if (sumAssembler[0] > 0)
                {
                    headline.Append(hlstart("Assembler", sumAssembler[0]));
                    if (optionalView) { headline.Append("Items Inventory " + f_percent(sumAssembler) + "Item Completion: " + Math.Truncate(sumAssembler[2] * 100 / sumAssembler[0]) + "% " + StopSep); }
                    else { headline.Append("Ingot Inventory " + f_percent(sumAssembler) + "Production Queue: " + plSing(" Item", sumAssembler[2]) + StopSep); }
                }
                if (sumProjector[0] > 0) { headline.Append(hlstart("Projector", sumProjector[0]) + f_percent(sumProjector) + "Remaining: " + plSing(" Block", sumProjector[2]) + StopSep); }
            }
            DrawHeadline(posHeadline, headline);   //draw headline
            return continueDrawing;
        }
        void DrawHeadline(Vector2 drawposHeadline, StringBuilder headline)
        {
            string headline_short = "";
            int scrollindex = 0;
            if (screenIndex < 50 && headlineIndex < 20) { scrollindex = scrollpositions[screenIndex, headlineIndex]; }
            if (noscrolling || headline.Length + 16 < scrollindex) { scrollindex = 0; }
            switch (containertype)
            {
                case WideBar:
                    if (trimmed_SB(scrollindex, headline, 440f, 1.3f, out headline_short)) { scrollindex++; }
                    AddTextSprite(frame, shifttext(drawposHeadline, 470f, textpos), headline_short, "DEBUG", 1.3f, headlineColorWideBar, textpos);
                    break;
                case SmallBar:
                    if (trimmed_SB(scrollindex, headline, 190f, 0.8f, out headline_short)) { scrollindex++; }
                    AddTextSprite(frame, shifttext(drawposHeadline, 190f, textpos), headline_short, "DEBUG", 0.8f, headlineColorWideBar, textpos);
                    break;
                case SingleContainer:
                    if (trimmed_SB(scrollindex, headline, (viewport.Width - 25 - drawposHeadline.X), 0.8f, out headline_short)) { scrollindex++; }
                    AddTextSprite(frame, shifttext(drawposHeadline, (viewport.Width - drawposHeadline.X), textpos), headline_short, "DEBUG", 0.8f, headlineColorWideBar, textpos);
                    break;
                case Textline:
                    if (trimmed_SB(scrollindex, headline, (viewport.Width - 25 - drawposHeadline.X), 0.8f, out headline_short)) { scrollindex++; }
                    AddTextSprite(frame, shifttext(drawposHeadline, (viewport.Width - drawposHeadline.X), textpos), headline_short, "DEBUG", 0.8f, headlineColor, textpos);
                    break;
            }
            headline.Clear();
            if (!noscrolling)
            {
                scrollpositions[screenIndex, headlineIndex] = scrollindex;
                headlineIndex++;
            }
        }
        bool trimmed_SB(int scrollindex, StringBuilder headline_orig, float field_width, float font_size, out string cutted_str)
        {
            if (scrollindex > (2 * headline_orig.Length + 19) || (scrollindex == 0 && surface.MeasureStringInPixels(headline_orig, "Debug", font_size).X < field_width))
            {
                cutted_str = headline_orig.ToString();
                return false;
            }
            else
            {
                StringBuilder headline_long = new StringBuilder("W");
                float temp_w = surface.MeasureStringInPixels(headline_long, "Debug", font_size).X;
                headline_long.Clear();
                headline_long = headline_orig;
                headline_long.Append(' ', 20).Append(headline_orig).Remove(0, scrollindex);
                float temp_f = 2f;
                while (temp_f > 1f)
                {
                    temp_f = (surface.MeasureStringInPixels(headline_long, "Debug", font_size).X - field_width) / temp_w;
                    headline_long.Length -= (int)temp_f;
                }
                cutted_str = headline_long.ToString();
                return true;
            }
        }
        Vector2 shifttext(Vector2 org_pos, float width, TextAlignment alignpos)
        {
            if (alignpos == TextAlignment.RIGHT) { org_pos.X += width; }
            else if (alignpos == TextAlignment.CENTER) { org_pos.X += (width / 2); }
            return org_pos;
        }
        string hlstart(string str, float number)
        {
            if (noname) { return StartSep; }
            else { return StartSep + plSing(str, number) + ": "; }
        }
        string plSing(string str, float number)
        {
            if (number != 1)
            {
                if (str.EndsWith("ry")) { return number + " " + str.Replace("ry", "ries"); }
                else { return number + " " + str + "s"; }
            }
            else { return number + " " + str; }
        }
        string TruncateUnit(float number, string unit)
        {
            string str = "";
            if (unit == "whr") { str = calculateunits(number, " T", " G", " M", " K", 85.932966f) + "Wh"; }
            if (unit == "lit") { str = calculateunits(number / 1000, "G ", "M ", "K ", " ", 85.932966f) + "L"; }
            if (unit == "c_l") { str = calculateunits(number, "G ", "M ", "K ", " ", 85.932966f) + "L"; }
            if (unit == "watt") { str = calculateunits(number, " T", " G", " M", " K", 85.932966f) + "W"; }
            if (unit == "kg") { str = calculateunits(number, "K ton", " ton", " Kg", " g", 107.10487f); }
            return str + "  ";
        }
        string calculateunits(float number, string sign1, string sign2, string sign3, string sign4, float width)
        {
            StringBuilder sb = new StringBuilder();
            if (number >= 1000000) { sb.Append(Math.Round((number / 1000000f), 2) + sign1); }
            else if (number >= 1000) { sb.Append(Math.Round((number / 1000f), 2) + sign2); }
            else if (number >= 1) { sb.Append(Math.Round(number, 2) + sign3); }
            else { sb.Append(Math.Round((number * 1000f), 2) + sign4); }
            float temp_f = ((width - surface.MeasureStringInPixels(sb, "Debug", 0.8f).X) / 4.981622f); // + 0.5f;
            sb.Insert(0, "                     ".Substring(0, (int)temp_f));
            return sb.ToString();
        }
        string f_percent(float[] werte)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Math.Round(TestForNaN(werte[1] / werte[0]), 0) + "% ");
            float temp_f = ((49.193516f - surface.MeasureStringInPixels(sb, "Debug", 0.8f).X) / 4.981622f); // + 0.5f;
            sb.Insert(0, "                     ".Substring(0, (int)temp_f));
            return sb.ToString();
        }
        bool GetBlockProperties(IMyTerminalBlock block, ref float percent)
        {
            invertColor = false;
            bool isValidBlock = false;
            string blocktype = "";
            if (block.BlockDefinition.SubtypeName.Contains("DSControl"))
            {
                sumShield[0]++; sumAll[0]++;
                float.TryParse(getBetween(block.CustomInfo, " (", "%)"), out percent);
                sumShield[1] = percent; sumAll[1] += percent;
                float.TryParse(getBetween(block.CustomInfo, "[Over Heated]: ", "%"), out sumShield[2]);
                icon = iconShield;
                pattern = patternShield;
                SetFrameColorAndValidBlock(block, ref isValidBlock);
            }
            else if (block.BlockDefinition.SubtypeName.Contains("ShieldGenerator"))
            {
                if (!block.CustomName.Contains(":")) { block.CustomName = block.CustomName + ":"; }
                sumEShield[0]++; sumAll[0]++;
                percent = TestForNaN(100 / EnergyShield.MaxHitpoints(block) * EnergyShield.CurrentHitpoints(block));
                sumEShield[1] = percent; sumAll[1] += percent;
                eShieldInput = EnergyShield.RequiredInput(block);
                icon = iconShield;
                pattern = patternShield;
                SetFrameColorAndValidBlock(block, ref isValidBlock);
            }
            else if (block == ts_block)
            {
                icon = "Cross";
                pattern = patternNone;
                frameColor = Color.Orange;
                percent = 0;
                isValidBlock = true;
            }
            else
            {
                blocktype = block.ToString().Remove(block.ToString().IndexOf('{')).Trim();
            }
            switch (blocktype)
            {
                case "MyBatteryBlock":
                    var bat = block as IMyBatteryBlock;
                    sumBat[0]++;
                    sumAll[0]++;
                    percent = 100 / bat.MaxStoredPower * bat.CurrentStoredPower;
                    sumAll[1] += percent;
                    if (optionalView)
                    {
                        sumBat[1] += bat.CurrentInput;
                        sumBat[2] += bat.CurrentOutput;
                    }
                    else
                    {
                        sumBat[1] += percent;
                        sumBat[2] += bat.CurrentStoredPower;
                    }
                    icon = "IconEnergy";
                    pattern = patternBattery;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyReactor":
                    if (optionalView)
                    {
                        invertColor = true;
                        PowerOutput2Arr(block, sumReactor, ref percent);
                    }
                    else
                    {
                        ItemCount2Arr(block, sumReactor, ref percent);
                    }
                    icon = iconReactor;
                    pattern = patternReactor;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyHydrogenEngine":
                    if (optionalView)
                    {
                        PowerOutput2Arr(block, sumGasgen, ref percent);
                        icon = "IconEnergy";
                    }
                    else
                    {
                        if (block.BlockDefinition.SubtypeName.Contains("Oil"))
                        {
                            sumOilEng[0]++;
                            sumAll[0]++;
                            percent = 100 / GasGen.MaxVolume(block) * GasGen.CurrentVolume(block);
                            sumOilEng[1] += percent;
                            sumAll[1] += percent;
                            sumOilEng[2] += GasGen.CurrentVolume(block);
                            icon = iconOil;
                        }
                        else
                        {
                            sumGasgen[0]++;
                            sumAll[0]++;
                            percent = 100 / GasGen.MaxVolume(block) * GasGen.CurrentVolume(block);
                            sumGasgen[1] += percent;
                            sumAll[1] += percent;
                            sumGasgen[2] += GasGen.CurrentVolume(block);
                            icon = "IconHydrogen";
                        }
                    }
                    pattern = patternGasgen;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyCargoContainer":
                    CargoVolume2Arr(block, sumCargo, ref percent);
                    icon = iconCargo;
                    pattern = patternCargo;
                    invertColor = true;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyGasTank":
                    var tank = block as IMyGasTank;
                    if (block.BlockDefinition.SubtypeName.Contains("Hydro"))
                    {
                        sumHydro[0]++;
                        sumAll[0]++;
                        percent = (float)Math.Round(tank.FilledRatio * 100);
                        sumHydro[1] += percent;
                        sumAll[1] += percent;
                        sumHydro[2] += (float)tank.Capacity * (float)tank.FilledRatio;
                        icon = "IconHydrogen";
                    }
                    else if (block.BlockDefinition.SubtypeName.Contains("Oil"))
                    {
                        sumOil[0]++;
                        sumAll[0]++;
                        percent = (float)Math.Round(tank.FilledRatio * 100);
                        sumOil[1] += percent;
                        sumAll[1] += percent;
                        sumOil[2] += (float)tank.Capacity * (float)tank.FilledRatio;
                        icon = iconOil;
                    }
                    else
                    {
                        sumOxy[0]++;
                        sumAll[0]++;
                        percent = (float)Math.Round(tank.FilledRatio * 100);
                        sumOxy[1] += percent;
                        sumAll[1] += percent;
                        sumOxy[2] += (float)tank.Capacity * (float)tank.FilledRatio;
                        icon = "IconOxygen";
                    }
                    pattern = patternTank;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyShipConnector":
                    if (block.BlockDefinition.SubtypeName.Contains("Large") || block.BlockDefinition.SubtypeName.Contains("Small"))
                    { // Ejector: ConnectorLarge_SEBuilder4, ConnectorSmall
                        CargoVolume2Arr(block, sumEjectors, ref percent);
                        icon = iconEjector;
                    }
                    else
                    { // Connector
                        CargoVolume2Arr(block, sumConnectors, ref percent);
                        icon = iconConnector;
                    }
                    pattern = patternConnector;
                    invertColor = true;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyCollector":
                    CargoVolume2Arr(block, sumCollectors, ref percent);
                    icon = iconCollector;
                    pattern = patternConnector;
                    invertColor = true;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyShipDrill":
                    CargoVolume2Arr(block, sumDrills, ref percent);
                    icon = iconDrill;
                    pattern = patternTool;
                    invertColor = true;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyJumpDrive":
                    var jumpdrive = block as IMyJumpDrive;
                    sumJdrives[0]++;
                    sumAll[0]++;
                    percent = 100 / jumpdrive.MaxStoredPower * jumpdrive.CurrentStoredPower;
                    sumJdrives[1] += percent;
                    sumAll[1] += percent;
                    sumJdrives[2] += jumpdrive.CurrentStoredPower;
                    icon = iconJumpdrive;
                    pattern = patternJumpdrive;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyThrust":
                    var thruster = block as IMyThrust;
                    sumThrust[0]++;
                    sumAll[0]++;
                    percent = 100 / thruster.MaxEffectiveThrust * thruster.CurrentThrust;
                    if (percent > 100) { percent = 0; }
                    sumThrust[1] += percent;
                    sumAll[1] += percent;
                    sumThrust[2] += thruster.ThrustOverridePercentage;
                    icon = iconThruster;
                    pattern = patternThruster;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MySolarPanel":
                    PowerOutput2Arr(block, sumSolar, ref percent);
                    icon = iconSolar;
                    pattern = patternSolar;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyWindTurbine":
                    PowerOutput2Arr(block, sumWindmill, ref percent);
                    icon = iconWindmill;
                    pattern = patternWindmill;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyGasGenerator":
                    if (block.BlockDefinition.SubtypeName.Contains("OilRefinery"))
                    {
                        sumOilRef[0]++;
                        sumAll[0]++;
                        percent = 100 / (float)block.GetInventory().MaxVolume * (float)block.GetInventory().CurrentVolume;
                        sumOilRef[1] += percent;
                        sumAll[1] += percent;
                        sumOilRef[2] += (float)block.GetInventory().CurrentVolume;
                        icon = iconOil;
                        pattern = patternGasgen;
                        SetFrameColorAndValidBlock(block, ref isValidBlock);
                    }
                    else
                    {
                        sumO2Gen[0]++;
                        sumAll[0]++;
                        percent = 100 / (float)block.GetInventory().MaxVolume * (float)block.GetInventory().CurrentVolume;
                        sumO2Gen[1] += percent;
                        sumAll[1] += percent;
                        sumO2Gen[2] += (float)block.GetInventory().CurrentVolume;
                        icon = iconIce;
                        pattern = patternGasgen;
                        SetFrameColorAndValidBlock(block, ref isValidBlock);
                    }
                    break;
                case "MyShipGrinder":
                    CargoVolume2Arr(block, sumGrinder, ref percent);
                    icon = iconGrinder;
                    pattern = patternTool;
                    invertColor = true;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyShipWelder":
                    CargoVolume2Arr(block, sumWelder, ref percent);
                    icon = iconWelder;
                    pattern = patternTool;
                    invertColor = true;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyRefinery":
                    IMyInventory Ref_inv = null;
                    var Refblock = block as IMyRefinery;
                    if (!optionalView)
                    {
                        Ref_inv = Refblock.InputInventory;
                    }
                    else
                    {
                        Ref_inv = Refblock.OutputInventory;
                    }
                    sumRefinery[0]++;
                    sumAll[0]++;
                    percent = 100 / (float)Ref_inv.MaxVolume * (float)Ref_inv.CurrentVolume;
                    sumRefinery[1] += percent;
                    sumAll[1] += percent;
                    sumRefinery[2] += (float)Ref_inv.CurrentVolume;
                    icon = iconRefinery;
                    pattern = patternRefinery;
                    invertColor = true;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyAssembler":
                    IMyInventory inventory = null;
                    var Assblock = block as IMyAssembler;
                    if (!optionalView)
                    {
                        inventory = Assblock.InputInventory;
                        List<MyProductionItem> queue = new List<MyProductionItem>();
                        Assblock.GetQueue(queue);
                        foreach (var item in queue) { sumAssembler[2] += ((float)item.Amount); }
                    }
                    else
                    {
                        inventory = Assblock.OutputInventory;
                        sumAssembler[2] += Assblock.CurrentProgress;
                    }
                    sumAssembler[0]++;
                    sumAll[0]++;
                    percent = 100 / (float)inventory.MaxVolume * (float)inventory.CurrentVolume;
                    sumAssembler[1] += percent;
                    sumAll[1] += percent;
                    icon = iconAssembler;
                    pattern = patternAssembler;
                    invertColor = true;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyLargeMissileTurret":
                    if (block.BlockDefinition.SubtypeName.StartsWith("MA_Designator")) break;
                    ItemCount2Arr(block, sumMissileTurret, ref percent);
                    icon = iconRocket;
                    pattern = patternTurret;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MyLargeGatlingTurret":
                case "MyLargeInteriorTurret":
                    ItemCount2Arr(block, sumGatling, ref percent);
                    icon = iconBullet;
                    pattern = patternTurret;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MySmallGatlingGun":
                    ItemCount2Arr(block, sumGatlingTurret, ref percent);
                    icon = iconBullet;
                    pattern = patternTurret;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MySmallMissileLauncher":
                case "MySmallMissileLauncherReload":
                    ItemCount2Arr(block, sumMissileLauncher, ref percent);
                    icon = iconRocket;
                    pattern = patternTurret;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
                case "MySpaceProjector":
                    var projector = block as IMyProjector;
                    var total = projector.TotalBlocks;
                    sumProjector[0]++;
                    sumAll[0]++;
                    if (total != 0) { percent = 100 / total * (total - projector.RemainingBlocks); }
                    sumProjector[1] += percent;
                    sumAll[1] += percent;
                    sumProjector[2] += projector.RemainingBlocks;
                    icon = iconProjector;
                    pattern = patternProjector;
                    SetFrameColorAndValidBlock(block, ref isValidBlock);
                    break;
            }
            return isValidBlock;
        }
        void PowerOutput2Arr(IMyTerminalBlock block, float[] arr, ref float percent)
        {
            var powProd = block as IMyPowerProducer;
            arr[0]++;
            sumAll[0]++;
            percent = TestForNaN(100 / powProd.MaxOutput * powProd.CurrentOutput);
            arr[1] += percent;
            sumAll[1] += percent;
            arr[2] += powProd.CurrentOutput;
        }
        void ItemCount2Arr(IMyTerminalBlock block, float[] arr, ref float percent)
        {
            arr[0]++;
            sumAll[0]++;
            percent = 100 / (float)block.GetInventory().MaxVolume * (float)block.GetInventory().CurrentVolume;
            arr[1] += percent;
            sumAll[1] += percent;
            arr[2] += itemCount(block);
        }
        void CargoVolume2Arr(IMyTerminalBlock block, float[] arr, ref float percent)
        {
            var inventory = block.GetInventory();
            if (inventory != null)
            {
                arr[0]++;
                sumAll[0]++;
                percent = 100 / (float)inventory.MaxVolume * (float)inventory.CurrentVolume;
                arr[1] += percent;
                sumAll[1] += percent;
                arr[2] += (float)inventory.CurrentVolume;
            }
        }
        float TestForNaN(float number)
        {
            var num = number;
            if (number != num)
            {
                return 0;
            }
            else
            {
                return number;
            }
        }
        void SetFrameColorAndValidBlock(IMyTerminalBlock block, ref bool isValidBlock)
        {
            if (block.IsFunctional) { frameColor = frameColorFunctional; } else { frameColor = frameColorNotFunctional; }
            isValidBlock = true;
        }
        void ClearGroupArrays()
        {
            ClearArray(sumBat);
            ClearArray(sumJdrives);
            ClearArray(sumCargo);
            ClearArray(sumHydro);
            ClearArray(sumOxy);
            ClearArray(sumOil);
            ClearArray(sumOilRef);
            ClearArray(sumOilEng);
            ClearArray(sumCollectors);
            ClearArray(sumConnectors);
            ClearArray(sumEjectors);
            ClearArray(sumDrills);
            ClearArray(sumGrinder);
            ClearArray(sumWelder);
            ClearArray(sumGasgen);
            ClearArray(sumShield);
            ClearArray(sumGatling);
            ClearArray(sumMissileTurret);
            ClearArray(sumGatlingTurret);
            ClearArray(sumMissileLauncher);
            ClearArray(sumThrust);
            ClearArray(sumReactor);
            ClearArray(sumSolar);
            ClearArray(sumWindmill);
            ClearArray(sumO2Gen);
            ClearArray(sumRefinery);
            ClearArray(sumAssembler);
            ClearArray(sumProjector);
            ClearArray(sumEShield);
            ClearArray(sumAll);
        }
        void ClearArray(Array arr)
        {
            Array.Clear(arr, 0, arr.Length);
        }
        float itemCount(IMyTerminalBlock block)
        {
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            block.GetInventory().GetItems(items);
            float count = 0;
            foreach (var item in items) { count += item.Amount.RawValue / 1000000; }
            return count;
        }
        void GetChargeStateValues(float percent, int containertype)
        {
            switch (containertype)
            {
                case WideBar:
                    chargeBarInitialOffset = new Vector2(14, 15);
                    chargeBarOffset = new Vector2(48, 0);
                    chargeBar = chargebarWide;
                    pattern = patternWideBar;
                    fillLevelLoopcount = (int)Math.Round(percent * 0.1);
                    break;
                case SmallBar:
                    chargeBarInitialOffset = new Vector2(12, 12);
                    chargeBarOffset = new Vector2(22, 0);
                    chargeBar = chargebarSmall;
                    pattern = patternSmallBar;
                    fillLevelLoopcount = (int)Math.Round(percent * 0.1);
                    break;
                case SingleContainer:
                    fillLevelLoopcount = (int)Math.Round(percent * 0.05);
                    drawpercent = Convert.ToString(Math.Truncate(percent)) + '%';
                    break;
            }
            if (percent > 90) { if (invertColor) { fillLevelColor = ChargeColorEmpty; } else fillLevelColor = ChargeColorLoaded; }
            else if (percent > 25) { fillLevelColor = ChargeColorNormal; }
            else { if (invertColor) { fillLevelColor = ChargeColorLoaded; } else fillLevelColor = ChargeColorEmpty; }
        }
        static string getBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }
        void DrawCargebarWide(Vector2 pos, Color framecolor)
        {
            var chargebarpos = pos + chargeBarInitialOffset;
            //Add pattern image
            AddMonoSprite(frame, pos, pattern, frameColorWideBar, TextAlignment.LEFT);
            //Add chargestate bar
            for (int i = 0; i < fillLevelLoopcount; i++)
            {
                AddMonoSprite(frame, chargebarpos, chargeBar, fillLevelColor, TextAlignment.LEFT);
                chargebarpos += chargeBarOffset;
            }
        }
        void DrawSingleContainer(Vector2 pos, Color framecolor)
        {
            pos.X += 33;
            var chargebarpos = pos + new Vector2(0, 70);
            //Add pattern image
            AddMonoSprite(frame, pos, pattern, framecolor, TextAlignment.CENTER);
            //Add chargestate bar
            for (int i = 0; i < fillLevelLoopcount; i++)
            {
                AddMonoSprite(frame, chargebarpos, chargebarSC, fillLevelColor, TextAlignment.CENTER);
                chargebarpos.Y -= 13.6f;
            }
            //Add icon
            pos.Y += 40;
            if (icon.Contains("\n")) { AddMonoSprite(frame, pos + new Vector2(0, -23), icon, pictogramColor.Alpha(0.4f), TextAlignment.CENTER); } else { AddTextureSprite(frame, icon, pos, new Vector2(30, 30), pictogramColor, TextAlignment.CENTER); }
            //Add chargestate number %
            pos.Y += 20;

            AddTextSprite(frame, pos, drawpercent, "DEBUG", 0.75f, percentDisplayColor, TextAlignment.CENTER);
        }
        void AddTextureSprite(MySpriteDrawFrame frame, string picture, Vector2 pos, Vector2 size, Color color, TextAlignment alignment)
        {
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = picture,
                Position = pos,
                Size = size,
                Color = color,
                Alignment = alignment
            };
            frame.Add(sprite);
        }
        void AddTextSprite(MySpriteDrawFrame frame, Vector2 pos, string str, string font, float size, Color color, TextAlignment alignment)
        {
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = str,
                Position = pos,
                RotationOrScale = size,
                Color = color,
                Alignment = alignment,
                FontId = font
            };
            frame.Add(sprite);
        }
        void AddMonoSprite(MySpriteDrawFrame frame, Vector2 pos, string str, Color color, TextAlignment alignment)
        {
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = str,
                Position = pos,
                RotationOrScale = 0.1f,
                Color = color,
                Alignment = alignment,
                FontId = "Monospace"
            };
            frame.Add(sprite);
        }
        void PrepareTextSurface(IMyTextSurface surface)
        {
            AddTextSprite(frame, new Vector2(-100, -100), Background, "Monospace", 10.0f, Color.Black, TextAlignment.LEFT);
            AddTextureSprite(frame, "SquareSimple", viewport.Center, viewport.Size, Color.Black, TextAlignment.CENTER);
        }
        void GeneratePatterns()
        {
            patternWideBar = xstr("", 174) + "\n" + xstr("", 172) + "\n" + xstr("", 170) + "\n" + xstr("", 168) + "\n" + xstr("" + xstr("", 166) + "\n", 17) + "" + xstr("", 168) + "\n" + xstr("", 170) + "\n" + xstr("", 172) + "\n" + xstr("", 174);
            patternSmallBar = xstr("", 85) + "\n" + xstr("", 83) + "\n" + xstr("", 81) + "\n" + xstr("" + xstr("", 79) + "\n", 12) + "" + xstr("", 81) + "\n" + xstr("", 83) + "\n" + xstr("", 85);
            chargebarSmall = xstr("\n", 10).Trim();
            chargebarSC = xstr(xstr("", 15) + "\n", 3).Trim();
            Background = xstr(xstr("", 10), 5).Trim();

            string[] LineDecoded = decodeLine1(13, "AaaAcmAgeAkmAmyAtcAuaAwmBgyBncBnoCumDaqGbpGebGkfGurGybHgbHpdJcfLjz");
            iconIce = decodedPattern(LineDecoded, "VNOPBHMHBPONV");
            iconWindmill = decodedPattern(LineDecoded, "VNNQBJBJBQNNV");
            iconSolar = decodedPattern(LineDecoded, "VNOREALAERONV");
            iconGrinder = decodedPattern(LineDecoded, "VNQSHIFMKTUNV");
            iconRefinery = decodedPattern(LineDecoded, "VNNQCCDGGQNNV");

            LineDecoded = decodeLine1(14, "AaaAhkAsmAuiAkmAfoBbsAbwBfkBmuDbcBnsBxoBoyByuCgeCiaBncDamGbqGjaGmsGvyHokJccKqgLkoMddMknMofMvpNpxMebPdpSenVfrVnbVqtWtjYgd");
            iconOil = decodedPattern(LineDecoded, "ACDDDDCDDDDCA");
            iconReactor = decodedPattern(LineDecoded, "aTTXDDJFEWTTa");
            iconThruster = decodedPattern(LineDecoded, "mbbbJJBBBBbbbm");
            iconCollector = decodedPattern(LineDecoded, "ZTUGCBAQSLYTZ");
            iconEjector = decodedPattern(LineDecoded, "ZTYLQJABGCUTZ");
            iconCargo = decodedPattern(LineDecoded, "mbbfJHHHHJfbbm");
            iconConnector = decodedPattern(LineDecoded, "ZTYLQJAJQLYTZ");
            iconDrill = decodedPattern(LineDecoded, "jcbCAJAJASAibj");
            iconJumpdrive = decodedPattern(LineDecoded, "lddBAkOOkABddl");
            iconShield = decodedPattern(LineDecoded, "jbhLPNPMPLIebj");
            iconBullet = decodedPattern(LineDecoded, "mbbbARNNRAbbbm");
            iconRocket = decodedPattern(LineDecoded, "mbbgHSKKSHgbbm");
            iconWelder = decodedPattern(LineDecoded, "ZTVDDDSLLLYTZ");
            iconAssembler = decodedPattern(LineDecoded, "mbbdCBBBBCdbbm");
            iconProjector = decodedPattern(LineDecoded, "mbbhAJACAJdebm");
            chargebarWide = decodedPattern(LineDecoded, "nnnnnnnnnnnnnnn");

            LineDecoded = decodeLine2("AAAAf4AAH//8IAACP+P+P//+QAABQf/BYAADbbbbcAAHeD4PfgA/f8H/f+P/f///");
            patternBattery = decodedPattern(LineDecoded, "BPPIIIIIIIIIIIIIIIIIIIIIIIIIIPP");
            patternCargo = decodedPattern(LineDecoded, "ALPIIIIDDDDDDIIIIIIDDDDDDIIIIPL");
            patternTank = decodedPattern(LineDecoded, "AFPIIIIIIDDDDDDDDDDDDDIIIIIIIPF");
            patternConnector = decodedPattern(LineDecoded, "APPGGGIIIIIIIGGIGGIIIIIIIIGGGPP");
            patternTool = decodedPattern(LineDecoded, "APPDDIIDDIIDDIIDDIIDDIIIIIIIIPP");
            patternJumpdrive = decodedPattern(LineDecoded, "APHGGGGIIIIIIIIIIIIIIIIIIGGGGHP");
            patternGasgen = decodedPattern(LineDecoded, "APPGGIIGGIIIIIIIIIIIIIIGGIIGGPP");
            patternShield = decodedPattern(LineDecoded, "AFPKIIAIIAIIAIIIIIIAIIAIIAIIKPF");
            patternTurret = decodedPattern(LineDecoded, "APPGGGGIIIIIIIIIIIIIIIIIIGGGGPP");
            patternThruster = decodedPattern(LineDecoded, "AFPIIIDDDDDDDDDDDDIIIIIIIIIIIJJ");
            patternReactor = decodedPattern(LineDecoded, "APNIIIIIIIIIIGGGGGIIIIIIIIIIINP");
            patternSolar = decodedPattern(LineDecoded, "APPDDDDDDDDDDDDDDDDDDDDDDDDDDPP");
            patternWindmill = decodedPattern(LineDecoded, "APPGGGIDDDIGGGIDDDIGGGIDDDIGGPP");
            patternNone = decodedPattern(LineDecoded, "ACFKIIIIIIIIIIIIIIIIIIIIIIIIKFC");
            patternRefinery = decodedPattern(LineDecoded, "AFPGGGGGGGGGGGIIIDDIIDDIIDDIIPP");
            patternAssembler = decodedPattern(LineDecoded, "AFMIIIDDDDDDDDIIIIIIGIGIGIGIGPP");
            patternProjector = decodedPattern(LineDecoded, "AEOKIIIIIIIIIIAAAIIIIIIIIIIIKOE");
        }
        string[] decodeLine1(int patternWidth, string sourcePattern)
        {
            string[] tempLine = new string[sourcePattern.Length / 3];
            int j = 0;
            for (int i = 0; i < sourcePattern.Length; i++)
            {
                int temp = ((int)sourcePattern[i++] - 65) * 676
                         + ((int)sourcePattern[i++] - 97) * 26
                         + ((int)sourcePattern[i] - 97);
                tempLine[j++] = number2line(patternWidth, temp);
            }
            return tempLine;
        }
        string[] decodeLine2(string sourcePattern)
        {
            string[] tempLine = new string[sourcePattern.Length / 4];
            int j = 0;
            for (int i = 0; i < sourcePattern.Length; i++)
            {
                tempLine[j++] = number2line(6, b64dec(sourcePattern[i + 3]))
                              + number2line(6, b64dec(sourcePattern[i + 2]))
                              + number2line(6, b64dec(sourcePattern[i + 1]))
                              + number2line(5, b64dec(sourcePattern[i]));
                i += 3;
            }
            return tempLine;
        }
        string number2line(int patternWidth, int temp)
        { //convert decimalnumber to binary pixel line
            string tempLine = "";
            for (int i = 0; i < patternWidth; i++)
            {
                if (temp % 2 != 0) { tempLine += ""; }
                else { tempLine += ""; }
                temp /= 2;
            }
            return tempLine;
        }
        string decodedPattern(string[] tempLine, string tempPattern)
        {
            StringBuilder tempstring = new StringBuilder();
            for (int i = 0; i < tempPattern.Length; i++) { tempstring.AppendLine(tempLine[b64dec(tempPattern[i])]); }
            return tempstring.ToString().Trim();
        }
        string xstr(string str, int repeat)
        {
            StringBuilder tempstring = new StringBuilder();
            for (int i = 0; i < repeat; i++) { tempstring.Append(str); }
            return tempstring.ToString();
        }
        int b64dec(char chr)
        { //Base64 in dec
            return "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".IndexOf(chr);
        }
        bool getVector(string option, string tagstart, out Vector2 tempvector, out float gfx_res)
        {
            tempvector = new Vector2(0, 0);
            gfx_res = 0.1f;
            var xyz = getBetween(option, tagstart.ToLower() + "(", ")").Split(',').ToArray();
            if (xyz.Length == 2)
            {
                return float.TryParse(xyz[0], out tempvector.X) && float.TryParse(xyz[1], out tempvector.Y);
            }
            else if (xyz.Length == 3)
            {
                return float.TryParse(xyz[0], out tempvector.X) && float.TryParse(xyz[1], out tempvector.Y) && float.TryParse(xyz[2], out gfx_res);
            }
            else return false;
        }

        const int Nothing = 0;
        const int SmallBar = 1;
        const int WideBar = 2;
        const int SingleContainer = 3;
        const int Textline = 4;

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;
    }
}
