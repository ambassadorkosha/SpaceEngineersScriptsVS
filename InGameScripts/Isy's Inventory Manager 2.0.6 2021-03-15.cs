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


namespace IsysInventoryManager
{
	public sealed class Program : MyGridProgram
	{
		//------------BEGIN--------------

		// Isy's Inventory Manager
		// ===================
		// Version: 2.8.6
		// Date: 2021-03-15

		// Guide: http://steamcommunity.com/sharedfiles/filedetails/?id=1226261795

		//  =======================================================================================
		//                                                                            --- Configuration ---
		//  =======================================================================================

		// --- Sorting ---
		// =======================================================================================

		// Define the keyword a cargo container has to contain in order to be recognized as a container of the given type.
		const string oreContainerKeyword = "Ores";
		const string ingotContainerKeyword = "Ingots";
		const string componentContainerKeyword = "Components";
		const string toolContainerKeyword = "Tools";
		const string ammoContainerKeyword = "Ammo";
		const string bottleContainerKeyword = "Bottles";

		// Keyword a block name has to contain to be skipped by the sorting (= no items will be taken out).
		// This list is expandable - just separate the entries with a ",". But it's also language specific, so adjust it if needed.
		// Default: string[] lockedContainerKeywords = { "Locked", "Seat", "Control Station" };
		string[] lockedContainerKeywords = { "Locked", "Seat", "Control Station", "Tools and Box Packing Assembler" };

		// Keyword a block name has to contain to be excluded from item counting (used by autocrafting and inventory panels)
		// This list is expandable - just separate the entries with a ",". But it's also language specific, so adjust it if needed.
		// Default: string[] hiddenContainerKeywords = { "Hidden" };
		string[] hiddenContainerKeywords = { "Hidden" };

		// Keyword for connectors to disable sorting of a grid, that is docked to that connector.
		// This also disables the usage of refineries, arc furnaces and assemblers on that grid.
		// Special containers, reactors and O2/H2 generators will still be filled.
		string noSortingKeyword = "[No Sorting]";

		// Keyword for connectors to disable IIM completely for a ship, that is docked to that connector.
		string noIIMKeyword = "[No IIM]";

		// Balance items between containers of the same type? This will result in an equal amount of all items in all containers of that type.
		bool balanceTypeContainers = false;

		// Show a fill level in the container's name?
		bool showFillLevel = true;

		// Fill bottles before storing them in the bottle container?
		bool fillBottles = true;


		// --- Automated container assignment ---
		// =======================================================================================

		// Master switch. If this is set to false, automated container un-/assignment is disabled entirely.
		bool autoContainerAssignment = false;

		// Assign new containers if a type is full or not present?
		bool assignNewContainers = false;

		// Unassign empty type containers that aren't needed anymore (at least one of each type always remains).
		// This doesn't touch containers with manual priority tokens, like [P1].
		bool unassignEmptyContainers = false;

		// Assign ores and ingots containers as one?
		bool oresIngotsInOne = true;

		// Assign tool, ammo and bottle containers as one?
		bool toolsAmmoBottlesInOne = true;


		// --- Autocrafting ---
		// =======================================================================================

		// Enable autocrafting or autodisassembling (disassembling will disassemble everything above the wanted amounts)
		// All assemblers will be used. To use one manually, add the manualMachineKeyword to it (by default: "!manual")
		bool enableAutocrafting = true;
		bool enableAutodisassembling = false;

		// A LCD with the keyword "Autocrafting" is required where you can set the wanted amount!
		// This has multi LCD support. Just append numbers after the keyword, like: "LCD Autocrafting 1", "LCD Autocrafting 2", ..
		string autocraftingKeyword = "Autocrafting";

		// If you want an assembler to only assemble or only disassemble, use the following keywords in its name.
		// A assembler without a keyword will do both tasks
		string assembleKeyword = "!assemble-only";
		string disassembleKeyword = "!disassemble-only";

		// You can teach the script new crafting recipes, by adding one of the following tags to an assembler's name.
		// This is needed if the autocrafting screen shows [NoBP] for an item. There are two tag options to teach new blueprints:
		// !learn will learn one item and then remove the tag so that the assembler is part of the autocrafting again.
		// !learnMany will learn everything you queue in it and will never be part of the autorafting again until you remove the tag.
		// To learn an item, queue it up about 100 times (Shift+Klick) and wait until the script removes it from the queue.
		string learnKeyword = "!learn";
		string learnManyKeyword = "!learnMany";

		// Default modifier that gets applied, when a new item is found. Modifiers can be one or more of these:
		// 'A' (Assemble only), 'D' (Disassemble only), 'P' (Always queue first (priority)), 'H' (Hide and manage in background), 'I' (Hide and ignore)
		string defaultModifier = "";

		// Margins for assembling or disassembling items in percent based on the wanted amount (default: 0 = exact value).
		// Examples:
		// assembleMargin = 5 with a wanted amount of 100 items will only produce new items, if less than 95 are available.
		// disassembleMargin = 10 with a wanted amount of 1000 items will only disassemble items if more than 1100 are available.
		double assembleMargin = 0;
		double disassembleMargin = 0;

		// Add the header to every screen when using multiple autocrafting LCDs?
		bool headerOnEveryScreen = false;

		// Show available modifiers help on the last screen?
		bool showAutocraftingModifiers = true;

		// Split assembler tasks (this is like cooperative mode but splits the whole queue between all assemblers equally)
		bool splitAssemblerTasks = true;

		// Sort the assembler queue based on the most needed components?
		bool sortAssemblerQueue = true;

		// Autocraft ingots from stone in survival kits until you have proper refineries?
		bool enableBasicIngotCrafting = false;

		// Disable autocrafting in survival kits when you have regular assemblers?
		bool disableBasicAutocrafting = true;


		// --- Special Loadout Containers ---
		// =======================================================================================

		// Keyword an inventory has to contain to be filled with a special loadout (see in it's custom data after you renamed it!)
		// Special containers will be filled with your wanted amount of items and never be drained by the auto sorting!
		const string specialContainerKeyword = "Special";

		// Are special containers allowed to 'steal' items from other special containers with a lower priority?
		bool allowSpecialSteal = true;


		// --- Refinery handling ---
		// =======================================================================================

		// By enabling ore balancing, the script will balance the ores between all refinieres so that every refinery has the same amount of ore in it.
		// To still use a refinery manually, add the manualMachineKeyword to it (by default: "!manual")
		bool enableOreBalancing = false;

		// Enable script assisted refinery filling? This will move in the most needed ore and will make room, if the refinery is already full
		// Also, the script puts as many ores into the refinery as possible and will pull ores even from other refineries if needed.
		bool enableScriptRefineryFilling = false;

		// Sort the refinery queue based on the most needed ingots?
		bool sortRefiningQueue = false;

		// If you want an ore to always be refined first, simply remove the two // in front of the ore name to enable it.
		// Enabled ores are refined in order from top to bottom so if you removed several // you can change the order by
		// copying and pasting them inside the list. Just be careful to keep the syntax correct: "OreName",
		// By default stone is enabled and will always be refined first.
		List<String> fixedRefiningList = new List<string> {
	"Stone",
	//"Iron",
	//"Nickel",
	//"Cobalt",
	//"Silicon",
	//"Uranium",
	//"Silver",
	//"Gold",
	//"Platinum",
	//"Magnesium",
	//"Scrap",
};


		// --- O2/H2 generator handling ---
		// =======================================================================================

		// Enable balancing of ice in O2/H2 generators?
		// All O2/H2 generators will be used. To use one manually, add the manualMachineKeyword to it (by default: "!manual")
		bool enableIceBalancing = false;

		// Put ice into O2/H2 generators that are turned off? (default: false)
		bool fillOfflineGenerators = false;

		// How much space should be left to fill bottles (aka how many bottles should fit in after it's filled with ice)?
		// WARNING! O2/H2 generators automatically pull ice and bottles if their inventory volume drops below 30%.
		// To avoid this, turn off "Use Conveyor" in the generator's terminal settings.
		int spaceForBottles = 1;


		// --- Reactor handling ---
		// =======================================================================================

		// Enable balancing of uranium in reactors? (Note: conveyors of reactors are turned off to stop them from pulling more)
		// All reactors will be used. To use one manually, add the manualMachineKeyword to it (by default: "!manual")
		bool enableUraniumBalancing = true;

		// Put uranium into reactors that are turned off? (default: false)
		bool fillOfflineReactors = true;

		// Amount of uranium in each reactor? (default: 100 for large grid reactors, 25 for small grid reactors)
		double uraniumAmountLargeGrid = 100;
		double uraniumAmountSmallGrid = 25;


		// --- Assembler Cleanup ---
		// =======================================================================================

		// This cleans up assemblers, if they have no queue and puts the contents back into a cargo container.
		bool enableAssemblerCleanup = true;


		// --- Internal item sorting ---
		// =======================================================================================

		// Sort the items inside all containers?
		// Note, that this could cause inventory desync issues in multiplayer, so that items are invisible
		// or can't be taken out. Use at your own risk!
		bool enableInternalSorting = false;

		// Internal sorting pattern. Always combine one of each category, e.g.: 'Ad' for descending item amount (from highest to lowest)
		// 1. Quantifier:
		// A = amount
		// N = name
		// T = type (alphabetical)
		// X = type (number of items)

		// 2. Direction:
		// a = ascending
		// d = descending

		string sortingPattern = "Na";

		// Internal sorting can also be set per inventory. Just use '(sort:PATTERN)' in the block's name.
		// Example: Small Cargo Container 3 (sort:Ad)
		// Note: Using this method, internal sorting will always be activated for this container, even if the main switch is turned off!


		// --- LCD panels ---
		// =======================================================================================

		// To display the main script informations, add the following keyword to any LCD name (default: IIM-main).
		// You can enable or disable specific informations on the LCD by editing its custom data.
		string mainLCDKeyword = "IIM-main";

		// To display current item amounts of different types, add the following keyword to any LCD name
		// and follow the on screen instructions.
		string inventoryLCDKeyword = "IIM-inventory";

		// To display all current warnings and problems, add the following keyword to any LCD name (default: IIM-warnings).
		string warningsLCDKeyword = "IIM-warnings";

		// To display the script performance (PB terminal output), add the following keyword to any LCD name (default: IIM-performance).
		string performanceLCDKeyword = "IIM-performance";

		// Default screen font, fontsize and padding, when a screen is first initialized. Fonts: "Debug" or "Monospace"
		string defaultFont = "Debug";
		float defaultFontSize = 0.6f;
		float defaultPadding = 0.1f;


		// --- Settings for enthusiasts ---
		// =======================================================================================

		// Extra breaks between script methods in ticks (1 tick = 16.6ms).
		double extraScriptTicks = 0;

		// Use dynamic script speed? The script will slow down automatically if the current runtime exceeds a set value (default: 0.5ms)
		bool useDynamicScriptSpeed = true;
		double maxCurrentMs = 0.5;

		// Exclude welders, grinders or drills from sorting? Set this to true, if you have huge welder or grinder walls!
		bool excludeWelders = false;
		bool excludeGrinders = false;
		bool excludeDrills = false;

		// Enable connection check for inventories (needed for [No Conveyor] info)?
		bool connectionCheck = false;

		// Tag inventories, that have no access to the main type containers with [No Conveyor]?
		// This only works if the above setting connectionCheck is set to true!
		bool showNoConveyorTag = true;

		// Script mode: "ship", "station" or blank for autodetect
		string scriptMode = "station";

		// Protect type containers when docking to another grid running the script?
		bool protectTypeContainers = true;

		// If you want to use a machine manually, append the keyword to it.
		// This works for assemblers, refineries, reactors and O2/H2 generators
		string manualMachineKeyword = "!manual";

		// Enable name correction? This option will automtically correct capitalization, e.g.: iim-main -> IIM-main
		bool enableNameCorrection = true;


		//  =======================================================================================
		//                                                                      --- End of Configuration ---
		//                                                        Don't change anything beyond this point!
		//  =======================================================================================


