using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using ServerMod;
using Slime;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Input;
using VRageMath;

namespace Scripts

{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), true)]
	public class CockpitUserInput : MyGameLogicComponent
	{
		private static bool inited = false;
		public static ushort COCKPIT_INPUT_PORT = 24000;
		public static ushort COCKPIT_ROTATION_PORT = 24001;
		public static float MOUSE_ROTATION_INDICATOR_MULTIPLIER = 0.075f;
		public static float ROTATION_INDICATOR_MULTIPLIER = 0.15f;  // empirical value for nice keyboard rotation: mouse/joystick/gamepad sensitivity can be tweaked by the user

		public uint ClientPacketID = 0;		// Current on client
		public uint ServerPacketID = 0;     // Last received on server

		IMyCockpit Controller;
		// Server side data
		protected List<MyKeys> ServerKeyboardInputState = new List<MyKeys>();	// keys pressed on client sitting in the cockpit
		protected string ServerKeyboardInputString = "";						// String-formatted data (named as enum MyKeys) for keys listed in the List<MyKeys> above
		protected Vector2 ServerRotationIndicator = Vector2.Zero;				// Horizontal and vertical mouse input indicator (as it comes to the cockpit)
		protected int ServerMouseWheelAbsoluteRotation = 0;						// Absolute mouse wheel valuse that comes from the client

		// Client side data
		protected List<MyKeys> ClientKeyboardInputStatePrev = new List<MyKeys>();
		protected List<MyKeys> ClientKeyboardInputStateCurrent = new List<MyKeys>();
		bool WasFunctional = false;
		int MouseWheelCumulativeData = 0;
		Vector2 ClientAbsoluteRotationIndicator = Vector2.Zero;
		IMyCharacter PreviousePlayer = null;

		public static void Init()
		{
			if (MyAPIGateway.Session.IsServer)
			{
				MyAPIGateway.Multiplayer.RegisterMessageHandler(COCKPIT_INPUT_PORT, ServerSetPressedKeyboardKeysFromPacket);
				MyAPIGateway.Multiplayer.RegisterMessageHandler(COCKPIT_ROTATION_PORT, ServerSetRotationFromPacket);
			}
		}

		public static void Close()
		{
			if (MyAPIGateway.Session.IsServer)
			{
				MyAPIGateway.Multiplayer.UnregisterMessageHandler(COCKPIT_INPUT_PORT, ServerSetPressedKeyboardKeysFromPacket);
				MyAPIGateway.Multiplayer.UnregisterMessageHandler(COCKPIT_ROTATION_PORT, ServerSetRotationFromPacket);
			}
		}
		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			Controller = Entity as IMyCockpit;
			if (!inited)
			{
				inited = true;				
				InitControls();
			}
			NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
		}

		private static void InitControls()
		{
			var propertyDistanceToSurface = MyAPIGateway.TerminalControls.CreateProperty<string, IMyCockpit>("KeyboardInput");
			propertyDistanceToSurface.SupportsMultipleBlocks = false;
			propertyDistanceToSurface.Getter = (block) => { return block.GetAs<CockpitUserInput>().ServerKeyboardInputString; };
			MyAPIGateway.TerminalControls.AddControl<IMyCockpit>(propertyDistanceToSurface);

			var propertyRotIndicatorX = MyAPIGateway.TerminalControls.CreateProperty<float, IMyCockpit>("RotationIndicatorX");
			propertyRotIndicatorX.SupportsMultipleBlocks = false;
			propertyRotIndicatorX.Getter = (block) => { return block.GetAs<CockpitUserInput>().ServerRotationIndicator.X; };
			MyAPIGateway.TerminalControls.AddControl<IMyCockpit>(propertyRotIndicatorX);

			var propertyRotIndicatorY = MyAPIGateway.TerminalControls.CreateProperty<float, IMyCockpit>("RotationIndicatorY");
			propertyRotIndicatorY.SupportsMultipleBlocks = false;
			propertyRotIndicatorY.Getter = (block) => { return block.GetAs<CockpitUserInput>().ServerRotationIndicator.Y; };
			MyAPIGateway.TerminalControls.AddControl<IMyCockpit>(propertyRotIndicatorY);

			var propertyMouseWheel = MyAPIGateway.TerminalControls.CreateProperty<int, IMyCockpit>("MouseWheelAbsoluteValue");
			propertyMouseWheel.SupportsMultipleBlocks = false;
			propertyMouseWheel.Getter = (block) => { return block.GetAs<CockpitUserInput>().ServerMouseWheelAbsoluteRotation; };
			propertyMouseWheel.Setter = (block, value) => { block.GetAs<CockpitUserInput>().ServerMouseWheelAbsoluteRotation = value; };			
			MyAPIGateway.TerminalControls.AddControl<IMyCockpit>(propertyMouseWheel);
		} 
		public override void UpdateBeforeSimulation()
		{
			try
			{
				if (Controller == null) return;
				if (MyAPIGateway.Input == null) return;
				if (MyAPIGateway.Gui == null) return;
				if (MyAPIGateway.Multiplayer == null) return;

				if (!MyAPIGateway.Utilities.IsDedicated)
				{					
					if (Controller.IsFunctional && Controller.Pilot != null && (Controller.Pilot.EntityId == MyAPIGateway.Session.LocalHumanPlayer.Character.EntityId))
					{
						if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None)
						{
							MyAPIGateway.Input.GetListOfPressedKeys(ClientKeyboardInputStateCurrent);

							ClientAbsoluteRotationIndicator += new Vector2(MyAPIGateway.Input.GetMouseYForGamePlay(), MyAPIGateway.Input.GetMouseXForGamePlay()) * MOUSE_ROTATION_INDICATOR_MULTIPLIER;

							MouseWheelCumulativeData += MyAPIGateway.Input.DeltaMouseScrollWheelValue();

							// Check if state was changed
							bool KeyboardStateChanged = false;
							if (ClientKeyboardInputStateCurrent.Count != ClientKeyboardInputStatePrev.Count)
								KeyboardStateChanged = true;
							else if (ClientKeyboardInputStateCurrent.Count > 0)
							{
								int KeyCount = Math.Min(ClientKeyboardInputStateCurrent.Count, ClientKeyboardInputStatePrev.Count);
								for (int i = 0; i < KeyCount; i++)
								{
									if (ClientKeyboardInputStateCurrent[i] != ClientKeyboardInputStatePrev[i])
									{
										KeyboardStateChanged = true;
										break;
									}
								}
							}
							if (KeyboardStateChanged)
							{
								byte[] data = ClientGetPressedKeyboardKeysAsPacket(ClientKeyboardInputStateCurrent);
								MyAPIGateway.Multiplayer.SendMessageToServer(COCKPIT_INPUT_PORT, data);
							}
							//MyVisualScriptLogicProvider.ShowNotification("ClientAbsoluteRotationIndicator: " + ClientAbsoluteRotationIndicator.ToString(), 16, "Green");
							byte[] rotation_data = ClientGetRotationAsPacket(ClientAbsoluteRotationIndicator, MouseWheelCumulativeData);
							MyAPIGateway.Multiplayer.SendMessageToServer(COCKPIT_ROTATION_PORT, rotation_data, false);  // we use unreliable transmission because it's faster. We actually transmit time critical data here, so we need a instant data flow.

							List<MyKeys> temp = ClientKeyboardInputStatePrev;
							ClientKeyboardInputStatePrev = ClientKeyboardInputStateCurrent;
							ClientKeyboardInputStateCurrent = temp;
							ClientPacketID++;
						}
					}
					else
					{
						if ((Controller.Pilot != PreviousePlayer) || (WasFunctional != Controller.IsFunctional))
						{
							ClientKeyboardInputStateCurrent.Clear();
							byte[] data = ClientGetPressedKeyboardKeysAsPacket(ClientKeyboardInputStateCurrent);
							MyAPIGateway.Multiplayer.SendMessageToServer(COCKPIT_INPUT_PORT, data);
						}
					}
					WasFunctional = Controller.IsFunctional;
					PreviousePlayer = Controller.Pilot;
				}
			}
			catch { }				
		}
		public static StringBuilder ConvertKeysToString(List<MyKeys> keys)
		{
			StringBuilder SB = new StringBuilder();
			for (int i = 0; i < keys.Count; i++)
			{
				SB.AppendLine(Enum.GetName(typeof(MyKeys), keys[i]));
			}
			return SB;
		}
		public byte[] ClientGetPressedKeyboardKeysAsPacket(List<MyKeys> Keys)
		{
			byte[] data = new byte[sizeof(long) + sizeof(byte) + Keys.Count];
			int offset = 0;
			data.Pack(0, Controller.EntityId); offset += sizeof(long);
			data[offset] = (byte)Keys.Count; offset += sizeof(byte);
			for (int i = 0; i < Keys.Count; i++)
			{
				data[offset] = (byte)Keys[i];
				offset += sizeof(byte);
			}
			return data;
		}
		public byte[] ClientGetRotationAsPacket(Vector2 InRotationIndicator, int MouseWheelValue)
		{
			byte[] data = new byte[sizeof(long) + sizeof(uint) + sizeof(float) * 2 + sizeof(int)];
			int offset = 0;
			data.Pack(0, Controller.EntityId); offset += sizeof(long);
			data.Pack(offset, ClientPacketID); offset += sizeof(uint);
			data.Pack(offset, InRotationIndicator.X); offset += sizeof(float);
			data.Pack(offset, InRotationIndicator.Y); offset += sizeof(float);
			data.Pack(offset, MouseWheelValue);
			return data;
		}
		public static void ServerSetPressedKeyboardKeysFromPacket(byte[] data)
		{			
			var id = data.Long(0);
			var block = id.As<IMyTerminalBlock>();
			if (block == null) return;
			if (block.GetAs<CockpitUserInput>() == null) return;
			ServerOnHandleKeyboardInput(block, data);
		}
		public static void ServerSetRotationFromPacket(byte[] data)
		{
			var id = data.Long(0);			
			var block = id.As<IMyTerminalBlock>();
			if (block == null) return;
			if (block.GetAs<CockpitUserInput>() == null) return;
			ServerOnHandleRotationInput(block, data);
		}

		public static void ServerOnHandleKeyboardInput(IMyTerminalBlock b, byte[] data)
		{
			var SC = (b as IMyShipController);
			var CUI = b.GetAs<CockpitUserInput>();
			if (CUI == null)
				return;

			try
			{
				int offset = sizeof(long); // we've read the ID already
				byte KeyCount = data[offset]; offset += sizeof(byte);

				if (KeyCount > 0)
				{
					List<MyKeys> keys = new List<MyKeys>();
					for (int i = 0; i < KeyCount; i++)
					{
						keys.Add((MyKeys)data[offset]);
						offset += sizeof(byte);
					}
					CUI.ServerKeyboardInputState = keys;
				}
				else
				{
					CUI.ServerKeyboardInputState.Clear();
				}
				
				CUI.ServerKeyboardInputString = ConvertKeysToString(CUI.ServerKeyboardInputState).ToString();
			}
			catch (Exception e) { }
		}
		public static void ServerOnHandleRotationInput(IMyTerminalBlock b, byte[] data)
		{
			try
			{
				var SC = (b as IMyShipController);
				var CUI = b.GetAs<CockpitUserInput>();
				if (CUI == null)
					return;
				var PackedID = data.UInt(sizeof(long));
				// If the networking data packet comes in wrong order, use the latest possible value to decrease rubber-banding
				if (PackedID <= CUI.ServerPacketID && (CUI.ServerPacketID - PackedID) < 100000)
				{
					return;
				}
				CUI.ServerPacketID = PackedID;

				int offset = sizeof(long) + sizeof(uint); // we've read the ID already
				float X = data.Float(offset); offset += sizeof(float);
				float Y = data.Float(offset); offset += sizeof(float);
				CUI.ServerMouseWheelAbsoluteRotation = data.Int(offset);
				CUI.ServerRotationIndicator.X = X;
				CUI.ServerRotationIndicator.Y = Y;
			}
			catch (Exception e) { }
		}
	}
}
 
 