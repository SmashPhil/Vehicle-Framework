using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace Vehicles;

// TODO 1.7 - Clean up all of the extension methods for Drivable and Standable
[PublicAPI]
[StaticConstructorOnStartup]
public static class Ext_Vehicles
{
	public static void SpawnPawnNearVehicle(this VehiclePawn vehicle, Pawn pawn)
	{
		if (pawn.Spawned)
			return;

		CellRect occupiedRect = vehicle.OccupiedRect().ExpandedBy(1);
		IntVec3 loc = vehicle.Position;
		if (occupiedRect.EdgeCells
		 .Where(cell => cell.InBounds(vehicle.Map) && cell.Standable(vehicle.Map) &&
				!cell.GetThingList(vehicle.Map).NotNullAndAny(thing => thing is Pawn))
		 .TryRandomElement(out IntVec3 newLoc))
		{
			loc = newLoc;
		}

		GenSpawn.Spawn(pawn, loc, vehicle.MapHeld);
		if (!loc.Standable(vehicle.Map))
		{
			pawn.pather.TryRecoverFromUnwalkablePosition(false);
		}

		if (vehicle.lord is not null)
		{
			pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
			vehicle.lord.AddPawn(pawn);
		}
	}

	// NOTE - Separated method hook for SOS2 to patch. This is intentionally redundant.
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsRoofed(IntVec3 cell, Map map)
	{
		return cell.Roofed(map);
	}

	[Pure]
	public static bool IsRoofRestricted(VehicleDef vehicleDef, IntVec3 cell, Map map)
	{
		CompProperties_VehicleLauncher compProperties =
			vehicleDef.GetSortedCompProperties<CompProperties_VehicleLauncher>();
		if (compProperties is null)
		{
			return true;
		}

		bool canRoofPunch = SettingsCache.TryGetValue(vehicleDef,
			typeof(CompProperties_VehicleLauncher),
			nameof(CompProperties_VehicleLauncher.canRoofPunch),
			compProperties.canRoofPunch);
		return IsRoofRestricted(cell, map, canRoofPunch);
	}

	[Pure]
	private static bool IsRoofRestricted(IntVec3 cell, Map map, bool canRoofPunch)
	{
		if (!canRoofPunch)
			return IsRoofed(cell, map);

		RoofDef roofDef = cell.GetRoof(map);
		return roofDef is { isThickRoof: true };
	}

	/// <summary>
	/// Verify if <paramref name="vehicle"/> has enough room for <paramref name="pawns"/>
	/// </summary>
	/// <param name="vehicle">Vehicle to check.</param>
	/// <param name="pawns">Pawns being validated for boarding.</param>
	/// <returns><see langword="true"/> if <paramref name="vehicle"/> has enough room for all <paramref name="pawns"/></returns>
	[MustUseReturnValue]
	public static bool HasRoomFor(this VehiclePawn vehicle, List<Pawn> pawns)
	{
		// TODO - account for permissions (eg. pacifist isn't eligible for turret role)
		VehicleReservationManager reservationMgr = null;
		if (vehicle.Spawned)
			reservationMgr = vehicle.Map.GetCachedMapComponent<VehicleReservationManager>();
		int totalRoom = 0;
		int reserved = 0;
		foreach (VehicleRoleHandler handler in vehicle.handlers)
		{
			totalRoom += handler.role.Slots;
			if (reservationMgr?.GetReservation<VehicleHandlerReservation>(vehicle) is { } reservation)
				reserved += reservation.ClaimantsOnHandler(handler);
		}
		return pawns.Count <= (totalRoom - reserved);
	}

