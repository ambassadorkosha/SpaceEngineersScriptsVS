using Sandbox.ModAPI;
using VRageMath;
using System.Collections.Generic;
using Digi;
using ServerMod;
using Digi2.AeroWings;

namespace Scripts.Specials.ShipClass
{
	public class OrientationOrderedBlocks
	{
		public delegate bool DelCompare(MyBlockOrientation O1, MyBlockOrientation O2);
		public const int BlockSideCount = 16; // obvious
		public class Blocks 
		{
			public MyBlockOrientation Orientation;
			public HashSet<IMyTerminalBlock> BlocksList;
			public Blocks(IMyTerminalBlock InBlock)
			{
				BlocksList = new HashSet<IMyTerminalBlock>();
				Orientation = InBlock.Orientation;
				BlocksList.Add(InBlock);
			}
		}

		public Blocks[] OrientedBlocks;
		public DelCompare CompareMethod;
		public OrientationOrderedBlocks()
		{
			OrientedBlocks = new Blocks[BlockSideCount];
		}

		public int Count {
			get {
				var l = 0;
				foreach (var b in OrientedBlocks) {
					if (b != null) l+=b.BlocksList.Count;
				}
				return l;
			}
		}
		
		public void Add(IMyTerminalBlock InBlock)
		{
			if (InBlock == null) return;
			//Log.ChatError("AddWBlock: " + InBlock.CustomName + " " + (InBlock.GetAs<WingTN>() != null) + " / " + (InBlock.GetAs<Eleron>() != null));

			int NullBlock = -1;
			if (CompareMethod != null)
				for (int i = 0; i < BlockSideCount; i++)
				{
					if (OrientedBlocks[i] != null)
					{
						if (CompareMethod(OrientedBlocks[i].Orientation, InBlock.Orientation))
						{
							OrientedBlocks[i].BlocksList.Add(InBlock);
							//Log.ChatError("AddWBlock: " + i + " " + InBlock.CustomName + " " + (InBlock.GetAs<WingTN>() != null) + " / " + (InBlock.GetAs<Eleron>() != null));
							return;
						}
					}
					else
						NullBlock = i;
				}
			else
				for (int i = 0; i < BlockSideCount; i++)
				{
					if (OrientedBlocks[i] != null)
					{
						if (OrientedBlocks[i].Orientation == InBlock.Orientation)
						{
							OrientedBlocks[i].BlocksList.Add(InBlock);
							//Log.ChatError("AddWBlock: " + i + " " + InBlock.CustomName + " " + (InBlock.GetAs<WingTN>() != null) + " / " + (InBlock.GetAs<Eleron>() != null));
							return;
						}
					}
					else
						NullBlock = i;
				}

			if (NullBlock >= 0)
			{
				OrientedBlocks[NullBlock] = new Blocks(InBlock);
				//Log.ChatError("AddWBlock: " + NullBlock + " " + InBlock.CustomName + " " + (InBlock.GetAs<WingTN>() != null) + " / " + (InBlock.GetAs<Eleron>() != null));
			}
				

			else
			{
				Log.ChatError("OOOPS? : " + InBlock.Orientation + " " + InBlock.CustomName + " " + (InBlock.GetAs<WingTN>() != null) + " / " + (InBlock.GetAs<Eleron>() != null));
			}
		}
		public bool Remove(IMyTerminalBlock InBlock)
		{
			if (InBlock == null) return false;
			if (CompareMethod != null)
				for (int i = 0; i < BlockSideCount; i++)
				{
					if (OrientedBlocks[i] != null && CompareMethod(OrientedBlocks[i].Orientation, InBlock.Orientation))
					{
						if (OrientedBlocks[i].BlocksList.Contains(InBlock))
						{
							OrientedBlocks[i].BlocksList.Remove(InBlock);
							return true;
						}
					}
				}
			else
				for (int i = 0; i < BlockSideCount; i++)
				{
					if (OrientedBlocks[i] != null && OrientedBlocks[i].Orientation == InBlock.Orientation)
					{
						if (OrientedBlocks[i].BlocksList.Contains(InBlock))
						{
							OrientedBlocks[i].BlocksList.Remove(InBlock);
							return true;
						}
					}
				}
			return false;
		}
	}
}
