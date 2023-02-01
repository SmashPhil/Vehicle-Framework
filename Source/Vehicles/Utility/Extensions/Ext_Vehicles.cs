using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class Ext_Vehicles
	{
		public static CellRect VehicleRect(this VehiclePawn vehicle, bool maxSizePossible = false)
		{
			return vehicle.VehicleRect(vehicle.Position, vehicle.Rotation, maxSizePossible: maxSizePossible);
		}

		public static CellRect VehicleRect(this VehiclePawn vehicle, IntVec3 center, Rot4 rot, bool maxSizePossible = false)
		{
			return VehicleRect(vehicle.VehicleDef, center, rot, maxSizePossible: maxSizePossible);
		}

		public static CellRect VehicleRect(this VehicleDef vehicleDef, IntVec3 center, Rot4 rot, bool maxSizePossible = false)
		{
			if (rot == Rot4.West) rot = Rot4.East;
			if (rot == Rot4.South) rot = Rot4.North;
			IntVec2 size = vehicleDef.size;
			if (maxSizePossible)
			{
				int maxSize = Mathf.Max(size.x, size.z);
				size.x = maxSize;
				size.z = maxSize;
			}
			return GenAdj.OccupiedRect(center, rot, size);
		}

		[DebugAction(VehicleHarmony.VehiclesLabel, "Test Vehicle Rect", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void TestMethod()
		{
			VehiclePawn firstVehicle = Find.CurrentMap.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn => pawn is VehiclePawn) as VehiclePawn;
			var cells = FullVehicleRectAllDirections(firstVehicle.VehicleDef, firstVehicle.Position);
			Log.Message($"COUNT: {cells.Count()}");
			foreach (IntVec3 cell in cells)
			{
				firstVehicle.Map.debugDrawer.FlashCell(cell, 0.75f, duration: 180);
			}
		}

		//TODO - Doesn't work for even sizes. Should instead calculate only North and East (north calculate full OccupiedRect, east only calculate past the inner square)
		public static IEnumerable<IntVec3> FullVehicleRectAllDirections(this VehicleDef vehicleDef, IntVec3 center)
		{
			//If size is square, rotation doesn't matter
			IntVec2 size = vehicleDef.size;
			if (size.x == size.z)
			{
				foreach (IntVec3 cell in GenAdj.OccupiedRect(center, Rot4.North, size))
				{
					yield return cell;
				}
				yield break;
			}
			GenAdj.AdjustForRotation(ref center, ref size, Rot4.North);

			int minSize = Mathf.Min(size.x, size.z);
			int maxSize = Mathf.Max(size.x, size.z);

			//Fetch inner square
			foreach (IntVec3 cell in GenAdj.OccupiedRect(center, Rot4.North, new IntVec2(minSize, minSize)))
			{
				yield return cell;
			}

			int cutoutSize = Mathf.CeilToInt((maxSize - minSize) / 2f);
			int distanceToEdge = Mathf.FloorToInt((maxSize - minSize) / 2f);
			IntVec2 subSize = new IntVec2(minSize, cutoutSize);
			int startingDist = distanceToEdge + cutoutSize;

			//North
			IntVec3 subCenter = center + new IntVec3(0, 0, startingDist);
			foreach (IntVec3 cell in GenAdj.OccupiedRect(subCenter, Rot4.North, subSize))
			{
				yield return cell;
			}

			//East
			subCenter = center + new IntVec3(startingDist, 0, 0);
			foreach (IntVec3 cell in GenAdj.OccupiedRect(subCenter, Rot4.East, subSize))
			{
				yield return cell;
			}

			//South
			subCenter = center + new IntVec3(0, 0, -startingDist);
			foreach (IntVec3 cell in GenAdj.OccupiedRect(subCenter, Rot4.South, subSize))
			{
				yield return cell;
			}

			//West
			subCenter = center + new IntVec3(-startingDist, 0, 0);
			foreach (IntVec3 cell in GenAdj.OccupiedRect(subCenter, Rot4.West, subSize))
			{
				yield return cell;
			}
		}

		public static void RegenerateEvents(this VehiclePawn vehicle)
		{
			vehicle.EventRegistry?.Clear();
			vehicle.RegisterEvents();
		}

		public static void RegisterEvents(this VehiclePawn vehicle)
		{
			if (vehicle.EventRegistry.NullOrEmpty())
			{
				vehicle.FillEvents_Def();

				//Pather
				vehicle.AddEvent(VehicleEventDefOf.PawnExited, vehicle.vPather.RecalculatePermissions);
				vehicle.AddEvent(VehicleEventDefOf.PawnChangedSeats, vehicle.vPather.RecalculatePermissions);
				vehicle.AddEvent(VehicleEventDefOf.PawnKilled, vehicle.vPather.RecalculatePermissions);
				vehicle.AddEvent(VehicleEventDefOf.PawnCapacitiesDirty, vehicle.vPather.RecalculatePermissions);
				vehicle.AddEvent(VehicleEventDefOf.DraftOff, vehicle.vPather.RecalculatePermissions);
				vehicle.AddEvent(VehicleEventDefOf.DamageTaken, vehicle.vPather.RecalculatePermissions);
				vehicle.AddEvent(VehicleEventDefOf.Repaired, vehicle.vPather.RecalculatePermissions);

				if (!vehicle.VehicleDef.statEvents.NullOrEmpty())
				{
					foreach (StatCache.EventLister eventLister in vehicle.VehicleDef.statEvents)
					{
						foreach (VehicleEventDef eventDef in eventLister.eventDefs)
						{
							vehicle.AddEvent(eventDef, () => vehicle.statHandler.MarkStatDirty(eventLister.statDef));
						}
					}
				}

				//One Shots
				if (!vehicle.VehicleDef.soundOneShotsOnEvent.NullOrEmpty())
				{
					foreach ((VehicleEventDef eventDef, SoundDef soundDef) in vehicle.VehicleDef.soundOneShotsOnEvent)
					{
						vehicle.AddEvent(eventDef, delegate ()
						{
							if (vehicle.Spawned)
							{
								soundDef.PlayOneShot(vehicle);
							}
						});
					}
				}
				//Sustainers
				if (!vehicle.VehicleDef.soundSustainersOnEvent.NullOrEmpty())
				{
					foreach ((Pair<VehicleEventDef, VehicleEventDef> eventStartStop, SoundDef soundDef) in vehicle.VehicleDef.soundSustainersOnEvent)
					{
						vehicle.AddEvent(eventStartStop.First, delegate ()
						{
							if (vehicle.Spawned)
							{
								vehicle.sustainers.Spawn(soundDef, MaintenanceType.PerTick);
							}
						});
						vehicle.AddEvent(eventStartStop.Second, delegate ()
						{
							vehicle.sustainers.EndAll(soundDef);
						});
					}
				}
			}
		}

		public static bool DeconstructibleBy(this VehiclePawn vehicle, Faction faction)
		{
			return DebugSettings.godMode || (vehicle.Faction == faction || vehicle.ClaimableBy(faction));
		}

		public static void RefundMaterials(this VehiclePawn vehicle, Map map, DestroyMode mode, List<Thing> listOfLeavingsOut = null)
		{
			switch (mode)
			{
				case DestroyMode.KillFinalize:
					vehicle.RefundMaterials(map, mode, multiplier: 0.25f);
					break;
				case DestroyMode.Deconstruct:
					vehicle.RefundMaterials(map, mode, multiplier: vehicle.VehicleDef.resourcesFractionWhenDeconstructed);
					break;
				case DestroyMode.Cancel:
				case DestroyMode.Refund:
					vehicle.RefundMaterials(map, mode, 1);
					break;
				default:
					GenLeaving.DoLeavingsFor(vehicle, map, mode, vehicle.OccupiedRect(), null, listOfLeavingsOut);
					break;
			}
		}

		public static void RefundMaterials(this VehiclePawn vehicle, Map map, DestroyMode mode, float multiplier, Predicate<IntVec3> nearPlaceValidator = null)
		{
			List<ThingDefCountClass> thingDefs = vehicle.VehicleDef.buildDef.CostListAdjusted(vehicle.Stuff);
			ThingOwner<Thing> thingOwner = new ThingOwner<Thing>();
			foreach (ThingDefCountClass thingDefCountClass in thingDefs)
			{
				if (thingDefCountClass.thingDef == ThingDefOf.ReinforcedBarrel && !Find.Storyteller.difficulty.classicMortars)
				{
					continue;
				}

				if (mode == DestroyMode.KillFinalize && vehicle.def.killedLeavings != null)
				{
					for (int k = 0; k < vehicle.def.killedLeavings.Count; k++)
					{
						Thing thing = ThingMaker.MakeThing(vehicle.def.killedLeavings[k].thingDef, null);
						thing.stackCount = vehicle.def.killedLeavings[k].count;
						thingOwner.TryAdd(thing, true);
					}
				}

				int refundCount = GenMath.RoundRandom(multiplier * thingDefCountClass.count);
				if (refundCount > 0 && mode == DestroyMode.KillFinalize && thingDefCountClass.thingDef.slagDef != null)
				{
					int count = thingDefCountClass.thingDef.slagDef.smeltProducts.First((ThingDefCountClass pro) => pro.thingDef == ThingDefOf.Steel).count;
					int proportionalCount = refundCount / count;
					proportionalCount = Mathf.Min(proportionalCount, vehicle.def.size.Area / 2);
					for (int n = 0; n < proportionalCount; n++)
					{
						thingOwner.TryAdd(ThingMaker.MakeThing(thingDefCountClass.thingDef.slagDef, null), true);
					}
					refundCount -= proportionalCount * count;
				}
				if (refundCount > 0)
				{
					Thing thing2 = ThingMaker.MakeThing(thingDefCountClass.thingDef);
					thing2.stackCount = refundCount;
					thingOwner.TryAdd(thing2, true);
				}
			}
			for (int i = vehicle.inventory.innerContainer.Count - 1; i >= 0; i--)
			{
				Thing thing = vehicle.inventory.innerContainer[i];
				thingOwner.TryAddOrTransfer(thing);
			}
			foreach (IRefundable refundable in vehicle.AllComps.Where(comp => comp is IRefundable))
			{
				foreach ((ThingDef refundDef, float count) in refundable.Refunds)
				{
					if (refundDef != null)
					{
						int countRounded = GenMath.RoundRandom(count);
						if (countRounded > 0)
						{
							Thing thing2 = ThingMaker.MakeThing(refundDef);
							thing2.stackCount = countRounded;
							thingOwner.TryAdd(thing2);
						}
					}
				}
			}
			RotatingList<IntVec3> occupiedCells = vehicle.OccupiedRect().Cells.InRandomOrder(null).ToRotatingList();
			while (thingOwner.Count > 0)
			{
				IntVec3 cell = occupiedCells.Next;
				if (mode == DestroyMode.KillFinalize && !map.areaManager.Home[cell])
				{
					thingOwner[0].SetForbidden(true, false);
				}
				if (!thingOwner.TryDrop(thingOwner[0], cell, map, ThingPlaceMode.Near, out _, null, nearPlaceValidator))
				{
					Log.Warning($"Failing to place all leavings for destroyed vehicle {vehicle} at {vehicle.OccupiedRect().CenterCell}");
					return;
				}
			}
		}

		/// <summary>
		/// Get AerialVehicle pawn is currently inside
		/// </summary>
		/// <param name="pawn"></param>
		/// <returns><c>null</c> if not currently inside an AerialVehicle</returns>
		public static AerialVehicleInFlight GetAerialVehicle(this Pawn pawn)
		{
			foreach (AerialVehicleInFlight aerialVehicle in VehicleWorldObjectsHolder.Instance.AerialVehicles)
			{
				if (aerialVehicle.vehicle == pawn || aerialVehicle.vehicle.AllPawnsAboard.Contains(pawn))
				{
					return aerialVehicle;
				}
			}
			return null;
		}

		/// <summary>
		/// Get all unique Vehicles in <paramref name="vehicles"/>
		/// </summary>
		/// <param name="vehicles"></param>
		public static List<VehicleDef> UniqueVehicleDefsInList(this IEnumerable<VehiclePawn> vehicles)
		{
			return vehicles.Select(v => v.VehicleDef).Distinct().ToList();
		}

		/// <summary>
		/// Get all unique Vehicles in <paramref name="vehicles"/>
		/// </summary>
		/// <param name="vehicles"></param>
		public static List<VehicleDef> UniqueVehicleDefsInList(this IEnumerable<Pawn> pawns)
		{
			return pawns.Where(pawn => pawn is VehiclePawn).Select(pawn => (pawn as VehiclePawn).VehicleDef).Distinct().ToList();
		}

		/// <summary>
		/// Check if <paramref name="thing"/> is a boat
		/// </summary>
		/// <param name="thing"></param>
		public static bool IsBoat(this Thing thing)
		{
			return thing is VehiclePawn vehicle && vehicle.VehicleDef.vehicleType == VehicleType.Sea;
		}

		/// <summary>
		/// Check if <paramref name="thingDef"/> is a boat
		/// </summary>
		/// <param name="thingDef"></param>
		/// <returns></returns>
		public static bool IsBoat(this ThingDef thingDef)
		{
			return thingDef is VehicleDef vehicleDef && vehicleDef.vehicleType == VehicleType.Sea;
		}

		/// <summary>
		/// Any Vehicle exists in collection of pawns
		/// </summary>
		/// <param name="pawns"></param>
		public static bool HasVehicle(this IEnumerable<Pawn> pawns)
		{
			return pawns.NotNullAndAny(x => x is VehiclePawn);
		}

		/// <summary>
		/// Any Boat exists in collection of pawns
		/// </summary>
		/// <param name="pawns"></param>
		/// <returns></returns>
		public static bool HasBoat(this IEnumerable<Pawn> pawns)
		{
			return pawns?.NotNullAndAny(x => IsBoat(x)) ?? false;
		}

		/// <summary>
		/// Caravan contains one or more Vehicles
		/// </summary>
		/// <param name="pawn"></param>
		public static bool HasVehicleInCaravan(this Pawn pawn)
		{
			return pawn.IsFormingCaravan() && pawn.GetLord().LordJob is LordJob_FormAndSendVehicles && pawn.GetLord().ownedPawns.NotNullAndAny(p => p is VehiclePawn);
		}

		/// <summary>
		/// Check if pawn is in VehicleCaravan
		/// </summary>
		/// <param name="pawn"></param>
		public static bool InVehicleCaravan(this Pawn pawn)
		{
			return pawn.GetVehicleCaravan() != null;
		}

		/// <summary>
		/// Get VehicleCaravan pawn is in
		/// </summary>
		/// <param name="pawn"></param>
		/// <returns><c>null</c> if pawn is not currently inside a VehicleCaravan</returns>
		public static VehicleCaravan GetVehicleCaravan(this Pawn pawn)
		{
			return pawn.ParentHolder as VehicleCaravan;
		}

		//REDO
		/// <summary>
		/// Vehicle speed should be reduced temporarily
		/// </summary>
		/// <param name="vehicle"></param>
		public static bool SlowSpeed(this VehiclePawn vehicle)
		{
			var lord = vehicle.GetLord();
			if (lord is null)
			{
				return false;
			}
			return false;// vehicleLordCategories.Contains(lord.LordJob.GetType());
		}

		/// <summary>
		/// Vehicle is able to travel on the coast of <paramref name="tile"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		/// <param name="tile"></param>
		public static bool CoastalTravel(this VehicleDef vehicleDef, int tile)
		{
			if (vehicleDef.properties.customBiomeCosts.TryGetValue(BiomeDefOf.Ocean, out float pathCost) && pathCost < WorldVehiclePathGrid.ImpassableMovementDifficulty)
			{
				WorldGrid worldGrid = Find.WorldGrid;
				List<int> neighbors = new List<int>();
				worldGrid.GetTileNeighbors(tile, neighbors);

				foreach (int neighborTile in neighbors)
				{
					if (worldGrid[neighborTile].biome == BiomeDefOf.Ocean) return true;
				}
			}
			return false;
				
		}

		/// <see cref="DrivableFast(VehiclePawn, IntVec3)"/>
		public static bool Drivable(this VehiclePawn vehicle, IntVec3 cell)
		{
			return cell.InBounds(vehicle.Map) && DrivableFast(vehicle, cell);
		}
		
		/// <see cref="DrivableFast(VehiclePawn, IntVec3)"/>
		public static bool DrivableFast(this VehiclePawn vehicle, int index)
		{
			IntVec3 cell = vehicle.Map.cellIndices.IndexToCell(index);
			return DrivableFast(vehicle, cell);
		}

		/// <see cref="DrivableFast(VehiclePawn, IntVec3)"/>
		public static bool DrivableFast(this VehiclePawn vehicle, int x, int z)
		{
			IntVec3 cell = new IntVec3(x, 0, z);
			return DrivableFast(vehicle, cell);
		}

		/// <summary>
		/// <paramref name="vehicle"/> is able to move into <paramref name="cell"/>
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="cell"></param>
		public static bool DrivableFast(this VehiclePawn vehicle, IntVec3 cell)
		{
			VehiclePawn claimedBy = vehicle.Map.GetCachedMapComponent<VehiclePositionManager>().ClaimedBy(cell);
			bool passable = (claimedBy is null || claimedBy == vehicle) &&
				vehicle.Map.GetCachedMapComponent<VehicleMapping>()[vehicle.VehicleDef].VehiclePathGrid.WalkableFast(cell);
			return passable;
		}

		/// <summary>
		/// Determine if <paramref name="dest"/> is not large enough to fit <paramref name="vehicle"/>'s size
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="dest"></param>
		public static bool LocationRestrictedBySize(this VehiclePawn vehicle, IntVec3 dest, Rot8 rot)
		{
			foreach (IntVec3 cell in vehicle.VehicleRect(dest, rot))
			{
				if (!cell.Walkable(vehicle.VehicleDef, vehicle.Map))
				{
					return true;
				}
			}
			return false;
		}

		public static bool FitsOnCell(this VehiclePawn vehicle, IntVec3 cell)
		{
			IntVec2 size = vehicle.VehicleDef.size;
			bool verticalBlocked = vehicle.LocationRestrictedBySize(cell, Rot8.North);
			bool horizontalBlocked = vehicle.LocationRestrictedBySize(cell, Rot8.East);
			return !verticalBlocked || !horizontalBlocked;
		}

		/// <summary>
		/// Ensures the cellrect inhabited by the vehicle contains no Things that will block pathing and movement.
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="c"></param>
		public static bool CellRectStandable(this VehiclePawn vehicle, Map map, IntVec3? c = null, Rot4? rot = null)
		{
			IntVec3 loc = c ?? vehicle.Position;
			Rot4 facing = rot ?? vehicle.Rotation;
			return vehicle.VehicleDef.CellRectStandable(map, loc, facing);
		}

		/// <summary>
		/// Ensures the cellrect inhabited by <paramref name="vehicleDef"/> contains no Things that will block pathing and movement at <paramref name="cell"/>.
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="c"></param>
		public static bool CellRectStandable(this VehicleDef vehicleDef, Map map, IntVec3 position, Rot4 rot)
		{
			foreach (IntVec3 cell in vehicleDef.VehicleRect(position, rot))
			{
				if (!GenGridVehicles.Standable(cell, vehicleDef, map))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Determine if <paramref name="cell"/> is able to fit the width of <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		/// <param name="cell"></param>
		/// <param name="dir"></param>
		public static bool WidthStandable(this VehicleDef vehicleDef, Map map, IntVec3 cell)
		{
			CellRect cellRect = CellRect.CenteredOn(cell, Mathf.Min(vehicleDef.size.x, vehicleDef.size.z) / 2);
			foreach (IntVec3 cellCheck in cellRect)
			{
				if (!cellCheck.InBounds(map) || GenGridVehicles.Impassable(cellCheck, map, vehicleDef))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Seats assigned to vehicle in caravan formation
		/// </summary>
		/// <param name="vehicle"></param>
		public static int CountAssignedToVehicle(this VehiclePawn vehicle)
		{
			return CaravanHelper.assignedSeats.Where(a => a.Value.vehicle == vehicle).Select(s => s.Key).Count();
		}

		/// <summary>
		/// Gets the vehicle that <paramref name="pawn"/> is in.
		/// </summary>
		/// <param name="pawn">Pawn to check</param>
		/// <returns>VehiclePawn <paramref name="pawn"/> is in, or null if they aren't in a vehicle.</returns>
        public static VehiclePawn GetVehicle(this Pawn pawn)
        {
            return (pawn.ParentHolder as VehicleHandler)?.vehicle;
        }

		/// <summary>
		/// Returns true if <paramref name="pawn"/> is in a vehicle.
		/// </summary>
		/// <param name="pawn">Pawn to check</param>
		/// <returns>true if <paramref name="pawn"/> is in a vehicle, false otherwise</returns>
        public static bool IsInVehicle(this Pawn pawn)
        {
            return pawn.ParentHolder is VehicleHandler;
        }
	}
}
