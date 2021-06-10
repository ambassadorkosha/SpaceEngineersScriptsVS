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

namespace BlargmodesAscentCruiseControl
{
    public sealed class Program : MyGridProgram
    {
		//------------BEGIN--------------
		/*
		Blargmode's Ascent Cruise Control (version 1.6, 2018-04-15)

		Tired of wasting fuel when leaving a gravity well? What you need is cruise control!

		This script adjusts the thrust of your rear thrusters to the lowest possible without losing speed.


		___/ Setup: \\__________

		1. Install script.
		2. Sit in flight seat and add the script to the toolbar: 'Run' and leave argument empty.

		The script looks for an occupied flight seat every 10 seconds until it finds one, to determine what is forward. 
		Once one is found, it stops looking and saves the seat internally. If the Wrong seat is stored, you can hop into
		the correct one, access the Programmable block, type 'reset' as the argument, and press run.


		___/ Usage: \\__________
		Press the button you set up in step two to turn the cruise conroll on or off.


		___/ Optional extras: \\__________

		The script can show status on LCDs, both regular and corner.
		It can also show if it's engaged or not using a light.

		In either case, just add the tag #ACC to the name of the light/LCD.

		You can have more than one light/LCD. 

		Cruise Control can also be engaged via a button, a sensor, or any other action. 



		___/ Settings: \\__________

		Are found in Custom Data.
		After changing one, press run with an empty argument field, or start the cruise control.

		The target speed can also be changed via argument. Type a number as the argument and press run.
		Or add it to your toolbar with 'Run' and a number as the argument.



		___/ Controlled descent: \\__________

		Note, this is an unintended feature I found handy, so I kept it.

		You can set the Target speed to a negative value. If you do, you have a controlled descent, backwards.
		Be sure to take manual control when you get close to landing and be careful aligning the ship properly.


		Warning: The following is an experimental feature. It can be dangerous to use with weak thrust, or if you
		point the ship in the wrong direction.

		As you're descending, the script will scale down your target speed in relation to your altitude. In theory 
		you could land like that. But it's dangerous to do. It's intended to help slowing down when descending, not
		to be full auto. Note that your altitude is measured at the center of mass, so the landing
		might not be gentle.
		And again, if your ship is too weak, or you've angled it wrong, or the terrain is weird, this could be your death.
		In my testing target speeds exceeding -95m/s have been fatal.





















		*/

