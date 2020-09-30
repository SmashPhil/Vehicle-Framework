using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Vehicles.AI;

using Verse;

namespace Vehicles
{
    public static class EnterMapUtilityVehicles
    {
        public static void EnterAndSpawn(Caravan caravan, Map map, CaravanEnterMode enterMode, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop,
            bool draftColonists = false, Predicate<IntVec3> extraValidator = null)
        {
            bool coastalSpawn = caravan.HasBoat();
            if (enterMode == CaravanEnterMode.None)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Caravan ", caravan, " tried to enter map ", map, " with no enter mode. Defaulting to edge."
                }), false);
                enterMode = CaravanEnterMode.Edge;
            }
            List<Pawn> pawns = new List<Pawn>(caravan.PawnsListForReading).ToList();

            IntVec3 enterCell = pawns.AnyNullified(v => v.IsBoat()) ? GetWaterCell(caravan, map, CaravanEnterMode.Edge) : GetEnterCellVehicle(caravan, map, enterMode, extraValidator);
            Rot4 edge = enterMode == CaravanEnterMode.Edge ? CellRect.WholeMap(map).GetClosestEdge(enterCell) : Rot4.North;
            Predicate<IntVec3> validator = (IntVec3 c) => coastalSpawn ? GenGridShips.Standable(c, map) : GenGrid.Standable(c, map);
            Func<Pawn, IntVec3> spawnCellGetter = (Pawn p) => CellFinderExtended.RandomSpawnCellForPawnNear(enterCell, map, p, validator);

            SpawnVehicles(caravan, pawns, map, spawnCellGetter, edge, draftColonists);
        }

        private static void SpawnVehicles(Caravan caravan, List<Pawn> pawns, Map map, Func<Pawn, IntVec3> spawnCellGetter, Rot4 edge, bool draftColonists)
        {
            for(int i = 0; i < pawns.Count; i++)
            {
                IntVec3 loc = SPMultiCell.ClampToMap(pawns[i], spawnCellGetter(pawns[i]), map, 2);
                
                Pawn pawn = (Pawn)GenSpawn.Spawn(pawns[i], loc, map, edge.Opposite, WipeMode.Vanish);
                pawn.drafter.Drafted = draftColonists ? true : false;
                if (pawn is VehiclePawn vehicle)
                {
                    vehicle.Angle = 0;
                }
            }
            caravan.RemoveAllPawns();
            if(caravan.Spawned)
            {
                Find.WorldObjects.Remove(caravan);
            }
        }

        private static IntVec3 GetWaterCell(Caravan caravan, Map map, CaravanEnterMode enterMode, bool landing = false)
        {
            switch(enterMode)
            {
                case CaravanEnterMode.Edge:
                    return FindNearEdgeWaterCell(map);
                case CaravanEnterMode.Center:
                    return FindCenterWaterCell(map, landing);
                default:
                    throw new NotImplementedException("ShipEnterMode");
            }
        }

        private static Rot4 GetEdgeToSpawnBoatOn(Caravan caravan, Map map)
        {
            if (!Find.World.CoastDirectionAt(map.Tile).IsValid)
            {
                if(!Find.WorldGrid[map.Tile]?.Rivers.NullOrEmpty() ?? false)
                {
                    List<Tile.RiverLink> rivers = Find.WorldGrid[map.Tile].Rivers;

                    float angle = Find.WorldGrid.GetHeadingFromTo(map.Tile, (from r1 in rivers
                                                                             orderby -r1.river.degradeThreshold
                                                                             select r1).First().neighbor);
                    if(angle < 45)
                    {
                        return Rot4.South;
                    }
                    else if(angle < 135)
                    {
                        return Rot4.East;
                    }
                    else if(angle < 225)
                    {
                        return Rot4.North;
                    }
                    else if(angle < 315)
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

        private static IntVec3 FindNearEdgeCell(Map map, Predicate<IntVec3> extraCellValidator)
		{
			Predicate<IntVec3> baseValidator = (IntVec3 x) => GenGrid.Standable(x, map) && !x.Fogged(map);
			Faction hostFaction = map.ParentFaction;
			IntVec3 root;
			if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator == null || extraCellValidator(x)) && ((hostFaction != null && map.reachability.CanReachFactionBase(x, hostFaction)) || (hostFaction == null && map.reachability.CanReachBiggestMapEdgeRoom(x))), map, CellFinder.EdgeRoadChance_Neutral, out root))
			{
				return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
			}
			if (extraCellValidator != null && CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && extraCellValidator(x), map, CellFinder.EdgeRoadChance_Neutral, out root))
			{
				return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
			}
			if (CellFinder.TryFindRandomEdgeCellWith(baseValidator, map, CellFinder.EdgeRoadChance_Neutral, out root))
			{
				return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
			}
			Log.Warning("Could not find any valid edge cell.", false);
			return CellFinder.RandomCell(map);
		}

		private static IntVec3 FindCenterCell(Map map, Predicate<IntVec3> extraCellValidator)
		{
			TraverseParms traverseParms = TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false);
			Predicate<IntVec3> baseValidator = (IntVec3 x) => GenGrid.Standable(x, map) && !x.Fogged(map) && map.reachability.CanReachMapEdge(x, traverseParms);
			IntVec3 result;
			if (extraCellValidator != null && RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => baseValidator(x) && extraCellValidator(x), map, out result))
			{
				return result;
			}
			if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(baseValidator, map, out result))
			{
				return result;
			}
			Log.Warning("Could not find any valid cell.", false);
			return CellFinder.RandomCell(map);
		}

        private static IntVec3 FindNearEdgeWaterCell(Map map)
        {
            Predicate<IntVec3> validator = (IntVec3 x) => GenGridShips.Standable(x, map) && !x.Fogged(map);
            Faction hostFaction = map.ParentFaction;
            IntVec3 root;
            if(CellFinder.TryFindRandomEdgeCellWith(validator, map, CellFinder.EdgeRoadChance_Ignore, out root))
            {
                return CellFinderExtended.RandomClosewalkCellNear(root, map, 5, null);
            }
            if(CellFinder.TryFindRandomEdgeCellWith(validator, map, CellFinder.EdgeRoadChance_Ignore, out root))
            {
                return CellFinderExtended.RandomClosewalkCellNear(root, map, 5, null);
            }
            Log.Warning("Could not find any valid edge cell.", false);
            return CellFinder.RandomCell(map);
        }

        private static IntVec3 FindCenterWaterCell(Map map, bool landing = false)
        {
            TraverseParms tp = TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false);
            Predicate<IntVec3> validator = (IntVec3 x) => GenGridShips.Standable(x, map) && !x.Fogged(map) && map.GetCachedMapComponent<WaterMap>().ShipReachability.CanReachMapEdge(x, tp);
            IntVec3 result;
            if(RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(null /*input validator here*/, map, out result))
            {
                return result; //REDO
            }
            Log.Warning("Could not find any valid center cell.", false);
            return CellFinder.RandomCell(map);
        }

        public static IntVec3 GetEnterCellVehicle(Caravan caravan, Map map, CaravanEnterMode enterMode, Predicate<IntVec3> extraCellValidator)
        {
			if (enterMode == CaravanEnterMode.Edge)
			{
				return FindNearEdgeCell(map, caravan, extraCellValidator);
			}
			if (enterMode != CaravanEnterMode.Center)
			{
				throw new NotImplementedException("CaravanEnterMode");
			}
			return FindCenterCell(map, extraCellValidator);
        }

        private static IntVec3 FindNearEdgeCell(Map map, Caravan caravan, Predicate<IntVec3> extraCellValidator)
		{
			Predicate<IntVec3> baseValidator = (IntVec3 x) => GenGrid.Standable(x, map) && !x.Fogged(map);
			Faction hostFaction = map.ParentFaction;
			IntVec3 root;
			if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator == null || extraCellValidator(x)) && ((hostFaction != null && map.reachability.CanReachFactionBase(x, hostFaction)) || (hostFaction == null && map.reachability.CanReachBiggestMapEdgeRoom(x))), map, CellFinder.EdgeRoadChance_Neutral, out root))
			{
				return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
			}
			if (extraCellValidator != null && CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && extraCellValidator(x), map, CellFinder.EdgeRoadChance_Neutral, out root))
			{
				return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
			}
			if (CellFinder.TryFindRandomEdgeCellWith(baseValidator, map, CellFinder.EdgeRoadChance_Neutral, out root))
			{
				return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
			}
			Log.Warning("Could not find any valid edge cell.", false);
			return CellFinder.RandomCell(map);
		}
    }
}
