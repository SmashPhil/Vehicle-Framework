using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimShips.AI;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

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
            Rot4 coast = Find.World.CoastDirectionAt(map.Tile);
            for(int i = 0; i < pawns.Count; i++)
            {
                IntVec3 loc = CellFinderExtended.MiddleEdgeCell(coast, map, pawns[i], (IntVec3 c) => GenGridShips.Standable(c, map, mapE) && !c.Fogged(map)); //Change back to spawnCellGetter later
                
                pawns[i].GetComp<CompShips>().Angle = 0;
                Pawn ship = GenSpawn.Spawn(pawns[i], loc, map, coast.Opposite, WipeMode.Vanish, false) as Pawn;
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