	[Pure]
	public static IntVec2 MirrorRotatedBy(this IntVec2 cell, Rot4 rot, IntVec2 size)
	{
		if (size is { x: 1, z: 1 })
			return cell;
		IntVec2 result = cell.RotatedBy(rot, size);
		switch (rot.AsInt)
		{
			case 1:
				result.x *= -1;
				result.z *= -1;
			break;
			case 3:
				if (size.x.IsEven())
				{
					result.z++;
					result.x--;
				}

				if (size.z.IsEven())
				{
					result.z--;
					result.x--;
				}

				result.x *= -1;
				result.z *= -1;
			break;
		}
		return result;
	}

	/// <summary>
	/// Rotates <paramref name="cell"/> for vehicle rect.
	/// </summary>
	///<remarks>
	/// Rotation is opposite of <paramref name="rot"/> ie. rotating 'east' will return a cell as if
	/// the cell were rotated counter-clockwise (or rotating based on the vehicle facing east). 
	///</remarks>
	[Pure]
	public static IntVec2 RotatedBy(this IntVec2 cell, Rot4 rot, IntVec2 size)
	{
		if (size is { x: 1, z: 1 })
			return cell;

		switch (rot.AsInt)
		{
			case 0:
				return cell;
			case 1:
				IntVec2 east = new(-cell.z, cell.x);
				return east;
			case 2:
				IntVec2 south = new(-cell.x, -cell.z);
				if (size.x.IsEven())
				{
					south.x++;
				}
				if (size.z.IsEven())
				{
					south.z++;
				}
				return south;
			case 3:
				IntVec2 west = new(cell.z, -cell.x);
				if (size.x.IsEven())
				{
					west.x++;
				}
				if (size.z.IsEven())
				{
					west.z++;
				}
				return west;
			default:
				return cell;
		}
	}

	public static void RemoveBoardedPawnsFromLord(this LordJob lordJob, PawnLostCondition condition)
	{
		foreach (Pawn pawn in lordJob.lord.ownedPawns)
		{
			if (pawn is not VehiclePawn vehicle)
				continue;

			foreach (Pawn innerPawn in vehicle.AllPawnsAboard)
			{
				innerPawn.GetLord()?.Notify_PawnLost(pawn, condition);

				lordJob.Map.attackTargetsCache.UpdateTarget(innerPawn);
				if (lordJob.EndPawnJobOnCleanup(innerPawn) && innerPawn.Spawned &&
					innerPawn.CurJob != null &&
					(!lordJob.DontInterruptLayingPawnsOnCleanup ||
						!RestUtility.IsLayingForJobCleanup(innerPawn)))
				{
					innerPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
				}
			}
		}
	}

	[Pure]
	public static CellRect VehicleRect(this VehiclePawn vehicle, bool maxSizePossible = false)
	{
		return vehicle.VehicleRect(vehicle.Position, vehicle.Rotation,
			maxSizePossible: maxSizePossible);
	}

	[Pure]
	public static CellRect VehicleRect(this VehiclePawn vehicle, IntVec3 center, Rot4 rot,
		bool maxSizePossible = false)
	{
		return VehicleRect(vehicle.VehicleDef, center, rot, maxSizePossible: maxSizePossible);
	}

	[Pure]
	public static CellRect VehicleRect(this VehicleDef vehicleDef, IntVec3 center, Rot4 rot,
		bool maxSizePossible = false)
	{
		IntVec2 size = vehicleDef.size;
		AdjustForVehicleOccupiedRect(ref size, ref rot, maxSizePossible: maxSizePossible);
		return GenAdj.OccupiedRect(center, rot, size);
	}

	public static void AdjustForVehicleOccupiedRect(ref IntVec2 size, ref Rot4 rot,
		bool maxSizePossible = false)
	{
		if (rot == Rot4.West) rot = Rot4.East;
		if (rot == Rot4.South) rot = Rot4.North;
		if (maxSizePossible)
		{
			int maxSize = Mathf.Max(size.x, size.z);
			size.x = maxSize;
			size.z = maxSize;
		}
	}

	[Pure]
	public static IntVec3 PadForHitbox(this IntVec3 cell, Map map, VehiclePawn vehicle)
	{
		return PadForHitbox(cell, map, vehicle.VehicleDef);
	}

