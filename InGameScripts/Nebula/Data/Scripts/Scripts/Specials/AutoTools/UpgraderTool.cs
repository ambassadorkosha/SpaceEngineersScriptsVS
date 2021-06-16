using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using ServerMod;
using Digi;
using VRage.Input;
using Sandbox.ModAPI.Weapons;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Definitions;
using ProtoBuf;
using Scripts.Shared;
using VRageMath;
using Slime;
using Sandbox.Game;
using VRage;
using VRage.Game.Entity;

namespace Scripts.Specials.AutoTools
{

    [ProtoContract]
    public class ChangeBlockTask
    {
        [ProtoMember(1)]
        public long grid;
        [ProtoMember(2)]
        public string from;
        [ProtoMember(3)]
        public string to;
        [ProtoMember(4)]
        public long inventory;

        /// <summary>
        /// From client to server
        /// </summary>
        [ProtoMember(5)]
        public List<Vector3I> positions = new List<Vector3I>();

        /// <summary>
        /// From server to client
        /// </summary>
        [ProtoMember(6)]
        public List<MyObjectBuilder_CubeBlock> blocks = new List<MyObjectBuilder_CubeBlock>();
    }

    public static class UpgraderTool
    {
        public static Dictionary<int, MyCubeBlockDefinition> Definitions = new Dictionary<int, MyCubeBlockDefinition>();

        public static Dictionary<string, string> downgrades = new Dictionary<string, string>();
        public static Dictionary<string, string> downgradeLevels = new Dictionary<string, string>();

