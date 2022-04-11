using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles.Lords;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
	public static class GatherItemsForVehicleCaravanUtility
	{
		private static HashSet<Thing> neededItems = new HashSet<Thing>();

		public static Thing FindThingToHaul(Pawn p, Lord lord)
		{
			neededItems.Clear();
			List<TransferableOneWay> transferables = ((LordJob_FormAndSendVehicles)lord.LordJob).transferables;
			for (int i = 0; i < transferables.Count; i++)
			{
				TransferableOneWay transferableOneWay = transferables[i];
				if (CountLeftToTransfer(p, transferableOneWay, lord) > 0)
				{
					for (int j = 0; j < transferableOneWay.things.Count; j++)
					{
						neededItems.Add(transferableOneWay.things[j]);
					}
				}
			}
			if (!neededItems.Any())
			{
				return null;
			}
			Thing result = GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), 
				PathEndMode.Touch, TraverseParms.For(p, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, (Thing x) => 
				neededItems.Contains(x) && p.CanReserve(x, 1, -1, null, false), null, 0, -1, false, RegionType.Set_Passable, false);
			neededItems.Clear();
			return result;
		}

		public static int CountLeftToTransfer(Pawn pawn, TransferableOneWay transferable, Lord lord)
		{
			if (transferable.CountToTransfer <= 0 || !transferable.HasAnyThing)
			{
				return 0;
			}
			int x = Mathf.Max(transferable.CountToTransfer - TransferableCountHauledByOthers(pawn, transferable, lord), 0);
			return x;
		}

		private static int TransferableCountHauledByOthers(Pawn pawn, TransferableOneWay transferable, Lord lord)
		{
			if (!transferable.HasAnyThing)
			{
				Log.Warning("Can't determine transferable count hauled by others because transferable has 0 things.");
				return 0;
			}
			List<Pawn> allPawnsSpawned = lord.Map.mapPawns.AllPawnsSpawned;
			int num = 0;
			for (int i = 0; i < allPawnsSpawned.Count; i++)
			{
				Pawn pawn2 = allPawnsSpawned[i];
				if (pawn2 != pawn)
				{
					if (pawn2.CurJob != null && pawn2.CurJob.def == JobDefOf_Vehicles.PrepareCaravan_GatheringVehicle && pawn2.CurJob.lord == lord)
					{
						Thing toHaul = ((JobDriver_PrepareVehicleCaravan_GatheringItems)pawn2.jobs.curDriver).ToHaul;
						if (transferable.things.Contains(toHaul) || TransferableUtility.TransferAsOne(transferable.AnyThing, toHaul, TransferAsOneMode.PodsOrCaravanPacking))
						{
							num += toHaul.stackCount;
						}
					}
				}
			}
			return num;
		}
	}
}