	[Pure]
	public static IntVec3 PadForHitbox(this IntVec3 cell, Map map, VehicleDef vehicleDef)
	{
		int largestSize = Mathf.Max(vehicleDef.Size.x, vehicleDef.Size.z);
		bool even = largestSize % 2 == 0;
		int padding = Mathf.CeilToInt(largestSize / 2f);
		if (even)
		{
			// If size is even, add 1 to account for rotations with lower center point.
			// This will ensure all rotations are padded enough
			padding += 1;
		}

		if (cell.x < padding)
		{
			cell.x = padding;
		}
		else if (cell.x + padding > map.Size.x)
		{
			cell.x = map.Size.x - padding;
		}

		if (cell.z < padding)
		{
			cell.z = padding;
		}
		else if (cell.z + padding > map.Size.z)
		{
			cell.z = map.Size.z - padding;
		}

		return cell;
	}

	public static void PlayOneShotOnVehicle<T>(this VehiclePawn vehicle,
		VehicleSoundEventEntry<T> soundEventEntry)
	{
		if (vehicle.Spawned)
		{
			soundEventEntry.value.PlayOneShot(vehicle);
		}
	}

	public static void StartSustainerOnVehicle<T>(this VehiclePawn vehicle,
		VehicleSustainerEventEntry<T> soundEventEntry)
	{
		if (vehicle.Spawned)
		{
			vehicle.sustainers.Spawn(vehicle, soundEventEntry.value);
		}
		else if (vehicle.SustainerTarget is not null)
		{
			vehicle.sustainers.Spawn(vehicle.SustainerTarget, soundEventEntry.value);
		}
	}

	public static void StopSustainerOnVehicle<T>(this VehiclePawn vehicle,
		VehicleSustainerEventEntry<T> soundEventEntry)
	{
		vehicle.sustainers.EndAll(soundEventEntry.value);
	}

	[Pure]
	public static bool DeconstructibleBy(this VehiclePawn vehicle, Faction faction)
	{
		return DebugSettings.godMode || (vehicle.Faction == faction || vehicle.ClaimableBy(faction));
	}

	public static void RefundMaterials(this VehiclePawn vehicle, Map map, DestroyMode mode)
	{
		float multiplier = RefundMaterialCount(vehicle.VehicleDef, mode);
		vehicle.RefundMaterials(map, mode, multiplier: multiplier);
	}

	[Pure]
	public static float RefundMaterialCount(VehicleDef vehicleDef, DestroyMode mode)
	{
		return mode switch
		{
			DestroyMode.Vanish                   => 0,
			DestroyMode.WillReplace              => 0,
			DestroyMode.KillFinalize             => 0.25f,
			DestroyMode.KillFinalizeLeavingsOnly => 0,
			DestroyMode.Deconstruct              => vehicleDef.resourcesFractionWhenDeconstructed,
			DestroyMode.FailConstruction         => 0.5f,
			DestroyMode.Cancel                   => 1,
			DestroyMode.Refund                   => 1,
			DestroyMode.QuestLogic               => 0,
			_                                    => throw new ArgumentException("Unknown destroy mode " + mode),
		};
	}

