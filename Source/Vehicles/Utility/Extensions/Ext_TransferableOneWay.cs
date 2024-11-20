using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Vehicles
{
	public static class Ext_TransferableOneWay
	{
		public static void AddThing(this TransferableOneWay transferable, Thing thing)
		{
			if (transferable.things.Contains(thing) == false)
			{
				transferable.things.Add(thing);
				transferable.AdjustTo(transferable.CountToTransfer + thing.stackCount);
			}
		}

		public static bool RemoveThing(this TransferableOneWay transferable, Thing thing)
		{
			if (transferable.things.Remove(thing))
			{
				transferable.AdjustTo(transferable.CountToTransfer - thing.stackCount);

				return true;
			}

			return false;
		}

		public static void AddThing(this List<TransferableOneWay> transferables, Thing thing, TransferAsOneMode mode = TransferAsOneMode.PodsOrCaravanPacking)
		{
			var transferable = TransferableUtility.TransferableMatching(thing, transferables, mode);

			if (transferable == null)
			{
				transferable = new TransferableOneWay();
				transferables.Add(transferable);
			}

			transferable.AddThing(thing);
		}

		public static bool RemoveThing(this List<TransferableOneWay> transferables, Thing thing)
		{
			var transferable = transferables.FindTransferableFor(thing);

			if (transferable?.RemoveThing(thing) == true)
			{
				if (transferable.HasAnyThing == false)
				{
					transferables.Remove(transferable);
				}

				return true;
			}

			return false;
		}

		// A more precise version of RimWorld.TransferableUtility.TransferableMatching.
		public static TransferableOneWay FindTransferableFor(this List<TransferableOneWay> transferables, Thing thing)
		{
			return transferables.FirstOrFallback(transferable => transferable?.things.Contains(thing) == true);
		}
	}
}
