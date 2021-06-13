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
using VRage.Game.GUI.TextPanel;
using System.Linq;

namespace Inventory
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------

        public Program()
        {
            Echo("Script ready to be launched..\n");
            //assembleMargin /= 100;
            //disassembleMargin /= 100;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string args)
        {
            try
            { 

            }
        }

        public void Save()
        {
        
        }

        //------------END--------------
    }
}