	public static void RefundMaterials(this VehiclePawn vehicle, Map map, DestroyMode mode,
		float multiplier)
	{
		ThingOwner<Thing> thingOwner = [];
		foreach (ThingDefCountClass thingDefCountClass in
			vehicle.VehicleDef.buildDef.CostListAdjusted(vehicle.Stuff))
		{
			if (thingDefCountClass.thingDef == ThingDefOf.ReinforcedBarrel &&
				!Find.Storyteller.difficulty.classicMortars)
			{
				continue;
			}

			if (mode == DestroyMode.KillFinalize && vehicle.def.killedLeavings != null)
			{
				foreach (ThingDefCountClass killedLeaving in vehicle.def.killedLeavings)
				{
					Thing thing = ThingMaker.MakeThing(killedLeaving.thingDef);
					thing.stackCount = killedLeaving.count;
					thingOwner.TryAdd(thing);
				}
			}

			int refundCount = GenMath.RoundRandom(multiplier * thingDefCountClass.count);
			if (refundCount > 0 && mode == DestroyMode.KillFinalize &&
				thingDefCountClass.thingDef.slagDef != null)
			{
				int count = thingDefCountClass.thingDef.slagDef.smeltProducts
				 .First(sp => sp.thingDef == ThingDefOf.Steel).count;
				int proportionalCount = refundCount / count;
				proportionalCount = Mathf.Min(proportionalCount, vehicle.def.size.Area / 2);
				for (int n = 0; n < proportionalCount; n++)
				{
					thingOwner.TryAdd(ThingMaker.MakeThing(thingDefCountClass.thingDef.slagDef));
				}

				refundCount -= proportionalCount * count;
			}

			if (refundCount > 0)
			{
				Thing thing2 = ThingMaker.MakeThing(thingDefCountClass.thingDef);
				thing2.stackCount = refundCount;
				thingOwner.TryAdd(thing2);
			}
		}

		for (int i = vehicle.inventory.innerContainer.Count - 1; i >= 0; i--)
		{
			Thing thing = vehicle.inventory.innerContainer[i];
			thingOwner.TryAddOrTransfer(thing);
		}

		foreach (ThingComp thingComp in vehicle.AllComps)
		{
			if (thingComp is IRefundable refundable)
			{
				foreach ((ThingDef refundDef, float count) in refundable.Refunds)
				{
					if (refundDef != null)
					{
						Thing thing = ThingMaker.MakeThing(refundDef);
						thing.stackCount = GenMath.RoundRandom(count * multiplier);
						thingOwner.TryAdd(thing);
					}
				}
			}
		}

		TryDropAllOutsideVehicle(thingOwner, map, vehicle.OccupiedRect());
	}

	public static bool TryDropOutsideVehicle(this ThingOwner container, Thing thing, Map map,
		CellRect cellRect, DestroyMode mode = DestroyMode.Refund)
	{
		IntVec3 cell = cellRect.EdgeCells.RandomElement();
		if (mode == DestroyMode.KillFinalize && !map.areaManager.Home[cell])
			thing.SetForbidden(true, warnOnFail: false);

		return container.TryDrop(thing, ThingPlaceMode.Near, thing.stackCount, out _,
			nearPlaceValidator: CanPlaceAt);

		bool CanPlaceAt(IntVec3 canPlaceAtCell)
		{
			if (!canPlaceAtCell.InBounds(map))
				return false;

			return map.thingGrid.ThingAt<VehiclePawn>(canPlaceAtCell) is null &&
				map.pathing.Normal.pathGrid.WalkableFast(canPlaceAtCell);
		}
	}

	public static bool TryDropAllOutsideVehicle(this ThingOwner container, Map map,
		CellRect cellRect, DestroyMode mode = DestroyMode.Refund)
	{
		RotatingList<IntVec3> occupiedCells = cellRect.EdgeCells.InRandomOrder().ToRotatingList();
		while (container.Count > 0)
		{
			IntVec3 cell = occupiedCells.Next;
			if (mode == DestroyMode.KillFinalize && !map.areaManager.Home[cell])
			{
				container[0].SetForbidden(true, warnOnFail: false);
			}

			if (!container.TryDrop(container[0], cell, map, ThingPlaceMode.Near, out _,
				nearPlaceValidator: CanPlaceAt))
			{
				Log.Warning($"Failing to drop all from container {container.Owner}");
				return false;
			}
		}
		return true;

		bool CanPlaceAt(IntVec3 cell)
		{
			if (!cell.InBounds(map))
				return false;
			if (map.thingGrid.ThingAt<VehiclePawn>(cell) != null)
				return false;
			return map.pathing.Normal.pathGrid.WalkableFast(cell);
		}
	}

