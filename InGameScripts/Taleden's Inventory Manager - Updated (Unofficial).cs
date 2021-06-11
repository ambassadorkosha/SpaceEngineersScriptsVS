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

namespace Taledens_Inventory_Manager
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        /*
            Taleden's Inventory Manager - Updated (Unofficial)
            version 1.7.8 (2021-04-07)

            Unoffical maintained version of TIM.

            Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=2301831956
            User's Guide:   http://steamcommunity.com/sharedfiles/filedetails/?id=546909551

            Source code:
            Since this script is minimised to reduce size and get round PB limits, you won't be able
            to edit this script directly. To view the source code, and possibly give contributions,
            please head to https://github.com/XReelZ/TaledensInvManagerUpdatedUnofficial

            *******************
            BASIC CONFIGURATION

            These are the main settings for TIM. They allow you to adjust how often the script will
            update, and the maximum load of each call before deferring execution to the next call.
            */
        // whether to use real time (second between calls) or pure UpdateFrequency
        // for update frequency
        readonly bool USE_REAL_TIME = false;

        // how often the script should update
        //     UpdateFrequency.None      - No automatic updating (manual only)
        //     UpdateFrequency.Once      - next tick (is unset after run)
        //     UpdateFrequency.Update1   - update every tick
        //     UpdateFrequency.Update10  - update every 10 ticks
        //     UpdateFrequency.Update100 - update every 100 ticks
        // (if USE_REAL_TIME == true, this is ignored)
        const UpdateFrequency UPDATE_FREQUENCY = UpdateFrequency.Update100;

        // How often the script should update in milliseconds
        // (if USE_REAL_TIME == false, this is ignored)
        const int UPDATE_REAL_TIME = 1000;

        // The maximum run time of the script per call.
        // Measured in milliseconds.
        const double MAX_RUN_TIME = 35;

        // The maximum percent load that this script will allow
        // regardless of how long it has been executing.
        const double MAX_LOAD = 0.8;

        /*
            ***********************
            ADVANCED CONFIGURATION

            The settings below may be changed if you like, but read the notes and remember
            that any changes will be reverted when you update the script from the workshop.
            */

        // Each "Type/" section can have multiple "/Subtype"s, which are formatted like
        // "/Subtype,MinQta,PctQta,Label,Blueprint". Label and Blueprint specified only
        // if different from Subtype, but Ingot and Ore have no Blueprint. Quota values
        // are based on material requirements for various blueprints (some built in to
        // the game, some from the community workshop).      
        //NATO_5p56x45mm,,,,NATO_5p56x45mmMagazine    - deprecated after patch 1.980
        const string DEFAULT_ITEMS = @"
                                    AmmoMagazine/
                                    /Missile200mm
                                    /NATO_25x184mm,,,,NATO_25x184mmMagazine  
                                    /AutomaticRifleGun_Mag_20rd,,,,
                                    /ElitePistolMagazine,,,,
                                    /FullAutoPistolMagazine,,,,
                                    /PreciseAutomaticRifleGun_Mag_5rd,,,,
                                    /RapidFireAutomaticRifleGun_Mag_50rd,,,,
                                    /SemiAutoPistolMagazine,,,,
                                    /UltimateAutomaticRifleGun_Mag_30rd,,,,

                                    Component/
                                    /BulletproofGlass,1000,2%
                                    /Computer,30,5%,,ComputerComponent
                                    /Construction,150,20%,,ConstructionComponent
                                    /Detector,10,0.1%,,DetectorComponent
                                    /Display,10,0.5%
                                    /Explosives,5,0.1%,,ExplosivesComponent
                                    /Girder,10,0.5%,,GirderComponent
                                    /GravityGenerator,1,0.1%,GravityGen,GravityGeneratorComponent
                                    /InteriorPlate,100,10%
                                    /LargeTube,10,2%
                                    /Medical,15,0.1%,,MedicalComponent
                                    /MetalGrid,20,2%
                                    /Motor,20,4%,,MotorComponent
                                    /PowerCell,20,1%
                                    /RadioCommunication,10,0.5%,RadioComm,RadioCommunicationComponent
                                    /Reactor,25,2%,,ReactorComponent
                                    /SmallTube,50,3%
                                    /SolarCell,20,0.1%
                                    /SteelPlate,150,40%
                                    /Superconductor,10,1%
                                    /Thrust,15,5%,,ThrustComponent
                                    /Canvas,5,0.01%
                                    /CompT02,10000,1%
                                    /CompT03,10000,1%
                                    /CompT04,10000,1%
                                    /CompT05,10000,1%
                                    /CompT06,10000,1%
                                    /CompT07,10000,1%
                                    /CompT08,10000,1%
                                    /CompT09,10000,1%
                                    /CompT10,10000,1%
                                    /CompT11,10000,1%
                                    /CompT12,10000,1%


                                    GasContainerObject/
                                    /HydrogenBottle

                                    Ingot/
                                    /Cobalt,50,3.5%
                                    /Gold,5,0.2%
                                    /Iron,200,88%
                                    /Magnesium,5,0.1%
                                    /Nickel,30,1.5%
                                    /Platinum,5,0.1%
                                    /Silicon,50,2%
                                    /Silver,20,1%
                                    /Stone,50,2.5%
                                    /Uranium,1,0.1%

                                    Ore/
                                    /Cobalt
                                    /Gold
                                    /Ice
                                    /Iron
                                    /Magnesium
                                    /Nickel
                                    /Platinum
                                    /Scrap
                                    /Silicon
                                    /Silver
                                    /Stone
                                    /Uranium

                                    OxygenContainerObject/
                                    /OxygenBottle

                                    PhysicalGunObject/
                                    /AngleGrinderItem,,,,AngleGrinder
                                    /AngleGrinder2Item,,,,AngleGrinder2
                                    /AngleGrinder3Item,,,,AngleGrinder3
                                    /AngleGrinder4Item,,,,AngleGrinder4
                                    /AutomaticRifleItem,,,AutomaticRifle,AutomaticRifle
                                    /HandDrillItem,,,,HandDrill
                                    /HandDrill2Item,,,,HandDrill2
                                    /HandDrill3Item,,,,HandDrill3
                                    /HandDrill4Item,,,,HandDrill4     
                                    /SemiAutoPistolItem,,,,SemiAutoPistol  
                                    /BasicHandHeldLauncherItem,,,,BasicHandHeldLauncher
                                    /ElitePistolItem,,,,EliteAutoPistol
                                    /FullAutoPistolItem,,,,FullAutoPistol
                                    /AdvancedHandHeldLauncherItem,,,AdvancedHandHeldLauncher,AdvancedHandHeldLauncher
                                    /PreciseAutomaticRifleItem,,,PreciseAutomaticRifle,PreciseAutomaticRifle
                                    /RapidFireAutomaticRifleItem,,,RapidFireAutomaticRifle,RapidFireAutomaticRifle
                                    /UltimateAutomaticRifleItem,,,UltimateAutomaticRifle,UltimateAutomaticRifle
                                    /WelderItem,,,,Welder
                                    /Welder2Item,,,,Welder2
                                    /Welder3Item,,,,Welder3
                                    /Welder4Item,,,,Welder4
                                    ";

        // Item types which may have quantities which are not whole numbers.
        static readonly HashSet<string> FRACTIONAL_TYPES = new HashSet<string> { "INGOT", "ORE" };

        // Ore subtypes which refine into Ingots with a different subtype name, or
        // which cannot be refined at all (if set to "").
        static readonly Dictionary<string, string> ORE_PRODUCT = new Dictionary<string, string>
{
// vanilla products
{ "ICE", "" }, { "ORGANIC", "" }, { "SCRAP", "IRON" },

// better stone products
// http://steamcommunity.com/sharedfiles/filedetails/?id=406244471
{"DENSE IRON", "IRON"}, {"ICY IRON", "IRON"}, {"HEAZLEWOODITE", "NICKEL"}, {"CATTIERITE", "COBALT"}, {"PYRITE", "GOLD"},
{"TAENITE", "NICKEL"}, {"COHENITE", "COBALT"}, {"KAMACITE", "NICKEL"}, {"GLAUCODOT", "COBALT"}, {"ELECTRUM", "GOLD"},
{"PORPHYRY", "GOLD"}, {"SPERRYLITE", "PLATINUM"}, {"NIGGLIITE", "PLATINUM"}, {"GALENA", "SILVER"}, {"CHLORARGYRITE", "SILVER"},
{"COOPERITE", "PLATINUM"}, {"PETZITE", "SILVER"}, {"HAPKEITE", "SILICON"}, {"DOLOMITE", "MAGNESIUM"}, {"SINOITE", "SILICON"},
{"OLIVINE", "MAGNESIUM"}, {"QUARTZ", "SILICON"}, {"AKIMOTOITE", "MAGNESIUM"}, {"WADSLEYITE", "MAGNESIUM"}, {"CARNOTITE", "URANIUM"},
{"AUTUNITE", "URANIUM"}, {"URANIAURITE", "GOLD"}
};

        // Block types/subtypes which restrict item types/subtypes from their first
        // inventory. Missing or "*" subtype indicates all subtypes of the given type.
        const string DEFAULT_RESTRICTIONS =
        MOB + "Assembler:AmmoMagazine,Component,GasContainerObject,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "InteriorTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_25x184mm," + NON_AMMO +
        MOB + "LargeGatlingTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "LargeMissileTurret:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "OxygenGenerator:AmmoMagazine,Component,Ingot,Ore/Cobalt,Ore/Gold,Ore/Iron,Ore/Magnesium,Ore/Nickel,Ore/Organic,Ore/Platinum,Ore/Scrap,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,PhysicalGunObject\n" +
        MOB + "OxygenTank:AmmoMagazine,Component,GasContainerObject,Ingot,Ore,PhysicalGunObject\n" +
        MOB + "OxygenTank/LargeHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "OxygenTank/SmallHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Reactor:AmmoMagazine,Component,GasContainerObject,Ingot/Cobalt,Ingot/Gold,Ingot/Iron,Ingot/Magnesium,Ingot/Nickel,Ingot/Platinum,Ingot/Scrap,Ingot/Silicon,Ingot/Silver,Ingot/Stone,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Refinery:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Ice,Ore/Organic,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Refinery/Blast Furnace:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Gold,Ore/Ice,Ore/Magnesium,Ore/Organic,Ore/Platinum,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "SmallGatlingGun:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "SmallMissileLauncher:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "SmallMissileLauncherReload:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "Parachute:Ingot,Ore,OxygenContainerObject,PhysicalGunObject,AmmoMagazine,GasContainerObject,Component/Construction,Component/MetalGrid,Component/InteriorPlate,Component/SteelPlate,Component/Girder,Component/SmallTube,Component/LargeTube,Component/Motor,Component/Display,Component/BulletproofGlass,Component/Superconductor,Component/Computer,Component/Reactor,Component/Thrust,Component/GravityGenerator,Component/Medical,Component/RadioCommunication,Component/Detector,Component/Explosives,Component/Scrap,Component/SolarCell,Component/PowerCell"
        ;

        // =================================================
        //                 SCRIPT INTERNALS
        //
        //            Do not edit anything below
        // =================================================
        const string MOB = "MyObjectBuilder_";
        const string NON_AMMO = "Component,GasContainerObject,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n";
        readonly string Y = string.Format("v{0}.{1}.{2} ({3})", 1, 7, 7, "2019-04-07");
        bool aa; 
        bool ab; 
        char ac; 
        char ad;
        string ae;
        bool af; 
        bool ag; 
        bool ah; 
        bool ai; 
        string aj;
        readonly System.Text.RegularExpressions.Regex al = new System.Text.RegularExpressions.Regex(@"^([^=\n]*)(?:=([^=\n]*))?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Compiled);
        readonly string[] am ={"quotas","sorting","refineries","assemblers"};
        const StringComparison an = StringComparison.OrdinalIgnoreCase;
        const StringSplitOptions ao = StringSplitOptions.RemoveEmptyEntries; 
        static readonly char[] ap = { ' ', '\t', '\u00AD' }, aq = { ':' }, ar = { '\r', '\n' }, at ={' ','\t','\u00AD', ','};
        static Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>> au = new Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>>(); 
        static List<string> av = new List<string>(); 
        static Dictionary<string, string> aw = new Dictionary<string, string>(); 
        static Dictionary<string, List<string>> ax = new Dictionary<string, List<string>>();
        static Dictionary<string, long> ay = new Dictionary<string, long>();
        static List<string> az = new List<string>(); 
        static Dictionary<string, string> aA = new Dictionary<string, string>(); 
        static Dictionary<string, List<string>> aB = new Dictionary<string, List<string>>();
        static Dictionary<string, Dictionary<string, bK>> aC = new Dictionary<string, Dictionary<string, bK>>();
        static Dictionary<MyDefinitionId, ak> aD = new Dictionary<MyDefinitionId, ak>();
        string aE = ""; 
        string[] aF = new string[12];
        DateTime aG;
        TimeSpan aH = new TimeSpan(0, 0, 0, 0, UPDATE_REAL_TIME); 
        long aI;
        int aJ; 
        int aK;
        int aL;
        int aM; 
        readonly Action[] aN;
        System.Text.RegularExpressions.Regex aO; 
        static bool aP; 
        string aQ;
        StringBuilder aR = new StringBuilder(); 
        HashSet<IMyCubeGrid> aS = new HashSet<IMyCubeGrid>();
        Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>> aT = new Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>>(); 
        Dictionary<IMyTextPanel, int> aU = new Dictionary<IMyTextPanel, int>(); 
        Dictionary<IMyTextPanel, List<string>> aV = new Dictionary<IMyTextPanel, List<string>>();
        Dictionary<IMyTextPanel, List<string>> aW = new Dictionary<IMyTextPanel, List<string>>(); 
        List<IMyTextPanel> aX = new List<IMyTextPanel>();
        List<IMyTextPanel> aY = new List<IMyTextPanel>();
        HashSet<string> aZ = new HashSet<string>();
        List<string> a_ = new List<string>();
        Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match> b0 = new Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match>();
        Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match> b1 = new Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match>(); 
        HashSet<IMyInventory> b2 = new HashSet<IMyInventory>(); 
        HashSet<IMyInventory> b3 = new HashSet<IMyInventory>();
        Dictionary<IMyRefinery, HashSet<string>> b4 = new Dictionary<IMyRefinery, HashSet<string>>();
        Dictionary<IMyAssembler, HashSet<ak>> b5 = new Dictionary<IMyAssembler, HashSet<ak>>();
        Dictionary<IMyFunctionalBlock, bJ> b6 = new Dictionary<IMyFunctionalBlock, bJ>();
        Dictionary<IMyFunctionalBlock, int> b7 = new Dictionary<IMyFunctionalBlock, int>(); 
        Dictionary<IMyTextPanel, a9> b8 = new Dictionary<IMyTextPanel, a9>();
        Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>> b9 = new Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>>(); 
        public Action<string> EchoR; 
        int ba 
        {
            get 
            {
                return (int)((DateTime.Now - aG).TotalMilliseconds + 0.5);
            }
        }
        double bb
        {
            get
            {
                return Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount;
            }
        }
        class a7 : Exception { }
        class a8 : Exception { }
        struct a9
        {
            public int A, B; 
            public a9(int b, int c)
            {
                A = b;
                B = c;
            }
        }
        struct ak 
        {
            public string type, subType;
            public ak(string b, string c) 
            {
                type = b; 
                subType = c; 
            }
        }
        struct bJ
        { 
            public ak item;
            public double quantity;
            public bJ(ak b, double c) 
            {
                item = b; 
                quantity = c;
            }
        }
        class bK
        {
            public string subType, label; 
            public MyDefinitionId blueprint; 
            public long amount, avail, locked, quota, minimum; 
            public float ratio; 
            public int qpriority, hold, jam;
            public Dictionary<IMyInventory, long> invenTotal;
            public Dictionary<IMyInventory, int> invenSlot; 
            public HashSet<IMyFunctionalBlock> producers;
            public Dictionary<string, double> prdSpeed; 
            public static void InitItem(string b, string c, long e = 0L, float f = 0.0f, string g = "", string h = "")
            {
                string j = b, k = c;
                b = b.ToUpper(); 
                c = c.ToUpper(); 
                if (!ax.ContainsKey(b))
                {
                    av.Add(b); 
                    aw[b] = j;
                    ax[b] = new List<string>();
                    ay[b] = 0L;
                    aC[b] = new Dictionary<string, bK>();
                }
                if (!aB.ContainsKey(c))
                {
                    az.Add(c); 
                    aA[c] = k;
                    aB[c] = new List<string>();
                }
                if (!aC[b].ContainsKey(c))
                {
                    aP = true;
                    ax[b].Add(c);
                    aB[c].Add(b);
                    aC[b][c] = new bK(c, e, f, g == "" ? k : g, h == "" ? k : h);
                    if (h != null) aD[aC[b][c].blueprint] = new ak(b, c);
                }
            }
            private bK(string b, long c, float e, string f, string g)
            {
                subType = b;
                this.label = f;
                this.blueprint = g == null ? default(MyDefinitionId) : MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + g);
                amount = avail = locked = quota = 0L; 
                this.minimum = (long)(c * 1000000.0 + 0.5);
                this.ratio = e / 100.0f;
                qpriority = -1;
                hold = jam = 0;
                invenTotal = new Dictionary<IMyInventory, long>();
                invenSlot = new Dictionary<IMyInventory, int>();
                producers = new HashSet<IMyFunctionalBlock>(); 
                prdSpeed = new Dictionary<string, double>();
            }
        }
        public Program()
        {
            EchoR = b => {
                aR.AppendLine(b);
                Echo(b);
            }; 
            aN = new Action[]{
                ProcessStepProcessArgs,
                ProcessStepScanGrids,
                ProcessStepStandbyCheck,
                ProcessStepInventoryScan,
                ProcessStepParseTags,
                ProcessStepAmountAdjustment,
                ProcessStepQuotaPanels,
                ProcessStepLimitedItemRequests,
                ProcessStepManageRefineries,
                ProcessStepUnlimitedItemRequests,
                ProcessStepManageAssemblers,
                ProcessStepScanProduction,
                ProcessStepUpdateInventoryPanels,
            }; 
            int c;
            ScreenFormatter.Init(); 
            aE = "Taleden's Inventory Manager\n" +
                "" + Y + "\n\n" + "" + 
                ScreenFormatter.Format("Run", 80, out c, 1) + 
                ScreenFormatter.Format("F-Step", 125 + c, out c, 1) + 
                ScreenFormatter.Format("Time", 145 + c, out c, 1) + 
                ScreenFormatter.Format("Load", 105 + c, out c, 1) + 
                ScreenFormatter.Format("S", 65 + c, out c, 1) + 
                ScreenFormatter.Format("R", 65 + c, out c, 1) + 
                ScreenFormatter.Format("A", 65 + c, out c, 1) + "\n\n";
            bc(DEFAULT_ITEMS);
            bd(DEFAULT_RESTRICTIONS); 
            if (USE_REAL_TIME) Runtime.UpdateFrequency = UpdateFrequency.Update1;
            else Runtime.UpdateFrequency = UPDATE_FREQUENCY;
            EchoR("Compiled TIM " + Y);
            aQ = string.Format("Taleden's Inventory Manager\n{0}\nLast run: #{{0}} at {{1}}", Y);
        }
        public void Main(string b)
        {
            if (USE_REAL_TIME)
            {
                DateTime c = DateTime.Now;
                if (c - aG >= aH) aG = c;
                else 
                {
                    Echo(aR.ToString());
                    return;
                }
            }
            else aG = DateTime.Now;
            aR.Clear(); 
            int e = aM;
            bool f = false;
            EchoR(string.Format(aQ, ++aI, aG.ToString("h:mm:ss tt"))); 
            a_.Clear(); 
            aZ.Clear();
            aJ = aK = aL = 0; 
            try
            {
                do
                {
                    a_.Add(string.Format("> Doing step {0}", aM));
                    aN[aM]();
                    aM++; f = true;
                } while (aM < aN.Length && bf()); 
                aM = 0;
            }
            catch (ArgumentException ex) { EchoR(ex.Message); aM = 0; return; }
            catch (a7) { aM = 0; return; }
            catch (a8) { }
            catch (Exception ex)
            {
                string g = "An error occured,\n" + 
                    "please give the following information to the developer:\n" + 
                    string.Format("Current step on error: {0}\n" +
                    "{1}", aM, ex.ToString().Replace("\r", "")); 
                a_.Add(g); 
                bH(); 
                EchoR(g);
                throw ex;
            }
            string h, j;
            int k = aM == 0 ? 13 : aM;
            int l = ba; 
            double m = Math.Round(100.0f * bb, 1);
            int n;
            aF[aI % aF.Length] = ScreenFormatter.Format("" + aI, 80, out n, 1) + 
                ScreenFormatter.Format((aM == 0 ? aN.Length : aM) + " / " + aN.Length, 125 + n, out n, 1, true) + 
                ScreenFormatter.Format(l + " ms", 145 + n, out n, 1) + 
                ScreenFormatter.Format(m + "%", 105 + n, out n, 1, true) + 
                ScreenFormatter.Format("" + aJ, 65 + n, out n, 1, true) + 
                ScreenFormatter.Format("" + aK, 65 + n, out n, 1, true) +
                ScreenFormatter.Format("" + aL, 65 + n, out n, 1, true) + "\n";
            if (aM == 0 && e == 0 && f) j = "all steps";
            else if (aM == e) j = string.Format("step {0} partially", aM);
            else if (k - e == 1) j = string.Format("step {0}", e);
            else j = string.Format("steps {0} to {1}", e, k - 1);
            EchoR(h = string.Format("Completed {0} in {1}ms, {2}% load ({3} instructions)", j, l, m, Runtime.CurrentInstructionCount));
            a_.Add(h);
            bH();
        }
        void bc(string b)
        {
            string c = ""; long e; float f; foreach (string line in b.Split(ar, ao))
            {
                string[] g = (line.Trim() + ",,,,").Split(at, 6); g[0] = g[0].Trim();
                if (g[0].EndsWith("/")) 
                {
                    c = g[0].Substring(0, g[0].Length - 1);
                }
                else if (c != "" & g[0].StartsWith("/"))
                {
                    long.TryParse(g[1], out e);
                    float.TryParse(g[2].Substring(0, (g[2] + "%").IndexOf("%")), out f); 
                    bK.InitItem(c, g[0].Substring(1), e, f, g[3].Trim(), c == "Ingot" | c == "Ore" ? null : g[4].Trim());
                }
            }
        }
        void bd(string b)
        {
            foreach (string line in b.Split(ar, ao))
            {
                string[] c = (line + ":").Split(':');
                string[] e = (c[0] + "/*").Split('/');
                foreach (string item in c[1].Split(','))
                {
                    string[] f = item.ToUpper().Split('/');
                    bg(e[0].Trim(ap), e[1].Trim(ap), f[0], f.Length > 1 ? f[1] : null, true);
                }
            }
        }
        void be()
        {
            aa = true;
            ac = '['; 
            ad = ']';
            ae = "TIM";
            af = false;
            ag = false; 
            ah = false; 
            ai = false; 
            ab = true; 
            string b, c;
            bool e; 
            foreach (System.Text.RegularExpressions.Match match in al.Matches(Me.CustomData))
            {
                b = match.Groups[1].Value.ToLower(); 
                e = match.Groups[2].Success;
                if (e) c = match.Groups[2].Value.Trim(); 
                else c = ""; 
                switch (b)
                {
                    case "rewrite":
                        if (e) throw new ArgumentException("Argument 'rewrite' does not have a value");
                        aa = true; 
                        a_.Add("Tag rewriting enabled"); 
                        break;
                    case "norewrite":
                        if (e) throw new ArgumentException("Argument 'norewrite' does not have a value");
                        aa = false;
                        a_.Add("Tag rewriting disabled");
                        break;
                    case "tags":
                        if (c.Length != 2) throw new ArgumentException(string.Format("Invalid 'tags=' delimiters '{0}': must be exactly two characters", c));
                        else if (char.ToUpper(c[0]) == char.ToUpper(c[1])) throw new ArgumentException(string.Format("Invalid 'tags=' delimiters '{0}': characters must be different", c));
                        else
                        {
                            ac = char.ToUpper(c[0]);
                            ad = char.ToUpper(c[1]);
                            a_.Add(string.Format("Tags are delimited by '{0}' and '{1}", ac, ad));
                        }
                        break;
                    case "prefix":
                        ae = c.ToUpper(); 
                        if (ae == "") a_.Add("Tag prefix disabled");
                        else a_.Add(string.Format("Tag prefix is '{0}'", ae));
                        break;
                    case "scan":
                        switch (c.ToLower())
                        {
                            case "collectors":
                                af = true; 
                                a_.Add("Enabled scanning of Collectors"); 
                                break;
                            case "drills":
                                ag = true; 
                                a_.Add("Enabled scanning of Drills");
                                break;
                            case "grinders": ah = true;
                                a_.Add("Enabled scanning of Grinders");
                                break;
                            case "welders":
                                ai = true;
                                a_.Add("Enabled scanning of Welders"); 
                                break;
                            default:
                                throw new ArgumentException(string.Format("Invalid 'scan=' block type '{0}': must be 'collectors', 'drills', 'grinders' or 'welders'", c));
                        }
                        break;
                    case "quota":
                        switch (c.ToLower())
                        {
                            case "literal": ab = false;
                                a_.Add("Disabled stable dynamic quotas"); 
                                break;
                            case "stable":
                                ab = true; 
                                a_.Add("Enabled stable dynamic quotas");
                                break;
                            default:
                                throw new ArgumentException(string.Format("Invalid 'quota=' mode '{0}': must be 'literal' or 'stable'", c));
                        }
                        break;
                    case "debug":
                        c = c.ToLower(); if (am.Contains(c)) aZ.Add(c);
                        else throw new ArgumentException(string.Format("Invalid 'debug=' type '{0}': must be 'quotas', 'sorting', 'refineries', or 'assemblers'", c)); 
                        break;
                    case "":
                    case "tim_version":
                        break;
                    default: 
                        throw new ArgumentException(string.Format("Unrecognized argument: '{0}'", b));
                }
            }
            aO = new System.Text.RegularExpressions.Regex(
                string.Format(
                    ae != "" ? @"{0} *{2}(|[ ,]+[^{1}]*){1}" : @"{0}([^{1}]*){1}",
                    System.Text.RegularExpressions.Regex.Escape(ac.ToString()),
                    System.Text.RegularExpressions.Regex.Escape(ad.ToString()), 
                    System.Text.RegularExpressions.Regex.Escape(ae)
                    ),
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
        }
        public void ProcessStepProcessArgs()
        { 
            if (Me.CustomData != aj) 
            {
                a_.Add("Arguments changed, re-processing..."); 
                be();
                aj = Me.CustomData;
            }
        }
        public void ProcessStepScanGrids()
        { 
            a_.Add("Scanning grid connectors...");
            bl();
        }
        public void ProcessStepStandbyCheck()
        {
            List<IMyTerminalBlock> b = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(b, c => (c == Me) | (aO.IsMatch(c.CustomName) & aS.Contains(c.CubeGrid)));
            int e = b.IndexOf(Me);
            int f = b.FindIndex(g => g.IsFunctional & g.IsWorking); 
            string h = ac + ae + (b.Count > 1 ? " #" + (e + 1) : "") + ad;
            Me.CustomName = aO.IsMatch(Me.CustomName) ? aO.Replace(Me.CustomName, h, 1) : Me.CustomName + " " + h;
            if (e != f)
            {
                EchoR("TIM #" + (f + 1) + " is on duty. Standing by.");
                if (("" + (b[f] as IMyProgrammableBlock).TerminalRunArgument).Trim() != ("" + Me.TerminalRunArgument).Trim()) EchoR("WARNING: Script arguments do not match TIM #" + (f + 1) + ".");
                throw new a7();
            }
        }
        public void ProcessStepInventoryScan()
        {
            a_.Add("Scanning inventories..."); 
            foreach (string itype in av)
            {
                ay[itype] = 0; 
                foreach (bK data in aC[itype].Values)
                {
                    data.amount = 0L; 
                    data.avail = 0L; 
                    data.locked = 0L;
                    data.invenTotal.Clear();
                    data.invenSlot.Clear();
                }
            }
            b1.Clear();
            b0.Clear();
            b2.Clear(); 
            b3.Clear(); bm();
            bn<IMyAssembler>();
            bn<IMyCargoContainer>(); 
            if (af) bn<IMyCollector>(); 
            bn<IMyGasGenerator>(); 
            bn<IMyGasTank>();
            bn<IMyOxygenFarm>();
            bn<IMyReactor>(); 
            bn<IMyRefinery>(); 
            bn<IMyShipConnector>();
            bn<IMyShipController>();
            if (ag) bn<IMyShipDrill>(); 
            if (ah) bn<IMyShipGrinder>(); 
            if (ai) bn<IMyShipWelder>();
            bn<IMyTextPanel>();
            bn<IMyUserControllableGun>(); 
            bn<IMyParachute>(); 
            if (aP)
            {
                aP = false;
                av.Sort(); 
                foreach (string itype in av) ax[itype].Sort();
                az.Sort();
                foreach (string isub in az) aB[isub].Sort();
            }
        }
        public void ProcessStepParseTags()
        {
            a_.Add("Scanning tags..."); 
            foreach (string itype in av)
            {
                foreach (bK data in aC[itype].Values)
                {
                    data.qpriority = -1; 
                    data.quota = 0L;
                    data.producers.Clear();
                }
            }
            aU.Clear(); 
            aV.Clear();
            aW.Clear(); 
            aT.Clear(); 
            aX.Clear(); 
            aY.Clear(); 
            b4.Clear();
            b5.Clear();
            b8.Clear(); 
            bq();
        }
        public void ProcessStepAmountAdjustment() { a_.Add("Adjusting tallies..."); bo(); }
        public void ProcessStepQuotaPanels() { a_.Add("Scanning quota panels..."); bp(ab); }
        public void ProcessStepLimitedItemRequests() { a_.Add("Processing limited item requests..."); bA(true); }
        public void ProcessStepManageRefineries() { a_.Add("Managing refineries..."); bE(); }
        public void ProcessStepScanProduction() { a_.Add("Scanning production..."); bD(); }
        public void ProcessStepUnlimitedItemRequests() { 
            a_.Add("Processing remaining item requests...");
            //bA(false);
        }
        public void ProcessStepManageAssemblers() { a_.Add("Managing assemblers..."); bF(); }
        public void ProcessStepUpdateInventoryPanels() { a_.Add("Updating inventory panels..."); bG(); }
        bool bf() { if (ba > MAX_RUN_TIME || bb > MAX_LOAD) throw new a8(); return true; }
        void bg(string b, string c, string e, string f, bool g = false)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> h; 
            Dictionary<string, HashSet<string>> j; 
            HashSet<string> k; 
            if (!au.TryGetValue(b.ToUpper(), out h)) au[b.ToUpper()] = h = new Dictionary<string, Dictionary<string, HashSet<string>>>{{"*", new Dictionary<string, HashSet<string>>()}};
            if (!h.TryGetValue(c.ToUpper(), out j))
            {
                h[c.ToUpper()] = j = new Dictionary<string, HashSet<string>>(); 
                if (c != "*" & !g)
                {
                    foreach (KeyValuePair<string, HashSet<string>> pair in h["*"])
                        j[pair.Key] = pair.Value != null ? new HashSet<string>(pair.Value) : null;
                }
            }
            if (f == null | f == "*") 
            {
                j[e] = null;
            }
            else
            {
                (j.TryGetValue(e, out k) ? k : j[e] = new HashSet<string>()).Add(f);
            }
            if (!g) a_.Add(b + "/" + c + " does not accept " + aw[e] + "/" + aA[f]);
        }
        bool bh(IMyCubeBlock b, string c, string e)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> f; 
            Dictionary<string, HashSet<string>> g;
            HashSet<string> h; 
            if (au.TryGetValue(b.BlockDefinition.TypeIdString.ToUpper(), out f))
            {
                f.TryGetValue(b.BlockDefinition.SubtypeName.ToUpper(), out g); 
                if ((g ?? f["*"]).TryGetValue(c, out h)) return !(h == null || h.Contains(e));
            }
            return true;
        }
        HashSet<string> bi(IMyCubeBlock b, string c, HashSet<string > e = null)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> f; 
            Dictionary<string, HashSet<string>> g;
            HashSet<string> h;
            e = e ?? new HashSet<string>(ax[c]); 
            if (au.TryGetValue(b.BlockDefinition.TypeIdString.ToUpper(), out f))
            {
                f.TryGetValue(b.BlockDefinition.SubtypeName.ToUpper(), out g);
                if ((g ?? f["*"]).TryGetValue(c, out h)) e.ExceptWith(h ?? e);
            }
            return e;
        }
        string bj(IMyCubeBlock b, string c)
        {
            string e; 
            e = null;
            foreach (string itype in aB[c]) { if (bh(b, itype, c)) { if (e != null) return null; 
                    e = itype;
                }
            }
            return e;
        }
        string bk(long b)
        {
            long c; 
            if (b <= 0L) return "0"; 
            if (b < 10000L) return "< 0.01"; 
            if (b >= 100000000000000L) return "" + b / 1000000000000L + " M"; 
            c = (long)Math.Pow(10.0,Math.Floor(Math.Log10(b)) - 2.0);
            b = (long)((double)b / c + 0.5) * c;
            if (b < 1000000000L) return (b / 1e6).ToString("0.##");
            if (b < 1000000000000L) return (b / 1e9).ToString("0.##") + " K";
            return (b / 1e12).ToString("0.##") + " M";
        }
        void bl()
        {
            List<IMyTerminalBlock> b = new List<IMyTerminalBlock>();
            IMyCubeGrid c, e;
            Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>> f = new Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>>();
            Dictionary<IMyCubeGrid, int> g = new Dictionary<IMyCubeGrid, int>();
            List<HashSet<IMyCubeGrid>> h = new List<HashSet<IMyCubeGrid>>();
            List<string> j = new List<string>(); 
            HashSet<IMyCubeGrid> k;
            List<IMyCubeGrid> l = new List<IMyCubeGrid>();
            int m, n, o;
            IMyShipConnector p;
            HashSet<string> q = new HashSet<string>();
            HashSet<string> r = new HashSet<string>();
            System.Text.RegularExpressions.Match t; 
            Dictionary<int, Dictionary<int, List<string>>> u = new Dictionary<int, Dictionary<int, List<string>>>();
            Dictionary<int, List<string>> v; 
            List<string> w;
            HashSet<int> x = new HashSet<int>();
            Queue<int> y = new Queue<int>(); 
            GridTerminalSystem.GetBlocksOfType<IMyMechanicalConnectionBlock>(b);
            foreach (IMyTerminalBlock block in b)
            {
                c = block.CubeGrid; 
                e = (block as IMyMechanicalConnectionBlock).TopGrid;
                if (e == null) continue; 
                (f.TryGetValue(c, out k) ? k : f[c] = new HashSet<IMyCubeGrid>()).Add(e);
                (f.TryGetValue(e, out k) ? k : f[e] = new HashSet<IMyCubeGrid>()).Add(c);
            }
            foreach (IMyCubeGrid grid in f.Keys)
            {
                if (!g.ContainsKey(grid))
                {
                    n = (grid.Max - grid.Min + Vector3I.One).Size;
                    c = grid;
                    g[grid] = h.Count;
                    k = new HashSet<IMyCubeGrid> { grid }; 
                    l.Clear();
                    l.AddRange(f[grid]);
                    for (m = 0; m < l.Count; m++)
                    {
                        e = l[m]; 
                        if (!k.Add(e)) continue; 
                        o = (e.Max - e.Min + Vector3I.One).Size;
                        c = o > n ? e : c;
                        n = o > n ? o : n;
                        g[e] = h.Count;
                        l.AddRange(f[e].Except(k));
                    }
                    h.Add(k); 
                    j.Add(c.CustomName);
                }
            }
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(b); 
            foreach (IMyTerminalBlock block in b)
            {
                p = (block as IMyShipConnector).OtherConnector; 
                if (p != null && block.EntityId < p.EntityId & (block as IMyShipConnector).Status == MyShipConnectorStatus.Connected)
                {
                    q.Clear();
                    r.Clear(); 
                    if ((t = aO.Match(block.CustomName)).Success)
                    {
                        foreach (string attr in t.Groups[1].Captures[0].Value.Split(at, ao))
                        {
                            if (attr.StartsWith("DOCK:", an)) q.UnionWith(attr.Substring(5).ToUpper().Split(aq, ao));
                        }
                    }
                    if ((t = aO.Match(p.CustomName)).Success)
                    {
                        foreach (string attr in t.Groups[1].Captures[0].Value.Split(at, ao))
                        {
                            if (attr.StartsWith("DOCK:", an)) r.UnionWith(attr.Substring(5).ToUpper().Split(aq, ao)); 
                        }
                    }
                    if ((q.Count > 0 | r.Count > 0) & !q.Overlaps(r)) continue;
                    c = block.CubeGrid; 
                    e = p.CubeGrid;
                    if (!g.TryGetValue(c, out n))
                    {
                        g[c] = n = h.Count;
                        h.Add(new HashSet<IMyCubeGrid> { c });
                        j.Add(c.CustomName);
                    }
                    if (!g.TryGetValue(e, out o))
                    {
                        g[e] = o = h.Count;
                        h.Add(new HashSet<IMyCubeGrid> { e }); 
                        j.Add(e.CustomName);
                    }
                    ((u.TryGetValue(n, out v) ? v : u[n] = new Dictionary<int, List<string>>()).TryGetValue(o, out w) ? w : u[n][o] = new List<string>()).Add(block.CustomName); 
                    ((u.TryGetValue(o, out v) ? v : u[o] = new Dictionary<int, List<string>>()).TryGetValue(n, out w) ? w : u[o][n] = new List<string>()).Add(p.CustomName);
                }
            }
            aS.Clear(); 
            aS.Add(Me.CubeGrid); 
            if (!g.TryGetValue(Me.CubeGrid, out n)) return;
            x.Add(n); 
            aS.UnionWith(h[n]);
            y.Enqueue(n); 
            while (y.Count > 0)
            {
                n = y.Dequeue(); 
                if (!u.TryGetValue(n, out v)) continue;
                foreach (int ship2 in v.Keys)
                {
                    if (x.Add(ship2))
                    {
                        aS.UnionWith(h[ship2]);
                        y.Enqueue(ship2);
                        a_.Add(j[ship2] + " docked to " + j[n] + " at " + String.Join(", ", v[ship2]));
                    }
                }
            }
        }
        void bm()
        {
            List<IMyBlockGroup> b = new List<IMyBlockGroup>();
            List<IMyTerminalBlock> c = new List<IMyTerminalBlock>(); 
            System.Text.RegularExpressions.Match e;
            GridTerminalSystem.GetBlockGroups(b); 
            foreach (IMyBlockGroup group in b)
            {
                if ((e = aO.Match(group.Name)).Success) 
                {
                    group.GetBlocks(c); 
                    foreach (IMyTerminalBlock block in c) b0[block] = e; 
                }
            }
        }
        void bn<T>() where T : class
        {
            List<IMyTerminalBlock> b = new List<IMyTerminalBlock>(); 
            System.Text.RegularExpressions.Match c; 
            int e, f, g;
            IMyInventory h;
            List<MyInventoryItem> j = new List<MyInventoryItem>(); 
            string k, l; 
            bK m; 
            long n, o; 
            GridTerminalSystem.GetBlocksOfType<T>(b);
            foreach (IMyTerminalBlock block in b)
            {
                if (!aS.Contains(block.CubeGrid)) continue;
                c = aO.Match(block.CustomName);
                if (c.Success) 
                {
                    b0.Remove(block);
                    b1[block] = c; 
                }
                else if (b0.TryGetValue(block, out c)) 
                {
                    b1[block] = c;
                }
                if ((block is IMySmallMissileLauncher & !(block is IMySmallMissileLauncherReload | block.BlockDefinition.SubtypeName == "LargeMissileLauncher")) | block is IMyLargeInteriorTurret) 
                {
                    b2.Add(block.GetInventory(0)); 
                }
                else if (block is IMyFunctionalBlock && (block as IMyFunctionalBlock).Enabled & block.IsFunctional)
                {
                    if ((block is IMyRefinery | block is IMyReactor | block is IMyGasGenerator) & !b1.ContainsKey(block)) 
                    {
                        b2.Add(block.GetInventory(0)); 
                    }
                    else if (block is IMyAssembler && !(block as IMyAssembler).IsQueueEmpty) 
                    {
                        b2.Add(block.GetInventory((block as IMyAssembler).Mode == MyAssemblerMode.Disassembly ? 1 : 0));
                    }
                }
                e = block.InventoryCount;
                while (e-- > 0)
                {
                    h = block.GetInventory(e);
                    j.Clear(); 
                    h.GetItems(j);
                    f = j.Count; 
                    while (f-- > 0)
                    {
                        k = "" + j[f].Type.TypeId; 
                        k = k.Substring(k.LastIndexOf('_') + 1);
                        l = j[f].Type.SubtypeId; 
                        bK.InitItem(k, l, 0L, 0.0f, j[f].Type.SubtypeId, null);
                        k = k.ToUpper();
                        l = l.ToUpper(); 
                        n = (long)((double)j[f].Amount * 1e6);
                        ay[k] += n;
                        m = aC[k][l]; 
                        m.amount += n;
                        m.avail += n; 
                        m.invenTotal.TryGetValue(h, out o);
                        m.invenTotal[h] = o + n; 
                        m.invenSlot.TryGetValue(h, out g); 
                        m.invenSlot[h] = Math.Max(g, f + 1);
                    }
                }
            }
        }
        void bo()
        {
            string b, c;
            long e;
            bK f; 
            List<MyInventoryItem> g = new List<MyInventoryItem>();
            foreach (IMyInventory inven in b3)
            {
                g.Clear(); 
                inven.GetItems(g);
                foreach (MyInventoryItem stack in g)
                {
                    b = "" + stack.Type.TypeId; 
                    b = b.Substring(b.LastIndexOf('_') + 1).ToUpper();
                    c = stack.Type.SubtypeId.ToUpper();
                    e = (long)((double)stack.Amount * 1e6); 
                    ay[b] -= e;
                    aC[b][c].amount -= e;
                }
            }
            foreach (IMyInventory inven in b2)
            {
                g.Clear(); 
                inven.GetItems(g); 
                foreach (MyInventoryItem stack in g)
                {
                    b = "" + stack.Type.TypeId
                        b = b.Substring(b.LastIndexOf('_') + 1).ToUpper();
                    c = stack.Type.SubtypeId.ToUpper();
                    e = (long)((double)stack.Amount * 1e6);
                    f = aC[b][c];
                    f.avail -= e;
                    f.locked += e;
                }
            }
        }
        void bp(bool b)
        {
            bool c = aZ.Contains("quotas");
            int e, f, g, h, j, k, l, m, n, o;
            long p, q, r;
            float t;
            bool u;
            string v, w, x; 
            string[] y, z = new string[1] { " " };
            string[][] C; 
            IMyTextPanel D; 
            IMySlimBlock E; 
            Matrix F; 
            StringBuilder G = new StringBuilder();
            List<string> H = new List<string>(), I = new List<string>(), J = new List<string>();
            Dictionary<string, SortedDictionary<string, string[]>> K = new Dictionary<string, SortedDictionary<string, string[]>>(); 
            bK L; 
            ScreenFormatter M; 
            foreach (bK d in aC["ORE"].Values) d.minimum = d.amount == 0L ? 0L : Math.Max(d.minimum, d.amount);
            foreach (IMyTextPanel panel in aU.Keys)
            {
                h = panel.BlockDefinition.SubtypeName.EndsWith("Wide") ? 2 : 1; 
                j = panel.BlockDefinition.SubtypeName.StartsWith("Small") ? 3 : 1;
                k = l = 1;
                if (b8.ContainsKey(panel))
                {
                    k = b8[panel].A;
                    l = b8[panel].B;
                }
                C = new string[k][]; 
                panel.Orientation.GetMatrix(out F); 
                G.Clear();
                for (g = 0; g < l; g++)
                {
                    m = 0;
                    for (f = 0; f < k; f++)
                    {
                        C[f] = z;
                        E = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position + f * h * j * F.Right + g * j * F.Down)); 
                        D = E != null ? E.FatBlock as IMyTextPanel : null;
                        if (D != null && "" + D.BlockDefinition == "" + panel.BlockDefinition & D.GetPublicTitle().ToUpper().Contains("QUOTAS"))
                        {
                            C[f] = D.GetText().Split('\n');
                            m = Math.Max(m, C[f].Length);
                        }
                    }
                    for (e = 0; e < m; e++) 
                    {
                        for (f = 0; f < k; f++) G.Append(e < C[f].Length ? C[f][e] : " ");
                        G.Append("\n"); 
                    }
                }
                o = aU[panel];
                v = "";
                H.Clear();
                K.Clear(); 
                I.Clear(); 
                foreach (string line in G.ToString().Split('\n'))
                {
                    y = line.ToUpper().Split(ap, 4, ao);
                    if (y.Length >= 1)
                    {
                        if (bs(null, y, v, out w, out x, out n, out p, out t, out u) & w == v & w != "" & x != "")
                        {
                            L = aC[w][x];
                            K[w][x] = new[]{L.label,""+Math.Round(p/1e6,2),""+Math.Round(t*100.0f,2)+"%"}; 
                            if ((o > 0 & (o < L.qpriority | L.qpriority <= 0)) | (o == 0 & L.qpriority < 0))
                            {
                                L.qpriority = o;
                                L.minimum = p; 
                                L.ratio = t;
                            }
                            else if (o == L.qpriority)
                            {
                                L.minimum = Math.Max(L.minimum, p);
                                L.ratio = Math.Max(L.ratio, t); 
                            }
                        }
                        else if (bs(null, y, "", out w, out x, out n, out p, out t, out u) & w != v & w != "" & x == "") 
                        {
                            if (!K.ContainsKey(v = w))
                            {
                                H.Add(v);
                                K[v] = new SortedDictionary<string, string[]>();
                            }
                        }
                        else if (v != "")
                        {
                            K[v][y[0]] = y; 
                        }
                        else
                        {
                            I.Add(line);
                        }
                    }
                }
                M = new ScreenFormatter(4, 2); 
                M.SetAlign(1, 1);
                M.SetAlign(2, 1);
                if (H.Count == 0 & aV[panel].Count == 0) aV[panel].AddRange(av); 
                foreach (string qtype in aV[panel])
                {
                    if (!K.ContainsKey(qtype))
                    {
                        H.Add(qtype); K[qtype] = new SortedDictionary <string, string[]>();
                    }
                }
                foreach (string qtype in H)
                {
                    if (qtype == "ORE") continue;
                    if (M.GetNumRows() > 0) M.AddBlankRow(); 
                    M.Add(0, aw[qtype], true); 
                    M.Add(1, "  Min", true);
                    M.Add(2, "  Pct", true); 
                    M.Add(3, "", true); 
                    M.AddBlankRow();
                    foreach (bK d in aC[qtype].Values)
                    {
                        if (!K[qtype].ContainsKey(d.subType)) K[qtype][d.subType] = new[] { d.label, "" + Math.Round(d.minimum / 1e6, 2), "" + Math.Round(d.ratio * 100.0f, 2) + "%" };
                    }
                    foreach (string qsub in K[qtype].Keys)
                    {
                        y = K[qtype][qsub];
                        M.Add(0, aC[qtype].ContainsKey(qsub) ? y[0] : y[0].ToLower(), true); 
                        M.Add(1, y.Length > 1 ? y[1] : "", true); 
                        M.Add(2, y.Length > 2 ? y[2] : "", true);
                        M.Add(3, y.Length > 3 ? y[3] : "", true);
                    }
                }
                bI("TIM Quotas", M, panel, true, I.Count == 0 ? "" : String.Join("\n", I).Trim().ToLower() + "\n\n");
            }
            foreach (string qtype in av)
            {
                q = 1L;
                if (!FRACTIONAL_TYPES.Contains(qtype)) q = 1000000L;
                r = ay[qtype];
                if (b & r > 0L)
                {
                    J.Clear();
                    foreach (bK d in aC[qtype].Values)
                    {
                        if (d.ratio > 0.0f & r >= (long)(d.minimum / d.ratio)) J.Add(d.subType);
                    }
                    if (J.Count > 0)
                    {
                        J.Sort((N, O) => {
                            bK P = aC[qtype][N], Q = aC[qtype][O]; long R = (long)(P.amount / P.ratio), S = (long)(Q.amount / Q.ratio); return R
              == S ? P.ratio.CompareTo(Q.ratio) : R.CompareTo(S);
                        }); x = J[(J.Count - 1) / 2]; L = aC[qtype][x]; r = (long)(L.amount / L.ratio + 0.5f); if (c)
                        {
                            a_.Add(
"median " + aw[qtype] + " is " + aA[x] + ", " + r / 1e6 + " -> " + L.amount / 1e6 / L.ratio); foreach (string qsub in J)
                            {
                                L = aC[qtype][qsub]; a_.Add("  " +
aA[qsub] + " @ " + L.amount / 1e6 + " / " + L.ratio + " => " + (long)(L.amount / 1e6 / L.ratio + 0.5f));
                            }
                        }
                    }
                }
                foreach (bK d in aC[qtype].Values)
                {
                    p = Math.Max(d.quota, Math.Max(d.minimum, (long)(d.ratio * r + 0.5f)));
                    d.quota = p / q * q;
                }
            }
        }
        void bq()
        {
            StringBuilder b = new StringBuilder();
            IMyTextPanel c;
            IMyRefinery e; 
            IMyAssembler f;
            System.Text.RegularExpressions.Match g; 
            int h, j, k, l; 
            string[] m, n; 
            string o, p, q; 
            long r;
            float t;
            bool u, v, w = false; 
            foreach (IMyTerminalBlock block in b1.Keys)
            {
                g = b1[block]; 
                m = g.Groups[1].Captures[0].Value.Split(at, ao); 
                b.Clear();
                if (!(u = b0.ContainsKey(block))) 
                {
                    b.Append(block.CustomName, 0, g.Index); 
                    b.Append(ac);
                    if (ae != "") b.Append(ae + " "); 
                }
                if ((c = block as IMyTextPanel) != null)
                {
                    foreach (string a in m)
                    {
                        o = a.ToUpper();
                        n = o.Split(aq);
                        o = n[0]; 
                        if (o.Length >= 4 & "STATUS".StartsWith(o))
                        {
                            if (c.Enabled) aX.Add(c);
                            b.Append("STATUS ");
                        }
                        else if (o.Length >= 5 & "DEBUGGING".StartsWith(o)) 
                        {
                            if (c.Enabled) aY.Add(c);
                            b.Append("DEBUG ");
                        }
                        else if (o == "SPAN")
                        {
                            if (n.Length >= 3 && int.TryParse(n[1], out k) & int.TryParse(n[2], out l) & k >= 1 & l >= 1)
                            {
                                b8[c] = new a9(k, l);
                                b.Append("SPAN:" + k + ":" + l + " ");
                            }
                            else
                            {
                                b.Append((o = String.Join(":", n).ToLower()) + " ");
                                a_.Add("Invalid panel span rule: " + o);
                            }
                        }
                        else if (o == "THE")
                        {
                            w = true;
                        }
                        else if (o == "ENCHANTER" & w)
                        {
                            w = false;
                            c.SetValueFloat("FontSize", 0.2f);
                            c.WritePublicTitle("TIM the Enchanter");
                            ((IMyTextSurface) c).ContentType = ContentType.TEXT_AND_IMAGE;
                            b.Append("THE ENCHANTER ");
                        }
                        else if (o.Length >= 3 & "QUOTAS".StartsWith(o))
                        {
                            if (c.Enabled & !aU.ContainsKey(c)) aU[c] = 0; 
                            if (c.Enabled & !aV.ContainsKey(c)) aV[c] = new List<string>(); 
                            b.Append("QUOTA");
                            h = 0;
                            while (++h < n.Length)
                            {
                                if (br(null, true, n[h], "", out p, out q) & p != "ORE" & q == "")
                                {
                                    if (c.Enabled) aV[c].Add(p);
                                    b.Append(":" + aw[p]); 
                                }
                                else if (n[h].StartsWith("P") & int.TryParse(n[h].Substring(Math.Min(1, n[h].Length)), out j)) 
                                {
                                    if (c.Enabled) aU[c] = Math.Max(0, j); 
                                    if (j > 0) b.Append(":P" + j);
                                }
                                else
                                {
                                    b.Append(":" + n[h].ToLower()); 
                                    a_.Add("Invalid quota panel rule: " + n[h].ToLower());
                                }
                            }
                            b.Append(" ");
                        }
                        else if (o.Length >= 3 & "INVENTORY".StartsWith(o))
                        {
                            if (c.Enabled & !aW.ContainsKey(c)) aW[c] = new List<string>(); 
                            b.Append("INVEN"); h = 0; 
                            while (++h < n.Length)
                            {
                                if (br(null, true, n[h], "", out p, out q) & q == "")
                                {
                                    if (c.Enabled) aW[c].Add(p);
                                    b.Append(":" + aw[p]);
                                }
                                else
                                {
                                    b.Append(":" + n[h].ToLower());
                                    a_.Add("Invalid inventory panel rule: " + n[h].ToLower());
                                }
                            }
                            b.Append(" ");
                        }
                        else
                        {
                            b.Append((o = String.Join(":", n).ToLower()) + " ");
                            a_.Add("Invalid panel attribute: " + o);
                        }
                    }
                }
                else
                {
                    e = block as IMyRefinery;
                    f = block as IMyAssembler; 
                    foreach (string a in m)
                    {
                        o = a.ToUpper();
                        n = o.Split(aq); 
                        o = n[0];
                        if ((o.Length >= 4 & "LOCKED".StartsWith(o)) | o == "EXEMPT")
                        {
                            h = block.InventoryCount;
                            while (h-- > 0) b2.Add(block.GetInventory(h)); 
                            b.Append(o + " ");
                        }
                        else if (o == "HIDDEN")
                        {
                            h = block.InventoryCount;
                            while (h-- > 0) b3.Add(block.GetInventory(h));
                            b.Append("HIDDEN ");
                        }
                        else if (block is IMyShipConnector & o == "DOCK") 
                        {
                            b.Append(String.Join(":", n) + " ");
                        }
                        else if ((e != null | f != null) & o == "AUTO")
                        {
                            b.Append("AUTO");
                            HashSet<string> x, y = e == null | n.Length > 1 ? new HashSet<string>() : bi(e, "ORE");
                            HashSet<ak> z, C = new HashSet<ak>();
                            h = 0;
                            while (++h < n.Length)
                            {
                                if (br(null, true, n[h], e != null ? "ORE" : "", out p, out q) & e != null == (p == "ORE") & (e != null | p != "INGOT"))
                                {
                                    if (q == "")
                                    {
                                        if (e != null)
                                        {
                                            y.UnionWith(ax[p]);
                                        }
                                        else 
                                        { 
                                            foreach (string s in ax[p]) C.Add(new ak(p, s));
                                        }
                                        b.Append(":" + aw[p]);
                                    }
                                    else
                                    {
                                        if (e != null)
                                        {
                                            y.Add(q);
                                        }
                                        else
                                        {
                                            C.Add(new ak(p, q));
                                        }
                                        b.Append(":" + (e == null & aB[q].Count > 1 ? aw[p] + "/" : "") + aA[q]);
                                    }
                                }
                                else
                                {
                                    b.Append(":" + n[h].ToLower()); 
                                    a_.Add("Unrecognized or ambiguous item: " + n[h].ToLower());
                                }
                            }
                            if (e != null)
                            {
                                if (e.Enabled) (b4.TryGetValue(e, out x) ? x : b4[e] = new HashSet<string>()).UnionWith(y);
                            }
                            else if (f.Enabled) (b5.TryGetValue(f, out z) ? z : b5[f] = new HashSet<ak>()).UnionWith(C); 
                            b.Append(" ");
                        }
                        else if (!bs(block, n, "", out p, out q, out j, out r, out t, out v))
                        {
                            b.Append((o = String.Join(":", n).ToLower()) + " ");
                            a_.Add("Unrecognized or ambiguous item: " + o);
                        }
                        else if (!block.HasInventory |
                            (block is IMySmallMissileLauncher & 
                            !(block is IMySmallMissileLauncherReload | 
                            block.BlockDefinition.SubtypeName == "LargeMissileLauncher")) |
                            block is IMyLargeInteriorTurret)
                        {
                            b.Append(String.Join(":", n).ToLower() + " "); 
                            a_.Add("Cannot sort items to " + block.CustomName + ": no conveyor-connected inventory");
                        }
                        else
                        {
                            if (q == "") 
                            {
                                foreach (string s in v ? (IEnumerable<string>)ax[p] : (IEnumerable<string>)bi(block, p)) bt(block, 0, p, s, j, r); 
                            }
                            else
                            {
                                bt(block, 0, p, q, j, r);
                            }
                            if (aa & !u)
                            {
                                if (v) 
                                { 
                                    b.Append("FORCE:" + aw[p]);
                                    if (q != "") b.Append("/" + aA[q]);
                                }
                                else if (q == "")
                                { 
                                    b.Append(aw[p]); 
                                }
                                else if (aB[q].Count == 1 || bj(block, q) == p)
                                { 
                                    b.Append(aA[q]);
                                }
                                else 
                                {
                                    b.Append(aw[p] + "/" + aA[q]);
                                }
                                if (j > 0 & j < int.MaxValue) b.Append(":P" + j); 
                                if (r >= 0L) b.Append(":" + r / 1e6); 
                                b.Append(" ");
                            }
                        }
                    }
                }
                if (aa & !u)
                {
                    if (b[b.Length - 1] == ' ') b.Length--; 
                    b.Append(ad).Append(block.CustomName, g.Index + g.Length, block.CustomName.Length - g.Index - g.Length); 
                    block.CustomName = b.ToString();
                }
                if (block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.Owner & 
                    block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.FactionShare) a_.Add("Cannot control \"" + block.CustomName + "\" due to differing ownership");
            }
        }
        bool br(IMyCubeBlock b, bool c, string e, string f, out string g, out string h)
        {
            int j, k, l;
            string[] m; 
            g = "";
            h = ""; 
            l = 0;
            m = e.Trim().Split('/'); 
            if (m.Length >= 2)
            {
                m[0] = m[0].Trim(); 
                m[1] = m[1].Trim();
                if (ax.ContainsKey(m[0]) && m[1] == "" | aC[m[0]].ContainsKey(m[1]))
                {
                    if (c || bh(b, m[0], m[1])) 
                    {
                        l = 1;
                        g = m[0]; 
                        h = m[1]; 
                    }
                }
                else
                {
                    j = av.BinarySearch(m[0]);
                    j = Math.Max(j, ~j);
                    while (l < 2 & j < av.Count && av[j].StartsWith(m[0]))
                    {
                        k = ax[av[j]].BinarySearch(m[1]);
                        k = Math.Max(k, ~k);
                        while (l < 2 & k < ax[av[j]].Count && ax[av[j]][k].StartsWith(m[1])) 
                        {
                            if (c || bh(b, av[j], ax[av[j]][k]))
                            {
                                l++; 
                                g = av[j];
                                h = ax[av[j]][k]; 
                            }
                            k++; 
                        }
                        if (l == 0 & av[j] == "INGOT" & "GRAVEL".StartsWith(m[1]) & (c || bh(b, "INGOT", "STONE"))) 
                        {
                            l++; 
                            g = "INGOT"; 
                            h = "STONE"; 
                        }
                        j++;
                    }
                }
            }
            else if (ax.ContainsKey(m[0]))
            {
                if (c || bh(b, m[0], "")) 
                {
                    l++; 
                    g = m[0];
                    h = ""; 
                }
            }
            else if (aB.ContainsKey(m[0]))
            {
                if (f != "" && aC[f].ContainsKey(m[0])) 
                {
                    l++;
                    g = f; 
                    h = m[0]; 
                }
                else
                {
                    j = aB[m[0]].Count;
                    while (l < 2 & j-- > 0) { if (c || bh(b, aB[m[0]][j], m[0]))
                        {
                            l++; 
                            g = aB[m[0]][j]; 
                            h = m[0]; 
                        }
                    }
                }
            }
            else if (f != "")
            {
                k = ax[f].BinarySearch(m[0]);
                k = Math.Max(k, ~k); 
                while (l < 2 & k < ax[f].Count && ax[f][k].StartsWith(m[0])) 
                {
                    l++;
                    g = f;
                    h = ax[f][k]; 
                    k++;
                }
                if (l == 0 & f == "INGOT" & "GRAVEL".StartsWith(m[0]))
                {
                    l++;
                    g = "INGOT";
                    h = "STONE"; 
                }
            }
            else
            {
                j = av.BinarySearch(m[0]); 
                j = Math.Max(j, ~j);
                while (l < 2 & j < av.Count && av[j].StartsWith(m[0]))
                {
                    if (c || bh(b, av[j], ""))
                    {
                        l++; 
                        g = av[j];
                        h = "";
                    }
                    j++;
                }
                k = az.BinarySearch(m[0]);
                k = Math.Max(k, ~k);
                while (l < 2 & k < az.Count && az[k].StartsWith(m[0]))
                {
                    j = aB[az[k]].Count; 
                    while (l < 2 & j-- > 0)
                    {
                        if (c || bh(b, aB[az[k]][j], az[k]))
                        {
                            if (l != 1 || g != aB[az[k]][j] | h != "" | ax[g].Count != 1) l++;
                            g = aB[az[k]][j]; 
                            h = az[k];
                        }
                    }
                    k++;
                }
                if (l == 0 & "GRAVEL".StartsWith(m[0]) & (c || bh(b, "INGOT", "STONE")))
                {
                    l++; 
                    g = "INGOT";
                    h = "STONE";
                }
            }
            if (!c & b != null & l == 1 & h == "")
            {
                HashSet<string> n = bi(b, g); 
                if (n.Count == 1) h = n.First();
            }
            return l == 1;
        }
        bool bs(IMyCubeBlock b, string[] c, string e, out string f, out stringg, out int h, out long j, out float k, out bool l)
        {
            int m, n;
            double o, p;
            f = ""; 
            g = ""; 
            h = 0; 
            j = -1L;
            k = -1.0f;
            l = b == null;
            m = 0;
            if (c[0].Trim() == "FORCE")
            { if (c.Length == 1)
                    return false;
                l = true; 
                m = 1;
            }
            if (!br(b, l, c[m], e, out f, out g)) return false; while (++m < c.Length)
            {
                c[m] = c[m].Trim();
                n = c[m].Length; if (n != 0)
                {
                    if (c[m] == "IGNORE") { j = 0L; }
                    else if (c[m] == "OVERRIDE" | c[m] == "SPLIT") { }
                    else if (c[m][n - 1] == '%' & double.TryParse(c[m].Substring(0, n - 1), out o)) { k = Math.Max(0.0f, (float)(o / 100.0)); }
                    else if (c[m][0] == 'P' & double.TryParse(c[m].Substring(1), out o))
                    {
                        h = Math.Max(1, (int)(o + 0.5));
                    }
                    else
                    {
                        p = 1.0; if (c[m][n - 1] == 'K') { n--; p = 1e3; } else if (c[m][n - 1] == 'M') { n--; p = 1e6; }
                        if (double.TryParse(c[m].Substring(0, n), out o)) j = Math.Max(0L, (long)(o * p * 1e6 + 0.5));
                    }
                }
            }
            return true;
        }
        void bt(IMyTerminalBlock b, int c, string e, string f, int g, long h)
        {
            long j; Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>> k; Dictionary<string, Dictionary<IMyInventory, long
          >> l; Dictionary<IMyInventory, long> m; if (g == 0) g = int.MaxValue; k = aT.TryGetValue(g, out k) ? k : aT[g] = new Dictionary<string, Dictionary<
                      string, Dictionary<IMyInventory, long>>>(); l = k.TryGetValue(e, out l) ? l : k[e] = new Dictionary<string, Dictionary<IMyInventory, long>>(); m =
                                     l.TryGetValue(f, out m) ? m : l[f] = new Dictionary<IMyInventory, long>(); IMyInventory n = b.GetInventory(c); m.TryGetValue(n, out j); m[n] = h;
            aC[e][f].quota += Math.Min(0L, -j) + Math.Max(0L, h); if (n.Owner != null)
            {
                if (b is IMyRefinery && (b as IMyProductionBlock).UseConveyorSystem)
                { b.GetActionWithName("UseConveyor").Apply(b); 
                    a_.Add("Disabling conveyor system for " + b.CustomName);
                }
                if (b is IMyGasGenerator && (b as IMyGasGenerator).UseConveyorSystem)
                {
                    b.GetActionWithName("UseConveyor").Apply(b); 
                    a_.Add("Disabling conveyor system for " + b.CustomName);
                }
                if (b is IMyReactor && (b as IMyReactor).UseConveyorSystem)
                {
                    b.GetActionWithName("UseConveyor").Apply(b);
                    a_.Add("Disabling conveyor system for " + b.CustomName);
                }
                if (b is IMyLargeConveyorTurretBase && ((IMyLargeConveyorTurretBase)b).UseConveyorSystem)
                {
                    b.GetActionWithName("UseConveyor").Apply(b); 
                    a_.Add("Disabling conveyor system for " + b.CustomName);
                }
                if (b is IMySmallGatlingGun && ((IMySmallGatlingGun)b).UseConveyorSystem)
                {
                    b.GetActionWithName("UseConveyor").Apply(b); 
                    a_.Add("Disabling conveyor system for " + b.CustomName);
                }
                if (b is IMySmallMissileLauncher && ((IMySmallMissileLauncher)b).UseConveyorSystem)
                {
                    b.GetActionWithName("UseConveyor").Apply(b);
                    a_.Add("Disabling conveyor system for " + b.CustomName);
                }
            }
        }
        List<int> bu; int bv; List<string> bw; int bx; List<string> by; int bz; void bA(bool b)
        {
            if (bu == null) { bu = new List<int>(aT.Keys); bu.Sort(); bv = 0; }
            for (; bv < bu.Count; bv++)
            {
                if (bw == null) { bw = new List<string>(aT[bu[bv]].Keys); bx = 0; }
                for (; bx < bw.Count; bx++)
                {
                    if (by == null)
                    {
                        by = new List<string>(aT[bu[bv]][bw[bx]].Keys); bz = 0;
                    }
                    bool c = false; for (; bz < by.Count; bz++) { if (c) bf(); bB(b, bu[bv], bw[bx], by[bz]); c = true; }
                    by = null;
                }
                bw = null;
            }
            bu = null; if (!b)
            {
                foreach (string itype in av)
                {
                    foreach (bK data in aC[itype].Values)
                    {
                        if (data.avail > 0L) a_.Add("No place to put " + bk(data.avail) + " " + aw[itype] + "/" + aA[data.subType] + ", containers may be full");
                    }
                }
            }
        }
        void bB(bool b, int c, string e, string f)
        {
            bool g = aZ.Contains("sorting");
            int h, j;
            long k, l, m, n, o, p, q;
            List<IMyInventory> r = null;
            Dictionary<IMyInventory, long> t;
            if (g) a_.Add("sorting " + aw[e] + "/" + aA[f] + " lim=" + b + " p=" + c); q = 1L; 
            if (!FRACTIONAL_TYPES.Contains(e)) q = 1000000L;
            t = new Dictionary<IMyInventory, long>(); 
            bK u = aC[e][f]; 
            k = 0L;
            foreach (IMyInventory reqInven in aT[c][e][f].Keys)
            {
                m = aT[c][e][f][reqInven]; 
                if (m != 0L & b == m >= 0L)
                {
                    if (m < 0L)
                    {
                        m = 1000000L; 
                        if (reqInven.MaxVolume != VRage.MyFixedPoint.MaxValue) m = (long)((double)reqInven.MaxVolume * 1e6);
                    }
                    t[reqInven] = m; k += m;
                }
            }
            if (g) a_.Add("total req=" + k / 1e6); 
            if (k <= 0L) return; 
            l = u.avail + u.locked;
            if (g) a_.Add("total avail=" + l / 1e6); 
            if (l > 0L)
            {
                r = new List<IMyInventory>(u.invenTotal.Keys); do
                {
                    h = 0; j = 0;
                    foreach (IMyInventory amtInven in r)
                    {
                        n = u.invenTotal[amtInven]; if (n > 0L & b2.Contains(amtInven))
                        {
                            h++; 
                            t.TryGetValue(amtInven, out m);
                            o = (long)((double)m / k * l); 
                            if (b) o = Math.Min(o, m); o = o / q * q; 
                            if (n >= o)
                            {
                                if (g) a_.Add("locked " + (amtInven.Owner == null ? "???" : (amtInven.Owner as IMyTerminalBlock).CustomName) + " gets " + o / 1e6 + ", has " + n / 1e6); 
                                j++;
                                k -= m; 
                                t[amtInven] = 0L;
                                l -= n; 
                                u.locked -= n; 
                                u.invenTotal[amtInven] = 0L;
                            }
                        }
                    }
                } while (h > j & j > 0);
            }
            foreach (IMyInventory reqInven in t.Keys)
            {
                m = t[reqInven]; if (m <= 0L | k <= 0L | l <= 0L)
                {
                    if (b & m > 0L) a_.Add("Insufficient " + aw
[e] + "/" + aA[f] + " to satisfy " + (reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName)); continue;
                }
                o = (long)((
double)m / k * l); if (b) o = Math.Min(o, m); o = o / q * q; if (g) a_.Add((reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName
) + " gets " + m / 1e6 + " / " + k / 1e6 + " of " + l / 1e6 + " = " + o / 1e6); k -= m; if (u.invenTotal.TryGetValue(reqInven, out n))
                {
                    n = Math.Min(n, o); o -= n; l -= n
; if (b2.Contains(reqInven)) { u.locked -= n; } else { u.avail -= n; }
                    u.invenTotal[reqInven] -= n;
                }
                foreach (IMyInventory amtInven in r)
                {
                    n = Math.Min
(u.invenTotal[amtInven], o); p = 0L; if (n > 0L & b2.Contains(amtInven) == false)
                    {
                        p = bC(e, f, n, amtInven, reqInven); o -= p; l -= p; u.avail -= p; u.
invenTotal[amtInven] -= p;
                    }
                    if (o <= 0L | (p != 0L & p != n)) break;
                }
                if (b & o > 0L)
                {
                    a_.Add("Insufficient " + aw[e] + "/" + aA[f] + " to satisfy " + (reqInven.
Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName));
                }
            }
            if (g) a_.Add("" + l / 1e6 + " left over");
        }
        long bC(string b, string
c, long e, IMyInventory f, IMyInventory g)
        {
            bool h = aZ.Contains("sorting"); List<MyInventoryItem> j = new List<MyInventoryItem>(); int k;
            VRage.MyFixedPoint l, m; uint n; string o, p; l = (VRage.MyFixedPoint)(e / 1e6); f.GetItems(j); k = Math.Min(aC[b][c].invenSlot[f], j.Count);
            while (l > 0 & k-- > 0)
            {
                o = "" + j[k].Type.TypeId; o = o.Substring(o.LastIndexOf('_') + 1).ToUpper(); p = j[k].Type.SubtypeId.ToUpper(); if (o == b & p == c)
                {
                    m = j[k].Amount; n = j[k].ItemId; if (f == g) { l -= m; if (l < 0) l = 0; }
                    else if (f.TransferItemTo(g, k, null, true, l))
                    {
                        j.Clear(); f.GetItems(j); if (k < j.
Count && j[k].ItemId == n) m -= j[k].Amount; if (m <= 0)
                        {
                            if ((double)g.CurrentVolume < (double)g.MaxVolume / 2 & g.Owner != null)
                            {
                                VRage.ObjectBuilders
.SerializableDefinitionId q = (g.Owner as IMyCubeBlock).BlockDefinition; bg(q.TypeIdString, q.SubtypeName, b, c);
                            }
                            k = 0;
                        }
                        else
                        {
                            aJ++; if (h) a_
.Add("Transferred " + bk((long)((double)m * 1e6)) + " " + aw[b] + "/" + aA[c] + " from " + (f.Owner == null ? "???" : (f.Owner as IMyTerminalBlock).
CustomName) + " to " + (g.Owner == null ? "???" : (g.Owner as IMyTerminalBlock).CustomName));
                        }
                        l -= m;
                    }
                    else if (!f.IsConnectedTo(g) & f.Owner !=
null & g.Owner != null)
                    {
                        if (!b9.ContainsKey(f.Owner as IMyTerminalBlock)) b9[f.Owner as IMyTerminalBlock] = new HashSet<IMyTerminalBlock>(
      ); b9[f.Owner as IMyTerminalBlock].Add(g.Owner as IMyTerminalBlock); k = 0;
                    }
                }
            }
            return e - (long)((double)l * 1e6 + 0.5);
        }
        void bD()
        {
            List<
IMyTerminalBlock> b = new List<IMyTerminalBlock>(), c = new List<IMyTerminalBlock>(); List<MyInventoryItem> e = new List<MyInventoryItem>();
            string f, g, h; List<MyProductionItem> j = new List<MyProductionItem>(); ak k; b6.Clear(); GridTerminalSystem.GetBlocksOfType<
                     IMyGasGenerator>(b, l => aS.Contains(l.CubeGrid)); GridTerminalSystem.GetBlocksOfType<IMyRefinery>(c, l => aS.Contains(l.CubeGrid));
            foreach (IMyFunctionalBlock blk in b.Concat(c))
            {
                e.Clear(); blk.GetInventory(0).GetItems(e); if (e.Count > 0 & blk.Enabled)
                {
                    f = "" + e[0].Type.
TypeId; f = f.Substring(f.LastIndexOf('_') + 1).ToUpper(); g = e[0].Type.SubtypeId.ToUpper(); if (ax.ContainsKey(f) & aB.ContainsKey(g)) aC[f][
g].producers.Add(blk); if (f == "ORE" & (ORE_PRODUCT.TryGetValue(g, out h) ? h : h = g) != "" & aC["INGOT"].ContainsKey(h)) aC["INGOT"][h].producers
.Add(blk); b6[blk] = new bJ(new ak(f, g), (double)e[0].Amount);
                }
            }
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(b, l => aS.Contains(l.
CubeGrid)); foreach (IMyAssembler blk in b)
            {
                if (blk.Enabled & !blk.IsQueueEmpty & blk.Mode == MyAssemblerMode.Assembly)
                {
                    blk.GetQueue(j); if (
aD.TryGetValue(j[0].BlueprintId, out k))
                    {
                        if (ax.ContainsKey(k.type) & aB.ContainsKey(k.subType)) aC[k.type][k.subType].producers.Add(
blk); b6[blk] = new bJ(k, (double)j[0].Amount - blk.CurrentProgress);
                    }
                }
            }
        }
        void bE()
        {
            if (!ax.ContainsKey("ORE") | !ax.ContainsKey("INGOT"))
                return; bool b = aZ.Contains("refineries"); string c, e, f, g, h; bK j; int k, l; List<string> m = new List<string>(); Dictionary<string, int> n = new
                                Dictionary<string, int>(); List<MyInventoryItem> o = new List<MyInventoryItem>(); double p, q; bJ r; bool t; List<IMyRefinery> u = new List<
                                          IMyRefinery>(); if (b) a_.Add("Refinery management:"); foreach (string isubOre in ax["ORE"])
            {
                if (!ORE_PRODUCT.TryGetValue(isubOre, out h)
) h = isubOre; if (h != "" & aC["ORE"][isubOre].avail > 0L & aC["INGOT"].TryGetValue(h, out j))
                {
                    if (j.quota > 0L)
                    {
                        k = (int)(100L * j.amount / j.quota); m.
Add(isubOre); n[isubOre] = k; if (b) a_.Add("  " + aA[h] + " @ " + j.amount / 1e6 + "/" + j.quota / 1e6 + "," + (isubOre == h ? "" : " Ore/" + aA[isubOre]) + " L=" +
k + "%");
                    }
                }
            }
            foreach (IMyRefinery rfn in b4.Keys)
            {
                c = f = g = ""; o.Clear(); rfn.GetInventory(0).GetItems(o); if (o.Count > 0)
                {
                    c = "" + o[0].Type.
TypeId; c = c.Substring(c.LastIndexOf('_') + 1).ToUpper(); f = o[0].Type.SubtypeId.ToUpper(); if (c == "ORE" & n.ContainsKey(f)) n[f] += Math.Max(1
, n[f] / b4.Count); if (o.Count > 1)
                    {
                        e = "" + o[1].Type.TypeId; e = e.Substring(e.LastIndexOf('_') + 1).ToUpper(); g = o[1].Type.SubtypeId.ToUpper();
                        if (e == "ORE" & n.ContainsKey(g)) n[g] += Math.Max(1, n[g] / b4.Count); bt(rfn, 0, e, g, -2, (long)((double)o[1].Amount * 1e6 + 0.5));
                    }
                }
                if (b6.
TryGetValue(rfn, out r))
                {
                    j = aC[r.item.type][r.item.subType]; q = j.prdSpeed.TryGetValue("" + rfn.BlockDefinition, out q) ? q : 1.0; p = r.item.
         subType == f ? Math.Max(r.quantity - (double)o[0].Amount, 0.0) : Math.Max(r.quantity, q); p = Math.Min(Math.Max((p + q) / 2.0, 0.2), 10000.0); j.
                             prdSpeed["" + rfn.BlockDefinition] = p; if (b & (int)(q + 0.5) != (int)(p + 0.5)) a_.Add("  Update " + rfn.BlockDefinition.SubtypeName + ":" + aA[r.
                                            item.subType] + " refine speed: " + (int)(q + 0.5) + " -> " + (int)(p + 0.5) + "kg/cycle");
                }
                if (b4[rfn].Count > 0) b4[rfn].IntersectWith(n.Keys);
                else b4[rfn].UnionWith(n.Keys); t = b4[rfn].Count > 0; if (o.Count > 0)
                {
                    p = c == "ORE" ? aC["ORE"][f].prdSpeed.TryGetValue("" + rfn.BlockDefinition
, out p) ? p : 1.0 : 1e6; bt(rfn, 0, c, f, -1, (long)Math.Min((double)o[0].Amount * 1e6 + 0.5, 10 * p * 1e6 + 0.5)); t = t & c == "ORE" & (double)o[0].Amount < 2.5 * p
& o.Count == 1;
                }
                if (t) u.Add(rfn); if (b) a_.Add("  " + rfn.CustomName + (o.Count < 1 ? " idle" : " refining " + (int)o[0].Amount + "kg " + (f == "" ?
        "unknown" : aA[f] + (!n.ContainsKey(f) ? "" : " (L=" + n[f] + "%)")) + (o.Count < 2 ? "" : ", then " + (int)o[1].Amount + "kg " + (g == "" ? "unknown" : aA[g] + (!n
        .ContainsKey(g) ? "" : " (L=" + n[g] + "%)")))) + "; " + (n.Count == 0 ? "nothing to do" : t ? "ready" : b4[rfn].Count == 0 ? "restricted" : "busy"));
            }
            if (m.
Count > 0 & u.Count > 0)
            {
                m.Sort((v, w) => {
                    string x, y; if (!ORE_PRODUCT.TryGetValue(v, out x)) x = v; if (!ORE_PRODUCT.TryGetValue(w, out y)) y = w;
                    return -1 * aC["INGOT"][x].quota.CompareTo(aC["INGOT"][y].quota);
                }); u.Sort((z, C) => b4[z].Count.CompareTo(b4[C].Count)); foreach (
IMyRefinery rfn in u)
                {
                    f = ""; k = int.MaxValue; foreach (string isubOre in m)
                    {
                        if ((f == "" | n[isubOre] < k) & b4[rfn].Contains(isubOre))
                        {
                            f =
isubOre; k = n[f];
                        }
                    }
                    if (f != "")
                    {
                        aK++; rfn.UseConveyorSystem = false; l = rfn.GetInventory(0).IsItemAt(0) ? -4 : -3; p = aC["ORE"][f].prdSpeed.
        TryGetValue("" + rfn.BlockDefinition, out p) ? p : 1.0; bt(rfn, 0, "ORE", f, l, (long)(10 * p * 1e6 + 0.5)); n[f] += Math.Min(Math.Max((int)(n[f] * 0.41),
                            1), 100 / b4.Count); if (b) a_.Add("  " + rfn.CustomName + " assigned " + (int)(10 * p + 0.5) + "kg " + aA[f] + " (L=" + n[f] + "%)");
                    }
                    else if (b) a_.Add("  "
+ rfn.CustomName + " unassigned, nothing to do");
                }
            }
            for (l = -1; l >= -4; l--)
            {
                if (aT.ContainsKey(l))
                {
                    foreach (string isubOre in aT[l]["ORE"].
Keys) bB(true, l, "ORE", isubOre);
                }
            }
        }
        void bF()
        {
            if (!ax.ContainsKey("INGOT")) return; bool b = aZ.Contains("assemblers"); long c; int e, f; bK g
, h; ak j, k; List<ak> l; Dictionary<ak, int> m = new Dictionary<ak, int>(), n = new Dictionary<ak, int>(); List<MyProductionItem> o = new List<
MyProductionItem>(); double p, q; bJ r; bool t, u; List<IMyAssembler> v = new List<IMyAssembler>(); if (b) a_.Add("Assembler management:"); ay.
       TryGetValue("COMPONENT", out c); f = 90 + (int)(10 * aC["INGOT"].Values.Min(w => w.subType != "URANIUM" & (w.minimum > 0L | w.ratio > 0.0f) ? w.amount /
               Math.Max(w.minimum, 17.5 * w.ratio * c) : 2.0)); if (b) a_.Add("  Component par L=" + f + "%"); foreach (string itype in av)
            {
                if (itype != "ORE" & itype
!= "INGOT")
                {
                    foreach (string isub in ax[itype])
                    {
                        g = aC[itype][isub]; g.hold = Math.Max(0, g.hold - 1); j = new ak(itype, isub); n[j] = itype ==
"COMPONENT" & g.ratio > 0.0f ? f : 100; e = (int)(100L * g.amount / Math.Max(1L, g.quota)); if (g.quota > 0L & e < n[j] & g.blueprint != default(
         MyDefinitionId))
                        {
                            if (g.hold == 0) m[j] = e; if (b) a_.Add("  " + aw[itype] + "/" + aA[isub] + (g.hold > 0 ? "" : " @ " + g.amount / 1e6 + "/" + g.quota / 1e6 +
                    ", L=" + e + "%") + (g.hold > 0 | g.jam > 0 ? "; HOLD " + g.hold + "/" + 10 * g.jam : ""));
                        }
                    }
                }
            }
            foreach (IMyAssembler asm in b5.Keys)
            {
                t = u = false; g = h = null; j = k
= new ak("", ""); if (!asm.IsQueueEmpty)
                {
                    asm.GetQueue(o); g = aD.TryGetValue(o[0].BlueprintId, out j) ? aC[j.type][j.subType] : null; if (g !=
null & m.ContainsKey(j)) m[j] += Math.Max(1, (int)(1e8 * (double)o[0].Amount / g.quota + 0.5)); if (o.Count > 1 && aD.TryGetValue(o[1].BlueprintId,
     out k) & m.ContainsKey(k)) m[k] += Math.Max(1, (int)(1e8 * (double)o[1].Amount / aC[k.type][k.subType].quota + 0.5));
                }
                if (b6.TryGetValue(asm,
out r))
                {
                    h = aC[r.item.type][r.item.subType]; q = h.prdSpeed.TryGetValue("" + asm.BlockDefinition, out q) ? q : 1.0; if (r.item.type != j.type | r.
                         item.subType != j.subType) { p = Math.Max(q, (asm.IsQueueEmpty ? 2 : 1) * r.quantity); b7.Remove(asm); }
                    else if (asm.IsProducing)
                    {
                        p = r.quantity - (
double)o[0].Amount + asm.CurrentProgress; b7.Remove(asm);
                    }
                    else
                    {
                        p = Math.Max(q, r.quantity - (double)o[0].Amount + asm.CurrentProgress); if ((
b7[asm] = (b7.TryGetValue(asm, out e) ? e : 0) + 1) >= 3)
                        {
                            a_.Add("  " + asm.CustomName + " is jammed by " + aA[j.subType]); b7.Remove(asm); asm.
ClearQueue(); h.hold = 10 * (h.jam < 1 | h.hold < 1 ? h.jam = Math.Min(10, h.jam + 1) : h.jam); u = true;
                        }
                    }
                    p = Math.Min(Math.Max((p + q) / 2.0, Math.Max(0.2, 0.5
* q)), Math.Min(1000.0, 2.0 * q)); h.prdSpeed["" + asm.BlockDefinition] = p; if (b & (int)(q + 0.5) != (int)(p + 0.5)) a_.Add("  Update " + asm.
BlockDefinition.SubtypeName + ":" + aw[r.item.type] + "/" + aA[r.item.subType] + " assemble speed: " + (int)(q * 100) / 100.0 + " -> " + (int)(p * 100) /
100.0 + "/cycle");
                }
                if (b5[asm].Count == 0) b5[asm].UnionWith(m.Keys); else b5[asm].IntersectWith(m.Keys); p = g != null && g.prdSpeed.
     TryGetValue("" + asm.BlockDefinition, out p) ? p : 1.0; if (!u & (asm.IsQueueEmpty || (double)o[0].Amount - asm.CurrentProgress < 2.5 * p & o.Count == 1 &
             asm.Mode == MyAssemblerMode.Assembly)) { if (h != null) h.jam = Math.Max(0, h.jam - (h.hold < 1 ? 1 : 0)); if (t = b5[asm].Count > 0) v.Add(asm); }
                if (b) a_.
Add("  " + asm.CustomName + (asm.IsQueueEmpty ? " idle" : (asm.Mode == MyAssemblerMode.Assembly ? " making " : " breaking ") + o[0].Amount + "x " + (j
.type == "" ? "unknown" : aA[j.subType] + (!m.ContainsKey(j) ? "" : " (L=" + m[j] + "%)")) + (o.Count <= 1 ? "" : ", then " + o[1].Amount + "x " + (k.type == "" ?
"unknown" : aA[k.subType] + (!m.ContainsKey(k) ? "" : " (L=" + m[k] + "%)")))) + "; " + (m.Count == 0 ? "nothing to do" : t ? "ready" : b5[asm].Count == 0 ?
"restricted" : "busy"));
            }
            if (m.Count > 0 & v.Count > 0)
            {
                l = new List<ak>(m.Keys); l.Sort((x, y) => -1 * aC[x.type][x.subType].quota.CompareTo(aC[y.
type][y.subType].quota)); v.Sort((z, C) => b5[z].Count.CompareTo(b5[C].Count)); foreach (IMyAssembler asm in v)
                {
                    j = new ak("", ""); e = int.
MaxValue; foreach (ak i in l) { if (m[i] < Math.Min(e, n[i]) & b5[asm].Contains(i) & aC[i.type][i.subType].hold < 1) { j = i; e = m[i]; } }
                    if (j.type != "")
                    {
                        aL++; asm.UseConveyorSystem = true; asm.CooperativeMode = false; asm.Repeating = false; asm.Mode = MyAssemblerMode.Assembly; g = aC[j.type][j.
                                    subType]; p = g.prdSpeed.TryGetValue("" + asm.BlockDefinition, out p) ? p : 1.0; f = Math.Max((int)(10 * p), 10); asm.AddQueueItem(g.blueprint, (
                                                     double)f); m[j] += (int)Math.Ceiling(1e8 * f / g.quota); if (b) a_.Add("  " + asm.CustomName + " assigned " + f + "x " + aA[j.subType] + " (L=" + m[j] +
                                                               "%)");
                    }
                    else if (b) a_.Add("  " + asm.CustomName + " unassigned, nothing to do");
                }
            }
        }
        void bG()
        {
            string b, c, e;
            Dictionary<string, List<IMyTextPanel>> f = new Dictionary<string, List<IMyTextPanel>>(); 
            ScreenFormatter g;
            long h, j; 
            foreach (IMyTextPanel panel in aW.Keys)
            {
                b = String.Join("/", aW[panel]);
                if (f.ContainsKey(b)) f[b].Add(panel); 
                else f[b] = new List<IMyTextPanel>() { panel };
            }
            foreach (List<IMyTextPanel> panels in f.Values)
            {
                g = new ScreenFormatter(6);
                g.SetBar(0); 
                g.SetFill(0); 
                g.SetAlign(2, 1);
                g.SetAlign(3, 1); 
                g.SetAlign(4, 1);
                g.SetAlign(5, 1); 
                h = j = 0L;
                foreach (string itype in aW[panels[0]].Count > 0 ? aW[panels[0]] : av)
                {
                    c = " Asm ";
                    e = "Quota"; 
                    if (itype == "INGOT")
                    {
                        c = " Ref ";
                    }
                    else if (itype == "ORE") 
                    {
                        c = " Ref ";
                        e = "Max"; 
                    }
                    if (g.GetNumRows() > 0) g.AddBlankRow();
                    g.Add(0, "");
                    g.Add(1, aw[itype], true);
                    g.Add(2, c, true); 
                    g.Add(3, "Qty", true);
                    g.Add(4, " / ", true); 
                    g.Add(5, e, true); 
                    g.AddBlankRow(); 
                    foreach (bK data in aC[itype].Values)
                    {
                        g.Add(0, data.amount == 0L ? "0.0" : "" + (double)data.amount / data.quota);
                        g.Add(1, data.label, true);
                        b = data.producers.Count > 0 ? data.producers.Count + " " + (data.producers.All(k => !(k is IMyProductionBlock) || (k as IMyProductionBlock).IsProducing) ? " " : "!") : data.hold > 0 ? "-  " : "";
                        g.Add(2, b, true);
                        g.Add(3, data.amount > 0L | data.quota > 0L ? bk(data.amount) : "");
                        g.Add(4, data.quota > 0L ? " / " : "", true);
                        g.Add(5, data.quota > 0L ? bk(data.quota) : ""); 
                        h = Math.Max(h, data.amount); 
                        j = Math.Max(j, data.quota);
                    }
                }
                g.SetWidth(3, ScreenFormatter.GetWidth("8.88" + (h >= 1000000000000L ? " M" : h >= 1000000000L ? " K" : ""), true));
                g.SetWidth(5, ScreenFormatter.GetWidth("8.88" + (j >= 1000000000000L ? " M" : j >= 1000000000L ? " K" : ""), true));
                foreach (IMyTextPanel panel in panels) bI("TIM Inventory", g, panel);
            }
        }
        void bH()
        {
            long b; 
            StringBuilder c;
            if (aX.Count > 0)
            {
                c = new StringBuilder();
                c.Append(aE); 
                for (b = Math.Max(1, aI - aF.Length + 1); b <= aI; b++) c.Append(aF[b % aF.Length]);
                foreach (IMyTextPanel panel in aX)
                {
                    panel.WritePublicTitle("Script Status");
                    if (b8.ContainsKey(panel)) a_.Add("Status panels cannot be spanned");
                    panel.WriteText(c.ToString()); 
                    ((IMyTextSurface)panel).ContentType = ContentType.TEXT_AND_IMAGE;
                }
            }
            if (aY.Count > 0)
            {
                foreach (IMyTerminalBlock blockFrom in b9.Keys)
                {
                    foreach (IMyTerminalBlock blockTo in b9[blockFrom]) a_.Add("No conveyor connection from " + blockFrom.CustomName + " to " + blockTo.CustomName);
                }
                foreach (IMyTextPanel panel in aY)
                {
                    panel.WritePublicTitle("Script Debugging");
                    if (b8.ContainsKey(panel)) a_.Add("Debug panels cannot be spanned");
                    panel.WriteText(String.Join("\n", a_));
                    ((IMyTextSurface)panel).ContentType = ContentType.TEXT_AND_IMAGE;
                }
            }
            b9.Clear();
        }
        void bI(string b, ScreenFormatter c, IMyTextPanel e, bool f = true, string g = "", string h = "")
        {
            int j, k, l, m, n,o, p;
            int q, r, t; 
            float u; 
            string[][] v; 
            string w; 
            Matrix x;
            IMySlimBlock y;
            IMyTextPanel z;
            m = e.BlockDefinition.SubtypeName.EndsWith("Wide")? 2 : 1; 
            n = e.BlockDefinition.SubtypeName.StartsWith("Small") ? 3 : 1;
            j = k = 1;
            if (f & b8.ContainsKey(e)) 
            {
                j = b8[e].A; 
                k = b8[e].B; 
            }
            q = c.GetMinWidth();
            q = q / j + (q % j > 0 ? 1 : 0);
            r = c.GetNumRows();
            r = r / k + (r % k > 0 ? 1 : 0);
            o = 646 * m; 
            u = e.GetValueFloat("FontSize");
            if (u < 0.25f) u = 1.0f; 
            if (q > 0) u = Math.Min(u, Math.Max(0.5f, o * 100 / q / 100.0f));
            if (r > 0) u = Math.Min(u, Math.Max(0.5f, 1760 / r / 100.0f));
            o = (int)(o / u);
            p = (int)(17.6f / u);
            if (j > 1 | k > 1)
            {
                v = c.ToSpan(o, j);
                e.Orientation.GetMatrix(out x); 
                for (q = 0; q < j; q++)
                {
                    t = 0; 
                    for (r = 0; r < k; r++)
                    {
                        y = e.CubeGrid.GetCubeBlock(new Vector3I(e.Position + q * m * n * x.Right + r * n * x.Down));
                        if (y != null && y.FatBlock is IMyTextPanel && "" + y.FatBlock.BlockDefinition == "" + e.BlockDefinition)
                        {
                            z = y.FatBlock as IMyTextPanel;
                            l = Math.Max(0, v[q].Length - t);
                            if (r + 1 < k) l = Math.Min(l, p);
                            w = "";
                            if (t < v[q].Length) w = String.Join("\n", v[q], t, l);
                            if (q == 0) w += r == 0 ? g : r + 1 == k ? h : "";
                            z.SetValueFloat("FontSize", u);
                            z.WritePublicTitle(b + " (" + (q + 1) + "," + (r + 1) + ")"); 
                            z.WriteText(w);
                            ((IMyTextSurface)z).ContentType = ContentType.TEXT_AND_IMAGE;
                        }
                        t += p;
                    }
                }
            }
            else
            {
                e.SetValueFloat("FontSize", u);
                e.WritePublicTitle(b);
                e.WriteText(g + c.ToString(o) + h);
                ((IMyTextSurface)e).ContentType = ContentType.TEXT_AND_IMAGE;
            }
        }
        public class ScreenFormatter
        {
            private static Dictionary<char, byte> U = new Dictionary<char, byte>();
            private static Dictionary<string, int> V = new Dictionary<string, int>();
            private static byte W;
            private static byte X; 
            public static int GetWidth(string b, bool c = false)
            {
                int e; 
                if (!V.TryGetValue(b, out e))
                {
                    Dictionary<char, byte> f = U;
                    string g = b + "\0\0\0\0\0\0\0";
                    int h = g.Length - g.Length % 8;
                    byte j, k, l, m, n, o, p, q; 
                    while (h > 0)
                    {
                        f.TryGetValue(g[h - 1], out j); 
                        f.TryGetValue(g[h - 2], out k); 
                        f.TryGetValue(g[h - 3], out l);
                        f.TryGetValue(g[h - 4], out m);
                        f.TryGetValue(g[h - 5], out n); 
                        f.TryGetValue(g[h - 6], out o); 
                        f.TryGetValue(g[h - 7], out p);
                        f.TryGetValue(g[h - 8], out q);
                        e += j + k + l + m + n + o + p + q; 
                        h -= 8;
                    }
                    if (c) V[b] = e;
                }
                return e;
            }
            public static string Format(string b, int c, out int e, int f = -1, bool g = false)
            {
                int h, j;
                e = c - GetWidth(b, g);
                if (e <= W / 2) return b;
                h = e / W; 
                j = 0;
                e -= h * W;
                if (2 * e <= W + h * (X - W)) 
                {
                    j = Math.Min(h, (int)((float)e / (X - W) + 0.4999f)); 
                    h -= j; 
                    e -= j * (X - W);
                }
                else if (e > W / 2)
                {
                    h++;
                    e -= W; 
                }
                if (f > 0) return new String(' ', h) + new String('\u00AD', j) + b;
                if (f < 0) return b + new String('\u00AD', j) + new String(' ', h); 
                if (h % 2 > 0 & j % 2 == 0) return new String(' ', h / 2) + new String('\u00AD', j / 2) + b + new String('\u00AD', j / 2) + new String(' ', h - h / 2);
                return new String(' ', h - h / 2) + new String('\u00AD', j / 2) + b + new String('\u00AD', j - j / 2) + new String(' ', h / 2);
            }
            public static string Format(double b, int c, out int e)
            {
                int f, g;
                b = Math.Min(Math.Max(b, 0.0f), 1.0f); 
                f = c / W; 
                g = (int)(f * b + 0.5f);
                e = c - f * W; 
                return new String('I', g) + new String(' ', f - g);
            }
            public static void Init()
            {
                Y(0, "\u2028\u2029\u202F");
                Y(7, "'|\u00A6\u02C9\u2018\u2019\u201A");
                Y(8, "\u0458"); 
                Y(9, " !I`ijl\u00A0\u00A1\u00A8\u00AF\u00B4\u00B8\u00CC\u00CD\u00CE\u00CF\u00EC\u00ED\u00EE\u00EF\u0128\u0129\u012A\u012B\u012E\u012F\u0130\u0131\u0135\u013A\u013C\u013E\u0142\u02C6\u02C7\u02D8\u02D9\u02DA\u02DB\u02DC\u02DD\u0406\u0407\u0456\u0457\u2039\u203A\u2219");
                Y(10, "(),.1:;[]ft{}\u00B7\u0163\u0165\u0167\u021B"); 
                Y(11, "\"-r\u00AA\u00AD\u00BA\u0140\u0155\u0157\u0159"); 
                Y(12, "*\u00B2\u00B3\u00B9");
                Y(13, "\\\u00B0\u201C\u201D\u201E");
                Y(14, "\u0491");
                Y(15, "/\u0133\u0442\u044D\u0454");
                Y(16, "L_vx\u00AB\u00BB\u0139\u013B\u013D\u013F\u0141\u0413\u0433\u0437\u043B\u0445\u0447\u0490\u2013\u2022");
                Y(17, "7?Jcz\u00A2\u00BF\u00E7\u0107\u0109\u010B\u010D\u0134\u017A\u017C\u017E\u0403\u0408\u0427\u0430\u0432\u0438\u0439\u043D\u043E\u043F\u0441\u044A\u044C\u0453\u0455\u045C");
                Y(18, "3FKTabdeghknopqsuy\u00A3\u00B5\u00DD\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E8\u00E9\u00EA\u00EB\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF\u00FF\u0101\u0103\u0105\u010F\u0111\u0113\u0115\u0117\u0119\u011B\u011D\u011F\u0121\u0123\u0125\u0127\u0136\u0137\u0144\u0146\u0148\u0149\u014D\u014F\u0151\u015B\u015D\u015F\u0161\u0162\u0164\u0166\u0169\u016B\u016D\u016F\u0171\u0173\u0176\u0177\u0178\u0219\u021A\u040E\u0417\u041A\u041B\u0431\u0434\u0435\u043A\u0440\u0443\u0446\u044F\u0451\u0452\u045B\u045E\u045F"); 
                Y(19, "+<=>E^~\u00AC\u00B1\u00B6\u00C8\u00C9\u00CA\u00CB\u00D7\u00F7\u0112\u0114\u0116\u0118\u011A\u0404\u040F\u0415\u041D\u042D\u2212");
                Y(20, "#0245689CXZ\u00A4\u00A5\u00C7\u00DF\u0106\u0108\u010A\u010C\u0179\u017B\u017D\u0192\u0401\u040C\u0410\u0411\u0412\u0414\u0418\u0419\u041F\u0420\u0421\u0422\u0423\u0425\u042C\u20AC"); 
                Y(21, "$&GHPUVY\u00A7\u00D9\u00DA\u00DB\u00DC\u00DE\u0100\u011C\u011E\u0120\u0122\u0124\u0126\u0168\u016A\u016C\u016E\u0170\u0172\u041E\u0424\u0426\u042A\u042F\u0436\u044B\u2020\u2021");
                Y(22, "ABDNOQRS\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D8\u0102\u0104\u010E\u0110\u0143\u0145\u0147\u014C\u014E\u0150\u0154\u0156\u0158\u015A\u015C\u015E\u0160\u0218\u0405\u040A\u0416\u0444");
                Y(23, "\u0459"); 
                Y(24, "\u044E");
                Y(25, "%\u0132\u042B");
                Y(26, "@\u00A9\u00AE\u043C\u0448\u045A"); 
                Y(27, "M\u041C\u0428");
                Y(28, "mw\u00BC\u0175\u042E\u0449");
                Y(29, "\u00BE\u00E6\u0153\u0409");
                Y(30, "\u00BD\u0429");
                Y(31, "\u2122");
                Y(32, "W\u00C6\u0152\u0174\u2014\u2026\u2030");
                W = U[' '];
                X = U['\u00AD'];
            }
            private static void Y(byte b, string c)
            {
                Dictionary<char, byte> e = U;
                string f = c + "\0\0\0\0\0\0\0";
                byte g = Math.Max((byte)0, b);
                int h = f.Length - f.Length % 8; 
                while (h > 0)
                {
                    e[f[--h]] = g; 
                    e[f[--h]] = g;
                    e[f[--h]] = g; 
                    e[f[--h]] = g; 
                    e[f[--h]] = g;
                    e[f[--h]] = g; 
                    e[f[--h]] = g; 
                    e[f[--h]] = g;
                }
                e['\0'] = 0;
            }
            private int Z;
            private int _;
            private int a0;
            private List<string>[] a1;
            private List<int>[] a2; 
            private int[] a3;
            private int[] a4; 
            private bool[] a5;
            private int[] a6; 
            public ScreenFormatter(int b, int c = 1)
            {
                this.Z = b; _ = 0; 
                this.a0 = c; 
                a1 = new List<string>[b];
                a2 = new List<int>[b]; 
                a3 = new int[b]; 
                a4 = new int[b]; 
                a5 = new bool[b];
                a6 = new int[b]; 
                for (int e = 0; e < b; e++) 
                {
                    a1[e] = new List<string>();
                    a2[e] = new List<int>(); 
                    a3[e] = -1;
                    a4[e] = 0;
                    a5[e] = false;
                    a6[e] = 0; 
                }
            }
            public void Add(int b, string c, bool e = false)
            {
                int f = 0; 
                a1[b].Add(c);
                if (a5[b] == false) 
                {
                    f = GetWidth(c, e); 
                    a6[b] = Math.Max(a6[b], f);
                }
                a2[b].Add(f); _ = Math.Max(_, a1[b].Count);
            }
            public void AddBlankRow() 
            {
                for (int b = 0; b < Z; b++) 
                {
                    a1[b].Add("");
                    a2[b].Add(0); 
                }
                _++; 
            }
            public int GetNumRows()
            {
                return _;
            }
            public int GetMinWidth() 
            {
                int b = a0 * W;
                for (int c = 0; c < Z; c++) b += a0 * W + a6[c]; 
                return b; 
            }
            public void SetAlign(int b, int c) 
            {
                a3[b] = c; 
            }
            public void SetFill(int b, int c = 1)
            {
                a4[b] = c; 
            }
            public void SetBar(int b, bool c = true)
            {
                a5[b] = c; 
            }
            public void SetWidth(int b, int c) 
            {
                a6[b] = c; 
            }
            public string[][] ToSpan(int b = 0, int c = 1)
            {
                int e, f, g, h, j, k, l, m;
                int[] n; 
                byte o;
                double p;
                string q;
                StringBuilder r; 
                string[][] t; 
                n = (int[])this.a6.Clone();
                l = b * c - a0 * W;
                m = 0;
                for (e = 0; e < Z; e++)
                {
                    l -= a0 * W;
                    if (a4[e] == 0) l -= n[e];
                    m += a4[e];
                }
                for (e = 0; e < Z & m > 0; e++)
                {
                    if (a4[e] > 0)
                    {
                        n[e] = Math.Max(n[e], a4[e] * l / m);
                        l -= n[e];
                        m -= a4[e];
                    }
                }
                t = new string[c][];
                for (g = 0; g < c; g++) t[g] = new string[_];
                c--; h = 0;
                r = new StringBuilder();
                for (f = 0; f < _; f++)
                {
                    r.Clear(); 
                    g = 0; 
                    m = b;
                    l = 0;
                    for (e = 0; e < Z; e++)
                    {
                        l += a0 * W;
                        if (f >= a1[e].Count || a1[e][f] == "")
                        {
                            l += n[e]; 
                        }
                        else
                        {
                            q = a1[e][f];
                            U.TryGetValue(q[0], out o);
                            k = a2[e][f]; 
                            if (a5[e]) 
                            {
                                if (double.TryParse(q, out p)) p = Math.Min(Math.Max(p, 0.0), 1.0);
                                h = (int)(n[e] / W * p + 0.5); 
                                o = W; 
                                k = h * W;
                            }
                            if (a3[e] > 0)
                            {
                                l += n[e] - k;
                            }
                            else if (a3[e] == 0) 
                            {
                                l += (n[e] - k) / 2;
                            } 
                            while (g < c & l > m - o)
                            {
                                r.Append(' ');
                                t[g][f] = r.ToString(); 
                                r.Clear();
                                g++;
                                l -= m; 
                                m = b;
                            }
                            m -= l; 
                            r.Append(Format("", l, out l)); 
                            m += l;
                            if (a3[e] < 0)
                            {
                                l += n[e] - k;
                            } 
                            else if (a3[e] == 0) 
                            {
                                l += n[e] - k - (n[e] - k) / 2;
                            }
                            if (a5[e])
                            {
                                while (g < c & k > m)
                                {
                                    j = m / W; 
                                    m -= j * W;
                                    k -= j * W; 
                                    r.Append(new String('I', j));
                                    t[g][f] = r.ToString();
                                    r.Clear(); 
                                    g++;
                                    l -= m;
                                    m = b;
                                    h -= j;
                                }
                                q = new String('I', h);
                            }
                            else
                            {
                                while (g < c & k > m)
                                {
                                    h = 0; 
                                    while (m >= o) 
                                    {
                                        m -= o; 
                                        k -= o;
                                        U.TryGetValue(q[++h], out o);
                                    }
                                    r.Append(q, 0, h);
                                    t[g][f] = r.ToString();
                                    r.Clear();
                                    g++; 
                                    l -= m;
                                    m = b;
                                    q = q.Substring(h);
                                }
                            }
                            m -= k; r.Append(q);
                        }
                    }
                    t[g][f] = r.ToString();
                }
                return t;
            }
            public string ToString(int b = 0) 
            {
                return String.Join("\n", ToSpan(b)[0]);
            }
        }
        //------------END--------------
    }
}