        public static Dictionary<string, string> upgradeLevels = new Dictionary<string, string>(){
             {"T11", "T12"},
             {"T10", "T11"},
             {"T09", "T10"},
             {"T08", "T09"},
             {"T07", "T08"},
             {"T06", "T07"},
             {"T05", "T06"},
             {"T04", "T05"},
             {"T03", "T04"},
             {"T02", "T03"},
             {"", "T02"},
        };
        public static Dictionary<string, string> upgrades = new Dictionary<string, string>()
        {
            {"LargeBlockArmorBlock","LargeHeavyBlockArmorBlock"},
            {"LargeBlockArmorSlope","LargeHeavyBlockArmorSlope"},
            {"LargeBlockArmorCorner","LargeHeavyBlockArmorCorner"},
            {"LargeBlockArmorCornerInv","LargeHeavyBlockArmorCornerInv"},

            {"LargeHalfArmorBlock","LargeHeavyHalfArmorBlock"},
            {"LargeHalfSlopeArmorBlock","LargeHeavyHalfSlopeArmorBlock"},
            {"HalfArmorBlock","HeavyHalfArmorBlock"},

            //{"HalfArmorBlock","HeavyHalfBlockArmorBlock"},
            {"HalfSlopeArmorBlock","HeavyHalfSlopeArmorBlock"},

            {"LargeBlockArmorRoundSlope","LargeHeavyBlockArmorRoundSlope"},
            {"LargeBlockArmorRoundCorner","LargeHeavyBlockArmorRoundCorner"},
            {"LargeBlockArmorRoundCornerInv","LargeHeavyBlockArmorRoundCornerInv"},

            {"LargeBlockArmorSlope2Base","LargeHeavyBlockArmorSlope2Base"},
            {"LargeBlockArmorSlope2Tip","LargeHeavyBlockArmorSlope2Tip"},
            {"LargeBlockArmorCorner2Base","LargeHeavyBlockArmorCorner2Base"},
            {"LargeBlockArmorCorner2Tip","LargeHeavyBlockArmorCorner2Tip"},
            {"LargeBlockArmorInvCorner2Base","LargeHeavyBlockArmorInvCorner2Base"},
            {"LargeBlockArmorInvCorner2Tip","LargeHeavyBlockArmorInvCorner2Tip"},

            {"SmallBlockArmorBlock","SmallHeavyBlockArmorBlock"},
            {"SmallBlockArmorSlope","SmallHeavyBlockArmorSlope"},
            {"SmallBlockArmorCorner","SmallHeavyBlockArmorCorner"},
            {"SmallBlockArmorCornerInv","SmallHeavyBlockArmorCornerInv"},

            {"SmallBlockArmorRoundSlope","SmallHeavyBlockArmorRoundSlope"},
            {"SmallBlockArmorRoundCorner","SmallHeavyBlockArmorRoundCorner"},
            {"SmallBlockArmorRoundCornerInv","SmallHeavyBlockArmorRoundCornerInv"},

            {"SmallBlockArmorSlope2Base","SmallHeavyBlockArmorSlope2Base"},
            {"SmallBlockArmorSlope2Tip","SmallHeavyBlockArmorSlope2Tip"},
            {"SmallBlockArmorCorner2Base","SmallHeavyBlockArmorCorner2Base"},
            {"SmallBlockArmorCorner2Tip","SmallHeavyBlockArmorCorner2Tip"},
            {"SmallBlockArmorInvCorner2Base","SmallHeavyBlockArmorInvCorner2Base"},
            {"SmallBlockArmorInvCorner2Tip","SmallHeavyBlockArmorInvCorner2Tip"},
            
            //============ Armors 2

            {"LargeBlockArmorCornerSquare","LargeBlockHeavyArmorCornerSquare"},
            {"SmallBlockArmorCornerSquare","SmallBlockHeavyArmorCornerSquare"},

            {"LargeBlockArmorCornerSquareInverted","LargeBlockHeavyArmorCornerSquareInverted"},
            {"SmallBlockArmorCornerSquareInverted","SmallBlockHeavyArmorCornerSquareInverted"},

            {"LargeBlockArmorHalfCorner","LargeBlockHeavyArmorHalfSlopeCorner"},
            {"SmallBlockArmorHalfCorner","SmallBlockHeavyArmorHalfSlopeCorner"},

            {"LargeBlockArmorHalfSlopeCornerInverted","LargeBlockHeavyArmorHalfSlopeCornerInverted"},
            {"SmallBlockArmorHalfSlopeCornerInverted","SmallBlockHeavyArmorHalfSlopeCornerInverted"},

            {"LargeBlockArmorHalfSlopedCorner","LargeBlockHeavyArmorHalfSlopedCorner"},
            {"SmallBlockArmorHalfSlopedCorner","SmallBlockHeavyArmorHalfSlopedCorner"},

            {"LargeBlockArmorHalfSlopedCornerBase","LargeBlockHeavyArmorHalfSlopedCornerBase"},
            {"SmallBlockArmorHalfSlopedCornerBase","SmallBlockHeavyArmorHalfSlopedCornerBase"},

            {"LargeBlockArmorHalfSlopeInverted","LargeBlockHeavyArmorHalfSlopeInverted"},
            {"SmallBlockArmorHalfSlopeInverted","SmallBlockHeavyArmorHalfSlopeInverted"},

            {"LargeBlockArmorSlopedCorner","LargeBlockHeavyArmorSlopedCorner"},
            {"SmallBlockArmorSlopedCorner","SmallBlockHeavyArmorSlopedCorner"},

            {"LargeBlockArmorSlopedCornerBase","LargeBlockHeavyArmorSlopedCornerBase"},
            {"SmallBlockArmorSlopedCornerBase","SmallBlockHeavyArmorSlopedCornerBase"},


            {"LargeBlockArmorSlopedCornerTip","LargeBlockHeavyArmorSlopedCornerTip"},
            {"SmallBlockArmorSlopedCornerTip","SmallBlockHeavyArmorSlopedCornerTip"},

        };

        public static Connection<ChangeBlockTask> Connection;