	[Pure]
	public static bool InAerialVehicle(this Pawn pawn)
	{
		return pawn.GetAerialVehicle() != null;
	}

	/// <summary>
	/// Get AerialVehicle pawn is currently inside
	/// </summary>
	/// <param name="pawn"></param>
	/// <returns><c>null</c> if not currently inside an AerialVehicle</returns>
	[Pure]
	public static AerialVehicleInFlight GetAerialVehicle(this Pawn pawn)
	{
		// may get triggered prematurely from loading save
		if (Find.World.GetComponent<VehicleWorldObjectsHolder>()?.AerialVehicles is null)
			return null;

		foreach (AerialVehicleInFlight aerialVehicle in Find.World.GetComponent<VehicleWorldObjectsHolder>()
		 .AerialVehicles)
		{
			Assert.IsNotNull(aerialVehicle);
			if (aerialVehicle.Vehicle == pawn || aerialVehicle.Vehicle.AllPawnsAboard.Contains(pawn))
				return aerialVehicle;
		}
		return null;
	}

	[MustUseReturnValue]
	public static List<VehicleDef> UniqueVehicleDefs(this IEnumerable<VehiclePawn> vehicles)
	{
		using var cs = GlobalObjectPool.Get(out HashSet<VehicleDef> uniqueVehicleDefs);
		List<VehicleDef> vehicleDefs = [];
		foreach (VehiclePawn vehicle in vehicles)
		{
			if (uniqueVehicleDefs.Add(vehicle.VehicleDef))
			{
				vehicleDefs.Add(vehicle.VehicleDef);
			}
		}
		return vehicleDefs;
	}

	/// <summary>
	/// Get all unique Vehicles in <paramref name="vehicles"/>
	/// </summary>
	[MustUseReturnValue]
	public static List<VehicleDef> UniqueVehicleDefsInList(this List<VehiclePawn> vehicles)
	{
		using var cs = GlobalObjectPool.Get(out HashSet<VehicleDef> uniqueVehicleDefs);
		List<VehicleDef> vehicleDefs = [];
		foreach (VehiclePawn vehicle in vehicles)
		{
			if (uniqueVehicleDefs.Add(vehicle.VehicleDef))
			{
				vehicleDefs.Add(vehicle.VehicleDef);
			}
		}
		return vehicleDefs;
	}

	/// <summary>
	/// Get all unique Vehicles in <paramref name="pawns"/>
	/// </summary>
	[MustUseReturnValue]
	public static List<VehicleDef> UniqueVehicleDefsInList(this List<Pawn> pawns)
	{
		using var cs = GlobalObjectPool.Get(out HashSet<VehicleDef> uniqueVehicleDefs);
		List<VehicleDef> vehicleDefs = [];
		foreach (Pawn pawn in pawns)
		{
			if (pawn is VehiclePawn vehicle && uniqueVehicleDefs.Add(vehicle.VehicleDef))
			{
				vehicleDefs.Add(vehicle.VehicleDef);
			}
		}
		return vehicleDefs;
	}

	/// <summary>
	/// Check if <paramref name="thing"/> is a boat
	/// </summary>
	/// <param name="thing"></param>
	[Pure]
	public static bool IsBoat(this Thing thing)
	{
		return thing is VehiclePawn vehicle && vehicle.VehicleDef.type == VehicleType.Sea;
	}

	/// <summary>
	/// Any Vehicle exists in collection of pawns
	/// </summary>
	/// <param name="pawns"></param>
	[Pure]
	public static bool HasVehicle(this List<Pawn> pawns)
	{
		return pawns.Exists(pawn => pawn is VehiclePawn);
	}

	/// <summary>
	/// Any Boat exists in collection of pawns
	/// </summary>
	/// <param name="pawns"></param>
	[Pure]
	public static bool HasBoat(this List<Pawn> pawns)
	{
		return pawns.Exists(pawn => pawn.IsBoat());
	}

