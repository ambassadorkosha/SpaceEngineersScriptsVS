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
namespace ScriptRenamer
{
    public sealed class Program : MyGridProgram
    {
        //------------START--------------

        // Block Renaming Script by Isy
        // =======================
        // Version: 1.6.2
        // Date: 2021-01-27

        // Guide: https://steamcommunity.com/sharedfiles/filedetails/?id=2066808115

        // =======================================================================================
        //                                                                            --- Configuration ---
        // =======================================================================================

        // Define the character that will be added between names and numbers when sorting or adding strings.
        // Default (whitespace): char spaceCharacter = ' ';
        const char spaceCharacter = ' ';

        // Add an additional space character after/before addfront/addback?
        bool extraSpace = true;

        // Use custom number length?
        // By default, the script will determine automatically, how long your numbers should be by getting the total
        // amount of blocks of your current filter. If you don't like that, enable this option and adjust your wanted length.
        // The length of a number is the amount of digits and will be filled with zeros, if needed.
        // Example: Automatic mode in a set of 1000 blocks would produce: 0001, 0010, 0100, 1000
        // Example: A custom length of 3 in the same set would produce: 001, 010, 100, 1000
        bool useCustomNumberLength = false;
        int customNumberLength = 0;


        // --- Modifier Keywords ---
        // =======================================================================================

        // Keyword for sorting after performing another operation
        const string sortKeyword = "!sort";

        // Keyword for test mode (changes will only be shown but not executed)
        const string testKeyword = "!test";

        // Keyword for help (can be added to any command to see the in-script help, example: 'rename,!help')
        const string helpKeyword = "!help";


        // --- LCD Panels ---
        // =======================================================================================

        // Keword for LCD output
        // Everything the script writes to the terminal, will also be written on the LCD containing the keyword.
        // The keyword will be transformed to my universal [IsyLCD] keyword, once the script has recognized it. That way,
        // it's also possible to show the contents on block LCDs. The screen can be changed in the custom data (see guide).
        string mainLCDKeyword = "[IBR-main]";

        // Default screen font, fontsize and padding, when a screen is first initialized. Fonts: "Debug" or "Monospace"
        string defaultFont = "Debug";
        float defaultFontSize = 0.6f;
        float defaultPadding = 2f;


        // =======================================================================================
        //                                                                      --- End of Configuration ---
        //                                                        Don't change anything beyond this point!
        // =======================================================================================


