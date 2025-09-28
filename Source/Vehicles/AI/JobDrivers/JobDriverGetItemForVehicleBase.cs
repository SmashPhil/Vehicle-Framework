using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

[PublicAPI]
public abstract class JobDriverGetItemForVehicleBase : JobDriverLoadVehicleBase
{
	private static readonly ObjectPool<ThingDefSet> SetPool = new(5);
	private static readonly ObjectPool<ThingDefCountSearch> SearchPool = new(5);

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
		return CountLeftToPack(Vehicle, pawn, job.def, GetMatchingThing(ToHaul));
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

	public static int CountLeftToPack(VehiclePawn vehicle, Pawn pawn, JobDef jobDef, ThingDefCountClass thingDefCount)
	{
		if (thingDefCount.count <= 0 || thingDefCount.thingDef == null)
			return 0;

		using var ops = SearchPool.GetTemporary(out ThingDefCountSearch thingDefSearch);
		thingDefSearch.Init(jobDef, thingDefCount);
		int hauledByOthers = Search.CountHauledByOthersForPacking(vehicle, pawn, thingDefSearch);
		int hauledBySelf = 0;
		foreach (Thing thing in UnpackedCaravanItems.Invoke(pawn.inventory))
		{
			hauledBySelf += thing.def == thingDefCount.thingDef ? thing.stackCount : 0;
		}
		int remaining = thingDefCount.count - hauledByOthers - hauledBySelf;
		return Mathf.Clamp(remaining, 0, int.MaxValue);
	}

	/// <summary>
	/// Search for item to pack given the list of required items in <paramref name="thingDefCounts"/>.
	/// </summary>
	/// <remarks>
	/// Assumes the pawn is currently operating the job. If the pawn has not yet started, you must specify
	/// which JobDef to filter when searching for other colonists hauling for the same job.
	/// </remarks>
	public static Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn,
		[CanBeNull] IEnumerable<ThingDefCountClass> thingDefCounts)
	{
		return FindThingToPack(vehicle, pawn, pawn.CurJobDef, thingDefCounts);
	}

	/// <summary>
	/// Search for item to pack given the list of required items in <paramref name="thingDefCounts"/>.
	/// </summary>
	public static Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn, JobDef jobDef,
		[CanBeNull] IEnumerable<ThingDefCountClass> thingDefCounts)
	{
		if (thingDefCounts == null)
			return null;

		Assert.IsNotNull(jobDef);
		using var ops = SetPool.GetTemporary(out ThingDefSet thingDefSet);
		foreach (ThingDefCountClass thingDefCount in thingDefCounts)
		{
			int countLeftToTransfer = CountLeftToPack(vehicle, pawn, jobDef, thingDefCount);
			if (countLeftToTransfer <= 0)
				continue;
			thingDefSet.Add(thingDefCount.thingDef);
		}
		if (thingDefSet.Count == 0)
			return null;

		return Search.FindNearestThing(pawn, thingDefSet.IsValid);
	}

	private ThingDefCountClass GetMatchingThing(Thing thing)
	{
		foreach (ThingDefCountClass countClass in ThingsToLoad)
		{
			if (countClass.thingDef == thing.def)
				return countClass;
		}
		return null;
	}

	private sealed class ThingDefSet : IPoolable
	{
		private readonly HashSet<ThingDef> neededThingDefs = [];

		public int Count => neededThingDefs.Count;

		bool IPoolable.InPool { get; set; }

		public void Add(ThingDef thingDef)
		{
			neededThingDefs.Add(thingDef);
		}

		public bool IsValid(Thing thing)
		{
			return neededThingDefs.Contains(thing.def);
		}

		void IPoolable.Reset()
		{
			neededThingDefs.Clear();
		}
	}

	protected sealed class ThingDefCountSearch : ISharedJobSearch, IPoolable
	{
		private JobDef jobDef;
		private ThingDefCountClass thingDefCount;

		bool IPoolable.InPool { get; set; }

		public void Init(JobDef jobDef, ThingDefCountClass thingDefCount)
		{
			this.jobDef = jobDef;
			this.thingDefCount = thingDefCount;
		}

		bool ISharedJobSearch.IsMatchingThing(Thing thing)
		{
			return thing.def == thingDefCount.thingDef;
		}

		bool ISharedJobSearch.ShouldConsiderPawn(Pawn otherPawn)
		{
			return otherPawn.CurJobDef == jobDef;
		}

		void IPoolable.Reset()
		{
			jobDef = null;
			thingDefCount = null;
		}
	}
}