	[Pure]
	public static bool IsFormingVehicleCaravan(this Pawn pawn)
	{
		return pawn.GetLord()?.LordJob is LordJob_FormAndSendVehicles;
	}

	/// <summary>
	/// Check if pawn is in VehicleCaravan
	/// </summary>
	/// <param name="pawn"></param>
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InVehicleCaravan(this Pawn pawn)
	{
		return pawn.GetVehicleCaravan() != null;
	}

	/// <summary>
	/// Get VehicleCaravan pawn is in
	/// </summary>
	/// <param name="pawn"></param>
	/// <returns><c>null</c> if pawn is not currently inside a VehicleCaravan</returns>
	[Pure]
	public static VehicleCaravan GetVehicleCaravan(this Pawn pawn)
	{
		IThingHolder current = pawn.ParentHolder;
		while (current.GetVehicle() is { } vehicle)
		{
			Assert.AreNotEqual(current, vehicle.ParentHolder);
			current = vehicle.ParentHolder;
		}
		return current as VehicleCaravan;
	}

	/// <summary>
	/// Vehicle is able to travel on the coast of <paramref name="tile"/>
	/// </summary>
	/// <param name="vehicleDef"></param>
	/// <param name="tile"></param>
	[Pure]
	public static bool CoastalTravel(this VehicleDef vehicleDef, PlanetTile tile)
	{
		if (vehicleDef.properties.customBiomeCosts.TryGetValue(BiomeDefOf.Ocean,
				out float pathCost) && pathCost < WorldVehiclePathGrid.ImpassableMovementDifficulty)
		{
			WorldGrid worldGrid = Find.WorldGrid;
			List<PlanetTile> neighbors = [];
			worldGrid.GetTileNeighbors(tile, neighbors);

			foreach (int neighborTile in neighbors)
			{
				if (worldGrid[neighborTile].PrimaryBiome == BiomeDefOf.Ocean)
					return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Vehicle can path over cell and cell is in bounds.
	/// </summary>
	[MustUseReturnValue]
	public static bool Drivable(this VehiclePawn vehicle, IntVec3 cell)
	{
		return cell.InBounds(vehicle.Map) && DrivableFast(vehicle, cell);
	}

	/// <summary>
	/// Vehicle can path over cell at ( <paramref name="x"/>, <paramref name="z"/> )
	/// </summary>
	[MustUseReturnValue]
	public static bool DrivableFast(this VehiclePawn vehicle, int x, int z)
	{
		return DrivableFast(vehicle, vehicle.Map.cellIndices.CellToIndex(x, z));
	}

	/// <summary>
	/// <paramref name="vehicle"/> is able to move into <paramref name="cell"/>
	/// </summary>
	/// <param name="vehicle"></param>
	/// <param name="cell"></param>
	[MustUseReturnValue]
	public static bool DrivableFast(this VehiclePawn vehicle, IntVec3 cell)
	{
		int index = vehicle.Map.cellIndices.CellToIndex(cell);
		return DrivableFast(vehicle, index);
	}

	/// <summary>
	/// Vehicle can path over cell at <paramref name="index"/>.
	/// </summary>
	[MustUseReturnValue]
	public static bool DrivableFast(this VehiclePawn vehicle, int index)
	{
		VehiclePawn claimedBy = vehicle.Map.GetDetachedMapComponent<VehiclePositionManager>()
		 .ClaimedBy(vehicle.Map.cellIndices.IndexToCell(index));
		bool passable = (claimedBy is null || claimedBy == vehicle) &&
			vehicle.Map.GetCachedMapComponent<VehiclePathingSystem>()[vehicle.VehicleDef]
			 .VehiclePathGrid.WalkableFast(index);
		return passable;
	}

	/// <summary>
	/// Determine if <paramref name="dest"/> is not large enough to fit <paramref name="vehicle"/>'s full hitbox
	/// </summary>
	[Pure]
	public static bool LocationRestrictedBySize(this VehiclePawn vehicle, Map map, IntVec3 dest,
		Rot8 rot)
	{
		foreach (IntVec3 cell in vehicle.VehicleRect(dest, rot))
		{
			if (!cell.Walkable(vehicle.VehicleDef, map))
				return true;
		}
		return false;
	}

	/// <summary>
	/// NxN rect of smallest dimension of vehicle
	/// </summary>
	/// <remarks>3x5 vehicle returns 3x3 rect, 2x4 returns 2x2, etc.</remarks>
	/// <param name="vehicle"></param>
	/// <param name="cell"></param>
	[Pure]
	public static CellRect MinRect(this VehiclePawn vehicle, IntVec3 cell)
	{
		int minSize = Mathf.Min(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);
		return CellRect.CenteredOn(cell, Mathf.FloorToInt(minSize / 2f));
	}

	/// <summary>
	/// NxN rect of largest dimension of vehicle
	/// </summary>
	/// <remarks>3x5 vehicle returns 3x3 rect, 2x4 returns 2x2, etc.</remarks>
	/// <param name="vehicle"></param>
	/// <param name="cell"></param>
	[Pure]
	public static CellRect MaxRect(this VehiclePawn vehicle, IntVec3 cell)
	{
		int maxSize = Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);
		return CellRect.CenteredOn(cell, Mathf.FloorToInt(maxSize / 2f));
	}

	/// <summary>
	/// Determines if vehicle is able to traverse this cell given its minimum bounds.
	/// </summary>
	/// <remarks>DOES take other vehicles into account</remarks>
	[Pure]
	public static bool DrivableRectOnCell(this VehiclePawn vehicle, IntVec3 cell,
		DestinationHitboxReq hitboxReq = DestinationHitboxReq.MinSize)
	{
		if (hitboxReq == DestinationHitboxReq.MinSize)
			return MinRect(vehicle, cell).Cells.All(vehicle.Drivable);

		bool rectNorth = DrivableRect(vehicle, cell, Rot8.North);
		if (hitboxReq == DestinationHitboxReq.AnyRotation)
			return rectNorth || DrivableRect(vehicle, cell, Rot8.East);

		return rectNorth && DrivableRect(vehicle, cell, Rot8.East);

		static bool DrivableRect(VehiclePawn vehicle, IntVec3 cell, Rot8 rot)
		{
			foreach (IntVec3 rectCell in vehicle.VehicleRect(cell, rot))
			{
				if (!vehicle.Drivable(rectCell))
					return false;
			}
			return true;
		}
	}

	/// <summary>
	/// Determines if vehicle fits on this cell with its minimum bounds
	/// </summary>
	/// <remarks>DOES NOT take other vehicles into account</remarks>
	/// <param name="vehicle"></param>
	/// <param name="cell"></param>
	[Pure]
	public static bool FitsOnCell(this VehiclePawn vehicle, IntVec3 cell)
	{
		int minSize = Mathf.Min(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);
		CellRect cellRect = CellRect.CenteredOn(cell, Mathf.FloorToInt(minSize / 2f));
		return cellRect.Cells.All(cellRectCell =>
			cellRectCell.Walkable(vehicle.VehicleDef, vehicle.Map));
	}

	/// <summary>
	/// Ensures the cellrect inhabited by the vehicle contains no Things that will block pathing and movement.
	/// </summary>
	[Pure]
	public static bool CellRectStandable(this VehiclePawn vehicle, Map map, IntVec3? c = null, Rot4? rot = null)
	{
		IntVec3 position = c ?? vehicle.Position;
		Rot4 facing = rot ?? vehicle.Rotation;
		foreach (IntVec3 cell in vehicle.VehicleDef.VehicleRect(position, facing))
		{
			if (!cell.Standable(vehicle, map))
				return false;
		}
		return true;
	}

	/// <summary>
	/// Ensures the cellrect inhabited by <paramref name="vehicleDef"/> contains no Things that will
	/// block pathing and movement at <paramref name="position"/>.
	/// </summary>
	[Pure]
	public static bool CellRectStandable(this VehicleDef vehicleDef, Map map, IntVec3 position,
		Rot4 rot)
	{
		foreach (IntVec3 cell in vehicleDef.VehicleRect(position, rot))
		{
			if (!cell.Standable(vehicleDef, map))
				return false;
		}
		return true;
	}

	/// <summary>
	/// Determine if <paramref name="cell"/> is able to fit the width of <paramref name="vehicleDef"/>
	/// </summary>
	[Pure]
	[Obsolete("Use VehicleDef->FullRectWalkable extension method instead.", error: true)] // TODO 1.7 - Remove
	public static bool WidthStandable(this VehicleDef vehicleDef, Map map, IntVec3 cell)
	{
		CellRect cellRect = CellRect.CenteredOn(cell, vehicleDef.SizePadding);
		foreach (IntVec3 cellCheck in cellRect)
		{
			if (!cellCheck.Walkable(vehicleDef, map))
				return false;
		}
		return true;
	}

	[Pure]
	public static bool FullRectWalkable(this VehicleDef vehicleDef, VehiclePathingSystem pathing, IntVec3 cell, Rot4 rot)
	{
		VehiclePathingSystem.VehiclePathData pathData = pathing[vehicleDef];
		foreach (IntVec3 hitboxCell in vehicleDef.VehicleRect(cell, rot))
		{
			if (!pathData.VehiclePathGrid.Walkable(hitboxCell))
				return false;
		}
		return true;
	}

	/// <summary>
	/// Seats assigned to vehicle in caravan formation
	/// </summary>
	/// <param name="vehicle"></param>
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CountAssignedToVehicle(this VehiclePawn vehicle)
	{
		return CaravanHelper.assignedSeats.GetAssignments(vehicle).Count;
	}

	/// <summary>
	/// Gets the vehicle that <paramref name="pawn"/> is in.
	/// </summary>
	/// <param name="pawn">Pawn to check</param>
	/// <returns>VehiclePawn <paramref name="pawn"/> is in, or null if they aren't in a vehicle.</returns>
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VehiclePawn GetVehicle(this Pawn pawn)
	{
		return pawn.ParentHolder.GetVehicle();
	}

	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VehiclePawn GetVehicle(this IThingHolder thingHolder)
	{
		return (thingHolder as VehicleRoleHandler)?.vehicle ??
			(thingHolder as Pawn_InventoryTracker)?.pawn as VehiclePawn;
	}

	// TODO 1.7 - Remove
	/// <summary>
	/// Check if <paramref name="pawn"/> is in a vehicle.
	/// </summary>
	/// <param name="pawn">Pawn to check</param>
	/// <returns><see langword="true"/> if <paramref name="pawn"/> is in a vehicle, false otherwise</returns>
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InVehicle(this Pawn pawn)
	{
		return pawn.ParentHolder is VehicleRoleHandler or Pawn_InventoryTracker { pawn: VehiclePawn };
	}

	/// <summary>
	/// Check if <paramref name="thing"/> is in a vehicle.
	/// </summary>
	/// <param name="thing">Thing to check</param>
	/// <returns><see langword="true"/> if <paramref name="thing"/> is in a vehicle, false otherwise</returns>
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InVehicle(this Thing thing)
	{
		return thing.ParentHolder is VehicleRoleHandler or Pawn_InventoryTracker { pawn: VehiclePawn };
	}

	[MustUseReturnValue]
	public static float GetStatValueAbstract(this VehicleDef vehicleDef, VehicleStatDef statDef)
	{
		return statDef.Worker.GetValueAbstract(vehicleDef);
	}

	public enum DestinationHitboxReq
	{
		MinSize,
		AnyRotation,
		AllRotations
	}
}