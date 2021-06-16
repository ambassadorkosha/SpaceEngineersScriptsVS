using System;
using System.Collections.Generic;
using Digi;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Specials.Production;
using ServerMod;
using SpaceEngineers.Game.Definitions.SafeZone;
using VRage.Utils;

namespace Scripts.Specials {
    public class CustomControl {
        public static void Init() {
            if (!MyAPIGateway.Session.IsServer || !MyAPIGateway.Utilities.IsDedicated) {
                MyAPIGateway.TerminalControls.CustomControlGetter += TerminalControls_CustomControlGetter;
            }
        } 
        
        private static void TerminalControls_CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            var name = block.BlockDefinition.SubtypeName;
            if (name == null) return;
            if (AdjustableProductionMod.IDs.Contains(name)) {
                controls.FindAndMove(0, (x)=>x.Id == "AdjustableRef_Yield");
                controls.FindAndMove(0, (x)=>x.Id == "AdjustableRef_Power");
                controls.FindAndMove(0, (x)=>x.Id == "AdjustableRef_Speed");
            } else if (name.Contains("Hologram")) { //x.Id == "OnOff" || 
                controls.RemoveAll (x=>(x.Id == "ShowInTerminal" || x.Id == "ShowOnHUD" || x.Id == "ShowInToolbarConfig" || x.Id == "Name" || x.Id == "ShowOnHud"));
            } else if (name.Contains("JumpDrive")) {
                controls.FindAndMove(0, (x)=>x.Id == "Recharge");
                controls.FindAndMove(1, (x)=>x.Id == "GpsList");
                controls.FindAndMove(2, (x)=>x.Id == "SelectBtn");
                controls.FindAndMove(3, (x)=>x.Id == "RemoveBtn");
                controls.FindAndMove(4, (x)=>x.Id == "SelectedTarget");
                controls.FindAndMove(5, (x)=>x.Id == "JumpDistance");
            } else if (name.Contains ("Zone")) {
                try {
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneColor");
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneTextureCombo");
                    
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneConvertToStationCb");
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneLandingGearCb");
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneVoxelHandCb");
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneDamageCb");
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneShootingCb");
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneDrillingCb");
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneWeldingCb");
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneGrindingCb");
                    controls.FindAndMove(0, (x)=>x.Id == "SafeZoneBuildingCb");
                    
                    var label = controls.FindAndMove(0,(x)=>x.Id == "Label") as IMyTerminalControlLabel;
                    label.Label = MyStringId.GetOrCompute("ON ALL / DONT BUILD OUTSIDE");
                    
                    controls.FindAndMove(0,(x)=>x.Id == "SafeZoneZSlider");
                    controls.FindAndMove(0,(x)=>x.Id == "SafeZoneYSlider");
                    controls.FindAndMove(0,(x)=>x.Id == "SafeZoneXSlider");
                    controls.FindAndMove(0,(x)=>x.Id == "SafeZoneSlider");
                    controls.FindAndMove(0,(x)=>x.Id == "SafeZoneShapeCombo");
                    
                    controls.FindAndMove(0,(x)=>x.Id == "SafeZoneCreate");
                    controls.FindAndMove(0,(x)=>x.Id == "OnOff");

                    var slider1 = controls.Find((x)=>x.Id == "SafeZoneXSlider") as IMyTerminalControlSlider;
                    var slider2 = controls.Find((x)=>x.Id == "SafeZoneYSlider") as IMyTerminalControlSlider;
                    var slider3 = controls.Find((x)=>x.Id == "SafeZoneZSlider") as IMyTerminalControlSlider;

                   
                
					Func<IMyTerminalBlock, float> min = (x)=>(x.SlimBlock.BlockDefinition as MySafeZoneBlockDefinition).MinSafeZoneRadius*2;
					Func<IMyTerminalBlock, float> max = (x)=>(x.SlimBlock.BlockDefinition as MySafeZoneBlockDefinition).MaxSafeZoneRadius*2;

					slider1.SetLogLimits (min, max);
                    slider2.SetLogLimits (min, max);
                    slider3.SetLogLimits (min, max);

                    
                    if (slider1.Getter (block) > max(block)) {
                        slider1.Setter.Invoke (block, max(block));
                    }

                    if (slider2.Getter (block) > max(block)) {
                        slider2.Setter.Invoke (block, max(block));
                    }

                    if (slider3.Getter (block) > max(block)) {
                        slider3.Setter.Invoke (block, max(block));
                    }
                    
                } catch (Exception e) {
                    Log.Error (e);
                }
            }
        }                          
    }
}