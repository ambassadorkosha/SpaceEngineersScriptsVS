using System;
using System.Collections.Generic;
using System.Text;
using Digi;
using Digi2.AeroWings;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Shared.Serialization;
using Scripts.Specials.Blocks.Reactions;
using Scripts.Specials.Messaging;
using ServerMod;
using Slime;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Scripts.Specials.Blocks
{

    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
    public class CockpitUpgrade3 : MyGameLogicComponent
    {
        private static bool INITED = false;
        private static Connection<IdData> Connection;

        private Ship Ship;
        private IMyCockpit Controller;
        

        static void HandleRequest (IdData data, ulong player, bool isFromServer)
        {
            if (isFromServer) return;
            if (data == null) return;
            var cockpit = data.Id.As<IMyCockpit>();
            RestoreFromVoxels(cockpit, player);
        }

        public static void Init ()
        {
            Connection = new Connection<IdData>(42267, HandleRequest);
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        { 
            Controller = Entity as IMyCockpit;
            if (!INITED)
            {
                INITED = true;
                InitActions();
            }
        }

        public static void RestoreFromVoxels(IMyCockpit cockpit, ulong player)
        {
            if (cockpit == null || cockpit.Pilot == null) return;
            var pl = cockpit.GetPlayer();
            if (pl == null) return;
            if (player != pl.SteamUserId) return;
            long identity = pl.IdentityId;

            if (cockpit == null) return;
            var sh = cockpit.CubeGrid.GetShip();
            if (sh == null) return;
            sh.updateClosestPlanet();
            if (sh.closestPlanet == null) return;
            var planet = sh.closestPlanet;
            var min = planet.MinimumRadius * 0.9;
            if ((cockpit.WorldMatrix.Translation - planet.WorldMatrix.Translation).LengthSquared() > min * min)
            {
                Common.SendChatMessage("You need fall a bit deeper, to use this function", "Voxel escape tool", identity, "Red");
                return;
            }

            var grids = cockpit.CubeGrid.GetConnectedGrids(GridLinkTypeEnum.Physical);
            var aabb = new BoundingBoxD(cockpit.WorldAABB.Min, cockpit.WorldAABB.Max);
            foreach (var g in grids)
            {
                aabb.Include(g.WorldAABB);
            }

            var pos = planet.GetClosestSurfacePointGlobal(cockpit.WorldMatrix.Translation);
            foreach (var g in grids)
            {
                g.Physics.AngularVelocity = Vector3.Zero;
                g.Physics.LinearVelocity = Vector3.Zero;
            }

            var vec = (pos - planet.WorldMatrix.Translation);
            vec.Normalize();
            pos = pos + vec * (aabb.Size.Max()+5);

            var m = cockpit.CubeGrid.WorldMatrix;
            m.Translation = pos;
            
            cockpit.CubeGrid.WorldMatrix = m;

            FrameExecutor.addFrameLogic(new GridStopper(cockpit.CubeGrid));
        }

        private class GridStopper : Action1<long>
        {
            IMyCubeGrid grid;
            int time = 1800;
            public GridStopper (IMyCubeGrid grid)
            {
                this.grid = grid;
            }

            public void run(long t)
            {
                time--;
                float maxSpeed = 10;
                if (grid != null && !grid.MarkedForClose && grid.Physics != null && !grid.Physics.IsStatic)
                {
                    if (grid.Physics.LinearVelocity.LengthSquared () > maxSpeed* maxSpeed)
                    {
                        var v = grid.Physics.LinearVelocity;
                        v.Normalize();
                        grid.Physics.LinearVelocity = v * 10;
                    }
                }

                if (time <= 0) {
                    FrameExecutor.removeFrameLogic(this);
                }
            }
        }

        private static void InitActions()
        {
            var AlignOnOff = MyAPIGateway.TerminalControls.CreateAction<IMyCockpit>("Cockpit_GetOutOffVoxels");
            AlignOnOff.Action = (b) =>
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    RestoreFromVoxels (b as IMyCockpit, MyAPIGateway.Session.Player?.SteamUserId ?? 0);
                } 
                else
                {
                    Connection.SendMessageToServer(new IdData() { Id = b.EntityId });
                }
            };
            AlignOnOff.Name = new StringBuilder("Get out of voxels");
            AlignOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            AlignOnOff.Writer = (b, sb) => sb.Append("Get out");
            AlignOnOff.ValidForGroups = true;
            AlignOnOff.Enabled = (b) => b.GetAs<CockpitUpgrade3>() != null;

            MyAPIGateway.TerminalControls.AddAction<IMyCockpit>(AlignOnOff);
            
        }
    }
}