        public static void Init()
        {
            Connection = new Connection<ChangeBlockTask>(27743, Handler);

            foreach (var x in upgrades)
            {
                try
                {
                    downgrades.Add(x.Value, x.Key);
                }
                catch (Exception e)
                {
                    Log.ChatError("Key:" + x.Value);
                }
            }

            foreach (var x in upgradeLevels)
            {
                try
                {
                    downgradeLevels.Add(x.Value, x.Key);
                }
                catch (Exception e)
                {
                    Log.ChatError("Key:" + x.Value);
                }
            }

            var all = MyDefinitionManager.Static.GetAllDefinitions();
            foreach (var x in all)
            {
                var def = x as MyCubeBlockDefinition;
                if (def != null)
                {
                    try
                    {
                        var id = MyStringHash.GetOrCompute(x.Id.TypeId.ToString() + "/" + x.Id.SubtypeId);
                        Definitions.Add(id.m_hash, def);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        private static void Handler(ChangeBlockTask task, ulong player, bool isFromServer)
        {
            if (!isFromServer)
            {
                var g = task.grid.As<IMyCubeGrid>();

                if (g == null) return;
                if (g.Physics != null && g.Physics.LinearVelocity.LengthSquared() > 1) return;

                var sh = g.GetShip();
                if (sh == null) return;

                var identity = player.Identity();
                if (identity == null) return;

                bool isAdmin = player.IsUserAdmin();

                var character = identity.GetPlayer()?.Character;
                if (character == null) return;

                var creative = MyAPIGateway.Session.CreativeMode;

                IMyTerminalBlock term = null;
                if (!creative)
                {
                    term = task.inventory.As<IMyTerminalBlock>();
                    if (term == null) return;
                    if (!term.HasPlayerAccess(identity.IdentityId)) return;
                    if ((character.WorldMatrix.Translation - term.WorldMatrix.Translation).Length() > 50) return;
                }

                var from = MyStringHash.GetOrCompute(task.from);

                var replacedBlocks = new List<MyObjectBuilder_CubeBlock>();
                foreach (var x in task.positions)
                {
                    var bl = g.GetCubeBlock(x);

                    var volumeNeeded = bl.GetComponentsVolume();
                    if (!creative && term.GetInventory(0).GetLeftVolumeInLiters() - volumeNeeded < 500) return;


                    if (bl == null) continue;
                    if (bl.BlockDefinition.Id.SubtypeId.m_hash != from.m_hash) continue;

                    if (isAdmin || bl.GetRelation(identity.IdentityId) >= 0)
                    {
                        var ob = Replace(bl, creative ? null : term.GetInventory(0), task.to);
                        if (ob != null)
                        {
                            replacedBlocks.Add(ob);
                        }
                    }
                }

                task.blocks = replacedBlocks;
                Connection.SendMessageToOthers(task);
            }
            else
            {
                var g = task.grid.As<IMyCubeGrid>();
                if (g == null) return;

                foreach (var block in task.blocks)
                {
                    g.AddBlock(block, false);
                }

            }
        }

        public static IMyCubeBlock GetCargo()
        {
            var tr = MyAPIGateway.Session?.Player?.Character.WorldMatrix.Translation;
            if (!tr.HasValue) return null;

            var cargos = new List<IMyCubeBlock>();
            var sphere = new BoundingSphereD(tr.Value, 47);
            InventoryUtils.GetAllCargosInRangeSimple(ref sphere, cargos);

            cargos.Sort((a, b) => b.GetInventory().GetLeftVolumeInLiters() >= a.GetInventory().GetLeftVolumeInLiters() ? 1 : -1);

            if (cargos.Count == 0) return null;
            return cargos[0];
        }

        public static void UpgradeOrDowngrade(IMyHandheldGunObject<Sandbox.Game.Weapons.MyToolBase> tool)
        {

            var caster = (tool as IMyEngineerToolBase).Components.Get<MyCasterComponent>();
            var block = (caster.HitBlock as IMySlimBlock);
            if (block == null) return;

            List<MyMouseButtonsEnum> mouse = new List<MyMouseButtonsEnum>();
            List<MyKeys> keyboard = new List<MyKeys>();
            mouse.Clear();
            keyboard.Clear();
            MyAPIGateway.Input.GetListOfPressedMouseButtons(mouse);
            MyAPIGateway.Input.GetListOfPressedKeys(keyboard);

            if (mouse.Contains(MyMouseButtonsEnum.Middle) && mouse.Count == 1 && keyboard.Contains(MyKeys.Control))
            {
                var cargo = GetCargo();
                if (cargo == null && !MyAPIGateway.Session.CreativeMode) return;
                bool upgrade = !keyboard.Contains(MyKeys.LeftAlt);

                var mode = keyboard.Contains(MyKeys.Shift) ? UpgradeMode.ReplaceAll : UpgradeMode.ReplaceOne;
                var task = ReplaceAll(block, (x) => {
                    switch (mode)
                    {
                        case UpgradeMode.ReplaceOne: return x == block;
                        case UpgradeMode.ReplaceAll: return true;
                        default: return false;
                    }
                }, upgrade);

                if (task != null)
                {
                    task.inventory = MyAPIGateway.Session.CreativeMode ? 0 : cargo.EntityId;
                    Connection.SendMessageToServer(task);
                }
            }
        }

        public static MyObjectBuilder_CubeBlock Replace(IMySlimBlock block, IMyInventory inventory, string newName)
        {
            var ob = block.GetObjectBuilder();
            ob.EntityId = 0;
            ob.SubtypeName = newName;
            ob.BuildPercent = MyAPIGateway.Session.CreativeMode ? 1f : 0.000001f;
            ob.IntegrityPercent = MyAPIGateway.Session.CreativeMode ? 1f : 0.000001f;

            if (!MyAPIGateway.Session.CreativeMode)
            {
                var fat = block.FatBlock;
                if (fat != null)
                {
                    for (var x = 0; x < fat.InventoryCount; x++)
                    {
                        var inv = fat.GetInventory(x);
                        if (inv.ItemCount > 0) return null; //TODO teleport items
                    }
                }

                ob.ConstructionInventory?.Clear();
                if (ob.ConstructionStockpile != null) ob.ConstructionStockpile.Items = new MyObjectBuilder_StockpileItem[0];

                if (inventory != null) block.MoveItemsFromConstructionStockpile(inventory);
                block.FullyDismount(null);
                if (inventory != null) block.MoveItemsFromConstructionStockpile(inventory);
                block.CubeGrid.RazeBlock(block.Position);
                if (MyAPIGateway.Session.IsServer)
                {
                    inventory.RemoveAmount((block.BlockDefinition as MyCubeBlockDefinition).Components[0].Definition.Id, 1);
                }
            }
            else
            {
                block.CubeGrid.RazeBlock(block.Position);
            }

            var newBlock = block.CubeGrid.AddBlock(ob, false);
            if (!MyAPIGateway.Session.CreativeMode)
            {
                if (inventory != null) {
                    newBlock.MoveItemsToConstructionStockpile(inventory);
                    return newBlock.GetObjectBuilder();
                }
            }
            return ob;
        }

        private static ChangeBlockTask ReplaceAll(IMySlimBlock block, Func<IMySlimBlock, bool> canUpgrade, bool upgrade)
        {
            var task = new ChangeBlockTask();
            task.from = block.BlockDefinition.Id.SubtypeName;
            task.grid = block.CubeGrid.EntityId;
            string newSubtype = null;

            if ((upgrade ? upgrades : downgrades).TryGetValue(task.from, out newSubtype))
            {
                //found
            }
            else
            {
                newSubtype = TryUpgrade(block, upgrade);
            }
            if (newSubtype == null) return null;


            task.to = newSubtype;

            task.positions = new List<Vector3I>();

            var blocks = new List<IMySlimBlock>();
            block.CubeGrid.GetBlocks(blocks);

            var oldId = MyStringHash.GetOrCompute(task.from);
            var type = block.BlockDefinition.Id.TypeId;
            foreach (var x in blocks)
            {
                if (x.BlockDefinition.Id.SubtypeId != oldId || x.BlockDefinition.Id.TypeId != type) continue;
                if (x.IsDestroyed) continue;
                if (!canUpgrade(x)) continue;

                task.positions.Add(x.Position);
            }

            if (task.positions.Count == 0) return null;

            return task;
        }

        private static string TryUpgrade(IMySlimBlock block, bool isUpgrade)
        {
            var id = MyStringHash.GetOrCompute(block.BlockDefinition.Id.TypeId.ToString() + "/" + block.BlockDefinition.Id.SubtypeId);
            var sn = block.BlockDefinition.Id.SubtypeName;
            var ob = block.GetObjectBuilder();
            foreach (var y in (isUpgrade ? upgradeLevels : downgradeLevels))
            {
                if (y.Key != "")
                {
                    if (sn.Contains(y.Key))
                    {

                        var newsn = sn.Replace(y.Key, y.Value);
                        var newID = ob.TypeId.ToString() + "/" + newsn;
                        var hash = MyStringHash.GetOrCompute(newID);

                        if (Definitions.ContainsKey(hash.m_hash))
                        {
                            return newsn;
                        }
                    }
                }
                else
                {
                    var newsn = sn + y.Value;

                    var newID = ob.TypeId.ToString() + "/" + newsn;
                    var hash = MyStringHash.GetOrCompute(newID);

                    if (Definitions.ContainsKey(hash.m_hash))
                    {
                        return newsn;
                    }

                    return null;
                }
            }

            return null;
        }

        private enum UpgradeMode
        {
            ReplaceOne,
            ReplaceSame,
            ReplaceSameTime,
            ReplaceAll
        }
    }
}



/*
        Dictionary<string, GroupInfo> teached = new Dictionary<string, GroupInfo>();

        public class GroupInfo
        {
            public string sname1;
            public string sname2;
            
            public MyBlockOrientation block1;
            public MyBlockOrientation block2;

            public Vector3I position1;
            public Vector3I position2;
            

            public string sname3;
            public MyBlockOrientation block3;
            public MyObjectBuilder_CubeBlock ob;
            public Vector3I position3;
        }


        public void Teach (IMySlimBlock block)
        {
            var blocks = new List<IMySlimBlock>();
            var grid = block.CubeGrid;
            grid.GetBlocks(blocks);
            var gi = new GroupInfo();
            foreach (var b in blocks)
            {
                var def = (MyCubeBlockDefinition)block.BlockDefinition;
                if (def == null)
                {
                    Log("Def is null");
                }
                if (def.Size.Volume() != 1)
                {
                    gi.sname3 = b.BlockDefinition.Id.SubtypeName;
                    gi.block3 = b.Orientation;
                    gi.ob = b.GetObjectBuilder();
                }
                else
                {
                    if (gi.sname1 == null)
                    {
                        gi.sname1 = b.BlockDefinition.Id.SubtypeName;
                        gi.block1 = b.Orientation;
                        gi.position1 = b.Position;
                    }
                    else
                    {
                        gi.sname2 = b.BlockDefinition.Id.SubtypeName;
                        gi.block2 = b.Orientation;
                        gi.position2 = b.Position;
                    }
                }
            }

            
            teached.Add (gi.sname1, gi);
            
        }

        public void Group (IMySlimBlock block)
        {
            var blocks = new List<IMySlimBlock>();
            var grid = block.CubeGrid;
            grid.GetBlocks(blocks);

            foreach (var x in blocks)
            {
                IMySlimBlock other;
                GroupInfo gi;
                if (CanGroup (x, out other, out gi))
                {
                    grid.RazeBlock(x.Position);
                    grid.RazeBlock(other.Position);

                    var ob = gi.ob;
                    ob.Min = x.Position;
                    grid.AddBlock(ob, false);
                }
            }
        }

        public bool CanGroup (IMySlimBlock x, out IMySlimBlock other, out GroupInfo gi)
        {
            other = null;
            gi = null;
            if (teached.ContainsKey(x.BlockDefinition.Id.SubtypeName))
            {
                var lesson = teached[x.BlockDefinition.Id.SubtypeName];
                if (x.Orientation == lesson.block1)
                {
                    foreach (var y in x.Neighbours)
                    {
                        if (y.BlockDefinition.Id.SubtypeName == lesson.sname2)
                        {
                            if (y.Orientation == lesson.block2)
                            {
                                gi = lesson;
                                other = y;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        */