        bool Ĉ, ć, Ć, ą; string ĉ = @"( |" + spaceCharacter + @")\d+\b"; List<string> Ċ = new List<string>() { "", "", "", "" }; List<
                             IMyTerminalBlock> ę = new List<IMyTerminalBlock>(); List<string> Ě = new List<string>(); List<string> Ę = new List<string>(); List<IMyTerminalBlock>
                                         ė = new List<IMyTerminalBlock>(); List<IMyTerminalBlock> Ė = new List<IMyTerminalBlock>(); List<IMyCubeGrid> ĕ = new List<
                                                IMyCubeGrid>(); HashSet<String> Ĕ = new HashSet<String>(); List<string> ē = new List<string>(); int Ē = 0; int đ = 0; int Đ = 0; DateTime ď = DateTime.
                                                                  Now; int Ď = 0; Program() { č(); J(); Ħ(); }
        void č() { Ě = new List<string>() { "Isy's Block Renaming Script\n======================\n" }; }
        void Main(string Č)
        {
            Đ++; if (ė.Count > 0)
            {
                Į(); if (!ą) L("Creating sort list"); Ď += Runtime.CurrentInstructionCount; ė.Clear(); return;
            }
            if (ĕ.Count > 0)
            {
                İ(); L("Collecting unique names"); Ď += Runtime.CurrentInstructionCount; if (đ < ĕ.Count) { return; }
                else
                {
                    ĕ.Clear(); đ
= 0; return;
                }
            }
            if (Ė.Count > 0)
            {
                Ļ(); if (Ē < Ė.Count) { L("Renaming blocks: " + Ę.Count); Ď += Runtime.CurrentInstructionCount; return; }
                else { Runtime.UpdateFrequency = UpdateFrequency.None; Ė.Clear(); Ē = 0; Ħ(); return; }
            }
            č(); ď = DateTime.Now; Đ = 0; Ď = 0; Č = Č.Replace("*", "");
            List<string> ċ = Č.Split(',').ToList(); ċ = ċ.Concat(Ċ).ToList(); Ĉ = false; ć = false; Ć = false; ą = false; ę.Clear(); Ę.Clear(); if (Č.Contains
                                 (testKeyword)) { Ĉ = true; ċ.Remove(testKeyword); }
            if (Č.Contains(sortKeyword)) { ć = true; ċ.Remove(sortKeyword); }
            if (Č.Contains(
helpKeyword)) { ą = true; ċ.Remove(helpKeyword); }
            if (ċ[0] == "undo") Ć = true; if (ċ.Count == 0) { J(); Ħ(); return; }
            if (!Ĉ && !Ć && !ą && Č != String.Empty)
                Storage = ""; if (Č == String.Empty) { J(); }
            else if (ą) { J(ċ[0]); }
            else if (ċ[0] == "rename")
            {
                if (ċ[2] != "") { Ą(ċ[1], ċ[2], ċ[3], ċ[4]); }
                else
                {
                    x(ċ[0
], 2); J(ċ[0]);
                }
            }
            else if (ċ[0] == "replace") { if (ċ[2] != "") { ý(ċ[1], ċ[2], ċ[3], ċ[4]); } else { x(ċ[0], 2); J(ċ[0]); } }
            else if (ċ[0] ==
"remove") { if (ċ[1] != "") { Ā(ċ[1], ċ[2], ċ[3]); } else { x(ċ[0], 1); J(ċ[0]); } }
            else if (ċ[0] == "removenumbers") { Ă(ċ[1], ċ[2]); }
            else if (ċ[0] ==
"sort") { if (ċ[1] != "") { ā(ċ[1], ċ[2]); } else { x(ċ[0], 1); J(ċ[0]); } }
            else if (ċ[0] == "sortbygrid")
            {
                if (ċ[1] != "") { ě(ċ[1]); }
                else
                {
                    x(ċ[0], 1); J
("sort");
                }
            }
            else if (ċ[0] == "autosort")
            {
                ĳ(ċ[1]); if (ĕ.Count > 0)
                {
                    L("Getting list of grids"); Ď += Runtime.CurrentInstructionCount;
                    Runtime.UpdateFrequency = UpdateFrequency.Update1; return;
                }
            }
            else if (ċ[0] == "addfront" || ċ[0] == "addback")
            {
                if (ċ[1] != "")
                {
                    Ĺ(ċ[0], ċ[1], ċ[
2], ċ[3]);
                }
                else { x(ċ[0], 1); J(ċ[0]); }
            }
            else if (ċ[0] == "renamegrid") { if (ċ[1] != "") { Ķ(ċ[1], ċ[2]); } else { x(ċ[0], 1); J(ċ[0]); } }
            else
if (ċ[0] == "copydata") { if (ċ[2] != "") { Ĭ(ċ[1], ċ[2], ċ[3]); } else { x(ċ[0], 2); J(ċ[0]); } }
            else if (ċ[0] == "deletedata")
            {
                if (ċ[2] != "")
                {
                    ğ(ċ
[1], ċ[2]);
                }
                else { x(ċ[0], 1); J(ċ[0]); }
            }
            else if (Ć) { Ğ(); } else { Ę.Add("Error!\nUnknown Command!\n"); J(); }
            if (ć) { Ġ(ę); }
            if (ċ[0] ==
"renamegrid") { Ħ("grids"); }
            else if (ċ[0].Contains("data")) { Ħ("data"); } else { Ħ(); }
        }
        void Ą(string M, string z, string s = "", string r = "")
        {
            var
q = u(M, s, r); foreach (var n in q) { string þ = n.CustomName; string Ä = Å(þ); string ÿ = z + Ä; ę.Add(n); Á(þ, ÿ); ª(n, ÿ); }
        }
        void ý(string ü,
string û, string s = "", string r = "")
        {
            var q = u(ü, s, r); foreach (var n in q)
            {
                string M = n.CustomName; string z = M.Replace(ü, û); ę.Add(n); Á(
M, z); ª(n, z);
            }
        }
        void Ā(string ă, string s = "", string r = "")
        {
            var q = u(ă, s, r); foreach (var n in q)
            {
                string M = n.CustomName;
                StringBuilder z = new StringBuilder(M); z.Replace(ă + " ", "").Replace(" " + ă, "").Replace(ă + spaceCharacter, "").Replace(spaceCharacter + ă, "");
                ę.Add(n); Á(M, z.ToString()); ª(n, z.ToString());
            }
        }
        void Ă(string s = "", string r = "")
        {
            var q = u("", s, r); foreach (var n in q)
            {
                string
M = n.CustomName; string z = Ã(M); ę.Add(n); Á(M, z); ª(n, z);
            }
        }
        void ā(string s, string r = "") { var q = u("", s, r); Ġ(q, true); }
        void ě(
string s)
        {
            var q = u("", s); HashSet<IMyCubeGrid> Ĵ = new HashSet<IMyCubeGrid>(); foreach (var n in q) { Ĵ.Add(n.CubeGrid); }
            foreach (var r
in Ĵ) { ā(s, r.CustomName); }
        }
        void ĳ(string Ĳ = "")
        {
            if (Ĳ != "")
            {
                HashSet<IMyCubeGrid> ı = new HashSet<IMyCubeGrid>(); List<
IMyTerminalBlock> q = new List<IMyTerminalBlock>(); GridTerminalSystem.GetBlocks(q); foreach (var n in q)
                {
                    if (Ĳ == "all") { ı.Add(n.CubeGrid); }
                    else
                    { if (n.CubeGrid.CustomName.Contains(Ĳ)) { ı.Add(n.CubeGrid); } }
                }
                ĕ = ı.ToList();
            }
            else { ĕ = new List<IMyCubeGrid>() { Me.CubeGrid }; }
        }
        void İ()
        {
            Ĕ.Clear(); ē.Clear(); GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(ė, C => C.CubeGrid == ĕ[đ]); đ++; foreach (var n
            in ė) { string į = Ã(n.CustomName); ē.Add(į); Ĕ.Add(į); }
        }
        void Į()
        {
            List<string> ĭ = Ĕ.OrderByDescending(t => t).ToList(); foreach (var t
in ĭ)
            {
                for (int h = ē.Count - 1; h >= 0; h--) { if (ē[h] == t) { Ė.Add(ė[h]); ė.RemoveAt(h); ē.RemoveAt(h); } }
                Ė.Add(null); if (Runtime.
CurrentInstructionCount > 45000)
                {
                    Ę.Add("Error!\nAutosort had to be stopped! You have too many different block names. Try another filter or simplify your blocknames first!"
); Runtime.UpdateFrequency = UpdateFrequency.None; ė.Clear(); Ė.Clear(); ĕ.Clear(); Ĕ.Clear(); ē.Clear(); Ē = 0; đ = 0; ą = true; Ħ();
                    return;
                }
            }
        }
        void Ļ()
        {
            List<IMyTerminalBlock> ĺ = new List<IMyTerminalBlock>(); for (int h = Ē; h < Ė.Count; h++)
            {
                Ē++; if (Ė[h] != null)
                {
                    ĺ.Add(Ė[
h]);
                }
                else { break; }
            }
            Ġ(ĺ, true);
        }
        void Ĺ(string ĸ, string ķ, string s = "", string r = "")
        {
            var q = u("", s, r); foreach (var n in q)
            {
                string
M = n.CustomName; string z = M; if (ĸ.Contains("addfront"))
                {
                    if (!M.StartsWith(ķ))
                    {
                        z = ķ + (extraSpace ? spaceCharacter.ToString() : "") + M
;
                    }
                }
                if (ĸ.Contains("addback")) { if (!M.EndsWith(ķ)) { z = M + (extraSpace ? spaceCharacter.ToString() : "") + ķ; } }
                ę.Add(n); Á(M, z); ª(n, z);
            }
        }
        void Ķ(string z, string ĵ = "")
        {
            if (ĵ == "") { Á(Me.CubeGrid.CustomName, z); if (!Ĉ) Me.CubeGrid.CustomName = z; }
            else
            {
                List<IMyTerminalBlock> q = new List<IMyTerminalBlock>();
                List<IMyCubeGrid> Ĵ = new List<IMyCubeGrid>();
                GridTerminalSystem.GetBlocks(q);
                foreach (var n in q)
                {
                    if (n.CustomName.Contains(ĵ) && !Ĵ.Contains(n.CubeGrid))
                    {
                        Ĵ.Add(n.CubeGrid);
                        Á(n.CubeGrid.CustomName, z);
                        if (!Ĉ) n.CubeGrid.CustomName = z;
                    }
                }
            }
        }
        void Ĭ(string Ĝ, string s, string r = "")
        {
            var º = u(Ĝ); if (º.Count == 0)
            {
                Ě.Add("Source block not found:\n'" + Ĝ + "'\n");
                return;
            }
            var q = u("", s, r); foreach (var n in q)
            {
                if (n == º) continue; À(º[0].CustomName, n.CustomName); if (!Ĉ) n.CustomData = º[0].
CustomData;
            }
        }
        void ğ(string s, string r = "") { var q = u("", s, r); foreach (var n in q) { À("", n.CustomName); if (!Ĉ) n.CustomData = ""; } }
        void Ğ()
        {
            var ĝ = Storage.Split('\n'); if (Storage.Length == 0) { Ě.Add("No saved operations to undo!\n"); }
            else
            {
                for (int h = ĝ.Length - 1; h >= 0; h--
)
                {
                    var Ĝ = ĝ[h].Split(';'); if (Ĝ.Length != 2) continue; long N; if (!long.TryParse(Ĝ[0], out N)) continue; IMyTerminalBlock n =
                               GridTerminalSystem.GetBlockWithId(N); if (n != null) { Á(n.CustomName, Ĝ[1]); if (!Ĉ) n.CustomName = Ĝ[1]; }
                }
            }
        }
        void Ġ(List<IMyTerminalBlock> ī, bool y =
false)
        {
            if (ī.Count == 0) { Ę.Add("Nothing to sort here.."); return; }
            int Ī = ī.Count.ToString().Length; if (useCustomNumberLength) Ī =
customNumberLength; ī.Sort((ĩ, C) => ĩ.CustomName.CompareTo(C.CustomName)); for (int h = 0; h < ī.Count; h++)
            {
                string M = ī[h].CustomName; string z = M;
                string Ĩ = ""; if (ī.Count > 1) { int ħ = (h + 1).ToString().Length; Ĩ = spaceCharacter + w('0', Ī - ħ) + (h + 1); }
                z = Ã(z) + Ĩ; Á(M, z); ª(ī[h], z, y);
            }
        }
        void
Ħ(string ĥ = "blocks")
        {
            List<string> Ĥ = new List<string>(Ě); Ĥ = Ĥ.Concat(Ę).ToList(); int ģ = Ę.Count; if (!ą)
            {
                if (ģ == 0) Ĥ.Add("No " + ĥ +
" found!"); if (Ĉ)
                {
                    if (ĥ == "data") { Ĥ.Add("\nTest Mode. No custom data was changed!"); }
                    else
                    {
                        Ĥ.Add("\nTest Mode. No " + ĥ +
" were renamed!");
                    }
                }
                else if (Ć) { Ĥ.Add("\nUndid renaming of " + ģ + " " + ĥ + "!"); }
                else if (ĥ == "data")
                {
                    Ĥ.Add("\nChanged the custom data of " + ģ +
" blocks!");
                }
                else { Ĥ.Add("\nRenamed " + (ć ? ģ / 2 : ģ) + " " + ĥ + "!"); }
            }
            Ĥ.Add("\nThis operation took " + (DateTime.Now - ď).TotalMilliseconds +
"ms and " + (Ď + Runtime.CurrentInstructionCount) + " instructions!"); if (Đ > 0) Ĥ.Add("The script was restartet " + Đ +
" times to split the load."); string V = String.Join("\n", Ĥ); var Ģ = H(mainLCDKeyword); for (int h = 0; h < Ģ.Count; h++)
            {
                var ġ = Ģ[h].É(mainLCDKeyword); foreach (
var ú in ġ)
                {
                    var W = ú.Key; var R = ú.Value; if (!W.GetText().EndsWith("\a"))
                    {
                        W.Font = defaultFont; W.FontSize = defaultFontSize; W.
TextPadding = defaultPadding; W.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT; W.ContentType = VRage.Game.GUI.TextPanel.
ContentType.TEXT_AND_IMAGE;
                    }
                    StringBuilder v = new StringBuilder(V); v = W.ò(v, ð: false); W.WriteText(v.Append("\a"));
                }
            }
            if (Ĥ.Count > 100)
            {
                for
(int h = 0; h < 50; h++) { Echo(Ĥ[h]); }
                Echo(".\n.\n."); for (int h = Ĥ.Count - 50; h < Ĥ.Count; h++) { Echo(Ĥ[h]); }
            }
            else { Echo(V); }
        }
        List<
IMyTerminalBlock> u(string t = "", string s = "", string r = "")
        {
            List<IMyTerminalBlock> q = new List<IMyTerminalBlock>(); if (s.StartsWith("G:"))
            {
                var
p = GridTerminalSystem.GetBlockGroupWithName(s.Substring(2)); if (p != null)
                {
                    p.GetBlocksOfType<IMyTerminalBlock>(q, C => C.
CustomName.Contains(t) && C.CubeGrid.CustomName.Contains(r)); Ě.Add("Filtered by group:\n" + s.Substring(2) + "\n");
                }
                else
                {
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(q, C => C.CustomName.Contains(t) && C.CubeGrid.CustomName.Contains(r)); Ě.Add(
                          "Not filtered - Group not found!\n");
                }
            }
            else if (s.StartsWith("T:"))
            {
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(q, C => C.BlockDefinition.ToString().
ToLower().Contains(s.Substring(2).ToLower()) && C.CustomName.Contains(t) && C.CubeGrid.CustomName.Contains(r)); if (q.Count != 0)
                {
                    HashSet<string> o = new HashSet<string>(); Ě.Add("Filtered by type:"); foreach (var n in q)
                    {
                        if (o.Contains(n.BlockDefinition.ToString(
))) continue; o.Add(n.BlockDefinition.ToString()); Ě.Add(n.BlockDefinition.TypeId.ToString() + "/\n" + n.BlockDefinition.
SubtypeId.ToString() + "\n");
                    }
                    Ě.Add("");
                }
                else
                {
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(q, C => C.CustomName.Contains(t) && C
.CubeGrid.CustomName.Contains(r)); Ě.Add("Not filtered - Type not found!\n");
                }
            }
            else
            {
                GridTerminalSystem.GetBlocksOfType<
IMyTerminalBlock>(q, C => C.CustomName.Contains(t) && C.CustomName.Contains(s) && C.CubeGrid.CustomName.Contains(r));
            }
            return q;
        }
        string w(char Æ
, int Ç)
        { if (Ç <= 0) { return ""; } return new string(Æ, Ç); }
        string Å(string Â)
        {
            string Ä = System.Text.RegularExpressions.Regex.Match
(Â, ĉ).Value; return Ä == String.Empty ? "" : Ä;
        }
        string Ã(string Â)
        {
            try
            {
                return System.Text.RegularExpressions.Regex.Replace(Â, ĉ,
"");
            }
            catch { return Â; }
        }
        void Á(string M, string z) { Ę.Add((Ę.Count + 1) + ". " + M + "\n   -> " + z); }
        void À(string º, string µ)
        {
            if (º == ""
) { Ę.Add((Ę.Count + 1) + ". Data deleted:\n   -> " + µ); }
            else { Ę.Add((Ę.Count + 1) + ". Data copied: " + º + "\n   -> " + µ); }
        }
        void ª(
IMyTerminalBlock n, string z, bool y = true)
        { if (!Ĉ) { if (y) j(n.EntityId, n.CustomName); n.CustomName = z; } }
        void x(string l, int P)
        {
            Ę.Add(
"Error!\n'" + l + "' needs at least " + P + " additional parameter" + (P > 1 ? "s" : "") + "!\n");
        }
        void j(long N, string M) { Storage += N + ";" + M + "\n"; }
        void L(string K) { Echo(Ě[0] + "\n" + K); }
        void J(string I = "")
        {
            bool O = false; if (I == "") { Ę.Add("Instructions:\n"); O = true; }
            else
            {
                Ę.Add(
"Usage:\n");
            }
            if (I == "" || I == "rename")
            {
                Ę.Add("--- Rename ---"); Ę.Add("Rename a block containing OLDNAME:"); Ę.Add(
"rename,OLDNAME,NEWNAME [,FILTER] [,GRID]\n"); O = true;
            }
            if (I == "" || I == "replace")
            {
                Ę.Add("--- Replace ---"); Ę.Add("Replace a string with another one:"); Ę.Add(
"replace,OLDSTRING,NEWSTRING [,FILTER] [,GRID]\n"); O = true;
            }
            if (I == "" || I == "remove")
            {
                Ę.Add("--- Remove ---"); Ę.Add("Remove a string:"); Ę.Add(
"remove,STRING [,FILTER] [,GRID]\n"); O = true;
            }
            if (I == "" || I == "removenumbers")
            {
                Ę.Add("--- Remove Numbers ---"); Ę.Add("Remove numbers from blocknames:"); Ę.Add(
"removenumbers [,FILTER] [,GRID]\n"); O = true;
            }
            if (I == "" || I == "sort")
            {
                Ę.Add("--- Sort ---"); Ę.Add("Create new continuous numbers:"); Ę.Add(
"sort,FILTER[,GRID]\n"); Ę.Add("Create new numbers based on the grid:"); Ę.Add("sortbygrid,FILTER\n"); O = true;
            }
            if (I == "" || I == "autosort")
            {
                Ę.Add(
"--- Autosort ---"); Ę.Add("Autosort all blocks on your grid with automatic numbers:"); Ę.Add("autosort\n"); Ę.Add(
"Autosort all blocks on a specific grid:"); Ę.Add("autosort,GRID\n"); Ę.Add("Autosort every connected grid:"); Ę.Add("autosort,all\n"); O = true;
            }
            if (I == "" || I ==
"addfront" || I == "addback")
            {
                Ę.Add("--- Add strings ---"); Ę.Add("Add a string at the front or back:"); Ę.Add(
"addfront,STRING [,FILTER] [,GRID]"); Ę.Add("addback,STRING [,FILTER] [,GRID]\n"); O = true;
            }
            if (I == "" || I == "renamegrid")
            {
                Ę.Add("--- Rename grid ---"); Ę.Add(
"Rename a grid:"); Ę.Add("renamegrid,NEWNAME [,BLOCKONGRID]\n"); O = true;
            }
            if (I == "" || I == "copydata")
            {
                Ę.Add("--- Copy Custom Data ---"); Ę.Add(
"Copy the custom data from BLOCK to all blocks matching FILTER:"); Ę.Add("copydata,BLOCK,FILTER [,GRID]\n"); O = true;
            }
            if (I == "" || I == "deletedata")
            {
                Ę.Add("--- Delete Custom Data ---"); Ę.Add(
"Delete the custom data of all blocks matching FILTER:"); Ę.Add("deletedata,FILTER [,GRID]\n"); O = true;
            }
            if (I == "" || I == "undo")
            {
                Ę.Add("--- Undo last operation ---"); Ę.Add(
"Mistakes are made. Undo your last operation with this:"); Ę.Add("undo\n"); O = true;
            }
            if (!O) { Ę.Add("--- Error ---"); Ę.Add("No topic with the given name exists!"); }
            if (O)
            {
                Ę.Add(
"To skip parameters, use asterisk *\n"); Ę.Add("FILTER:"); Ę.Add("Can be either a part of a blockname"); Ę.Add("Or a group with the group token 'G:'"); Ę.Add(
"Or a type with the type token 'T:'\n"); Ę.Add("GRID:"); Ę.Add("A gridname filtering blocks that should be used."); Ę.Add(
"Partial gridnames are also supported.\n"); Ę.Add("Additional parameters:"); Ę.Add("Test before renaming:\n" + testKeyword); Ę.Add("Sort after renaming:\n" +
sortKeyword);
            }
            ą = true;
        }
        List<IMyTerminalBlock> H(string G, string[] F = null)
        {
            string E = "[IsyLCD]"; var D = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(D, C => C.IsSameConstructAs(Me) && (C.CustomName.Contains(G) || (C.CustomName.Contains
             (E) && C.CustomData.Contains(G)))); var B = D.FindAll(C => C.CustomName.Contains(G)); foreach (var A in B)
            {
                A.CustomName = A.
CustomName.Replace(G, "").Replace(" " + G, "").TrimEnd(' '); bool Q = false; if (A is IMyTextSurface)
                {
                    if (!A.CustomName.Contains(E)) Q = true;
                    if (!A.CustomData.Contains(G)) A.CustomData = "@0 " + G + (F != null ? "\n" + String.Join("\n", F) : "");
                }
                else if (A is
IMyTextSurfaceProvider)
                {
                    if (!A.CustomName.Contains(E)) Q = true; int k = (A as IMyTextSurfaceProvider).SurfaceCount; for (int h = 0; h < k; h++)
                    {
                        if (!A.
CustomData.Contains("@" + h)) { A.CustomData += (A.CustomData == "" ? "" : "\n\n") + "@" + h + " " + G + (F != null ? "\n" + String.Join("\n", F) : ""); break; }
                    }
                }
                else { D.Remove(A); }
                if (Q) A.CustomName += " " + E;
            }
            return D;
        }
    }
    public static partial class g
    {
        private static Dictionary<char, float> f = new Dictionary<char, float>(); public static void e(
string d, float Z)
        { foreach (char S in d) { f[S] = Z; } }
        public static void Y()
        {
            if (f.Count > 0) return; e(
"3FKTabdeghknopqsuy£µÝàáâãäåèéêëðñòóôõöøùúûüýþÿāăąďđēĕėęěĝğġģĥħĶķńņňŉōŏőśŝşšŢŤŦũūŭůűųŶŷŸșȚЎЗКЛбдекруцяёђћўџ", 18); e("ABDNOQRSÀÁÂÃÄÅÐÑÒÓÔÕÖØĂĄĎĐŃŅŇŌŎŐŔŖŘŚŜŞŠȘЅЊЖф□", 22); e("#0245689CXZ¤¥ÇßĆĈĊČŹŻŽƒЁЌАБВДИЙПРСТУХЬ€", 20); e(
"￥$&GHPUVY§ÙÚÛÜÞĀĜĞĠĢĤĦŨŪŬŮŰŲОФЦЪЯжы†‡", 21); e("！ !I`ijl ¡¨¯´¸ÌÍÎÏìíîïĨĩĪīĮįİıĵĺļľłˆˇ˘˙˚˛˜˝ІЇії‹›∙", 9); e("？7?Jcz¢¿çćĉċčĴźżžЃЈЧавийнопсъьѓѕќ", 17); e(
"（）：《》，。、；【】(),.1:;[]ft{}·ţťŧț", 10); e("+<=>E^~¬±¶ÈÉÊË×÷ĒĔĖĘĚЄЏЕНЭ−", 19); e("L_vx«»ĹĻĽĿŁГгзлхчҐ–•", 16); e("\"-rª­ºŀŕŗř", 11); e("WÆŒŴ—…‰", 32); e("'|¦ˉ‘’‚", 7)
; e("@©®мшњ", 26); e("mw¼ŵЮщ", 28); e("/ĳтэє", 15); e("\\°“”„", 13); e("*²³¹", 12); e("¾æœЉ", 29); e("%ĲЫ", 25); e("MМШ", 27); e("½Щ", 30);
            e("ю", 24); e("ј", 8); e("љ", 23); e("ґ", 14); e("™", 31);
        }
        public static Vector2 X(this IMyTextSurface W, StringBuilder V)
        {
            Y();
            Vector2 U = new Vector2(); if (W.Font == "Monospace") { float T = W.FontSize; U.X = (float)(V.Length * 19.4 * T); U.Y = (float)(28.8 * T); return U; }
            else
            {
                float T = (float)(W.FontSize * 0.779); foreach (char S in V.ToString()) { try { U.X += f[S] * T; } catch { } }
                U.Y = (float)(28.8 * W.FontSize)
; return U;
            }
        }
        public static float m(this IMyTextSurface A, StringBuilder V) { Vector2 ã = A.X(V); return ã.X; }
        public static float
m(this IMyTextSurface A, string V)
        { Vector2 ã = A.X(new StringBuilder(V)); return ã.X; }
        public static float ê(this
IMyTextSurface A, char é)
        { float è = m(A, new string(é, 1)); return è; }
        public static int ç(this IMyTextSurface A)
        {
            Vector2 æ = A.SurfaceSize;
            float å = A.TextureSize.Y; æ.Y *= 512 / å; float ä = æ.Y * (100 - A.TextPadding * 2) / 100; Vector2 ã = A.X(new StringBuilder("T")); return (int)(ä /
                                  ã.Y);
        }
        public static float â(this IMyTextSurface A)
        {
            Vector2 æ = A.SurfaceSize; float å = A.TextureSize.Y; æ.X *= 512 / å; return æ.X *
(100 - A.TextPadding * 2) / 100;
        }
        public static StringBuilder ù(this IMyTextSurface A, char ø, double ö)
        {
            int õ = (int)(ö / ê(A, ø)); if (
õ < 0) õ = 0; return new StringBuilder().Append(ø, õ);
        }
        private static DateTime ô = DateTime.Now; private static Dictionary<int, List
<int>> ó = new Dictionary<int, List<int>>(); public static StringBuilder ò(this IMyTextSurface A, StringBuilder V, int ñ = 3, bool
ð = true, int ï = 0)
        {
            int î = A.GetHashCode(); if (!ó.ContainsKey(î)) { ó[î] = new List<int> { 1, 3, ñ, 0 }; }
            int í = ó[î][0]; int ì = ó[î][1]; int
ë = ó[î][2]; int á = ó[î][3]; var Ó = V.ToString().TrimEnd('\n').Split('\n'); List<string> ß = new List<string>(); if (ï == 0) ï = A.ç();
            float Ñ = A.â(); StringBuilder Ð, Ï = new StringBuilder(); for (int h = 0; h < Ó.Length; h++)
            {
                if (h < ñ || h < ë || ß.Count - ë > ï || A.m(Ó[h]) <= Ñ)
                {
                    ß.Add
(Ó[h]);
                }
                else
                {
                    try
                    {
                        Ï.Clear(); float Î, Í; var Ò = Ó[h].Split(' '); string Ä = System.Text.RegularExpressions.Regex.Match(Ó[h],
             @"\d+(\.|\:)\ ").Value; Ð = A.ù(' ', A.m(Ä)); foreach (var Ì in Ò)
                        {
                            Î = A.m(Ï); Í = A.m(Ì); if (Î + Í > Ñ)
                            {
                                ß.Add(Ï.ToString()); Ï = new StringBuilder(Ð + Ì +
" ");
                            }
                            else { Ï.Append(Ì + " "); }
                        }
                        ß.Add(Ï.ToString());
                    }
                    catch { ß.Add(Ó[h]); }
                }
            }
            if (ð)
            {
                if (ß.Count > ï)
                {
                    if (DateTime.Now.Second != á)
                    {
                        á =
DateTime.Now.Second; if (ì > 0) ì--; if (ì <= 0) ë += í; if (ë + ï - ñ >= ß.Count && ì <= 0) { í = -1; ì = 3; }
                        if (ë <= ñ && ì <= 0) { í = 1; ì = 3; }
                    }
                }
                else { ë = ñ; í = 1; ì = 3; }
                ó[î][
0] = í; ó[î][1] = ì; ó[î][2] = ë; ó[î][3] = á;
            }
            else { ë = ñ; }
            StringBuilder Ë = new StringBuilder(); for (var Ê = 0; Ê < ñ; Ê++)
            {
                Ë.Append(ß[Ê] + "\n"
);
            }
            for (var Ê = ë; Ê < ß.Count; Ê++) { Ë.Append(ß[Ê] + "\n"); }
            return Ë;
        }
        public static Dictionary<IMyTextSurface, string> É(this
IMyTerminalBlock n, string G, Dictionary<string, string> È = null)
        {
            var Ô = new Dictionary<IMyTextSurface, string>(); if (n is IMyTextSurface)
            {
                Ô[n
as IMyTextSurface] = n.CustomData;
            }
            else if (n is IMyTextSurfaceProvider)
            {
                var à = System.Text.RegularExpressions.Regex.Matches(n
.CustomData, @"@(\d) *(" + G + @")"); int Þ = (n as IMyTextSurfaceProvider).SurfaceCount; foreach (System.Text.RegularExpressions.
Match Ý in à)
                {
                    int Ü = -1; if (int.TryParse(Ý.Groups[1].Value, out Ü))
                    {
                        if (Ü >= Þ) continue; string Ø = n.CustomData; int Û = Ø.IndexOf("@" + Ü
); int Ú = Ø.IndexOf("@", Û + 1) - Û; string R = Ú <= 0 ? Ø.Substring(Û) : Ø.Substring(Û, Ú); Ô[(n as IMyTextSurfaceProvider).GetSurface(Ü)]
= R;
                    }
                }
            }
            return Ô;
        }
        public static bool Ù(this string R, string Õ)
        {
            var Ø = R.Replace(" ", "").Split('\n'); foreach (var Ê in Ø)
            {
                if (Ê
.StartsWith(Õ + "=")) { try { return Convert.ToBoolean(Ê.Replace(Õ + "=", "")); } catch { return true; } }
            }
            return true;
        }
        public static
string Ö(this string R, string Õ)
        {
            var Ø = R.Replace(" ", "").Split('\n'); foreach (var Ê in Ø)
            {
                if (Ê.StartsWith(Õ + "="))
                {
                    return Ê.
Replace(Õ + "=", "");
                }
            }
            return "";
        }
        //------------END--------------
    }
}