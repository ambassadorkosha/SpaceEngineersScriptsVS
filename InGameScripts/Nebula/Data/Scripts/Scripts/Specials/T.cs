/*using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Scripts.Specials.Systems
{

    [ProtoContract]
    public class VoxelSettings
    {
        [ProtoMember(1)]
        public float Chance = 0.33f;
        [ProtoMember(2)]
        public float[] Multipliers = new float[] { 0.33f, 0.5f, 0.75f, 1.25f, 1.5f, 2f, 3f };
        [ProtoMember(3)]
        public List<string> IgnoredVoxelMaterialSubtypes = new List<string>() { };
        [ProtoMember(4)]
        public List<string> IgnoredOres = new List<string>() { "Bacterial", "Stone" };

        public WeekManager WeekManager = new WeekManager();


        public bool NeedRegenerate(VoxelSettings settings)
        {
            if (settings == null) return true;
            if (!settings.IgnoredVoxelMaterialSubtypes.SequenceEqual(IgnoredVoxelMaterialSubtypes)) return true;
            if (!settings.IgnoredOres.SequenceEqual(IgnoredOres)) return true;
            return false;
        }

        internal bool CanUse(MyVoxelMaterialDefinition x)
        {
            if (IgnoredOres.Contains(x.MinedOre)) return false;
            if (IgnoredVoxelMaterialSubtypes.Contains(x.Id.SubtypeName)) return false;
            return true;
        }
    }

    [ProtoContract]
    public class BlueprintSettings
    {
        [ProtoMember(1)]
        public float Chance = 0.33f;
        [ProtoMember(2)]
        public float[] Multipliers = new float[] { 0.33f, 0.5f, 0.75f, 1.25f, 1.5f, 2f, 3f };
        [ProtoMember(3)]
        public List<string> IgnoredSubtypes = new List<string>() { };
        [ProtoMember(4)]
        public List<string> IgnoreWithSubtypesInResult = new List<string>() { };
        [ProtoMember(5)]
        public List<string> IgnoreClasses = new List<string>() { "LargeBlocks", "SmallBlocks", "Tools", "BasicTools", "EliteTools", "OxygenBottles" };
        [ProtoMember(6)]
        public string[] TakeBlueprintsFromBlocks = new string[] { "MyObjectBuilder_Assembler/LargeAssembler", "MyObjectBuilder_Refinery/LargeRefinery" };

        public WeekManager WeekManager = new WeekManager();

        public bool NeedRegenerate(BlueprintSettings settings)
        {
            if (settings == null) return true;
            if (!settings.IgnoreClasses.SequenceEqual(IgnoreClasses)) return true;
            if (!settings.IgnoreWithSubtypesInResult.SequenceEqual(IgnoreWithSubtypesInResult)) return true;
            if (!settings.TakeBlueprintsFromBlocks.SequenceEqual(TakeBlueprintsFromBlocks)) return true;
            if (!settings.IgnoredSubtypes.SequenceEqual(IgnoredSubtypes)) return true;
            return false;
        }

        public bool CanUse(MyBlueprintDefinitionBase b)
        {
            if (!b.Enabled) return false;
            if (IgnoredSubtypes.Contains(b.Id.SubtypeName)) return false;
            if (IgnoreWithSubtypesInResult.Count > 0)
            {
                List<MyDefinitionId> ignored = new List<MyDefinitionId>();
                foreach (var x in IgnoreWithSubtypesInResult)
                {
                    var type = MyDefinitionId.Parse(x);
                    ignored.Add(type);
                }

                foreach (var r in b.Results)
                {
                    foreach (var x in ignored)
                    {
                        if (r.Id.Equals(x)) return false;
                    }
                }
            }

            return true;
        }
    }

    public class WeekManager
    {
        public static DayOfWeekSettings DEFAULT = new DayOfWeekSettings();
        public bool Enabled = false;
        public List<DayOfWeekSettings> Settings = new List<DayOfWeekSettings>();


        public DayOfWeekSettings GetToday()
        {
            if (!Enabled) return DEFAULT;
            var dayofweek = Ext.DayOfWeek(DateTime.Now);
            foreach (var x in Settings)
            {
                if (x.DayOfWeek == dayofweek) return x;
            }

            return DEFAULT;
        }
    }

    public class DayOfWeekSettings
    {
        public int DayOfWeek = 1;

        public float ForAllMultiplier = 1f;
        public float RolledMultiplier = 1f;
        public float ChanceMultiplier = 1f;

        public int RerollsExtra = 0;
        public bool RerollForBetterBonus = true;
    }



    [ProtoContract]
    public class Settings
    {
        [ProtoMember(1)]
        public BlueprintSettings Blueprints = new BlueprintSettings();
        [ProtoMember(2)]
        public VoxelSettings VoxelMaterials = new VoxelSettings();

        public bool NeedRegenerate(Settings settings)
        {
            if (settings == null) return true;
            if (settings.Blueprints.NeedRegenerate(Blueprints)) return true;
            if (settings.VoxelMaterials.NeedRegenerate(VoxelMaterials)) return true;
            return false;

        }

        public bool Debug = false;
        public int RegenerateOffsetInSeconds = 0;
        public int RegenerateIntervalInSeconds = 24 * 60 * 60;
        public int RegenerateSeed = new Random().Next(1000);

        public int version = 1;

        internal void Fix()
        {
            for (var x = 0; x < Blueprints.Multipliers.Length; x++)
            {
                if (Blueprints.Multipliers[x] < 0.001f)
                {
                    Blueprints.Multipliers[x] = 0.001f;
                }
            }

            for (var x = 0; x < VoxelMaterials.Multipliers.Length; x++)
            {
                if (VoxelMaterials.Multipliers[x] < 0.001f)
                {
                    VoxelMaterials.Multipliers[x] = 0.001f;
                }
            }
        }
    }

    [ProtoContract]
    public class Difference
    {
        [ProtoMember(1)]
        public Dictionary<string, float> voxels = new Dictionary<string, float>();
        [ProtoMember(2)]
        public Dictionary<string, float> blueprints = new Dictionary<string, float>();

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var x in voxels)
            {
                sb.Append(x.Key).Append("->").Append(x.Value).AppendLine();
            }

            foreach (var x in blueprints)
            {
                sb.Append(x.Key).Append("->").Append(x.Value).AppendLine();
            }
            return sb.ToString();
        }
    }

    [ProtoContract]
    public class Request
    {
        [ProtoMember(1)]
        public ulong player;
    }

    [ProtoContract]
    public class Response
    {
        [ProtoMember(1)]
        public Settings settings;
        [ProtoMember(2)]
        public Difference diff;
    }

    public enum BlueprintType
    {
        OreToIngot,
        OresToIngots,
        OresToOres,

        IngotsToIngots,
        IngotsToComponent,
        IngotsToComponents,

        ComponentsToComponents,

        OtherToTools,
        IngotsToTools,

        Other
    }

    public static class Ext
    {

        static readonly MyObjectBuilderType COMPONENT = MyObjectBuilderType.Parse("MyObjectBuilder_Component");
        static readonly MyObjectBuilderType ORE = MyObjectBuilderType.Parse("MyObjectBuilder_Ore");
        static readonly MyObjectBuilderType INGOT = MyObjectBuilderType.Parse("MyObjectBuilder_Ingot");
        static readonly MyObjectBuilderType TOOL = MyObjectBuilderType.Parse("MyObjectBuilder_PhysicalGunObject");
        static readonly MyObjectBuilderType TOOL2 = MyObjectBuilderType.Parse("MyObjectBuilder_OxygenContainerObject");

        public static int DayOfWeek(DateTime time) //Saturday = 6, Sunday = 7
        {
            var utcZero = new DateTime(1970, 1, 1);
            if (time < utcZero) return 1;
            var dd = (int)(time - utcZero).TotalDays;
            dd = dd - (dd / 7) * 7 + 4; //1970 was Thursday
            if (dd > 7) dd -= 7;
            return dd;
        }

        public static float Roll(this Random r, ref float[] values, int times = 1, bool forHigher = true)
        {
            float v = values[r.Next(values.Length)];
            times--;

            while (times != 0)
            {
                var v2 = values[r.Next(values.Length)];
                v = forHigher ? Math.Max(v, v2) : Math.Min(v, v2);
                times--;
            }

            return v;
        }

        public static BlueprintType GetBlueprintType(this MyBlueprintDefinitionBase b)
        {
            var hasInputOres = false;
            var hasInputIngots = false;
            var hasInputComponents = false;
            var hasInputOther = false;

            var hasOutputOres = false;
            var hasOutputIngots = false;
            var hasOutputComponents = false;
            var hasOutputTools = false;
            var hasOutputOther = false;

            foreach (var r in b.Prerequisites)
            {
                if (r.Id.TypeId == COMPONENT)
                {
                    hasInputComponents = true;
                    continue;
                }
                if (r.Id.TypeId == ORE)
                {
                    hasInputOres = true;
                    continue;
                }
                if (r.Id.TypeId == INGOT)
                {
                    hasInputIngots = true;
                    continue;
                }

                hasInputOther = true;
            }

            foreach (var r in b.Results)
            {
                if (r.Id.TypeId == COMPONENT)
                {
                    hasOutputComponents = true;
                    continue;
                }
                if (r.Id.TypeId == TOOL || r.Id.TypeId == TOOL2)
                {
                    hasOutputTools = true;
                    continue;
                }
                if (r.Id.TypeId == ORE)
                {
                    hasOutputOres = true;
                    continue;
                }
                if (r.Id.TypeId == INGOT)
                {
                    hasOutputIngots = true;
                    continue;
                }

                hasOutputOther = true;
            }

            var i = (hasInputOres ? 1 : 0) + (hasInputIngots ? 1 : 0) + (hasInputComponents ? 1 : 0) + (hasInputOther ? 1 : 0);
            var o = (hasOutputOres ? 1 : 0) + (hasOutputIngots ? 1 : 0) + (hasOutputComponents ? 1 : 0) + (hasOutputTools ? 1 : 0) + (hasOutputOther ? 1 : 0);

            if (i != 1) return BlueprintType.Other;
            if (o != 1) return BlueprintType.Other;

            if (hasOutputTools) return hasInputIngots ? BlueprintType.IngotsToTools : BlueprintType.OtherToTools;
            if (hasInputOres && hasOutputIngots) return b.Results.Length == 1 && b.Prerequisites.Length == 1 ? BlueprintType.OreToIngot : BlueprintType.OresToIngots;
            if (hasInputIngots && hasOutputComponents) return b.Results.Length > 1 ? BlueprintType.IngotsToComponents : BlueprintType.IngotsToComponent;
            if (hasInputOres && hasOutputOres) return BlueprintType.OresToOres;
            if (hasInputIngots && hasOutputIngots) return BlueprintType.IngotsToIngots;
            if (hasInputComponents && hasOutputComponents) return BlueprintType.ComponentsToComponents;
            if (hasOutputTools) return BlueprintType.IngotsToTools;

            return BlueprintType.Other;
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class DailyEffects : MySessionComponentBase
    {
        const string FILE = "DailyEffects.xml";

        private static DailyEffects Instance;

        private static Dictionary<MyVoxelMaterialDefinition, float> defaultVoxels;
        private static Dictionary<MyBlueprintDefinitionBase, float> defaultBlueprints;
        private static Dictionary<string, string> oreSubtypeIdToMinedOre = new Dictionary<string, string>();
        private static Dictionary<string, BlueprintType> blueprintType = new Dictionary<string, BlueprintType>();
        private static Dictionary<string, MyBlueprintDefinitionBase> idToblueprint = new Dictionary<string, MyBlueprintDefinitionBase>();

        private static byte[] cached_response_data = null;

        private int seed = -1;
        private int timer = 0;
        private static Settings settings = null;
        private static Difference difference = null;

        private bool requested = false;

        public DailyEffects()
        {
            Instance = this;
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {


            if (MyAPIGateway.Session.IsServer)
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(FILE, typeof(DailyEffects)))
                {
                    var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(FILE, typeof(DailyEffects));
                    settings = new Settings();

                    var sat = new DayOfWeekSettings()
                    {
                        DayOfWeek = 6,
                        ForAllMultiplier = 2f,
                        RerollForBetterBonus = true,
                        RolledMultiplier = 0,
                        RerollsExtra = 0,
                        ChanceMultiplier = 0f
                    };

                    var sun = new DayOfWeekSettings()
                    {
                        DayOfWeek = 7,
                        ForAllMultiplier = 1f,
                        RerollForBetterBonus = true,
                        RolledMultiplier = 1f,
                        RerollsExtra = 1,
                        ChanceMultiplier = 1000f
                    };

                    settings.Blueprints.WeekManager.Settings.Add(sat);
                    settings.VoxelMaterials.WeekManager.Settings.Add(sun);

                    settings.Blueprints.WeekManager.Enabled = false;
                    settings.VoxelMaterials.WeekManager.Enabled = false;


                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
                    writer.Flush();
                    writer.Close();
                }
                else
                {
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(FILE, typeof(DailyEffects));
                    var file = reader.ReadToEnd();
                    reader.Close();
                    settings = MyAPIGateway.Utilities.SerializeFromXML<Settings>(file);

                    settings.Fix();
                }

                StoreDefaultValues(settings);
                MyAPIGateway.Multiplayer.RegisterMessageHandler(43501, OnRequestMessage);
            }
            else
            {
                MyAPIGateway.Multiplayer.RegisterMessageHandler(43500, OnMessageFromServer);
            }

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            if (requested || MyAPIGateway.Session.IsServer)
            {
                return;
            }

            if (MyAPIGateway.Session.Player != null)
            {
                requested = true;
                Request();
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
            if (!MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(43500, OnMessageFromServer);
            }
            else
            {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(43501, OnRequestMessage);
            }
        }

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (messageText.ToLower().StartsWith("!production stats"))
            {
                sendToOthers = false;
                if (difference == null)
                {
                    MyVisualScriptLogicProvider.SendChatMessage("Default stats", "Economy", MyAPIGateway.Session.Player.IdentityId);
                }
                else if (settings == null)
                {
                    MyVisualScriptLogicProvider.SendChatMessage("Waiting for information", "Economy", MyAPIGateway.Session.Player.IdentityId);
                }
                else
                {
                    GenerateText(difference);
                }
            }
        }

        private void Session_OnSessionReady()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                UpdateSeed();
            }
            
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (timer % 1000 == 1 && MyAPIGateway.Session.IsServer)
            {
                UpdateSeed();
            }
            timer++;
        }

        public void UpdateSeed()
        {
            var time = (DateTime.Now - new DateTime(2020, 1, 1, 0, 0, 0, 0)).TotalSeconds + settings.RegenerateOffsetInSeconds;
            var newvalue = (int)(time / settings.RegenerateIntervalInSeconds) + settings.RegenerateSeed;
            if (newvalue != seed)
            {
                seed = newvalue;
                difference = Generate(seed, settings);
                ApplyDifference(difference);

                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    GenerateText(difference);
                }

                var response = new Response();
                response.diff = difference;
                response.settings = settings;
                cached_response_data = MyAPIGateway.Utilities.SerializeToBinary(response);

                MyAPIGateway.Multiplayer.SendMessageToOthers(43500, cached_response_data);
            }
        }

        public void Request()
        {
            var data = MyAPIGateway.Utilities.SerializeToBinary(new Request { player = MyAPIGateway.Session.Player.SteamUserId });
            MyAPIGateway.Multiplayer.SendMessageToServer(43501, data);
        }

        public static void OnRequestMessage(byte[] data)
        {
            try
            {
                var request = MyAPIGateway.Utilities.SerializeFromBinary<Request>(data);
                if (request != null)
                {
                    MyAPIGateway.Multiplayer.SendMessageTo(43500, cached_response_data, request.player);
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.SendChatMessage(e.ToString(), "Economy system", MyAPIGateway.Session.Player.IdentityId);
            }

        }

        private static void ReturnValuesToDefault()
        {
            if (defaultVoxels != null)
            {
                foreach (var x in defaultVoxels)
                {
                    x.Key.MinedOreRatio = x.Value;
                }
            }

            if (defaultBlueprints != null)
            {
                foreach (var x in defaultBlueprints)
                {
                    x.Key.BaseProductionTimeInSeconds = x.Value;
                }
            }

        }



        public static void OnMessageFromServer(byte[] data)
        {
            try
            {
                var response = MyAPIGateway.Utilities.SerializeFromBinary<Response>(data);
                if (response != null)
                {
                    if (response.settings.NeedRegenerate(settings))
                    {
                        ReturnValuesToDefault();
                        settings = response.settings;
                        StoreDefaultValues(settings);
                    }

                    difference = response.diff;
                    ApplyDifference(response.diff);
                    GenerateText(response.diff);
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.SendChatMessage(e.ToString(), "Economy system", MyAPIGateway.Session.Player.IdentityId);
            }
        }



        private static void StoreDefaultValues(Settings settings)
        {
            defaultVoxels = new Dictionary<MyVoxelMaterialDefinition, float>();
            defaultBlueprints = new Dictionary<MyBlueprintDefinitionBase, float>();
            var voxelMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Where((x) => settings.VoxelMaterials.CanUse(x));
            var blueprints = new HashSet<MyBlueprintDefinitionBase>();

            foreach (var x in settings.Blueprints.TakeBlueprintsFromBlocks)
            {
                List<MyBlueprintClassDefinition> classes = null;
                MyCubeBlockDefinition blockdef;
                if (!MyDefinitionManager.Static.TryGetCubeBlockDefinition(MyDefinitionId.Parse(x), out blockdef))
                {
                    continue;
                }
                var prodBlock = blockdef as MyProductionBlockDefinition;
                if (prodBlock != null) classes = prodBlock.BlueprintClasses;

                if (classes != null)
                {
                    foreach (var c in classes)
                    {
                        if (!c.Enabled) continue;
                        if (settings.Blueprints.IgnoreClasses.Contains(c.Id.SubtypeName))
                        {
                            if (settings.Debug)
                            {
                                MyLog.Default.WriteLine($"DailyEvents: Ignoring BlueprintClass [{c.Id.SubtypeName}] from Block [{x}]");
                            }
                            continue;
                        }
                        foreach (var b in c)
                        {
                            if (blueprints.Contains(b)) continue;
                            if (!settings.Blueprints.CanUse(b)) continue;

                            if (settings.Debug)
                            {
                                MyLog.Default.WriteLine($"DailyEvents: Block [{x}] BlueprintSubtype [{b.Id.SubtypeName}] BlueprintClassSubtype [{c.Id.SubtypeName}] Type[{b.GetBlueprintType()}] Info [{b.ToString()}]");
                            }

                            blueprints.Add(b);
                        }
                    }
                }
            }

            foreach (var x in voxelMaterials)
            {
                defaultVoxels[x] = x.MinedOreRatio;
                oreSubtypeIdToMinedOre[x.Id.SubtypeName] = x.MinedOre;
            }

            foreach (var x in blueprints)
            {
                defaultBlueprints[x] = x.BaseProductionTimeInSeconds;
                blueprintType[x.Id.SubtypeName] = x.GetBlueprintType();
                idToblueprint[x.Id.SubtypeName] = x;
            }
        }

        private static float GetMultiplier(Random r, float Chance, ref float[] multipliers, DayOfWeekSettings settings)
        {
            if (r.NextDouble() < Chance * settings.ChanceMultiplier)
            {
                return settings.RolledMultiplier * r.Roll(ref multipliers, 1 + settings.RerollsExtra, settings.RerollForBetterBonus);
            }
            else
            {
                return settings.ForAllMultiplier;
            }
        }

        private static int GenerateHash(MyBlueprintDefinitionBase b)
        {
            long hash = 0;
            foreach (var x in b.Prerequisites)
            {
                hash += x.Id.GetHashCode();
            }

            hash += 1;

            foreach (var x in b.Results)
            {
                hash += x.Id.GetHashCode();
            }

            return (int)hash;
        }

        private static Difference Generate(int seed, Settings settings)
        {
            var vsettings = settings.VoxelMaterials.WeekManager.GetToday();
            var bsettings = settings.Blueprints.WeekManager.GetToday();
            var diff = new Difference();
            foreach (var x in defaultVoxels)
            {
                var r = new Random(x.Key.MinedOre.GetHashCode() + seed);
                var v = GetMultiplier(r, settings.VoxelMaterials.Chance, ref settings.VoxelMaterials.Multipliers, vsettings);
                if (v != 1f)
                {
                    diff.voxels[x.Key.Id.SubtypeName] = v;
                }
            }

            foreach (var x in defaultBlueprints)
            {
                var r = new Random(GenerateHash(x.Key) + seed);

                var v = GetMultiplier(r, settings.Blueprints.Chance, ref settings.Blueprints.Multipliers, vsettings);
                if (v != 1f)
                {
                    diff.blueprints[x.Key.Id.SubtypeName] = v;
                }
            }
            return diff;
        }

        private static void ApplyDifference(Difference diff)
        {
            foreach (var def in defaultVoxels)
            {
                var key = def.Key.Id.SubtypeName;
                def.Key.MinedOreRatio = def.Value * (diff.voxels.ContainsKey(key) ? diff.voxels[def.Key.Id.SubtypeName] : 1);
            }

            foreach (var def in defaultBlueprints)
            {
                var key = def.Key.Id.SubtypeName;
                var value = def.Value;
                if (diff.blueprints.ContainsKey(key))
                {
                    value *= 1 / diff.blueprints[def.Key.Id.SubtypeName];
                }
                def.Key.BaseProductionTimeInSeconds = value;
            }
        }

        private static void GenerateText(Difference diff)
        {
            if (diff.blueprints.Count == 0 && diff.voxels.Count == 0)
            {
                MyVisualScriptLogicProvider.SendChatMessage("All values were set to default", "Economy", MyAPIGateway.Session.Player.IdentityId);
                return;
            }

            var sb = new StringBuilder();
            if (diff.voxels.Count != 0)
            {
                sb.Clear();
                sb.AppendLine("Mining Yield Changes");

                var changes = new SortedDictionary<float, HashSet<string>>();
                foreach (var x in diff.voxels)
                {
                    HashSet<string> set;
                    if (!changes.ContainsKey(x.Value))
                    {
                        set = new HashSet<string>();
                        changes.Add(x.Value, set);
                    }
                    else
                    {
                        set = changes[x.Value];
                    }

                    var minedOre = oreSubtypeIdToMinedOre.ContainsKey(x.Key) ? oreSubtypeIdToMinedOre[x.Key] : x.Key;
                    set.Add(minedOre);
                }

                foreach (var x in changes)
                {
                    sb.Append($"{(int)(x.Key * 100)}% :");
                    foreach (var y in x.Value)
                    {
                        sb.Append(" ").Append(y);
                    }
                    sb.AppendLine();
                }

                MyVisualScriptLogicProvider.SendChatMessage(sb.ToString(), "Economy", MyAPIGateway.Session.Player.IdentityId);
            }

            if (diff.blueprints.Count != 0)
            {
                var changesRefinery = new SortedDictionary<float, HashSet<string>>();
                var changesAssembler = new SortedDictionary<float, HashSet<string>>();

                foreach (var x in diff.blueprints)
                {
                    var type = blueprintType[x.Key];
                    var changes = (BlueprintType.OreToIngot | BlueprintType.OresToIngots).HasFlag(type) ? changesRefinery : changesAssembler;
                    var key = x.Value;

                    HashSet<string> set;
                    if (!changes.ContainsKey(key))
                    {
                        set = new HashSet<string>();
                        changes.Add(key, set);
                    }
                    else
                    {
                        set = changes[key];
                    }


                    if (!idToblueprint.ContainsKey(x.Key))
                    {
                        MyVisualScriptLogicProvider.SendChatMessage("Not present: " + x.Key);
                        set.Add(x.Key);
                        continue;
                    }

                    if (idToblueprint[x.Key].Results.Length == 1)
                    {
                        set.Add(idToblueprint[x.Key].Results[0].Id.SubtypeName);
                    }
                    else
                    {
                        set.Add(x.Key);
                    }
                }

                if (changesRefinery.Count > 0)
                {
                    sb.Clear();
                    sb.AppendLine("Refine Speed Changes");
                    foreach (var x in changesRefinery)
                    {
                        sb.Append($"{(int)(x.Key * 100)}% : ");
                        foreach (var y in x.Value)
                        {
                            sb.Append(" ").Append(y);
                        }
                        sb.AppendLine();
                    }

                    MyVisualScriptLogicProvider.SendChatMessage(sb.ToString(), "Economy", MyAPIGateway.Session.Player.IdentityId);
                }

                if (changesAssembler.Count > 0)
                {
                    sb.Clear();
                    sb.AppendLine("Assemble Speed Changes");
                    foreach (var x in changesAssembler)
                    {
                        sb.Append($"{(int)(x.Key * 100)}% : ");
                        foreach (var y in x.Value)
                        {
                            sb.Append(" ").Append(y);
                        }
                        sb.AppendLine();
                    }

                    MyVisualScriptLogicProvider.SendChatMessage(sb.ToString(), "Economy", MyAPIGateway.Session.Player.IdentityId);
                }
            }
        }
    }
}
*/