using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;
using VRage;
using Digi;
using VRage.ObjectBuilders;
using Sandbox.ModAPI;
using Scripts.Specials.AutoTools;
using Sandbox.Game;
using VRageMath;
using VRage.Game.Entity;
using Sandbox.Game.Entities;
using Slime;
using VRage.Game.ModAPI;
using static VRage.Game.ModAPI.Ingame.MyInventoryItemExtension;

//using VRage.Game.ModAPI.Ingame;

namespace ServerMod {
    static class InventoryUtils {
        
        // получает все предметы в инвентарях
        public static void GetInventoryItems(IMyCubeBlock block, Dictionary<string, double> dictionary, string type = "", string subtypeId = "", bool NamesByClient = false, bool IgnoreGarage = false) {
            if (block == null || !block.HasInventory) return;
            if (IgnoreGarage && block.SubtypeName().Contains("Garage")) return;



            for (int i = 0; i < block.InventoryCount; i++)
            {
                var inventory = (MyInventory) block.GetInventory(i);
                var items = inventory.GetItems();

                foreach (var item in items)
                {
                    var _item = MyDefinitionManager.Static.TryGetPhysicalItemDefinition(item.GetDefinitionId());

                    if ((string.IsNullOrWhiteSpace(type) || _item.Id.TypeId.ToString() == type) &&
                        _item.Id.SubtypeName.Contains(subtypeId))
                    {
                        var name = NamesByClient ? _item.DisplayNameText : _item.Id.SubtypeName;
                        double count = item.Amount.RawValue / 1000000d;

                        if (dictionary.ContainsKey(name)) dictionary[name] += count;
                        else dictionary.Add(name, count);
                    }
                }
            }
        }

        public static void GetInventoryItems(IMyCubeBlock block, Dictionary<MyPhysicalItemDefinition, double> dictionary, string type = "", string subtypeId = "", bool IgnoreGarage = false) {
            if (block == null || !block.HasInventory) return;
            if (IgnoreGarage && block.SubtypeName().Contains("Garage")) return;

            for (int i = 0; i < block.InventoryCount; i++)
            {
                var inventory = (MyInventory) block.GetInventory(i);
                var items = inventory.GetItems();

                foreach (var item in items)
                {
                    var _item = MyDefinitionManager.Static.TryGetPhysicalItemDefinition(item.GetDefinitionId());

                    if ((string.IsNullOrWhiteSpace(type) || _item.Id.TypeId.ToString() == type) &&
                        _item.Id.SubtypeName.Contains(subtypeId))
                    {
                        double count = item.Amount.RawValue / 1000000d;

                        if (dictionary.ContainsKey(_item)) dictionary[_item] += count;
                        else dictionary.Add(_item, count);
                    }
                }
            }
        }

        // получает текущее компоненты в блоках
        public static void GetComponents(this IMySlimBlock block, Dictionary<string, double> dictionary) {
            var components = (block.BlockDefinition as MyCubeBlockDefinition).Components;

            foreach (var component in components)
            {
                var name = component.Definition.Id.SubtypeName;
                int count = component.Count;

                if (dictionary.ContainsKey(name)) dictionary[name] += count;
                else dictionary.Add(name, count);
            }
            
            var missingComponents = new Dictionary<string, int>();
            block.GetMissingComponents(missingComponents);
            
            foreach (var component in missingComponents) {
                string name = component.Key;
                int count = component.Value;

                if (dictionary.ContainsKey(name)) dictionary[name] -= count;
                else dictionary.Add(name, count);
            }
        }

        // получает все компоненты в блоках
        public static void GetTotalComponents(this IMySlimBlock block, Dictionary<string, double> dictionary) {
            var components = (block.BlockDefinition as MyCubeBlockDefinition).Components;

            foreach (var component in components)
            {
                var name = component.Definition.Id.SubtypeName;
                
                int count = component.Count;

                if (dictionary.ContainsKey(name)) dictionary[name] += count;
                else dictionary.Add(name, count);
            }
        }

        public static void GetComponentsTranslation(this IMySlimBlock block, Dictionary<string, string> dictionary)
        {
            var components = (block.BlockDefinition as MyCubeBlockDefinition).Components;
            
            foreach (var component in components)
            {
                var SubtypeName = component.Definition.Id.SubtypeName;
                var TextName = component.Definition.DisplayNameText;

                if (!dictionary.ContainsKey(SubtypeName)) dictionary.Add(SubtypeName, TextName);
            }
            
        }


