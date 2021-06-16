using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using static Draygo.API.HudAPIv2;
using VRage.Game;
using Sandbox.Game;
using Sandbox.ModAPI;
using Digi;
using ServerMod;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Slime;
using VRage;
using VRage.Game.ModAPI;
using VRageRender;
using VRage.Utils;
using Scripts.Shared;

namespace Scripts.Specials.ExtraInfo
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    class ShowInfo : MySessionComponentBase
    {
        public static StringBuilder Extra = new StringBuilder();
        public static String Extra2 = "";
        private Timer timer = new AutoTimer(3);

        HUDMessage hudInfo;
        HUDMessage hudInfoNoGC;
        HUDMessage hudInfoTools;

        List<IMyGasTank> tanks = new List<IMyGasTank>();
        List<MyFueledPowerProducer> gens = new List<MyFueledPowerProducer>();
        String searchGas = "Oil";
        String searchGas2 = "Oxygen";

        StringBuilder information1 = new StringBuilder();
        StringBuilder information2 = new StringBuilder();
        StringBuilder information3 = new StringBuilder();

        public Vector2D offset = new Vector2D(-0.0085f * 30, 0f);
        public Vector2D oilOffset = new Vector2D(1, -0.20);
        public Vector2D noGridCoresOffset = new Vector2D(-0.45, -0.63);
        public Vector2D noGridCoresOffset2 = new Vector2D(-0.45, -0.55);
        public Vector2D toolsOffset = new Vector2D(-0.06, -0.69);

        Dictionary<string, string> toolsInfo = new Dictionary<string, string>() {
            { "Welder", "Auto pull radius : 18m. Use Ctrl+Shift+MMB+(Alt) to upgrade/downgrade" },
            { "Welder2", "Auto pull radius : 25m. Use Ctrl+Shift+MMB+(Alt) to upgrade/downgrade" },
            { "Welder3", "Auto pull radius : 32m. Use Ctrl+Shift+MMB+(Alt) to upgrade/downgrade" },
            { "Welder4", "Auto pull radius : 40m. Use Ctrl+Shift+MMB+(Alt) to upgrade/downgrade" },

            { "AngleGrinder", "Auto push radius : 18m" },
            { "AngleGrinder2", "Auto push radius : 25m" },
            { "AngleGrinder3", "Auto push radius : 32m" },
            { "AngleGrinder4", "Auto push radius : 40m" },

            { "HandDrill",  "Autotransfer : 28m" },
            { "HandDrill2", "Autotransfer : 36m" },
            { "HandDrill3", "Autotransfer : 44m" },
            { "HandDrill4", "Can't mine ore! Perfect for creating tunnels" }
        };

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyVisualScriptLogicProvider.PlayerLeftCockpit -= LeaveCockpit;
            MyVisualScriptLogicProvider.PlayerEnteredCockpit -= EnterCockpit;
        }


        private void EnterCockpit(string entityName, long playerId, string gridName)
        {
            try
            {
                updateInfo();
            }
            catch (Exception e)
            {
                //Log.Error(e);
            }
        }


        private void LeaveCockpit(string entityName, long playerId, string gridName) { }

        public override void Draw()
        {
            if (GameBase.HudAPI == null || !GameBase.HudAPI.Heartbeat)
            {
                return;
            }

            

            if (timer.tick())
            {
                try
                {
                    hudInfo?.DeleteMessage();
                    hudInfoTools?.DeleteMessage();
                    hudInfoNoGC?.DeleteMessage();

                    information1.Clear();
                    information2.Clear();

                    updateInfo();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
            Extra.Clear();
        }

        public void updateInfo()
        { //TODO need optimizations
            var pp = MyAPIGateway.Session.Player;

            var cockpit = pp.Controller.ControlledEntity as IMyCockpit;
            if (cockpit != null)
            {
                GenerateOilInfo(cockpit, information1);
                GenerateLiquidInfo(cockpit, information1);
                GenerateFrictionInfo(cockpit, information1);
                GenerateAtmosphereInfo(cockpit, information1);
                GenerateThrusterLiftInfo(cockpit, information1);

                var mlt = cockpit.CubeGrid.GetShip()?.damageReduction ?? 1f;//( as MyCubeGrid).GridGeneralDamageModifier;
                if (mlt != 1f)
                {
                    ShowInfo.Extra.Append("Damage reduction: ").AppendLine(((int)(100 / mlt)) + "%");
                }

                var mlt2 = cockpit.CubeGrid.GetShip()?.forcesMultiplier ?? 1f;//( as MyCubeGrid).GridGeneralDamageModifier;
                if (mlt2 != 1f)
                {
                    ShowInfo.Extra.Append("Env forces: ").AppendLine(((int)(mlt2 * 100)) + "%");
                }

                information1.Append(Extra);
                information1.Append(Extra2);
                hudInfo = new HUDMessage(information1, oilOffset, offset, 10, 1, true, true, null, BlendTypeEnum.PostPP);
            }
            else
            {
                //hudInfoTools.Visible = true;
                GenerateHeldItemInfo(information2);
                hudInfoTools = new HUDMessage(information2, toolsOffset, offset, 10, 1, true, true, null, BlendTypeEnum.PostPP);

                information1.Append(Extra2);
                hudInfo = new HUDMessage(information1, oilOffset, offset, 10, 1, true, true, null, BlendTypeEnum.PostPP);
            }

            if (FrameExecutor.currentFrame % 60 < 30)
            {

                int infos = 0;
                information3.Clear();
                if (HasNoGridCores())
                {
                    infos++;
                    information3.Append("At least 1 ship\nwithout gridcore");
                }

                
                if (IsNearWithSafezone (2000))
                {
                    infos++;
                    if (information3.Length != 0) {
                        information3.Append("\n\n");
                        
                    }
                    information3.Append("Mining disabled\nnear safezones");
                }
                    
                if (infos != 0)
                {
                    hudInfoNoGC = new HUDMessage(information3, infos == 2 ? noGridCoresOffset2 : noGridCoresOffset, offset, 10, 1, true, true, null, BlendTypeEnum.PostPP);
                }
            }
        }

        public static bool IsNearWithSafezone (float distance)
        {
            if (MyAPIGateway.Session.Player == null) return false;
            var ent = MyAPIGateway.Session.Player.Character;
            if (ent == null) return false;

            var t = ent.WorldMatrix.Translation;
            foreach (var x in MySessionComponentSafeZones.SafeZones)
            {
                if ((x.WorldMatrix.Translation-t).Length() < distance + x.Radius) return true;
            }

            return false;
        }

        public static bool HasNoGridCores()
        {
            var grids = new HashSet<IMyCubeGrid>();
            foreach (var x in GameBase.instance.gridToShip)
            {
                grids.Add(x.Value.grid);
            }


            while (grids.Count > 0)
            {
                var x = grids.FirstElement();
                var conn = x.GetConnectedGrids(GridLinkTypeEnum.Physical);

                bool hasBeacon = false;
                foreach (var y in conn)
                {
                    grids.Remove(y);
                    var sh = y.GetShip();
                    if (sh == null) continue;
                    hasBeacon |= sh.beacons.Count > 0;
                }

                if (!hasBeacon)
                {
                    if (conn.Count == 1 && IsDropContainer(conn[0]))
                    {
                        continue;
                    }
                    return true;
                }
            }

            return false;
        }

        public static bool IsDropContainer(IMyCubeGrid grid)
        {
            if (grid.GridSizeEnum != MyCubeSize.Small) return false;
            int am = 0;
            foreach (var x in ((MyCubeGrid)grid).GetFatBlocks())
            {
                if (x is IMyTerminalBlock)
                {
                    am++;
                    if (x.DisplayNameText != null && !x.DisplayNameText.StartsWith("Special Content"))
                    {
                        return false;
                    }
                }
            }
            return am > 0;
        }



        public bool GenerateHeldItemInfo(StringBuilder info)
        {
            var ch = MyAPIGateway.Session.Player.Character;
            if (ch == null) return false;
            var tool = ch.EquippedTool as IMyHandheldGunObject<MyDeviceBase>;
            if (tool == null) return false;
            var sn = tool.DefinitionId.SubtypeName;
            if (!toolsInfo.ContainsKey(sn))
            {
                return false;
            }
            info.Append(toolsInfo[sn]);
            return true;
        }

        public void GenerateAtmosphereInfo(IMyCockpit cockpit, StringBuilder info)
        {
            //Ship ship = cockpit.CubeGrid.GetShip();
            //ship.AtmosphereProperties.updateAtmosphere(cockpit.CubeGrid);
            //var d = ship.AtmosphereProperties.getAtmosphereDensity();
            //TODO Rewrite 
            var PoO = cockpit.WorldMatrix.Translation;
            double atmo = 0;

            foreach (var plan in GameBase.instance.planets)
            {
                var grav = plan.Value.Components.Get<MyGravityProviderComponent>();
                if (grav.IsPositionInRange(cockpit.WorldMatrix.Translation))
                {
                    if (plan.Value.HasAtmosphere)
                    {
                        atmo += MathHelper.Clamp(plan.Value.GetAirDensity(PoO) - 0.77f, 0f, 1f) * 4; // for earth planet
                    }
                }
            }

            info.Append("Air density:").Append(atmo.toHumanQuantity()).Append("\n");
        }

        public void GenerateFrictionInfo(IMyCockpit cockpit, StringBuilder info)
        {
            var ship = cockpit.CubeGrid.GetShip();

            if (ship != null)
            {
                var elevation = ship.getElevation2();
                if (elevation < 0)
                {
                    elevation = 9999999d;
                }

                var el = AirFriction.getElevation(elevation);
                var large = ship.grid.GridSizeEnum == VRage.Game.MyCubeSize.Large;

                var max = AirFriction.getMaxSpeed(large, el);
                var min = AirFriction.getFrictionStart(large, el);

                info.Append("Speed: ").Append(min).Append("-").Append(max).Append("m/s").Append("\n");//.Append($"{cockpit.CustomName} : {elevation}")
            }
        }

        public void GenerateLiquidInfo(IMyCockpit cockpit, StringBuilder info)
        {
            var grids = cockpit.CubeGrid.GetConnectedGrids(GridLinkTypeEnum.Logical);
            MyFixedPoint mass = 0;
            foreach (var g in grids)
            {
                g.FindBlocks((x) => {
                    if (x.FatBlock == null) return false;
                    var fat = x.FatBlock;
                    var tank = fat as IMyGasTank;
                    if (tank != null)
                    {
                        mass += tank.GetInventory(0).CurrentMass;
                        return false;
                    }
                    return false;
                });
            }

            if (mass > 0)
            {
                info.Append("Fuel Mass: ").Append(((double)mass).toHumanWeight()).Append("\n");
            }
        }

        public bool GenerateThrusterLiftInfo(IMyCockpit cockpit, StringBuilder info)
        {
            if (cockpit.CubeGrid.Physics == null) return false;

            var g = cockpit.CubeGrid.Physics.Gravity;

            if (g.Length() < 0.05) return false;

            var ship = cockpit.CubeGrid.GetShip();
            var sum = 0d;
            foreach (var x in ship.thrusters)
            {
                if (!x.IsFunctional) continue;
                sum += MathHelper.Clamp(Vector3D.Normalize(g).Dot(x.WorldMatrix.Forward), 0, 1) * x.MaxEffectiveThrust;
            }

            var thrust = sum / g.Length();

            if (thrust > 0)
            {
                info.Append("Max takeoff mass:").AppendLine(thrust.toPhysicQuantity(" kg"));
            }
            return true;
        }

        public bool GenerateOilInfo(IMyCockpit cockpit, StringBuilder info)
        {
            gens.Clear();
            tanks.Clear();

            var fuelMass = 0;

            cockpit.CubeGrid.FindBlocks((x) => {
                if (x.FatBlock == null) return false;
                var fat = x.FatBlock;

                if (!(fat is IMyFunctionalBlock))
                {
                    return false;
                }

                //information.Append ((fat as IMyFunctionalBlock).CustomName).Append(" ").Append (fat.GetType().Name).Append (fat is IMyPowerProducer ? "true"  : "false").Append ("/").Append (fat is SandBox.IMyGasTank ? "true"  : "false").Append("\n");
                if (fat is MyFueledPowerProducer)
                {
                    var def = x.BlockDefinition as MyGasFueledPowerProducerDefinition;
                    if (def != null && def.Fuel.FuelId.SubtypeName == searchGas)
                    {
                        gens.Add(fat as MyFueledPowerProducer);
                    }
                    return false;
                }


                if (fat is IMyGasTank)
                {
                    var def = (x.BlockDefinition as MyGasTankDefinition);
                    if (def != null && def.StoredGasId.SubtypeName == searchGas)
                    {
                        tanks.Add(fat as IMyGasTank);
                    }
                    return false;
                }
                return false;
            });

            var haveFuel = 0d;
            var maxFuel = 0d;
            var maxConsume = 0d;
            var Consume = 0d;
            var output = 0d;
            foreach (var x in tanks)
            {
                haveFuel += x.FilledRatio * x.Capacity;
                maxFuel += x.Capacity;
            }

            var mlt = 0f;

            foreach (var x in gens)
            {
                var def = (MyGasFueledPowerProducerDefinition)(((IMyFunctionalBlock)x).SlimBlock.BlockDefinition);
                haveFuel += x.Capacity;
                maxFuel += def.FuelCapacity;
                mlt = def.FuelProductionToCapacityMultiplier;
                var mw = x.CurrentOutput;
                maxConsume += def.MaxPowerOutput / mlt;
                Consume += x.CurrentOutput / mlt;
                output += mw;
            }

            if (tanks.Count + gens.Count != 0)
            {
                //info.Append ("Oil Tanks/Gens:").Append(tanks.Count).Append("/").Append(gens.Count).Append("\n");
                info.Append("Filled:").Append(((haveFuel / (maxFuel == 0 ? 1 : maxFuel)) * 100).toHumanQuantity()).Append("%\n");
                //information.Append ("Output:").Append(output).Append("\n");
                //information.Append ("MaxConsumeFuel:").Append(maxConsume).Append("\n");
                //info.Append ("Fuel usage:").Append(Consume.toHumanQuantityCeiled()).Append("/").Append(maxConsume.toHumanQuantityCeiled()).Append(" L/s\n");
                info.Append("Average left:").Append(Consume == 0 ? "Infinite" : (haveFuel / Consume).toHumanTime()).Append("\n");
                info.Append("Maximum left:").Append(Consume == 0 ? "Infinite" : (haveFuel / maxConsume).toHumanTime()).Append("\n");
                //information.Append ("Mlt:").Append(mlt);//.Append("\n");
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}