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

namespace Script1
{
    public sealed class Program : MyGridProgram
    {
        //------------START--------------
        bool boolWriteToLCD = true;
        string strDisplayNameContains = "[TBInfo]";
        string strBlockName = "";
        string strDebug = "";
        IMyTerminalBlock Block = null;
        IMyTerminalBlock DisplayBlock;
        



        StringBuilder Check(IMyTerminalBlock Block)
        {
            var refStrData = new StringBuilder();
            refStrData.AppendLine("=======");
            refStrData.AppendLine("Block CustomName: " + Block.CustomName);
            //refStrData.AppendLine("Block DisplayNameText: "+Block.DisplayNameText); 
/*
            List<ITerminalProperty> propertyList = new List<ITerminalProperty>();
            Block.GetProperties(propertyList);
            if (propertyList.Count > 0)
            {
                refStrData.AppendLine("Properties Found:" + (propertyList.Count));
                for (int i = 0; i < propertyList.Count; i++)
                {
                    // get a list of "properties" by looking up ITerminalProperty 
                    refStrData.AppendLine(i + ". ID: " + propertyList[i].Id);
                    refStrData.AppendLine("   TypeName: " + propertyList[i].TypeName);
                }
            }
            else
                refStrData.AppendLine("\nNo Properties Found");
            refStrData.AppendLine("");
*/
/*
            List<ITerminalAction> actionList = new List<ITerminalAction>();
            Block.GetActions(actionList);
            if (actionList.Count > 0)
            {
                refStrData.AppendLine("Actions Found:" + actionList.Count);
                for (int i = 0; i < actionList.Count; i++)
                {
                    refStrData.AppendLine(i + ". Name: " + actionList[i].Name);
                    refStrData.AppendLine("   ID: " + actionList[i].Id);
                    //refStrData.AppendLine("Icon: "+actionList[i].Icon); 
                }
            }
            else
                refStrData.AppendLine("\nNo Actions Found");
            refStrData.AppendLine("");
*/
            //refStrData.AppendLine("Block GetType:\n" + Block.GetType().ToString() + "\n");
            //refStrData.AppendLine("Block Definition:\n" + Block.BlockDefinition + "\n"); //IMyCubeBlock 
            //refStrData.AppendLine("Block DetailedInfo:\n" + Block.DetailedInfo + "\n"); //IMyTerminalBlock 
            refStrData.AppendLine("Block OwnerId: " + Block.OwnerId);
            refStrData.AppendLine("Block CubeGrid: " + Block.CubeGrid);
            refStrData.AppendLine("[X: " + Block.Position.X + ", Y: " + Block.Position.X + ", Y: " + Block.Position.Z + "]");
            

            /*
                        List<String> lGrid = new List<String>();
                        List<IMyTerminalBlock> lBlocks = new List<IMyTerminalBlock>();
                        GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(lBlocks);
                        if (lBlocks.Count > 0)
                        {
                            for (int i = 0; i < lBlocks.Count; i++)
                            {
                                if (!lGrid.Contains(lBlocks[i].CubeGrid.ToString()))
                                    lGrid.Add(lBlocks[i].CubeGrid.ToString());
                            }
                            if (lGrid.Count > 0)
                            {
                                refStrData.AppendLine("Connected Grids:");
                                for (int i = 0; i < lGrid.Count; i++)
                                {
                                    refStrData.AppendLine(lGrid[i]);
                                }
                            }
                        }
            */
            //========================================================================// 
            // Take information and display it to detailed info via echo and display if toggled on 
            //========================================================================//
            return refStrData;
        }
        //============================================================================// 
        // End Main 
        //============================================================================// 
        //============================================================================// 
        // Get DisplayBlock 
        //============================================================================// 
        void GetDisplayBlock()
        {
            var DisplayBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(strDisplayNameContains, DisplayBlocks);
            if (DisplayBlocks.Count == 1)
            {
                DisplayBlock = DisplayBlocks[0];
                //Echo("Using: " + DisplayBlock.CustomName);
            }
            else
            {
                //Echo("No Display Block found with \"" + strDisplayNameContains + "\" in it's name or multiple matches were found.");
            }
        }




        public void Main(string argument)
        {

            //Get Block and position 
            if (boolWriteToLCD)
            {
                if (DisplayBlock == null)
                    GetDisplayBlock();
            }
            var rData = new StringBuilder();
            
            var blocks = new List<IMyTerminalBlock>();  //Create empty list for all blocks 
            GridTerminalSystem.GetBlocks(blocks);  //Populate list of blocks
            
            rData.AppendLine("Count:" + blocks.Count.ToString() + "\n");
            for (int a = 0; a < blocks.Count - 1; a++)  //Add block positions to display output
            {
                
                if (blocks[a].OwnerId == 0)
                {
                    rData.AppendStringBuilder(Check(blocks[a]));
                }
            }
            Echo(rData.ToString().Trim());
            if (boolWriteToLCD)
            {
                if (DisplayBlock != null)
                {
                    ((IMyTextPanel)DisplayBlock).WritePublicText(rData.ToString().Trim());
                    ((IMyTextPanel)DisplayBlock).ShowTextureOnScreen();
                    ((IMyTextPanel)DisplayBlock).ShowPublicTextOnScreen();
                }
            }
        }
        //------------END--------------
    }
}