		List<IMyTerminalBlock> ɗ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ɖ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ɘ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʌ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʊ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʉ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʈ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʇ = new List<IMyTerminalBlock>();
		List<IMyShipConnector> ʆ = new List<IMyShipConnector>();
		List<IMyRefinery> ʅ = new List<IMyRefinery>();
		List<IMyRefinery> ʋ = new List<IMyRefinery>();
		List<IMyRefinery> ʄ = new List<IMyRefinery>();
		List<IMyRefinery> ʂ = new List<IMyRefinery>();
		List<IMyAssembler> ʁ = new List<IMyAssembler>();
		List<IMyAssembler> ʀ = new List<IMyAssembler>();
		List<IMyAssembler> ɿ = new List<IMyAssembler>();
		List<IMyAssembler> ɾ = new List<IMyAssembler>();
		List<IMyGasGenerator> ɽ = new List<IMyGasGenerator>();
		List<IMyGasTank> ʃ = new List<IMyGasTank>();
		List<IMyReactor> ɼ = new List<IMyReactor>();
		List<IMyTextPanel> ʍ = new List<IMyTextPanel>();
		List<string> ʟ = new List<string>();
		HashSet<IMyCubeGrid> ʝ = new HashSet<IMyCubeGrid>();
		HashSet<IMyCubeGrid> ʜ = new HashSet<IMyCubeGrid>();
		List<IMyTerminalBlock> ʛ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʚ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʙ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʘ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʗ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʖ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ʕ = new List<IMyTerminalBlock>();
		string ʔ = "";
		IMyTerminalBlock ʓ;
		IMyInventory ʒ;
		IMyTerminalBlock ʑ;
		bool ʐ = false;
		int ʏ = 0;
		int ʎ = 0;
		int ʞ = 0;
		int ɻ = 0;
		int ɺ = 0;
		int ɧ = 0;
		int ɦ = 0;
		int ɥ = 0;
		int ɤ = 0;
		int ɣ = 0;
		int ɢ = 0;
		int ɨ = 0;
		int ɡ = 0;
		int ɟ = 0;
		string ɞ = "";
		string[] ɝ = { "/", "-", "\\", "|" };
		int ɜ = 0;
		List<IMyTerminalBlock> ɛ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ɚ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ɠ = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> ə = new List<IMyTerminalBlock>();
		StringBuilder ɪ = new StringBuilder();
		string[] Ǒ = {
			"showHeading=true",
			"showWarnings=true",
			"showContainerStats=true",
			"showManagedBlocks=true",
			"showLastAction=true",
			"scrollTextIfNeeded=true"
		};
		string[] ɹ = {
			"showHeading=true",
			"scrollTextIfNeeded=true"
		};
		string Ŀ;
		int ɸ = 0;
		string ɷ = "";
		bool ɶ = false;
		bool ɵ = false;
		bool ɴ = false;
		HashSet<string> ɳ = new HashSet<string>();
		HashSet<string> ɲ = new HashSet<string>();
		int ɱ = 0;
		int ɰ = 0;
		int ɯ = 0;
		bool ɮ = true;
		bool ɭ = false;
		int ɬ = 0;
		string ɫ = "itemID;blueprintID";
		Dictionary<string, string> ɩ = new Dictionary<string, string>()
		{
			{"oreContainer",oreContainerKeyword},
			{"ingotContainer",ingotContainerKeyword},
			{"componentContainer",componentContainerKeyword},
			{"toolContainer",toolContainerKeyword},
			{"ammoContainer",ammoContainerKeyword},
			{"bottleContainer",bottleContainerKeyword},
			{"specialContainer",specialContainerKeyword},
			{"oreBalancing","true"},
			{"iceBalancing","true"},
			{"uraniumBalancing","true"}
		};
		string ɕ = "IIM Autocrafting";
		string Ȼ = "Remove a line to show this item on the LCD again!\n" +
			"Add an amount to manage the item without being on the LCD.\n" +
			"Example: '-SteelPlate=1000'";
		char[] ȗ = { '=', '>', '<' };
		IMyAssembler Ȗ;
		string ȕ = "";
		MyDefinitionId Ȕ;
		HashSet<string> ȓ = new HashSet<string>{
			"Uranium","Silicon","Silver","Gold","Platinum","Magnesium","Iron","Nickel","Cobalt","Stone","Scrap"
		};
		List<MyItemType> Ȓ = new List<MyItemType>();
		List<MyItemType> Ș = new List<MyItemType>();
		Dictionary<string, double> ȑ = new Dictionary<string, double>()
		{
			{"Cobalt",0.3},
			{"Gold",0.01},
			{"Iron",0.7},
			{"Magnesium",0.007},
			{"Nickel",0.4},
			{"Platinum",0.005},
			{"Silicon",0.7},
			{"Silver",0.1},
			{"Stone",0.014},
			{"Uranium",0.01}
		};
		const string ȏ = "MyObjectBuilder_";
		const string Ȏ = "Ore";
		const string ȍ = "Ingot";
		const string Ȍ = "Component";
		const string ȋ = "AmmoMagazine";
		const string Ȋ = "OxygenContainerObject";
		const string Ȑ = "GasContainerObject";
		const string ȉ = "PhysicalGunObject";
		const string Ț = "PhysicalObject";
		const string Ȭ = "ConsumableItem";
		const string Ȫ = "Datapad";
		const string ȩ = ȏ + "BlueprintDefinition/";
		Dictionary<string, float> Ȩ = new Dictionary<string, float>()
		{
			{Ȏ,0.01f},
			{ȍ,0.01f},
			{Ȍ,0.16f},
			{ȋ,0.06f},
			{Ȋ,0.12f},
			{Ȑ,0.12f},
			{ȉ,0.025f},
			{Ț,0.01f},
			{Ȭ,0.012f},
			{Ȫ,0.01f},
		};
		SortedSet<MyDefinitionId> ȧ = new SortedSet<MyDefinitionId>(new Ţ());
		SortedSet<string> Ȧ = new SortedSet<string>();
		SortedSet<string> ȥ = new SortedSet<string>();
		SortedSet<string> Ȥ = new SortedSet<string>();
		SortedSet<string> ȣ = new SortedSet<string>();
		SortedSet<string> Ȣ = new SortedSet<string>();
		SortedSet<string> ȡ = new SortedSet<string>();
		SortedSet<string> Ƞ = new SortedSet<string>();
		SortedSet<string> ȟ = new SortedSet<string>();
		SortedSet<string> Ȟ = new SortedSet<string>();
		SortedSet<string> ȝ = new SortedSet<string>();
		Dictionary<MyDefinitionId, double> Ȝ = new Dictionary<MyDefinitionId, double>();
		Dictionary<MyDefinitionId, double> ț = new Dictionary<MyDefinitionId, double>();
		Dictionary<MyDefinitionId, double> Ȉ = new Dictionary<MyDefinitionId, double>();
		Dictionary<MyDefinitionId, int> Ǽ = new Dictionary<MyDefinitionId, int>();
		Dictionary<MyDefinitionId, MyDefinitionId> Ǯ = new Dictionary<MyDefinitionId, MyDefinitionId>();
		Dictionary<MyDefinitionId, MyDefinitionId> Ǻ = new Dictionary<MyDefinitionId, MyDefinitionId>();
		Dictionary<string, MyDefinitionId> ǹ = new Dictionary<string, MyDefinitionId>();
		Dictionary<string, string> Ǹ = new Dictionary<string, string>();
		bool Ƿ = false;
		string Ƕ = "station_mode;\n";
		string ǵ = "ship_mode;\n";
		string ǻ = "[PROTECTED] ";
		string Ǵ = "";
		string ǲ = "";
		string Ǳ = "";
		DateTime ǰ;
		string[] ǯ = {
			"Get inventory blocks","Find new items","Create item lists","Name correction","Assign containers",
			"Fill special containers","Sort items","Container balancing","Internal sorting",
			"Add fill level to names","Get global item amount","Get assembler queue",
			"Autocrafting","Sort assembler queue","Clean up assemblers","Learn unknown blueprints",
			"Fill refineries","Ore balancing","Ice balancing","Uranium balancing"};
		Program()
		{
			Echo("Script ready to be launched..\n");
			assembleMargin /= 100;
			disassembleMargin /= 100;
			Runtime.UpdateFrequency = UpdateFrequency.Update10;
		}
		void Main(string ǳ)
		{
			if (ɸ >= 10)
			{
				throw new Exception("Too many errors in script step " + ɯ + ":\n" + ǯ[ɯ] +
			"\n\nPlease recompile!\nScript stoppped!\n\nLast error:\n" + ɷ + "\n");
			}
			try
			{
				if (ɮ)
				{
					if (ɯ > 0) Echo("Initializing script.. (" + (ɯ + 1) + "/10) \n");
					if (ɯ >= 2)
					{
						Echo("Getting inventory blocks..");
						if (ɯ == 2) Ʉ();
						if (ʐ) return;
					}
					if (ɯ >= 3)
					{
						Echo("Loading saved items..");
						if (ɯ == 3)
						{
							if (!Ó())
							{
								ɶ = true;
								enableAutocrafting = false;
								enableAutodisassembling = false;
							}
						}
						if (ɶ)
						{
							Echo("-> No assemblers found!");
							Echo("-> Autocrafting deactivated!");
						}
					}
					if (ɯ >= 4)
					{
						Echo("Clearing assembler queues..");
						if (ɯ == 4 && (enableAutocrafting || enableAutodisassembling))
						{
							GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(ʍ, u => u.IsSameConstructAs(Me) && u.CustomName.Contains(autocraftingKeyword));
							if (ʍ.Count > 0)
							{
								foreach (var Ø in ʁ)
								{
									Ø.Mode = MyAssemblerMode.Disassembly;
									Ø.ClearQueue();
									Ø.Mode = MyAssemblerMode.Assembly;
									Ø.ClearQueue();
								}
							}
						}
					}
					if (ɯ >= 5)
					{
						Echo("Checking blueprints..");
						if (ɯ == 5)
						{
							foreach (var M in ȧ)
							{
								Ư(M);
							}
						}
					}
					if (ɯ >= 6)
					{
						Echo("Checking type containers..");
						if (ɯ == 6) ɔ();
						if (ɯ == 6) ɍ();
					}
					if (ɯ >= 7)
					{
						if (scriptMode == "station")
						{ Ƿ = true; }
						else if (Me.CubeGrid.IsStatic && scriptMode != "ship") { Ƿ = true; }
						Echo("Setting script mode to: " + (Ƿ ? "station.." :
"ship..")); if (ɯ == 7) Me.CustomData = (Ƿ ? Ƕ : ǵ) + Me.CustomData.Replace(Ƕ, "").Replace(ǵ, "");
					}
					if (ɯ >= 8) { Echo("Starting script.."); }
					if (ɯ >= 9)
					{ ɯ = 0; ɮ = false; return; }
					ɯ++; return;
				}
				if (ǳ != "") { Ǳ = ǳ; ɯ = 1; ǲ = ""; ǰ = DateTime.Now; }
				if (useDynamicScriptSpeed)
				{
					if (ɱ > 0)
					{
						Ǖ(
"Dynamic script speed control"); É(".."); ɱ--; return;
					}
				}
				if (ɰ < extraScriptTicks) { Runtime.UpdateFrequency = UpdateFrequency.Update1; ɰ++; return; }
				if (ɭ)
				{
					if (ɬ == 0)
						ĥ(); if (ɬ == 1) Ł(); if (ɬ == 2) ņ(); if (ɬ == 3) Ņ(); if (ɬ > 3) ɬ = 0; ɭ = false; return;
				}
				if (ɯ == 0 || ɵ || ʐ)
				{
					if (!ɴ) Ʉ(); if (ʐ) return; ɵ = false; ɴ = false;
					if (!Ǔ(30))
					{
						ɛ = ǒ(mainLCDKeyword, Ǒ, defaultFont, defaultFontSize, defaultPadding); ɚ = ǒ(warningsLCDKeyword, ɹ, defaultFont,
					defaultFontSize, defaultPadding); ɠ = ǒ(performanceLCDKeyword, ɹ, defaultFont, defaultFontSize, defaultPadding); ə = ǒ(inventoryLCDKeyword, null,
							 defaultFont, defaultFontSize, defaultPadding);
					}
					else { ɵ = true; ɴ = true; }
					if (ɯ == 0) { Ǖ(ǯ[ɯ]); É(); ɯ++; }
					return;
				}
				if (!Ƿ) ȁ(); if (Ȇ(Ǳ)) return; ɰ = 0;
				Runtime.UpdateFrequency = UpdateFrequency.Update10; ɭ = true; if (ɯ == 1) { Õ(); }
				if (ɯ == 2) { è(); }
				if (ɯ == 3) { if (enableNameCorrection) ȿ(); }
				if (ɯ
== 4) { if (autoContainerAssignment) { if (unassignEmptyContainers) Ȯ(); if (assignNewContainers) ȴ(); } }
				if (ɯ == 5) { if (ʕ.Count != 0) Α(); }
				if (ɯ == 6) { Ξ(); }
				if (ɯ == 7) { if (balanceTypeContainers) ʷ(); }
				if (ɯ == 8) { Τ(); }
				if (ɯ == 9) { τ(ʉ); τ(ʕ); }
				if (ɯ == 10) { Ʒ(); }
				if (ɯ == 11)
				{
					if (
enableAutocrafting || enableAutodisassembling) Ɵ();
				}
				if (ɯ == 12) { if (enableAutocrafting || enableAutodisassembling) μ(); }
				if (ɯ == 13)
				{
					if (
splitAssemblerTasks) ˬ(); if (sortAssemblerQueue) ͱ();
				}
				if (ɯ == 14)
				{
					if (enableAssemblerCleanup) Ͱ(); if (enableBasicIngotCrafting)
					{
						if (ʅ.Count > 0)
						{
							enableBasicIngotCrafting = false;
						}
						else { ʺ(); }
					}
				}
				if (ɯ == 15) { Ú(); }
				if (ɯ == 16) { ʲ(); }
				if (ɯ == 17)
				{
					if (enableOreBalancing) ʠ(); if (sortRefiningQueue)
					{
						ʩ(ʄ, Ȓ); ʩ(ʂ, Ș
);
					}
				}
				if (ɯ == 18) { if (enableIceBalancing) ǭ(); }
				if (ɯ == 19)
				{
					if (enableUraniumBalancing) { â("uraniumBalancing", "true"); ę(); }
					else if (!
enableUraniumBalancing && ä("uraniumBalancing") == "true") { â("uraniumBalancing", "false"); foreach (IMyReactor ĉ in ɼ) { ĉ.UseConveyorSystem = true; } }
				}
				Ǖ(
ǯ[ɯ]); É(); ɱ = (int)Math.Floor((ǂ > 20 ? 20 : ǂ) / maxCurrentMs); if (ɯ >= 19)
				{
					ɯ = 0; ɳ = new HashSet<string>(ɲ); ɲ.Clear(); if (ɸ > 0) ɸ--; if (ɳ.
Count == 0) Ŀ = null;
				}
				else { ɯ++; }
			}
			catch (NullReferenceException e)
			{
				ɸ++; ɵ = true; ʐ = false; ɷ = e.ToString(); ƹ(
"Execution of script step aborted:\n" + ǯ[ɯ] + " (ID: " + ɯ + ")\n\nCached block not available..");
			}
			catch (Exception e)
			{
				ɸ++; ɵ = true; ʐ = false; ɷ = e.ToString(); ƹ(
"Critical error in script step:\n" + ǯ[ɯ] + " (ID: " + ɯ + ")\n\n" + e);
			}
		}
		bool Ȇ(string ǳ)
		{
			if (ǳ.Contains("pauseThisPB"))
			{
				Echo("Script execution paused!\n"); var ȅ = ǳ.
Split(';'); if (ȅ.Length == 3)
				{
					Echo("Found:"); Echo("'" + ȅ[1] + "'"); Echo("on grid:"); Echo("'" + ȅ[2] + "'"); Echo(
	 "also running the script.\n"); Echo("Type container protection: " + (protectTypeContainers ? "ON" : "OFF") + "\n"); Echo(
			   "Everything else is managed by the other script.");
				}
				return true;
			}
			bool Ȅ = true; bool ȃ = true; bool ȇ = false; if (ǳ != "reset" && ǳ != "msg")
			{
				if (!ǳ.Contains(" on") && !ǳ.Contains(" off")
&& !ǳ.Contains(" toggle")) return false; if (ǳ.Contains(" off")) ȃ = false; if (ǳ.Contains(" toggle")) ȇ = true;
			}
			if (ǳ == "reset")
			{
				Ƹ();
				return true;
			}
			else if (ǳ == "msg") { }
			else if (ǳ.StartsWith("balanceTypeContainers"))
			{
				Ǵ = "Balance type containers"; if (ȇ) ȃ = !
balanceTypeContainers; balanceTypeContainers = ȃ;
			}
			else if (ǳ.StartsWith("showFillLevel"))
			{
				Ǵ = "Show fill level"; if (ȇ) ȃ = !showFillLevel; showFillLevel
= ȃ;
			}
			else if (ǳ.StartsWith("autoContainerAssignment"))
			{
				Ǵ = "Auto assign containers"; if (ȇ) ȃ = !autoContainerAssignment;
				autoContainerAssignment = ȃ;
			}
			else if (ǳ.StartsWith("assignNewContainers"))
			{
				Ǵ = "Assign new containers"; if (ȇ) ȃ = !assignNewContainers;
				assignNewContainers = ȃ;
			}
			else if (ǳ.StartsWith("unassignEmptyContainers"))
			{
				Ǵ = "Unassign empty containers"; if (ȇ) ȃ = !unassignEmptyContainers;
				unassignEmptyContainers = ȃ;
			}
			else if (ǳ.StartsWith("oresIngotsInOne"))
			{
				Ǵ = "Assign ores and ingots as one"; if (ȇ) ȃ = !oresIngotsInOne; oresIngotsInOne = ȃ
;
			}
			else if (ǳ.StartsWith("toolsAmmoBottlesInOne"))
			{
				Ǵ = "Assign tools, ammo and bottles as one"; if (ȇ) ȃ = !toolsAmmoBottlesInOne;
				toolsAmmoBottlesInOne = ȃ;
			}
			else if (ǳ.StartsWith("fillBottles")) { Ǵ = "Fill bottles"; if (ȇ) ȃ = !fillBottles; fillBottles = ȃ; }
			else if (ǳ.StartsWith(
"enableAutocrafting")) { Ǵ = "Autocrafting"; if (ȇ) ȃ = !enableAutocrafting; enableAutocrafting = ȃ; }
			else if (ǳ.StartsWith("enableAutodisassembling"))
			{
				Ǵ =
"Autodisassembling"; if (ȇ) ȃ = !enableAutodisassembling; enableAutodisassembling = ȃ;
			}
			else if (ǳ.StartsWith("headerOnEveryScreen"))
			{
				Ǵ =
"Show header on every autocrafting screen"; if (ȇ) ȃ = !headerOnEveryScreen; headerOnEveryScreen = ȃ;
			}
			else if (ǳ.StartsWith("sortAssemblerQueue"))
			{
				Ǵ = "Sort assembler queue"
; if (ȇ) ȃ = !sortAssemblerQueue; sortAssemblerQueue = ȃ;
			}
			else if (ǳ.StartsWith("enableBasicIngotCrafting"))
			{
				Ǵ =
"Basic ingot crafting"; if (ȇ) ȃ = !enableBasicIngotCrafting; enableBasicIngotCrafting = ȃ;
			}
			else if (ǳ.StartsWith("disableBasicAutocrafting"))
			{
				Ǵ =
"Disable autocrafting in survival kits"; if (ȇ) ȃ = !disableBasicAutocrafting; disableBasicAutocrafting = ȃ;
			}
			else if (ǳ.StartsWith("allowSpecialSteal"))
			{
				Ǵ =
"Allow special container steal"; if (ȇ) ȃ = !allowSpecialSteal; allowSpecialSteal = ȃ;
			}
			else if (ǳ.StartsWith("enableOreBalancing"))
			{
				Ǵ = "Ore balancing"; if (ȇ) ȃ = !
enableOreBalancing; enableOreBalancing = ȃ;
			}
			else if (ǳ.StartsWith("enableScriptRefineryFilling"))
			{
				Ǵ = "Script assisted refinery filling"; if (ȇ) ȃ =
!enableScriptRefineryFilling; enableScriptRefineryFilling = ȃ;
			}
			else if (ǳ.StartsWith("sortRefiningQueue"))
			{
				Ǵ =
"Sort refinery queue"; if (ȇ) ȃ = !sortRefiningQueue; sortRefiningQueue = ȃ;
			}
			else if (ǳ.StartsWith("enableIceBalancing"))
			{
				Ǵ = "Ice balancing"; if (ȇ) ȃ = !
enableIceBalancing; enableIceBalancing = ȃ;
			}
			else if (ǳ.StartsWith("fillOfflineGenerators"))
			{
				Ǵ = "Fill offline O2/H2 generators"; if (ȇ) ȃ = !
fillOfflineGenerators; fillOfflineGenerators = ȃ;
			}
			else if (ǳ.StartsWith("enableUraniumBalancing"))
			{
				Ǵ = "Uranium balancing"; if (ȇ) ȃ = !
enableUraniumBalancing; enableUraniumBalancing = ȃ;
			}
			else if (ǳ.StartsWith("fillOfflineReactors"))
			{
				Ǵ = "Fill offline reactors"; if (ȇ) ȃ = !
fillOfflineReactors; fillOfflineReactors = ȃ;
			}
			else if (ǳ.StartsWith("enableAssemblerCleanup"))
			{
				Ǵ = "Assembler cleanup"; if (ȇ) ȃ = !
enableAssemblerCleanup; enableAssemblerCleanup = ȃ;
			}
			else if (ǳ.StartsWith("enableInternalSorting"))
			{
				Ǵ = "Internal sorting"; if (ȇ) ȃ = !
enableInternalSorting; enableInternalSorting = ȃ;
			}
			else if (ǳ.StartsWith("useDynamicScriptSpeed"))
			{
				Ǵ = "Dynamic script speed"; if (ȇ) ȃ = !
useDynamicScriptSpeed; useDynamicScriptSpeed = ȃ;
			}
			else if (ǳ.StartsWith("excludeWelders"))
			{
				Ǵ = "Exclude welders"; if (ȇ) ȃ = !excludeWelders;
				excludeWelders = ȃ;
			}
			else if (ǳ.StartsWith("excludeGrinders")) { Ǵ = "Exclude grinders"; if (ȇ) ȃ = !excludeGrinders; excludeGrinders = ȃ; }
			else if (ǳ.
StartsWith("connectionCheck")) { Ǵ = "Connection check"; if (ȇ) ȃ = !connectionCheck; connectionCheck = ȃ; ɍ(); }
			else if (ǳ.StartsWith(
"showNoConveyorTag")) { Ǵ = "Show no conveyor access"; if (ȇ) ȃ = !showNoConveyorTag; showNoConveyorTag = ȃ; ɍ(); }
			else if (ǳ.StartsWith(
"protectTypeContainers")) { Ǵ = "Protect type containers"; if (ȇ) ȃ = !protectTypeContainers; protectTypeContainers = ȃ; }
			else if (ǳ.StartsWith(
"enableNameCorrection")) { Ǵ = "Name correction"; if (ȇ) ȃ = !enableNameCorrection; enableNameCorrection = ȃ; }
			else { Ȅ = false; }
			if (Ȅ)
			{
				TimeSpan Ȃ = DateTime.Now -
ǰ; if (ǲ == "") ǲ = Ǵ + " temporarily " + (ȃ ? "enabled" : "disabled") + "!\n"; Echo(ǲ); Echo("Continuing in " + Math.Ceiling(3 - Ȃ.TotalSeconds
) + " seconds.."); Ǳ = "msg"; if (Ȃ.TotalSeconds >= 3) { Ǵ = ""; ǲ = ""; Ǳ = ""; }
			}
			return Ȅ;
		}
		void ȁ()
		{
			List<IMyProgrammableBlock> Ȁ = new List<
IMyProgrammableBlock>(); GridTerminalSystem.GetBlocksOfType(Ȁ, ǿ => ǿ != Me); if (Ǳ.StartsWith("pauseThisPB") || Ǳ == "")
			{
				Ǳ = ""; foreach (var Ǿ in Ȁ)
				{
					if (Ǿ.
CustomData.Contains(Ƕ) || (Ǿ.CustomData.Contains(ǵ) && Í(Ǿ) < Í(Me)))
					{
						Ǳ = "pauseThisPB;" + Ǿ.CustomName + ";" + Ǿ.CubeGrid.CustomName; foreach (
var X in ʉ) { if (protectTypeContainers && !X.CustomName.Contains(ǻ) && X.IsSameConstructAs(Me)) X.CustomName = ǻ + X.CustomName; }
						return;
					}
				}
				if (Ǳ == "") { foreach (var X in ʈ) { X.CustomName = X.CustomName.Replace(ǻ, ""); } }
			}
		}
		void ȫ()
		{
			ʝ.Clear(); ʜ.Clear();
			GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(ʆ); foreach (var ǽ in ʆ)
			{
				if (ǽ.Status != MyShipConnectorStatus.Connected) continue; if (ǽ.
OtherConnector.CubeGrid.IsSameConstructAs(Me.CubeGrid))
				{
					if (ǽ.CustomName.Contains(noSortingKeyword)) ʝ.Add(ǽ.CubeGrid); if (ǽ.CustomName.
Contains(noIIMKeyword)) ʜ.Add(ǽ.CubeGrid);
				}
				else
				{
					if (ǽ.CustomName.Contains(noSortingKeyword)) ʝ.Add(ǽ.OtherConnector.CubeGrid); if (ǽ.
CustomName.Contains(noIIMKeyword)) ʜ.Add(ǽ.OtherConnector.CubeGrid);
				}
			}
		}
		void ȭ()
		{
			if (ʒ != null)
			{
				try { ʒ = ʓ.GetInventory(0); } catch { ʒ = null; }
			}
			if (ʒ == null)
			{
				try
				{
					foreach (var X in ʉ)
					{
						foreach (var W in ɖ)
						{
							if (X == W) continue; if (X.GetInventory(0).IsConnectedTo(W.
GetInventory(0))) { ʓ = ʉ[0]; ʒ = ʓ.GetInventory(0); return; }
						}
					}
				}
				catch { ʒ = null; }
			}
		}
		void ɋ(IMyTerminalBlock W)
		{
			foreach (var Ç in ʜ)
			{
				if (Ç.
IsSameConstructAs(Me.CubeGrid)) { if (W.CubeGrid == Ç) return; }
				else { if (W.CubeGrid.IsSameConstructAs(Ç)) return; }
			}
			if (W.BlockDefinition.SubtypeId.
Contains("Locker") || W.BlockDefinition.SubtypeId == "VendingMachine" || W.BlockDefinition.TypeIdString.Contains("Parachute")) return;
			if (W is IMySafeZoneBlock) return; if (W is IMyShipWelder && excludeWelders) return; if (W is IMyShipGrinder && excludeGrinders)
				return; if (W is IMyShipDrill && excludeDrills) return; string Ⱦ = W.CustomName; if (Ⱦ.Contains(ǻ)) { ʈ.Add(W); return; }
			bool Ɋ = Ⱦ.Contains(
specialContainerKeyword), Ɍ = false, ɉ = Ⱦ.Contains(manualMachineKeyword), Ɉ = false, ɇ = Ⱦ.Contains(learnKeyword) || Ⱦ.Contains(learnManyKeyword), Ɇ = true, Ʌ =
false; foreach (var ê in lockedContainerKeywords) { if (Ⱦ.Contains(ê)) { Ɍ = true; break; } }
			foreach (var ê in hiddenContainerKeywords)
			{
				if
(Ⱦ.Contains(ê)) { Ɉ = true; break; }
			}
			foreach (var Ç in ʝ)
			{
				if (Ç.IsSameConstructAs(Me.CubeGrid)) { if (W.CubeGrid == Ç) return; }
				else
				{
					if (
!Ɋ && !(W is IMyReactor) && !(W is IMyGasGenerator)) { if (W.CubeGrid.IsSameConstructAs(Ç)) return; }
				}
			}
			if (!Ɉ) ɖ.Add(W); if (
connectionCheck)
			{
				if (ʒ != null) { if (!W.GetInventory(0).IsConnectedTo(ʒ)) { Ɇ = false; } }
				if (!Ɇ)
				{
					if (showNoConveyorTag) ɐ(W, "[No Conveyor]"); return;
				}
				else { ɐ(W, "[No Conveyor]", false); }
			}
			if (Ⱦ.Contains(oreContainerKeyword)) { ʛ.Add(W); Ʌ = true; }
			if (Ⱦ.Contains(
ingotContainerKeyword)) { ʚ.Add(W); Ʌ = true; }
			if (Ⱦ.Contains(componentContainerKeyword)) { ʙ.Add(W); Ʌ = true; }
			if (Ⱦ.Contains(toolContainerKeyword))
			{
				ʘ.
Add(W); Ʌ = true;
			}
			if (Ⱦ.Contains(ammoContainerKeyword)) { ʗ.Add(W); Ʌ = true; }
			if (Ⱦ.Contains(bottleContainerKeyword))
			{
				ʖ.Add(W); Ʌ = true
;
			}
			if (Ɋ) { ʕ.Add(W); if (W.CustomData.Length < 200) î(W); }
			if (Ʌ) ʉ.Add(W); if (W.GetType().ToString().Contains("Weapon") && !(W is
IMyShipDrill)) return; if (W is IMyRefinery)
			{
				if (W.IsSameConstructAs(Me) && !Ɋ && !ɉ && W.IsWorking)
				{
					(W as IMyRefinery).UseConveyorSystem = true
; ʅ.Add(W as IMyRefinery); if (W.BlockDefinition.SubtypeId == "Blast Furnace") { ʂ.Add(W as IMyRefinery); }
					else
					{
						ʄ.Add(W as
IMyRefinery);
					}
				}
				if (!Ɍ && W.GetInventory(1).ItemCount > 0) ʋ.Add(W as IMyRefinery);
			}
			else if (W is IMyAssembler)
			{
				if (W.IsSameConstructAs(Me)
&& !ɉ && !ɇ && W.IsWorking)
				{
					ʁ.Add(W as IMyAssembler); if (W.BlockDefinition.SubtypeId.Contains("Survival")) ɾ.Add(W as
IMyAssembler);
				}
				if (!Ɍ && !ɇ && W.GetInventory(1).ItemCount > 0) ʀ.Add(W as IMyAssembler); if (ɇ) ɿ.Add(W as IMyAssembler);
			}
			else if (W is
IMyGasGenerator)
			{
				if (!Ɋ && !ɉ && W.IsFunctional)
				{
					if (fillOfflineGenerators && !(W as IMyGasGenerator).Enabled) { ɽ.Add(W as IMyGasGenerator); }
					else if ((W as IMyGasGenerator).Enabled) { ɽ.Add(W as IMyGasGenerator); }
				}
			}
			else if (W is IMyGasTank)
			{
				if (!Ɋ && !ɉ && !Ɍ && W.IsWorking &&
W.IsSameConstructAs(Me)) { ʃ.Add(W as IMyGasTank); }
			}
			else if (W is IMyReactor)
			{
				if (!Ɋ && !ɉ && W.IsFunctional)
				{
					if (
fillOfflineReactors && !(W as IMyReactor).Enabled) { ɼ.Add(W as IMyReactor); }
					else if ((W as IMyReactor).Enabled) { ɼ.Add(W as IMyReactor); }
				}
			}
			else
if (W is IMyCargoContainer) { if (W.IsSameConstructAs(Me) && !Ʌ && !Ɍ && !Ɋ) ʇ.Add(W); }
			if (W.InventoryCount == 1 && !Ɋ && !Ɍ && !(W is
IMyReactor))
			{
				if (W.GetInventory(0).ItemCount > 0) ʌ.Add(W); if (!W.BlockDefinition.TypeIdString.Contains("Oxygen"))
				{
					if (W.
IsSameConstructAs(Me)) { ʊ.Insert(0, W); }
					else { ʊ.Add(W); }
				}
			}
		}
		void Ʉ()
		{
			if (!ʐ)
			{
				ȫ(); if (connectionCheck) ȭ(); try
				{
					for (int H = 0; H < ʕ.Count; H++)
					{
						if (!ʕ[H
].CustomName.Contains(specialContainerKeyword)) ʕ[H].CustomData = "";
					}
				}
				catch { }
				ʉ.Clear(); ʛ.Clear(); ʚ.Clear(); ʙ.Clear(); ʘ.
Clear(); ʗ.Clear(); ʖ.Clear(); ʕ.Clear(); ʇ.Clear(); ʈ.Clear(); ɖ.Clear(); ʌ.Clear(); ʊ.Clear(); ʅ.Clear(); ʄ.Clear(); ʂ.Clear(); ʋ.Clear
(); ʁ.Clear(); ɾ.Clear(); ʀ.Clear(); ɿ.Clear(); ɽ.Clear(); ʃ.Clear(); ɼ.Clear(); ʑ = null; ʏ = 0; GridTerminalSystem.GetBlocksOfType<
IMyTerminalBlock>(ɗ, ŧ => ŧ.HasInventory);
			}
			Runtime.UpdateFrequency = UpdateFrequency.Update1; for (int H = ʏ; H < ɗ.Count; H++)
			{
				if (ɗ[H].CubeGrid.
CustomName.Contains(noSortingKeyword)) ʝ.Add(ɗ[H].CubeGrid); if (ɗ[H].CubeGrid.CustomName.Contains(noIIMKeyword)) ʜ.Add(ɗ[H].CubeGrid)
; ɋ(ɗ[H]); ʏ++; if (H % 200 == 0) { ʐ = true; return; }
			}
			if (ʎ == 0) ɓ(ʛ); if (ʎ == 1) ɓ(ʚ); if (ʎ == 2) ɓ(ʙ); if (ʎ == 3) ɓ(ʘ); if (ʎ == 4) ɓ(ʗ); if (ʎ == 5) ɓ(ʕ);
			if (ʎ == 6) ɓ(ʖ); if (ʎ == 7) ʇ.Sort((ɑ, ŧ) => ŧ.GetInventory().MaxVolume.ToIntSafe().CompareTo(ɑ.GetInventory().MaxVolume.ToIntSafe()
					 )); ʎ++; if (ʎ > 7) { ʎ = 0; } else { ʐ = true; return; }
			if (disableBasicAutocrafting && ʁ.Count != ɾ.Count) ʁ.RemoveAll(Ī => Ī.BlockDefinition.
SubtypeId.Contains("Survival")); if (fillBottles)
			{
				ʌ.Sort((ɑ, ŧ) => ŧ.BlockDefinition.TypeIdString.Contains("Oxygen").CompareTo(ɑ.
BlockDefinition.TypeIdString.Contains("Oxygen")));
			}
			ʐ = false; Runtime.UpdateFrequency = UpdateFrequency.Update10;
		}
		void ɓ(List<
IMyTerminalBlock> ɒ)
		{ if (ɒ.Count >= 2 && ɒ.Count <= 500) ɒ.Sort((ɑ, ŧ) => Í(ɑ).CompareTo(Í(ŧ))); if (!Ǔ()) ʎ++; }
		void ɐ(IMyTerminalBlock W, string ɏ, bool
Ɏ = true)
		{
			if (Ɏ) { if (W.CustomName.Contains(ɏ)) return; W.CustomName += " " + ɏ; }
			else
			{
				if (!W.CustomName.Contains(ɏ)) return; W.
CustomName = W.CustomName.Replace(" " + ɏ, "").Replace(ɏ, "").TrimEnd(' ');
			}
		}
		void ɍ()
		{
			for (int H = 0; H < ɖ.Count; H++)
			{
				ɐ(ɖ[H], "[No Conveyor]",
false);
			}
		}
		void ɔ()
		{
			bool Ƀ = false; string Ɂ = ä("oreContainer"); string Ⱥ = ä("ingotContainer"); string ȹ = ä("componentContainer");
			string ȸ = ä("toolContainer"); string ȷ = ä("ammoContainer"); string ȶ = ä("bottleContainer"); string ȵ = ä("specialContainer"); if (
						oreContainerKeyword != Ɂ) { Ƀ = true; }
			else if (ingotContainerKeyword != Ⱥ) { Ƀ = true; }
			else if (componentContainerKeyword != ȹ) { Ƀ = true; }
			else if (
toolContainerKeyword != ȸ) { Ƀ = true; }
			else if (ammoContainerKeyword != ȷ) { Ƀ = true; }
			else if (bottleContainerKeyword != ȶ) { Ƀ = true; }
			else if (
specialContainerKeyword != ȵ) { Ƀ = true; }
			if (Ƀ)
			{
				for (int H = 0; H < ɖ.Count; H++)
				{
					if (ɖ[H].CustomName.Contains(Ɂ))
					{
						ɖ[H].CustomName = ɖ[H].CustomName.Replace(Ɂ,
oreContainerKeyword);
					}
					if (ɖ[H].CustomName.Contains(Ⱥ)) { ɖ[H].CustomName = ɖ[H].CustomName.Replace(Ⱥ, ingotContainerKeyword); }
					if (ɖ[H].CustomName.
Contains(ȹ)) { ɖ[H].CustomName = ɖ[H].CustomName.Replace(ȹ, componentContainerKeyword); }
					if (ɖ[H].CustomName.Contains(ȸ))
					{
						ɖ[H].
CustomName = ɖ[H].CustomName.Replace(ȸ, toolContainerKeyword);
					}
					if (ɖ[H].CustomName.Contains(ȷ))
					{
						ɖ[H].CustomName = ɖ[H].CustomName.
Replace(ȷ, ammoContainerKeyword);
					}
					if (ɖ[H].CustomName.Contains(ȶ))
					{
						ɖ[H].CustomName = ɖ[H].CustomName.Replace(ȶ,
bottleContainerKeyword);
					}
					if (ɖ[H].CustomName.Contains(ȵ)) { ɖ[H].CustomName = ɖ[H].CustomName.Replace(ȵ, specialContainerKeyword); }
				}
				â("oreContainer"
, oreContainerKeyword); â("ingotContainer", ingotContainerKeyword); â("componentContainer", componentContainerKeyword); â(
"toolContainer", toolContainerKeyword); â("ammoContainer", ammoContainerKeyword); â("bottleContainer", bottleContainerKeyword); â(
"specialContainer", specialContainerKeyword);
			}
		}
		void ȴ()
		{
			for (int H = 0; H < ʇ.Count; H++)
			{
				bool Ȳ = false; bool ȱ = false; string Ȱ = ʇ[H].CustomName;
				string ȯ = ""; if (ʛ.Count == 0 || ʔ == "Ore")
				{
					if (oresIngotsInOne) { ȱ = true; }
					else
					{
						ʇ[H].CustomName += " " + oreContainerKeyword; ʛ.Add(ʇ[H]); ȯ =
"Ores";
					}
				}
				else if (ʚ.Count == 0 || ʔ == "Ingot")
				{
					if (oresIngotsInOne) { ȱ = true; }
					else
					{
						ʇ[H].CustomName += " " + ingotContainerKeyword; ʚ.Add(ʇ[H
]); ȯ = "Ingots";
					}
				}
				else if (ʙ.Count == 0 || ʔ == "Component")
				{
					ʇ[H].CustomName += " " + componentContainerKeyword; ʙ.Add(ʇ[H]); ȯ =
"Components";
				}
				else if (ʘ.Count == 0 || ʔ == "PhysicalGunObject")
				{
					if (toolsAmmoBottlesInOne) { Ȳ = true; }
					else
					{
						ʇ[H].CustomName += " " +
toolContainerKeyword; ʘ.Add(ʇ[H]); ȯ = "Tools";
					}
				}
				else if (ʗ.Count == 0 || ʔ == "AmmoMagazine")
				{
					if (toolsAmmoBottlesInOne) { Ȳ = true; }
					else
					{
						ʇ[H].CustomName +=
" " + ammoContainerKeyword; ʗ.Add(ʇ[H]); ȯ = "Ammo";
					}
				}
				else if (ʖ.Count == 0 || ʔ == "OxygenContainerObject" || ʔ == "GasContainerObject")
				{
					if
(toolsAmmoBottlesInOne) { Ȳ = true; }
					else { ʇ[H].CustomName += " " + bottleContainerKeyword; ʖ.Add(ʇ[H]); ȯ = "Bottles"; }
				}
				if (ȱ)
				{
					ʇ[H].
CustomName += " " + oreContainerKeyword + " " + ingotContainerKeyword; ʛ.Add(ʇ[H]); ʚ.Add(ʇ[H]); ȯ = "Ores and Ingots";
				}
				if (Ȳ)
				{
					ʇ[H].CustomName +=
" " + toolContainerKeyword + " " + ammoContainerKeyword + " " + bottleContainerKeyword; ʘ.Add(ʇ[H]); ʗ.Add(ʇ[H]); ʖ.Add(ʇ[H]); ȯ =
"Tools, Ammo and Bottles";
				}
				if (ȯ != "") { ɞ = "Assigned '" + Ȱ + "' as a new container for type '" + ȯ + "'."; }
				ʔ = "";
			}
		}
		void Ȯ()
		{
			ȳ(ʛ, oreContainerKeyword); ȳ(ʚ,
ingotContainerKeyword); ȳ(ʙ, componentContainerKeyword); ȳ(ʘ, toolContainerKeyword); ȳ(ʗ, ammoContainerKeyword); ȳ(ʖ, bottleContainerKeyword);
		}
		void ȳ
(List<IMyTerminalBlock> ć, string ȼ)
		{
			if (ć.Count > 1)
			{
				bool ɂ = false; foreach (var X in ć)
				{
					if (X.CustomName.Contains("[P")) continue
; if (X.GetInventory(0).ItemCount == 0)
					{
						if (ɂ) continue; X.CustomName = X.CustomName.Replace(ȼ, ȼ + "!"); ɂ = true; if (X.CustomName.
Contains(ȼ + "!!!"))
						{
							string ɀ = System.Text.RegularExpressions.Regex.Replace(X.CustomName, @"(" + ȼ + @")(!+)", ""); ɀ = System.Text.
				  RegularExpressions.Regex.Replace(ɀ, @"\(\d+\.?\d*\%\)", ""); ɀ = ɀ.Replace("  ", " "); X.CustomName = ɀ.TrimEnd(' '); ʉ.Remove(X); ɞ = "Unassigned '" + ɀ
							 + "' from being a container for type '" + ȼ + "'.";
						}
					}
					else
					{
						if (X.CustomName.Contains(ȼ + "!"))
						{
							string ɀ = System.Text.
RegularExpressions.Regex.Replace(X.CustomName, @"(" + ȼ + @")(!+)", ȼ); ɀ = ɀ.Replace("  ", " "); X.CustomName = ɀ.TrimEnd(' ');
						}
					}
				}
			}
		}
		void ȿ()
		{
			for (int H
= 0; H < ɖ.Count; H++)
			{
				string Ⱦ = ɖ[H].CustomName; string ș = Ⱦ.ToLower(); List<string> Ƚ = new List<string>(); if (ș.Contains(
		oreContainerKeyword.ToLower()) && !Ⱦ.Contains(oreContainerKeyword)) Ƚ.Add(oreContainerKeyword); if (ș.Contains(ingotContainerKeyword.ToLower())
			&& !Ⱦ.Contains(ingotContainerKeyword)) Ƚ.Add(ingotContainerKeyword); if (ș.Contains(componentContainerKeyword.ToLower()) && !Ⱦ.
			   Contains(componentContainerKeyword)) Ƚ.Add(componentContainerKeyword); if (ș.Contains(toolContainerKeyword.ToLower()) && !Ⱦ.Contains(
				 toolContainerKeyword)) Ƚ.Add(toolContainerKeyword); if (ș.Contains(ammoContainerKeyword.ToLower()) && !Ⱦ.Contains(ammoContainerKeyword)) Ƚ.Add(
					   ammoContainerKeyword); if (ș.Contains(bottleContainerKeyword.ToLower()) && !Ⱦ.Contains(bottleContainerKeyword)) Ƚ.Add(bottleContainerKeyword);
				foreach (var ê in lockedContainerKeywords) { if (ș.Contains(ê.ToLower()) && !Ⱦ.Contains(ê)) { Ƚ.Add(ê); break; } }
				foreach (var ê in
hiddenContainerKeywords) { if (ș.Contains(ê.ToLower()) && !Ⱦ.Contains(ê)) { Ƚ.Add(ê); break; } }
				if (ș.Contains(specialContainerKeyword.ToLower()) && !Ⱦ.
Contains(specialContainerKeyword)) Ƚ.Add(specialContainerKeyword); if (ș.Contains(noSortingKeyword.ToLower()) && !Ⱦ.Contains(
noSortingKeyword)) Ƚ.Add(noSortingKeyword); if (ș.Contains(manualMachineKeyword.ToLower()) && !Ⱦ.Contains(manualMachineKeyword)) Ƚ.Add(
manualMachineKeyword); if (ș.Contains(autocraftingKeyword.ToLower()) && !Ⱦ.Contains(autocraftingKeyword)) Ƚ.Add(autocraftingKeyword); if (ș.
Contains(assembleKeyword.ToLower()) && !Ⱦ.Contains(assembleKeyword)) Ƚ.Add(assembleKeyword); if (ș.Contains(disassembleKeyword.
ToLower()) && !Ⱦ.Contains(disassembleKeyword)) Ƚ.Add(disassembleKeyword); if (ș.Contains(learnKeyword.ToLower()) && !Ⱦ.Contains(
learnKeyword)) Ƚ.Add(learnKeyword); if (ș.Contains(learnManyKeyword.ToLower()) && !Ⱦ.Contains(learnManyKeyword)) Ƚ.Add(learnManyKeyword);
				if (ș.Contains("[p") && !Ⱦ.Contains("[P")) Ƚ.Add("[P"); if (ș.Contains("[pmax]") && !Ⱦ.Contains("[PMax]")) Ƚ.Add("[PMax]"); if (ș.
						  Contains("[pmin]") && !Ⱦ.Contains("[PMin]")) Ƚ.Add("[PMin]"); foreach (var Ë in Ƚ)
				{
					ɖ[H].CustomName = ɖ[H].CustomName.ŷ(Ë, Ë); ɞ =
"Corrected name\nof: '" + Ⱦ + "'\nto: '" + ɖ[H].CustomName + "'";
				}
			}
			var Ɯ = new List<IMyTerminalBlock>(); GridTerminalSystem.GetBlocksOfType<
IMyTextSurfaceProvider>(Ɯ, ŧ => ŧ.IsSameConstructAs(Me)); for (int H = 0; H < Ɯ.Count; H++)
			{
				string Ⱦ = Ɯ[H].CustomName; string ș = Ⱦ.ToLower(); List<string> Ƚ =
new List<string>(); if (ș.Contains(mainLCDKeyword.ToLower()) && !Ⱦ.Contains(mainLCDKeyword)) Ƚ.Add(mainLCDKeyword); if (ș.Contains
(inventoryLCDKeyword.ToLower()) && !Ⱦ.Contains(inventoryLCDKeyword)) Ƚ.Add(inventoryLCDKeyword); if (ș.Contains(
warningsLCDKeyword.ToLower()) && !Ⱦ.Contains(warningsLCDKeyword)) Ƚ.Add(warningsLCDKeyword); if (ș.Contains(performanceLCDKeyword.ToLower()) && !
Ⱦ.Contains(performanceLCDKeyword)) Ƚ.Add(performanceLCDKeyword); foreach (var Ë in Ƚ)
				{
					Ɯ[H].CustomName = Ɯ[H].CustomName.ŷ(Ë, Ë)
; ɞ = "Corrected name\nof: '" + Ⱦ + "'\nto: '" + Ɯ[H].CustomName + "'";
				}
			}
		}
		void Ξ()
		{
			if (ʞ == 0) Ν(Ȏ, ʛ, oreContainerKeyword); if (ʞ == 1) Ν(ȍ, ʚ,
ingotContainerKeyword); if (ʞ == 2) Ν(Ȍ, ʙ, componentContainerKeyword); if (ʞ == 3) Ν(ȉ, ʘ, toolContainerKeyword); if (ʞ == 4) Ν(ȋ, ʗ, ammoContainerKeyword); if (ʞ
== 5) Ν(Ȋ, ʖ, bottleContainerKeyword); if (ʞ == 6) Ν(Ȑ, ʖ, bottleContainerKeyword); if (ʞ == 7) Ν(Ț, ʘ, toolContainerKeyword); if (ʞ == 8) Ν(Ȭ, ʘ,
toolContainerKeyword); if (ʞ == 9) Ν(Ȫ, ʘ, toolContainerKeyword); ʞ++; if (ʞ > 9) ʞ = 0;
		}
		void Ν(string Ζ, List<IMyTerminalBlock> Μ, string Λ)
		{
			if (Μ.Count == 0)
			{
				ƹ
("There are no containers for type '" + Λ + "'!\nBuild new ones or add the tag to existing ones!"); ʔ = Ζ; return;
			}
			IMyTerminalBlock Y = null; int Κ = int.MaxValue; for (int H = 0; H < Μ.Count; H++)
			{
				if (Ζ == Ȋ && Μ[H].BlockDefinition.TypeIdString.Contains("OxygenTank")
&& Μ[H].BlockDefinition.SubtypeId.Contains("Hydrogen")) { continue; }
				else if (Ζ == Ȑ && Μ[H].BlockDefinition.TypeIdString.Contains(
"OxygenTank") && !Μ[H].BlockDefinition.SubtypeId.Contains("Hydrogen")) { continue; }
				var Q = Μ[H].GetInventory(0); if ((float)Q.CurrentVolume
<= (float)Q.MaxVolume - Ȩ[Ζ]) { Y = Μ[H]; Κ = Í(Μ[H]); break; }
			}
			if (Y == null)
			{
				ƹ("All containers for type '" + Λ +
"' are full!\nYou should build new cargo containers!"); ʔ = Ζ; return;
			}
			IMyTerminalBlock Θ = null; if (fillBottles && (Ζ == Ȋ || Ζ == Ȑ)) { Θ = Η(Ζ); }
			for (int H = 0; H < ʌ.Count; H++)
			{
				if (ʌ[H] == Y || (ʌ[H]
.CustomName.Contains(Λ) && Í(ʌ[H]) <= Κ) || (Ζ == "Ore" && ʌ[H].GetType().ToString().Contains("MyGasGenerator"))) { continue; }
				if (ʌ[H]
.CustomName.Contains(Λ) && balanceTypeContainers && !ʌ[H].BlockDefinition.TypeIdString.Contains("OxygenGenerator") && !ʌ[H].
BlockDefinition.TypeIdString.Contains("OxygenTank")) continue; if (!Υ(ʌ[H])) continue; if (Θ != null)
				{
					if (!ʌ[H].BlockDefinition.TypeIdString.
Contains("Oxygen")) { Æ(Ζ, ʌ[H], 0, Θ, 0); continue; }
				}
				Æ(Ζ, ʌ[H], 0, Y, 0);
			}
			for (int H = 0; H < ʋ.Count; H++)
			{
				if (ʋ[H] == Y || (ʋ[H].CustomName.Contains
(Λ) && Í(ʋ[H]) <= Κ)) { continue; }
				if (!Υ(ʋ[H])) continue; Æ(Ζ, ʋ[H], 1, Y, 0);
			}
			for (int H = 0; H < ʀ.Count; H++)
			{
				if ((ʀ[H].Mode ==
MyAssemblerMode.Disassembly && ʀ[H].IsProducing) || ʀ[H] == Y || (ʀ[H].CustomName.Contains(Λ) && Í(ʀ[H]) <= Κ)) { continue; }
				if (!Υ(ʀ[H])) continue; if (Θ
!= null) { Æ(Ζ, ʀ[H], 1, Θ, 0); continue; }
				Æ(Ζ, ʀ[H], 1, Y, 0);
			}
			if (!Ǔ()) ʞ++;
		}
		IMyTerminalBlock Η(string Ζ)
		{
			List<IMyGasTank> Ι = new List<
IMyGasTank>(ʃ); if (Ζ == Ȋ) Ι.RemoveAll(Ε => Ε.BlockDefinition.SubtypeId.Contains("Hydrogen")); if (Ζ == Ȑ) Ι.RemoveAll(Ε => !Ε.BlockDefinition.
SubtypeId.Contains("Hydrogen")); foreach (var Ϋ in Ι)
			{
				if (Ϋ.FilledRatio > 0)
				{
					var Ω = Ϋ.GetInventory(); if ((float)(Ω.MaxVolume - Ω.
CurrentVolume) < 0.120) continue; Ϋ.AutoRefillBottles = true; return Ϋ;
				}
			}
			List<IMyGasGenerator> Ψ = ɽ.Where(Χ => Χ.IsSameConstructAs(Me) && Χ.
Enabled == true).ToList(); MyDefinitionId đ = MyItemType.MakeOre("Ice"); foreach (var Φ in Ψ)
			{
				if (G(đ, Φ) > 0)
				{
					Φ.AutoRefill = true; return Φ;
				}
			}
			return null;
		}
		bool Υ(IMyTerminalBlock W)
		{
			if (W.GetOwnerFactionTag() != Me.GetOwnerFactionTag())
			{
				ƹ("'" + W.CustomName +
"'\nhas a different owner/faction!\nCan't move items from there!"); return false;
			}
			return true;
		}
		void Τ()
		{
			char Σ = '0'; char Ρ = '0'; char[] Π = { 'A', 'N', 'T', 'X' }; char[] Ο = { 'a', 'd' }; if (
sortingPattern.Length == 2) { Σ = sortingPattern[0]; Ρ = sortingPattern[1]; }
			ɘ = new List<IMyTerminalBlock>(ʌ); ɘ.AddRange(ʕ); if (
enableInternalSorting)
			{
				if (Σ.ToString().IndexOfAny(Π) < 0 || Ρ.ToString().IndexOfAny(Ο) < 0)
				{
					ƹ("You provided the invalid sorting pattern '" +
sortingPattern + "'!\nCan't sort the inventories!"); return;
				}
			}
			else { ɘ = ɘ.FindAll(H => H.CustomName.ToLower().Contains("(sort:")); }
			for (var ƙ = ɻ
; ƙ < ɘ.Count; ƙ++)
			{
				if (Ǔ()) return; if (ɻ >= ɘ.Count - 1) { ɻ = 0; } else { ɻ++; }
				var Q = ɘ[ƙ].GetInventory(0); var A = new List<MyInventoryItem>(
); Q.GetItems(A); if (A.Count > 200) continue; char Ϊ = Σ; char Δ = Ρ; string Γ = System.Text.RegularExpressions.Regex.Match(ɘ[ƙ].
CustomName, @"(\(sort:)(.{2})", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[2].Value; if (Γ.Length == 2)
				{
					Σ = Γ[0]; Ρ = Γ[1
]; if (Σ.ToString().IndexOfAny(Π) < 0 || Ρ.ToString().IndexOfAny(Ο) < 0)
					{
						ƹ("You provided an invalid sorting pattern in\n'" + ɘ[ƙ].
CustomName + "'!\nUsing global pattern!"); Σ = Ϊ; Ρ = Δ;
					}
				}
				var Ή = new List<MyInventoryItem>(); Q.GetItems(Ή); if (Σ == 'A')
				{
					if (Ρ == 'd')
					{
						Ή.Sort((ɑ,
ŧ) => ŧ.Amount.ToIntSafe().CompareTo(ɑ.Amount.ToIntSafe()));
					}
					else
					{
						Ή.Sort((ɑ, ŧ) => ɑ.Amount.ToIntSafe().CompareTo(ŧ.Amount.
ToIntSafe()));
					}
				}
				else if (Σ == 'N')
				{
					if (Ρ == 'd') { Ή.Sort((ɑ, ŧ) => ŧ.Type.SubtypeId.ToString().CompareTo(ɑ.Type.SubtypeId.ToString())); }
					else { Ή.Sort((ɑ, ŧ) => ɑ.Type.SubtypeId.ToString().CompareTo(ŧ.Type.SubtypeId.ToString())); }
				}
				else if (Σ == 'T')
				{
					if (Ρ == 'd')
					{
						Ή.Sort((
ɑ, ŧ) => ŧ.Type.ToString().CompareTo(ɑ.Type.ToString()));
					}
					else
					{
						Ή.Sort((ɑ, ŧ) => ɑ.Type.ToString().CompareTo(ŧ.Type.ToString()))
;
					}
				}
				else if (Σ == 'X')
				{
					if (Ρ == 'd')
					{
						Ή.Sort((ɑ, ŧ) => (ŧ.Type.TypeId.ToString() + ŧ.Amount.ToIntSafe().ToString(@"000000000")).
CompareTo((ɑ.Type.TypeId.ToString() + ɑ.Amount.ToIntSafe().ToString(@"000000000"))));
					}
					else
					{
						Ή.Sort((ɑ, ŧ) => (ɑ.Type.TypeId.ToString() +
ɑ.Amount.ToIntSafe().ToString(@"000000000")).CompareTo((ŧ.Type.TypeId.ToString() + ŧ.Amount.ToIntSafe().ToString(
@"000000000"))));
					}
				}
				if (Ή.SequenceEqual(A, new š())) continue; foreach (var Ë in Ή)
				{
					string Β = Ë.ToString(); for (int H = 0; H < A.Count; H++)
					{
						if (A[
H].ToString() == Β) { Q.TransferItemTo(Q, H, A.Count, false); A.Clear(); Q.GetItems(A); break; }
					}
				}
				Σ = Ϊ; Ρ = Δ;
			}
		}
		void Α()
		{
			for (int ƙ = ɺ; ƙ < ʕ
.Count; ƙ++)
			{
				if (Ǔ()) return; ɺ++; î(ʕ[ƙ]); int f = 0; if (ʕ[ƙ].BlockDefinition.SubtypeId.Contains("Assembler"))
				{
					IMyAssembler Ø = ʕ[ƙ
] as IMyAssembler; if (Ø.Mode == MyAssemblerMode.Disassembly) f = 1;
				}
				var Ò = ʕ[ƙ].CustomData.Split('\n'); List<string> ΐ = new List<
string>(); double Ώ = 0; double ʾ = 0; double Ύ = 0; foreach (var Ð in Ò)
				{
					if (!Ð.Contains("=")) continue; MyDefinitionId M; double Ό = 0; var Ί =
Ð.Split('='); if (Ί.Length >= 2)
					{
						if (!MyDefinitionId.TryParse(ȏ + Ί[0], out M)) continue; double.TryParse(Ί[1], out Ό); if (Ί[1].
   ToLower().Contains("all")) { Ό = int.MaxValue; }
					}
					else { continue; }
					Ώ = G(M, ʕ[ƙ], f); ʾ = 0; Ύ = 0; if (Ό >= 0) { ʾ = Ό - Ώ; } else { ʾ = Math.Abs(Ό) - Ώ; }
					if (ʾ >= 1
&& Ό >= 0)
					{
						var Q = ʕ[ƙ].GetInventory(f); if ((float)Q.CurrentVolume > (float)Q.MaxVolume * 0.98f) continue; IMyTerminalBlock Ä = null; if (
							 allowSpecialSteal) { Ä = d(M, true, ʕ[ƙ]); }
						else { Ä = d(M); }
						if (Ä != null) { Ύ = Æ(M.ToString(), Ä, 0, ʕ[ƙ], f, ʾ, true); }
						if (ʾ > Ύ && Ό != int.MaxValue)
						{
							ΐ.Add(ʾ - Ύ + " "
+ M.SubtypeName);
						}
					}
					else if (ʾ < 0) { IMyTerminalBlock Y = e(ʕ[ƙ], ʇ); if (Y != null) Æ(M.ToString(), ʕ[ƙ], f, Y, 0, Math.Abs(ʾ), true); }
				}
				if (ΐ
.Count > 0) { ƹ(ʕ[ƙ].CustomName + "\nis missing the following items to match its quota:\n" + String.Join(", ", ΐ)); }
			}
			ɺ = 0;
		}
		void τ(
List<IMyTerminalBlock> ć)
		{
			foreach (var X in ć)
			{
				string σ = X.CustomName; string ɀ = ""; var ς = System.Text.RegularExpressions.Regex.
Match(σ, @"\(\d+\.?\d*\%\)").Value; if (ς != "") { ɀ = σ.Replace(ς, "").TrimEnd(' '); } else { ɀ = σ; }
				var Q = X.GetInventory(0); string Ǩ = ((
float)Q.CurrentVolume).Ƅ((float)Q.MaxVolume); if (showFillLevel) { ɀ += " (" + Ǩ + ")"; ɀ = ɀ.Replace("  ", " "); }
				if (ɀ != σ) X.CustomName = ɀ;
			}
		}
		StringBuilder ρ()
		{
			if (ʍ.Count > 1)
			{
				string π = @"(" + autocraftingKeyword + @" *)(\d*)"; ʍ.Sort((ɑ, ŧ) => System.Text.RegularExpressions.Regex.
Match(ɑ.CustomName, π).Groups[2].Value.CompareTo(System.Text.RegularExpressions.Regex.Match(ŧ.CustomName, π).Groups[2].Value));
			}
			StringBuilder ń = new StringBuilder(); if (!ʍ[0].GetText().Contains(ɕ))
			{
				ʍ[0].Font = defaultFont; ʍ[0].FontSize = defaultFontSize;
				ʍ[0].TextPadding = defaultPadding;
			}
			foreach (var u in ʍ)
			{
				ń.Append(u.GetText() + "\n"); u.WritePublicTitle(
"Craft item manually once to show up here"); u.Font = ʍ[0].Font; u.FontSize = ʍ[0].FontSize; u.TextPadding = ʍ[0].TextPadding; u.Alignment = TextAlignment.LEFT; u.ContentType =
ContentType.TEXT_AND_IMAGE;
			}
			var ο = new List<string>(ń.ToString().Split('\n')); var κ = new List<string>(); var ξ = new HashSet<string>();
			string υ; foreach (var Ð in ο) { if (Ð.IndexOfAny(ȗ) <= 0) continue; υ = Ð.Remove(Ð.IndexOf(" ")); if (!ξ.Contains(υ)) { κ.Add(Ð); ξ.Add(υ); } }
			List<string> Ò = ʍ[0].CustomData.Split('\n').ToList(); foreach (var J in ʟ)
			{
				bool ϋ = false; if (ξ.Contains(J)) { continue; }
				foreach (var
Ð in Ò)
				{
					if (!Ð.StartsWith("-")) continue; string ϊ = ""; try
					{
						if (Ð.Contains("=")) { ϊ = Ð.Substring(1, Ð.IndexOf("=") - 1); }
						else
						{
							ϊ = Ð.
Substring(1);
						}
					}
					catch { continue; }
					if (ϊ == J) { ϋ = true; break; }
				}
				if (!ϋ)
				{
					MyDefinitionId M = ǟ(J); double ˢ = Math.Ceiling(G(M)); κ.Add(J + " " + ˢ +
" = " + ˢ + defaultModifier);
				}
			}
			foreach (var Ð in Ò) { if (!Ð.StartsWith("-")) continue; if (Ð.Contains("=")) { κ.Add(Ð); } }
			StringBuilder Ǝ =
new StringBuilder(); try
			{
				IOrderedEnumerable<string> ω; ω = κ.OrderBy(ɑ => ɑ); bool ψ; string χ, J, φ; foreach (var Ð in ω)
				{
					ψ = false; if (Ð.
StartsWith("-")) { J = Ð.Remove(Ð.IndexOf("=")).TrimStart('-'); χ = "-"; }
					else { J = Ð.Remove(Ð.IndexOf(" ")); χ = ""; }
					φ = Ð.Replace(χ + J, "");
					foreach (var Ë in ʟ) { if (Ë == J) { ψ = true; break; } }
					if (ψ) Ǝ.Append(χ + J + φ + "\n");
				}
			}
			catch { }
			return Ǝ;
		}
		void ν(StringBuilder ń)
		{
			if (ń.Length == 0
)
			{
				ń.Append("Autocrafting error!\n\nNo items for crafting available!\n\nIf you hid all items, check the custom data of the first autocrafting panel and reenable some of them.\n\nOtherwise, store or build new items manually!"
			  ); ń = ʍ[0].Ř(ń, 2, false); ʍ[0].WriteText(ń); return;
			}
			var Ľ = ń.ToString().TrimEnd('\n').Split('\n'); int Ļ = Ľ.Length; int ĺ = 0; float
ΰ = 0; foreach (var u in ʍ)
			{
				float ų = u.ŋ(); int Ĺ = u.Ŏ(); int ĸ = 0; List<string> Ǝ = new List<string>(); if (u == ʍ[0] ||
	 headerOnEveryScreen)
				{
					string ί = ɕ; if (headerOnEveryScreen && ʍ.Count > 1)
					{
						ί += " " + (ʍ.IndexOf(u) + 1) + "/" + ʍ.Count; try { ί += " [" + Ľ[ĺ][0] + "-#]"; }
						catch
						{
							ί +=
" [Empty]";
						}
					}
					Ǝ.Add(ί); Ǝ.Add(u.ō('=', u.ũ(ί)).ToString() + "\n"); string ή = "Component "; string έ = "Current | Wanted "; ΰ = u.ũ("Wanted ");
					string Ǥ = u.ō(' ', ų - u.ũ(ή) - u.ũ(έ)).ToString(); Ǝ.Add(ή + Ǥ + έ + "\n"); ĸ = 5;
				} while ((ĺ < Ļ && ĸ < Ĺ) || (u == ʍ[ʍ.Count - 1] && ĺ < Ļ))
				{
					var Ð = Ľ[ĺ].Split
(' '); Ð[0] += " "; Ð[1] = Ð[1].Replace('$', ' '); string Ǥ = u.ō(' ', ų - u.ũ(Ð[0]) - u.ũ(Ð[1]) - ΰ).ToString(); string ά = Ð[0] + Ǥ + Ð[1] + Ð[2]
; Ǝ.Add(ά); ĺ++; ĸ++;
				}
				if (headerOnEveryScreen && ʍ.Count > 1) { Ǝ[0] = Ǝ[0].Replace('#', Ľ[ĺ - 1][0]); }
				u.WriteText(String.Join("\n", Ǝ));
			}
			if (showAutocraftingModifiers)
			{
				string α = "\n\n---\n\nModifiers (append after wanted amount):\n" + "'A' - Assemble only\n" +
"'D' - Disassemble only\n" + "'P' - Always queue first (priority)\n" + "'H' - Hide and manage in background\n" + "'I' - Hide and ignore\n"; ʍ[ʍ.Count - 1].
WriteText(α, true);
			}
		}
		void μ()
		{
			ʍ.Clear(); GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(ʍ, u => u.IsSameConstructAs(Me) && u.
CustomName.Contains(autocraftingKeyword)); if (ʍ.Count == 0) return; if (ʁ.Count == 0)
			{
				ƹ(
"No assemblers found!\nBuild assemblers to enable autocrafting!"); return;
			}
			ˮ(); List<MyDefinitionId> λ = new List<MyDefinitionId>(); var κ = ρ().ToString().TrimEnd('\n').Split('\n');
			StringBuilder Ǝ = new StringBuilder(); foreach (var Ð in κ)
			{
				string J = ""; bool ι = true; if (Ð.StartsWith("-"))
				{
					ι = false; try
					{
						J = Ð.Substring(1, Ð.
IndexOf("=") - 1);
					}
					catch { continue; }
				}
				else { try { J = Ð.Substring(0, Ð.IndexOf(" ")); } catch { continue; } }
				MyDefinitionId M = ǟ(J); if (M == null)
					continue; double θ = Math.Ceiling(G(M)); string η = Ð.Substring(Ð.IndexOfAny(ȗ) + 1).ToLower(); double ζ = 0; double.TryParse(System.Text.
								RegularExpressions.Regex.Replace(η, @"\D", ""), out ζ); string ε = θ.ToString(); string δ = ζ.ToString(); string γ = ""; bool æ = false; if (η.Contains("h"
												) && ι)
				{
					if (!ʍ[0].CustomData.StartsWith(Ȼ)) ʍ[0].CustomData = Ȼ; ʍ[0].CustomData += "\n-" + J + "=" + η.Replace("h", "").Replace(" ", "").
				   ToUpper(); continue;
				}
				else if (η.Contains("i") && ι)
				{
					if (!ʍ[0].CustomData.StartsWith(Ȼ)) ʍ[0].CustomData = Ȼ; ʍ[0].CustomData += "\n-" + J;
					continue;
				}
				if (η.Contains("a")) { if (θ > ζ) ζ = θ; γ += "A"; }
				if (η.Contains("d")) { if (θ < ζ) ζ = θ; γ += "D"; }
				if (η.Contains("p")) { æ = true; γ += "P"; }
				Ɲ(M, ζ
); double β = Math.Abs(ζ - θ); bool Έ; MyDefinitionId Ô = Ʀ(M, out Έ); double ˏ = ƞ(Ô); if (θ >= ζ + ζ * assembleMargin && ˏ > 0 && ƪ(Ô) > 0)
				{
					ˍ(Ô); Ʃ(Ô
, 0); ˏ = 0; ɞ = "Removed '" + M.SubtypeId.ToString() + "' from the assembling queue.";
				}
				if (θ <= ζ - ζ * disassembleMargin && ˏ > 0 && ƪ(Ô) < 0)
				{
					ˍ(
Ô); Ʃ(Ô, 0); ˏ = 0; ɞ = "Removed '" + M.SubtypeId.ToString() + "' from the disassembling queue.";
				}
				string Ù = ""; if (ˏ > 0 || β > 0)
				{
					if ((
enableAutodisassembling || η.Contains("d")) && θ > ζ + ζ * disassembleMargin) { Ʃ(Ô, -1); Ù = "$[D:"; }
					else if (enableAutocrafting && θ < ζ - ζ * assembleMargin)
					{
						Ʃ(Ô, 1);
						Ù = "$[A:";
					}
					if (Ù != "") { if (ˏ == 0) { Ù += "Wait]"; } else { Ù += Math.Round(ˏ) + "]"; } }
				}
				else { Ʃ(Ô, 0); }
				if (!Έ) Ù = "$[NoBP!]"; if (Έ && æ) { λ.Add(Ô); }
				string ʻ = "$=$ "; if (θ > ζ) ʻ = "$>$ "; if (θ < ζ) ʻ = "$<$ "; if (ι) Ǝ.Append(J + " " + ε + Ù + ʻ + δ + γ + "\n"); if (Ù.Contains("[D:Wait]")) { ˎ(Ô, β); }
				else if
(Ù.Contains("[A:Wait]")) { Ά(Ô, β, æ); ɞ = "Queued " + β + " '" + M.SubtypeId.ToString() + "' in the assemblers."; }
				else if (Ù.Contains(
"[NoBP!]") && ζ > θ)
				{
					ƹ("Can't craft\n'" + M.SubtypeId.ToString() +
   "'\nThere's no blueprint stored for this item!\nTag an assembler with the '" + learnKeyword + "' keyword and queue\nit up about 100 times to learn the blueprint.");
				}
			}
			ͷ(λ); ν(Ǝ);
		}
		void ʺ()
		{
			if (ʅ.Count > 0)
				return; MyDefinitionId ʬ = MyItemType.MakeOre("Stone"); MyDefinitionId Ô = MyDefinitionId.Parse(ȩ + "StoneOreToIngotBasic"); double ʹ = G
						 (ʬ); if (ʹ > 0) { double ʼ = Math.Floor(ʹ / 500 / ɾ.Count); if (ʼ < 1) return; foreach (var ʸ in ɾ) { if (ʸ.IsQueueEmpty) ʸ.AddQueueItem(Ô, ʼ); } }
		}
		void ʷ()
		{
			if (ɧ == 0) ɧ += ʶ(ʛ, Ȏ, true, true); if (ɧ == 1) ɧ += ʶ(ʚ, ȍ, true, true); if (ɧ == 2) ɧ += ʶ(ʙ, Ȍ, true, true); if (ɧ == 3) ɧ += ʶ(ʘ, ȉ, true, true)
									; if (ɧ == 4) ɧ += ʶ(ʗ, ȋ, true, true); if (ɧ == 5) ɧ += ʶ(ʖ, "ContainerObject", true, true); ɧ++; if (ɧ > 5) ɧ = 0;
		}
		int ʶ(List<IMyTerminalBlock> ɒ,
string ʵ = "", bool ʴ = false, bool ʳ = false)
		{
			if (ʴ) ɒ.RemoveAll(Ū => Ū.InventoryCount == 2 || Ū.BlockDefinition.TypeIdString.Contains(
"OxygenGenerator") || Ū.BlockDefinition.TypeIdString.Contains("OxygenTank")); if (ʳ) ɒ.RemoveAll(H => !H.CubeGrid.IsSameConstructAs(Me.CubeGrid)
); if (ɒ.Count < 2) { return 1; }
			Dictionary<MyItemType, double> ʽ = new Dictionary<MyItemType, double>(); for (int H = 0; H < ɒ.Count; H++)
			{
				var A = new List<MyInventoryItem>(); ɒ[H].GetInventory(0).GetItems(A); foreach (var Ë in A)
				{
					if (!Ë.Type.TypeId.ToString().
Contains(ʵ)) continue; MyItemType M = Ë.Type; if (ʽ.ContainsKey(M)) { ʽ[M] += (double)Ë.Amount; } else { ʽ[M] = (double)Ë.Amount; }
				}
			}
			Dictionary<
MyItemType, double> ˈ = new Dictionary<MyItemType, double>(); foreach (var Ë in ʽ) { ˈ[Ë.Key] = (int)(Ë.Value / ɒ.Count); }
			for (int ˇ = 0; ˇ < ɒ.Count
; ˇ++)
			{
				if (Ǔ()) return 0; var ˆ = new List<MyInventoryItem>(); ɒ[ˇ].GetInventory(0).GetItems(ˆ); Dictionary<MyItemType, double> ˁ =
				 new Dictionary<MyItemType, double>(); foreach (var Ë in ˆ)
				{
					MyItemType M = Ë.Type; if (ˁ.ContainsKey(M)) { ˁ[M] += (double)Ë.Amount; }
					else { ˁ[M] = (double)Ë.Amount; }
				}
				double Ģ = 0; foreach (var Ë in ʽ)
				{
					ˁ.TryGetValue(Ë.Key, out Ģ); double ˀ = ˈ[Ë.Key]; if (Ģ <= ˀ + 1) continue;
					for (int ʿ = 0; ʿ < ɒ.Count; ʿ++)
					{
						if (ɒ[ˇ] == ɒ[ʿ]) continue; double Ğ = G(Ë.Key, ɒ[ʿ]); if (Ğ >= ˀ - 1) continue; double ʾ = ˀ - Ğ; if (ʾ > Ģ - ˀ) ʾ = Ģ - ˀ; if (
							 ʾ > 0) { Ģ -= Æ(Ë.Key.ToString(), ɒ[ˇ], 0, ɒ[ʿ], 0, ʾ, true); if (Ģ.ƚ(ˀ - 1, ˀ + 1)) break; }
					}
				}
			}
			return Ǔ() ? 0 : 1;
		}
		void ʲ()
		{
			if (ʅ.Count == 0) return;
			if (ɡ == 0) Ȓ = ͳ(ʄ); if (ɡ == 1) Ș = ͳ(ʂ); if (enableScriptRefineryFilling)
			{
				if (ɡ == 2) ʣ(ʄ, Ȓ); if (ɡ == 3) ʣ(ʂ, Ș); if (ɡ == 4) ʱ(ʄ, Ȓ); if (ɡ == 5) ʱ(ʂ, Ș);
				if (ɡ == 6 && ʄ.Count > 0 && ʂ.Count > 0) { bool ʫ = false; ʫ = ʮ(ʄ, ʂ, Ȓ); if (!ʫ) ʮ(ʂ, ʄ, Ș); }
			}
			else { if (ɡ > 1) ɡ = 6; }
			ɡ++; if (ɡ > 6) ɡ = 0;
		}
		void ʠ()
		{
			if (ɟ == 0)
				ɟ += ʶ(ʄ.ToList<IMyTerminalBlock>()); if (ɟ == 1) ɟ += ʶ(ʂ.ToList<IMyTerminalBlock>()); ɟ++; if (ɟ > 1) ɟ = 0;
		}
		void ʩ(List<IMyRefinery> ʨ,
List<MyItemType> ʡ)
		{
			foreach (IMyRefinery Ý in ʨ)
			{
				var Q = Ý.GetInventory(0); var A = new List<MyInventoryItem>(); Q.GetItems(A); if (A.
Count < 2) continue; bool ʪ = false; int ʧ = 0; string ʥ = ""; foreach (var ʤ in ʡ)
				{
					for (int H = 0; H < A.Count; H++)
					{
						if (A[H].Type == ʤ)
						{
							ʧ = H; ʥ = ʤ.
SubtypeId; ʪ = true; break;
						}
					}
					if (ʪ) break;
				}
				if (ʧ != 0)
				{
					Q.TransferItemTo(Q, ʧ, 0, true); ɞ = "Sorted the refining queue.\n'" + ʥ +
"' is now at the front of the queue.";
				}
			}
		}
		void ʣ(List<IMyRefinery> ʢ, List<MyItemType> ʡ)
		{
			if (ʢ.Count == 0) { ɡ++; return; }
			MyItemType ʦ = new MyItemType(); MyItemType ʬ =
MyItemType.MakeOre("Stone"); foreach (var ʤ in ʡ) { if (G(ʤ) > 100) { ʦ = ʤ; break; } }
			if (!ʦ.ToString().Contains(Ȏ)) return; for (int H = 0; H < ʢ.Count
; H++)
			{
				if (Ǔ()) return; var Q = ʢ[H].GetInventory(0); if ((float)Q.CurrentVolume > (float)Q.MaxVolume * 0.75f)
				{
					var A = new List<
MyInventoryItem>(); Q.GetItems(A); foreach (var Ë in A) { if (Ë.Type == ʦ) return; }
					IMyTerminalBlock Y = e(ʢ[H], ʛ); if (Y != null) { Æ("", ʢ[H], 0, Y, 0); }
				}
			}
			if (!Ǔ()) ɡ++;
		}
		void ʱ(List<IMyRefinery> ʢ, List<MyItemType> ʡ)
		{
			if (ʢ.Count == 0) { ɡ++; return; }
			double ʰ; foreach (var ʤ in ʡ)
			{
				if (G(ʤ)
== 0) continue; IMyTerminalBlock ʯ = d(ʤ, true); if (ʯ == null) continue; for (int H = 0; H < ʢ.Count; H++)
				{
					if (Ǔ()) return; var Q = ʢ[H].
GetInventory(0); if ((float)Q.CurrentVolume > (float)Q.MaxVolume * 0.98f) continue; ʰ = Æ(ʤ.ToString(), ʯ, 0, ʢ[H], 0); if (ʰ == 0)
					{
						ʯ = d(ʤ, true); if (ʯ ==
null) break;
					}
				}
			}
			if (!Ǔ()) ɡ++;
		}
		bool ʮ(List<IMyRefinery> ʭ, List<IMyRefinery> ˉ, List<MyItemType> ʡ)
		{
			for (int H = 0; H < ʭ.Count; H++)
			{
				if ((
float)ʭ[H].GetInventory(0).CurrentVolume > 0.05f) continue; for (int ƺ = 0; ƺ < ˉ.Count; ƺ++)
				{
					if ((float)ˉ[ƺ].GetInventory(0).
CurrentVolume > 0) { foreach (var ʤ in ʡ) { Æ(ʤ.ToString(), ˉ[ƺ], 0, ʭ[H], 0, -0.5); } return true; }
				}
			}
			return false;
		}
		List<MyItemType> ͳ(List<
IMyRefinery> ʢ)
		{
			if (ʢ.Count == 0) { ɡ++; return null; }
			List<string> Ͳ = new List<string>(ȓ); Ͳ.Sort((ɑ, ŧ) => (G(MyItemType.MakeIngot(ɑ)) / ơ(ɑ)).
CompareTo((G(MyItemType.MakeIngot(ŧ)) / ơ(ŧ)))); Ͳ.InsertRange(0, fixedRefiningList); List<MyItemType> ʹ = new List<MyItemType>();
			MyItemType M; foreach (var Ë in Ͳ)
			{
				M = MyItemType.MakeOre(Ë); foreach (var Ý in ʢ)
				{
					if (Ý.GetInventory(0).CanItemsBeAdded(1, M))
					{
						ʹ.Add(M);
						break;
					}
				}
			}
			if (!Ǔ()) ɡ++; return ʹ;
		}
		void Ͱ()
		{
			foreach (var Ø in ʁ)
			{
				if (Ø.GetOwnerFactionTag() == Me.GetOwnerFactionTag())
				{
					var Q = Ø.
GetInventory(0); if ((float)Q.CurrentVolume == 0) continue; if (Ø.IsQueueEmpty || Ø.Mode == MyAssemblerMode.Disassembly || (float)Q.CurrentVolume
> (float)Q.MaxVolume * 0.98f) { IMyTerminalBlock Y = e(Ø, ʚ); if (Y != null) Æ("", Ø, 0, Y, 0); }
				}
			}
		}
		void ͱ()
		{
			foreach (IMyAssembler Ø in ʁ)
			{
				if (Ø.Mode == MyAssemblerMode.Disassembly) continue; if (Ø.CustomData.Contains("skipQueueSorting")) { Ø.CustomData = ""; continue; }
				var Ù = new List<MyProductionItem>(); Ø.GetQueue(Ù); if (Ù.Count < 2) continue; int ʧ = 0; string ʥ = ""; double ͽ = Double.MaxValue; double
								  ͼ = Double.MinValue; double ͻ, ͺ; for (int H = 0; H < Ù.Count; H++)
				{
					MyDefinitionId M = Ƥ(Ù[H].BlueprintId); ͺ = G(M); ͻ = (double)Ù[H].Amount
; if (ͺ < 100 && ͺ < ͽ) { ͽ = ͺ; ʧ = H; ʥ = M.SubtypeId.ToString(); continue; }
					if (ͽ == Double.MaxValue && ͻ > ͼ) { ͼ = ͻ; ʧ = H; ʥ = M.SubtypeId.ToString(); }
				}
				if (ʧ != 0)
				{
					Ø.MoveQueueItemRequest(Ù[ʧ].ItemId, 0); ɞ = "Sorted the assembling queue.\n'" + ʥ +
			"' is now at the front of the queue.";
				}
			}
		}
		void ͷ(List<MyDefinitionId> Ͷ)
		{
			if (Ͷ.Count == 0) return; if (Ͷ.Count > 1) Ͷ.Sort((ɑ, ŧ) => G(Ƥ(ɑ)).CompareTo(G(Ƥ(ŧ)))); foreach (
var Ø in ʁ)
			{
				var Ù = new List<MyProductionItem>(); Ø.GetQueue(Ù); if (Ù.Count < 2) continue; foreach (var Ô in Ͷ)
				{
					int ƙ = Ù.FindIndex(H
=> H.BlueprintId == Ô); if (ƙ == -1) continue; if (ƙ == 0) { Ø.CustomData = "skipQueueSorting"; break; }
					Ø.MoveQueueItemRequest(Ù[ƙ].ItemId, 0
); Ø.CustomData = "skipQueueSorting"; ɞ = "Sorted the assembler queue by priority.\n'" + Ƥ(Ô).SubtypeId.ToString() +
"' is now at the front of the queue."; break;
				}
			}
		}
		void Ά(MyDefinitionId Ô, double Á, bool æ)
		{
			List<IMyAssembler> ˋ = new List<IMyAssembler>(); foreach (IMyAssembler Ø
in ʁ)
			{
				if (Ø.CustomName.Contains(disassembleKeyword)) continue; if (æ == false && Ø.Mode == MyAssemblerMode.Disassembly && !Ø.
			 IsQueueEmpty) continue; if (Ø.Mode == MyAssemblerMode.Disassembly) { Ø.ClearQueue(); Ø.Mode = MyAssemblerMode.Assembly; }
				if (Ø.CanUseBlueprint(Ô
)) { ˋ.Add(Ø); }
			}
			if (ˋ.Count == 0) ƹ("There's no assembler available to produce '" + Ô.SubtypeName +
  "'. Make sure, that you have at least one assembler with no tags or the !assemble-only tag!"); ˌ(ˋ, Ô, Á);
		}
		void ˎ(MyDefinitionId Ô, double Á)
		{
			List<IMyAssembler> ˋ = new List<IMyAssembler>(); foreach (IMyAssembler Ø in ʁ)
			{
				if (Ø.CustomName.Contains(assembleKeyword)) continue; if (Ø.Mode == MyAssemblerMode.Assembly && Ø.IsProducing) continue; if (Ø.Mode ==
						  MyAssemblerMode.Assembly) { Ø.ClearQueue(); Ø.Mode = MyAssemblerMode.Disassembly; }
				if (Ø.Mode == MyAssemblerMode.Assembly) continue; if (Ø.
CanUseBlueprint(Ô)) { ˋ.Add(Ø); }
			}
			if (ˋ.Count == 0) ƹ("There's no assembler available to dismantle '" + Ô.SubtypeName +
"'. Make sure, that you have at least one assembler with no tags or the !disassemble-only tag!"); ˌ(ˋ, Ô, Á);
		}
		void ˌ(List<IMyAssembler> ˋ, MyDefinitionId Ô, double Á)
		{
			if (ˋ.Count == 0) return; double ˊ = Math.Ceiling(Á / ˋ.Count);
			foreach (IMyAssembler Ø in ˋ) { if (ˊ > Á) ˊ = Math.Ceiling(Á); if (Á > 0) { Ø.InsertQueueItem(0, Ô, ˊ); Á -= ˊ; } else { break; } }
		}
		void ˍ(
MyDefinitionId Ô)
		{
			foreach (IMyAssembler Ø in ʁ)
			{
				var Ù = new List<MyProductionItem>(); Ø.GetQueue(Ù); for (int H = 0; H < Ù.Count; H++)
				{
					if (Ù[H].
BlueprintId == Ô) Ø.RemoveQueueItem(H, Ù[H].Amount);
				}
			}
		}
		void ˮ()
		{
			foreach (IMyAssembler Ø in ʁ)
			{
				Ø.UseConveyorSystem = true; Ø.CooperativeMode
= false; Ø.Repeating = false;
			}
		}
		void ˬ()
		{
			List<IMyAssembler> ˤ = new List<IMyAssembler>(ʁ); ˤ.RemoveAll(ɑ => ɑ.IsQueueEmpty); if (ˤ.
Count == 0) return; List<IMyAssembler> ˣ = new List<IMyAssembler>(ʁ); ˣ.RemoveAll(ɑ => !ɑ.IsQueueEmpty); if (ˣ.Count == 0) return; double ˢ, ˡ
= 0, ˠ; IMyAssembler ˑ = null; List<MyProductionItem> Ù = new List<MyProductionItem>(); foreach (var ː in ˤ)
			{
				Ù.Clear(); ː.GetQueue(Ù)
; ˢ = 0; foreach (var Ë in Ù) { ˢ += (double)Ë.Amount; }
				if (ˢ > ˡ) { ˡ = ˢ; ˑ = ː; }
			}
			if (ˑ == null) return; Ù.Clear(); ˑ.GetQueue(Ù); for (int H = 0; H < Ù
.Count; H++)
			{
				ˢ = (double)Ù[H].Amount; if (ˢ <= 10) continue; for (int ƺ = 0; ƺ < ˣ.Count; ƺ++)
				{
					if (!ˣ[ƺ].CanUseBlueprint(Ù[0].BlueprintId)
) continue; if (ˑ.Mode == MyAssemblerMode.Assembly && ˣ[ƺ].CustomName.Contains(disassembleKeyword)) continue; if (ˑ.Mode ==
MyAssemblerMode.Disassembly && ˣ[ƺ].CustomName.Contains(assembleKeyword)) continue; ˣ[ƺ].Mode = ˑ.Mode; if (ˣ[ƺ].Mode != ˑ.Mode) continue; ˠ = Math.
Ceiling(ˢ / ˣ.Count + 1 - ƺ); ˢ -= ˠ; ˣ[ƺ].AddQueueItem(Ù[H].BlueprintId, ˠ); ˑ.RemoveQueueItem(H, ˠ);
				}
			}
		}
		void ǭ()
		{
			if (ɽ.Count == 0) return;
			double ƣ = spaceForBottles * 0.12; MyDefinitionId đ = MyItemType.MakeOre("Ice"); string Đ = đ.ToString(); double ď = 0.00037; foreach (
						  IMyGasGenerator Č in ɽ)
			{
				var Q = Č.GetInventory(0); double Ď = G(đ, Č); double Ē = Ď * ď; double č = (double)Q.MaxVolume - ƣ - ď; if (Ē > č + ď)
				{
					IMyTerminalBlock Y = e(Č, ʛ); if (Y != null) { double ú = (Ē - č) / ď; Æ(Đ, Č, 0, Y, 0, ú); }
				}
				else if (Ē < č - ď)
				{
					IMyTerminalBlock Ä = d(đ, true); if (Ä != null)
					{
						double ú
= (č - Ē) / ď; Æ(Đ, Ä, 0, Č, 0, ú);
					}
				}
			}
			double ċ = 0; double Ċ = 0; foreach (var Č in ɽ)
			{
				ċ += G(đ, Č); var Q = Č.GetInventory(0); Ċ += (double)Q.
MaxVolume;
			}
			double Ĕ = (ċ * ď) / Ċ; foreach (var Ĥ in ɽ)
			{
				var h = Ĥ.GetInventory(0); double Ģ = G(đ, Ĥ); double ġ = Ģ * ď; double Ġ = (double)h.MaxVolume
; if (ġ > Ġ * (Ĕ + 0.001))
				{
					foreach (var ğ in ɽ)
					{
						if (Ĥ == ğ) continue; var P = ğ.GetInventory(0); double Ğ = G(đ, ğ); double ĝ = Ğ * ď; double Ĝ = (
  double)P.MaxVolume; if (ĝ < Ĝ * (Ĕ - 0.001))
						{
							double ě = ((Ĝ * Ĕ) - ĝ) / ď; if ((Ģ - ě) * ď >= Ġ * Ĕ && ě > 5) { Ģ -= Æ(Đ, Ĥ, 0, ğ, 0, ě); continue; }
							if ((Ģ - ě) * ď < Ġ * Ĕ && ě >
5) { double Ě = (Ģ * ď - Ġ * Ĕ) / ď; Æ(Đ, Ĥ, 0, ğ, 0, Ě); break; }
						}
					}
				}
			}
		}
		void ę()
		{
			if (ɼ.Count == 0) return; MyDefinitionId Ę = MyItemType.MakeIngot(
"Uranium"); string ė = Ę.ToString(); double Ė = 0; double ĕ = 0; foreach (IMyReactor ĉ in ɼ)
			{
				ĉ.UseConveyorSystem = false; double ü = G(Ę, ĉ);
				double ï = uraniumAmountLargeGrid; if (ĉ.CubeGrid.GridSize == 0.5f) ï = uraniumAmountSmallGrid; ĕ += ï; if (ü > ï + 0.05)
				{
					IMyTerminalBlock Y = e(ĉ
, ʚ); if (Y != null) { double ú = ü - ï; Æ(ė, ĉ, 0, Y, 0, ú); }
				}
				else if (ü < ï - 0.05)
				{
					IMyTerminalBlock Ä = d(Ę, true); if (Ä != null)
					{
						double ú = ï - ü; Æ(ė
, Ä, 0, ĉ, 0, ú);
					}
				}
				Ė += G(Ę, ĉ);
			}
			double ù = Ė / ĕ; foreach (var ø in ɼ)
			{
				double û = G(Ę, ø); double ö = ù * uraniumAmountLargeGrid; if (ø.CubeGrid
.GridSize == 0.5f) ö = ù * uraniumAmountSmallGrid; if (û > ö + 0.05)
				{
					foreach (var ô in ɼ)
					{
						if (ø == ô) continue; double ó = G(Ę, ô); double ò = ù *
uraniumAmountLargeGrid; if (ô.CubeGrid.GridSize == 0.5f) ò = ù * uraniumAmountSmallGrid; if (ó < ò - 0.05)
						{
							û = G(Ę, ø); double ñ = ò - ó; if (û - ñ >= ö)
							{
								Æ(ė, ø, 0, ô, 0, ñ);
								continue;
							}
							if (û - ñ < ö) { ñ = û - ö; Æ(ė, ø, 0, ô, 0, ñ); break; }
						}
					}
				}
			}
		}
		StringBuilder ð(IMyTextSurface u, bool õ = true, bool ý = true, bool Ă = true, bool Ĉ
= true, bool Ć = true)
		{
			bool ą = false; StringBuilder º = new StringBuilder(); if (õ)
			{
				º.Append("Isy's Inventory Manager\n"); º.Append(
u.ō('=', u.ũ(º))).Append("\n\n");
			}
			if (ý && Ŀ != null) { º.Append("Warning!\n" + Ŀ + "\n\n"); ą = true; }
			if (Ă)
			{
				º.Append(ă(u, ʛ, "Ores")); º.
Append(ă(u, ʚ, "Ingots")); º.Append(ă(u, ʙ, "Components")); º.Append(ă(u, ʘ, "Tools")); º.Append(ă(u, ʗ, "Ammo")); º.Append(ă(u, ʖ,
"Bottles")); º.Append("=> " + ʉ.Count + " type containers: Balancing " + (balanceTypeContainers ? "ON" : "OFF") + "\n\n"); ą = true;
			}
			if (Ĉ)
			{
				º.
Append("Managed blocks:\n"); float Ą = u.ũ(ɖ.Count.ToString()); º.Append(ɖ.Count + " Inventories (total) / " + ʌ.Count +
" have items to sort\n"); if (ʕ.Count > 0) { º.Append(u.ō(' ', Ą - u.ũ(ʕ.Count.ToString())).ToString() + ʕ.Count + " Special Containers\n"); }
				if (ʅ.Count > 0)
				{
					º
.Append(u.ō(' ', Ą - u.ũ(ʅ.Count.ToString())).ToString() + ʅ.Count + " Refineries: "); º.Append("Ore Balancing " + (
enableOreBalancing ? "ON" : "OFF") + "\n");
				}
				if (ɽ.Count > 0)
				{
					º.Append(u.ō(' ', Ą - u.ũ(ɽ.Count.ToString())).ToString() + ɽ.Count + " O2/H2 Generators: ");
					º.Append("Ice Balancing " + (enableIceBalancing ? "ON" : "OFF") + "\n");
				}
				if (ɼ.Count > 0)
				{
					º.Append(u.ō(' ', Ą - u.ũ(ɼ.Count.ToString())
).ToString() + ɼ.Count + " Reactors: "); º.Append("Uranium Balancing " + (enableUraniumBalancing ? "ON" : "OFF") + "\n");
				}
				if (ʁ.Count > 0
)
				{
					º.Append(u.ō(' ', Ą - u.ũ(ʁ.Count.ToString())).ToString() + ʁ.Count + " Assemblers: "); º.Append("Craft " + (enableAutocrafting ?
						  "ON" : "OFF") + " | "); º.Append("Uncraft " + (enableAutodisassembling ? "ON" : "OFF") + " | "); º.Append("Cleanup " + (
										enableAssemblerCleanup ? "ON" : "OFF") + "\n");
				}
				if (ɾ.Count > 0)
				{
					º.Append(u.ō(' ', Ą - u.ũ(ɾ.Count.ToString())).ToString() + ɾ.Count + " Survival Kits: "); º.
Append("Ingot Crafting " + (enableBasicIngotCrafting ? "ON" : "OFF") + (ʅ.Count > 0 ? " (Auto OFF - refineries exist)" : "") + "\n");
				}
				º.Append
("\n"); ą = true;
			}
			if (Ć && ɞ != "") { º.Append("Last Action:\n" + ɞ); ą = true; }
			if (!ą) { º.Append("-- No informations to show --"); }
			return
º;
		}
		StringBuilder ă(IMyTextSurface u, List<IMyTerminalBlock> ć, string K)
		{
			double ā = 0, Ā = 0; foreach (var X in ć)
			{
				var Q = X.
GetInventory(0); ā += (double)Q.CurrentVolume; Ā += (double)Q.MaxVolume;
			}
			string Ê = ć.Count + "x " + K + ":"; string ÿ = ā.Ɓ(); string ģ = Ā.Ɓ();
			StringBuilder þ = ǝ(u, Ê, ā, Ā, ÿ, ģ); return þ;
		}
		void ĥ(string ń = null)
		{
			if (ɛ.Count == 0) { ɬ++; return; }
			for (int H = ɦ; H < ɛ.Count; H++)
			{
				if (Ǔ()) return; ɦ
++; var İ = ɛ[H].ƌ(mainLCDKeyword); foreach (var į in İ)
				{
					var ĭ = į.Key; var ħ = į.Value; bool õ = ħ.Ɩ("showHeading"); bool ý = ħ.Ɩ(
"showWarnings"); bool Ă = ħ.Ɩ("showContainerStats"); bool Ĉ = ħ.Ɩ("showManagedBlocks"); bool Ć = ħ.Ɩ("showLastAction"); bool ł = ħ.Ɩ(
"scrollTextIfNeeded"); StringBuilder º = new StringBuilder(); if (ń != null) { º.Append(ń); } else { º = ð(ĭ, õ, ý, Ă, Ĉ, Ć); }
					º = ĭ.Ř(º, õ ? 3 : 0, ł); ĭ.WriteText(º);
				}
			}
			ɬ++; ɦ = 0;
		}
		void Ł()
		{
			if (ɚ.Count == 0) { ɬ++; return; }
			StringBuilder ŀ = new StringBuilder(); if (ɳ.Count == 0)
			{
				ŀ.Append(
"- No problems detected -");
			}
			else { int Ń = 1; foreach (var Ŀ in ɳ) { ŀ.Append(Ń + ". " + Ŀ.Replace("\n", " ") + "\n"); Ń++; } }
			for (int H = ɥ; H < ɚ.Count; H++)
			{
				if (Ǔ())
					return; ɥ++; var İ = ɚ[H].ƌ(warningsLCDKeyword); foreach (var į in İ)
				{
					var ĭ = į.Key; var ħ = į.Value; bool õ = ħ.Ɩ("showHeading"); bool ł = ħ.Ɩ
("scrollTextIfNeeded"); StringBuilder º = new StringBuilder(); if (õ)
					{
						º.Append("Isy's Inventory Manager Warnings\n"); º.Append(
ĭ.ō('=', ĭ.ũ(º))).Append("\n\n");
					}
					º.Append(ŀ); º = ĭ.Ř(º, õ ? 3 : 0, ł); ĭ.WriteText(º);
				}
			}
			ɬ++; ɥ = 0;
		}
		void ņ()
		{
			if (ɠ.Count == 0)
			{
				ɬ++;
				return;
			}
			for (int H = ɤ; H < ɠ.Count; H++)
			{
				if (Ǔ()) return; ɤ++; var İ = ɠ[H].ƌ(performanceLCDKeyword); foreach (var į in İ)
				{
					var ĭ = į.Key; var ħ
= į.Value; bool õ = ħ.Ɩ("showHeading"); bool ł = ħ.Ɩ("scrollTextIfNeeded"); StringBuilder º = new StringBuilder(); if (õ)
					{
						º.Append(
"Isy's Inventory Manager Performance\n"); º.Append(ĭ.ō('=', ĭ.ũ(º))).Append("\n\n");
					}
					º.Append(ɪ); º = ĭ.Ř(º, õ ? 3 : 0, ł); ĭ.WriteText(º);
				}
			}
			ɬ++; ɤ = 0;
		}
		void Ņ()
		{
			if (ə.Count ==
0) { ɬ++; return; }
			Dictionary<IMyTextSurface, string> Ň = new Dictionary<IMyTextSurface, string>(); Dictionary<IMyTextSurface,
   string> ľ = new Dictionary<IMyTextSurface, string>(); List<IMyTextSurface> ļ = new List<IMyTextSurface>(); List<IMyTextSurface> ı = new
			List<IMyTextSurface>(); foreach (var W in ə)
			{
				var İ = W.ƌ(inventoryLCDKeyword); foreach (var į in İ)
				{
					if (į.Value.Contains(
inventoryLCDKeyword + ":")) { Ň[į.Key] = į.Value; ļ.Add(į.Key); }
					else { ľ[į.Key] = į.Value; ı.Add(į.Key); }
				}
			}
			HashSet<string> Į = new HashSet<string>();
			foreach (var ĭ in Ň) { Į.Add(System.Text.RegularExpressions.Regex.Match(ĭ.Value, inventoryLCDKeyword + @":[A-Za-z]+").Value); }
			Į.
RemoveWhere(Ĳ => Ĳ == ""); List<string> Ĭ = Į.ToList(); for (int H = ɣ; H < Ĭ.Count; H++)
			{
				if (Ǔ()) return; ɣ++; var ī = Ň.Where(Ī => Ī.Value.Contains(Ĭ[H])
); var ĩ = from pair in ī
		   orderby System.Text.RegularExpressions.Regex.Match(pair.Value, inventoryLCDKeyword + @":\w+").Value
		   ascending
		   select pair; IMyTextSurface Ĩ = ĩ.ElementAt(0).Key; string ħ = ĩ.ElementAt(0).Value; StringBuilder º = ķ(Ĩ, ħ); if (!ħ.ToLower().
					   Contains("noscroll")) { int Ħ = 0; foreach (var Ĵ in ĩ) { Ħ += Ĵ.Key.Ŏ(); } º = Ĩ.Ř(º, 0, true, Ħ); }
				var Ľ = º.ToString().Split('\n'); int Ļ = Ľ.Length
; int ĺ = 0; int Ĺ, ĸ; foreach (var Ĵ in ĩ)
				{
					IMyTextSurface ĭ = Ĵ.Key; ĭ.FontSize = Ĩ.TextureSize.Y / ĭ.TextureSize.Y * Ĩ.FontSize; ĭ.Font =
Ĩ.Font; ĭ.TextPadding = Ĩ.TextPadding; ĭ.Alignment = Ĩ.Alignment; ĭ.ContentType = ContentType.TEXT_AND_IMAGE; Ĺ = ĭ.Ŏ(); ĸ = 0; º.Clear()
		 ; while (ĺ < Ļ && ĸ < Ĺ) { º.Append(Ľ[ĺ] + "\n"); ĺ++; ĸ++; }
					ĭ.WriteText(º);
				}
			}
			for (int H = ɢ; H < ı.Count; H++)
			{
				if (Ǔ()) return; ɢ++;
				IMyTextSurface ĭ = ı[H]; string ħ = ľ[ĭ]; StringBuilder º = ķ(ĭ, ħ); if (!ħ.ToLower().Contains("noscroll")) { º = ĭ.Ř(º, 0); }
				ĭ.WriteText(º); ĭ.
Alignment = TextAlignment.LEFT; ĭ.ContentType = ContentType.TEXT_AND_IMAGE;
			}
			ɬ++; ɣ = 0; ɢ = 0;
		}
		StringBuilder ķ(IMyTextSurface u, string ħ)
		{
			StringBuilder º = new StringBuilder(); var Ķ = ħ.Split('\n').ToList(); Ķ.RemoveAll(ĵ => ĵ.StartsWith("@") || ĵ.Length <= 1); bool ĳ = true; try
			{
				if (Ķ[
0].Length <= 1) ĳ = false;
			}
			catch { ĳ = false; }
			if (!ĳ)
			{
				º.Append("Put an item, type name or Echo command in the custom data.\n\n" +
"Examples:\nComponent\nIngot\nSteelPlate\nEcho My cool text\n\n" + "Optionally, add a max amount for the bars as a 2nd parameter.\n\n" + "Example:\nIngot 100000\n\n" +
"At last, add any of these 5 modifiers (optional):\n\n" + "'noHeading' to hide the heading\n" + "'singleLine' to force one line per item\n" + "'noBar' to hide the bars\n" +
"'noScroll' to prevent the screen from scrolling\n" + "'hideEmpty' to hide items that have an amount of 0\n\n" +
"Examples:\nComponent 100000 noBar\nSteelPlate noHeading noBar hideEmpty\n\n" + "To display multiple different items, use a new line for every item!\n" +
"Full guide: https://steamcommunity.com/sharedfiles/filedetails/?id=1226261795");
			}
			else
			{
				foreach (var Ð in Ķ)
				{
					var Ö = Ð.Split(' '); double q = -1; bool o = false; bool m = false; bool w = false; bool k = false; if (Ö.
Length >= 2) { try { q = Convert.ToDouble(Ö[1]); } catch { q = -1; } }
					string ª = Ð.ToLower(); if (ª.Contains("noheading")) o = true; if (ª.Contains(
"nobar")) m = true; if (ª.Contains("hideempty")) w = true; if (ª.Contains("singleline")) k = true; if (ª.StartsWith("echoc"))
					{
						string z = Ð.ŷ(
"echoc ", "").ŷ("echoc", ""); º.Append(u.ō(' ', (u.ŋ() - u.ũ(z)) / 2)).Append(z + "\n");
					}
					else if (ª.StartsWith("echor"))
					{
						string z = Ð.ŷ(
"echor ", "").ŷ("echor", ""); º.Append(u.ō(' ', u.ŋ() - u.ũ(z))).Append(z + "\n");
					}
					else if (ª.StartsWith("echo"))
					{
						º.Append(Ð.ŷ("echo ", ""
).ŷ("echo", "") + "\n");
					}
					else { º.Append(v(u, Ö[0], q, o, m, w, k)); }
				}
			}
			if (º.Length > 2) { return º.Replace("\n", "", 0, 2); }
			else
			{
				return new
StringBuilder("Nothing to show at the moment...");
			}
		}
		StringBuilder v(IMyTextSurface u, string r, double q, bool o = false, bool m = false, bool
w = false, bool k = false)
		{
			StringBuilder º = new StringBuilder(); bool Ì = q == -1 ? true : false; foreach (var Ë in ȧ)
			{
				if (Ë.ToString().
ToLower().Contains(r.ToLower()))
				{
					if (º.Length == 0 && !o)
					{
						string Ê = "Items containing '" + char.ToUpper(r[0]) + r.Substring(1).ToLower() +
"'"; º.Append("\n" + u.ō(' ', (u.ŋ() - u.ũ(Ê)) / 2)).Append(Ê + "\n\n");
					}
					double Á = G(Ë); if (Á == 0 && w) continue; if (Ì)
					{
						q = ƨ(Ë, true); if (q == 0)
							continue;
					}
					º.Append(ǝ(u, Ë.SubtypeId.ToString(), Á, q, Á.Ɔ(), q.Ɔ(), m, k));
				}
			}
			if (º.Length == 0 && !w)
			{
				º.Append("Error!\n\n"); º.Append(
"No items containing '" + r + "' found!\nCheck the custom data of this LCD and enter a valid type or item name!\n");
			}
			return º;
		}
		void É(string È = "")
		{
			ɜ = ɜ >= 3 ? 0 : ɜ + 1; Echo("Isy's Inventory Manager " + ɝ[ɜ] + "\n====================\n"); if (Ŀ != null) { Echo("Warning!\n" + Ŀ + "\n"); }
			StringBuilder º = new StringBuilder(); º.Append("Script is running in " + (Ƿ ? "station" : "ship") + " mode\n\n"); º.Append("Task: " + ǯ[ɯ] + È + "\n")
						; º.Append("Script step: " + ɯ + " / " + (ǯ.Length - 1) + "\n\n"); º.Append(ƿ); if (ʝ.Count > 0)
			{
				º.Append(
"[No Sorting] grids:\n==============\n"); foreach (var Ç in ʝ) { º.Append(Ç.CustomName + "\n"); }
				º.Append("\n");
			}
			if (ʜ.Count > 0)
			{
				º.Append(
"[No IIM] grids:\n===========\n"); foreach (var Ç in ʜ) { º.Append(Ç.CustomName + "\n"); }
			}
			ɪ = º; Echo(º.ToString()); if (ɛ.Count == 0)
			{
				Echo(
"Hint:\nBuild a LCD and add the main LCD\nkeyword '" + mainLCDKeyword + "' to its name to get\nmore informations about your base\nand the current script actions.\n");
			}
		}
		double Æ
(string Å, IMyTerminalBlock Ä, int Ã, IMyTerminalBlock Y, int Â, double Á = -1, bool À = false)
		{
			var h = Ä.GetInventory(Ã); var P = Y.
GetInventory(Â); if (!h.IsConnectedTo(P))
			{
				ƹ("Item transfer from '" + Ä.CustomName + "'\nto '" + Y.CustomName +
"'\nnot possible! There's no valid conveyor path!"); return 0;
			}
			if (P.IsFull || Á == 0) { return 0; }
			var A = new List<MyInventoryItem>(); h.GetItems(A); if (A.Count == 0) return 0; double N
= 0; MyDefinitionId M = new MyDefinitionId(); MyDefinitionId L = new MyDefinitionId(); string K = ""; string J = ""; bool I = false;
			string O = ""; if (Á == -0.5) O = "halfInventory"; if (Á == -1) O = "completeInventory"; for (int H = A.Count - 1; H >= 0; H--)
			{
				M = A[H].Type; if (À ? M.
ToString() == Å : M.ToString().Contains(Å))
				{
					if (O != "" && M != L) N = 0; L = M; K = M.TypeId.ToString().Replace(ȏ, ""); J = M.SubtypeId.ToString(); I =
 true; if (!h.CanTransferItemTo(P, M))
					{
						ƹ("'" + J + "' couldn't be transferred\nfrom '" + Ä.CustomName + "'\nto '" + Y.CustomName +
"'\nThe conveyor type is too small!"); return 0;
					}
					double F = (double)A[H].Amount; double E = 0; if (O == "completeInventory") { h.TransferItemTo(P, H, null, true); }
					else if (
O == "halfInventory") { double D = Math.Ceiling((double)A[H].Amount / 2); h.TransferItemTo(P, H, null, true, (VRage.MyFixedPoint)D); }
					else { if (!K.Contains(ȍ)) Á = Math.Ceiling(Á); h.TransferItemTo(P, H, null, true, (VRage.MyFixedPoint)Á); }
					A.Clear(); h.GetItems(A); try
					{
						if ((MyDefinitionId)A[H].Type == M) { E = (double)A[H].Amount; }
					}
					catch { E = 0; }
					double C = F - E; N += C; Á -= C; if (Á <= 0 && O == "") break; if ((float)
P.CurrentVolume > (float)P.MaxVolume * 0.98) break;
				}
			}
			if (!I) return 0; if (N > 0)
			{
				string B = Math.Round(N, 2) + " " + J + " " + K; ɞ = "Moved: " + B
+ "\nfrom: '" + Ä.CustomName + "'\nto: '" + Y.CustomName + "'";
			}
			else
			{
				string B = Math.Round(Á, 2) + " " + Å.Replace(ȏ, ""); if (O ==
"completeInventory") B = "all items"; if (O == "halfInventory") B = "half of the items"; ƹ("Couldn't move '" + B + "'\nfrom '" + Ä.CustomName + "'\nto '" + Y.
CustomName + "'\nCheck conveyor connection and owner/faction!");
			}
			return N;
		}
		double G(MyDefinitionId M, IMyTerminalBlock W, int f = 0)
		{
			return (double)W.GetInventory(f).GetItemAmount(M); ;
		}
		IMyTerminalBlock d(MyDefinitionId M, bool Z = false, IMyTerminalBlock Y = null)
		{
			try { if (ʑ.GetInventory(0).FindItem(M) != null && ʑ != Y) { return ʑ; } } catch { }
			foreach (var X in ʌ)
			{
				if (M.SubtypeId.ToString() == "Ice" && X
.GetType().ToString().Contains("MyGasGenerator")) continue; if (X.GetInventory(0).FindItem(M) != null) { ʑ = X; return X; }
			}
			if (Z)
			{
				foreach (var X in ʕ) { if (Y != null) { if (Í(X) <= Í(Y)) continue; } if (X.GetInventory(0).FindItem(M) != null) { return X; } }
			}
			return null;
		}
		IMyTerminalBlock e(IMyTerminalBlock T, List<IMyTerminalBlock> S)
		{
			IMyTerminalBlock V = null; V = U(T, S); if (V != null) return V; V = U(T, ʊ); if (V == null)
				ƹ("'" + T.CustomName + "'\nhas no empty containers to move its items!"); return V;
		}
		IMyTerminalBlock U(IMyTerminalBlock T, List<
IMyTerminalBlock> S)
		{
			var R = T.GetInventory(0); foreach (var X in S)
			{
				if (X == T) continue; var Q = X.GetInventory(0); if ((float)Q.CurrentVolume < (
float)Q.MaxVolume * 0.95f) { if (!X.GetInventory(0).IsConnectedTo(R)) continue; return X; }
			}
			return null;
		}
		int Í(IMyTerminalBlock W)
		{
			string ç = System.Text.RegularExpressions.Regex.Match(W.CustomName, @"\[p(\d+|max|min)\]", System.Text.RegularExpressions.
			RegexOptions.IgnoreCase).Groups[1].Value.ToLower(); int æ = 0; bool å = true; if (ç == "max") { æ = int.MinValue; }
			else if (ç == "min")
			{
				æ = int.MaxValue
;
			}
			else { å = Int32.TryParse(ç, out æ); }
			if (!å)
			{
				string Ç = W.IsSameConstructAs(Me) ? "" : "1"; Int32.TryParse(Ç + W.EntityId.ToString().
Substring(0, 4), out æ);
			}
			return æ;
		}
		string ä(string á)
		{
			ã(); var Þ = Storage.Split('\n'); foreach (var Ð in Þ)
			{
				if (Ð.Contains(á))
				{
					return Ð.
Replace(á + "=", "");
				}
			}
			return "";
		}
		void â(string á, string à = "")
		{
			ã(); var Þ = Storage.Split('\n'); string ß = ""; foreach (var Ð in Þ)
			{
				if (Ð.
Contains(á)) { ß += á + "=" + à + "\n"; }
				else { ß += Ð + "\n"; }
			}
			Storage = ß.TrimEnd('\n');
		}
		void ã()
		{
			var Þ = Storage.Split('\n'); if (Þ.Length != ɩ.Count)
			{ string ß = ""; foreach (var Ë in ɩ) { ß += Ë.Key + "=" + Ë.Value + "\n"; } Storage = ß.TrimEnd('\n'); }
		}
		void î(IMyTerminalBlock X)
		{
			foreach (
var í in Ǹ.Keys.ToList()) { Ǹ[í] = "0"; }
			List<string> ì = X.CustomData.Replace(" ", "").TrimEnd('\n').Split('\n').ToList(); ì.
RemoveAll(Ð => !Ð.Contains("=") || Ð.Length < 8); bool ë = false; foreach (var Ð in ì)
			{
				var ê = Ð.Split('='); if (!Ǹ.ContainsKey(ê[0]))
				{
					MyDefinitionId M; if (MyDefinitionId.TryParse(ȏ + ê[0], out M)) { Ü(M); ë = true; }
				}
				Ǹ[ê[0]] = ê[1];
			}
			if (ë) Ó(); List<string> é = new List<string>{
"Special Container modes:","","Positive number: stores wanted amount, removes excess (e.g.: 100)",
"Negative number: doesn't store items, only removes excess (e.g.: -100)","Keyword 'all': stores all items of that subtype (like a type container)",""}; foreach (var Ë in Ǹ)
			{
				é.Add(Ë.Key + "=" + Ë.
Value);
			}
			X.CustomData = string.Join("\n", é);
		}
		void è()
		{
			ʟ.Clear(); ʟ.AddRange(Ȥ); ʟ.AddRange(ȣ); ʟ.AddRange(Ȣ); ʟ.AddRange(ȡ); ʟ.
AddRange(Ƞ); ʟ.AddRange(ȝ); Ǹ.Clear(); foreach (var Ë in Ȥ) { Ǹ[Ȍ + "/" + Ë] = "0"; }
			foreach (var Ë in Ȧ) { Ǹ[Ȏ + "/" + Ë] = "0"; }
			foreach (var Ë in ȥ)
			{
				Ǹ[ȍ + "/" + Ë] = "0";
			}
			foreach (var Ë in ȣ) { Ǹ[ȋ + "/" + Ë] = "0"; }
			foreach (var Ë in Ȣ) { Ǹ[Ȋ + "/" + Ë] = "0"; }
			foreach (var Ë in ȡ)
			{
				Ǹ[Ȑ + "/" + Ë] =
"0";
			}
			foreach (var Ë in Ƞ) { Ǹ[ȉ + "/" + Ë] = "0"; }
			foreach (var Ë in ȟ) { Ǹ[Ț + "/" + Ë] = "0"; }
			foreach (var Ë in Ȟ) { Ǹ[Ȭ + "/" + Ë] = "0"; }
			foreach (
var Ë in ȝ) { Ǹ[Ȫ + "/" + Ë] = "0"; }
		}
		void Õ()
		{
			for (int H = ɨ; H < ɖ.Count; H++)
			{
				if (Ǔ()) return; if (ɨ >= ɖ.Count - 1) { ɨ = 0; } else { ɨ++; }
				var A = new
List<MyInventoryItem>(); ɖ[H].GetInventory(0).GetItems(A); foreach (var Ë in A)
				{
					MyDefinitionId M = Ë.Type; if (ȧ.Contains(M))
						continue; ɞ = "Found new item!\n" + M.SubtypeId.ToString() + " (" + M.TypeId.ToString().Replace(ȏ, "") + ")"; Î(M); Ü(M); Ư(M);
				}
			}
		}
		bool Ó()
		{
			Û();
			var Ò = Me.CustomData.Split('\n');
			GridTerminalSystem.GetBlocksOfType(ʁ);
			bool Ñ = false;
			foreach (var Ð in Ò)
			{
				var Ï = Ð.Split(';');
				if (Ï.Length < 2) continue;
				MyDefinitionId M;
				if (!MyDefinitionId.TryParse(Ï[0], out M)) continue;
				if (ʁ.Count == 0)
				{
					Ñ = true;
				}
				else
				{
					MyDefinitionId Ô;
					if (MyDefinitionId.TryParse(Ï[1], out Ô))
					{
						if (Ʊ(Ô))
						{
							ǡ(M, Ô);
						}
						else
						{
							Ʋ(M);
							continue;
						}
					}
				}
				Î(M);
				Ǡ(M);
			}
			if (Ñ) return false;
			return true;
		}
		void Î(MyDefinitionId M)
		{
			string K = M.TypeId.ToString().Replace(ȏ, ""); string J = M.SubtypeId.ToString(); if (K == Ȏ)
			{
				Ȧ.Add(J);
				Ƣ(J); if (!J.Contains("Ice")) { foreach (var Ý in ʅ) { if (Ý.GetInventory(0).CanItemsBeAdded(1, M)) { ȓ.Add(J); break; } } }
			}
			else if (K ==
ȍ) { ȥ.Add(J); }
			else if (K == Ȍ) { Ȥ.Add(J); }
			else if (K == ȋ) { ȣ.Add(J); }
			else if (K == Ȋ) { Ȣ.Add(J); }
			else if (K == Ȑ) { ȡ.Add(J); }
			else if (K == ȉ
) { Ƞ.Add(J); }
			else if (K == Ț) { ȟ.Add(J); } else if (K == Ȭ) { Ȟ.Add(J); } else if (K == Ȫ) { ȝ.Add(J); }
		}
		void Ü(MyDefinitionId M)
		{
			Û(); var Ò =
Me.CustomData.Split('\n').ToList(); foreach (var Ð in Ò) { try { if (Ð.Substring(0, Ð.IndexOf(";")) == M.ToString()) return; } catch { } }
			for (int H = Ò.Count - 1; H >= 0; H--) { if (Ò[H].Contains(";")) { Ò.Insert(H + 1, M + ";noBP"); break; } }
			Me.CustomData = String.Join("\n", Ò); Ǡ(M)
;
		}
		void Û() { if (!Me.CustomData.Contains(ɫ)) { Me.CustomData = (Ƿ ? Ƕ : ǵ) + ɫ; } }
		void Ú()
		{
			if (Ȗ != null)
			{
				var A = new List<MyInventoryItem>(
); Ȗ.GetInventory(1).GetItems(A); var Ù = new List<MyProductionItem>(); Ȗ.GetQueue(Ù); if (A.Count == 0) return; Ȗ.CustomName = ȕ;
				MyDefinitionId Ô = Ù[0].BlueprintId; MyDefinitionId M = A[0].Type; if (A.Count == 1 && Ù.Count == 1 && Ȗ.Mode == MyAssemblerMode.Assembly && Ô == Ȕ)
				{
					if (ȕ.
Contains(learnKeyword) && !ȕ.Contains(learnManyKeyword)) Ȗ.CustomName = ȕ.Replace(" " + learnKeyword, "").Replace(learnKeyword + " ", ""); Ȗ
.ClearQueue(); Ȕ = new MyDefinitionId(); ɞ = "Learned new Blueprint!\n'" + Ô.ToString().Replace(ȏ, "") + "'\nproduces: '" + M.ToString
().Replace(ȏ, "") + "'"; Ǡ(M); Î(M); ǡ(M, Ô); Ü(M); ƫ(M, Ô); µ(Ȗ); Ȗ = null; return;
				}
				else if (Ù.Count != 1)
				{
					ƹ(
"Blueprint learning aborted!\nExactly 1 itemstack in the queue is needed to learn new recipes!");
				}
			}
			Ȗ = null; Ȕ = new MyDefinitionId(); foreach (var Ø in ɿ)
			{
				var Ù = new List<MyProductionItem>(); Ø.GetQueue(Ù); if (Ù.Count == 1 && Ø.
Mode == MyAssemblerMode.Assembly)
				{
					if (!µ(Ø)) return; Ȗ = Ø; Ȕ = Ù[0].BlueprintId; ȕ = Ø.CustomName; Ø.CustomName = "Learning " + Ȕ.SubtypeName
+ " in: " + Ø.CustomName; return;
				}
			}
		}
		bool µ(IMyAssembler Ø)
		{
			if (Ø.GetInventory(1).ItemCount != 0)
			{
				IMyTerminalBlock Y = e(Ø, ʙ); if (Y
!= null) { Æ("", Ø, 1, Y, 0); return true; }
				else
				{
					ƹ(
"Can't learn blueprint!\nNo free containers to clear the output inventory found!"); return false;
				}
			}
			return true;
		}
		bool Ʊ(MyDefinitionId Ô)
		{
			try { foreach (var Ø in ʁ) { if (Ø.CanUseBlueprint(Ô)) return true; } }
			catch { return false; }
			return false;
		}
		void Ư(MyDefinitionId M)
		{
			if (ʁ.Count == 0) return; if (M.TypeId.ToString() == ȏ + Ȏ || M.TypeId.
ToString() == ȏ + ȍ) return; MyDefinitionId Ô; bool ƥ = Ǯ.TryGetValue(M, out Ô); if (ƥ) ƥ = Ʊ(Ô); if (!ƥ)
			{
				var ư = new List<string>{"BP","",
"Component","Magazine","_Blueprint"}; bool Ʈ = false; foreach (var ƭ in ư)
				{
					string Ƭ = ȩ + M.SubtypeId.ToString().Replace("Item", "") + ƭ;
					MyDefinitionId.TryParse(Ƭ, out Ô); Ʈ = Ʊ(Ô); if (Ʈ) { ǡ(M, Ô); ƫ(M, Ô); ƥ = true; return; }
				}
			}
		}
		void ƫ(MyDefinitionId M, MyDefinitionId Ô)
		{
			Û(); var Ò = Me.
CustomData.Split('\n'); for (var H = 0; H < Ò.Length; H++)
			{
				if (Ò[H].Substring(0, Ò[H].IndexOf(";")) != M.ToString()) continue; var Ï = Ò[H].Split(
';'); Ò[H] = Ï[0] + ";" + Ô.ToString(); Me.CustomData = String.Join("\n", Ò); return;
			}
		}
		void Ʋ(MyDefinitionId M)
		{
			Û(); var Ò = Me.CustomData
.Split('\n').ToList(); Ò.RemoveAll(H => H.Contains(M.ToString() + ";")); Me.CustomData = String.Join("\n", Ò);
		}
		void ƹ(string ń)
		{
			ɳ.
Add(ń); ɲ.Add(ń); Ŀ = ɳ.ElementAt(0);
		}
		void Ƹ()
		{
			Me.CustomData = ""; foreach (var X in ʕ)
			{
				List<string> Ò = X.CustomData.Replace(" ", "").
TrimEnd('\n').Split('\n').ToList(); Ò.RemoveAll(Ð => !Ð.Contains("=") || Ð.Contains("=0")); X.CustomData = string.Join("\n", Ò);
			}
			Echo(
"Stored items deleted!\n"); if (ʕ.Count > 0) Echo("Also deleted itemlists of " + ʕ.Count + " Special containers!\n"); Echo(
"Please hit 'Recompile'!\n\nScript stopped!");
		}
		void Ʒ()
		{
			Ȝ.Clear(); List<IMyTerminalBlock> ƶ = ʀ.ToList<IMyTerminalBlock>(); List<IMyTerminalBlock> Ƶ = ʋ.ToList<
IMyTerminalBlock>(); ƴ(ɖ, 0); ƴ(ƶ, 1); ƴ(Ƶ, 1);
		}
		void ƴ(List<IMyTerminalBlock> Ƴ, int f)
		{
			for (int H = 0; H < Ƴ.Count; H++)
			{
				var A = new List<
MyInventoryItem>(); Ƴ[H].GetInventory(f).GetItems(A); for (int ƺ = 0; ƺ < A.Count; ƺ++)
				{
					MyDefinitionId M = A[ƺ].Type; if (Ȝ.ContainsKey(M))
					{
						Ȝ[M] += (
double)A[ƺ].Amount;
					}
					else { Ȝ[M] = (double)A[ƺ].Amount; }
				}
			}
		}
		double G(MyDefinitionId M) { double Ź; Ȝ.TryGetValue(M, out Ź); return Ź; }
		void Ƣ(string Ơ) { if (!ȑ.ContainsKey(Ơ)) { ȑ[Ơ] = 0.5; } }
		double ơ(string Ơ)
		{
			double Ź; Ơ = Ơ.Replace(ȏ + Ȏ + "/", ""); ȑ.TryGetValue(Ơ, out Ź)
; return Ź != 0 ? Ź : 0.5;
		}
		void Ɵ()
		{
			ț.Clear(); foreach (IMyAssembler Ø in ʁ)
			{
				var Ù = new List<MyProductionItem>(); Ø.GetQueue(Ù); if (Ù
.Count > 0 && !Ø.IsProducing)
				{
					if (Ø.Mode == MyAssemblerMode.Assembly) ƹ("'" + Ø.CustomName +
"' has a queue but is currently not assembling!\nAre there enough ingots for the craft?"); if (Ø.Mode == MyAssemblerMode.Disassembly) ƹ("'" + Ø.CustomName +
   "' has a queue but is currently not disassembling!\nAre the items to disassemble missing?");
				}
				foreach (var Ë in Ù)
				{
					MyDefinitionId Ô = Ë.BlueprintId; if (ț.ContainsKey(Ô)) { ț[Ô] += (double)Ë.Amount; }
					else
					{
						ț[Ô] = (double)Ë.
Amount;
					}
				}
			}
		}
		double ƞ(MyDefinitionId Ô) { double Ź; ț.TryGetValue(Ô, out Ź); return Ź; }
		void Ɲ(MyDefinitionId M, double Á) { Ȉ[M] = Á; }
		double ƪ(MyDefinitionId Ô) { int Ź; if (!Ǽ.TryGetValue(Ô, out Ź)) Ź = 0; return Ź; }
		void Ʃ(MyDefinitionId M, int à) { Ǽ[M] = à; }
		double ƨ(
MyDefinitionId M, bool Ƨ = false)
		{ double Ź; if (!Ȉ.TryGetValue(M, out Ź) && Ƨ) Ź = 10000; return Ź; }
		MyDefinitionId Ʀ(MyDefinitionId M, out bool ƥ)
		{
			MyDefinitionId Ô; ƥ = Ǯ.TryGetValue(M, out Ô); return Ô;
		}
		MyDefinitionId Ƥ(MyDefinitionId Ô)
		{
			MyDefinitionId M; Ǻ.TryGetValue(Ô, out M); return
M;
		}
		bool ǋ(MyDefinitionId Ô) { return Ǻ.ContainsKey(Ô); }
		void ǡ(MyDefinitionId M, MyDefinitionId Ô) { Ǯ[M] = Ô; Ǻ[Ô] = M; }
		void Ǡ(
MyDefinitionId M)
		{ ȧ.Add(M); ǹ[M.SubtypeId.ToString()] = M; }
		MyDefinitionId ǟ(string J)
		{
			MyDefinitionId M = new MyDefinitionId(); ǹ.TryGetValue
(J, out M); return M;
		}
		StringBuilder ǝ(IMyTextSurface u, string Ê, double à, double ǜ, string Ǜ = null, string Ǟ = null, bool m = false,
bool Ǣ = false, string Ą = "")
		{
			string ÿ = à.ToString(); string ģ = ǜ.ToString(); if (Ǜ != null) { ÿ = Ǜ; }
			if (Ǟ != null) { ģ = Ǟ; }
			float ū = u.FontSize;
			float Ǭ = 0.61f; float ǫ = 1.01f; if (u.Font == "Monospace") { Ǭ = 0.41f; ǫ = 0.81f; }
			float ų = u.ŋ(); char Ǫ = ' '; float ǩ = u.ő(Ǫ); StringBuilder Ǩ =
new StringBuilder(" " + à.Ƅ(ǜ)); Ǩ = u.ō(Ǫ, u.ũ("9999.9%") - u.ũ(Ǩ)).Append(Ǩ); StringBuilder ǧ = new StringBuilder(ÿ + " / " + ģ);
			StringBuilder Ǧ = new StringBuilder(); StringBuilder ǥ = new StringBuilder(); StringBuilder Ǥ; if (ǜ == 0)
			{
				Ǧ.Append(Ą + Ê + " "); Ǥ = u.ō(Ǫ, ų - u.ũ(Ǧ) - u
.ũ(ÿ)); Ǧ.Append(Ǥ).Append(ÿ); return Ǧ.Append("\n");
			}
			double ǚ = 0; if (ǜ > 0) ǚ = à / ǜ >= 1 ? 1 : à / ǜ; if (Ǣ && !m)
			{
				if (ū < Ǭ || (ū < ǫ && ų > 512))
				{
					Ǧ.
Append(ǣ(u, ų * 0.25f, ǚ, Ą) + " " + Ê + " "); Ǥ = u.ō(Ǫ, ų * 0.75 - u.ũ(Ǧ) - u.ũ(ÿ + " /")); Ǧ.Append(Ǥ).Append(ǧ); Ǥ = u.ō(Ǫ, ų - u.ũ(Ǧ) - u.ũ(Ǩ)); Ǧ.Append(
Ǥ); Ǧ.Append(Ǩ);
				}
				else { Ǧ.Append(ǣ(u, ų * 0.3f, ǚ, Ą) + " " + Ê + " "); Ǥ = u.ō(Ǫ, ų - u.ũ(Ǧ) - u.ũ(Ǩ)); Ǧ.Append(Ǥ); Ǧ.Append(Ǩ); }
			}
			else
			{
				Ǧ.Append
(Ą + Ê + " "); if (ū < Ǭ || (ū < ǫ && ų > 512))
				{
					Ǥ = u.ō(Ǫ, ų * 0.5 - u.ũ(Ǧ) - u.ũ(ÿ + " /")); Ǧ.Append(Ǥ).Append(ǧ); Ǥ = u.ō(Ǫ, ų - u.ũ(Ǧ) - u.ũ(Ǩ)); Ǧ.Append
		 (Ǥ).Append(Ǩ); if (!m) { ǥ = ǣ(u, ų, ǚ, Ą).Append("\n"); }
				}
				else
				{
					Ǥ = u.ō(Ǫ, ų - u.ũ(Ǧ) - u.ũ(ǧ)); Ǧ.Append(Ǥ).Append(ǧ); if (!m)
					{
						ǥ = ǣ(u, ų - u.ũ(Ǩ
), ǚ, Ą); ǥ.Append(Ǩ).Append("\n");
					}
				}
			}
			return Ǧ.Append("\n").Append(ǥ);
		}
		StringBuilder ǣ(IMyTextSurface u, float Ŭ, double ǚ,
string Ą)
		{
			StringBuilder ǉ, ǈ; char Ǉ = '['; char ǆ = ']'; char ǅ = 'I'; char Ǆ = '∙'; float Ǌ = u.ő(Ǉ); float ǃ = u.ő(ǆ); float ǁ = 0; if (Ą != "") ǁ = u.ũ
							 (Ą); float ǀ = Ŭ - Ǌ - ǃ - ǁ; ǉ = u.ō(ǅ, ǀ * ǚ); ǈ = u.ō(Ǆ, ǀ - u.ũ(ǉ)); return new StringBuilder().Append(Ą).Append(Ǉ).Append(ǉ).Append(ǈ).
												   Append(ǆ);
		}
		StringBuilder ƿ = new StringBuilder("No performance Information available!"); Dictionary<string, int> ƾ = new Dictionary<
string, int>(); List<int> ƽ = new List<int>(new int[600]); List<double> Ƽ = new List<double>(new double[600]); double ǂ, ƻ, ǌ, Ǚ, ǘ; int Ǘ, ǖ =
			   0; void Ǖ(string ǔ)
		{
			ǖ = ǖ >= 599 ? 0 : ǖ + 1; Ǘ = Runtime.CurrentInstructionCount; if (Ǘ > ƻ) ƻ = Ǘ; ƽ[ǖ] = Ǘ; Ǚ = ƽ.Sum() / ƽ.Count; ƿ.Clear(); ƿ.
					   Append("Instructions: " + Ǘ + " / " + Runtime.MaxInstructionCount + "\n"); ƿ.Append("Max. Instructions: " + ƻ + " / " + Runtime.
								MaxInstructionCount + "\n"); ƿ.Append("Avg. Instructions: " + Math.Floor(Ǚ) + " / " + Runtime.MaxInstructionCount + "\n\n"); ǂ = Runtime.LastRunTimeMs; if
											   (ǂ > ǌ && ƾ.ContainsKey(ǔ)) ǌ = ǂ; Ƽ[ǖ] = ǂ; ǘ = Ƽ.Sum() / Ƽ.Count; ƿ.Append("Last runtime: " + Math.Round(ǂ, 4) + " ms\n"); ƿ.Append(
																	 "Max. runtime: " + Math.Round(ǌ, 4) + " ms\n"); ƿ.Append("Avg. runtime: " + Math.Round(ǘ, 4) + " ms\n\n"); ƿ.Append("Instructions per Method:\n"); ƾ[
																				  ǔ] = Ǘ; foreach (var Ë in ƾ.OrderByDescending(H => H.Value)) { ƿ.Append("- " + Ë.Key + ": " + Ë.Value + "\n"); }
			ƿ.Append("\n");
		}
		bool Ǔ(
double à = 10)
		{ return Runtime.CurrentInstructionCount > à * 1000; }
		List<IMyTerminalBlock> ǒ(string Ƌ, string[] Ǒ = null, string ǐ = "Debug",
float Ǐ = 0.6f, float ǎ = 2f)
		{
			string Ǎ = "[IsyLCD]"; var Ɯ = new List<IMyTerminalBlock>(); GridTerminalSystem.GetBlocksOfType<
IMyTextSurfaceProvider>(Ɯ, ŧ => ŧ.IsSameConstructAs(Me) && (ŧ.CustomName.Contains(Ƌ) || (ŧ.CustomName.Contains(Ǎ) && ŧ.CustomData.Contains(Ƌ)))); var ƀ =
   Ɯ.FindAll(ŧ => ŧ.CustomName.Contains(Ƌ)); foreach (var u in ƀ)
			{
				u.CustomName = u.CustomName.Replace(Ƌ, "").Replace(" " + Ƌ, "").
TrimEnd(' '); bool Ŧ = false; bool ť = false; int Ť = 0; if (u is IMyTextSurface)
				{
					if (!u.CustomName.Contains(Ǎ)) Ŧ = true; if (!u.CustomData.
Contains(Ƌ)) { ť = true; u.CustomData = "@0 " + Ƌ + (Ǒ != null ? "\n" + String.Join("\n", Ǒ) : ""); }
				}
				else if (u is IMyTextSurfaceProvider)
				{
					if (!u.
CustomName.Contains(Ǎ)) Ŧ = true; int ţ = (u as IMyTextSurfaceProvider).SurfaceCount; for (int H = 0; H < ţ; H++)
					{
						if (!u.CustomData.Contains("@" +
H)) { ť = true; Ť = H; u.CustomData += (u.CustomData == "" ? "" : "\n\n") + "@" + H + " " + Ƌ + (Ǒ != null ? "\n" + String.Join("\n", Ǒ) : ""); break; }
					}
				}
				else
				{ Ɯ.Remove(u); }
				if (Ŧ) u.CustomName += " " + Ǎ; if (ť)
				{
					var ĭ = (u as IMyTextSurfaceProvider).GetSurface(Ť); ĭ.Font = ǐ; ĭ.FontSize = Ǐ; ĭ.
TextPadding = ǎ; ĭ.Alignment = TextAlignment.LEFT; ĭ.ContentType = ContentType.TEXT_AND_IMAGE;
				}
			}
			return Ɯ;
		}
	}
	class Ţ : IComparer<MyDefinitionId>
	{
		public int Compare(MyDefinitionId Š, MyDefinitionId ş)
		{
			return Š.ToString().CompareTo(ş.
ToString());
		}
	}
	class š : IEqualityComparer<MyInventoryItem>
	{
		public bool Equals(MyInventoryItem Š, MyInventoryItem ş)
		{
			return Š.
ToString() == ş.ToString();
		}
		public int GetHashCode(MyInventoryItem Ë) { return Ë.ToString().GetHashCode(); }
	}
	public static partial
class Ş
	{
		private static Dictionary<char, float> Ũ = new Dictionary<char, float>(); public static void Ų(string Ű, float ů)
		{
			foreach (
char Ū in Ű) { Ũ[Ū] = ů; }
		}
		public static void Ů()
		{
			if (Ũ.Count > 0) return; Ų(
"3FKTabdeghknopqsuy£µÝàáâãäåèéêëðñòóôõöøùúûüýþÿāăąďđēĕėęěĝğġģĥħĶķńņňŉōŏőśŝşšŢŤŦũūŭůűųŶŷŸșȚЎЗКЛбдекруцяёђћўџ", 18); Ų("ABDNOQRSÀÁÂÃÄÅÐÑÒÓÔÕÖØĂĄĎĐŃŅŇŌŎŐŔŖŘŚŜŞŠȘЅЊЖф□", 22); Ų("#0245689CXZ¤¥ÇßĆĈĊČŹŻŽƒЁЌАБВДИЙПРСТУХЬ€", 20); Ų(
"￥$&GHPUVY§ÙÚÛÜÞĀĜĞĠĢĤĦŨŪŬŮŰŲОФЦЪЯжы†‡", 21); Ų("！ !I`ijl ¡¨¯´¸ÌÍÎÏìíîïĨĩĪīĮįİıĵĺļľłˆˇ˘˙˚˛˜˝ІЇії‹›∙", 9); Ų("？7?Jcz¢¿çćĉċčĴźżžЃЈЧавийнопсъьѓѕќ", 17); Ų(
"（）：《》，。、；【】(),.1:;[]ft{}·ţťŧț", 10); Ų("+<=>E^~¬±¶ÈÉÊË×÷ĒĔĖĘĚЄЏЕНЭ−", 19); Ų("L_vx«»ĹĻĽĿŁГгзлхчҐ–•", 16); Ų("\"-rª­ºŀŕŗř", 11); Ų("WÆŒŴ—…‰", 32); Ų("'|¦ˉ‘’‚", 7)
; Ų("@©®мшњ", 26); Ų("mw¼ŵЮщ", 28); Ų("/ĳтэє", 15); Ų("\\°“”„", 13); Ų("*²³¹", 12); Ų("¾æœЉ", 29); Ų("%ĲЫ", 25); Ų("MМШ", 27); Ų("½Щ", 30);
			Ų("ю", 24); Ų("ј", 8); Ų("љ", 23); Ų("ґ", 14); Ų("™", 31);
		}
		public static Vector2 ŭ(this IMyTextSurface ĭ, StringBuilder ń)
		{
			Ů();
			Vector2 Ŭ = new Vector2(); if (ĭ.Font == "Monospace") { float ū = ĭ.FontSize; Ŭ.X = (float)(ń.Length * 19.4 * ū); Ŭ.Y = (float)(28.8 * ū); return Ŭ; }
			else
			{
				float ū = (float)(ĭ.FontSize * 0.779); foreach (char Ū in ń.ToString()) { try { Ŭ.X += Ũ[Ū] * ū; } catch { } }
				Ŭ.Y = (float)(28.8 * ĭ.FontSize)
; return Ŭ;
			}
		}
		public static float ũ(this IMyTextSurface u, StringBuilder ń) { Vector2 ň = u.ŭ(ń); return ň.X; }
		public static float
ũ(this IMyTextSurface u, string ń)
		{ Vector2 ň = u.ŭ(new StringBuilder(ń)); return ň.X; }
		public static float ő(this
IMyTextSurface u, char Ő)
		{ float ŏ = ũ(u, new string(Ő, 1)); return ŏ; }
		public static int Ŏ(this IMyTextSurface u)
		{
			Vector2 Ŋ = u.SurfaceSize;
			float ŉ = u.TextureSize.Y; if (Ŋ.X < 512 || ŉ != Ŋ.Y) Ŋ.Y *= 512 / ŉ; float Ō = Ŋ.Y * (100 - u.TextPadding * 2) / 100; Vector2 ň = u.ŭ(new StringBuilder(
									   "T")); return (int)(Ō / ň.Y);
		}
		public static float ŋ(this IMyTextSurface u)
		{
			Vector2 Ŋ = u.SurfaceSize; float ŉ = u.TextureSize.Y; if (Ŋ
.X < 512 || ŉ != Ŋ.Y) Ŋ.X *= 512 / ŉ; return Ŋ.X * (100 - u.TextPadding * 2) / 100;
		}
		public static StringBuilder ō(this IMyTextSurface u, char
ŗ, double ŝ)
		{ int ś = (int)(ŝ / ő(u, ŗ)); if (ś < 0) ś = 0; return new StringBuilder().Append(ŗ, ś); }
		private static DateTime Ś = DateTime.
Now; private static Dictionary<int, List<int>> ř = new Dictionary<int, List<int>>(); public static StringBuilder Ř(this
IMyTextSurface u, StringBuilder ń, int Ŝ = 3, bool ł = true, int Ĺ = 0)
		{
			int Ŗ = u.GetHashCode(); if (!ř.ContainsKey(Ŗ))
			{
				ř[Ŗ] = new List<int> { 1, 3, Ŝ, 0 };
			}
			int ŕ = ř[Ŗ][0]; int Ŕ = ř[Ŗ][1]; int œ = ř[Ŗ][2]; int Œ = ř[Ŗ][3]; var ű = ń.ToString().TrimEnd('\n').Split('\n'); List<string> Ľ = new
						  List<string>(); if (Ĺ == 0) Ĺ = u.Ŏ(); float ų = u.ŋ(); StringBuilder Ą, Ö = new StringBuilder(); for (int H = 0; H < ű.Length; H++)
			{
				if (H < Ŝ || H < œ ||
Ľ.Count - œ > Ĺ || u.ũ(ű[H]) <= ų) { Ľ.Add(ű[H]); }
				else
				{
					try
					{
						Ö.Clear(); float ƒ, Ƒ; var Ɛ = ű[H].Split(' '); string Ə = System.Text.
RegularExpressions.Regex.Match(ű[H], @"\d+(\.|\:)\ ").Value; Ą = u.ō(' ', u.ũ(Ə)); foreach (var Ɠ in Ɛ)
						{
							ƒ = u.ũ(Ö); Ƒ = u.ũ(Ɠ); if (ƒ + Ƒ > ų)
							{
								Ľ.Add(Ö.
ToString()); Ö = new StringBuilder(Ą + Ɠ + " ");
							}
							else { Ö.Append(Ɠ + " "); }
						}
						Ľ.Add(Ö.ToString());
					}
					catch { Ľ.Add(ű[H]); }
				}
			}
			if (ł)
			{
				if (Ľ.Count > Ĺ)
				{
					if (DateTime.Now.Second != Œ)
					{
						Œ = DateTime.Now.Second; if (Ŕ > 0) Ŕ--; if (Ŕ <= 0) œ += ŕ; if (œ + Ĺ - Ŝ >= Ľ.Count && Ŕ <= 0) { ŕ = -1; Ŕ = 3; }
						if (œ <= Ŝ && Ŕ <= 0)
						{ ŕ = 1; Ŕ = 3; }
					}
				}
				else { œ = Ŝ; ŕ = 1; Ŕ = 3; }
				ř[Ŗ][0] = ŕ; ř[Ŗ][1] = Ŕ; ř[Ŗ][2] = œ; ř[Ŗ][3] = Œ;
			}
			else { œ = Ŝ; }
			StringBuilder Ǝ = new StringBuilder(); for (
var Ð = 0; Ð < Ŝ; Ð++) { Ǝ.Append(Ľ[Ð] + "\n"); }
			for (var Ð = œ; Ð < Ľ.Count; Ð++) { Ǝ.Append(Ľ[Ð] + "\n"); }
			return Ǝ;
		}
		public static Dictionary<
IMyTextSurface, string> ƌ(this IMyTerminalBlock W, string Ƌ, Dictionary<string, string> Ɗ = null)
		{
			var ƍ = new Dictionary<IMyTextSurface, string>(
); if (W is IMyTextSurface) { ƍ[W as IMyTextSurface] = W.CustomData; }
			else if (W is IMyTextSurfaceProvider)
			{
				var Ɖ = System.Text.
RegularExpressions.Regex.Matches(W.CustomData, @"@(\d).*(" + Ƌ + @")"); int Ɣ = (W as IMyTextSurfaceProvider).SurfaceCount; foreach (System.Text.
RegularExpressions.Match ƛ in Ɖ)
				{
					int ƙ = -1; if (int.TryParse(ƛ.Groups[1].Value, out ƙ))
					{
						if (ƙ >= Ɣ) continue; string Ò = W.CustomData; int Ƙ = Ò.IndexOf
("@" + ƙ); int Ɨ = Ò.IndexOf("@", Ƙ + 1) - Ƙ; string ħ = Ɨ <= 0 ? Ò.Substring(Ƙ) : Ò.Substring(Ƙ, Ɨ); ƍ[(W as IMyTextSurfaceProvider).
GetSurface(ƙ)] = ħ;
					}
				}
			}
			return ƍ;
		}
		public static bool Ɩ(this string ħ, string á)
		{
			var Ò = ħ.Replace(" ", "").Split('\n'); foreach (var Ð in Ò)
			{ if (Ð.StartsWith(á + "=")) { try { return Convert.ToBoolean(Ð.Replace(á + "=", "")); } catch { return true; } } }
			return true;
		}
		public
static string ƕ(this string ħ, string á)
		{
			var Ò = ħ.Replace(" ", "").Split('\n'); foreach (var Ð in Ò)
			{
				if (Ð.StartsWith(á + "="))
				{
					return
Ð.Replace(á + "=", "");
				}
			}
			return "";
		}
	}
	public static partial class Ş
	{
		public static bool ƚ(this double à, double ƈ, double ģ, bool
ž = false, bool Ž = false)
		{ bool ż = à >= ƈ; bool Ż = à <= ģ; if (Ž) ż = à > ƈ; if (ž) Ż = à < ģ; return ż && Ż; }
	}
	public static partial class Ş
	{
		public
static string ſ(this char ź, int Ÿ)
		{ if (Ÿ <= 0) { return ""; } return new string(ź, Ÿ); }
	}
	public static partial class Ş
	{
		public static
string ŷ(this string Ŷ, string ŵ, string Ŵ)
		{
			string Ź = System.Text.RegularExpressions.Regex.Replace(Ŷ, System.Text.
RegularExpressions.Regex.Escape(ŵ), Ŵ, System.Text.RegularExpressions.RegexOptions.IgnoreCase); return Ź;
		}
	}
	public static partial class Ş
	{
		public static string Ɓ(this float à)
		{
			string ƅ = "kL"; if (à < 1) { à *= 1000; ƅ = "L"; }
			else if (à >= 1000 && à < 1000000) { à /= 1000; ƅ = "ML"; }
			else if (
à >= 1000000 && à < 1000000000) { à /= 1000000; ƅ = "BL"; }
			else if (à >= 1000000000) { à /= 1000000000; ƅ = "TL"; }
			return Math.Round(à, 1) + " " + ƅ;
		}
		public static string Ɓ(this double à) { float Ƈ = (float)à; return Ƈ.Ɓ(); }
	}
	public static partial class Ş
	{
		public static string Ɔ(
this double à)
		{
			string ƅ = ""; if (à >= 1000 && à < 1000000) { à /= 1000; ƅ = " k"; }
			else if (à >= 1000000 && à < 1000000000) { à /= 1000000; ƅ = " M"; }
			else
if (à >= 1000000000) { à /= 1000000000; ƅ = " B"; }
			return Math.Round(à, 1) + ƅ;
		}
	}
	public static partial class Ş
	{
		public static string Ƅ(
this double ƃ, double ē)
		{ double Ƃ = Math.Round(ƃ / ē * 100, 1); if (ē == 0) { return "0%"; } else { return Ƃ + "%"; } }
		public static string Ƅ(this
float ƃ, float ē)
		{ double Ƃ = Math.Round(ƃ / ē * 100, 1); if (ē == 0) { return "0%"; } else { return Ƃ + "%"; } }

		//------------END--------------
	}
}