        // получает объем компонентов в блоках
        public static float GetComponentsVolume(this IMySlimBlock block)

        {
            float total = 0f; 
            foreach (var component in (block.BlockDefinition as MyCubeBlockDefinition).Components)
            {
                total += component.Definition.Volume;
            }
            return total;
        }

        /// <summary>
        /// Adds items to many invetories, modifies `items` param, leaving info about how much items wasn't spawned
        /// </summary>
        /// <param name="inventories"></param>
        /// <param name="items"></param>
        public static void AddItems(this List<IMyInventory> inventories, Dictionary<MyDefinitionId, double> items)
        {
            var keys = new List<MyDefinitionId>(items.Keys);
            var zero = (MyFixedPoint)0.0001;

            if (inventories.Count == 0 || keys.Count == 0) return;

            foreach (var y in keys)
            {
                foreach (var x in inventories)
                {
                    if (!items.ContainsKey(y)) continue;
                    if (!x.CanItemsBeAdded(zero, y)) continue;


                    var amount = items[y];
                    var am = ((MyInventoryBase)x).ComputeAmountThatFits(y);
                    if (am >= (MyFixedPoint)amount)
                    {
                        x.AddItem(y, amount);
                        items.Remove(y);
                        break;
                    }
                    else
                    {
                        x.AddItem(y, am);
                        items[y] = amount - (double)am;
                    }
                }
            }
        }

        // получает недостающие компоненты
        // IMySlimBlock.GetConstructionStockpileItemAmount() работает (работал) некорректно,
        // поэтому поиск через IMySlimBlock.GetMissingComponents()
        public static void GetMissingComponents(this IMySlimBlock block, Dictionary<string, double> dictionary, bool NamesByClient = false) 
        { 
            var missingComponents = new Dictionary<string, int>();
            block.GetMissingComponents(missingComponents);

            foreach (var component in missingComponents) {
                string name = component.Key;
                int count = component.Value;

                if (dictionary.ContainsKey(name)) dictionary[name] += count;
                else dictionary.Add(name, count);
            }
        }
        