		public class Profiler { Program P; double[] Time; int Size; public int Index; public double Result = 0; public Profiler(Program p, int size) { P = p; Size = size; Time = new double[size]; } public void Log() { if (Index <= Size - 1) { Time[Index] = P.Runtime.LastRunTimeMs; if (Index == Size - 1) { Result = Time.Average(); P.Me.CustomData = string.Join("\n", Time.Select(x => x.ToString()).ToArray()); } Index++; } } }
		public class FixedWidthText { private List<string> Text; public int Width { get; private set; } public FixedWidthText(int width) { Text = new List<string>(); Width = width; } public void Clear() { Text.Clear(); } public void Append(string t) { Text[Text.Count - 1] += t; } public void AppendLine() { Text.Add(""); } public void AppendLine(string t) { Text.Add(t); } public void Combine(List<string> input) { Text.AddRange(input); } public List<string> GetRaw() { return Text; } public override string ToString() { return GetText(Width); } public string GetText() { return GetText(Width); } public string GetText(int lineWidth) { string finalText = ""; foreach (var line in Text) { string rest = line; if (rest.Length > lineWidth) { while (rest.Length > lineWidth) { string part = rest.Substring(0, lineWidth); rest = rest.Substring(lineWidth); for (int i = part.Length - 1; i > 0; i--) { if (part[i] == ' ') { finalText += part.Substring(0, i) + "\n"; rest = part.Substring(i + 1) + rest; break; } } } } finalText += rest + "\n"; } return finalText; } }
		public string ScriptName = "Blarg's Ascent Cruise Control"; IMyShipController Controller; List<IMyThrust> Forwards = new List<IMyThrust>(); List<IMyThrust> Backwards = new List<IMyThrust>(); List<IMyThrust> Upwards = new List<IMyThrust>(); List<IMyThrust> Downwards = new List<IMyThrust>(); List<IMyLightingBlock> Lights = new List<IMyLightingBlock>(); List<IMyTextPanel> Panels = new List<IMyTextPanel>(); List<IMyTimerBlock> Timers = new List<IMyTimerBlock>(); bool SettingsInitialized = false; Dictionary<ID, Setting> Settings = new Dictionary<ID, Setting>(); private FixedWidthText SettingsProblems = new FixedWidthText(40); bool ShowSettingsProblems = false; TimeSpan Time = TimeSpan.Zero; FixedWidthText Debug = new FixedWidthText(40); FixedWidthText DetailedInfo = new FixedWidthText(40); FixedWidthText CustomData = new FixedWidthText(70); TimeSpan NextSetupTry = TimeSpan.MinValue; double TargetSpeed = 95; double LastSpeed = -1; TimeSpan LastSpeedMeasurement = TimeSpan.MaxValue; float ThrustOverride = 1; float Cutoff = 0.05f; bool Initialized = false; bool Active = false; bool StartedOutsideGravity = false; ThrustDirection SelectedThrusters = ThrustDirection.Auto; IMyTextPanel DEBUGLCD; public enum ID { Tag, TargetSpeed, DisableAtGravTransition, SelectThrusters, ThrustEffectivnessCutoff }; public enum ThrustDirection { Auto, Rear, Bottom }; public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update10; DEBUGLCD = GridTerminalSystem.GetBlockWithName("DEBUG") as IMyTextPanel; }
		public void Save() { }
		public void Main(string argument, UpdateType updateType) { if (Runtime.TimeSinceLastRun.TotalMilliseconds < 1 && updateType != UpdateType.Trigger && updateType != UpdateType.Terminal) return; Time += Runtime.TimeSinceLastRun; if (Controller != null) InGravity(); Loop(argument, updateType); UpdateDetailedInfo(); }
		public void Loop(string argument, UpdateType updateType) { if (!SettingsInitialized) { ShowSettingsProblems = false; Settings = new Dictionary<ID, Setting>(); Settings.Add(ID.Tag, new Setting("Tag", "#ACC")); Settings.Add(ID.TargetSpeed, new Setting("Target Speed", (double)95)); Settings.Add(ID.DisableAtGravTransition, new Setting("Disable when exiting gravity", true)); Settings.Add(ID.SelectThrusters, new Setting("Select thrusters (Auto, Rear, or Bottom)", ThrustDirection.Auto)); Settings.Add(ID.ThrustEffectivnessCutoff, new Setting("Thrust effectivenes cutoff (%)", 5.0)); ParseUserSettings(Me.CustomData); PrintSettingsToCustomData(); SettingsInitialized = true; TargetSpeed = (double)Settings[ID.TargetSpeed].Value; } if (updateType == UpdateType.Terminal) { if (argument.ToLower() == "reset") { ResetController(); } if (SettingsInitialized) { ParseInputNumber(argument); ShowSettingsProblems = false; ParseUserSettings(Me.CustomData); PrintSettingsToCustomData(); TargetSpeed = (double)Settings[ID.TargetSpeed].Value; } InitThrusters(); if (!Active) Panels.ForEach(x => UpdateTextPanel(x)); } if (Controller == null) { ResetDebug(); Debug.AppendLine(">Controller is null"); if (Time > NextSetupTry) { var blocks = new List<IMyTerminalBlock>(); GridTerminalSystem.GetBlocks(blocks); foreach (var block in blocks) { if (block is IMyLightingBlock && LightMeetsConditions(block as IMyLightingBlock)) Lights.Add(block as IMyLightingBlock); else if (block is IMyTextPanel && PanelMeetsConditions(block as IMyTextPanel)) Panels.Add(block as IMyTextPanel); else if (block is IMyTimerBlock && TimerMeetsConditions(block)) Timers.Add(block as IMyTimerBlock); } if (GetController()) { InitThrusters(); if (SelectedThrusters == ThrustDirection.Rear && Forwards.Count == 0) { Debug.AppendLine(">No Forwards thrusters"); return; } if (SelectedThrusters == ThrustDirection.Bottom && Downwards.Count == 0) { Debug.AppendLine(">No Downwards thrusters"); return; } Initialized = true; Panels.ForEach(x => UpdateTextPanel(x)); } else { Debug.AppendLine(">Setup failed, tries again in 10 seconds"); NextSetupTry = Time + TimeSpan.FromSeconds(10); Panels.ForEach(x => UpdateTextPanel(x)); return; } } return; } if (Initialized) { ResetDebug(); Debug.AppendLine(">Ready"); if (updateType == UpdateType.Trigger) { if (ParseInputNumber(argument)) { if (!Active) Panels.ForEach(x => UpdateTextPanel(x)); } else if (argument.ToLower() == "off") { if (Active) DisableCC(); } else if (argument.ToLower() == "on") { if (!Active) EnableCC(); } else if (argument.ToLower() == "swap") { if (Active) { DisableCC(); if (SelectedThrusters == ThrustDirection.Rear) SelectedThrusters = ThrustDirection.Bottom; else SelectedThrusters = ThrustDirection.Rear; EnableCC(); } else { if (SelectedThrusters == ThrustDirection.Rear) SelectedThrusters = ThrustDirection.Bottom; else SelectedThrusters = ThrustDirection.Rear; Panels.ForEach(x => UpdateTextPanel(x)); } Settings[ID.SelectThrusters].Value = SelectedThrusters; PrintSettingsToCustomData(); } else { if (Active) { DisableCC(); } else { EnableCC(); } } } if (Active) { Debug.AppendLine(">Active"); if (SelectedThrusters == ThrustDirection.Bottom) CruiseControl(Downwards); else CruiseControl(Forwards); Panels.ForEach(x => UpdateTextPanel(x)); } } }
		void CruiseControl(List<IMyThrust> thrusters) { var vel = Controller.GetShipVelocities().LinearVelocity; double speed = 0; if (SelectedThrusters == ThrustDirection.Bottom) { speed = (Vector3D.TransformNormal(vel, MatrixD.Transpose(Controller.WorldMatrix))).Y; } else { speed = -(Vector3D.TransformNormal(vel, MatrixD.Transpose(Controller.WorldMatrix))).Z; } Debug.AppendLine($">Vel: {speed.ToString()}"); double acceleration = 0; if (LastSpeed != -1) { acceleration = Math.Round((speed - LastSpeed) / (Time - LastSpeedMeasurement).TotalSeconds, 3); Debug.AppendLine($">Vel: {acceleration}"); double difference = TargetSpeed - speed; double errorMagnitude = Math.Abs(difference / TargetSpeed); if (difference < acceleration) { ThrustOverride -= (float)errorMagnitude; } else { ThrustOverride += (float)errorMagnitude; } if (ThrustOverride < 0) ThrustOverride = 0.00001f; else if (ThrustOverride > 1) ThrustOverride = 1; if (Active) { foreach (var thruster in thrusters) { if (thruster.MaxEffectiveThrust / thruster.MaxThrust <= Cutoff) thruster.ThrustOverridePercentage = 0; else thruster.ThrustOverridePercentage = ThrustOverride; } } DEBUGLCD?.WritePublicText($"Speed {speed}\nDiff {difference}\nAccel {acceleration}\nThrust ov {ThrustOverride}"); } LastSpeed = speed; LastSpeedMeasurement = Time; if ((bool)Settings[ID.DisableAtGravTransition].Value) { if (!InGravity() && !StartedOutsideGravity) { Active = false; DisableCC(); Timers.ForEach(x => x.Trigger()); } else if (TargetSpeed < 0) { double height; if (Controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out height)) { double newTarget = -(height / 10.0); if (TargetSpeed < newTarget) TargetSpeed = newTarget; } } } }
		void InitThrusters() { var thrusters = new List<IMyThrust>(); GridTerminalSystem.GetBlocksOfType(thrusters, x => x.CubeGrid == Me.CubeGrid); Forwards.Clear(); Backwards.Clear(); Upwards.Clear(); Downwards.Clear(); Base6Directions.Direction forward = Controller.Orientation.Forward; Base6Directions.Direction backward = Base6Directions.GetOppositeDirection(forward); Base6Directions.Direction upward = Controller.Orientation.Up; Base6Directions.Direction downward = Base6Directions.GetOppositeDirection(upward); float rearThrust = 0; float bottomThrust = 0; foreach (var item in thrusters) { if (item.Orientation.Forward == forward) { Backwards.Add(item); } else if (item.Orientation.Forward == backward) { Forwards.Add(item); rearThrust += item.MaxThrust; } if (item.Orientation.Forward == upward) { Upwards.Add(item); } else if (item.Orientation.Forward == downward) { Downwards.Add(item); bottomThrust += item.MaxThrust; } } if ((ThrustDirection)Settings[ID.SelectThrusters].Value == ThrustDirection.Auto) { if (rearThrust >= bottomThrust) { SelectedThrusters = ThrustDirection.Rear; } else { SelectedThrusters = ThrustDirection.Bottom; } } else if ((ThrustDirection)Settings[ID.SelectThrusters].Value == ThrustDirection.Rear) { SelectedThrusters = ThrustDirection.Rear; } else if ((ThrustDirection)Settings[ID.SelectThrusters].Value == ThrustDirection.Bottom) { SelectedThrusters = ThrustDirection.Bottom; } }
		void InitThrustersOld() { var thrusters = new List<IMyThrust>(); GridTerminalSystem.GetBlocksOfType(thrusters, x => x.CubeGrid == Me.CubeGrid); Forwards.Clear(); Backwards.Clear(); Base6Directions.Direction forward = Controller.Orientation.Forward; Base6Directions.Direction backward = Base6Directions.GetOppositeDirection(forward); foreach (var item in thrusters) { if (item.Orientation.Forward == forward) Backwards.Add(item); else if (item.Orientation.Forward == backward) Forwards.Add(item);/*if(item.GridThrustDirection.Z==1){Forwards.Add(item);}else if(item.GridThrustDirection.Z==-1){Backwards.Add(item);}*/} }
		bool ParseInputNumber(string text) { double temp = 0; if (double.TryParse(text, out temp)) { Settings[ID.TargetSpeed].Value = temp; TargetSpeed = temp; PrintSettingsToCustomData(); return true; } return false; }
		bool InGravity() { return Controller.GetNaturalGravity().Length() > 0.0; }
		void EnableCC() { ThrustDirection dir = (ThrustDirection)Settings[ID.SelectThrusters].Value; ParseUserSettings(Me.CustomData); PrintSettingsToCustomData(); TargetSpeed = (double)Settings[ID.TargetSpeed].Value; Cutoff = (float)((double)Settings[ID.ThrustEffectivnessCutoff].Value / 100); if ((ThrustDirection)Settings[ID.SelectThrusters].Value != dir) InitThrusters(); Active = true; ThrustOverride = 1; if (SelectedThrusters == ThrustDirection.Bottom) { Upwards.ForEach(x => x.Enabled = false); Downwards.ForEach(x => x.ThrustOverridePercentage = ThrustOverride); } else { Backwards.ForEach(x => x.Enabled = false); Forwards.ForEach(x => x.ThrustOverridePercentage = ThrustOverride); } Lights.ForEach(x => x.Enabled = true); if (InGravity()) StartedOutsideGravity = false; else StartedOutsideGravity = true; }
		void DisableCC() { TargetSpeed = (double)Settings[ID.TargetSpeed].Value; Active = false; if (SelectedThrusters == ThrustDirection.Bottom) { Upwards.ForEach(x => x.Enabled = true); Downwards.ForEach(x => x.ThrustOverridePercentage = 0); } else { Backwards.ForEach(x => x.Enabled = true); Forwards.ForEach(x => x.ThrustOverridePercentage = 0); } Lights.ForEach(x => x.Enabled = false); Panels.ForEach(x => UpdateTextPanel(x)); }
		void UpdateDetailedInfo() { DetailedInfo.Clear(); DetailedInfo.AppendLine(ScriptName); DetailedInfo.AppendLine(); if (ShowSettingsProblems) { DetailedInfo.Combine(SettingsProblems.GetRaw()); } if (Active) { DetailedInfo.AppendLine("Status: Engaged"); DetailedInfo.AppendLine($"Target speed: {Math.Round(TargetSpeed, 1)}m/s"); DetailedInfo.AppendLine($"Current speed: {Math.Round(LastSpeed, 1).ToString("n1")}m/s"); DetailedInfo.AppendLine($"Thrust override: {(ThrustOverride * 100).ToString("n1")}%"); DetailedInfo.AppendLine(); DetailedInfo.AppendLine("Press toolbar button again to disengage."); DetailedInfo.AppendLine(); if ((bool)Settings[ID.DisableAtGravTransition].Value) DetailedInfo.AppendLine("Will automatically disengage."); else DetailedInfo.AppendLine("Manual disengage only."); DetailedInfo.AppendLine($"Using {SelectedThrusters.ToString()} thrusters."); } else if (!Initialized) { DetailedInfo.AppendLine("Status: NOT Ready"); DetailedInfo.AppendLine(); DetailedInfo.AppendLine("Sit in flight seat for up to 10 seconds to finish setup."); DetailedInfo.AppendLine("(To determine what's forward)."); } else { DetailedInfo.AppendLine("Status: ready"); DetailedInfo.AppendLine($"Target speed: {Math.Round(TargetSpeed, 1)}m/s"); DetailedInfo.AppendLine(); DetailedInfo.AppendLine("Click 'Custom Data' for settings."); DetailedInfo.AppendLine(); if ((bool)Settings[ID.DisableAtGravTransition].Value) DetailedInfo.AppendLine("Will automatically disengage."); else DetailedInfo.AppendLine("Manual disengage only."); DetailedInfo.AppendLine($"Using {SelectedThrusters.ToString()} thrusters."); DetailedInfo.AppendLine($"Connected Lights: {Lights.Count}"); DetailedInfo.AppendLine($"Connected LCDs: {Panels.Count}"); } Echo(DetailedInfo.ToString()); }
		void UpdateTextPanel(IMyTextPanel panel) { bool isCorner = panel.BlockDefinition.SubtypeId.ToString().ToLower().Contains("corner"); if (isCorner) { UpdateCornerTextPanel(panel); } else { if (panel.FontSize > 1.7f) { panel.FontSize = 1.7f; } var text = new StringBuilder(); string padding = ""; bool centered = panel.GetValue<long>("alignment") == 2; if (panel.GetValue<long>("alignment") == 0) { padding = " "; } if (Active) { if (centered) text.AppendLine(""); text.AppendLine($"{padding}Ascent Cruise Control"); text.AppendLine($"{padding}Status: Engaged"); text.AppendLine(); text.AppendLine($"{padding}Target speed: {Math.Round(TargetSpeed, 1)}m/s"); text.AppendLine($"{padding}Current speed: {Math.Round(LastSpeed, 1).ToString("n1")}m/s"); text.AppendLine($"{padding}Thrust override: {(ThrustOverride * 100).ToString("n1")}%"); text.AppendLine($"{padding}Using {SelectedThrusters.ToString().ToLower()} thrusters"); text.AppendLine(); if ((bool)Settings[ID.DisableAtGravTransition].Value) text.AppendLine($"{padding}Will auto-disengage"); panel.WritePublicText(text.ToString()); } else if (!Initialized) { if (centered) text.AppendLine(""); text.AppendLine($"{padding}Ascent Cruise Control"); text.AppendLine($"{padding}Status: NOT Ready"); text.AppendLine(""); text.AppendLine($"{padding}Sit in flight seat"); text.AppendLine($"{padding}for up to 10 seconds"); text.AppendLine($"{padding}to finish setup."); panel.WritePublicText(text.ToString()); } else { if (centered) text.AppendLine(""); text.AppendLine($"{padding}Ascent Cruise Control"); text.AppendLine($"{padding}Status: Ready"); text.AppendLine($"{padding}Target speed: {Math.Round(TargetSpeed, 1)}m/s"); text.AppendLine($"{padding}Using {SelectedThrusters.ToString().ToLower()} thrusters"); text.AppendLine(""); text.AppendLine($"{padding}Run script via"); text.AppendLine($"{padding}toolbar or button"); text.AppendLine($"{padding}to activate."); panel.WritePublicText(text.ToString()); } } }
		void UpdateCornerTextPanel(IMyTextPanel panel) { string newline = ""; if (panel.FontSize < 1) { newline = "\n\n"; } else if (panel.FontSize < 1.5) { newline = "\n"; } string direction = ">"; if (SelectedThrusters == ThrustDirection.Bottom) direction = "^"; if (Active) panel.WritePublicText($"{newline}{Math.Round(LastSpeed, 1).ToString("n1")}m/s [{Math.Round(TargetSpeed, 1)}]{direction} - {(ThrustOverride * 100).ToString("n1")}%"); else if (!Initialized) panel.WritePublicText($"{newline}Cruise Control NOT Ready"); else panel.WritePublicText($"{newline}Cruise Control Ready [{TargetSpeed}]{direction}"); }
		bool GetController() { Debug.AppendLine($">Storage contains '{Storage}'"); long id = 0; var ctrls = new List<IMyShipController>(); if (long.TryParse(Storage, out id)) { Debug.AppendLine($">Parsed storage fine '{id}'"); GridTerminalSystem.GetBlocksOfType(ctrls, x => x.EntityId == id); if (ctrls.Count == 1) { Debug.AppendLine($">Using '{ctrls[0].CustomName}' for fidning grid forward."); Controller = ctrls[0]; Storage = Controller.EntityId.ToString(); return true; } else { Debug.AppendLine(">Couldn't find controller with ID: " + id); } } Debug.AppendLine(">Couldn't get ID from storage"); ctrls.Clear(); GridTerminalSystem.GetBlocksOfType(ctrls, x => ControllerMeetsConditions(x)); if (ctrls.Count > 0) { Debug.AppendLine($">Using '{ctrls[0].CustomName}' for fidning grid forward."); Controller = ctrls[0]; Storage = Controller.EntityId.ToString(); return true; } else { Debug.AppendLine(">No occupied controller"); } return false; }
		bool ControllerMeetsConditions(IMyShipController ctrl) { if (ctrl is IMyCryoChamber) return false; if (ctrl.CubeGrid != Me.CubeGrid) return false; if (!ctrl.IsUnderControl) return false; return true; }
		bool LightMeetsConditions(IMyLightingBlock light) { if (!light.CustomName.Contains((string)Settings[ID.Tag].Value)) return false; light.Enabled = false; return true; }
		bool PanelMeetsConditions(IMyTextPanel panel) { if (!panel.CustomName.Contains((string)Settings[ID.Tag].Value)) return false; panel.ShowPublicTextOnScreen(); return true; }
		bool TimerMeetsConditions(IMyTerminalBlock timer) { if (!timer.CustomName.Contains((string)Settings[ID.Tag].Value)) return false; return true; }
		void ResetDebug() { Debug.Clear(); }
		void ResetController() { Storage = ""; Controller = null; Initialized = false; Active = false; }
		private void ParseUserSettings(string text) { SettingsProblems.Clear(); SettingsProblems.AppendLine("Problem with settings:"); if (text.Length > 0) { var lines = text.Split('\n'); bool inSettings = true; for (int i = 0; i < lines.Length; i++) { if (!inSettings) { if (lines[i].Contains(ScriptName + " Settings")) inSettings = true; } else { if (lines[i].StartsWith(" \t ")) inSettings = false; else { var keys = new List<ID>(Settings.Keys); foreach (var key in keys) { if (lines[i].StartsWith(Settings[key].Text)) { var parts = lines[i].Split(new char[] { ':' }, 2); if (!string.IsNullOrEmpty(parts[1])) { var val = parts[1].Trim(); if (Settings[key].Value is bool) { if (val.ToLower() == "yes") { Settings[key].Value = true; } else if (val.ToLower() == "no") { Settings[key].Value = false; } else { SettingsProblemIllegible(key, "Has to be yes or no"); } } else if (Settings[key].Value is int) { int temp = 0; if (int.TryParse(val, out temp)) { Settings[key].Value = temp; } else { SettingsProblemIllegible(key, "Has to be a whole number"); } } else if (Settings[key].Value is double) { double temp = 0; if (double.TryParse(val, out temp)) { Settings[key].Value = temp; } else { SettingsProblemIllegible(key, "Has to be a number"); } } else if (Settings[key].Value is string) { if (!string.IsNullOrEmpty(val) && !string.IsNullOrWhiteSpace(val)) { Settings[key].Value = val; } else { SettingsProblemIllegible(key, "Has to be text"); } } else if (Settings[key].Value is ThrustDirection) { ThrustDirection td; if (Enum.TryParse<ThrustDirection>(val, true, out td)) { Settings[key].Value = td; } else { string temp = string.Join(", ", Enum.GetNames(typeof(ThrustDirection))); SettingsProblemIllegible(key, $"Must be one of these: {temp}."); } } } else { SettingsProblemIllegible(key); } } } } } } } SettingsProblems.AppendLine("______________________________"); SettingsProblems.AppendLine(); }
		private void SettingsProblemIllegible(ID key, string additionalInfo = "") { ShowSettingsProblems = true; SettingsProblems.AppendLine(); SettingsProblems.AppendLine("Did not understand setting. Using default or previous value."); SettingsProblems.AppendLine("> " + Settings[key].Text); if (additionalInfo != "") SettingsProblems.AppendLine("> " + additionalInfo); }
		private void PrintSettingsToCustomData() { if (CustomData == null) CustomData = new FixedWidthText(70); CustomData.Clear(); CustomData.AppendLine(ScriptName + " Settings"); CustomData.AppendLine("----------------------------------------------------------------------"); CustomData.AppendLine("To change settings: Edit the value after the colon, then press run with no argument. Or engage cruise control."); CustomData.AppendLine("----------------------------------------------------------------------"); CustomData.AppendLine(); foreach (var setting in Settings.Values) { for (int i = 0; i < setting.SpaceAbove; i++) { CustomData.AppendLine(); } CustomData.AppendLine(setting.Text + ": " + SettingToString(setting.Value)); } CustomData.AppendLine(" \t "); CustomData.AppendLine(); CustomData.AppendLine(); CustomData.AppendLine(); CustomData.AppendLine("More info:"); CustomData.AppendLine("----------------------------------------------------------------------"); CustomData.AppendLine("The script can show status using lights and LCDs."); CustomData.AppendLine("To set them up, add the tag above to the names of the LCDs/lights."); CustomData.AppendLine("Supports both regular and corner LCDs."); CustomData.AppendLine("After adding tags, press run with no argument to update"); CustomData.AppendLine(); CustomData.AppendLine("----------------------------------------------------------------------"); CustomData.AppendLine("If the script has gotten what's forward wrong:"); CustomData.AppendLine("Sit in a cockpit/flight seat facing the desired forward direction, make sure only one seat is in use, and send the reset command."); CustomData.AppendLine("(The reset command is sent by typing 'reset' as the argument and pressing run)."); CustomData.AppendLine(); CustomData.AppendLine("----------------------------------------------------------------------"); CustomData.AppendLine("Arguments you can run from the toolbar/button/sensor/etc.."); CustomData.AppendLine("(All of which can be used even while cruise control is engaged)."); CustomData.AppendLine("(Minus the brackets)."); CustomData.AppendLine("  [] (empty) Toggles cruise control on or off."); CustomData.AppendLine("  [on] Toggles cruise control on."); CustomData.AppendLine("  [off] Toggles cruise control off."); CustomData.AppendLine("  [95] (any number, including negative) Sets a new target speed."); CustomData.AppendLine("  [swap] Switches between rear and bottom thrusters."); Me.CustomData = CustomData.GetText(); }
		private string SettingToString(object input) { if (input is bool) { return (bool)input ? "yes" : "no"; } if (input is Color) { var color = (Color)input; return "R:" + color.R + ", G:" + color.G + ", B:" + color.B; } return input.ToString(); }
		class Setting { public string Text; private object _value; public object Value { get { return _value; } set { _value = value; } } public int SpaceAbove; public Setting(string text, object value, int spaceAbove = 0) { Text = text; Value = value; SpaceAbove = spaceAbove; } }
		//------------END--------------
	}
}