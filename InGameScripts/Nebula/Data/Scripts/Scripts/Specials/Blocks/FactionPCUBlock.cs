using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials.Messaging;
using Slime;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Scripts.Specials.Blocks
{
	//[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true)]
	public class FactionPCUBlock : MyGameLogicComponent
	{
		private static bool INITED = false;
		IMyFunctionalBlock block;
		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			base.Init(objectBuilder);
			block = (Entity as IMyFunctionalBlock);
			InitControls();
		}

		public void InitControls ()
		{
			if (INITED) return;
			INITED = true;

			MyAPIGateway.TerminalControls.CreateButton<FactionPCUBlock, IMyUpgradeModule>("FactionPCUBlock", "Transfer my pcu", "Transfer your pcu to faction NPC" , 
				(x)=>x.Transfer(MyAPIGateway.Session.Player.IdentityId), 
				(x)=>x.block.HasLocalPlayerAccess(), 
				(x)=>true);
		}

		

		public void Transfer (long player)
		{
			var fac = player.PlayerFaction();
			if (fac == null) return;
			var npc = GetOrCreateNpc (fac);

			Common.SendChatMessage ("NPC:"+npc);

			var list = new List<IMySlimBlock>(1000);
			var subs = block.CubeGrid.GetConnectedGrids(GridLinkTypeEnum.Logical);
            foreach (var g in subs)
			{
				g.GetBlocks(list, (b) => {
					if (b.BuiltBy == player && b.BuiltBy != npc) return false;
					if (TorchConnection.IsLimitedBlock((b.BlockDefinition as MyCubeBlockDefinition).BlockPairName)) return false; // мы не переносим лимиты
					return true;
				});	
			}

			TorchConnection.TransferPCU(list, npc);
		}

		private static long GetOrCreateNpc(IMyFaction fac)
		{
			var allBots = new List<IMyIdentity>();
			MyAPIGateway.Players.GetAllIdentites(allBots, (x) => x.isBot());
			var npc = new List<IMyIdentity>();

			foreach (var m in fac.Members)
			{
				foreach (var b in allBots)
				{
					if (b.IdentityId == m.Value.PlayerId)
					{
						npc.Add(b);

						break;
					}
				}

			}

			long npcId = 0;

			if (npc.Count == 0)
			{
				return TorchConnection.CreateNPC(fac);
			}
			else
			{
				return npc[0].IdentityId;
			}
		}
	}
}
