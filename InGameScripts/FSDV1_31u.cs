using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using System.Linq;

namespace ScriptFSD
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        //  ****************************************************************************************************************************************
        string ver = "FSD Version 31u";
        //  ****************************************************************************************************************************************
        //  Config
        //  ****************************************************************************************************************************************

        const string lcdtag = "ShowStats";       // Tag the script reacts to - write it to the custom data of your screen/cockpit
        const string endtag = "EndStats";          // Tag to end the command list (optional)
        const string def_tag = "FSD options:";    // Tag to switch the default options in the CustomData of the programming block
        const string FpM_tag = "framerate=";       // Tag to set the frame rate in the CustomData of the programming block
        const string LpM_tag = "layoutrate=";       // Tag to set the layout change rate in the CustomData of the programming block
        const string sequ_tag = "sequence";         // Tag define the screen layout sequence in the CustomData of the programming block
        const string fast_tag = "fastmode";          // Tag to activate the fastmode in the CustomData of the programming block
        const string panel_tag = "Panel ";               // Tag to switch the default options in the CustomData of the programming block
        const string nostatustag = "NoStatus";          // Tag to not to display FSD status on the PB
        const string gfxStart = "gfxStart";             //Tag to indicate the start of a graphic sprite
        const string gfxEnd = "gfxEnd";              //Tag to indicate the end of a graphic sprite
        const string widebartag = "WideBar";            // Tag to display a large chargebar instead of each block individually
        const string smallbartag = "SmallBar";           // Tag to display a small chargebar instead of each block individually
        const string singleicontag = "SingleIcon";        // Tag to display only a single icon instead of each block individually
        const string noicontag = "noIcons";            //Tag to display only the headlines without any graphics
        const string gaptag = "Gap";                   // Tag to display a gap between two blocks
        const string positiontag = "Position";            // Tag to position the next blocks
        const string texttag = "Text:";                  //Tag to display a custom text
        const string nosubgridLCDtag = "nosubgridLCDs"; // Tag to exclude subgrid LCDs
        const string verbatimtag = "verbatim";            // Tag to require exact Name match

        string[] PaintTag = {
  "IconColor",       // Tag to repaint the icon
  "TextColor",       // Tag to repaint the texts
  "PercentColor", // Tag to repaint the percent number
  "FrameColor",   // Tag to repaint the frame
  "BarColor"         // Tag to repaint the fill level bar
};

        string[] aligntag = {
  "left",      // Tag to display text left aligned
  "center", // Tag to display text center aligned
  "right"     // Tag to display text right aligned
};

        string[] opt_tag = {
  "optional",       // Tag to display either power or inventory if a block has both (in case of battery it's load percentage or input/output)
  "addinfo",        // Tag to display additional informations
  "addsize",       // Tag to display size informations
  "health",          // Tag to display additional healthbar
  "noheadline",  // Tag to display the blocks without headline
  "nosubgrids",  // Tag to exclude subgrids
  "nogroups",     //] Tag to skip groups in the detection
  "nolinebreak", // Tag to prevent linebreaks
  "noscrolling",  // Tag to prevent scrolling
  "noNames"      // Tag to display only the headlines without the names
};

        // The Separators. Used to indicate where the option keyword part begins
        char[] seperators = { '\n', ',', ':' };

        int FpM = 60; //How often the displays will be updated (In frames per minute)
        int LpM = 12;  //How often the display layouts switch  (In changes per minute)

        // The Default COLORS. Any RGB-Color could be used from Black new Color(0, 0, 0) to White new Color(255, 255, 255)
        Color frameColorIsNotThere = new Color(30, 30, 30); //color if block is not there
        Color frameColorIsUnfunctional = Color.DarkRed;       //color if block is not funktional
        Color frameColorIsFunctional = new Color(0, 70, 70);        //color if block is funktional but not working
        Color frameColorIsWorking = Color.Cyan;          //color if block is working
        Color frameColorWideBar = Color.Gray;      //color wide bar
        Color headlineColor = Color.White;      //headline color
        Color pictogramColor = Color.White;      //symols on single blocks
        Color optioncolor = Color.Silver;      //optional information on single blocks
        Color percentDisplayColor = Color.White;      //fillstate percentvalue on single blocks

        //  ****************************************************************************************************************************************
        //  Config  End - Do not change anything below
        //  ****************************************************************************************************************************************
        int execCounter1 = 0, execCounter2 = 0, bsize, rotatecount = 0, containertype, Layout = 0, LayoutCount = 0, LayoutIndex = 0, TpM = 60, screenIndex, headlineIndex, placehlds, fillLevelLoopcount, chargeBarOffset, sumBlock;
        int[] Layouts;
        int[,] scrollpositions = new int[50, 20];
        string[] sArgs, lines, sections;
        string argument, pattern, icon, drawpercent, optionchoice, Displayname, oldCustomData = "", eShieldInput = "", added_txt = "", temp_txt, iconAssembler, iconBlock, iconBullet, iconCargo, iconCollector, iconConnector, iconCockpit, iconDoor, iconDrill, iconEjector, iconEngine, iconGrinder, iconIce, iconJumpdrive, iconPara, iconProjector, iconReactor, iconRefinery, iconRocket, iconShield, iconSolar, iconSorter, iconThruster, iconVent, iconWelder, iconWindmill, patternAssembler, patternBattery, patternCargo, patternConnector, patternGasgen, patternJumpdrive, patternNone, patternProjector, patternReactor, patternRefinery, patternShield, patternSmallBar, patternSolar, patternTank, patternThruster, patternTool, patternTurret, patternVent, patternWideBar, patternWindmill, Background, StartTag, StartSep = "[ ", SepStart = "[ ", StopSep = " ]", SepStop = " ]";
        StringBuilder gfxSprite = new StringBuilder();
        bool[] opt_def = new bool[10], opt_chc = new bool[10], paints = new bool[5];
        bool get_sprite, invertColor, verbatim, verbatimdefault, nosubgridLCDs, nostatus;
        TextAlignment textpos = L_align, textposdefault = L_align;
        Color integrityLevelColor;
        Color[] Colors = new Color[6];
        float[] sumBat = new float[3], sumJdrives = new float[3], sumCargo = new float[3], sumCockpit = new float[3], sumPara = new float[3], sumSorter = new float[3], sumHydro = new float[3], sumOxy = new float[3], sumDoor = new float[2], sumVent = new float[2], sumCollectors = new float[3], sumConnectors = new float[3], sumEjectors = new float[3], sumDrills = new float[3], sumGrinder = new float[3], sumWelder = new float[3], sumGasgen = new float[3], sumShield = new float[3], sumGatling = new float[3], sumMissileTurret = new float[3], sumGatlingTurret = new float[3], sumMissileLauncher = new float[3], sumThrust = new float[3], sumReactor = new float[3], sumSolar = new float[3], sumWindmill = new float[3], sumO2Gen = new float[3], sumRefinery = new float[3], sumAssembler = new float[3], sumProjector = new float[3], sumEShield = new float[2], sumAll = new float[2];
        HashSet<IMyCubeGrid> ignoredGrids = new HashSet<IMyCubeGrid>();
        List<IMyTerminalBlock> surfaceProviders = new List<IMyTerminalBlock>(), Group = new List<IMyTerminalBlock>(), subgroup = new List<IMyTerminalBlock>();
        IMyTextSurface surface;
        RectangleF viewport;
        UpdateFrequency speed = UpdateFrequency.Update100;
        MySpriteDrawFrame frame = new MySpriteDrawFrame();
        Vector2 chargeBarSize, chargeBarInitialOffset, LineStartpos, pos, tempvector;

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

        Program()
        {
            GeneratePatterns();
            if (!Me.CustomName.EndsWith("[FSD]")) { Me.CustomName += " [FSD]"; }
            Runtime.UpdateFrequency = speed;
        }
        void Save() { }

        void Main(string preargument, UpdateType updateType)
        {
            if (Storage == "offline" && preargument == "") { argument = "shutdown"; } else { argument = preargument.ToLower(); }
            if (FpM < 1) { FpM = 1; }
            execCounter1++;
            if (execCounter1 >= TpM / FpM) { execCounter1 = 0; }
            switch (argument)
            {
                case "layout+":
                    layoutswitch(+1);
                    break;
                case "layout-":
                    layoutswitch(-1);
                    break;
                default:
                    if ((LayoutCount > 1) && (LpM > 0))
                    {
                        execCounter2++;
                        if (execCounter2 >= TpM / LpM) { layoutswitch(+1); }
                    }
                    break;
            }
            if ((argument == "") && (execCounter1 != 0)) { return; }
            int surfaceIndex;
            screenIndex = 0;
            temp_txt = "";
            if ((oldCustomData != Me.CustomData) && (Me.CustomData.Contains(def_tag)))
            {
                ClearArray(opt_chc);
                LayoutCount = 0;
                temp_txt = getBetween((Me.CustomData + "\n"), def_tag, "\n").ToLower() + " ";
                nosubgridLCDs = temp_txt.Contains(nosubgridLCDtag.ToLower());
                verbatimdefault = temp_txt.Contains(verbatimtag.ToLower());
                opt_def = wordparse(opt_tag, opt_chc, temp_txt);
                nostatus = temp_txt.Contains(nostatustag.ToLower()) || Me.CustomData.Contains(lcdtag);
                textposdefault = get_align(temp_txt, L_align);
                if (temp_txt.Contains(FpM_tag.ToLower())) { int.TryParse(getBetween(temp_txt, FpM_tag, " "), out FpM); }
                if (temp_txt.Contains(LpM_tag.ToLower())) { int.TryParse(getBetween(temp_txt, LpM_tag, " "), out LpM); }
                if (temp_txt.Contains(fast_tag.ToLower()))
                {
                    TpM = 600;
                    speed = UpdateFrequency.Update10;
                }
                else
                {
                    TpM = 60;
                    speed = UpdateFrequency.Update100;
                }
                if (temp_txt.Contains(sequ_tag + "("))
                {
                    lines = getBetween(temp_txt, sequ_tag + "(", ")").Split(new[] { ' ', ',', '.', ':', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    Layouts = new int[lines.Length];
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (int.TryParse(lines[i], out Layouts[LayoutCount])) { LayoutCount++; };
                    }
                }
                Runtime.UpdateFrequency = speed;
                oldCustomData = Me.CustomData;
            }
            if (nosubgridLCDs)
            {
                ignoredGrids.Clear();
                GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(surfaceProviders, (p => p.CustomData.Contains(lcdtag) && p.CubeGrid == Me.CubeGrid));
            }
            else
            {
                DetectIgnoredGrids();
                GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(surfaceProviders, p => p.CustomData.Contains(lcdtag));
            }
            foreach (var block in surfaceProviders)
            {
                bool skipBlock = false;
                foreach (var grid in ignoredGrids) { if (block.CubeGrid == grid) { skipBlock = true; } }
                if (skipBlock) { break; }
                var provider = block as IMyTextSurfaceProvider;
                Displayname = block.CustomName;
                if (provider.SurfaceCount < 1)
                {
                    Echo("         << F.S.D. Warning >>");
                    Echo("The CustomData field of the block:");
                    Echo('"' + Displayname + '"');
                    Echo("contains the trigger word:");
                    Echo('"' + lcdtag + '"');
                    Echo("but doesn't provide any textpanels");
                    Echo("");
                    continue;
                }
                StartTag = lcdtag + " " + Layout;
                if (!("\n" + block.CustomData).Contains("\n" + StartTag)) { StartTag = lcdtag; }
                temp_txt = block.CustomData.Substring(block.CustomData.IndexOf(StartTag)).Trim();
                if (temp_txt.IndexOfAny(seperators) < 0) { break; }
                while (temp_txt.StartsWith(lcdtag)) { temp_txt = temp_txt.Substring(temp_txt.IndexOfAny(seperators)).Trim(); }
                if (temp_txt.Contains(endtag)) { temp_txt = temp_txt.Remove(temp_txt.IndexOf(endtag)); }
                sections = ("\n" + temp_txt).Split(new[] { panel_tag, panel_tag.ToLower(), panel_tag.ToUpper() }, StringSplitOptions.None).Where(x => !string.IsNullOrEmpty(x)).Select(s => s.Trim()).ToArray();
                for (int i = 0; i < sections.Length; i++)
                {
                    lines = sections[i].Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    if (lines.Length == 0) { continue; }
                    if ((int.TryParse(lines.First(), out surfaceIndex)) && (surfaceIndex < provider.SurfaceCount))
                    {
                        sArgs = lines.Skip(1).ToArray();
                    }
                    else
                    {
                        surfaceIndex = 0;
                        sArgs = lines;
                    }
                    surface = provider.GetSurface(surfaceIndex);
                    DrawScreen(surface, argument);
                    screenIndex++;
                }
            }
            added_txt = "";
            rotatecount++;
            rotatecount %= 4;
            switch (argument)
            {
                case "version":
                    Echo("Version: " + ver);
                    break;
                case "shutdown":
                    Storage = "offline";
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    added_txt = "             << Notice >>\nThis F.S.D. entity is shut down.\nRun this programming block\nwith the argument " + '"' + "powerup" + '"' + "\nto restart it again.";
                    break;
                case "powerup":
                    Storage = "online";
                    Runtime.UpdateFrequency = speed;
                    added_txt = "FSD is booting";
                    Echo(" ");
                    break;
                case "refresh":
                    Runtime.UpdateFrequency = speed;
                    added_txt = "FSD is refreshing the displays";
                    break;
                case "":
                    added_txt = "   << F.S.D. is active >>\n" + "|/-\u005C".Substring(rotatecount, 1) + "\nThis programming block is\nhandling " + plSing("LCD Panel", screenIndex) + "\nat the moment.";
                    break;
                default:
                    if (argument.StartsWith(LpM_tag))
                    {
                        LpM = NumParse(argument, LpM_tag);
                    }
                    else if (argument.StartsWith(FpM_tag))
                    {
                        FpM = NumParse(argument, FpM_tag);
                    }
                    else if (argument.StartsWith("layout="))
                    {
                        Layout = NumParse(argument, "layout=");
                    }
                    break;
            }
            Echo(added_txt);
            if (!nostatus)
            {
                IMyTextSurface surface = Me.GetSurface(0);
                frame = surface.DrawFrame();
                viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
                PrepareTextSurface(surface);
                if (Me.BlockDefinition.SubtypeName.StartsWith("S"))
                {
                    AddTextSprite(frame, viewport.Center + new Vector2(0, -50), added_txt.Trim(), "DEBUG", 0.7f, Color.White, C_align);
                }
                else
                {
                    AddTextSprite(frame, viewport.Center + new Vector2(0, -90), added_txt.Trim(), "DEBUG", 1.3f, Color.White, C_align);
                }
                frame.Dispose();
            }
        }

        void DetectIgnoredGrids()
        {
            List<IMyProgrammableBlock> pbs = new List<IMyProgrammableBlock>();
            ignoredGrids.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(pbs, x => x.CustomName.Contains("[FSD]") && !(x.CubeGrid == Me.CubeGrid));
            foreach (var pb in pbs) { ignoredGrids.Add(pb.CubeGrid); }
        }

        void DrawScreen(IMyTextSurface surface, string argument)
        {
            viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
            pos = LineStartpos = viewport.Position + new Vector2(7, 0);
            frame = surface.DrawFrame();
            string[] groupAndOptions;
            string c_arg;
            float gfx_res = 0.1f;
            bool continueDrawing = true;
            int lastDraw = Nothing;
            int optionpos = 0;
            int texttagpos = 0;
            headlineIndex = 0;
            PrepareTextSurface(surface);
            if (Storage == "offline" || argument == "shutdown" || argument == "refresh")
            {
                AddTextSprite(frame, pos, " ", "DEBUG", 1f, Color.Black, L_align);
            }
            else
            {
                get_sprite = false;
                gfxSprite.Clear();
                foreach (var arg in sArgs)
                {
                    if (get_sprite)
                    {
                        if (arg.ToLower().TrimStart(new[] { ':', ',' }) == gfxEnd.ToLower())
                        {
                            AddTextSprite(frame, pos, gfxSprite.ToString(), "Monospace", gfx_res, Color.White, L_align);
                            get_sprite = false;
                            gfxSprite.Clear();
                        }
                        else
                        {
                            gfxSprite.AppendLine(arg);
                        }
                        continue;
                    }
                    LineStartpos.X = viewport.X + 7;
                    optionpos = arg.IndexOfAny(new char[] { ',', ':' });
                    containertype = SingleBlock;
                    for (int i = 0; i < paints.Length; i++) { paints[i] = false; }
                    Colors[ico] = pictogramColor;
                    Colors[txt] = headlineColor;
                    Colors[per] = percentDisplayColor;
                    Colors[opt] = optioncolor;
                    if (optionpos >= 0)
                    {
                        c_arg = arg.ToLower().Substring(optionpos + 1);
                        texttagpos = c_arg.IndexOf(texttag.ToLower());
                        if (texttagpos >= 0)
                        {
                            added_txt = arg.Substring(texttagpos + texttag.Length + optionpos + 1);
                            c_arg = c_arg.Remove(texttagpos);
                        }
                        else
                        {
                            added_txt = "";
                        }
                        if (c_arg.StartsWith(gfxStart.ToLower()))
                        {
                            if (getVector(c_arg, gfxStart, out tempvector, out gfx_res))
                            {
                                pos = LineStartpos = tempvector + viewport.Position;
                                get_sprite = true;
                                gfxSprite.Clear();
                                continue;
                            }
                        }
                        if (c_arg.Contains(positiontag.ToLower()))
                        {
                            if (getVector(c_arg, positiontag, out tempvector, out gfx_res)) { pos = LineStartpos = tempvector + viewport.Position; lastDraw = Nothing; }
                        }
                        textpos = get_align(c_arg, textposdefault);
                        opt_chc = wordparse(opt_tag, opt_def, c_arg);
                        for (int i = 0; i < paints.Length; i++)
                        {
                            if (c_arg.Contains(PaintTag[i].ToLower())) { paints[i] = getcolors(c_arg, PaintTag[i], out Colors[i]); }
                        }
                        if (!paints[ico]) { Colors[ico] = pictogramColor; }
                        if (!paints[txt]) { Colors[txt] = headlineColor; }
                        if (paints[per])
                        {
                            Colors[opt] = Colors[per];
                        }
                        else
                        {
                            Colors[per] = percentDisplayColor;
                            Colors[opt] = optioncolor;
                        }
                        if (c_arg.Contains(widebartag.ToLower()))
                        {
                            if (viewport.Width < 500) { containertype = SmallBar; } else { containertype = WideBar; }
                        }
                        else if (c_arg.Contains(smallbartag.ToLower()))
                        {
                            containertype = SmallBar;
                        }
                        else if (c_arg.Contains(singleicontag.ToLower()))
                        {
                            containertype = SingleIcon;
                        }
                        else if (c_arg.Contains(noicontag.ToLower()))
                        {
                            containertype = Textline;
                        }
                        c_arg = arg.Substring(0, optionpos);
                    }
                    else
                    {
                        opt_chc = opt_def;
                        added_txt = "";
                        c_arg = arg;
                    }
                    groupAndOptions = c_arg.Trim().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                    if (continueDrawing)
                    {
                        continueDrawing = CycleDrawBlocks(groupAndOptions, lastDraw);
                        lastDraw = containertype;
                    }
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
            float integrity = 0;
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
                case SingleIcon:
                    if (containertype != lastDraw || (pos.X + 72) >= viewport.Width || !opt_chc[4]) { LineStartpos.Y += 95; pos = LineStartpos; }
                    break;
                case SingleBlock:
                    LineStartpos.Y += 95; pos = LineStartpos;
                    break;
                case Textline:
                    LineStartpos.Y += 23; pos = LineStartpos;
                    break;
            }
            if ((containertype == SingleBlock || containertype == SingleIcon) && (!opt_chc[4] || added_txt != ""))
            {
                LineStartpos.Y += 23; pos = LineStartpos;
                posContainerGroup = pos;
            }
            foreach (string entry in groupOrBlocknames)
            {
                string name = entry.Trim();
                placehlds = 0;
                if (containertype == SingleBlock && name.EndsWith("#"))
                {
                    placehlds = name.Length - name.TrimEnd('#').Length;
                    name = name.TrimEnd('#');
                }
                verbatim = verbatimdefault || (name.StartsWith("" + '"') && name.EndsWith("" + '"'));
                if (verbatim) { name = name.Trim('"'); }
                var foundgroup = GridTerminalSystem.GetBlockGroupWithName(name);
                if (foundgroup != null && !opt_chc[6])
                {
                    if (groupOrBlocknames.Length == 1 && added_txt == "" && !opt_chc[9])
                    {
                        headline.Append(foundgroup.Name + ": ");
                    }
                    foundgroup.GetBlocks(Group, (x => x.CubeGrid == Me.CubeGrid || !opt_chc[5]));
                }
                else
                {
                    Group.Clear();
                }
                if (Group.Count == 0)
                {
                    GridTerminalSystem.SearchBlocksOfName(name, Group, (x => x.CubeGrid == Me.CubeGrid || !opt_chc[5]));
                }
                for (int i = 0; i < placehlds; i++) { Group.Add(Me); }
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
                    Echo("         << F.S.D. Warning >>");
                    Echo("No group or block with the name:");
                    Echo('"' + name + '"');
                    Echo("found as listed in the to-do list of:");
                    Echo('"' + Displayname + '"');
                }
                bool continuegroup = true;
                int place_used = 0;
                foreach (var block in Group)
                {
                    if (verbatim && block != Me && block.CustomName != name) { continue; }
                    if (!GetBlockProperties(block, ref percent, ref integrity)) { continue; }
                    percent = Math.Abs(percent);
                    if (placehlds > 0 && place_used >= placehlds) { continue; }
                    if (!continuegroup || containertype != SingleBlock) { continue; }
                    if ((pos.X + 72) > viewport.Width)
                    {
                        if (!opt_chc[7])
                        {
                            if ((pos.Y + 170) > viewport.Bottom)
                            {
                                continuegroup = false;
                            }
                            else
                            {
                                LineStartpos.Y += 95;
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
                        if (pattern == patternNone)
                        {
                            fillLevelLoopcount = 0;
                            if (block != Me) { integrityLevelColor = cvar(1 - (float)Math.Pow(TestForNaN(integrity), 3)); }
                        }
                        else
                        {
                            GetChargeStateValues(percent, SingleBlock, integrity);
                        }
                        DrawSingleContainer(pos, integrity);
                        pos.X += 72;
                        place_used++;
                    }
                }
            }
            if (sumAll[0] + sumBlock == 0 && added_txt != "")
            {
                if (containertype == SingleBlock) { LineStartpos.Y -= 23; pos = LineStartpos; }
                containertype = Textline;
            }
            else if (sumAll[0] > 0)
            {
                percent = (int)Math.Round(sumAll[1] / sumAll[0], 0);
            }
            switch (containertype)
            {
                case WideBar:
                    GetChargeStateValues(percent, WideBar);
                    posHeadline = pos + new Vector2(15, 14);
                    DrawCargebarWide(pos);
                    pos.X += 508;
                    if (pos.Y > viewport.Height) { continueDrawing = false; }
                    break;
                case SmallBar:
                    GetChargeStateValues(percent, SmallBar);
                    posHeadline = pos + new Vector2(15, 14);
                    DrawCargebarWide(pos);
                    pos.X += 256;
                    if (pos.Y > viewport.Height) { continueDrawing = false; }
                    break;
                case SingleIcon:
                    GetChargeStateValues(percent, SingleIcon);
                    posHeadline = posContainerGroup + new Vector2(0, -23);
                    DrawSingleContainer(pos, 0);
                    pos.X += 72;
                    if (pos.Y > viewport.Height) { continueDrawing = false; }
                    break;
                case SingleBlock:
                    posHeadline = posContainerGroup + new Vector2(0, -23);
                    if (pos.Y > viewport.Height) { continueDrawing = false; }
                    break;
                case Textline:
                    posHeadline = LineStartpos;
                    if ((pos.Y + 23) >= viewport.Height) { continueDrawing = false; }
                    break;
            }
            headline.Append(added_txt);
            if (!opt_chc[4])
            {
                if (opt_chc[9])
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
                    if (opt_chc[0]) { headline.Append("In " + TruncateUnit(sumBat[1], "watt") + "Out "); }
                    else { headline.Append(f_percent(sumBat)); }
                    headline.Append(TruncateUnit(sumBat[2], "whr") + StopSep);
                }
                if (sumReactor[0] > 0)
                {
                    headline.Append(hlstart("Reactor", sumReactor[0]));
                    if (opt_chc[0]) { headline.Append("Output " + f_percent(sumReactor) + TruncateUnit(sumReactor[2], "watt") + StopSep); }
                    else { headline.Append(TruncateUnit(sumReactor[2], "kg") + "Uranium " + StopSep); }
                }
                if (sumGasgen[0] > 0)
                {
                    headline.Append(hlstart("Hydrogen Engine", sumGasgen[0]));
                    if (opt_chc[0]) { headline.Append("Output " + f_percent(sumGasgen) + TruncateUnit(sumGasgen[2], "watt") + StopSep); }
                    else { headline.Append(f_percent(sumGasgen) + TruncateUnit(sumGasgen[2], "lit") + StopSep); }
                }
                if (sumRefinery[0] > 0)
                {
                    headline.Append(hlstart("Refinery", sumRefinery[0]));
                    if (opt_chc[0]) { headline.Append("Ingots " + f_percent(sumRefinery) + TruncateUnit(sumRefinery[2], "c_l") + StopSep); } else { headline.Append("Ores " + f_percent(sumRefinery) + TruncateUnit(sumRefinery[2], "c_l") + StopSep); }
                }
                if (sumAssembler[0] > 0)
                {
                    headline.Append(hlstart("Assembler", sumAssembler[0]));
                    if (opt_chc[0]) { headline.Append("Items Inventory " + f_percent(sumAssembler) + "Item Completion: " + Math.Round(sumAssembler[2] * 100 / sumAssembler[0], 0) + "% " + StopSep); }
                    else { headline.Append("Ingot Inventory " + f_percent(sumAssembler) + "Production Queue: " + plSing(" Item", sumAssembler[2]) + StopSep); }
                }
                if (sumDoor[0] > 0)
                {
                    headline.Append(hlstart("Door", sumDoor[0]) + f_percent(sumDoor));
                    if (opt_chc[0]) { headline.Append("open" + StopSep); } else { headline.Append("closed" + StopSep); }
                }
                if (sumJdrives[0] > 0) { headline.Append(hlstart("Jumpdrive", sumJdrives[0]) + f_percent(sumJdrives) + TruncateUnit(sumJdrives[2], "whr") + StopSep); }
                if (sumCargo[0] > 0) { headline.Append(hlstart("Cargo", sumCargo[0]) + f_percent(sumCargo) + TruncateUnit(sumCargo[2], "c_l")); }
                if (sumCockpit[0] > 0) { headline.Append(hlstart("Cockpit", sumCockpit[0]) + f_percent(sumCockpit) + TruncateUnit(sumCockpit[2], "c_l")); }
                if (sumPara[0] > 0) { headline.Append(hlstart("Parachute Crate", sumPara[0]) + f_percent(sumPara) + TruncateUnit(sumPara[2], "c_l")); }
                if (sumSorter[0] > 0) { headline.Append(hlstart("Sorter", sumSorter[0]) + f_percent(sumSorter) + TruncateUnit(sumSorter[2], "c_l")); }
                if (sumCollectors[0] > 0) { headline.Append(hlstart("Collector", sumCollectors[0]) + f_percent(sumCollectors) + TruncateUnit(sumCollectors[2], "c_l") + StopSep); }
                if (sumConnectors[0] > 0) { headline.Append(hlstart("Connector", sumConnectors[0]) + f_percent(sumConnectors) + TruncateUnit(sumConnectors[2], "c_l") + StopSep); }
                if (sumEjectors[0] > 0) { headline.Append(hlstart("Ejector", sumEjectors[0]) + f_percent(sumEjectors) + TruncateUnit(sumEjectors[2], "c_l") + StopSep); }
                if (sumDrills[0] > 0) { headline.Append(hlstart("Drill", sumDrills[0]) + f_percent(sumDrills) + TruncateUnit(sumDrills[2], "c_l") + StopSep); }
                if (sumWelder[0] > 0) { headline.Append(hlstart("Welder", sumWelder[0]) + f_percent(sumWelder) + TruncateUnit(sumWelder[2], "c_l") + StopSep); }
                if (sumGrinder[0] > 0) { headline.Append(hlstart("Grinder", sumGrinder[0]) + f_percent(sumGrinder) + TruncateUnit(sumGrinder[2], "c_l") + StopSep); }
                if (sumHydro[0] > 0) { headline.Append(hlstart("Hydrogen Tank", sumHydro[0]) + f_percent(sumHydro) + TruncateUnit(sumHydro[2], "lit") + StopSep); }
                if (sumOxy[0] > 0) { headline.Append(hlstart("Oxygen Tank", sumOxy[0]) + f_percent(sumOxy) + TruncateUnit(sumOxy[2], "lit") + StopSep); }
                if (sumEShield[0] > 0) { headline.Append(hlstart("Shield Generator", sumEShield[0]) + f_percent(sumEShield) + "Required Input: " + eShieldInput + " " + StopSep); }
                if (sumShield[0] > 0) { headline.Append(hlstart("Shield Controller", sumShield[0]) + Math.Round(sumShield[1], 0) + "% Overheated: " + Math.Round(sumShield[2], 0) + "% " + StopSep); }
                if (sumMissileTurret[0] > 0) { headline.Append(hlstart("Missile Turret", sumMissileTurret[0]) + f_percent(sumMissileTurret) + plSing(" Missile", sumMissileTurret[2]) + " " + StopSep); }
                if (sumGatlingTurret[0] > 0) { headline.Append(hlstart("Gatling Turret", sumGatlingTurret[0]) + f_percent(sumGatlingTurret) + plSing(" Unit", sumGatlingTurret[2]) + " " + StopSep); }
                if (sumMissileLauncher[0] > 0) { headline.Append(hlstart("Rocket Launcher", sumMissileLauncher[0]) + f_percent(sumMissileLauncher) + plSing(" Rocket", sumMissileLauncher[2]) + " " + StopSep); }
                if (sumGatling[0] > 0) { headline.Append(hlstart("Gatling Gun", sumGatling[0]) + f_percent(sumGatling) + plSing(" Unit", sumGatling[2]) + " " + StopSep); }
                if (sumThrust[0] > 0) { headline.Append(hlstart("Thruster", sumThrust[0]) + f_percent(sumThrust) + "Eff. " + Math.Round(sumThrust[2] * 100 / sumThrust[0], 0) + "% Override" + StopSep); }
                if (sumSolar[0] > 0) { headline.Append(hlstart("Solar Panel", sumSolar[0]) + "Output " + f_percent(sumSolar) + TruncateUnit(sumSolar[2], "watt") + StopSep); }
                if (sumWindmill[0] > 0) { headline.Append(hlstart("Wind Turbine", sumWindmill[0]) + "Output " + f_percent(sumWindmill) + TruncateUnit(sumWindmill[2], "watt") + StopSep); }
                if (sumO2Gen[0] > 0) { headline.Append(hlstart("O2H2 Generator", sumO2Gen[0]) + f_percent(sumO2Gen) + TruncateUnit(sumO2Gen[2], "c_l") + StopSep); }
                if (sumProjector[0] > 0) { headline.Append(hlstart("Projector", sumProjector[0]) + f_percent(sumProjector) + "Remaining: " + plSing(" Block", sumProjector[2]) + StopSep); }
                if (sumVent[0] > 0) { headline.Append(hlstart("Air Vent", sumVent[0]) + f_percent(sumVent) + "Pressure" + StopSep); }
                if (sumBlock > 0) { headline.Append(hlstart("unidentified Block", sumBlock) + StopSep); }
            }
            DrawHeadline(posHeadline, headline);
            return continueDrawing;
        }

        void DrawHeadline(Vector2 drawposHeadline, StringBuilder headline)
        {
            string headline_short = "";
            int scrollindex = 0;
            if (screenIndex < 50 && headlineIndex < 20) { scrollindex = scrollpositions[screenIndex, headlineIndex]; }
            if (opt_chc[8] || headline.Length + 16 < scrollindex) { scrollindex = 0; }
            switch (containertype)
            {
                case WideBar:
                    if (trimmed_SB(scrollindex, headline, 440f, 1.3f, out headline_short)) { scrollindex++; }
                    AddTextSprite(frame, shifttext(drawposHeadline, 470f, textpos), headline_short, "DEBUG", 1f, Colors[txt], textpos);
                    break;
                case SmallBar:
                    if (trimmed_SB(scrollindex, headline, 190f, 0.8f, out headline_short)) { scrollindex++; }
                    AddTextSprite(frame, shifttext(drawposHeadline, 190f, textpos), headline_short, "DEBUG", 0.8f, Colors[txt], textpos);
                    break;
                case SingleIcon:
                case SingleBlock:
                    if (trimmed_SB(scrollindex, headline, (viewport.Width - 25 - drawposHeadline.X), 0.8f, out headline_short)) { scrollindex++; }
                    AddTextSprite(frame, shifttext(drawposHeadline, (viewport.Width - drawposHeadline.X), textpos), headline_short, "DEBUG", 0.8f, Colors[txt], textpos);
                    break;
                case Textline:
                    if (trimmed_SB(scrollindex, headline, (viewport.Width - 25 - drawposHeadline.X), 0.8f, out headline_short)) { scrollindex++; }
                    AddTextSprite(frame, shifttext(drawposHeadline, (viewport.Width - drawposHeadline.X), textpos), headline_short, "DEBUG", 0.8f, Colors[txt], textpos);
                    break;
            }
            headline.Clear();
            if (!opt_chc[8] && screenIndex < 50 && headlineIndex < 20)
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
            if (alignpos == R_align) { org_pos.X += width; }
            else if (alignpos == C_align) { org_pos.X += (width / 2); }
            return org_pos;
        }

        string hlstart(string str, float number)
        {
            if (opt_chc[9]) { return StartSep; }
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
            float temp_f = ((width - surface.MeasureStringInPixels(sb, "Debug", 0.8f).X) / 4.981622f);
            sb.Insert(0, "                     ".Substring(0, (int)temp_f));
            return sb.ToString();
        }

        string f_percent(float[] werte)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0:N0}% ", TestForNaN(werte[1] / werte[0]));
            float temp_f = ((49.193516f - surface.MeasureStringInPixels(sb, "Debug", 0.8f).X) / 4.981622f);
            sb.Insert(0, "                     ".Substring(0, (int)temp_f));
            return sb.ToString();
        }

        bool GetBlockProperties(IMyTerminalBlock block, ref float percent, ref float integrity)
        {
            invertColor = false;
            bool isValidBlock = true;
            string blocktype = "";
            string subtype = "";
            bsize = 0;
            optionchoice = "";
            if (block.BlockDefinition.SubtypeName.Contains("DSControl"))
            {
                sumShield[0]++; sumAll[0]++;
                float.TryParse(getBetween(block.CustomInfo, " (", "%)"), out percent);
                sumShield[1] = percent; sumAll[1] += percent;
                float.TryParse(getBetween(block.CustomInfo, "[Over Heated]: ", "%"), out sumShield[2]);
                icon = iconShield;
                pattern = patternShield;
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
            }
            else if (block == Me)
            {
                icon = "Cross";
                pattern = patternNone;
                optionchoice = "UNKNOWN";
                drawpercent = "???";
                percent = 0;
            }
            else
            {
                blocktype = block.ToString().Remove(block.ToString().IndexOf('{')).Trim();
                subtype = block.BlockDefinition.SubtypeName;
            }
            switch (blocktype)
            {
                case "MyBatteryBlock":
                    bsize = get_size(subtype, 'B');
                    var bat = block as IMyBatteryBlock;
                    if (opt_chc[0])
                    {
                        optionchoice = "Load";
                        percent = TestForNaN(100 / bat.MaxOutput * bat.CurrentOutput) - TestForNaN(100 / bat.MaxInput * bat.CurrentInput);
                        sumBat[1] += bat.CurrentInput;
                        sumBat[2] += bat.CurrentOutput;
                    }
                    else
                    {
                        optionchoice = "Charge";
                        percent = TestForNaN(100 / bat.MaxStoredPower * bat.CurrentStoredPower);
                        sumAll[1] += percent;
                        sumBat[1] += percent;
                        sumBat[2] += bat.CurrentStoredPower;
                    }
                    sumBat[0]++;
                    sumAll[0]++;
                    icon = "IconEnergy";
                    pattern = patternBattery;
                    break;
                case "MyReactor":
                    bsize = get_size(subtype, 'A');
                    if (opt_chc[0])
                    {
                        optionchoice = "Load";
                        invertColor = true;
                        PowerOutput2Arr(block, sumReactor, ref percent);
                    }
                    else
                    {
                        optionchoice = "Fuel";
                        ItemCount2Arr(block, sumReactor, ref percent);
                    }
                    icon = iconReactor;
                    pattern = patternReactor;
                    break;
                case "MyHydrogenEngine":
                    bsize = get_size(subtype, 'T');
                    if (opt_chc[0])
                    {
                        optionchoice = "Load";
                        invertColor = true;
                        PowerOutput2Arr(block, sumGasgen, ref percent);
                    }
                    else
                    {
                        optionchoice = "Fuel";
                        float MaxVol, CurVol;
                        string temp_text = block.DetailedInfo;
                        if (temp_text.Contains("л"))
                        {
                            float.TryParse(getBetween(temp_text, " л/", " л)"), out MaxVol);
                            float.TryParse(getBetween(temp_text, " (", " л/"), out CurVol);
                        }
                        else
                        {
                            float.TryParse(getBetween(temp_text, "L/", "L)"), out MaxVol);
                            float.TryParse(getBetween(temp_text, " (", "L/"), out CurVol);
                        }
                        sumGasgen[0]++;
                        sumAll[0]++;
                        percent = 100 / MaxVol * CurVol;
                        sumGasgen[1] += percent;
                        sumAll[1] += percent;
                        sumGasgen[2] += CurVol;
                    }
                    icon = iconEngine;
                    pattern = patternGasgen;
                    break;
                case "MyCargoContainer":
                    bsize = get_size(subtype, 'A');
                    isValidBlock = CargoVolume2Arr(block, sumCargo, ref percent);
                    icon = iconCargo;
                    optionchoice = "Fill";
                    pattern = patternCargo;
                    invertColor = true;
                    break;
                case "MyCockpit":
                    isValidBlock = CargoVolume2Arr(block, sumCockpit, ref percent);
                    icon = iconCockpit;
                    optionchoice = "Fill";
                    pattern = patternCargo;
                    invertColor = true;
                    break;
                case "MyGasTank":
                    if (block.BlockDefinition.SubtypeName.Contains("Hydro"))
                    {
                        bsize = get_size(subtype, 'H');
                        TankVolume2Arr(block, sumHydro, ref percent);
                        icon = "IconHydrogen";
                    }
                    else
                    {
                        bsize = get_size(subtype, 'T');
                        TankVolume2Arr(block, sumOxy, ref percent);
                        icon = "IconOxygen";
                    }
                    optionchoice = "Fill";
                    pattern = patternTank;
                    break;
                case "MyShipConnector":
                    bsize = get_size(subtype, 'C');
                    if ((bsize == 1) || (subtype == "ConnectorLarge_SEBuilder4"))
                    {
                        isValidBlock = CargoVolume2Arr(block, sumEjectors, ref percent);
                        icon = iconEjector;
                    }
                    else
                    {
                        isValidBlock = CargoVolume2Arr(block, sumConnectors, ref percent);
                        icon = iconConnector;
                    }
                    optionchoice = "Fill";
                    pattern = patternConnector;
                    invertColor = true;
                    break;
                case "MyCollector":
                    bsize = get_size(subtype, 'T');
                    isValidBlock = CargoVolume2Arr(block, sumCollectors, ref percent);
                    optionchoice = "Fill";
                    icon = iconCollector;
                    pattern = patternConnector;
                    invertColor = true;
                    break;
                case "MyShipDrill":
                    bsize = get_size(subtype, 'T');
                    isValidBlock = CargoVolume2Arr(block, sumDrills, ref percent);
                    icon = iconDrill;
                    optionchoice = "Fill";
                    pattern = patternTool;
                    invertColor = true;
                    break;
                case "MyJumpDrive":
                    bsize = 8;
                    var jumpdrive = block as IMyJumpDrive;
                    sumJdrives[0]++;
                    sumAll[0]++;
                    percent = 100 / jumpdrive.MaxStoredPower * jumpdrive.CurrentStoredPower;
                    sumJdrives[1] += percent;
                    sumAll[1] += percent;
                    sumJdrives[2] += jumpdrive.CurrentStoredPower;
                    icon = iconJumpdrive;
                    optionchoice = "Charge";
                    pattern = patternJumpdrive;
                    break;
                case "MyThrust":
                    Echo("1");
                    bsize = get_size(subtype, 'A');
                    var thruster = block as IMyThrust;
                    sumThrust[0]++;
                    sumAll[0]++;
                    percent = 100 / thruster.MaxEffectiveThrust * thruster.CurrentThrust;
                    if (percent > 100) { percent = 0; }
                    sumThrust[1] += percent;
                    sumAll[1] += percent;
                    sumThrust[2] += thruster.ThrustOverridePercentage;
                    icon = iconThruster;
                    optionchoice = "Thrst";
                    pattern = patternThruster;
                    break;
                case "MySolarPanel":
                    bsize = get_size(subtype, 'T');
                    PowerOutput2Arr(block, sumSolar, ref percent);
                    icon = iconSolar;
                    optionchoice = "Load";
                    invertColor = true;
                    pattern = patternSolar;
                    break;
                case "MyWindTurbine":
                    bsize = 8;
                    PowerOutput2Arr(block, sumWindmill, ref percent);
                    icon = iconWindmill;
                    optionchoice = "Load";
                    invertColor = true;
                    pattern = patternWindmill;
                    break;
                case "MyGasGenerator":
                    bsize = get_size(subtype, 'T');
                    isValidBlock = CargoVolume2Arr(block, sumO2Gen, ref percent);
                    icon = iconIce;
                    optionchoice = "Fill";
                    pattern = patternGasgen;
                    break;
                case "MyShipGrinder":
                    bsize = get_size(subtype, 'T');
                    isValidBlock = CargoVolume2Arr(block, sumGrinder, ref percent);
                    icon = iconGrinder;
                    optionchoice = "Fill";
                    pattern = patternTool;
                    invertColor = true;
                    break;
                case "MyShipWelder":
                    bsize = get_size(subtype, 'T');
                    isValidBlock = CargoVolume2Arr(block, sumWelder, ref percent);
                    icon = iconWelder;
                    optionchoice = "Fill";
                    pattern = patternTool;
                    invertColor = true;
                    break;
                case "MyRefinery":
                    bsize = get_size(subtype, 'R');
                    IMyInventory Ref_inv = null;
                    var Refblock = block as IMyRefinery;
                    if (!opt_chc[0])
                    {
                        optionchoice = "Ores";
                        Ref_inv = Refblock.InputInventory;
                    }
                    else
                    {
                        optionchoice = "Ingots";
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
                    break;
                case "MyAssembler":
                    bsize = get_size(subtype, 'R');
                    IMyInventory inventory = null;
                    var Assblock = block as IMyAssembler;
                    if (!opt_chc[0])
                    {
                        optionchoice = "Ingots";
                        inventory = Assblock.InputInventory;
                        List<MyProductionItem> queue = new List<MyProductionItem>();
                        Assblock.GetQueue(queue);
                        foreach (var item in queue) { sumAssembler[2] += ((float)item.Amount); }
                    }
                    else
                    {
                        optionchoice = "Items";
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
                    break;
                case "MyLargeMissileTurret":
                    bsize = get_size(subtype, 'T');
                    ItemCount2Arr(block, sumMissileTurret, ref percent);
                    icon = iconRocket;
                    optionchoice = "Ammo";
                    pattern = patternTurret;
                    break;
                case "MyLargeGatlingTurret":
                case "MyLargeInteriorTurret":
                    bsize = get_size(subtype + blocktype, 'W');
                    ItemCount2Arr(block, sumGatlingTurret, ref percent);
                    icon = iconBullet;
                    optionchoice = "Ammo";
                    pattern = patternTurret;
                    break;
                case "MySmallGatlingGun":
                    bsize = 4;
                    ItemCount2Arr(block, sumGatling, ref percent);
                    icon = iconBullet;
                    optionchoice = "Ammo";
                    pattern = patternTurret;
                    break;
                case "MySmallMissileLauncher":
                case "MySmallMissileLauncherReload":
                    bsize = get_size(subtype + blocktype, 'W');
                    ItemCount2Arr(block, sumMissileLauncher, ref percent);
                    icon = iconRocket;
                    optionchoice = "Ammo";
                    pattern = patternTurret;
                    break;
                case "MySpaceProjector":
                    var projector = block as IMyProjector;
                    var total = projector.TotalBlocks;
                    sumProjector[0]++;
                    sumAll[0]++;
                    if (total != 0) { percent = 100 * (total - projector.RemainingBlocks) / total; }
                    sumProjector[1] += percent;
                    sumAll[1] += percent;
                    sumProjector[2] += projector.RemainingBlocks;
                    icon = iconProjector;
                    optionchoice = "Done";
                    pattern = patternProjector;
                    break;
                case "MyAirVent":
                    bsize = get_size(subtype, 'T');
                    var vent = block as IMyAirVent;
                    sumVent[0]++;
                    sumAll[0]++;
                    percent = (float)Math.Round(vent.GetOxygenLevel() * 100);
                    sumVent[1] += percent;
                    sumAll[1] += percent;
                    icon = iconVent;
                    optionchoice = "Press";
                    pattern = patternVent;
                    break;
                case "MyParachute":
                    isValidBlock = CargoVolume2Arr(block, sumPara, ref percent);
                    icon = iconPara;
                    optionchoice = "Fill";
                    pattern = patternCargo;
                    break;
                case "MyConveyorSorter":
                    bsize = get_size(subtype, 'C');
                    isValidBlock = CargoVolume2Arr(block, sumSorter, ref percent);
                    icon = iconSorter;
                    optionchoice = "Fill";
                    pattern = patternConnector;
                    invertColor = true;
                    break;
                case "MyAirtightHangarDoor":
                case "MyDoor":
                case "MyAirtightSlideDoor":
                    var door = block as IMyDoor;
                    sumDoor[0]++;
                    sumAll[0]++;
                    percent = (float)Math.Round(door.OpenRatio * 100);
                    optionchoice = "open";
                    invertColor = true;
                    if (!opt_chc[0])
                    {
                        optionchoice = "closed";
                        percent = 100 - percent;
                        invertColor = false;
                    }
                    sumDoor[1] += percent;
                    sumAll[1] += percent;
                    icon = iconDoor;
                    pattern = patternJumpdrive;
                    break;
                case "":
                    break;
                default:
                    if (opt_chc[3])
                    {
                        sumBlock++;
                        pattern = patternNone;
                        drawpercent = "-----";
                        icon = iconBlock;
                    }
                    else { isValidBlock = false; }
                    break;
            }
            if (isValidBlock)
            {
                if (opt_chc[3]) { integrity = GetMyTerminalBlockHealth(block.CubeGrid.GetCubeBlock(block.Position)); } else { integrity = 0; }
                if (!paints[frm])
                {
                    if (block == Me) { Colors[frm] = frameColorIsNotThere; integrity = 0; }
                    else if (block.IsWorking) { Colors[frm] = frameColorIsWorking; }
                    else if (block.IsFunctional) { Colors[frm] = frameColorIsFunctional; }
                    else { Colors[frm] = frameColorIsUnfunctional; }
                }
            }
            else
            {
                blocktype = "";
                subtype = "";
                bsize = 0;
                optionchoice = "";
            }
            return isValidBlock;
        }

        float GetMyTerminalBlockHealth(IMySlimBlock slimblock)
        {
            return (slimblock.BuildIntegrity - slimblock.CurrentDamage) / slimblock.MaxIntegrity;
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

        bool ItemCount2Arr(IMyTerminalBlock block, float[] arr, ref float percent)
        {
            bool result = block.HasInventory;
            if (result)
            {
                arr[0]++;
                sumAll[0]++;
                percent = 100 / (float)block.GetInventory().MaxVolume * (float)block.GetInventory().CurrentVolume;
                arr[1] += percent;
                sumAll[1] += percent;
                arr[2] += itemCount(block);
            }
            else
            {
                percent = 0;
            }
            return result;
        }

        bool CargoVolume2Arr(IMyTerminalBlock block, float[] arr, ref float percent)
        {
            bool result = block.HasInventory;
            if (result)
            {
                var inventory = block.GetInventory();
                arr[0]++;
                sumAll[0]++;
                percent = 100 / (float)inventory.MaxVolume * (float)inventory.CurrentVolume;
                arr[1] += percent;
                sumAll[1] += percent;
                arr[2] += (float)inventory.CurrentVolume;
            }
            else
            {
                percent = 0;
            }
            return result;
        }

        void TankVolume2Arr(IMyTerminalBlock block, float[] arr, ref float percent)
        {
            var tank = block as IMyGasTank;
            if (tank != null)
            {
                arr[0]++;
                sumAll[0]++;
                percent = (float)Math.Round(tank.FilledRatio * 100);
                arr[1] += percent;
                sumAll[1] += percent;
                arr[2] += (float)tank.Capacity * (float)tank.FilledRatio;
            }
        }

        float TestForNaN(float number)
        {
            if (double.IsNaN(number)) { number = 0; }
            return number;
        }

        void ClearGroupArrays()
        {
            ClearArray(sumBat);
            ClearArray(sumJdrives);
            ClearArray(sumCargo);
            ClearArray(sumCockpit);
            ClearArray(sumPara);
            ClearArray(sumSorter);
            ClearArray(sumHydro);
            ClearArray(sumOxy);
            ClearArray(sumVent);
            ClearArray(sumDoor);
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
            sumBlock = 0;
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

        void GetChargeStateValues(float percent, int containertype, float integrity = 0)
        {
            percent = TestForNaN(percent);
            integrity = TestForNaN(integrity);
            int counter = drawcounter(percent) + 5;
            switch (containertype)
            {
                case WideBar:
                    chargeBarInitialOffset = new Vector2(14, 37);
                    chargeBarOffset = 48;
                    chargeBarSize = new Vector2(40, 43);
                    pattern = patternWideBar;
                    fillLevelLoopcount = counter / 10;
                    break;
                case SmallBar:
                    chargeBarInitialOffset = new Vector2(12, 27);
                    chargeBarOffset = 22;
                    chargeBarSize = new Vector2(20, 29);
                    pattern = patternSmallBar;
                    fillLevelLoopcount = counter / 10;
                    break;
                case SingleIcon:
                case SingleBlock:
                    fillLevelLoopcount = (counter + 5) / 20;
                    drawpercent = String.Format("{0:N0}%", percent);
                    break;
                default:
                    fillLevelLoopcount = 0;
                    break;
            }
            if (!paints[bar])
            {
                if (invertColor)
                {
                    Colors[bar] = cvar(percent / 100);
                }
                else
                {
                    Colors[bar] = cvar(1 - (percent / 100));
                }
            }
            integrityLevelColor = cvar(1 - (float)Math.Pow(integrity, 3));
        }

        int drawcounter(float percent)
        {
            return Math.Max(Math.Min((int)percent, 100), 0);
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

        int get_size(string type, char methode)
        {
            int res = 0;

            Echo(type + "_-_" + methode);
            int lengthstring = type.Length;
            if (methode == 'A')
            {
                Echo(type);
                Echo("%i", lengthstring);
            }
            switch (methode)
            {
                case ('A'):
                    {
                        if (type.Length < 11)
                        {
                            switch (type)
                            {
                                case ("SmallBlockS"): { res = 1; } break;
                                case ("SmallBlockM"): { res = 2; } break;
                                case ("SmallBlockL"): { res = 3; } break;
                                case ("LBSHAT2"): { res = 5; } break;
                            }
                        }
                        switch (type.Substring(0, 11))
                        {
                            case ("SmallBlockS"): { res = 1; } break;
                            case ("SmallBlockM"): { res = 2; } break;
                            case ("SmallBlockL"): { res = 3; } break;
                            case ("LargeBlockS"): { res = 5; } break;
                            default: { res = 7; } break;
                        }
                    }
                    break;
                case ('B'):
                    {
                        if (type.StartsWith("Large")) { res = 7; }
                        else if (type.Contains("BlockBattery")) { res = 2; }
                        else { res = 1; }
                    }
                    break;
                case ('C'):
                    {
                        if (type.Contains("Small")) { res = 1; }
                        else if (type.Contains("Medium")) { res = 2; }
                        else { res = 7; }
                    }
                    break;
                case ('H'):
                    {
                        if (type.StartsWith("Large")) { res = 7; } else { res = 3; }
                        if (type.EndsWith("Small")) { res -= 2; }
                    }
                    break;
                case ('R'):
                    {
                        if (type.StartsWith("Large")) { res = 7; } else { res = 5; }
                    }
                    break;
                case ('T'):
                    {
                        if (type.Contains("Small")) { res = 4; } else { res = 8; }
                    }
                    break;
                case ('W'):
                    {
                        switch (type.Substring(0, 6))
                        {
                            case ("MySmal"): { res = 1; } break;
                            case ("SmallR"): { res = 2; } break;
                            case ("SmallG"): { res = 4; } break;
                            case ("LargeI"): { res = 5; } break;
                            default: { res = 7; } break;
                        }
                    }
                    break;
            }
            return res;
        }

        void DrawCargebarWide(Vector2 pos)
        {
            var chargebarpos = pos + chargeBarInitialOffset;
            if (!paints[frm]) { Colors[frm] = frameColorWideBar; }
            AddTextSprite(frame, pos, pattern, "Monospace", 0.1f, Colors[frm], L_align);
            for (int i = 0; i < fillLevelLoopcount; i++)
            {
                AddTextureSprite(frame, "SquareSimple", chargebarpos, chargeBarSize, Colors[bar], L_align);
                chargebarpos.X += chargeBarOffset;
            }
        }

        void DrawSingleContainer(Vector2 pos, float integrity = 0)
        {
            var chargebarpos = pos + new Vector2(10.8f, 75);
            var iconpos = pos + new Vector2(33, 36);
            var optionpos = pos + new Vector2(33, 54.5f);
            var numberpos = pos + new Vector2(33, 62);
            var integritypos = pos + new Vector2(49.5f, 79.5f - (integrity * 31.7f));
            var chargebarsize = new Vector2(44, 9);
            var integritysize = new Vector2(6f, 63.4f * integrity);
            AddTextSprite(frame, pos, pattern, "Monospace", 0.1f, Colors[frm], L_align);
            if (opt_chc[3] && (containertype == SingleBlock))
            {
                chargebarsize = new Vector2(33, 9);
                AddTextureSprite(frame, "SquareSimple", integritypos, integritysize, integrityLevelColor, L_align);
                AddTextSprite(frame, pos + new Vector2(52f, 12), "+\n+\n+\n+", "Monospace", 0.54f, Color.Black, C_align);
                AddTextSprite(frame, pos + new Vector2(52f, 20), "+\n+\n+\n+", "Monospace", 0.54f, Color.Black, C_align);
            }
            for (int i = 0; i < fillLevelLoopcount; i++)
            {
                AddTextureSprite(frame, "SquareSimple", chargebarpos, chargebarsize, Colors[bar], L_align);
                chargebarpos.Y -= 13.6f;
            }
            if (icon.Contains("\n")) { AddTextSprite(frame, iconpos + new Vector2(0, -20), icon, "Monospace", 0.1f, Colors[ico], C_align); } else { AddTextureSprite(frame, icon, iconpos, new Vector2(39, 35), Colors[ico], C_align); }
            if (opt_chc[1] && (optionchoice != "")) { AddTextSprite(frame, optionpos, optionchoice, "DEBUG", 0.4f, Colors[per], C_align); }
            if (opt_chc[2] && (containertype == SingleBlock))
            {
                AddTextSprite(frame, pos + new Vector2(7, 9), " SMLSML█".Substring(bsize, 1), "Monospace", 0.3f, Colors[opt], L_align);
                AddTextSprite(frame, pos + new Vector2(59, 9), " ████".Substring(bsize, 1), "Monospace", 0.3f, Colors[opt], R_align);
                AddTextSprite(frame, pos + new Vector2(7, 74), " ████".Substring(bsize, 1), "Monospace", 0.3f, Colors[opt], L_align);
                AddTextSprite(frame, pos + new Vector2(59, 74), " SMLSML█".Substring(bsize, 1), "Monospace", 0.3f, Colors[opt], R_align);
            }
            AddTextSprite(frame, numberpos, drawpercent, "DEBUG", 0.7f, Colors[per], C_align);
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

        void PrepareTextSurface(IMyTextSurface surface)
        {
            surface.ContentType = ContentType.SCRIPT;
            surface.Script = "";
            AddTextSprite(frame, new Vector2(-100, -100), Background, "Monospace", 10.0f, Color.Black, L_align);
            AddTextureSprite(frame, "SquareSimple", viewport.Center, viewport.Size, Color.Black, C_align);
        }

        void GeneratePatterns()
        {
            patternWideBar = xstr("", 174) + "\n" + xstr("", 172) + "\n" + xstr("", 170) + "\n" + xstr("", 168) + "\n" + xstr("" + xstr("", 166) + "\n", 17) + "" + xstr("", 168) + "\n" + xstr("", 170) + "\n" + xstr("", 172) + "\n" + xstr("", 174);
            patternSmallBar = xstr("", 85) + "\n" + xstr("", 83) + "\n" + xstr("", 81) + "\n" + xstr("" + xstr("", 79) + "\n", 12) + "" + xstr("", 81) + "\n" + xstr("", 83) + "\n" + xstr("", 85);
            Background = xstr(xstr("", 10) + "\n", 10).Trim();

            string[] LineDecoded = decodeLine1(13, "AaaAcmAgeAiqAkmAmyAtcAuaBxoAwmBaeBemBgyBncBnoBsmAyuBtsCnsCumBxcDaqGbpGebGkfJbdIjhGsfGurGybHgbHmfHorHpdGqnGlhJcfLjz");
            iconIce = decodedPattern(LineDecoded, "lWXYBJVJBYXWl");
            iconWindmill = decodedPattern(LineDecoded, "lWWcBNBNBcWWl");
            iconSolar = decodedPattern(LineDecoded, "lWXdFATAFdXWl");
            iconGrinder = decodedPattern(LineDecoded, "lWceJMGVOhkWl");
            iconRefinery = decodedPattern(LineDecoded, "lWWcCCEHHcWWlA");
            iconVent = decodedPattern(LineDecoded, "lWWgANANAgWWl");
            iconBlock = decodedPattern(LineDecoded, "lWZaIUPPQijWl");
            iconPara = decodedPattern(LineDecoded, "lWYfSRJFDYYWl");
            iconSorter = decodedPattern(LineDecoded, "lWWgHLKJHgWWl");
            iconEngine = decodedPattern(LineDecoded, "lWcWGGBBDbYWl");

            LineDecoded = decodeLine1(14, "AaaAhkAsmAuiAkmAfoBbsAbwBfkBmuDbcBnsBxoAfgBoyByuCgeAuqCiaAkuBtgBncDamGbkGbqGjaGmsGvyHokJccKqgLkoMddMknMofMvpNpxNqvMebMhtPdpVfrVnbVqtWtjYgd");
            iconReactor = decodedPattern(LineDecoded, "fYYcDDJFEbYYf");
            iconThruster = decodedPattern(LineDecoded, "sgggJJBBBBgggs");
            iconCollector = decodedPattern(LineDecoded, "eYZGCBASWLdYe");
            iconEjector = decodedPattern(LineDecoded, "eYdLSJABGCZYe");
            iconCargo = decodedPattern(LineDecoded, "sggkJHHHHJkggs");
            iconConnector = decodedPattern(LineDecoded, "eYdLSJAJSLdYe");
            iconDrill = decodedPattern(LineDecoded, "pghACAJAWAXggp");
            iconJumpdrive = decodedPattern(LineDecoded, "riiBAqPPqABiir");
            iconShield = decodedPattern(LineDecoded, "pgoLQOQMQLIjgp");
            iconBullet = decodedPattern(LineDecoded, "sgggAVOOVAgggs");
            iconRocket = decodedPattern(LineDecoded, "sggmHWKKWHmggs");
            iconWelder = decodedPattern(LineDecoded, "eYaDDDWLLLdYe");
            iconAssembler = decodedPattern(LineDecoded, "sggiCBBBCiggs");
            iconProjector = decodedPattern(LineDecoded, "sggoAJACAJijgs");
            iconDoor = decodedPattern(LineDecoded, "sgolLLOLLlogs");
            iconCockpit = decodedPattern(LineDecoded, "sggnNTRUWoggs");

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
            patternVent = decodedPattern(LineDecoded, "AEPIIIIIIIIIIIDDDIIIIIIIIIIIIPE");
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
        {
            string tempLine = "";
            for (int i = 0; i < patternWidth; i++)
            {
                if (temp % 2 != 0) { tempLine += ""; }
                else { tempLine += ""; }
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
        {
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

        bool getcolors(string option, string tagstart, out Color tempColor)
        {
            bool valid = false;
            int red = 0, green = 0, blue = 0;
            tempColor = new Color(0, 0, 0);
            var xyz = getBetween(option.ToLower(), tagstart.ToLower() + "(", ")").Split(',').ToArray();
            if (xyz.Length == 3)
            {
                valid = int.TryParse(xyz[0], out red) && int.TryParse(xyz[1], out green) && int.TryParse(xyz[2], out blue);
            }
            if (valid) { tempColor = new Color(red, green, blue); }
            return valid;
        }

        Color cvar(float percent)
        {
            float factor = Math.Max(1 - percent, percent);
            return new Color(percent / factor, (1 - percent) / factor, 0);
        }

        bool[] wordparse(string[] words, bool[] inv, string text)
        {
            int len = words.Length;
            bool[] res = new bool[len];
            for (int i = 0; i < len; i++)
            {
                res[i] = text.Contains(words[i].ToLower()) ^ inv[i];
            }
            return res;
        }

        void layoutswitch(int change)
        {
            if (LayoutCount > 1)
            {
                execCounter1 = 0;
                execCounter2 = 0;
                LayoutIndex += change + LayoutCount;
                LayoutIndex %= LayoutCount;
                Layout = Layouts[LayoutIndex];
            }
        }

        int NumParse(string argument, string trigger)
        {
            int output = 0;
            int.TryParse(argument.Substring(argument.IndexOf(trigger) + trigger.Length).Trim(), out output);
            return output;
        }

        TextAlignment get_align(string text, TextAlignment align)
        {
            TextAlignment res = align;
            if (text.Contains(aligntag[0])) { res = L_align; }
            else if (text.Contains(aligntag[1])) { res = C_align; }
            else if (text.Contains(aligntag[2])) { res = R_align; }
            return res;
        }

        const TextAlignment L_align = TextAlignment.LEFT;
        const TextAlignment C_align = TextAlignment.CENTER;
        const TextAlignment R_align = TextAlignment.RIGHT;

        const int ico = 0;
        const int txt = 1;
        const int per = 2;
        const int frm = 3;
        const int bar = 4;
        const int opt = 5;

        const int Nothing = 0;
        const int SmallBar = 1;
        const int WideBar = 2;
        const int SingleIcon = 3;
        const int SingleBlock = 4;
        const int Textline = 5;
        //------------END--------------
    }
}