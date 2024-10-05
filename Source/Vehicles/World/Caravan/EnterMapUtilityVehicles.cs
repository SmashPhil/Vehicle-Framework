using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using SmashTools;

namespace Vehicles
{
	public static class EnterMapUtilityVehicles
	{
		public static void EnterAndSpawn(VehicleCaravan caravan, Map map, CaravanEnterMode enterMode, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop,
			bool draftColonists = false, Predicate<IntVec3> extraValidator = null)
		{
			bool coastalSpawn = caravan.HasBoat();
			if (enterMode == CaravanEnterMode.None)
			{
				Log.Error($"VehicleCaravan {caravan} tried to enter map {map} with no enter mode. Defaulting to edge.");
				enterMode = CaravanEnterMode.Edge;
			}
			IntVec3 enterCell = GetEnterCellVehicle(caravan, map, enterMode, extraValidator);
			Rot4 edge = enterMode == CaravanEnterMode.Edge ? CellRect.WholeMap(map).GetClosestEdge(enterCell) : Rot4.North;
			Func<Pawn, IntVec3> spawnCellGetter = (Pawn pawn) => CellFinderExtended.RandomSpawnCellForPawnNear(enterCell, map, pawn, 
				(IntVec3 c) => GenGridVehicles.StandableUnknown(c, pawn, map), coastalSpawn);
			SpawnVehicles(caravan, caravan.PawnsListForReading.Where(p => !p.IsInVehicle()).ToList(), map, spawnCellGetter, edge, draftColonists);
		}

		private static void SpawnVehicles(VehicleCaravan caravan, List<Pawn> pawns, Map map, Func<Pawn, IntVec3> spawnCellGetter, Rot4 edge, bool draftColonists)
		{
			for (int i = 0; i < pawns.Count; i++)
			{
				IntVec3 loc = pawns[i].ClampToMap(spawnCellGetter(pawns[i]), map, 2);
				Pawn pawn = (Pawn)GenSpawn.Spawn(pawns[i], loc, map, edge.Opposite, WipeMode.Vanish);
				
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
			caravan.RemoveAllPawns();
			if (caravan.Spawned)
			{
				Find.WorldObjects.Remove(caravan);
			}
		}

		private static Rot4 CalculateEdgeToSpawnBoatOn(Map map)
		{
			if (!Find.World.CoastDirectionAt(map.Tile).IsValid)
			{
				if(!Find.WorldGrid[map.Tile]?.Rivers.NullOrEmpty() ?? false)
				{
					List<Tile.RiverLink> rivers = Find.WorldGrid[map.Tile].Rivers;

					float angle = Find.WorldGrid.GetHeadingFromTo(map.Tile, (from r1 in rivers
																			 orderby -r1.river.degradeThreshold
																			 select r1).First().neighbor);
					if (angle < 45)
					{
						return Rot4.South;
					}
					else if (angle < 135)
					{
						return Rot4.East;
					}
					else if (angle < 225)
					{
						return Rot4.North;
					}
					else if (angle < 315)
					{
						return Rot4.West;
					}
					else
					{
						return Rot4.South;
					}
				}
			}
			return Find.World.CoastDirectionAt(map.Tile);
		}

		private static IntVec3 FindCenterCell(Map map, VehicleDef vehicleDef, Predicate<IntVec3> extraCellValidator)
		{
			TraverseParms traverseParms = TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false);
			Predicate<IntVec3> baseValidator = (IntVec3 x) => GenGridVehicles.Standable(x, vehicleDef, map) && !x.Fogged(map) && map.reachability.CanReachMapEdge(x, traverseParms);
			IntVec3 result;
			if (extraCellValidator != null && RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => baseValidator(x) && extraCellValidator(x), map, out result))
			{
				return result;
			}
			if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(baseValidator, map, out result))
			{
				return result;
			}
			Log.Warning("Could not find any valid cell.");
			return CellFinder.RandomCell(map);
		}

		public static IntVec3 GetEnterCellVehicle(VehicleCaravan caravan, Map map, CaravanEnterMode enterMode, Predicate<IntVec3> extraCellValidator)
		{
			if (enterMode == CaravanEnterMode.Edge)
			{
				return FindNearEdgeCell(map, caravan.LeadVehicle.VehicleDef, caravan.Faction, extraCellValidator);
			}
			if (enterMode != CaravanEnterMode.Center)
			{
				throw new NotImplementedException("CaravanEnterMode");
			}
			return FindCenterCell(map, caravan.LeadVehicle.VehicleDef, extraCellValidator);
		}

		private static IntVec3 FindNearEdgeCell(Map map, VehicleDef vehicleDef, Faction faction, Predicate<IntVec3> extraCellValidator)
		{
			IntVec3 root;
			Rot4 rot = Rot4.Random;
			if (vehicleDef.vehicleType == VehicleType.Sea)
			{
				rot = CalculateEdgeToSpawnBoatOn(map);
			}
			RoadPreference preference = RoadPreferenceFor(faction);
			while (preference > RoadPreference.Invalid)
			{
				if (TryFindCellWithBestPreference(preference, out root))
				{
					return root;
				}
				preference--;
			}
			Log.Warning("Could not find any valid edge cell.");
			return CellFinder.RandomCell(map);

			bool TryFindCellWithBestPreference(RoadPreference preference, out IntVec3 root)
			{
				root = IntVec3.Invalid;
				if (TryFindNearEdgeCell(map, vehicleDef, rot, preference, extraCellValidator, out root)) return true;
				if (TryFindNearEdgeCell(map, vehicleDef, rot.Opposite, preference, extraCellValidator, out root)) return true;
				if (TryFindNearEdgeCell(map, vehicleDef, rot.Rotated(RotationDirection.Clockwise), preference, extraCellValidator, out root)) return true;
				if (TryFindNearEdgeCell(map, vehicleDef, rot.Rotated(RotationDirection.Counterclockwise), preference, extraCellValidator, out root)) return true;
				return false;
			}
		}

		private static bool TryFindNearEdgeCell(Map map, VehicleDef vehicleDef, Rot4 rot, RoadPreference roadPref, Predicate < IntVec3> extraCellValidator, out IntVec3 root)
		{
			Faction hostFaction = map.ParentFaction;
			if (CellFinderExtended.TryFindRandomEdgeCellWith((IntVec3 cell) => Validator(cell) &&
					(extraCellValidator == null || extraCellValidator(cell)) &&
					((hostFaction != null && map.reachability.CanReachFactionBase(cell, hostFaction)) ||
					(hostFaction == null && map.reachability.CanReachBiggestMapEdgeDistrict(cell))) &&
					AllowsPreference(map, cell, roadPref),
					map, rot, vehicleDef, CellFinder.EdgeRoadChance_Always, out root))
			{
				return true;
			}
			if (CellFinderExtended.TryFindRandomEdgeCellWith((IntVec3 x) => Validator(x) &&
				(extraCellValidator is null || extraCellValidator(x)),
				map, rot, vehicleDef, CellFinder.EdgeRoadChance_Always, out root))
			{
				root = CellFinderExtended.RandomClosewalkCellNear(root, map, vehicleDef, 5);
				return true;
			}
			return false;

			bool Validator(IntVec3 cell) => GenGridVehicles.Standable(cell, vehicleDef, map) && !cell.Fogged(map);
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

		private enum RoadPreference : int
		{
			Invalid,
			None,
			NoAvoidal,
			Prioritize,
		}
	}
}
