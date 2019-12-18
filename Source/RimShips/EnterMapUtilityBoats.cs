using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimShips.AI;
using SPExtendedLibrary;
using Verse;

namespace RimShips
{
    public static class EnterMapUtilityBoats
    {
        public static void Enter(Caravan caravan, Map map, CaravanEnterMode enterMode, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop,
            bool draftColonists = true, Predicate<IntVec3> extraValidator = null)
        {
            if (enterMode == CaravanEnterMode.None)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Caravan ", caravan, " tried to enter map ", map, " with no enter mode. Defaulting to edge."
                }), false);
                enterMode = CaravanEnterMode.Edge;
            }
            //Ensure pawns are onboard till a fix for dock settling is done
            if (ShipHarmony.HasShip(caravan) && caravan.PawnsListForReading.Any(x => !ShipHarmony.IsShip(x)))
            {
                ShipHarmony.BoardAllCaravanPawns(caravan);
            }
            IntVec3 enterCell = GetWaterCell(caravan, map, CaravanEnterMode.Edge); //Caravan Enter Mode back to enterMode
            Func<Pawn, IntVec3> spawnCellGetter = (Pawn p) => p.ClampToMap(CellFinderExtended.RandomSpawnCellForPawnNear(enterCell, map, 4), map);
            EnterMapUtilityBoats.EnterSpawn(caravan, map, spawnCellGetter, dropInventoryMode, draftColonists);
        }

        public static void EnterSpawn(Caravan caravan, Map map, Func<Pawn, IntVec3> spawnCellGetter, CaravanDropInventoryMode caravanDropInventoryMode = CaravanDropInventoryMode.DoNotDrop, bool draftColonists = true)
        {
            List<Pawn> pawns = new List<Pawn>(caravan.PawnsListForReading).Where(x => ShipHarmony.IsShip(x)).ToList();
            MapExtension mapE = MapExtensionUtility.GetExtensionToMap(map);
            Rot4 spawnDir = GetEdgeToSpawnBoatOn(caravan, map);

            for(int i = 0; i < pawns.Count; i++)
            {
                IntVec3 loc = CellFinderExtended.MiddleEdgeCell(spawnDir, map, pawns[i], (IntVec3 c) => GenGridShips.Standable(c, map, mapE) && !c.Fogged(map)); //Change back to spawnCellGetter later
                
                pawns[i].GetComp<CompShips>().Angle = 0;
                Pawn ship = GenSpawn.Spawn(pawns[i], loc, map, spawnDir.Opposite, WipeMode.Vanish, false) as Pawn;
                ship.drafter.Drafted = draftColonists ? true : false;
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
                if(Find.WorldGrid[map.Tile]?.Rivers?.Any() ?? false)
                {
                    List<Tile.RiverLink> rivers = Find.WorldGrid[map.Tile].Rivers;

                    float angle = Find.WorldGrid.GetHeadingFromTo(map.Tile, (from r1 in rivers
                                                                             orderby -r1.river.degradeThreshold
                                                                             select r1).First<Tile.RiverLink>().neighbor);
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

        private static IntVec3 FindNearEdgeWaterCell(Map map)
        {
            Predicate<IntVec3> validator = (IntVec3 x) => GenGridShips.Standable(x, map, MapExtensionUtility.GetExtensionToMap(map)) && !x.Fogged(map);
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
            MapExtension mapE = MapExtensionUtility.GetExtensionToMap(map);
            Predicate<IntVec3> validator = (IntVec3 x) => GenGridShips.Standable(x, map, mapE) && !x.Fogged(map) && mapE.getShipReachability.CanReachMapEdge(x, tp);
            IntVec3 result;
            if(RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(null /*input validator here*/, map, out result))
            {
                return result; //RECHECK
            }
            Log.Warning("Could not find any valid center cell.", false);
            return CellFinder.RandomCell(map);
        }

        public static void DisembarkShips(List<Pawn> ships)
        {
            //ASSIGN JOBS TO EACH SHIP HERE
        }
    }
}
