using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Common.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRage.Game.Entity;
using ProtoBuf;
using Sandbox.Game.World;
using ServerMod;
using VRage.Game;
using System.Collections;

namespace Scripts.Specials.SlimGarage
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), true, "LargeGarage")]
    public class GarageBlockLogic : MyGameLogicComponent
    {
        public static readonly MyStringId Separator = MyStringId.GetOrCompute("—————— vVv Double click vVv ————");
        public static readonly MyStringId Separator2 = MyStringId.GetOrCompute("——————————————————————————————————— ");
        public BlockState m_currentStatus;
        public string ShipInfo = "";
        
        private static bool m_controlsCreated = false;
        private static MyTerminalControlListBoxItem m_selectedItem;
        private bool m_shouldSpin = true;
        private bool m_keepBuilder;
        private bool m_init = false;
        private int m_modelId;
        private MyEntitySubpart m_subpart1;
        private MyEntitySubpart m_subpart2;
        private MyEntitySubpart m_subpart3;
        private MyParticleEffect m_effect;
        private MyCubeGrid m_selectedGrid;
        private IMyFunctionalBlock m_block;
        private Dictionary<string, DateTime> m_btnCooldowns = new Dictionary<string, DateTime>();
        

        public bool KeepBuilder
        {
            get { return m_keepBuilder; }
            set
            {
                if (m_block.BuiltBy() == MyAPIGateway.Session.Player.IdentityId)
                { m_keepBuilder = value; SendSetStatusReq(); }
            }
        }

        public override void Init(MyObjectBuilder_EntityBase ob)
        {
            base.Init(ob);

            if (!(Container.Entity as IMyFunctionalBlock).HasInventory)
            {
                var inventory = new Sandbox.Game.MyInventory(8000000f, new Vector3(200d, 200d, 200d), MyInventoryFlags.CanReceive);
                Entity.Components.Add<MyInventoryBase>(inventory);
            }

            if (MyAPIGateway.Multiplayer.IsServer)
                return;

            //if (SlimGarage.GarageSubtypesHash.Contains((Entity as IMyProjector).SlimBlock.BlockDefinition.Id.SubtypeId))
            //{
            // SlimGarage.WriteToLogDbg("GarageBlockLogic initialized 1.2.);what?
            //}            

            if (!m_controlsCreated)
            {
                BlockControls.CreateControls();
                m_controlsCreated = true;
            }
        }

        private void MyInit()
        {
            try
            {                
                m_block = Container.Entity as IMyFunctionalBlock;
                m_block.AppendingCustomInfo += AppendCustomInfoEventHandler;
                m_block.OnMarkForClose += OnOnMarkForClose;
                MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;
                m_currentStatus = BlockState.None;
                SlimGarage.Comms.SendGetStatusFromServerReq(m_block.EntityId);
                SlimGarage.Comms.OnBlockStatusEvent += UpdateStatus;
                UpdateDummies();
                m_block.EnabledChanged += UpdateAnim;
                m_block.IsWorkingChanged += UpdateAnim;
                m_block.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                m_init = true;
                UpdateAnim(m_block);
            }
            catch (Exception ex) { SlimGarage.WriteToLogDbg($"MyInit() Exception: \n{ex}"); }
        }

        public override void UpdateBeforeSimulation()
        {
            if (!m_shouldSpin) return;

            if (m_subpart1 != null)
            {
                m_subpart1.PositionComp.LocalMatrix = Matrix.CreateRotationX(0.02f) * m_subpart1.PositionComp.LocalMatrixRef;
                //m_subpart1.PositionComp.LocalMatrix = Matrix.CreateRotationX(0.02f) * m_subpart1.PositionComp.LocalMatrixRef;
            }
            if (m_subpart2 != null)
            {
                m_subpart2.PositionComp.Scale = 0.8f;
                m_subpart2.PositionComp.LocalMatrix = Matrix.CreateRotationX(-0.02f) * Matrix.CreateRotationY(-0.02f) * m_subpart2.PositionComp.LocalMatrixRef;
            }
            if (m_subpart3 != null)
            {
                m_subpart3.PositionComp.Scale = 0.64f;
                m_subpart3.PositionComp.LocalMatrix = Matrix.CreateRotationX(0.02f) * Matrix.CreateRotationY(0.02f) * Matrix.CreateRotationZ(0.02f) * m_subpart3.PositionComp.LocalMatrixRef;
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if ((MyAPIGateway.Session == null) || (MyAPIGateway.Utilities == null)) return;

            if (MyAPIGateway.Multiplayer.IsServer) return;


            if (!m_init) MyInit();

            if (m_controlsCreated)
            {
                UpdateMyControls();
            }

            if (m_subpart1 == null || m_subpart1.MarkedForClose || m_subpart1.Closed)
            {
                m_subpart1 = null;
                m_subpart2 = null;
                m_subpart3 = null;
                UpdateDummies();
            }
        }

        private void OnOnMarkForClose(IMyEntity obj)
        {
            m_block.AppendingCustomInfo -= AppendCustomInfoEventHandler;
            m_block.EnabledChanged -= UpdateAnim;
            m_block.IsWorkingChanged -= UpdateAnim;
            MyAPIGateway.TerminalControls.CustomControlGetter -= CustomControlGetter;
            SlimGarage.Comms.OnBlockStatusEvent -= UpdateStatus;
            RemoveParticle();
            m_block.OnMarkForClose -= OnOnMarkForClose;
        }
        private void RemoveParticle()
        {
            if (m_effect != null)
            {
                m_effect.StopEmitting();
                m_effect.StopLights();
                m_effect.Clear();
                m_effect.Close();
            }
        }

        private void UpdateStatus(BlockStatus new_status)
        {
            if (new_status.BlockEntId == m_block.EntityId)
            {
                //SlimGarage.WriteToLogDbg($"UpdateStatus() m_current_Status = {m_currentStatus.ToString()} SteamId [{new_status.SteamId}]  BlockEntId [{ new_status.BlockEntId}] State [{ new_status.State}] ShareBuilder [{ new_status.ShareBuilder}] SavedShipInfo [{ new_status.SavedShipInfo}]");
                m_keepBuilder = new_status.ShareBuilder;//USE ONLY PRIVATE m_keepBuilder
                m_currentStatus = new_status.State;
                ShipInfo = new_status.SavedShipInfo;
                if (new_status.SteamId == MyAPIGateway.Session.LocalHumanPlayer.SteamUserId)
                {
                   // SlimGarage.WriteToLogDbg("UpdateStatus() Message for us.");
                    if (new_status.Cooldowns != null)
                    {
                        if (new_status.Cooldowns.Any())
                        {
                            foreach (var btn in new_status.Cooldowns)
                            {
                                if (m_btnCooldowns.ContainsKey(btn.Key))
                                {
                                    m_btnCooldowns[btn.Key] = btn.Value;
                                }
                                else
                                {
                                    m_btnCooldowns.Add(btn.Key, btn.Value);
                                }
                            }
                        }
                    }
                }
            }
        }

        #region Actions for controls
        private void SendSetStatusReq()
        {
            SlimGarage.Comms.SendSetStatusReq(MyAPIGateway.Session.Player.SteamUserId, m_block.EntityId, KeepBuilder);
        }

        public bool CheckBoxEnabled(IMyTerminalBlock block)
        {
            if (block.BuiltBy() == MyAPIGateway.Session.Player.IdentityId) return true;
            return false;
        }

        private void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            try
            {
                if (block is IMyProjector)
                {
                    if (!SlimGarage.GarageSubtypes.Contains(block.BlockDefinition.SubtypeId))
                    {
                        return;
                    }

                    //SlimGarage.WriteToLogDbg("CustomControlGetter hit.");
                    controls.FindAndMove(2, (x) => x.Id == "RotZ");
                    controls.FindAndMove(2, (x) => x.Id == "RotY");
                    controls.FindAndMove(2, (x) => x.Id == "RotX");
                    controls.FindAndMove(2, (x) => x.Id == "Z");
                    controls.FindAndMove(2, (x) => x.Id == "Y");
                    controls.FindAndMove(2, (x) => x.Id == "X");
                    controls.FindAndMove(2, (x) => (x as IMyTerminalControlLabel)?.Label == Separator2);
                    controls.FindAndMove(2, (x) => x.Id == "SlimGarage.ClearButton");
                    controls.FindAndMove(2, (x) => (x as IMyTerminalControlLabel)?.Label == Separator);
                    controls.FindAndMove(2, (x) => x.Id == "SlimGarage.SaveButton");
                    controls.FindAndMove(2, (x) => x.Id == "SlimGarage.LoadButton");
                    controls.FindAndMove(2, (x) => x.Id == "SlimGarage.ShowButton");
                    controls.FindAndMove(2, (x) => x.Id == "SlimGarage.GridsList");
                    controls.FindAndMove(2, (x) => x.Id == "SlimGarage.KeepBuilderChkBox");
                    (controls.Find((x) => x.Id == "X") as IMyTerminalControlSlider).SetLimits(-150, 150);
                    (controls.Find((x) => x.Id == "Y") as IMyTerminalControlSlider).SetLimits(-150, 150);
                    (controls.Find((x) => x.Id == "Z") as IMyTerminalControlSlider).SetLimits(-150, 150);
                    if (m_controlsCreated)
                    {
                        m_block.RefreshCustomInfo(); //maybe not needed.
                    }
                }
            }
            catch (Exception ex)
            {
                SlimGarage.WriteToLogDbg($"CustomControlGetter() Exception: \n{ex}");
            }
        }

        public static void GetGridsAround(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> listItems, List<MyTerminalControlListBoxItem> selectedItems)
        {
            try
            {
                listItems.Clear();
                var temp = ServerMod.GameBase.instance.gridToShip.Values.Where(x => GridConditions(x.grid, block));
                var GridGruops = new Dictionary<long, KeyValuePair<int, List<IMyCubeGrid>>>();
                foreach (var ship in temp)
                {
                    var gridGruop = MyAPIGateway.GridGroups.GetGroup(ship.grid, GridLinkTypeEnum.Logical);
                    gridGruop.SortNoAlloc((x, y) => (x as MyCubeGrid).BlocksCount.CompareTo((y as MyCubeGrid).BlocksCount));
                    gridGruop.Reverse();
                    gridGruop.SortNoAlloc((x, y) => x.GridSizeEnum.CompareTo(y.GridSizeEnum));

                    int blocks = 0;
                    foreach (var gridd in gridGruop)
                    {
                        blocks += (gridd as MyCubeGrid).BlocksCount;
                    }
                    GridGruops[gridGruop[0].EntityId] = new KeyValuePair<int, List<IMyCubeGrid>>(blocks, gridGruop);
                }

                var a = GridGruops.OrderByDescending((x) => x.Value.Key);
                foreach (var i in a)
                {
                    var gridgr = i.Value.Value;
                    int pcu = 0;
                    foreach (var gridd in gridgr)
                    {
                        pcu += (gridd as MyCubeGrid).BlocksPCU;
                    }
                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(gridgr[0].DisplayName),
                    MyStringId.GetOrCompute("PCU:" + pcu + " Blocks:" +
                    i.Value.Key + " SubGrids: " + (gridgr.Count - 1)), gridgr[0]);
                    listItems.Add(item);
                }

                selectedItems.Clear();
                if (listItems.Count == 1)
                {
                    selectedItems.Add(listItems[0]);
                    block.GetAs<GarageBlockLogic>().m_selectedGrid = (MyCubeGrid)listItems[0].UserData;
                    m_selectedItem = listItems[0];
                }
                else if (listItems.Count == 0)
                {
                    m_selectedItem = null;
                }
                else
                {
                    if (listItems.Count == 0 || m_selectedItem == null)
                        return;
                    var tempp = listItems.FirstOrDefault(x => x.Text == m_selectedItem.Text);
                    selectedItems.Add(tempp);
                    block.GetAs<GarageBlockLogic>().m_selectedGrid = (MyCubeGrid)tempp.UserData;
                }

                block.RefreshCustomInfo();
            }
            catch (Exception ex)
            {
                SlimGarage.WriteToLogDbg($"GridsAround() Exception: \n{ex}");
            }
        }

        public void Click(IMyTerminalControlButton btn)
        {
            try
            {
                var btn_name = btn.Id;
                var currentTime = DateTime.Now.ToUniversalTime();
                var usecooldown = new TimeSpan(0, 0, 0, 3);
                DateTime tmp;
                if (m_btnCooldowns.TryGetValue(btn_name, out tmp))
                {
                    m_btnCooldowns[btn_name] = currentTime.Add(usecooldown);
                }
                else
                {
                    m_btnCooldowns.Add(btn_name, currentTime.Add(usecooldown));
                }

                if (btn_name == "SlimGarage.ShowButton")
                {
                    SlimGarage.Comms.SendShowReq(MyAPIGateway.Session.Player.SteamUserId, m_block.EntityId);
                    btn.UpdateVisual();
                    return;
                }


                if (btn_name == "SlimGarage.SaveButton")
                {
                    SlimGarage.Comms.SendSaveReq(MyAPIGateway.Session.Player.SteamUserId, m_selectedGrid.EntityId, m_block.EntityId);
                    m_selectedItem = null;
                    m_selectedGrid = null;
                    m_currentStatus = BlockState.Busy;
                    return;
                }

                if (btn_name == "SlimGarage.LoadButton")
                {
                    if ((m_block as IMyProjector).ProjectedGrid != null)
                    {
                        SlimGarage.Comms.SendLoadReq(MyAPIGateway.Session.Player.SteamUserId, 0, m_block.EntityId, (m_block as IMyProjector).ProjectedGrid.PositionComp.WorldMatrixRef, (m_block as IMyProjector).ProjectedGrid.PositionComp.WorldAABB);
                        m_currentStatus = BlockState.Busy;
                        return;
                    }
                    else
                    {
                        SlimGarage.WriteToLogDbg("Click LoadButton but projection is null");
                        return;
                    }
                }

                if (btn_name == "SlimGarage.ClearButton")
                {
                    if ((tmp - currentTime).TotalSeconds >= 0.400 && (tmp - currentTime).TotalSeconds <= 5)
                    {                        
                        SlimGarage.Comms.SendClearReq(MyAPIGateway.Session.Player.SteamUserId, m_block.EntityId);
                        btn.UpdateVisual();
                        m_currentStatus = BlockState.Busy;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SlimGarage.WriteToLogDbg($"Click() [controls] Exception: \n{ex}");
            }
        }

        public bool ButtonsCooldown(IMyTerminalControlButton detailsButton)
        {
            try
            {
                if (m_currentStatus == BlockState.Busy)
                {
                    return false;
                }

                if (!m_block.IsWorking)
                {
                    return false;
                }

                var btn_name = detailsButton.Id;
                DateTime blocked_untill;
                var time = DateTime.Now.ToUniversalTime();
                if (m_btnCooldowns.TryGetValue(btn_name, out blocked_untill))
                {
                    if (blocked_untill >= time)
                    {                       
                        return false;
                    }
                }

                if (m_currentStatus == BlockState.Empty_ReadyForSave)
                {
                    if (m_selectedGrid?.Physics?.LinearVelocity.Length() > 2)
                    {
                        return false;
                    }
                }

                if (m_currentStatus == BlockState.Contains_ReadyForLoad && detailsButton.Id == "SlimGarage.LoadButton")
                {
                    if (!(m_block as IMyProjector).IsProjecting) return false;
                }

                if (m_block?.CubeGrid?.Physics?.LinearVelocity.Length() > 2f | m_block?.CubeGrid?.Physics?.AngularVelocity.Length() > 2f)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                SlimGarage.WriteToLogDbg($"ButtonsCooldown() [controls] Exception: \n{ex}");
                return false;
            }
        }

        private static bool GridConditions(IMyCubeGrid grid, IMyTerminalBlock block)
        {
            bool result = true;
            if (grid == null || block == null) return false;

            if (grid is MyCubeGrid)
            {
                var group = MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Logical);
                result &= grid != block?.CubeGrid;
                result &= !group.Contains(block.CubeGrid);
                result &= Vector3D.DistanceSquared(block.WorldMatrix.Translation, grid.PositionComp.GetPosition()) <= 1000 * 1000;
                result &= !(grid as MyCubeGrid).IsPreview;
            }
            else return false;
            return result;
        }

        public static void SetSelectedGrid(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> listItems)
        {
            try
            {               
                block.GetAs<GarageBlockLogic>().m_selectedGrid = (MyCubeGrid)listItems[0].UserData;
                m_selectedItem = listItems[0];
                block.GetAs<GarageBlockLogic>().UpdateMyControls();
            }
            catch (Exception ex)
            {
                SlimGarage.WriteToLogDbg($"SetSelectedGrid Exception: \n{ex}");
            }
        }

        /// <summary>
        /// Call when on/off , power lost, model change???
        /// </summary>
        private void UpdateAnim(IMyEntity block)
        {
            if (!UpdateDummies()) return;
            if (m_block.IsWorking)
            {
                m_shouldSpin = true;
            }
            else
            {
                m_shouldSpin = false;
            }
        }

        private bool UpdateDummies()
        {
            bool subpartsfound = m_block.TryGetSubpart("Circle1", out m_subpart1);
            subpartsfound &= m_block.TryGetSubpart("Circle2", out m_subpart2);
            subpartsfound &= m_block.TryGetSubpart("Circle3", out m_subpart3);
            if (subpartsfound)
            {
                try
                {
                    m_subpart1.SetEmissiveParts("Emissive", new Color(0, 0, 20), 5000f);
                    m_subpart2.SetEmissiveParts("Emissive", new Color(0, 0, 20), 5000f);
                    m_subpart3.SetEmissiveParts("Emissive", new Color(0, 0, 20), 5000f);
                    var blockMatrix = MatrixD.Identity; //what is that slim help please.
                    var a = m_block.WorldMatrix.Translation;
                    RemoveParticle();
                    MyParticlesManager.TryCreateParticleEffect("GarageEffect", ref blockMatrix, ref a, Entity.Render.GetRenderObjectID(), out m_effect);
                    // m_effect.Velocity = m_block.Physics.LinearVelocity;
                }
                catch (Exception ex)
                {
                    SlimGarage.WriteToLogDbg($"UpdateDummies() Exception: \n{ex}");
                    return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        ///HACK! Keen not have a proper method for update my controls so i use this
        /// </summary>
        private void UpdateMyControls()
        {
            /*foreach (var control in m_controls)
            { //IMPORTANT: NOT WORKS!
            control.Value.RedrawControl();
            control.Value.UpdateVisual();
            }*/
            if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
            {
                try
                {
                    // SlimGarage.WriteToLogDbg("UpdateMyControls()");
                    m_block.RefreshCustomInfo();
                    var myCubeBlock = m_block as MyCubeBlock;
                    if (myCubeBlock.IDModule != null)
                    { //hack
                        var share = myCubeBlock.IDModule.ShareMode;
                        var owner = myCubeBlock.IDModule.Owner;
                        myCubeBlock.ChangeOwner(owner, share == VRage.Game.MyOwnershipShareModeEnum.None ? VRage.Game.MyOwnershipShareModeEnum.Faction : VRage.Game.MyOwnershipShareModeEnum.None);
                        myCubeBlock.ChangeOwner(owner, share);
                    }
                }
                catch (Exception ex)
                {
                    SlimGarage.WriteToLogDbg($"UpdateMyControls() Exception: \n{ex}");
                }
            }
        }

        private void AppendCustomInfoEventHandler(IMyTerminalBlock block, StringBuilder Info)
        {
            try
            {
                //SlimGarage.WriteToLogDbg("AppendCustomInfoEventHandler()");
                Info.Clear();
                var blockbuilder = block.BuiltBy();
                var playerid = MyAPIGateway.Session.LocalHumanPlayer.IdentityId;

                Info.AppendLine($" ------Garage------ ");
                if (blockbuilder == 0)
                {
                    Info.AppendLine("Builded by nobody.");
                }
                else
                {
                    if (blockbuilder == playerid)
                    {
                        Info.AppendLine("Builded by you.");
                    }
                    else
                    {
                        List<IMyIdentity> players = new List<IMyIdentity>();
                        MyAPIGateway.Players.GetAllIdentites(players);
                        var found = players.Where(x => x.IdentityId == blockbuilder);
                        var name = "UNKNOWN";
                        if(found.Count() >= 1)
                        {
                            name = found.First().DisplayName;
                        }
                        
                        Info.AppendLine($"Builded by: {name}.");
                    }
                }
                // Empty / Ship Inside / Busy
                string status;
                switch (m_currentStatus)
                {
                    case BlockState.Empty_ReadyForSave:
                        status = "Empty";
                        break;
                    case BlockState.Contains_ReadyForLoad:
                        status = "Ship Inside";
                        break;
                    case BlockState.Busy:
                        status = "Busy";
                        break;
                    default:
                        status = "Null";
                        break;
                }
                Info.AppendLine($"Status: [{status}]");
                if (m_btnCooldowns.Any())
                {
                    var currtime = DateTime.Now.ToUniversalTime();

                    foreach (var btn in m_btnCooldowns)
                    {
                        if (btn.Value > currtime)
                        {
                            //Save cooldown : hh:mm:ss
                            string name;
                            switch (btn.Key)
                            {
                                case "SlimGarage.LoadButton":
                                    name = "Load";
                                    break;
                                case "SlimGarage.ShowButton":
                                    name = "Show";
                                    break;
                                case "SlimGarage.SaveButton":
                                    name = "Save";
                                    break;
                                case "SlimGarage.ClearButton":
                                    name = "Grind";
                                    break;
                                default:
                                    name = "Null";
                                    break;
                            }
                            Info.AppendLine($"{name} cooldown: [{(uint)(btn.Value - currtime).Hours}:{(uint)(btn.Value - currtime).Minutes}:{(uint)(btn.Value - currtime).Seconds}]");
                        }
                    }
                }

                if (m_currentStatus == BlockState.Empty_ReadyForSave)
                {
                    if (m_selectedGrid != null)
                    {

                        if (m_selectedGrid.Physics.LinearVelocity.Length() > 2f)
                        {
                            Info.AppendLine($"Unable to use, stop your grid!");
                        }
                        else
                        {
                            var gridlist = new List<IMyCubeGrid>();
                            MyAPIGateway.GridGroups.GetGroup(m_selectedGrid, GridLinkTypeEnum.Logical, gridlist);
                            gridlist.SortNoAlloc((x, y) => -(x as MyCubeGrid).BlocksCount.CompareTo((y as MyCubeGrid).BlocksCount));
                            //gridlist.Reverse();
                            StringBuilder str = new StringBuilder("\nGrid to save: [");
                            foreach (var grid in gridlist)
                            {
                                str.Append(grid.DisplayName + ',');
                            }
                            //str.TrimEnd(1); prohibited
                            str.Append("]");
                            var temp = str.Remove(str.Length - 2, 1);
                            Info.Append(str);
                        }
                    }
                }

                if (m_currentStatus == BlockState.Contains_ReadyForLoad)
                {
                    Info.AppendLine($"{ShipInfo}");
                }

                if (m_currentStatus == BlockState.Contains_ReadyForLoad)
                {
                    if (m_block?.CubeGrid?.Physics?.LinearVelocity.Length() > 2f | m_block?.CubeGrid?.Physics?.AngularVelocity.Length() > 2f)
                    {
                        Info.AppendLine($"Unable to use, stop your grid!");
                    }
                }
            }
            catch (Exception ex)
            {
                SlimGarage.WriteToLogDbg($"AppendCustomInfoEventHandler() Exception: \n{ex}");
            }
            // Info.AppendLine($"Please select grid for details");
        }

        /// <summary>
        /// Draw my garage controls only on my block
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public bool Visibility(IMyTerminalControl control, IMyTerminalBlock block)
        {
            try
            {
                if (!SlimGarage.GarageSubtypes.Contains(block.BlockDefinition.SubtypeId))
                {
                    return false; //if not our block hide all
                }

                if (m_currentStatus == BlockState.None)
                {
                    return false;
                }

                switch (control.Id)
                {
                    case "SlimGarage.KeepBuilderChkBox":
                        if (m_currentStatus == BlockState.Busy)
                            return false;
                        break;
                    case "SlimGarage.SaveButton":
                        if (m_currentStatus == BlockState.Contains_ReadyForLoad)
                            return false;
                        break;
                    case "SlimGarage.LoadButton":
                        if (m_currentStatus == BlockState.Empty_ReadyForSave)
                            return false;
                        break;
                    case "SlimGarage.ClearButton":
                        if (m_currentStatus == BlockState.Empty_ReadyForSave)
                            return false;
                        break;
                    case "SlimGarage.GridsList":
                        if (m_currentStatus == BlockState.Contains_ReadyForLoad)
                            return false;
                        break;
                    case "SlimGarage.ShowButton":
                        if (m_currentStatus != BlockState.Contains_ReadyForLoad)
                            return false;
                        break;
                    default:
                        break;
                }
                //not in switch, keen bug with labels GetId() method
                if (control is IMyTerminalControlLabel)
                {
                    if ((control as IMyTerminalControlLabel).Label == Separator)
                    {
                        if (m_currentStatus != BlockState.Contains_ReadyForLoad)
                            return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                SlimGarage.WriteToLogDbg($"Visibility() [controls] Exception: \n{ex}");
                return true;
            }
        }
        #endregion
    }

    [ProtoContract]
    public enum BlockState
    {
        [ProtoEnum]
        None = 0,
        [ProtoEnum]
        Busy = 1,
        [ProtoEnum]
        Empty_ReadyForSave = 2,//any
        [ProtoEnum]
        Contains_ReadyForLoad = 3
    }
}
