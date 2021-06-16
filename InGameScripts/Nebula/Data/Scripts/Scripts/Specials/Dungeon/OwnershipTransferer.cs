using Digi;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Specials.Messaging;
using ServerMod;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;
using VRageMath;

namespace Scripts.Specials.Dungeon {

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MedicalRoom), true, new string[] { "SmallOwnershipTransfer", "LargeOwnershipTransfer" })]
    class OwnershipTransferer : MyGameLogicComponent {
        public IMyMedicalRoom block;
        bool inited = false;

        public static void Init () {

        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            if (!inited) {
                InitActions ();
                inited = true;
            }

            if (!MyAPIGateway.Multiplayer.IsServer) {

            } else {
                block = (IMyMedicalRoom)Entity;
                block.OwnershipChanged += Block_OwnershipChanged;
            }
        }

        public static bool HasBlocksInVoxels (IMyCubeGrid grid) {
            return false;
        }

        public static void MakeFixed (IMyCubeGrid grid, bool strong) {
            //List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            //grid.GetBlocks (blocks, (x) => { 
            //        var fat = x.FatBlock;
            //        if (fat == null) return false;
            //        return fat is IMyShipConnector || x is IMyMotorRotor || x is IMyPistonBase || x is IMyMotorSuspension || x is IMyLandingGear;
            //    }
            //);

            try {
                 var sp = new BoundingSphereD (grid.WorldMatrix.Translation, 100);
                var ents = MyEntities.GetTopMostEntitiesInSphere (ref sp);
                foreach (var x in ents) {
                    var d = x as MyCubeGrid;
                    if (d != null && d != grid) {
                        MyCubeGrid.CreateGridGroupLink (GridLinkTypeEnum.Mechanical, 55, grid as MyCubeGrid, d);
                        Log.Error ("CREATED");
                    }
                }
            } catch (Exception e) {
                Log.Error (e);
            }
            //try {
            //    foreach (var x in blocks) {
            //        var fat = x.FatBlock;
            //        var piston = fat as IMyPistonBase;
            //        if (piston != null) {
            //            
            //        }
            //    }
            //} catch(Exception e) {
            //    Log.Error(e);
            //}
        }

        public static void MakeConnectedStaticOrDynamic (IMyCubeGrid grid, bool toStatic) {
            //CreateGridGroupLink
            //
            //if (grid.HasBlocksInVoxels()) {
            //
            //}
            //
            //foreach (var x in blocks) {
            //    var fat = x.FatBlock;
            //    var landing = fat as IMyLandingGear;
            //    if (landing != null && landing.IsLocked) {
            //        if (fat.GetAttachedEntity ()) {
            //
            //        }
            //    }
            //}
            //
            //
            var connections = MyAPIGateway.GridGroups.GetGroup (grid, GridLinkTypeEnum.Physical);
            foreach (var x in connections) {
                if (toStatic) {
                     (x as MyCubeGrid).ConvertToStatic();
                } else {
                     (x as MyCubeGrid).OnConvertToDynamic();
                }
            }
        }

        public static void InitActions () {
            var toStatic = MyAPIGateway.TerminalControls.CreateAction<IMyMedicalRoom> ("MakeStatic");
            toStatic.Action = (b) => { MakeConnectedStaticOrDynamic (b.CubeGrid, true); };
            toStatic.Name = new StringBuilder("Make Static");
            toStatic.Enabled =  (b) => { return b.BlockDefinition.SubtypeId.Contains ("OwnershipTransfer"); };

            var toDynamic = MyAPIGateway.TerminalControls.CreateAction<IMyMedicalRoom> ("MakeDynamic");
            toDynamic.Action = (b) => { MakeConnectedStaticOrDynamic (b.CubeGrid, false); };
            toDynamic.Name = new StringBuilder("Make Dynamic");
            toDynamic.Enabled =  (b) => { return b.BlockDefinition.SubtypeId.Contains ("OwnershipTransfer"); };

            //var spawn = MyAPIGateway.TerminalControls.CreateAction<IMyMedicalRoom> ("Spawn");
            //spawn.Action = (b) => { Spawn(b); };
            //spawn.Name = new StringBuilder("Spawn");
            //spawn.Enabled =  (b) => { return b.BlockDefinition.SubtypeId.Contains ("OwnershipTransfer"); };

            MyAPIGateway.TerminalControls.AddAction<IMyMedicalRoom> (toStatic);
            MyAPIGateway.TerminalControls.AddAction<IMyMedicalRoom> (toDynamic);
            //MyAPIGateway.TerminalControls.AddAction<IMyMedicalRoom> (spawn);

            /*var SpeedControl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyAssembler>("OwnershipTransferer_Subgrids");
            SpeedControl.Title = MyStringId.GetOrCompute("Include subgrids");
            SpeedControl.Tooltip = MyStringId.GetOrCompute("Include subgrids");
            SpeedControl.Getter = (b) => { return b.GameLogic.GetAs<OwnershipTransferer>().subGrids.Value; };
            SpeedControl.Setter = (b, v) => { b.GameLogic.GetAs<OwnershipTransferer>().subGrids.Value = v; };
            SpeedControl.Enabled = (b)=> b.BlockDefinition.SubtypeId.Contains ("OwnershipTransfer");
            SpeedControl.Visible = (b)=> b.BlockDefinition.SubtypeId.Contains ("OwnershipTransfer");
             MyAPIGateway.TerminalControls.AddControl<IMyAssembler>(SpeedControl);
             */
        }

        private void Block_OwnershipChanged(IMyTerminalBlock obj) {
            var owner = block.OwnerId;
            if (owner != block.BuiltBy()) {
                ((MyCubeGrid)block.CubeGrid).TransferBlocksBuiltByID (block.BuiltBy(), owner);
            }
        }
    }
}
