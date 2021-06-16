using System;
using System.Collections.Generic;
using Digi;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Game;
using ServerMod.Radar;
using Sandbox.Game.Entities;
using Scripts;
using Scripts.Specials.Economy;
using Scripts.Specials.AutoTools;
using Scripts.Specials.GPS;
using Scripts.Specials;
using Draygo.API;
using Scripts.Shared;
using Scripts.Specials.Blocks;
using ServerMod.Specials;
using Scripts.Specials.Doors;
using Slime;
using Scripts.Specials.SlimGarage;
using Digi2.AeroWings;
using Scripts.Specials.Blocks.Cockpit;
using Scripts.Specials.Safezones;
using Scripts.Specials.Trader;
using Sandbox.Definitions;
using VRage.ObjectBuilders;

namespace Scripts.Specials
{
    class ArmorRebalance
    {
        public static MyObjectBuilderType ARMOR_BLOCK = MyObjectBuilderType.Parse("MyObjectBuilder_CubeBlock");

        public static void Init ()
        {
            MyAPIGateway.Session.OnSessionReady += Session_OnSessionReady;
            RepatchArmor();
        }

        private static void Session_OnSessionReady()
        {
            RepatchArmor();
        }

        

        private static void RepatchArmor()
        {
            try
            {
                var allBlocks = MyDefinitionManager.Static.GetAllDefinitions();
                foreach (var x in allBlocks)
                {
                    if (x.Id.TypeId != ARMOR_BLOCK) continue;

                    var c = x as MyCubeBlockDefinition;
                    if (c != null && x.GetType() == typeof(MyCubeBlockDefinition))
                    {


                        if (x.Context.ModId != null)
                        {
                            if (x.Context.ModId == "1581994759.sbm")
                            {
                                ProcessCubeBlock(c);
                            }
                            else if (x.Context.ModId == "1481738859.sbm")
                            {
                                c.Enabled = false;
                                c.Public = false;
                            } else //if (x.Context.ModId == "1790443632.sbm") // Nebula mod pack
                            {
                                var sn = x.Id.SubtypeName;
                                if (sn.StartsWith("aero")) continue;
                                if (sn.StartsWith("Cat")) continue;
                                if (sn.StartsWith("Rai")) continue;
                                if (sn.Contains("Window")) continue;
                                if (sn.Contains("Storage")) continue;
                                if (sn.StartsWith("Grated")) continue;
                                if (sn.StartsWith("NebWing")) continue;
                                if (sn.StartsWith("Plane")) continue;
                                if (sn.StartsWith("Limit")) continue;

                                ProcessCubeBlock (c);
                            }
                        } 
                        else
                        {
                            if (c.CubeDefinition != null) // vanilla have CubeDefinition's
                            {
                                ProcessCubeBlock(c);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.ChatError("Couldn't repatch armor blocks");
            }
        }

        private static void ProcessCubeBlock(MyCubeBlockDefinition c)
        {
            float v = (float)c.Size.Volume();
            v = v / (float)Math.Pow(v, 0.72);
            if (c.Id.SubtypeName.Contains("Heavy"))
            {
                if (c.CubeSize == MyCubeSize.Large)
                {
                    c.DeformationRatio = 0.4f;
                    c.DisassembleRatio = 3f;
                    c.GeneralDamageMultiplier = 0.5f * v;
                }
                else
                {
                    c.DeformationRatio = 0.32f;
                    c.DisassembleRatio = 2.5f;
                    c.GeneralDamageMultiplier = 0.5f * v;
                }
            }
            else
            {
                
                if (c.CubeSize == MyCubeSize.Large)
                {
                    c.DeformationRatio = 0.4f;
                    c.DisassembleRatio = 3f;
                    c.GeneralDamageMultiplier = 0.11f * v;
                }
                else
                {
                    c.DeformationRatio = 0.32f;
                    c.DisassembleRatio = 2.5f;
                    c.GeneralDamageMultiplier = 0.11f * v;
                }
            }
        }
    }
}
