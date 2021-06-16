using Digi;
using Digi2.AeroWings;
using Sandbox.ModAPI;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Scripts.Specials.Blocks.Reactions
{
    public class MoveIndicator
    {
        public MechanicsAxis Mechanics;
        public double Threshold;
    }

    public abstract class KeysReactions
	{
		public List<VRage.Input.MyKeys> keys;
		public List<MoveIndicator> moveIndicators;
		public abstract void React();

		public bool CanReact(List<VRage.Input.MyKeys> pressedkeys, Dictionary<MechanicsAxis, MoveIndicator> indicators)
		{
            //Log.ChatError ("CanReact:" + pressedkeys.Print() + " " + indicators.Print());

			foreach (var x in keys)
			{
				if (!pressedkeys.Contains(x))
				{
					return false;
				}
			}

            foreach (var x in moveIndicators)
            {
                MoveIndicator indicator;
                if (!indicators.TryGetValue (x.Mechanics, out indicator))
                {
                    return false;
                }

                if (indicator.Threshold <= x.Threshold)
                {
                    return false;
                }
            }

            return true;
		}
	}

	public class ThrusterReactions : KeysReactions
	{
		public float power;
		public Sandbox.ModAPI.IMyThrust thrust;
		public override void React()
		{
			thrust.ThrustOverride = thrust.MaxThrust * power;
		}
	}


    public class RotorReactions : KeysReactions
    {
        public float velocity;
        public IMyMotorStator rotor;
        public override void React()
        {
            if (velocity == 0)
            {
                rotor.TargetVelocityRPM = velocity;
                rotor.RotorLock = true;
            } else
            {
                rotor.RotorLock = false;
                rotor.TargetVelocityRPM = velocity;
            }
        }
    }

    public class PistonReactions : KeysReactions
    {
        public float velocity;
        public IMyPistonBase rotor;
        public override void React()
        {
            if (velocity == 0)
            {
                rotor.Velocity = velocity;
            }
            else
            {
                rotor.Velocity = velocity;
            }
        }
    }

    public class Thruster360SetDegree: KeysReactions
	{
		public float degree;
		public Thruster360 thrust;
		public override void React()
		{
			Thruster360.SetTargetDegree (thrust.Entity as IMyTerminalBlock, degree);
		}
	}

	public class EleronSetDegree : KeysReactions
	{
		public float degree;
		public Eleron thrust;
		public override void React()
		{
			Eleron.SetTargetDegree(thrust.Entity as IMyTerminalBlock, degree);
		}
	}

	public class ReactionSet
	{
		List<KeysReactions> reactions = new List<KeysReactions>();

		public void React(List<VRage.Input.MyKeys> pressedkeys, Dictionary<MechanicsAxis, MoveIndicator> indicators)
		{
			foreach (var r in reactions)
			{
				if (r.CanReact(pressedkeys, indicators))
				{
					r.React();
					return;
				}
			}
		}
        
        public static string REGEX = "([\\w\\+:\\.]+)\\=([^:, ]+)(?::([\\d-]+))?";
        public static string REGEX2 = ":[^(+:)]*";

        public static string Set (string s, string setting, string value)
        {
            string group = null;
            foreach (Match match in Regex.Matches(s, REGEX))
            {
                var input = match.Groups[1].Value;
                input = Regex.Replace (input, REGEX2, "");
                if (input == setting)
                {
                    group = match.Value;
                    break;
                }
            }

            if (group != null)
            {
                return s.Replace(group, value);
            } 
            else
            {
                if (setting == "NONE")
                {
                    return s + (s.Length != 0 ? " " : "") + value;
                } 
                else
                {
                    return value + (s.Length != 0 ? " " : "") + s;
                }
                
            }
        }

        public static float? GetThreshold(string s, string setting)
        {
            foreach (Match match in Regex.Matches(s, REGEX))
            {
                try
                {
                    var input = match.Groups[1].Value.Split(new string[] { "+" }, StringSplitOptions.None);
                    if (input.Length != 1) continue;

                    var parts = input[0].Split(':');
                    if (parts.Length != 2) continue;
                    if (parts[0] != setting) continue;

                    float threshold;
                    if (!float.TryParse(parts[1], out threshold)) continue;

                    return threshold;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
            return null;
        }

        public static float? GetValue(string s, string setting)
        {
            foreach (Match match in Regex.Matches(s, REGEX))
            {
                try
                {
                    var input = match.Groups[1].Value;
                    input = Regex.Replace(input, REGEX2, "");

                    if (input != setting) continue;

                    float value;
                    if (!float.TryParse(match.Groups[2].Value, out value)) continue;

                    return value;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
            return null;
        }



        public static ReactionSet Parse(string s, Func<string, string, List<VRage.Input.MyKeys>, List<MoveIndicator>, KeysReactions> generator)
		{
            if (MyAPIGateway.Session.isTorchServer()) return null;

            if (s != null)
			{
				var reactions = new List<KeysReactions>();
				foreach (Match match in Regex.Matches(s, REGEX))
				{
					try
					{
						var input = match.Groups[1].Value.Split(new string[] { "+" }, StringSplitOptions.None);
						var action = match.Groups[2].Value;
						var actionInfo = match.Groups[3].Value;

						var keys = new List<VRage.Input.MyKeys>();
						var moveIndicators = new List<MoveIndicator>();
						foreach (var k in input)
						{
							if (k != "NONE")
							{
								var kk = ParseKey(k);
                                if (kk.HasValue)
                                {
                                    keys.Add(kk.Value);
                                } 
                                else
                                {
                                    var shipInput = ParseShipInput (k);
                                    if (shipInput != null)
                                    {
                                        moveIndicators.Add (shipInput);
                                    }
                                }
							}
						}

                        var g = generator (action, actionInfo, keys, moveIndicators);
                        if (g != null)
                        {
                            reactions.Add(g);
                        }
                    }
					catch (Exception e)
					{
						reactions.Clear();
						return null;
					}
				}

				if (reactions.Count > 0)
				{
					return new ReactionSet() { reactions = reactions };
				}
			}

			return null;
		}

        class ParsedReaction {
            public string Action;
            public string ActionInfo;
            public List<VRage.Input.MyKeys> keys;
            public List<MoveIndicator> moveIndicators;
        }


		public static VRage.Input.MyKeys? ParseKey(string s)
		{
			switch (s.ToUpper())
			{
				case "E": return VRage.Input.MyKeys.E;
				case "Q": return VRage.Input.MyKeys.Q;
				case "W": return VRage.Input.MyKeys.W;
				case "A": return VRage.Input.MyKeys.A;
				case "S": return VRage.Input.MyKeys.S;
				case "D": return VRage.Input.MyKeys.D;
				case "R": return VRage.Input.MyKeys.R;

                case "NUM1": return VRage.Input.MyKeys.NumPad1;
                case "NUM2": return VRage.Input.MyKeys.NumPad2;
                case "NUM3": return VRage.Input.MyKeys.NumPad3;
                case "NUM4": return VRage.Input.MyKeys.NumPad4;
                case "NUM5": return VRage.Input.MyKeys.NumPad5;
                case "NUM6": return VRage.Input.MyKeys.NumPad6;
                case "NUM7": return VRage.Input.MyKeys.NumPad7;
                case "NUM8": return VRage.Input.MyKeys.NumPad8;
                case "NUM9": return VRage.Input.MyKeys.NumPad9;

                case "X": return VRage.Input.MyKeys.X;
				case "C": return VRage.Input.MyKeys.C;
				case "Z": return VRage.Input.MyKeys.Z;


				case "SPACE": return VRage.Input.MyKeys.Space;
				case "SHIFT": return VRage.Input.MyKeys.Shift;
				case "CAPSLOCK": return VRage.Input.MyKeys.CapsLock;
				case "CONTROL": return VRage.Input.MyKeys.Control;
				default:
					return null;
			}
		}

        

        public static MoveIndicator ParseShipInput(string s)
        {
            var parts = s.Split (':');
            if (parts.Length != 2) return null;

            float threshold;
            if (!float.TryParse(parts[1], out threshold)) return null;

            switch (parts[0].ToUpper())
            {
                case "YAW": return new MoveIndicator() { Mechanics = MechanicsAxis.Yaw, Threshold = threshold };
                case "IYAW": return new MoveIndicator() { Mechanics = MechanicsAxis.InvertedYaw, Threshold = threshold };
                case "PITCH": return new MoveIndicator() { Mechanics = MechanicsAxis.Pitch, Threshold = threshold };
                case "IPITCH": return new MoveIndicator() { Mechanics = MechanicsAxis.InvertedPitch, Threshold = threshold };
                case "ROLL": return new MoveIndicator() { Mechanics = MechanicsAxis.Roll, Threshold = threshold };
                case "IROLL": return new MoveIndicator() { Mechanics = MechanicsAxis.InvertedRoll, Threshold = threshold };
                default:
                    return null;
            }
        }
    }
}
