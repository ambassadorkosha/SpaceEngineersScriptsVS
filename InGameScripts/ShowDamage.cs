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
namespace ScriptShowDamage
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        public const bool SHOW_DAMAGED = true;
        public const bool SHOW_INCONSTRUCTION = true;

        public const bool SHOW_NONAMEOWNER = true;
        public const string NAME_PREFIX_DAMAGE = "Требуется ремонт: ";
        public const string NAME_PREFIX_CONSTRUCT = "Недострой: ";
        public const string NAME_PREFIX_NONAME = "Ничейное: ";


        static bool enabled = true;


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                case "on":
                    enabled = true;
                    break;
                case "off":
                    enabled = false;
                    break;
            }
            Echo(argument);
            Echo(updateSource.ToString());
            Echo(DateTime.Now.ToString());

            List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(Blocks);
            for (int i = 0; i < Blocks.Count; i++)
            {
                IMyTerminalBlock block = Blocks[i];
                IMySlimBlock slim = block.CubeGrid.GetCubeBlock(block.Position);
                int change = 0; // 0 no change, 1 show, 2 hide 
                bool prefixed = (block.CustomName.StartsWith(NAME_PREFIX_DAMAGE) || block.CustomName.StartsWith(NAME_PREFIX_CONSTRUCT) || block.CustomName.StartsWith(NAME_PREFIX_NONAME));
                bool should_show = false;
                string b_name = block.CustomName;

                if (block as IMyThrust != null)
                {
                    int idx = b_name.LastIndexOf('(') - 1;
                    if (idx >= 0)
                        b_name = b_name.Substring(0, idx);
                }

                if (SHOW_DAMAGED && enabled)
                {
                    if (slim.CurrentDamage > 0)
                    {
                        should_show = true;
                        if (!prefixed)
                        {
                            block.CustomName = NAME_PREFIX_DAMAGE + b_name;
                            change = 1;
                            prefixed = true;
                        }
                    }
                }

                if (SHOW_INCONSTRUCTION && enabled && !should_show)
                {
                    if (slim.BuildIntegrity < slim.MaxIntegrity)
                    {
                        should_show = true;
                        if (!prefixed)
                        {
                            block.CustomName = NAME_PREFIX_CONSTRUCT + b_name;
                            change = 1;
                            prefixed = true;
                        }
                    }
                }

                if (SHOW_NONAMEOWNER && enabled && !should_show)
                {
                    if (block.OwnerId == 0)
                    {
                        should_show = true;
                        if (!prefixed)
                        {
                            block.CustomName = NAME_PREFIX_NONAME + b_name;
                            change = 1;
                            prefixed = true;
                        }
                    }
                }

                if (!should_show && prefixed)
                {
                    block.CustomName = b_name.Substring(b_name.IndexOf(' ') + 1);
                    change = 2;
                    prefixed = false;
                }

                switch (change)
                {
                    case 1:
                        if (!block.ShowOnHUD)
                            block.SetValueBool("ShowOnHUD", true);
                        break;
                    case 2:
                        if (block.ShowOnHUD)
                            block.SetValueBool("ShowOnHUD", false);
                        break;
                }
            }
        }
        //------------END--------------
    }
}