using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Text;
using Digi;
using Sandbox.Common.ObjectBuilders;
using ServerMod;
using Slime;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Entities;
using Scripts.Shared.Serialization;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using Scripts.Specials.Blocks.StackableMultipliers;
using Scripts.Specials;

namespace Scripts
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), true)]//LargeBlockMediumAtmosphericThrustT04
	public class RealisticThruster : MyGameLogicComponent
	{
		private const ushort PORT_REALISTIC_THRUST = 24002;
		private bool StorageLoaded = false;
		private static readonly Guid guid = new Guid("c88b4f1d-3642-4db6-b8e8-5935a6c02c59"); //SERVER ONLY
		static bool Initialized;
		public MyThrust Block;
		public bool RealisticMode { get; private set; }
		public bool AllwaysOff = false;
		public bool AllwaysOn = false;
		private EndlessMultiplierEffect effect;

        private static void SyncData(string data, long id)  // entity.id
        {
            if (data == null) Log.ChatError("SyncData = null");
            MyAPIGateway.Multiplayer.SendMessageToOthersProto(PORT_REALISTIC_THRUST, new StringData { Data = data, Id = id });
        }

        private static void HandleData(byte[] data)
        {
            try
            {
                var d = StringData.DeSerialize(data);
                var thr = d.Id.As<IMyThrust>();
                var RTX = thr?.GetAs<RealisticThruster>();
                if (RTX == null) return;
                RTX.DeserializeState(d.Data);
                RTX.SetRealisticMode(RTX.RealisticMode);
                RTX.SaveToStorage();
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }
        public static void InitHandlers()
        {
            try
            {
                MyAPIGateway.Multiplayer.RegisterMessageHandler(PORT_REALISTIC_THRUST, HandleData);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        public static void CloseHandlers()
        {
            try
            {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(PORT_REALISTIC_THRUST, HandleData);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }



        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			RealisticMode = false;
			Block = (MyThrust)Entity;
			NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
			var sn = Block.BlockDefinition.Id.SubtypeName;

			AllwaysOff = sn.Contains ("Hover");
			AllwaysOn = !sn.Contains ("Hover") && !sn.Contains ("Atmo") && !sn.Contains("Heli");
			

			if (!Initialized)
			{
				Initialized = true;
				InitActions();
				InitProperties();				
			}
			if (!StorageLoaded) // If placed in Init its loading incorrectly
			{
				LoadFromStorage();
			}
		}

		public override void UpdateOnceBeforeFrame()
		{
			base.UpdateOnceBeforeFrame();
			SetRealisticMode(RealisticMode);
		}
		
		public void SetRealisticMode(bool realisticMode)
		{
			if (AllwaysOn) realisticMode = true;
			if (AllwaysOff) realisticMode = false;

			RealisticMode = realisticMode;
			var tbase = Block.GetAs<ThrusterBase>();
			if (effect == null)
			{
				effect = new EndlessMultiplierEffect(2, Entity.EntityId, Entity.EntityId, 1, 1, 1);
				tbase.AddEffect(effect);
			}
			
			effect.m1 = realisticMode ? 1.5f : 1;
			tbase.Recalculate();
        }


		public string SerializeState()
		{
			return RealisticMode ? "true" : "false";
		}
		public void DeserializeState(string Values)
		{
			RealisticMode = Values == "true";
        }

		private void LoadFromStorage()
		{
			try
			{				
				if (Block.Storage == null) return;
				//MyVisualScriptLogicProvider.ShowNotification("LoadedStorage", 20000, "Green");
				string SData = "";
				if (!Block.Storage.TryGetValue(guid, out SData) || SData == null) return;
				DeserializeState(SData);
				StorageLoaded = true;
			}
			catch (Exception e)
			{
				Log.ChatError("LoadData:error", e);
			}
		}		
		private void SaveToStorage()
		{
			//if (Block.Storage == null) Entity.Storage = new MyModStorageComponent();
			MyModStorageComponentBase Storage = Serialization.GetOrCreateStorage(Block);
			
			string SData = SerializeState();
			//MyVisualScriptLogicProvider.ShowNotification("SavedStorage " + SData, 20000, "Green");
			Storage?.SetValue(guid, SData);
		}
		private static void InitProperties()
		{
			var UseRealisticThrust = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyThrust>("UseRealisticThrust");
			UseRealisticThrust.Title = MyStringId.GetOrCompute("Use Realistic Thrust");
			UseRealisticThrust.Tooltip = MyStringId.GetOrCompute("Реалистичный Вектор Тяги");
			UseRealisticThrust.Getter = (b) => b.GetAs<RealisticThruster>().RealisticMode;
			UseRealisticThrust.Setter = (b, v) =>
			{
				var RTX = b.GetAs<RealisticThruster>();
				if (RTX.AllwaysOn) v = true;
				if (RTX.AllwaysOff) v = false;

				if (RTX.RealisticMode != v)
				{
					RTX.SetRealisticMode(v);
					RTX.SaveToStorage();
					SyncData(RTX.SerializeState(), RTX.Block.EntityId);
				}
			};
			UseRealisticThrust.Enabled = (b) => {
				var RTX = b.GetAs<RealisticThruster>();
				if (RTX == null) return false;
				if (RTX.AllwaysOn || RTX.AllwaysOff) return false;
				return true;
			};
			UseRealisticThrust.Visible = (b) => b.GetAs<RealisticThruster>() != null;
			MyAPIGateway.TerminalControls.AddControl<IMyThrust>(UseRealisticThrust);
		}

		private static void InitActions()
		{
			var UseRealisticThrustAction = MyAPIGateway.TerminalControls.CreateAction<IMyThrust>("Thrust_UseRealisticThrust_OnOff");
			UseRealisticThrustAction.Action = (b) =>
			{
				var RTX = b.GetAs<RealisticThruster>();
				if (RTX.AllwaysOn) return;
				if (RTX.AllwaysOff) return;

				RTX.SetRealisticMode(!RTX.RealisticMode);
				RTX.SaveToStorage();
				SyncData(RTX.SerializeState(), RTX.Block.EntityId);				
			};

			UseRealisticThrustAction.Name = new StringBuilder("Use realistic thrust On/Off");
			UseRealisticThrustAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
			UseRealisticThrustAction.Writer = (b, sb) => sb.Append(b.GetAs<RealisticThruster>().RealisticMode ? "Realistic" : "Vanilla");
			UseRealisticThrustAction.ValidForGroups = true;
			UseRealisticThrustAction.Enabled = (b) => b.GetAs<RealisticThruster>() != null;
			MyAPIGateway.TerminalControls.AddAction<IMyThrust>(UseRealisticThrustAction);
		}
	}
}
