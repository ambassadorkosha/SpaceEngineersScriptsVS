        //  ****************************************************************************************************************************************
        string ver="FSD Version 35b";
        //  ****************************************************************************************************************************************
        //  Config
        //  FSD options:nolinebreak no groups nosubgrids nosubgridLCDs
        //  ****************************************************************************************************************************************
        
        const string lcdtag =                   "ShowStats";       // Tag the script reacts to - write it to the custom data of your screen/cockpit
        const string endtag =                  "EndStats";          // Tag to end the command list (optional)
        const string def_tag =                 "FSD options:";    // Tag to switch the default options in the CustomData of the programming block
        const string FpM_tag =               "Framerate=";       // Tag to set the frame rate in the CustomData of the programming block
        const string LpM_tag =               "Layoutrate=";       // Tag to set the layout change rate in the CustomData of the programming block
        const string sequ_tag =              "sequence";         // Tag define the screen layout sequence in the CustomData of the programming block
        const string fast_tag =                "fastmode";          // Tag to activate the fastmode in the CustomData of the programming block
        const string panel_tag =             "Panel ";               // Tag to switch the default options in the CustomData of the programming block
        const string nostatustag =          "NoStatus";          // Tag to not to display FSD status on the PB
        const string gfxStart =                "gfxStart";             //Tag to indicate the start of a graphic sprite
        const string gfxEnd =                  "gfxEnd";              //Tag to indicate the end of a graphic sprite
        const string widebartag =          "WideBar";            // Tag to display a large chargebar instead of each block individually
        const string smallbartag =         "SmallBar";           // Tag to display a small chargebar instead of each block individually
        const string tallbartag =             "TallBar";              // Tag to display a large vertical chargebar instead of each block individually
        const string shortbartag =          "ShortBar";           // Tag to display a small vertical chargebar instead of each block individually
        const string singleicontag =       "SingleIcon";        // Tag to display only a single icon instead of each block individually
        const string noicontag =             "noIcons";             //Tag to display only the headlines without any graphics
        const string gaptag =                  "Gap";                   // Tag to display a gap between two blocks
        const string positiontag =           "Position";            // Tag to position the next blocks
        const string counttag =               "IconCount=";       // Tag to define the number of icons to be displayed
        const string fsizetag =                "Fontsize=";          // Tag to define the Fontsize of the headline
        const string scaletag =               "Scale=";               // Tag to define the Scalefactor of the icons
        const string lengthtag =              "Length=";             // Tag to define an alternative length of a chargebar
        const string referencetag =           "Reference=";            // Tag to define an alternative reference value
        const string scrlspdtag =            "ScrollSpeed=";   // Tag to define the Fontsize of the headline
        const string texttag =                  "Text:";                  //Tag to display a custom text
        const string spritetag =               "Sprite:";               //Tag to display a custom texture sprite
        const string nosubgridLCDtag = "nosubgridLCDs"; // Tag to exclude subgrid LCDs
        const string verbatimtag =          "verbatim";           // Tag to require exact Name match
        
        string[] PaintTag  = {
        "IconColor",       // Tag to repaint the icon
        "TextColor",       // Tag to repaint the texts
        "PercentColor", // Tag to repaint the percent number
        "FrameColor",   // Tag to repaint the frame
        "BarColor"         // Tag to repaint the fill level bar
        };
        
        string[] aligntag  = {
        "left",      // Tag to display text left aligned
        "center", // Tag to display text center aligned
        "right"     // Tag to display text right aligned
        };
        
                string[] opt_tag = {
        "optional",        // [0]Tag to display either power or inventory if a block has both (in case of battery it's load percentage or input/output)
        "addinfo",         // [1]Tag to display additional informations
        "addsize",        // [2]Tag to display size informations
        "health",           // [3]Tag to display additional healthbar
        "noheadline",   // [4]Tag to display the blocks without headline
        "nosubgrids",   // [5]Tag to exclude subgrids
        "onlysubgrids",// [6]Tag to only show blocks on subgrids
        "nogroups",      // [7]Tag to skip groups in the detection
        "nolinebreak",  // [8]Tag to prevent linebreaks
        "noscrolling",   // [9]Tag to prevent scrolling
        "noNames",      //[10]Tag to display only the headlines without the names
        "noCR"              // [11]Tag to force all the elements in one line
        };

        // The Separators. Used to indicate where the option keyword part begins
        char[] seperators = {'\n' , ',' , ':'};

        int FpM = 60; //How often the displays will be updated (In frames per minute)
        int LpM = 12;  //How often the display layouts switch  (In changes per minute)
        int SsD = 10;  //Default Scroll Speed

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
        Program() {
        GeneratePatterns();
        if (!Me.CustomName.EndsWith("[FSD]")) { Me.CustomName += " [FSD]";}
        cs = scrollpositions.GetUpperBound(0) + 1;
        ce = scrollpositions.GetUpperBound(1) + 1;
        Runtime.UpdateFrequency = speed;
        }
        void Save() {}
        
        void Main(string preargument, UpdateType updateType) {
        int surfaceIndex;
        temp_txt = "";
        if (Storage == "offline" && preargument == "") { argument = "shutdown";} else { argument = TL(preargument);}
        switch (argument) {
            case "layout+":
            layoutswitch(+1);
            break;
            case "layout-":
            layoutswitch(-1);
            break;
            default:
            if ((LayoutCount>1)&&(LpM>0))  {
                execCounter2++;
                if (execCounter2 >= TpM/LpM) {layoutswitch(+1);}
            }
            break;
        }
        if ((Me.CustomData.Contains(def_tag))&&(oldCustomData!=Me.CustomData)) {setdefault();}
        if (nosubgridLCDs) {
            ignoredGrids.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(surfaceProviders, (p => p.CustomData.Contains(lcdtag) && p.CubeGrid == Me.CubeGrid));
        } else {
            DetectIgnoredGrids();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(surfaceProviders, p => p.CustomData.Contains(lcdtag));
        }
        if (startdisp==0) {
            if  (FpM < 1) {FpM = 1;}
            execCounter1++;
            if (execCounter1 >= TpM/FpM) {execCounter1 = 0;}
            if ((argument=="") && (execCounter1!=0)) {return;}
            screenIndex = 0;
        }
        dispnr = 0;
        timeout = false;
        foreach (var block in surfaceProviders) {
            bool skipBlock = false;
            foreach (var grid in ignoredGrids) { if (block.CubeGrid == grid) {skipBlock = true;} }
            if (dispnr<startdisp) {dispnr++;continue;}
            if ((Runtime.CurrentInstructionCount+highload)>Runtime.MaxInstructionCount) {
            timeout = true;
            startdisp = dispnr;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            }
            if (skipBlock || timeout) {break;}
            dispnr++;
            var provider = block as IMyTextSurfaceProvider;
            Displayname = block.CustomName;
            if (provider.SurfaceCount<1) {
            Echo("         << F.S.D. Warning >>");
            Echo("The CustomData field of the block:");
            Echo('"'+Displayname+'"');
            Echo("contains the trigger word:");
            Echo('"'+lcdtag+'"');
            Echo("but doesn't provide any textpanels");
            Echo("");
            continue;
            }
            StartTag=lcdtag+" "+Layout;
            if (!("\n"+block.CustomData).Contains("\n"+StartTag)) {StartTag = lcdtag;}
            temp_txt = block.CustomData.Substring(block.CustomData.IndexOf(StartTag)).Trim();
            if (temp_txt.IndexOfAny(seperators)<0) {break;}
            while (temp_txt.StartsWith(lcdtag)) {temp_txt = temp_txt.Substring(temp_txt.IndexOfAny(seperators)).Trim();}
            if (temp_txt.Contains(endtag)) { temp_txt = temp_txt.Remove(temp_txt.IndexOf(endtag));}
            sections = ("\n" + temp_txt).Split(new[] {panel_tag , TL(panel_tag) , panel_tag.ToUpper()}, StringSplitOptions.None).Where(x => !string.IsNullOrEmpty(x)).Select(s => s.Trim()).ToArray();
            for (int i = 0; i < sections.Length; i++) {
            lines = sections[i].Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
            if (lines.Length == 0) {continue;}
            if ((int.TryParse(lines.First(), out surfaceIndex)) && (surfaceIndex < provider.SurfaceCount)) {
                sArgs = lines.Skip(1).ToArray();
            } else {
                surfaceIndex = 0;
                sArgs = lines;
            }
            PrepareSurface(block, surfaceIndex);
            DrawScreen(surface, argument);
            screenIndex++;
            }
        }
        string stat_txt = "";
        rotatecount %= 4;
        switch (argument) {
            case "version":
            Echo("Version: "+ver);
            break;
            case "shutdown":
            Storage = "offline";
            Runtime.UpdateFrequency = UpdateFrequency.None;
            stat_txt = "             << Notice >>\nThis F.S.D. entity is shut down.\nRun this programming block\nwith the argument " + '"' + "powerup" + '"' + "\nto restart it again.";
            break;
            case "powerup":
            Storage = "online";
            Runtime.UpdateFrequency = speed;
            stat_txt = "FSD is booting";
            Echo(" ");
            break;
            case "refresh":
            Runtime.UpdateFrequency = speed;
            stat_txt = "FSD is refreshing the displays";
            break;
            case "":
            stat_txt = "   << F.S.D. is active >>\n"+"|/-\u005C".Substring(rotatecount,1)+"\nThis programming block is\nhandling " + plSing("LCD Panel", screenIndex) + "\nat the moment.";
            break;
            default:
            if (argument.StartsWith(TL(LpM_tag))) {
                LpM = NumParse(argument,LpM_tag);
            }
            else if (argument.StartsWith(TL(FpM_tag))) {
                FpM = NumParse(argument,FpM_tag);
            }
            else if (argument.StartsWith("layout=")) {
                Layout = NumParse(argument,"layout=");
            }
            else if (argument.StartsWith(TL(scrlspdtag))) {
                SsD = NumParse(argument,scrlspdtag);
            }
            break;
        }
        if (!timeout) {
            Echo(stat_txt);
            if (startdisp > 0) {
            Echo("  ");
            Echo("       << F.S.D. Warning >>");
            Echo("high Programmable Block load");
            }
            startdisp = 0;
            Runtime.UpdateFrequency = speed;
            rotatecount++;
        }
        if (!nostatus) {
            PrepareSurface(Me);
            if (Me.BlockDefinition.SubtypeName.StartsWith("S")) {
            AddTextSprite(viewport.Center + new Vector2(0, -50), stat_txt.Trim(), "Debug", 0.7f, Color.White, C_align);
            } else {
            AddTextSprite(viewport.Center + new Vector2(0, -90), stat_txt.Trim(), "Debug", 1.3f, Color.White, C_align);
            }
        frame.Dispose();
        }
        }
        
        void DetectIgnoredGrids() {
        List<IMyProgrammableBlock> pbs = new List<IMyProgrammableBlock>();
        ignoredGrids.Clear();
        GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(pbs, x => x.CustomName.Contains("[FSD]") && !(x.CubeGrid == Me.CubeGrid));
        foreach (var pb in pbs) { ignoredGrids.Add(pb.CubeGrid);}
        }
        
        void DrawScreen(IMyTextSurface surface, string argument) {
        pos = LineStartpos = viewport.Position + new Vector2(2.88f, 2.88f);
        string[] groupAndOptions;
        string c_arg;
        float gfx_res = 0.1f;
        bool continueDrawing = true;
        lastDraw = Nothing;
        lastLength = 0;
        int optionpos = 0;
        int texttagpos = 0;
        int spritetagpos = 0;
        headlineIndex = 0;
        if (Storage == "offline" || argument == "shutdown" || argument == "refresh") {
            AddTextSprite(pos, " ", "Debug", 1, Color.Black, L_align);
        } else {
            get_sprite = false;
            gfxSprite.Clear();
            foreach (var arg in sArgs) {
            if (get_sprite) {
                if (TL(arg).TrimStart(new[] { ':', ',' }) == TL(gfxEnd)) {
                AddTextSprite(pos, gfxSprite.ToString(), "Monospace", gfx_res, Color.White, L_align);
                get_sprite = false;
                gfxSprite.Clear();
                } else {
                gfxSprite.AppendLine(arg);
                }
                continue;
            }
            LineStartpos.X = viewport.X + 2.88f;
            optionpos = arg.IndexOfAny(new char[] { ',', ':' });
            containertype = MultiIcon;
            added_txt = "";
            scale = 1;
            iconcount = -1;
            set_length = 0;
            reference = 0;
            verti = horiz = false;
            for(int i=0;i<paints.Length;i++) {paints[i] = false;}
            Colors[ico] = pictogramColor;
            Colors[txt] = headlineColor;
            Colors[per] = percentDisplayColor;
            Colors[opt] = optioncolor;
            fontsize = 0f;
            scrl_spd = SsD;
            std_length = viewport.Width - LineStartpos.X;
            min_length = 1;
            if (optionpos >= 0) {
                c_arg = TL(arg).Substring(optionpos + 1);
                spritetagpos = c_arg.IndexOf(TL(spritetag));
                texttagpos = c_arg.IndexOf(TL(texttag));
                if (spritetagpos >= 0) {
                var sprite = new MySprite();
                if (getsprite(arg.Substring(spritetagpos + spritetag.Length + optionpos + 1), out sprite)) {frame.Add(sprite);}
                }
                else if (texttagpos >= 0) {
                added_txt = arg.Substring(texttagpos + texttag.Length + optionpos + 1);
                c_arg = c_arg.Remove(texttagpos);
                } else {
                added_txt = "";
                }
                if (c_arg.StartsWith(TL(gfxStart))) {
                if (getVector(c_arg, gfxStart, out tempvector, out gfx_res)) {
                    pos = LineStartpos = tempvector + viewport.Position;
                    get_sprite = true;
                    gfxSprite.Clear();
                    continue;
                }
                }
                if (have(c_arg,positiontag)) {
                if (getVector(c_arg, positiontag, out tempvector, out gfx_res)) { pos = LineStartpos = tempvector + viewport.Position; lastDraw = Nothing;}
                }
                textpos = get_align(c_arg,textposdefault);
                opt_chc = wordparse(opt_tag,opt_def,c_arg);
                for(int i=0;i<paints.Length;i++) {
                if (have(c_arg,PaintTag[i])) {paints[i] = getcolors(c_arg, PaintTag[i], out Colors[i]);}
                }
                if (!paints[ico]) {Colors[ico] = pictogramColor;}
                if (!paints[txt]) {Colors[txt] = headlineColor;}
                if (paints[per]) {
                Colors[opt] = Colors[per];
                } else {
                Colors[per] = percentDisplayColor;
                Colors[opt] = optioncolor;
                }
                scrl_spd = intparse(c_arg, scrlspdtag, 0, SsD);
                iconcount =  intparse(c_arg, counttag, 0, -1);
                fontsize = floatparse(c_arg, fsizetag, 0.1f, 0);
                scale = floatparse(c_arg, scaletag, 0.5f, 1);
                set_length = floatparse(c_arg, lengthtag, 0.1f, 0);
                reference = floatparse(c_arg, referencetag, 0, 0);
                std_length = viewport.Width - LineStartpos.X;
                if (have(c_arg,widebartag)) {
                horiz = true;
                containertype = WideBar;
                std_length = 501.12f;
                min_length = 188.8f;
                }
                else if (have(c_arg,smallbartag)) {
                horiz = true;
                containertype = SmallBar;
                std_length = 241.92f;
                min_length = 83.04f;
                }
                else if (have(c_arg,tallbartag)) {
                verti = true;
                containertype = TallBar;
                std_length = 501.12f;
                min_length = 123.12f;
                }
                else if (have(c_arg,shortbartag)) {
                verti = true;
                containertype = ShortBar;
                std_length = 239.04f;
                min_length = 89.04f;
                }
                else if (have(c_arg,singleicontag)) {
                containertype = SingleIcon;
                }
                else if (have(c_arg,noicontag)) {
                horiz = true;
                containertype = Textline;
                }
                if (horiz||verti) {scale=1;}
                c_arg = arg.Substring(0, optionpos);
            } else {
                opt_chc = opt_def;
                added_txt = "";
                c_arg = arg;
            }
            if (set_length < min_length) {set_length = std_length;}
            groupAndOptions = c_arg.Trim().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            if (continueDrawing) {
                continueDrawing = CycleDrawBlocks(groupAndOptions);
                lastDraw = containertype;
                lastscale = scale;
                lastLength = set_length;
            }
            }
        }
        frame.Dispose();
        }
        
        bool CycleDrawBlocks(string[] groupOrBlocknames) {
        Vector2 posContainerGroup = nulvec;
        float percent = 0;
        float integrity = 0;
        bool continueDrawing = true;
        ClearGroupArrays();
        Group.Clear();
        headline.Clear();
        bool same = (containertype == lastDraw);
        if (same && opt_chc[11]) {lastDraw = Nothing;}
        switch (lastDraw) {
            case WideBar:
            if (!same || (pos.X + lastLength) >= viewport.Width) { LineStartpos.Y += 72; pos = LineStartpos;}
            break;
            case SmallBar:
            if (!same || (pos.X + lastLength) >= viewport.Width) { LineStartpos.Y += 54; pos = LineStartpos;}
            break;
            case TallBar:
            if (!same || (lastLength != set_length) || (pos.X + 72) >= viewport.Width) { LineStartpos.Y += lastLength + 2.88f; pos = LineStartpos;}
            break;
            case ShortBar:
            if (!same || (lastLength != set_length) || (pos.X + 62) >= viewport.Width) { LineStartpos.Y += lastLength + 2.88f; pos = LineStartpos;}
            break;
            case SingleIcon:
            if (!same || (pos.X + 72*lastscale) >= viewport.Width || !opt_chc[4]) { LineStartpos.Y += 95*lastscale; pos = LineStartpos;}
            break;
            case MultiIcon:
            LineStartpos.Y += 95*lastscale; pos = LineStartpos;
            break;
            case Textline:
            LineStartpos.Y += (28.75f*last_fsize); pos = LineStartpos;
            break;
            default: break;
        }
        if (fontsize==0f) {
            if (containertype==WideBar || containertype==TallBar) {
            fontsize=1.3f;
            } else {
            fontsize=0.8f;
            }
        }
        float offset = 28.75f*fontsize;
        if ((containertype == MultiIcon || containertype == SingleIcon) && (!opt_chc[4] || added_txt != "")) {
            LineStartpos.Y += offset;
            posContainerGroup = pos = LineStartpos;
        }
        foreach (string entry in groupOrBlocknames) {
            string name = entry.Trim();
            iconcounter = 0;
            if (iconcount < 0) {
            if (containertype == MultiIcon && name.EndsWith("#")) {
                iconcounter = name.Length - name.TrimEnd('#').Length;
                name = name.TrimEnd('#');
            }
            } else {
            iconcounter = iconcount;
            }
            verbatim = verbatimdefault || (name.StartsWith("" + '"') && name.EndsWith("" + '"'));
            if (verbatim) { name = name.Trim('"');}
            var foundgroup = GridTerminalSystem.GetBlockGroupWithName(name);
            if (foundgroup != null && !opt_chc[7]) {
            if (groupOrBlocknames.Length == 1 && added_txt == "" && !opt_chc[10] && !opt_chc[4]) {
                headline.Append(foundgroup.Name + ": ");
            }
            foundgroup.GetBlocks(Group, (x => (x.CubeGrid == Me.CubeGrid && opt_chc[5]) || (!opt_chc[5] && !opt_chc[6]) || (x.CubeGrid != Me.CubeGrid && opt_chc[6])));
            } else {
            Group.Clear();
            }
            if (Group.Count == 0) {
            GridTerminalSystem.SearchBlocksOfName(name, Group, (x => (x.CubeGrid == Me.CubeGrid && opt_chc[5]) || (!opt_chc[5] && !opt_chc[6]) || (x.CubeGrid != Me.CubeGrid && opt_chc[6])));
            }
            for (int i = 0; i < iconcounter; i++) { Group.Add(Me);}
            if (have(TL(name),gaptag)) {
            int gap = 0;
            if (int.TryParse(TL(name).Replace(TL(gaptag), ""), out gap)) {
                pos.X += gap;
                Group.Clear();
            }
            }
            else if (Group.Count < 1) {
            Echo("         << F.S.D. Warning >>");
            Echo("No group or block with the name:");
            Echo('"' + name + '"');
            Echo("found as listed in the to-do list of:");
            Echo('"' + Displayname + '"');
            }
            bool continuegroup = true;
            int place_used = 0;
            foreach (var block in Group) {
            if (verbatim && block != Me && block.CustomName != name) { continue;}
            if (!GetBlockProperties(block, ref percent,ref integrity)) { continue;}
            percent = Math.Abs(percent);
            if (iconcounter > 0 && place_used >= iconcounter) { continue;}
            if (!continuegroup || containertype != MultiIcon) { continue;}
            if ((pos.X + 72*scale) > viewport.Width && !opt_chc[11]) {
                if (!opt_chc[8]) {
                if ((pos.Y + 170*scale) > viewport.Bottom) {
                    continuegroup = false;
                } else {
                    LineStartpos.Y += 95*scale;
                    pos = LineStartpos;
                }
                } else {
                continuegroup = false;
                }
            }
            if (continuegroup) {
                if (pattern == patternNone) {
                percent = 0;
                }
                GetChargeStateValues(percent);
                integrity = TestForNaN(integrity);
                integrityLevelColor  = cvar(1 - (float)Math.Pow(integrity,3));
                DrawObject(pos, integrity);
                pos.X += 72*scale;
                place_used++;
            }
            }
        }
        if (sumAll[0] + sumBlock == 0 && added_txt != "") {
            if (containertype == MultiIcon) { LineStartpos.Y -= offset; pos = LineStartpos;}
            containertype = Textline;
        }
        else if (sumAll[0] > 0) {
            percent = (int)Math.Round(sumAll[1] / sumAll[0], 0);
        }
        switch (containertype) {
            case WideBar:
            GetChargeStateValues(percent);
            scrollrange = set_length-28.8f;
            posHeadline = pos + new Vector2(14.4f, 36.4f-(16.63f*fontsize));
            fieldpos = pos + new Vector2(14.4f, 12);
            fieldsize = new Vector2(scrollrange, 49);
            DrawObject(pos);
            pos.X += set_length + 2.88f;
            break;
            case SmallBar:
            GetChargeStateValues(percent);
            scrollrange = set_length-23.04f;
            posHeadline = pos + new Vector2(11.52f, 28.36f-(17.95f*fontsize));
            fieldpos = pos + new Vector2(11.52f, 9);
            fieldsize = new Vector2(scrollrange, 33);
            DrawObject(pos);
            pos.X += set_length + 2.88f;
            break;
            case TallBar:
            GetChargeStateValues(percent);
            scrollrange = set_length-123.12f;
            posHeadline = pos + new Vector2(36, 109);
            fieldpos = pos + new Vector2(12, 109);
            fieldsize = new Vector2(48, scrollrange);
            DrawObject(pos);
            pos.X += 75;
            break;
            case ShortBar:
            GetChargeStateValues(percent);
            scrollrange = set_length-89.04f;
            posHeadline = pos + new Vector2(30.5f, 80);
            fieldpos = pos + new Vector2(10, 80);
            fieldsize = new Vector2(41, scrollrange);
            DrawObject(pos);
            pos.X += 64;
            break;
            case SingleIcon:
            GetChargeStateValues(percent);
            posHeadline = fieldpos = posContainerGroup + new Vector2(0, -offset);
            scrollrange = set_length;
            fieldsize = new Vector2(scrollrange,offset);
            DrawObject(pos);
            pos.X += 72*scale;
            break;
            case MultiIcon:
            posHeadline = fieldpos = posContainerGroup + new Vector2(0, -offset);
            scrollrange = set_length;
            fieldsize = new Vector2(scrollrange,offset);
            break;
            case Textline:
            posHeadline = fieldpos = LineStartpos;
            scrollrange = set_length;
            fieldsize = new Vector2(scrollrange,offset);
            if ((pos.Y + offset) >= viewport.Height) { continueDrawing = false;}
            break;
        }
        if (containertype!=Textline) {
            if (pos.Y > viewport.Height) { continueDrawing = false;}
        }
        headline.Append(added_txt);
        if (!opt_chc[4]) {
            if (opt_chc[10]) {
            StopSep = StartSep = "";
            } else {
            if (verti) {
                StartSep = "["; StopSep = "]";
            } else {
                StartSep = "[ "; StopSep = " ]";
            }
            }
            if (sumBat[0] > 0) {
            headline.Append(hlstart("Battery", sumBat[0]));
            if (opt_chc[0]) { headline.Append("In " + TruncateUnit(sumBat[1], "watt") + "Out ");}
            else { headline.Append(f_percent(sumBat));}
            headline.Append(TruncateUnit(sumBat[2], "whr") + StopSep);
            }
            if (sumReactor[0] > 0) {
            headline.Append(hlstart("Reactor", sumReactor[0]));
            if (opt_chc[0]) { headline.Append("Output " + f_percent(sumReactor) + TruncateUnit(sumReactor[2], "watt") + StopSep);}
            else { headline.Append(TruncateUnit(sumReactor[2], "kg") + "Uranium " + StopSep);}
            }
            if (sumGasgen[0] > 0) {
            headline.Append(hlstart("Hydrogen Engine", sumGasgen[0]));
            if (opt_chc[0]) { headline.Append("Output " + f_percent(sumGasgen) + TruncateUnit(sumGasgen[2], "watt") + StopSep);}
            else { headline.Append(f_percent(sumGasgen) + TruncateUnit(sumGasgen[2], "lit") + StopSep);}
            }
            if (sumRefinery[0] > 0) {
            headline.Append(hlstart("Refinery", sumRefinery[0]));
            if (opt_chc[0]) { headline.Append("Ingots " + f_percent(sumRefinery) + TruncateUnit(sumRefinery[2], "c_l") + StopSep);} else { headline.Append("Ores " + f_percent(sumRefinery) + TruncateUnit(sumRefinery[2], "c_l") + StopSep);}
            }
            if (sumAssembler[0] > 0) {
            headline.Append(hlstart("Assembler", sumAssembler[0]));
            if (opt_chc[0]) { headline.Append("Items Inventory " + f_percent(sumAssembler) + "Item Completion: " + Math.Round(sumAssembler[2] * 100 / sumAssembler[0], 0) + "% " + StopSep);}
            else { headline.Append("Ingot Inventory " + f_percent(sumAssembler) + "Production Queue: " + plSing(" Item", sumAssembler[2]) + StopSep);}
            }
            if (sumDoor[0] > 0) {
            headline.Append(hlstart("Door", sumDoor[0]) + f_percent(sumDoor));
            if (opt_chc[0]) {headline.Append("open" + StopSep);} else {headline.Append("closed" + StopSep);}
            }
            if (sumRotor[0] > 0) {
            headline.Append(hlstart("Rotor", sumRotor[0]) + f_percent(sumRotor));
            if (opt_chc[0]) {headline.Append("to the right" + StopSep);} else {headline.Append("to the left" + StopSep);}
            }
            if (sumHinge[0] > 0) {
            headline.Append(hlstart("Hinge", sumHinge[0]) + f_percent(sumHinge));
            if (opt_chc[0]) {headline.Append("to the right" + StopSep);} else {headline.Append("to the left" + StopSep);}
            }
            if (sumCockpit[0] > 0) {
            headline.Append(hlstart("Cockpit", sumCockpit[0]) + f_percent(sumCockpit));
            if (opt_chc[0]) {headline.Append("occupied" + StopSep);} else {headline.Append("Storage" + TruncateUnit(sumCockpit[2], "c_l") + StopSep);}
            }
            if (sumJdrives[0] > 0) { headline.Append(hlstart("Jumpdrive", sumJdrives[0]) + f_percent(sumJdrives) + TruncateUnit(sumJdrives[2], "whr") + StopSep);}
            if (sumCargo[0] > 0) { headline.Append(hlstart("Container", sumCargo[0]) + f_percent(sumCargo) + TruncateUnit(sumCargo[2], "c_l") + StopSep);}
            if (sumPara[0] > 0) { headline.Append(hlstart("Parachute Crate", sumPara[0]) + f_percent(sumPara) + TruncateUnit(sumPara[2], "c_l") + StopSep);}
            if (sumSorter[0] > 0) { headline.Append(hlstart("Sorter", sumSorter[0]) + f_percent(sumSorter) + TruncateUnit(sumSorter[2], "c_l") + StopSep);}
            if (sumCollectors[0] > 0) { headline.Append(hlstart("Collector", sumCollectors[0]) + f_percent(sumCollectors) + TruncateUnit(sumCollectors[2], "c_l") + StopSep);}
            if (sumConnectors[0] > 0) { headline.Append(hlstart("Connector", sumConnectors[0]) + f_percent(sumConnectors) + TruncateUnit(sumConnectors[2], "c_l") + StopSep);}
            if (sumEjectors[0] > 0) { headline.Append(hlstart("Ejector", sumEjectors[0]) + f_percent(sumEjectors) + TruncateUnit(sumEjectors[2], "c_l") + StopSep);}
            if (sumDrills[0] > 0) { headline.Append(hlstart("Drill", sumDrills[0]) + f_percent(sumDrills) + TruncateUnit(sumDrills[2], "c_l") + StopSep);}
            if (sumWelder[0] > 0) { headline.Append(hlstart("Welder", sumWelder[0]) + f_percent(sumWelder) + TruncateUnit(sumWelder[2], "c_l") + StopSep);}
            if (sumGrinder[0] > 0) { headline.Append(hlstart("Grinder", sumGrinder[0]) + f_percent(sumGrinder) + TruncateUnit(sumGrinder[2], "c_l") + StopSep);}
            if (sumHydro[0] > 0) { headline.Append(hlstart("Hydrogen Tank", sumHydro[0]) + f_percent(sumHydro) + TruncateUnit(sumHydro[2], "lit") + StopSep);}
            if (sumOxy[0] > 0) { headline.Append(hlstart("Oxygen Tank", sumOxy[0]) + f_percent(sumOxy) + TruncateUnit(sumOxy[2], "lit") + StopSep);}
            if (sumEShield[0] > 0) { headline.Append(hlstart("Shield Generator", sumEShield[0]) + f_percent(sumEShield) + "Required Input: " + eShieldInput + " " + StopSep);}
            if (sumShield[0] > 0) { headline.Append(hlstart("Shield Controller", sumShield[0]) + Math.Round(sumShield[1], 0) + "% Overheated: " + Math.Round(sumShield[2], 0) + "% " + StopSep);}
            if (sumMissileTurret[0] > 0) { headline.Append(hlstart("Missile Turret", sumMissileTurret[0]) + f_percent(sumMissileTurret) + plSing(" Missile", sumMissileTurret[2]) + " " + StopSep);}
            if (sumGatlingTurret[0] > 0) { headline.Append(hlstart("Gatling Turret", sumGatlingTurret[0]) + f_percent(sumGatlingTurret) + plSing(" Unit", sumGatlingTurret[2]) + " " + StopSep);}
            if (sumMissileLauncher[0] > 0) { headline.Append(hlstart("Rocket Launcher", sumMissileLauncher[0]) + f_percent(sumMissileLauncher) + plSing(" Rocket", sumMissileLauncher[2]) + " " + StopSep);}
            if (sumGatling[0] > 0) { headline.Append(hlstart("Gatling Gun", sumGatling[0]) + f_percent(sumGatling) + plSing(" Unit", sumGatling[2]) + " " + StopSep);}
            if (sumThrust[0] > 0) { headline.Append(hlstart("Thruster", sumThrust[0]) + f_percent(sumThrust) + "Eff. " + Math.Round(sumThrust[2] * 100 / sumThrust[0], 0) + "% Override" + StopSep);}
            if (sumSolar[0] > 0) { headline.Append(hlstart("Solar Panel", sumSolar[0]) + "Output " + f_percent(sumSolar) + TruncateUnit(sumSolar[2], "watt") + StopSep);}
            if (sumWindmill[0] > 0) { headline.Append(hlstart("Wind Turbine", sumWindmill[0]) + "Output " + f_percent(sumWindmill) + TruncateUnit(sumWindmill[2], "watt") + StopSep);}
            if (sumO2Gen[0] > 0) { headline.Append(hlstart("O2H2 Generator", sumO2Gen[0]) + f_percent(sumO2Gen) + TruncateUnit(sumO2Gen[2], "c_l") + StopSep);}
            if (sumProjector[0] > 0) { headline.Append(hlstart("Projector", sumProjector[0]) + f_percent(sumProjector) + "Remaining: " + plSing(" Block", sumProjector[2]) + StopSep);}
            if (sumVent[0] > 0) { headline.Append(hlstart("Air Vent", sumVent[0]) + f_percent(sumVent) + "Pressure" + StopSep);}
            if (sumPiston[0] > 0) { headline.Append(hlstart("Piston", sumPiston[0]) + f_percent(sumPiston) + "extended" + StopSep);}
            if (sumBlock > 0) { headline.Append(hlstart("unidentified Block", sumBlock) + StopSep);}
        }
        DrawHeadline();
        last_fsize = fontsize;
        return continueDrawing;
        }
        
        void DrawHeadline() {
        var sprite = new MySprite() {
            Type = SpriteType.CLIP_RECT,
            Position = fieldpos,
            Size = fieldsize,
        };
        frame.Add(sprite);
        if (screenIndex < cs && headlineIndex < ce) { scrollindex = scrollpositions[screenIndex, headlineIndex];} else {scrollindex = 0;}
        StringBuilder headline_mod = new StringBuilder();
        string dim = "X", outtxt;
        bool scrolling = !opt_chc[9];
        if (verti) {
            for (int i = 0; i < headline.Length; i++) {headline_mod.Append(headline[i]+"\n");}
            dim = "Y";
        } else {
            headline_mod = headline;
        }
        scrolling &= (scrollindex > 0 || sb_size(headline_mod,dim) > (scrollrange + sb_size(new StringBuilder("\n"),dim)));
        float second_pos = scrl_spd*(int)((sb_size(headline_mod,dim)+(scrollrange/2))/scrl_spd);
        outtxt = headline_mod.ToString();
        if (verti) {
            AddTextSprite(posHeadline - new Vector2(0, scrollindex) , outtxt, "Debug", fontsize, Colors[txt], C_align);
            if (scrolling) {AddTextSprite(posHeadline + new Vector2(0,(second_pos - scrollindex)) , outtxt, "Debug", fontsize, Colors[txt], C_align);}
        } else {
            TextAlignment align = textpos;
            if (scrolling) {posHeadline.X -= scrollindex; align = L_align;}
            else if (textpos == R_align) { posHeadline.X += scrollrange;}
            else if (textpos == C_align) { posHeadline.X += (scrollrange / 2);}
            AddTextSprite(posHeadline , outtxt, "Debug", fontsize, Colors[txt], align);
            if (scrolling) {
            posHeadline.X+=second_pos;
            AddTextSprite(posHeadline , outtxt, "Debug", fontsize, Colors[txt], align);
            }
        }
        if ((scrollindex + scrl_spd) >= second_pos || !scrolling) {
            scrollindex = 0;
        } else {
            scrollindex+=scrl_spd;
        }
        frame.Add(MySprite.CreateClearClipRect());
        headline.Clear();
        if (!opt_chc[9] && screenIndex < 50 && headlineIndex < 20) {
            scrollpositions[screenIndex, headlineIndex] = scrollindex;
            headlineIndex++;
        }
        }
        
        float sb_size(StringBuilder sb, string dim = "X") {
        var t_vec = surface.MeasureStringInPixels(sb, "Debug", fontsize);
        if (dim=="X") {return t_vec.X;} else {return t_vec.Y;}
        }
        
        string hlstart(string str, float number) {
        if (opt_chc[10]) { return StartSep;}
        else { return StartSep + plSing(str, number) + ": ";}
        }
        
        string plSing(string str, float number) {
        if (number != 1) {
            if (str.EndsWith("ry")) { return number + " " + str.Replace("ry", "ries");}
            else { return number + " " + str + "s";}
        }
        else { return number + " " + str;}
        }
        
        string TruncateUnit(float number, string unit) {
        string str = "";
        if (unit == "whr") { str = calculateunits(number, " T", " G", " M", " K", 107.4162075f, 10) + "Wh";}
        if (unit == "lit") { str = calculateunits(number / 1000, "G ", "M ", "K ", " ", 107.4162075f, 9) + "L";}
        if (unit == "c_l") { str = calculateunits(number, "G ", "M ", "K ", " ", 107.4162075f, 9) + "L";}
        if (unit == "watt") { str = calculateunits(number, " T", " G", " M", " K", 107.4162075f, 9) + "W";}
        if (unit == "kg") { str = calculateunits(number, "K ton", " ton", " Kg", " g", 133.8810875f, 11);}
        return str + "  ";
        }
        
        string calculateunits(float number, string sign1, string sign2, string sign3, string sign4, float width, int len) {
        StringBuilder sb = new StringBuilder();
        if (number >= 1000000) { sb.Append(Math.Round((number / 1000000f), 2) + sign1);}
        else if (number >= 1000) { sb.Append(Math.Round((number / 1000f), 2) + sign2);}
        else if (number >= 1) { sb.Append(Math.Round(number, 2) + sign3);}
        else { sb.Append(Math.Round((number * 1000f), 2) + sign4);}
        return adjust_length(width, len, sb);
        }
        
        string f_percent(float[] werte) {
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("{0:N0}% ", TestForNaN(werte[1] / werte[0]));
        return adjust_length(61.491895f, 5, sb);
        }
        
        String adjust_length(float soll, int len, StringBuilder sb) {
        int temp_i;
        if (verti) {
            temp_i = Math.Max(len-sb.Length,0);
        } else {
            temp_i = (int)Math.Max(((soll*fontsize) - sb_size(sb)) / sb_size(new StringBuilder(" ")),0f);
        }
        sb.Insert(0, "                     ".Substring(0,temp_i));
        return sb.ToString();
        }
        
        bool GetBlockProperties(IMyTerminalBlock block, ref float percent,ref float integrity) {
        invertColor = false;
        bool isValidBlock = true;
        string blocktype = "";
        string subtype = "";
        bsize = 0;
        optionchoice = "";
        if (block.BlockDefinition.SubtypeName.Contains("DSControl")) {
            sumShield[0]++; sumAll[0]++;
            float.TryParse(getBetween(block.CustomInfo, " (", "%)"), out percent);
            sumShield[1] = percent; sumAll[1] += percent;
            float.TryParse(getBetween(block.CustomInfo, "[Over Heated]: ", "%"), out sumShield[2]);
            icon = iconShield;
            pattern = patternShield;
        }
        else if (block.BlockDefinition.SubtypeName.Contains("ShieldGenerator")) {
            if (!block.CustomName.Contains(":")) { block.CustomName = block.CustomName + ":";}
            sumEShield[0]++; sumAll[0]++;
            percent = TestForNaN(100 / EnergyShield.MaxHitpoints(block) * EnergyShield.CurrentHitpoints(block));
            sumEShield[1] = percent; sumAll[1] += percent;
            eShieldInput = EnergyShield.RequiredInput(block);
            icon = iconShield;
            pattern = patternShield;
        }
        else if (block == Me) {
            icon = "Cross";
            pattern = patternNone;
            optionchoice = "UNKNOWN";
            drawpercent = "???";
            percent = 0;
        } else {
            blocktype = block.ToString().Remove(block.ToString().IndexOf('{')).Trim();
            subtype = block.BlockDefinition.SubtypeName;
        }
        float maxvol;
        switch (blocktype) {
            case "MyBatteryBlock":
            bsize = get_size(subtype,'B');
            var bat = block as IMyBatteryBlock;
            float maxstore, maxout, maxin;
            maxstore = set_ref(bat.MaxStoredPower,0.001f);
            maxout = set_ref(bat.MaxOutput,0.001f);
            maxin = set_ref(bat.MaxInput,0.001f);
            if (opt_chc[0]) {
                optionchoice = "Load";
                percent = clamp(100 * bat.CurrentOutput / maxout) - clamp(100 * bat.CurrentInput / maxin);
                sumBat[1] += bat.CurrentInput;
                sumBat[2] += bat.CurrentOutput;
            } else {
                optionchoice = "Charge";
                percent = clamp(100 * bat.CurrentStoredPower / maxstore);
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
            bsize = get_size(subtype,'A');
            if (opt_chc[0]) {
                optionchoice = "Load";
                invertColor = true;
                PowerOutput2Arr(block, sumReactor, ref percent);
            } else {
                optionchoice = "Fuel";
                CargoVol2Arr(block, sumReactor, ref percent, true);
            }
            icon = iconReactor;
            pattern = patternReactor;
            break;
            case "MyHydrogenEngine":
            bsize = get_size(subtype,'T');
            if (opt_chc[0]) {
                optionchoice = "Load";
                invertColor = true;
                PowerOutput2Arr(block, sumGasgen, ref percent);
            } else {
                optionchoice = "Fuel";
                float MaxVol,CurVol;
                string temp_text = block.DetailedInfo;
                if(temp_text.Contains("л")) {
                float.TryParse(getBetween(temp_text, " л/", " л)"), out MaxVol);
                float.TryParse(getBetween(temp_text, " (", " л/"), out CurVol);
                } else {
                float.TryParse(getBetween(temp_text, "L/", "L)"), out MaxVol);
                float.TryParse(getBetween(temp_text, " (", "L/"), out CurVol);
                }
                MaxVol = set_ref(MaxVol);
                sumGasgen[0]++;
                sumAll[0]++;
                percent = clamp(100 * CurVol / MaxVol);
                sumGasgen[1] += percent;
                sumAll[1] += percent;
                sumGasgen[2] += CurVol;
            }
            icon = iconEngine;
            pattern = patternGasgen;
            break;
            case "MyCargoContainer":
            bsize = get_size(subtype,'A');
            isValidBlock = CargoVol2Arr(block, sumCargo, ref percent);
            icon = iconCargo;
            optionchoice = "Fill";
            pattern = patternCargo;
            invertColor = true;
            break;
            case "MyCockpit":
            case "MyCryoChamber":
            if (opt_chc[0]) {
                var cp = block as IMyCockpit;
                if (cp.IsUnderControl) {percent = 100; optionchoice = "occupied";} else {percent = 0; optionchoice = "empty";}
                sumCockpit[1] += percent;
                sumAll[1] += percent;
                sumCockpit[0]++;
                sumAll[0]++;
            } else {
                isValidBlock = CargoVol2Arr(block, sumCockpit, ref percent);
                optionchoice = "Filled";
                invertColor = true;
            }
            icon = iconCockpit;
            pattern = patternCargo;
            break;
            case "MyGasTank":
            if (block.BlockDefinition.SubtypeName.Contains("Hydro")) {
                bsize = get_size(subtype,'H');
                TankVolume2Arr(block, sumHydro, ref percent);
                icon = "IconHydrogen";
            } else {
                bsize = get_size(subtype,'T');
                TankVolume2Arr(block, sumOxy, ref percent);
                icon = "IconOxygen";
            }
            optionchoice = "Fill";
            pattern = patternTank;
            break;
            case "MyShipConnector":
            bsize = get_size(subtype,'C');
            if ((bsize==1) || (subtype=="ConnectorLarge_SEBuilder4")) {
                isValidBlock = CargoVol2Arr(block, sumEjectors, ref percent);
                icon = iconEjector;
            } else {
                isValidBlock = CargoVol2Arr(block, sumConnectors, ref percent);
                icon = iconConnector;
            }
            optionchoice = "Fill";
            pattern = patternConnector;
            invertColor = true;
            break;
            case "MyCollector":
            bsize = get_size(subtype,'T');
            isValidBlock = CargoVol2Arr(block, sumCollectors, ref percent);
            optionchoice = "Fill";
            icon = iconCollector;
            pattern = patternConnector;
            invertColor = true;
            break;
            case "MyShipDrill":
            bsize = get_size(subtype,'T');
            isValidBlock = CargoVol2Arr(block, sumDrills, ref percent);
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
            bsize = get_size(subtype,'A');
            var thruster = block as IMyThrust;
            sumThrust[0]++;
            sumAll[0]++;
            percent = 100 / thruster.MaxEffectiveThrust * thruster.CurrentThrust;
            if (percent > 100) { percent = 0;}
            sumThrust[1] += percent;
            sumAll[1] += percent;
            sumThrust[2] += thruster.ThrustOverridePercentage;
            icon = iconThruster;
            optionchoice = "Thrst";
            pattern = patternThruster;
            break;
            case "MySolarPanel":
            bsize = get_size(subtype,'T');
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
            bsize = get_size(subtype,'T');
            isValidBlock = CargoVol2Arr(block, sumO2Gen, ref percent);
            icon = iconIce;
            optionchoice = "Fill";
            pattern = patternGasgen;
            break;
            case "MyShipGrinder":
            bsize = get_size(subtype,'T');
            isValidBlock = CargoVol2Arr(block, sumGrinder, ref percent);
            icon = iconGrinder;
            optionchoice = "Fill";
            pattern = patternTool;
            invertColor = true;
            break;
            case "MyShipWelder":
            bsize = get_size(subtype,'T');
            isValidBlock = CargoVol2Arr(block, sumWelder, ref percent);
            icon = iconWelder;
            optionchoice = "Fill";
            pattern = patternTool;
            invertColor = true;
            break;
            case "MyRefinery":
            bsize = get_size(subtype,'R');
            IMyInventory Ref_inv = null;
            var Refblock = block as IMyRefinery;
            if (!opt_chc[0]) {
                optionchoice = "Ores";
                Ref_inv = Refblock.InputInventory;
            } else {
                optionchoice = "Ingots";
                Ref_inv = Refblock.OutputInventory;
            }
            maxvol = set_ref((float)Ref_inv.MaxVolume);
            sumRefinery[0]++;
            sumAll[0]++;
            percent = 100 / maxvol * (float)Ref_inv.CurrentVolume;
            sumRefinery[1] += percent;
            sumAll[1] += percent;
            sumRefinery[2] += (float)Ref_inv.CurrentVolume;
            icon = iconRefinery;
            pattern = patternRefinery;
            invertColor = true;
            break;
            case "MyAssembler":
            bsize = get_size(subtype,'R');
            IMyInventory inventory = null;
            var Assblock = block as IMyAssembler;
            if (!opt_chc[0]) {
                optionchoice = "Ingots";
                inventory = Assblock.InputInventory;
                List<MyProductionItem> queue = new List<MyProductionItem>();
                Assblock.GetQueue(queue);
                foreach (var item in queue) { sumAssembler[2] += ((float)item.Amount);}
            } else {
                optionchoice = "Items";
                inventory = Assblock.OutputInventory;
                sumAssembler[2] += Assblock.CurrentProgress;
            }
            sumAssembler[0]++;
            sumAll[0]++;
            maxvol = set_ref((float)inventory.MaxVolume);
            percent = clamp(100 * (float)inventory.CurrentVolume / maxvol);
            sumAssembler[1] += percent;
            sumAll[1] += percent;
            icon = iconAssembler;
            pattern = patternAssembler;
            invertColor = true;
            break;
            case "MyLargeMissileTurret":
            bsize = get_size(subtype,'T');
            CargoVol2Arr(block, sumMissileTurret, ref percent, true);
            icon = iconRocket;
            optionchoice = "Ammo";
            pattern = patternTurret;
            break;
            case "MyLargeGatlingTurret":
            case "MyLargeInteriorTurret":
            bsize = get_size(subtype+blocktype,'W');
            CargoVol2Arr(block, sumGatlingTurret, ref percent, true);
            icon = iconBullet;
            optionchoice = "Ammo";
            pattern = patternTurret;
            break;
            case "MySmallGatlingGun":
            bsize = 4;
            CargoVol2Arr(block, sumGatling, ref percent, true);
            icon = iconBullet;
            optionchoice = "Ammo";
            pattern = patternTurret;
            break;
            case "MySmallMissileLauncher":
            case "MySmallMissileLauncherReload":
            bsize = get_size(subtype+blocktype,'W');
            CargoVol2Arr(block, sumMissileLauncher, ref percent, true);
            icon = iconRocket;
            optionchoice = "Ammo";
            pattern = patternTurret;
            break;
            case "MySpaceProjector":
            var projector = block as IMyProjector;
            var total = projector.TotalBlocks;
            sumProjector[0]++;
            sumAll[0]++;
            if (total != 0) { percent = 100 * (total - projector.RemainingBlocks) / total;}
            sumProjector[1] += percent;
            sumAll[1] += percent;
            sumProjector[2] += projector.RemainingBlocks;
            icon = iconProjector;
            optionchoice = "Done";
            pattern = patternProjector;
            break;
            case "MyAirVent":
            bsize = get_size(subtype,'T');
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
            isValidBlock = CargoVol2Arr(block, sumPara, ref percent);
            icon = iconPara;
            optionchoice = "Fill";
            pattern = patternCargo;
            break;
            case "MyConveyorSorter":
            bsize = get_size(subtype,'C');
            isValidBlock = CargoVol2Arr(block, sumSorter, ref percent);
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
            if (!opt_chc[0]) {
                optionchoice = "closed";
                percent=100-percent;
                invertColor = false;
            }
            sumDoor[1] += percent;
            sumAll[1] += percent;
            icon = iconDoor;
            pattern = patternJumpdrive;
            break;
            case "MyMotorAdvancedStator":
            case "MyMotorStator":
            bsize = get_size(subtype,'C');
            var rotor = block as IMyMotorStator;
            optionchoice = "left";
            float max_ang=rotor.UpperLimitRad;
            float min_ang=rotor.LowerLimitRad;
            float angle=rotor.Angle;
            if (max_ang>rad || min_ang<-rad) {
                percent = 100 * ((angle+rad)%rad)/ rad;
            } else {
                percent = 100 * (angle - min_ang) / (max_ang - min_ang);
            }
            percent = clamp(percent);
            if (opt_chc[0]) {
                optionchoice = "right";
                percent=100-percent;
            }
            sumAll[0]++;
            sumAll[1] += percent;
            if (subtype.Contains("Hinge")) {
                sumHinge[0]++;
                sumHinge[1] += percent;
                icon = iconHinge;
            } else {
                sumRotor[0]++;
                sumRotor[1] += percent;
                icon = iconRotor;
            }
            pattern = patternTool;
            invertColor = true;
            break;
            case "MyExtendedPistonBase":
            bsize = get_size(subtype,'C');
            var piston = block as IMyPistonBase;
            sumPiston[0]++;
            sumAll[0]++;
            percent = 100 * (piston.CurrentPosition - piston.MinLimit) / (piston.MaxLimit - piston.MinLimit);
            sumPiston[1] += percent;
            sumAll[1] += percent;
            icon = iconPiston;
            pattern = patternTool;
            invertColor = true;
            break;
            case "":
            break;
            default:
            if(opt_chc[3]) {
                sumBlock++;
                pattern = patternNone;
                drawpercent = "-----";
                icon = iconBlock;
            }
            else {isValidBlock = false;}
            break;
        }
        if (isValidBlock) {
            if(opt_chc[3]) {integrity = GetMyTerminalBlockHealth(block.CubeGrid.GetCubeBlock(block.Position));} else {integrity = 0;}
            if(!paints[frm]) {
            if (block == Me) {Colors[frm] = frameColorIsNotThere; integrity = 0;}
            else if (block.IsWorking) { Colors[frm] = frameColorIsWorking;}
            else if (block.IsFunctional) { Colors[frm] = frameColorIsFunctional;}
            else { Colors[frm] = frameColorIsUnfunctional;}
            }
        } else {
            blocktype = "";
            subtype = "";
            bsize = 0;
            optionchoice = "";
        }
        return isValidBlock;
        }
        
        float GetMyTerminalBlockHealth(IMySlimBlock slimblock) {
        return (slimblock.BuildIntegrity - slimblock.CurrentDamage) / slimblock.MaxIntegrity;
        }
        
        void PowerOutput2Arr(IMyTerminalBlock block, float[] arr, ref float percent) {
        var powProd = block as IMyPowerProducer;
        arr[0]++;
        sumAll[0]++;
        float power = powProd.CurrentOutput;
        float maxpwr = set_ref(powProd.MaxOutput,0.001f);
        percent = clamp(100 * power / maxpwr);
        arr[1] += percent;
        sumAll[1] += percent;
        arr[2] += powProd.CurrentOutput;
        }
        
        bool CargoVol2Arr(IMyTerminalBlock block, float[] arr, ref float percent, bool item = false) {
        bool result = block.HasInventory;
        if (result) {
            arr[0]++;
            sumAll[0]++;
            float volume = (float)block.GetInventory().CurrentVolume;
            float maxvol = set_ref((float)block.GetInventory().MaxVolume);
            percent = clamp(100 * volume / maxvol);
            arr[1] += percent;
            sumAll[1] += percent;
            if (item) {
            arr[2] += itemCount(block);
            } else {
            arr[2] += volume;
            }
        } else {
            percent = 0;
        }
        return result;
        }
        
        void TankVolume2Arr(IMyTerminalBlock block, float[] arr, ref float percent) {
        var tank = block as IMyGasTank;
        if (tank != null) {
            arr[0]++;
            sumAll[0]++;
            float cur_cap = tank.Capacity * (float)tank.FilledRatio;
            if (reference > 0) {
            percent = 100 * cur_cap / reference;
            } else {
            percent = 100 * (float)tank.FilledRatio;
            }
            arr[1] += percent;
            sumAll[1] += percent;
            arr[2] += cur_cap;
        }
        }
        
        float TestForNaN(float number) {
        if (double.IsNaN(number)) { number = 0;}
        return number;
        }
        
        void ClearGroupArrays() {
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
        ClearArray(sumHinge);
        ClearArray(sumPiston);
        ClearArray(sumRotor);
        ClearArray(sumAll);
        sumBlock=0;
        }
        
        void ClearArray(Array arr) {
        Array.Clear(arr, 0, arr.Length);
        }
        
        float itemCount(IMyTerminalBlock block) {
        List<MyInventoryItem> items = new List<MyInventoryItem>();
        block.GetInventory().GetItems(items);
        float count = 0;
        foreach (var item in items) { count += item.Amount.RawValue / 1000000;}
        return count;
        }
        
        void GetChargeStateValues(float percent) {
        percent = TestForNaN(percent);
        int counter = drawcounter(percent)+5;
        chargeBarOffset = nulvec;
        fillLevelLoopcount = counter/10;
        float v1=(set_length-20.28f)/10;
        float v2=v1-3;
        float v3=set_length-11.64f-(v2/2);
        float v4=(set_length-21.04f)/10;
        float v5=v4-8;
        float v6=set_length-14.52f-(v5/2);
        switch (containertype) {
            case WideBar:
            chargebarpos = pos + new Vector2(14, 37);
            chargeBarOffset.X = v4;
            chargeBarSize = new Vector2(v5, 43);
            break;
            case SmallBar:
            chargebarpos = pos + new Vector2(12, 27);
            chargeBarOffset.X = v1;
            chargeBarSize = new Vector2(v2, 29);
            break;
            case TallBar:
            chargebarpos = pos + new Vector2(14.5f, v6);
            chargeBarOffset.Y = -v4;
            chargeBarSize = new Vector2(43, v5);
            iconpos = pos + new Vector2(36, 36);
            optionpos = pos + new Vector2(36, 65);
            numberpos = pos + new Vector2(36, 80);
            break;
            case ShortBar:
            chargebarpos = pos + new Vector2(12,v3);
            chargeBarOffset.Y = -v1;
            chargeBarSize = new Vector2(37,v2);
            iconpos = pos + new Vector2(30.5f, 32);
            optionpos = pos + new Vector2(30.5f, 53);
            numberpos = pos + new Vector2(30.5f, 61);
            break;
            case SingleIcon:
            case MultiIcon:
            chargebarpos = pos + new Vector2(10.8f, 75)*scale;
            chargeBarOffset.Y = -13.6f*scale;
            chargeBarSize = new Vector2(44, 9)*scale;
            iconpos = pos + new Vector2(33, 36)*scale;
            optionpos = pos + new Vector2(33, 54.5f)*scale;
            numberpos = pos + new Vector2(33, 62)*scale;
            fillLevelLoopcount = (counter+5)/20;
            break;
            default:
            fillLevelLoopcount = 0;
            break;
        }
        if (pattern != patternNone) {drawpercent = String.Format("{0:N0}%",percent);}
        if (!paints[bar]) {
            if (invertColor) {
            Colors[bar] = cvar(percent/100);
            backg = 0.7f - (Math.Abs(40-percent)*0.03f);
            } else {
            Colors[bar] = cvar(1-(percent/100));
            backg = 0.7f - (Math.Abs(60-percent)*0.03f);
            }
        } else {
            backg = 0;
        }
        }
        
        int drawcounter (float percent) {
        return (int)clamp(percent);
        }
        
        float clamp(float wert,float max=100, float min=0) {
        return Math.Max(Math.Min(TestForNaN(wert),max),min);
        }
        
        static string getBetween(string strSource, string strStart, string strEnd) {
        int tag = strStart.Length;
        int Start = strSource.IndexOf(strStart);
        int End = strSource.IndexOf(strEnd, Start + tag);
        if (Start>=0 && End>=0) {
            Start+=tag;
            return strSource.Substring(Start, End - Start);
        } else {
            return "";
        }
        }
        
        string getval(string strSource, string strStart, char[] chars) {
        int tag = strStart.Length;
        int Start = strSource.IndexOf(TL(strStart));
        int End = strSource.IndexOfAny(chars, Start + tag);
        if (Start>=0 && End>=0) {
            Start+=tag;
            return strSource.Substring(Start, End - Start);
        } else {
            return "";
        }
        }
        
        float floatparse(string line, string tag, float min, float result) {
        if (have(line,tag)&&(float.TryParse(getval(line+" ", tag, new char[] {' ',',',':'}), out result))) {
            result=Math.Max(result,min);
        }
        return result;
        }
        
        int intparse(string line, string tag, int min, int result) {
        if (have(line,tag)&&(int.TryParse(getval(line+" ", tag, new char[] {' ',',',':'}), out result))) {
            result=Math.Max(result,min);
        }
        return result;
        }
        
        bool have(string a,string b) {return a.Contains(TL(b));}
        
        float set_ref(float normal, float factor=1) {
        if (reference > 0) {normal = reference * factor / 1000;}
        return normal;
        }
        
        int get_size(string type,char methode) {
        int res=0;
        switch(methode) {
            case('A'): {
            switch(type.Substring(0,11)) {
                case ("SmallBlockS"): {res=1;} break;
                case ("SmallBlockM"): {res=2;} break;
                case ("SmallBlockL"): {res=3;} break;
                case ("LargeBlockS"): {res=5;} break;
                default: {res=7;} break;
            }
            } break;
            case('B'): {
            if (type.StartsWith("Large")) {res=7;}
            else if (type.Contains("BlockBattery")) {res=2;}
            else {res=1;}
            } break;
            case('C'): {
            if (type.Contains("Small")) {res=1;}
            else if (type.Contains("Medium")) {res=2;}
            else {res=7;}
            } break;
            case('H'): {
            if (type.StartsWith("Large")) {res=7;} else {res=3;}
            if (type.EndsWith("Small")) {res-=2;}
            } break;
            case('R'): {
            if (type.StartsWith("Large")) {res=7;} else {res=5;}
            } break;
            case('T'): {
            if (type.Contains("Small")) {res=4;} else {res=8;}
            } break;
            case('W'): {
            switch(type.Substring(0,6)) {
                case ("MySmal"): {res=1;} break;
                case ("SmallR"): {res=2;} break;
                case ("SmallG"): {res=4;} break;
                case ("LargeI"): {res=5;} break;
                default: {res=7;} break;
            }
            } break;
        }
        return res;
        }
        
        void DrawObject(Vector2 pos, float integrity = 0) {
        if (!paints[frm] && containertype != MultiIcon) {Colors[frm] = frameColorWideBar;}
        switch (containertype) {
            case TallBar:
            AddFrameSprite(pos, new Vector2(72,set_length), true, Colors[frm]);
            break;
            case ShortBar:
            AddFrameSprite(pos, new Vector2(60.48f,set_length), false, Colors[frm]);
            break;
            case WideBar:
            AddFrameSprite(pos, new Vector2(set_length,72), true, Colors[frm]);
            break;
            case SmallBar:
            AddFrameSprite(pos, new Vector2(set_length,51.84f), false, Colors[frm]);
            break;
            default:
            for (int i = 0; i < pattern.Count();) {
                AddTextureSprite("SquareSimple", pos+(pattern[i++]*scale), pattern[i++]*scale, Colors[frm], C_align);
            }
            break;
        }
        if (opt_chc[3] && (containertype == MultiIcon)) {
            chargeBarSize = new Vector2(32, 9)*scale;
            var integritypos = pos + new Vector2(48, 79.5f - (integrity * 31.7f))*scale;
            var integritysize = new Vector2(9, 63.4f * integrity)*scale;
            AddTextureSprite("SquareSimple", integritypos, integritysize, integrityLevelColor, L_align);
            AddTextSprite(pos + new Vector2(52.5f, 12)*scale, "+\n+\n+\n+", "Monospace", 0.54f*scale, Color.Black, C_align);
            AddTextSprite(pos + new Vector2(52.5f, 20)*scale, "+\n+\n+\n+", "Monospace", 0.54f*scale, Color.Black, C_align);
        }
        for (int i = 0; i < fillLevelLoopcount; i++) {
            AddTextureSprite("SquareSimple", chargebarpos, chargeBarSize, Colors[bar], L_align);
            chargebarpos += chargeBarOffset;
        }
        if (horiz) {return;}
        if (icon.Contains("\n")) { AddTextSprite(iconpos + new Vector2(0, -20)*scale, icon, "Monospace", 0.1f*scale, Colors[ico], C_align); } else { AddTextureSprite(icon, iconpos, new Vector2(39, 35)*scale, Colors[ico], C_align); }
        if (opt_chc[1] && (optionchoice != "")) {AddTextSprite(optionpos, optionchoice, "Debug", 0.4f*scale, Colors[per], C_align,backg);}
        if (opt_chc[2] && (containertype == MultiIcon)) {
            AddTextSprite(pos + new Vector2(7,9)*scale,    " SMLSML█".Substring(bsize,1), "Monospace", 0.3f*scale, Colors[opt], L_align);
            AddTextSprite(pos + new Vector2(59, 9)*scale,  " ████".Substring(bsize,1), "Monospace", 0.3f*scale, Colors[opt], R_align);
            AddTextSprite(pos + new Vector2(7, 74)*scale,  " ████".Substring(bsize,1), "Monospace", 0.3f*scale, Colors[opt], L_align);
            AddTextSprite(pos + new Vector2(59, 74)*scale, " SMLSML█".Substring(bsize,1), "Monospace", 0.3f*scale, Colors[opt], R_align);
        }
        AddTextSprite(numberpos, drawpercent, "Debug", 0.7f*scale, Colors[per], C_align, backg);
        }
        
        void AddFrameSprite(Vector2 pos, Vector2 size, bool fat, Color color) {
        if (fat) {
            AddBoxSprite(pos, size,11.52f,col_cal(color,4));
            pos.X+=2.88f; pos.Y+=2.88f;
            size.X-=5.76f;size.Y-=5.76f;
            AddBoxSprite(pos, size,2.88f,col_cal(color,2));
        } else {
            AddBoxSprite(pos, size,8.64f,col_cal(color,4));
        }
        pos.X+=2.88f; pos.Y+=2.88f;
        size.X-=5.76f;size.Y-=5.76f;
        AddBoxSprite(pos, size,2.88f,color);
        }
        
        void AddBoxSprite(Vector2 pos, Vector2 size, float c, Color color) {
        float x=pos.X, y=pos.Y, a=size.X, b=size.Y, d=x+(a/2), e=c/2, f=y+(b/2);
        AddTextureSprite("SquareSimple", new Vector2(d,y+e), new Vector2(a,c), color, C_align);
        AddTextureSprite("SquareSimple", new Vector2(x+e,f), new Vector2(c,b), color, C_align);
        AddTextureSprite("SquareSimple", new Vector2(x+a-e,f), new Vector2(c,b), color, C_align);
        AddTextureSprite("SquareSimple", new Vector2(d,y+b-e), new Vector2(a,c), color, C_align);
        }
        
        void AddTextureSprite(string picture, Vector2 pos, Vector2 size, Color color, TextAlignment alignment) {
        var sprite = new MySprite() {
            Type = SpriteType.TEXTURE,
            Data = picture,
            Position = pos,
            Size = size,
            Color = color,
            Alignment = alignment
        };
        frame.Add(sprite);
        }
        
        void AddTextSprite(Vector2 pos, string str, string font, float size, Color color, TextAlignment alignment, float bg = 0) {
        if (bg>0) {
            var t_vec=surface.MeasureStringInPixels(new StringBuilder(str), font, size)*new Vector2(1,0.7f);
            AddTextureSprite("SquareSimple", pos+new Vector2(0,t_vec.Y*.8f), t_vec, Color.Black.Alpha(bg), alignment);
        }
        var sprite = new MySprite() {
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
        
        void PrepareSurface(IMyTerminalBlock block, int i = 0) {
        var provider = block as IMyTextSurfaceProvider;
        surface = provider.GetSurface(i);
        pos = surface.SurfaceSize;
        viewport = new RectangleF((surface.TextureSize - pos) / 2, pos);
        float x=0,y=0,w=0,h=0;
        switch (block.BlockDefinition.SubtypeName+i) {
            case "AtmBlock0": x=8; y=0; w=16; h=2; break;
            case "MedicalStation0": x=21; y=5; w=41; h=10; break;
            case "LabEquipment0": x=12; y=20; w=23; h=29; break;
            case "StoreBlock1": x=9; y=2; w=13; h=3; break;
            default: break;
        }
        viewport.X+=x;
        viewport.Y+=y;
        viewport.Width-=w;
        viewport.Height-=h;
        surface.ContentType = ContentType.SCRIPT;
        surface.Script = "";
        frame = surface.DrawFrame();
        AddTextureSprite("SquareSimple", viewport.Center, viewport.Size*1.2f, Color.Black, C_align);
        }
        
        void GeneratePatterns() {
        string[] LineDecoded = decodeLine(13, "AaaAcmAgeAiqAkmAmyAtcAuaBxoAwmBaeBemBgyBncBnoBsmAyuBtsChoCnsCumBxcDaqFswGbpGebGkfJbdIjhGsfGurGybHgbHmfHorHpdGqnGlhJcfLjzLulMap");
        iconIce = decodedPattern(LineDecoded,"nYZaBJWJBaZYn");
        iconWindmill = decodedPattern(LineDecoded,"nYYeBNBNBeYYn");
        iconSolar = decodedPattern(LineDecoded,"nYZfFAUAFfZYn");
        iconGrinder = decodedPattern(LineDecoded,"nYegJMGWOjmYn");
        iconRefinery = decodedPattern(LineDecoded,"nYYeCCEHHeYYn");
        iconVent = decodedPattern(LineDecoded,"nYYiANANAiYYn");
        iconBlock = decodedPattern(LineDecoded,"nYbcIVPPQklYn");
        iconPara = decodedPattern(LineDecoded,"nYahTRJFDaaYn");
        iconSorter = decodedPattern(LineDecoded,"nYYiHLKJHiYYn");
        iconReactor = decodedPattern(LineDecoded,"HSXXpoBADGGNG");
        iconEngine = decodedPattern(LineDecoded,"nYeYGGBBDdaYn");
        
        LineDecoded = decodeLine(14, "AaaAhkAlcAsmAuiBbsAbwBfkBmuDbcBnsBxoAfgBoyByuCgeLimAuqCiaAkuBtgBncDamMddMrxMknMofMgvMvpMxlNevNinNpxNqvMebOwfMhtPdpSenVnbWtjXel");
        iconThruster = decodedPattern(LineDecoded,"occcBDHESKhlXo");
        iconCollector = decodedPattern(LineDecoded,"oXZeDBASWhlXo");
        iconEjector = decodedPattern(LineDecoded,"oXlhSIABFcZXo");
        iconCargo = decodedPattern(LineDecoded,"oYbiWKPNNPhlXo");
        iconConnector = decodedPattern(LineDecoded,"oXlhSIAIShlXo");
        iconDrill = decodedPattern(LineDecoded,"oXZXDAIAWAmXXo");
        iconJumpdrive = decodedPattern(LineDecoded,"paaZAnOOnAZaap");
        iconShield = decodedPattern(LineDecoded,"oXlhPNPLPKfcXo");
        iconBullet = decodedPattern(LineDecoded,"oXXXAVNNVAXXXo");
        iconRocket = decodedPattern(LineDecoded,"oXXiGWJJWGiXXo");
        iconWelder = decodedPattern(LineDecoded,"oXadEEWKKhlXo");
        iconAssembler = decodedPattern(LineDecoded,"oXXaDBBBDaXXo");
        iconProjector = decodedPattern(LineDecoded,"oXXlAIADAIacXo");
        iconDoor = decodedPattern(LineDecoded,"oXlhKKNKKhlXo");
        iconCockpit = decodedPattern(LineDecoded,"oXXkMTRUWlXXo");
        iconHinge = decodedPattern(LineDecoded,"oXXgSQQQQSfaco");
        iconRotor = decodedPattern(LineDecoded,"oXXlIDWOOOjgXo");
        iconPiston = decodedPattern(LineDecoded,"oXgaCCIFFFegco");
        
        getpattern("ABPDPAXDADCdVDXdAdXf" , ref patternBattery);
        getpattern("ABEDJBOCTBXDECTDADCHVDXHBHCZVHWZANBTWNXTAZCdVZXdAdEfEdTeTdXfJeOf" , ref patternCargo);
        getpattern("BBWDACBJWCXJBDCdVDWdAWBeWWXeBdWf" , ref patternTank);
        getpattern("ABXDADBdWDXdBGCNVGWNBPCQVPWQBSCaVSWaAdXf" , ref patternConnector);
        getpattern("ABXDBDCVVDWVAFBHWFXHAJBLWJXLANBPWNXPARBTWRXTAVCdVVXdAdXf" , ref patternTool);
        getpattern("ABXCACBeGCRDWCXeBHCZVHWZGdReAeXf" , ref patternJumpdrive);
        getpattern("ABXDADBdWDXdBFCHVFWHBJCXVJWXBZCbVZWbAdXf" , ref patternGasgen);
        getpattern("BBWDACCGVCXGCDDEUDVEAHCJVHXJAKCMVKXMANCTVNXTAUCWVUXWAXCZVXXZAaCeVaXeCcDdUcVdBdWf" , ref patternShield);
        getpattern("ABXDADBdWDXdBHCZVHWZAdXf" , ref patternTurret);
        getpattern("BBWDACBGWCXGBDCSVDWSASCfVSXfDdFfGdIfJdLfMdOfPdRfSdUf" , ref patternThruster);
        getpattern("ABJDJBOCOBXDADCNVDXNANBSWNXSASCdVSXdAdJfOdXfJeOf" , ref patternReactor);
        getpattern("ABXDBDCdVDWdAdXf" , ref patternSolar);
        getpattern("ABXDADBHWDXHBGCLVGWLAKBPWKXPBOCTVOWTASBXWSXXBWCbVWWbAaBdWaXdAdXf" , ref patternWindmill);
        getpattern("CBVDBCDEUCWEADCdVDXdBcDeUcWeCdVf" , ref patternNone);
        getpattern("BBWDACBRWCXRBOCbVOWbATBVWTXVAXBZWXXZAbCfVbXfCdVf" , ref patternRefinery);
        getpattern("BBGDGBRCRBWDACBGWCXGBDCUVDWUAOBdWOXdBVCWVVWWBXCYVXWYBZCaVZWaBbCcVbWcAdXf" , ref patternAssembler);
        getpattern("BBKDNBWDACCOVCXOCDDEUDVEARCeVRXeCcDdUcVdBdKfNdWf" , ref patternProjector);
        getpattern("BBKDNBWDACBOKCNDWCXOBDCdVDWdARBeWRXeBdKfKdNeNdWf" , ref patternVent);
        }
        
        string[] decodeLine(int patternWidth, string pat) {
        string[] tempLine = new string[pat.Length / 3];
        int j = 0;
        for (int i = 0; i < pat.Length; i++) {
            int temp = ((int)pat[i++] - 65) * 676
                + ((int)pat[i++] - 97) * 26
                + ((int)pat[i] - 97);
            tempLine[j++] = number2line(patternWidth, temp);
        }
        return tempLine;
        }
        
        void getpattern(string pat, ref Vector2[] matr) {
        int a,b,c,d,j=0;
        matr = new Vector2[pat.Length/2];
        for (int i = 0; i < pat.Length;) {
            a=b64dec(pat[i++]);
            b=b64dec(pat[i++]);
            c=b64dec(pat[i++]);
            d=b64dec(pat[i++]);
            matr[j].X=1.44f*(a+c);
            matr[j++].Y=1.44f*(b+d);
            matr[j].X=2.88f*(c-a);
            matr[j++].Y=2.88f*(d-b);
        }
        }
        
        string number2line(int patternWidth, int temp) {
        string tempLine = "";
        for (int i = 0; i < patternWidth; i++) {
            if (temp % 2 != 0) { tempLine += "";}
            else { tempLine += "";}
            temp /= 2;
        }
        return tempLine;
        }
        
        string decodedPattern(string[] tempLine, string tempPattern) {
        StringBuilder tempstring = new StringBuilder();
        for (int i = 0; i < tempPattern.Length; i++) { tempstring.AppendLine(tempLine[b64dec(tempPattern[i])]);}
        return tempstring.ToString().Trim();
        }
        
        int b64dec(char chr) {
        return "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".IndexOf(chr);
        }
        
        bool getVector(string option, string tagstart, out Vector2 tempvector, out float gfx_res) {
        tempvector = nulvec;
        gfx_res = 0.1f;
        var xyz = getBetween(option, TL(tagstart) + "(", ")").Split(',').ToArray();
        if (xyz.Length == 2) {
            return float.TryParse(xyz[0], out tempvector.X) && float.TryParse(xyz[1], out tempvector.Y);
        }
        else if (xyz.Length == 3) {
            return float.TryParse(xyz[0], out tempvector.X) && float.TryParse(xyz[1], out tempvector.Y) && float.TryParse(xyz[2], out gfx_res);
        }
        else return false;
        }
        
        bool getcolors(string option, string tagstart, out Color tempColor) {
        bool valid = false;
        int red = 0, green = 0, blue = 0;
        tempColor = new Color(0, 0, 0);
        var xyz = getBetween(TL(option), TL(tagstart) + "(", ")").Split(',').ToArray();
        if (xyz.Length == 3) {
            valid = int.TryParse(xyz[0], out red) && int.TryParse(xyz[1], out green) && int.TryParse(xyz[2], out blue);
        }
        if (valid) {tempColor = new Color(red, green, blue);}
        return valid;
        }
        
        Color cvar (float percent) {
        float factor = Math.Max(1-percent,percent);
        return new Color(percent/factor, (1 - percent)/factor, 0);
        }
        
        Color col_cal (Color in_col, int fact) {return new Color(in_col.R/fact,in_col.G/fact,in_col.B/fact);}
        
        void setdefault() {
        ClearArray(opt_chc);
        LayoutCount = 0;
        temp_txt = TL(getBetween((Me.CustomData + "\n"), def_tag, "\n")) + " ";
        nosubgridLCDs = have(temp_txt,nosubgridLCDtag);
        verbatimdefault = have(temp_txt,verbatimtag);
        opt_def = wordparse(opt_tag,opt_chc,temp_txt);
        nostatus = have(temp_txt,nostatustag) || Me.CustomData.Contains(lcdtag);
        textposdefault = get_align(temp_txt,L_align);
        SsD = intparse(temp_txt, scrlspdtag, 0, SsD);
        FpM = intparse(temp_txt, FpM_tag, 0, FpM);
        LpM = intparse(temp_txt, LpM_tag, 0, LpM);
        if (have(temp_txt,fast_tag)) {
            TpM = 600;
            speed = UpdateFrequency.Update10;
        } else {
            TpM = 60;
            speed = UpdateFrequency.Update100;
        }
        if (temp_txt.Contains(sequ_tag+"(")) {
            lines = getBetween(temp_txt, sequ_tag+"(",")").Split(new[] {' ',',','.',':','/'}, StringSplitOptions.RemoveEmptyEntries);
            Layouts = new int[lines.Length];
            for (int i = 0; i < lines.Length;i++) {
            if(int.TryParse(lines[i], out Layouts[LayoutCount])) {LayoutCount++;};
            }
        }
        Runtime.UpdateFrequency = speed;
        oldCustomData = Me.CustomData;
        }
        
        bool[] wordparse(string[] words,bool[] inv,string text) {
        int len = words.Length;
        bool[] res = new bool[len];
        for(int i=0;i<len;i++) {
            res[i]=have(text,words[i]) ^ inv[i];
        }
        return res;
        }
        
        bool getsprite (string allvars, out MySprite sprite) {
        bool success = false;
        string[] sprvals = allvars.Split(new char[] {' ',',',':','/','(',')'}, StringSplitOptions.RemoveEmptyEntries);
        Vector2 tmppos = nulvec,tmpsize = nulvec;
        int tmpred=0,tmpgrn=0,tmpblu=0;
        float tmprot=0;
        string tmpname = "";
        if (sprvals.Length > 7 && sprvals.Length < 10) {
            success = true;
            tmpname = sprvals[0];
            success &= float.TryParse(sprvals[1], out tmppos.X);
            success &= float.TryParse(sprvals[2], out tmppos.Y);
            success &= float.TryParse(sprvals[3], out tmpsize.X);
            success &= float.TryParse(sprvals[4], out tmpsize.Y);
            success &= int.TryParse(sprvals[5], out tmpred);
            success &= int.TryParse(sprvals[6], out tmpgrn);
            success &= int.TryParse(sprvals[7], out tmpblu);
            if (sprvals.Length == 9) {success &= float.TryParse(sprvals[8], out tmprot);} else {tmprot=0;}
        }
        tmppos+= viewport.Position;
        sprite = new MySprite() {
            Type = SpriteType.TEXTURE,
            Data = tmpname,
            Position = tmppos,
            Size = tmpsize,
            Color = new Color(tmpred,tmpgrn,tmpblu),
            Alignment = C_align,
            RotationOrScale = tmprot
        };
        return success;
        }
        
        void layoutswitch(int change) {
        if (LayoutCount>1) {
            execCounter1 = 0;
            execCounter2 = 0;
            LayoutIndex += change + LayoutCount;
            LayoutIndex %= LayoutCount;
            Layout = Layouts[LayoutIndex];
        }
        }
        
        int NumParse(string argument, string trigger) {
        int output=0;
        argument=TL(argument);
        trigger=TL(trigger);
        int.TryParse(argument.Substring(argument.IndexOf(trigger) + trigger.Length).Trim(), out output);
        return Math.Max(output,0);
        }
        
        TextAlignment get_align(string text,TextAlignment align) {
        if (text.Contains(aligntag[0])) { align = L_align;}
        else if (text.Contains(aligntag[1])) { align = C_align;}
        else if (text.Contains(aligntag[2])) { align = R_align;}
        return align;
        }
        
        String TL(string tag) {
        return tag.ToLower();
        }
        
        public class EnergyShield {
        public static float CurrentHitpoints(IMyTerminalBlock block) {
            float val;
            float.TryParse(getBetween(block.CustomName, " (", "/"), out val);
            return val;
        }
        public static float MaxHitpoints(IMyTerminalBlock block) {
            float val;
            float.TryParse(getBetween(block.CustomName, "/", ")"), out val);
            return val;
        }
        public static string RequiredInput(IMyTerminalBlock block) {
            return getBetween(block.DetailedInfo, "\nRequired Input: ", "\n");
        }
        }
        
        int highload = 10000; // defining reserved command cycles
        int execCounter1 = 0,execCounter2 = 0,bsize,rotatecount = 0,containertype,lastDraw,Layout = 0,LayoutCount = 0,LayoutIndex = 0,TpM = 60,screenIndex,iconcount,iconcounter,headlineIndex,fillLevelLoopcount,sumBlock, scrl_spd = 10, startdisp=0 ,dispnr=0, scrollindex, cs, ce ;
        int[] Layouts;
        int[,] scrollpositions = new int[100, 20];
        string[] sArgs,lines,sections;
        string argument, icon, drawpercent, optionchoice, Displayname, oldCustomData = "", eShieldInput = "", added_txt = "", temp_txt, iconAssembler, iconBlock, iconBullet, iconCargo, iconCollector, iconConnector, iconCockpit, iconDoor, iconDrill, iconEjector, iconEngine, iconGrinder, iconHinge, iconIce, iconJumpdrive, iconPara, iconPiston, iconProjector, iconReactor, iconRefinery, iconRocket, iconRotor, iconShield, iconSolar, iconSorter, iconThruster, iconVent, iconWelder, iconWindmill, StartTag, StartSep, StopSep;
        StringBuilder gfxSprite = new StringBuilder(), headline = new StringBuilder();
        bool[] opt_def = new bool[12], opt_chc = new bool[12], paints = new bool[5];
        bool get_sprite, invertColor, verbatim,  verbatimdefault, nosubgridLCDs, nostatus, verti, horiz, timeout;
        TextAlignment textpos = L_align, textposdefault = L_align;
        Color integrityLevelColor;
        Color[] Colors = new Color[6];
        Vector2[] pattern, patternBattery, patternCargo, patternTank, patternConnector, patternTool, patternJumpdrive, patternGasgen, patternShield, patternTurret, patternThruster, patternReactor, patternSolar, patternWindmill, patternNone, patternRefinery, patternAssembler, patternProjector, patternVent;
        float[] sumBat = new float[3], sumJdrives = new float[3], sumCargo = new float[3], sumCockpit = new float[3], sumPara = new float[3], sumSorter = new float[3], sumHydro = new float[3], sumOxy = new float[3], sumDoor = new float[2], sumVent = new float[2], sumCollectors = new float[3], sumConnectors = new float[3], sumEjectors = new float[3], sumDrills = new float[3], sumGrinder = new float[3], sumWelder = new float[3], sumGasgen = new float[3], sumShield = new float[3], sumGatling = new float[3], sumMissileTurret = new float[3], sumGatlingTurret = new float[3], sumMissileLauncher = new float[3], sumThrust = new float[3], sumReactor = new float[3], sumSolar = new float[3], sumWindmill = new float[3], sumO2Gen = new float[3], sumRefinery = new float[3], sumAssembler = new float[3], sumProjector = new float[3], sumEShield = new float[2], sumPiston = new float[2], sumHinge = new float[2], sumRotor = new float[2], sumAll = new float[2];
        float last_fsize = 0, fontsize = 0, rad=2*(float)Math.PI, backg, scale, lastscale = 1, lastLength, scrollrange =0, set_length, min_length, std_length, reference;
        HashSet<IMyCubeGrid> ignoredGrids = new HashSet<IMyCubeGrid>();
        List<IMyTerminalBlock> surfaceProviders = new List<IMyTerminalBlock>(), Group = new List<IMyTerminalBlock>(), subgroup = new List<IMyTerminalBlock>();
        IMyTextSurface surface;
        RectangleF viewport;
        UpdateFrequency speed = UpdateFrequency.Update100;
        MySpriteDrawFrame frame = new MySpriteDrawFrame();
        Vector2 chargeBarSize, chargeBarOffset, iconpos, optionpos, numberpos, LineStartpos, pos, tempvector, chargebarpos, fieldsize, fieldpos, nulvec = new Vector2(0,0), posHeadline;
        
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
        const int ShortBar = 3;
        const int TallBar = 4;
        const int SingleIcon = 5;
        const int MultiIcon = 6;
        const int Textline = 7;
