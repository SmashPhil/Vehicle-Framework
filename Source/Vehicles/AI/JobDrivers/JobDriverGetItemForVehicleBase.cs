using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
public abstract class JobDriverGetItemForVehicleBase : JobDriverLoadVehicleBase
{
	private static readonly HashSet<ThingDef> NeededThingDefs = [];

	protected abstract string ListerTag { get; }

	protected VehiclePawn Vehicle => Carrier as VehiclePawn;

	protected abstract IEnumerable<ThingDefCountClass> ThingsToLoad { get; }

	protected override bool HasDuplicateOpportunity(Thing thing)
	{
		return ThingsToLoad.FirstOrDefault(thingDefCount => thingDefCount.thingDef == thing.def) != null;
	}

	protected override bool ShouldFailJob()
	{
		return !Map.GetCachedMapComponent<VehicleReservationManager>()
		 .VehicleListed(Vehicle, ListerTag);
	}

	protected override int CountLeftToTransfer()
	{
		return CountLeftToPack(Vehicle, pawn, GetMatchingThing(ToHaul));
	}

	protected override Thing FindThingToHaul()
	{
		return FindThingToPack(Vehicle, pawn, ThingsToLoad);
	}

	protected override bool IsUsableCarrier(Pawn carrier, bool allowColonists = true)
	{
		if (carrier.DestroyedOrNull() || !carrier.Spawned)
			return false;
		if (carrier.Faction != pawn.Faction)
			return false;
		if (carrier.IsBurning())
			return false;

		return carrier == Vehicle;
	}

	public static int CountLeftToPack(VehiclePawn vehicle, Pawn pawn, ThingDefCountClass thingDefCount)
	{
		if (thingDefCount.count <= 0 || thingDefCount.thingDef == null)
			return 0;

		int hauledByOthers = Search.TransferableCountHauledByOthersForPacking(vehicle, pawn, null, Validator);
		int hauledBySelf = 0;
		foreach (Thing thing in UnpackedCaravanItems.Invoke(pawn.inventory))
		{
			hauledBySelf += thing.def == thingDefCount.thingDef ? thing.stackCount : 0;
		}
		int remaining = thingDefCount.count - hauledByOthers - hauledBySelf;
		return Mathf.Clamp(remaining, 0, int.MaxValue);

		bool Validator(Thing thing)
		{
			return thing.def == thingDefCount.thingDef;
		}
	}

	public static Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn,
		[CanBeNull] IEnumerable<ThingDefCountClass> thingDefCounts)
	{
		if (thingDefCounts == null)
			return null;

		using ClearOnDispose<ThingDef> cod = new(NeededThingDefs);
		foreach (ThingDefCountClass thingDefCount in thingDefCounts)
		{
			int countLeftToTransfer = CountLeftToPack(vehicle, pawn, thingDefCount);
			if (countLeftToTransfer <= 0)
				continue;
			NeededThingDefs.Add(thingDefCount.thingDef);
		}
		if (NeededThingDefs.Count == 0)
			return null;

		return Search.FindNearestThing(pawn, HasThingDef);

		static bool HasThingDef(Thing thing)
		{
			return NeededThingDefs.Contains(thing.def);
		}
	}

	private ThingDefCountClass GetMatchingThing(Thing thing)
	{
		return ThingsToLoad.FirstOrDefault(thingDefCount => thing.def == thingDefCount.thingDef);
	}
}