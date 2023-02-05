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
				return FindNearEdgeCell(map, caravan, extraCellValidator);
			}
			if (enterMode != CaravanEnterMode.Center)
			{
				throw new NotImplementedException("CaravanEnterMode");
			}
			return FindCenterCell(map, caravan.LeadVehicle.VehicleDef, extraCellValidator);
		}

		private static IntVec3 FindNearEdgeCell(Map map, VehicleCaravan caravan, Predicate<IntVec3> extraCellValidator)
		{
			bool baseValidator(IntVec3 x) => caravan.AllVehicles().TrueForAll(v => GenGridVehicles.Standable(x, v, map)) && !x.Fogged(map);
			Faction hostFaction = map.ParentFaction;
			IntVec3 root;
			if (caravan.HasBoat())
			{
				if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator == null || extraCellValidator(x)) &&
					((hostFaction != null && map.reachability.CanReachFactionBase(x, hostFaction)) ||
					(hostFaction == null && map.reachability.CanReachBiggestMapEdgeDistrict(x))), map, CalculateEdgeToSpawnBoatOn(map), CellFinder.EdgeRoadChance_Ignore, out root))
				{
					return CellFinderExtended.RandomClosewalkCellNear(root, map, caravan.LeadVehicle.VehicleDef, 5);
				}
				if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator is null || extraCellValidator(x)), map, CellFinder.EdgeRoadChance_Always, out root))
				{
					return CellFinderExtended.RandomClosewalkCellNear(root, map, caravan.LeadVehicle.VehicleDef, 5);
				}
			}
			else
			{
				if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator == null || extraCellValidator(x)) &&
					((hostFaction != null && map.reachability.CanReachFactionBase(x, hostFaction)) ||
					(hostFaction == null && map.reachability.CanReachBiggestMapEdgeDistrict(x))), map, CellFinder.EdgeRoadChance_Always, out root))
				{
					return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
				}
				if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator is null || extraCellValidator(x)), map, CellFinder.EdgeRoadChance_Always, out root))
				{
					return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
				}
			}
			Log.Warning("Could not find any valid edge cell.");
			return CellFinder.RandomCell(map);
		}
	}
}
