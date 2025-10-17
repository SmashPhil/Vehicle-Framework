using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Performance;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public static class EnterMapUtilityVehicles
{
	private static bool SettleMapCellValidator(Map map, IntVec3 cell, VehicleDef vehicleDef)
	{
		return VehicleRegionAndRoomQuery.RoomAt(cell, map, vehicleDef) is { CellCount: >= 600 };
	}

	// TODO 1.6.2136
	[Obsolete("Use EnterMap instead.", error: true)]
	public static void EnterAndSpawn(VehicleCaravan caravan, Map map, CaravanEnterMode enterMode,
		CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop,
		bool draftColonists = false, Predicate<IntVec3> extraValidator = null)
	{
		if (enterMode == CaravanEnterMode.None)
		{
			Log.Error(
				$"VehicleCaravan {caravan} tried to enter map {map} with no enter mode. Defaulting to edge.");
			enterMode = CaravanEnterMode.Edge;
		}

		IntVec3 enterCell = GetEnterCellVehicle(caravan, map, enterMode, extraValidator);
		Rot4 edge = enterMode == CaravanEnterMode.Edge ?
			CellRect.WholeMap(map).GetClosestEdge(enterCell) :
			Rot4.North;
		// Removed and pulls straight from caravan pawn lists
		SpawnCaravanPawns(caravan, /*caravan.PawnsListForReading.Where(p => !p.InVehicle()).ToList(),*/ map,
			enterCell, edge, draftColonists);
	}

	// TODO 1.6.2136
	[Obsolete("Deprecated", error: true)]
	public static IntVec3 GetEnterCellVehicle(VehicleCaravan caravan, Map map,
		CaravanEnterMode enterMode, Predicate<IntVec3> extraCellValidator)
	{
		caravan.EnsureMapInitialized(map);
		switch (enterMode)
		{
			case CaravanEnterMode.Edge:
				return FindNearEdgeCell(map, caravan.LeadVehicle.VehicleDef, caravan.Faction, new SpawnParams(enterMode));
			case CaravanEnterMode.Center:
				return FindCenterCell(map, caravan.LeadVehicle.VehicleDef, new SpawnParams(enterMode));
			case CaravanEnterMode.None:
			default:
				throw new NotImplementedException("CaravanEnterMode");
		}
	}

	public static void EnterMap(VehicleCaravan caravan, Map map, in SpawnParams spawnParams)
	{
		if (spawnParams.enterMode == CaravanEnterMode.None)
		{
			Trace.Fail(
				$"VehicleCaravan {caravan} tried to enter map {map} with no enter mode. Defaulting to edge.");
		}

		IntVec3 enterCell = GetEnterCellVehicle(caravan, map, in spawnParams);
		Rot4 edge = spawnParams.enterMode == CaravanEnterMode.Edge ?
			CellRect.WholeMap(map).GetClosestEdge(enterCell) :
			Rot4.North;
		SpawnCaravanPawns(caravan, map, enterCell, edge, spawnParams.draftColonists);
	}

	private static IntVec3 GetEnterCellVehicle(VehicleCaravan caravan, Map map, in SpawnParams spawnParams)
	{
		caravan.EnsureMapInitialized(map);
		switch (spawnParams.enterMode)
		{
			case CaravanEnterMode.None:
			case CaravanEnterMode.Edge:
				return FindNearEdgeCell(map, caravan.LeadVehicle.VehicleDef, caravan.Faction, spawnParams);
			case CaravanEnterMode.Center:
				return FindCenterCell(map, caravan.LeadVehicle.VehicleDef, spawnParams);
			default:
				throw new NotImplementedException("CaravanEnterMode");
		}
	}

	[Obsolete("Method signature changed, patch SpawnCaravanPawns instead.")]
	private static void SpawnVehicles(VehicleCaravan caravan, List<Pawn> pawns, Map map, IntVec3 enterCell, Rot4 edge, bool draftColonists)
	{
	}

	private static void SpawnCaravanPawns(VehicleCaravan caravan, Map map, IntVec3 enterCell, Rot4 edge, bool draftColonists)
	{
		using (new VehicleCaravan.RecacheDisabler(caravan))
		{
			bool coastalSpawn = caravan.HasBoat();
			using var cr = GlobalObjectPool.Get(out List<Pawn> pawns);
			pawns.AddRange(caravan.pawns);
			foreach (Pawn pawn in pawns)
			{
				IntVec3 cell = CellFinderExtended.RandomSpawnCellForPawnNear(enterCell, map, pawn,
					cell => cell.StandableUnknown(pawn, map), coastalSpawn);
				IntVec3 loc = pawn.ClampToMap(cell, map, extraOffset: 2);
				GenSpawn.Spawn(pawn, loc, map, edge.Opposite);
				if (!pawn.Spawned)
				{
					Trace.Fail($"Unable to spawn {pawn} in map. Sending back to caravan.");
					if (!caravan.ContainsPawn(pawn))
						caravan.AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
					continue;
				}

				if (pawn.IsColonist && !pawn.InMentalState)
				{
					pawn.drafter.Drafted = draftColonists;
				}

				if (pawn is VehiclePawn vehicle)
				{
					vehicle.Angle = 0;
					vehicle.ignition.Drafted = draftColonists;
				}
			}
			// TODO 1.6.2136 - Trains of the Rim post-spawn patch
#pragma warning disable CS0618
			SpawnVehicles(caravan, pawns, map, enterCell, edge, draftColonists);
#pragma warning restore CS0618
		}
		
		if (caravan.pawns.Count == 0)
		{
			caravan.Destroy();
		}
	}

	private static Rot4 CalculateEdgeToSpawnBoatOn(Map map)
	{
		if (Find.World.CoastDirectionAt(map.Tile) is { IsValid: true } coastDir)
			return coastDir;

		SurfaceTile surfaceTile = Find.WorldGrid.Surface[map.Tile];
		if (surfaceTile is null || surfaceTile.Rivers.NullOrEmpty())
			return Rot4.Invalid;

		float angle = Find.WorldGrid.GetHeadingFromTo(map.Tile,
			surfaceTile.Rivers.OrderBy(link => link.river.degradeThreshold).First().neighbor);
		return angle.ClampAngle() switch
		{
			< 45 => Rot4.South,
			< 135 => Rot4.East,
			< 225 => Rot4.North,
			< 315 => Rot4.West,
			_ => throw new ArgumentException("ClampAndWrap did not return valid 0:360 value")
		};
	}

	private static IntVec3 FindCenterCell(Map map, VehicleDef vehicleDef, SpawnParams spawnParams)
	{
		if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(
			cell => Validator(map, vehicleDef, cell, spawnParams), map, out IntVec3 result))
			return result;
		Log.Warning("Could not find any valid cell.");
		return CellFinder.RandomCell(map);

		static bool Validator(Map map, VehicleDef vehicleDef, IntVec3 cell, in SpawnParams spawnParams)
		{
			if (spawnParams.extraCellValidator != null && !spawnParams.extraCellValidator(cell, map, vehicleDef))
				return false;

			return cell.Standable(vehicleDef, map) && !cell.Fogged(map) &&
				map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.NoPassClosedDoors));
		}
	}

	private static IntVec3 FindNearEdgeCell(Map map, VehicleDef vehicleDef, Faction faction, SpawnParams spawnParams)
	{
		Rot4 rot = Rot4.Random;
		if (vehicleDef.type == VehicleType.Sea)
		{
			rot = CalculateEdgeToSpawnBoatOn(map);
		}

		RoadPreference preference = RoadPreferenceFor(faction);
		while (preference > RoadPreference.Invalid)
		{
			if (TryFindCellWithBestPreference(out IntVec3 root))
				return root;
			preference--;
		}

		Log.Warning("Could not find any valid edge cell.");
		return CellFinder.RandomCell(map);

		bool TryFindCellWithBestPreference(out IntVec3 foundCell)
		{
			foundCell = IntVec3.Invalid;

			if (TryFindNearEdgeCell(map, vehicleDef, rot, preference, spawnParams, out foundCell))
				return true;

			if (TryFindNearEdgeCell(map, vehicleDef, rot.Opposite, preference, spawnParams,
				out foundCell))
				return true;

			if (TryFindNearEdgeCell(map, vehicleDef, rot.Rotated(RotationDirection.Clockwise),
				preference, spawnParams, out foundCell))
				return true;

			if (TryFindNearEdgeCell(map, vehicleDef, rot.Rotated(RotationDirection.Counterclockwise),
				preference, spawnParams, out foundCell))
				return true;

			return false;
		}
	}

	private static bool TryFindNearEdgeCell(Map map, VehicleDef vehicleDef, Rot4 rot, RoadPreference roadPref,
		SpawnParams spawnParams, out IntVec3 root)
	{
		Faction hostFaction = map.ParentFaction;
		if (CellFinderExtended.TryFindRandomEdgeCellWith(OptimalSpot, map, rot, vehicleDef, CellFinder.EdgeRoadChance_Always, 
			out root))
		{
			return true;
		}

		if (CellFinderExtended.TryFindRandomEdgeCellWith(MinimalValidator, map, rot, vehicleDef, 
			CellFinder.EdgeRoadChance_Always, out root))
		{
			root = CellFinderExtended.RandomClosewalkCellNear(root, map, vehicleDef, 5);
			return true;
		}

		return false;

		bool MinimalValidator(IntVec3 cell)
		{
			if (!cell.Standable(vehicleDef, map) || cell.Fogged(map))
				return false;

			if (spawnParams.extraCellValidator != null && !spawnParams.extraCellValidator(cell, map, vehicleDef))
				return false;

			return true;
		}

		bool OptimalSpot(IntVec3 cell)
		{
			if (!cell.Standable(vehicleDef, map) || cell.Fogged(map))
				return false;

			if (spawnParams.extraCellValidator != null && !spawnParams.extraCellValidator(cell, map, vehicleDef))
				return false;

			if (!AllowsPreference(map, cell, roadPref))
				return false;

			VehiclePathingSystem.VehiclePathData pathData = map.GetCachedMapComponent<VehiclePathingSystem>()[vehicleDef];
			return hostFaction != null && pathData.VehicleReachability.CanReachBase(cell, vehicleDef) || 
				hostFaction == null && pathData.VehicleReachability.CanReachBiggestMapEdgeRoom(cell);
		}
	}

	private static RoadPreference RoadPreferenceFor(Faction faction)
	{
		return faction.HostileTo(Faction.OfPlayer) ? RoadPreference.None : RoadPreference.Prioritize;
	}

	private static bool AllowsPreference(Map map, IntVec3 cell, RoadPreference roadPref)
	{
		switch (roadPref)
		{
			case RoadPreference.NoAvoidal:
				Area_RoadAvoidal areaAvoid = map.areaManager.Get<Area_RoadAvoidal>();
				return !areaAvoid[cell];
			case RoadPreference.Prioritize:
				Area_Road areaPrefer = map.areaManager.Get<Area_Road>();
				return areaPrefer[cell];
		}
		return true;
	}

	private enum RoadPreference
	{
		Invalid,
		None,
		NoAvoidal,
		Prioritize,
	}

	public struct SpawnParams
	{
		public required CaravanEnterMode enterMode = CaravanEnterMode.Edge;
		public CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop;
		public bool draftColonists = false;
		public SpawnCellValidator extraCellValidator;

		[SetsRequiredMembers]
		public SpawnParams(CaravanEnterMode enterMode)
		{
			this.enterMode = enterMode;
		}

		public delegate bool SpawnCellValidator(IntVec3 cell, Map map, VehicleDef vehicleDef);
	}
}