        public static void  Minus<T>(this Dictionary<T, MyFixedPoint> x, Dictionary<T, MyFixedPoint> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] -= other[i.Key];
                }
            }
        }
        
        public static void Plus<T>(this Dictionary<T, MyFixedPoint> x, Dictionary<T, MyFixedPoint> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] += other[i.Key];
                } else {
                    x.Add(i.Key, other[i.Key]);
                }
            }
        }
        
        public static void  Minus<T>(this Dictionary<T, int> x, Dictionary<T, int> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] -= other[i.Key];
                }
            }
        }
        
        public static void Plus<T>(this Dictionary<T, int> x, Dictionary<T, int> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] += other[i.Key];
                } else {
                    x.Add(i.Key, other[i.Key]);
                }
            }
        }

        public static void AddItem(this IMyInventory inv, MyDefinitionId id, double amount) {
            inv.AddItems((MyFixedPoint)amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
        }
        public static void AddItem(this IMyInventory inv, MyDefinitionId id, MyFixedPoint amount) {
            inv.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
        }

        public static void GetAllCargosInRange (ref BoundingSphereD sphere2, List<MyEntity> cargos, string blockName, bool allowAssemblers = false, Func<IMyTerminalBlock, bool> filter = null, Func<IMyTerminalBlock, IMyTerminalBlock, int> sort = null, int maxTake = Int32.MaxValue) {
            var data = MyEntities.GetTopMostEntitiesInSphere(ref sphere2);
            var ships = new HashSet<MyCubeGrid>();
            foreach (var x in data) {
                var g = x as MyCubeGrid;
                if (g != null) {
                    ships.Add(g);
                }
            }
            
            var allBlocks = new List<IMyTerminalBlock>();
            var enters = new List<IMyTerminalBlock>();
            var sphere3 = sphere2;
            
            foreach (var x in ships) {
                x?.OverFatBlocks((b) => {
                    if (!(b is IMyCargoContainer) && !(b is IMyShipConnector) && !(allowAssemblers && b is IMyAssembler)) return;
                    var term = b as IMyTerminalBlock;
                    if (!term.IsFunctional) return;
                    if (term.CustomName.Contains(blockName)) return;
                    if (!term.HasLocalPlayerAccess()) return;
                    if (sphere3.Contains(term.WorldMatrix.Translation) == ContainmentType.Contains && !isSeparated(term)) {
                        var add = true;
                        foreach (var e in enters) {
                            if (areConnected(e, term)) {
                                add = false;
                                break;
                            }
                        }
                        if (add) enters.Add(term);
                    }
                        
                    if (filter == null || filter.Invoke(term)) {
                        allBlocks.Add(term);
                    }
                });
            }
            
            if (sort != null) {
                allBlocks.Sort((a,b)=>sort.Invoke(a, b));
            }
            
            foreach (var x in allBlocks) {
                if (isSeparated(x)) {
                    cargos.Add (x as MyEntity);
                    if (cargos.Count >= maxTake) return;
                } else {
                    foreach (var y in enters) {
                        if (areConnected (x, y)) {
                            cargos.Add (x as MyEntity);
                            if (cargos.Count >= maxTake) return;
                            break;
                        }
                    }
                }
            }
            
        }

        public static void GetAllCargosInRangeSimple(ref BoundingSphereD sphere2, List<IMyCubeBlock> cargos, Func<IMyTerminalBlock, bool> filter = null)
        {
            var data = MyEntities.GetTopMostEntitiesInSphere(ref sphere2);
            var ships = new HashSet<MyCubeGrid>();
            foreach (var x in data)
            {
                var g = x as MyCubeGrid;
                if (g != null)
                {
                    ships.Add(g);
                }
            }

            var allBlocks = new List<IMyTerminalBlock>();
            var sphere3 = sphere2;

            foreach (var x in ships){
                x?.OverFatBlocks((b) => {
                    if (!(b is IMyCargoContainer) && !(b is IMyShipConnector)) return;
                    var term = b as IMyTerminalBlock;
                    if (!term.IsFunctional) return;
                    if (!term.HasLocalPlayerAccess()) return;
                    if (sphere3.Contains(term.WorldMatrix.Translation) != ContainmentType.Contains) return;

                    if (filter == null || filter.Invoke(term)) {
                        cargos.Add(b);
                    }
                });
            }

        }

        private static bool isSeparated(IMyTerminalBlock a) {
            var sn = a.SlimBlock.BlockDefinition.Id.SubtypeName;
            if (sn.StartsWith("Freight")) return true;
            if (sn.Equals("LargeBlockLockerRoom") || sn.Equals("LargeBlockLockerRoomCorner") || sn.Equals("LargeBlockLockers")) return true;
            return false;
        }

        private static bool areConnected (IMyTerminalBlock a, IMyTerminalBlock b) {
            //if (a.CubeGrid != b.CubeGrid) return false;
            if (a==b) return true;

            for (var x=0; x < a.InventoryCount; x++) {
                for (var y=0; y < b.InventoryCount; y++) {
                    if (a.GetInventory (x).IsConnectedTo (b.GetInventory(y))) {
                        return true;
                    }
                }
            }

            return false;
        }


        public static string CustomName (this IMyInventory x) {
            var term = (x.Owner as IMyTerminalBlock);
            if (term == null) return x.Owner.DisplayName;
            else return term.CustomName;
        }

        public static MyFixedPoint calculateCargoMass (this IMyCubeGrid grid) {
            MyFixedPoint mass = 0;
            grid.FindBlocks(x => {

                var fat = x.FatBlock;
                if (fat == null || !fat.HasInventory) return false;
                var i = fat.GetInventory();
                if (i == null) return false;

                mass += i.CurrentMass;
                return false;
            });

            return mass;
        }


        public static MyFixedPoint RemoveAmount (this IMyInventory inv, MyDefinitionId id, double amount) {
            if (amount <= 0) return (MyFixedPoint)amount;
            
            var items = inv.GetItems();
            var l = items.Count;
            var k = 0;
            var am = (MyFixedPoint)amount;
            for (var i = 0; i<l; i++) {
                var itm = items[i];
                if (itm.Content.GetId() == id) {
                    if (itm.Amount <= am) {
                        am -= itm.Amount;
                        inv.RemoveItemsAt(i-k);
                        k++;
                    } else {
                        inv.RemoveItemAmount(itm, am);
                        return 0;
                    }
                } 
            }

            return am;
        }

        public static bool RemoveAmount(this IMyInventory inv, Dictionary<MyDefinitionId, MyFixedPoint> toRemove)
        {
            if (toRemove == null || toRemove.Count == 0) return false;

            var items = inv.GetItems();
            var l = items.Count;
            var k = 0;

            for (var i = 0; i < l; i++)
            {
                var itm = items[i];
                var id = itm.Content.GetId();
                if (toRemove.ContainsKey(id))
                {
                    var am = toRemove[id];
                    if (itm.Amount <= am)
                    {
                        am -= itm.Amount;
                        toRemove[id] = am;
                        inv.RemoveItemsAt(i - k);
                        k++;
                    }
                    else
                    {
                        toRemove.Remove(id);
                        inv.RemoveItemAmount(itm, am);
                    }
                }
            }

            return toRemove.Count == 0;
        }

        public static bool RemoveAmount(this IMyInventory inv, Dictionary<MyDefinitionId, double> toRemove)
        {
            if (toRemove == null || toRemove.Count == 0) return false;

            var items = inv.GetItems();
            var l = items.Count;
            var k = 0;

            for (var i = 0; i < l; i++)
            {
                var itm = items[i];
                var id = itm.Content.GetId();
                if (toRemove.ContainsKey(id))
                {
                    var am = toRemove[id];
                    if ((double)itm.Amount <= am)
                    {
                        am -= (double)itm.Amount;
                        toRemove[id] = am;
                        inv.RemoveItemsAt(i - k);
                        k++;
                    }
                    else
                    {
                        toRemove.Remove(id);
                        inv.RemoveItemAmount(itm, (MyFixedPoint)am);
                    }
                }
            }

            return toRemove.Count == 0;
        }



        public static Dictionary<MyDefinitionId, MyFixedPoint> CountItems(this IMyInventory inventory, Dictionary<MyDefinitionId, VRage.MyFixedPoint> d = null) {
            var items = inventory.GetItems();

            if (d == null) {
                d = new Dictionary<MyDefinitionId, MyFixedPoint>();
            }

            foreach (var x in items) {
                var id = x.Content.GetId();
                if (!d.ContainsKey(id)) {
                    d.Add(x.Content.GetId(), x.Amount);
                } else {
                    d[id] += x.Amount;
                }

            }

            return d;
        }

        public static Dictionary<MyDefinitionId, double> CountItemsD(this IMyInventory inventory, Dictionary<MyDefinitionId, double> d = null)
        {
            var items = inventory.GetItems();

            if (d == null)
            {
                d = new Dictionary<MyDefinitionId, double>();
            }

            foreach (var x in items)
            {
                var id = x.Content.GetId();
                if (!d.ContainsKey(id))
                {
                    d.Add(x.Content.GetId(), (double)x.Amount);
                }
                else
                {
                    d[id] += (double)x.Amount;
                }

            }

            return d;
        }

        public static MyFixedPoint CountItemType(this IMyInventory inventory, String type, String subtype) {
            var total = MyFixedPoint.Zero;

            var items = inventory.GetItems();
            foreach (var x in items) {
                var id = x.Content.GetId();
                if (id.TypeId.ToString() == type) {
                    if (subtype == null || subtype == id.SubtypeName) {
                        total += x.Amount;
                    }
                }
            }

            return total;
        }


        public static int GetItemIndexByID (this IMyInventory inv, uint itemId) {
            for (var index=0; index<inv.ItemCount; index++) {
                if (inv.GetItemAt (index).Value.ItemId == itemId) {
                    return index;
                }
            }
            return -1;
        }

        public static void MoveAllItemsFrom(this IMyInventory inventory, IMyInventory from, Func<VRage.Game.ModAPI.Ingame.MyInventoryItem, MyFixedPoint?> p = null, bool alwaysAtEnd = false) {
            for (var x=from.ItemCount-1; x>=0; x--) {
                var t = from.GetItemAt(x);
                MyFixedPoint? amount = p!= null ? p.Invoke(t.Value) : null;
                if (amount == null || amount > 0) {
                    from.TransferItemTo (inventory, x, checkConnection:false, amount:amount, targetItemIndex: (alwaysAtEnd ? (int?)inventory.ItemCount : null));
                }
            }
        }

        public static void TeleportAllItemsFromRequest (this IMyInventory inventory, IMyInventory from, Func<VRage.Game.ModAPI.Ingame.MyInventoryItem, MyFixedPoint?> p = null) {
            for (var x=from.ItemCount-1; x>=0; x--) {
                var t = from.GetItemAt(x);
                MyFixedPoint? amount = p!= null ? p.Invoke(t.Value) : null;
                if (amount == null || amount > 0) {
                    AutoTools.TeleportItemsRequest ((MyInventory)from, (MyInventory)inventory, t.Value.ItemId, amount);
                }
            }
        }

        public static void PullRequest (this IMyInventory target, List<IMyInventory> from, Dictionary<MyDefinitionId, MyFixedPoint> what, bool alwaysAtEnd = false) {
            foreach (var x in from) {
                if (what.Count == 0) break;

                MoveAllItemsFrom (target, x, (i) => {
                    if (what.ContainsKey (i.Type)) {
                        var need = what[i.Type];
                        var have = i.Amount;
                       

                        if (need > have) {
                            what[i.Type] = need - have;
                            return null;
                        } else {
                            what.Remove(i.Type);
                            return need;
                        }
                    } 
                    return -1;
                }, alwaysAtEnd:alwaysAtEnd);
            }
        }

        public static void PushRequest (this IMyInventory target, List<IMyInventory> from, Dictionary<MyDefinitionId, MyFixedPoint> what, bool alwaysAtEnd = false) {
            foreach (var x in from) {
                if (what.Count == 0) break;

                MoveAllItemsFrom (x, target, (i) => {
                    if (what.ContainsKey (i.Type)) {
                        var need = what[i.Type];
                        var have = i.Amount;
                       

                        if (need > have) {
                            what[i.Type] = need - have;
                            return null;
                        } else {
                            what.Remove(i.Type);
                            return need;
                        }
                    } 
                    return -1;
                }, alwaysAtEnd:alwaysAtEnd);
            }
        }

        public static double CanProduce (this MyBlueprintDefinitionBase bp, Dictionary<MyDefinitionId, MyFixedPoint> items) {
            double can_produce_times = Double.MaxValue;
            foreach (var pre in bp.Prerequisites) {
                var have = items.GetOr (pre.Id, MyFixedPoint.Zero);
                var times = (double)have / (double)pre.Amount;
                if (times < can_produce_times) {
                    can_produce_times = times;
                    if (can_produce_times == 0) { return 0; }
                }
            }

            return can_produce_times;
        }

        public static double InputVolume (this MyBlueprintDefinitionBase bp) { //TODO
            return 1;
        }

        public static MyFixedPoint GetLeftVolume(this IMyInventory inventory) {
            return inventory.MaxVolume-inventory.CurrentVolume;
        }

        public static double GetLeftVolumeInLiters(this IMyInventory inventory) {
            return ((double)inventory.GetLeftVolume())*1000d;
         }

        public static double GetFilledRatio(this IMyInventory inventory) {
            return (double)inventory.CurrentVolume / (double)inventory.MaxVolume;
        }
        public static bool ParseHumanDefinition (string type, string subtype, out MyDefinitionId id)
        {
            if (type == "i" || type == "I")
            {
                type = "Ingot";
            } 
            else if (type == "o" || type == "O")
            {
                type = "Ore";
            }
            else if (type == "c" || type == "C")
            {
                type = "Component";
            }
            return MyDefinitionId.TryParse("MyObjectBuilder_" + type + "/" + subtype, out id);
        }
        public static string GetHumanName(MyDefinitionId id)
        {
            return id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "/" + id.SubtypeName;
        }
        public static void SetItems(this IMyInventory inventory, Dictionary<MyDefinitionId, MyFixedPoint> set) { //OLD???
            var items = inventory.GetItems();
            var have = inventory.CountItems();
            var l = items.Count;


            for (var i = 0; i < l; i++) {
                var x = items[i];
                var id = x.Content.GetId();

                if (set.ContainsKey(id)) {
                    var dx = set[id] - (have.ContainsKey(id) ? have[id] : 0);
                    if (dx < 0) {
                        if (x.Amount + dx > 0) {
                            Log.Info("SetItems : RemoveAmount " + (-dx));

                            inventory.RemoveItemAmount(x, -dx);
                            set.Remove(id);
                        } else {
                            Log.Info("SetItems : RemoveItem " + (-dx));

                            items.RemoveAt(i);
                            i--;
                            l--;
                            set[id] += dx;
                        }
                    } else {
                        Log.Info("SetItems : AddItem " + (dx) + " " + (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
                        inventory.AddItems(dx, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
                        set.Remove(id);
                    }
                } else {
                    Log.Info("SetItems : Other " + id + " " + x.Amount);
                }
            }

            foreach (var y in set) {
                Log.Info("SetItems : AddItem Left" + (y.Value) + " " + (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(y.Key));
                inventory.AddItems(y.Value, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(y.Key));
            }
        }
        
    }

    static class ItemsLibrary
    {
        public static readonly List<string> defaultComponents = new List<string>() {
            "BulletproofGlass",
            "Computer",
            "Construction",
            "Detector",
            "Display",
            "Girder",
            "GravityGenerator",
            "InteriorPlate",
            "InteriorPlateBox",
            "LargeTube",
            "LargeTubeBox",
            "MetalGrid",
            "MetalGridBox",
            "Medical",
            "Motor",
            "MotorBox",
            "PowerCell",
            "PowerCellBox",
            "RadioCommunication",
            "Reactor",
            "RebelComponent",
            "SmallTube",
            "SmallTubeBox",
            "SolarCell",
            "SteelPlate",
            "SteelPlateBox",
            "Superconductor",
            "Thrust",
            "HeavyComp",
            "CompT02",
            "CompT03",
            "CompT04",
            "CompT05",
            "CompT06",
            "CompT07",
            "CompT08",
            "CompT09",
            "CompT10",
            "CompT11",
            "CompT12"
        };
        public static readonly Dictionary<string, string> OreToReportLink = new Dictionary<string, string> {
            { "Lateryt", "Aluminum"},
            { "∑UnknownMaterial", "Cezium"},
            { "Malachit", "Copper"},
            { "⌊UnknownMaterial", "Diamond"},
            { "∏UnknownMaterial", "Kaliforn"},
            { "FrozenOil", "Oil"},
            { "∇UnknownMaterial", "Radium"}
        };
        public static readonly Dictionary<string, string> IngotToReportLink = new Dictionary<string, string> {
            { "AluminumIngot", "Aluminum"},
            { "Cez", "Cezium"},
            { "CopperIngot", "Copper"},
            { "PalladiumIngot","Palladium"},
            { "Rad", "Radium"}
         };
        public static readonly Dictionary<string, List<string>> Translation = new Dictionary<string, List<string>> {
            {"BulletproofGlass",new List<string>(){"Bulletproof Glass", "Бронированное стекло"}},
            {"Computer", new List<string>(){"Computer", "Компьютер"}},
            {"Construction", new List<string>(){"Construction", "Стройкомпонент"}},
            {"Detector", new List<string>(){"Detector", "Компонент детектора"}},
            {"Display", new List<string>(){"Display", "Экран"}},
            {"Girder", new List<string>(){"Girder", "Балка"}},
            {"GravityGenerator", new List<string>(){"Gravity Generator", "Гравикомпонент"}},
            {"InteriorPlate", new List<string>(){"Interior Plate", "Внутренняя пластина"}},
            {"InteriorPlateBox", new List<string>(){"Interior Plate Box", "Коробка внутренней пластины"}},
            {"LargeTube", new List<string>(){"Large Tube", "Большая труба"}},
            {"LargeTubeBox", new List<string>(){"Large Tube Box", "Коробка Большой трубы"}},
            {"MetalGrid", new List<string>(){"Metal Grid", "Компонент решётки"}},
            {"MetalGridBox", new List<string>(){"Metal Grid Box", "Коробка компонента решётки"}},
            {"Medical", new List<string>(){"Medical Comp.", "Медицинский Комп."}},
            {"Motor", new List<string>(){"Motor", "Мотор"}},
            {"MotorBox", new List<string>(){"Motor Box", "Коробка мотора"}},
            {"PowerCell", new List<string>(){"Power Cell", "Энергоячейка"}},
            {"PowerCellBox", new List<string>(){"Power Cell Box", "Коробка энергоячейки"}},
            {"RadioCommunication", new List<string>(){"Radio Communication", "Радио компонент"}},
            {"Reactor", new List<string>(){"Reactor", "Компонент реактор"}},
            {"RebelComponent", new List<string>(){"Rebel Component", "Ребел компонент"}},
            {"SmallTube", new List<string>(){"Small Tube", "Малая труба"}},
            {"SmallTubeBox", new List<string>(){"Small Tube Box", "Коробка малай трубы"}},
            {"SolarCell", new List<string>(){"Solar Cell", "Солнечная ячейка"}},
            {"SteelPlate", new List<string>(){"Steel Plate", "Стальная пластина"}},
            {"SteelPlateBox", new List<string>(){"Steel Plate Box", "Коробка стальной пластины"}},
            {"Superconductor", new List<string>(){"Superconductor", "Сверхпроводник"}},
            {"Thrust", new List<string>(){"Thrust", "Деталь ускорителя"}},
            {"HeavyComp", new List<string>(){"Turbo Pump", "Турбо насос"}},
            {"AdminComponent", new List<string>(){"Admin Component", "Админский компонент"}},
            {"CompT02", new List<string>(){"Upgrade CompT02","Модуль улучшения Т02"}},
            {"CompT03", new List<string>(){"Upgrade CompT03","Модуль улучшения Т03"}},
            {"CompT04", new List<string>(){"Upgrade CompT04","Модуль улучшения Т04"}},
            {"CompT05", new List<string>(){"Upgrade CompT05","Модуль улучшения Т05"}},
            {"CompT06", new List<string>(){"Upgrade CompT06","Модуль улучшения Т06"}},
            {"CompT07", new List<string>(){"Upgrade CompT07","Модуль улучшения Т07"}},
            {"CompT08", new List<string>(){"Upgrade CompT08","Модуль улучшения Т08"}},
            {"CompT09", new List<string>(){"Upgrade CompT09","Модуль улучшения Т09"}},
            {"CompT10", new List<string>(){"Upgrade CompT10","Модуль улучшения Т10"}},
            {"CompT11", new List<string>(){"Upgrade CompT11","Модуль улучшения Т11"}},
            {"CompT12", new List<string>(){"Upgrade CompT12","Модуль улучшения Т12"}},
            {"AntifreezeComponent", new List<string>(){"Antifreeze Component","Компонент Антифриза"}},
            //OresIngots
            {"Iron", new List<string>(){"Iron", "Железо"}},
            {"Nickel", new List<string>(){"Nickel", "Никель"}},
            {"Cobalt", new List<string>(){"Cobalt", "Кобальт"}},
            {"Silicon", new List<string>(){"Silicon", "Кремний"}},
            {"Silver", new List<string>(){"Silver", "Серебро"}},
            {"Magnesium", new List<string>(){"Magnesium", "Магний"}},
            {"Gold", new List<string>(){"Gold", "Золото"}},
            {"Platinum", new List<string>(){"Platinum", "Платина"}},
            {"Stone", new List<string>(){"Stone", "Камень"}},
            {"Uranium", new List<string>(){"Uranium", "Уран"}},
            {"CopperIngot", new List<string>(){"Copper", "Медь"}},
            {"Copper", new List<string>(){"Copper", "Медь"}},
            {"AluminumIngot", new List<string>(){"AluminumIngot", "Алюминий"}},
            {"Aluminum", new List<string>(){"Aluminum", "Алюминий"}},
            {"PalladiumIngot", new List<string>(){"PalladiumIngot", "Палладий"}},
            {"Palladium", new List<string>(){"Palladium", "Палладий"}},
            {"Cez", new List<string>(){"Cezium", "Цезий"}},
            {"Diamond", new List<string>(){"Diamond", "Алмаз"}},
            {"Scrap", new List<string>(){"Scrap", "Скрап"}},
            {"Ice", new List<string>(){"Ice", "Лед"}}
        };
    }
}
