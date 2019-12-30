using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimShips.AI;
using RimShips.Defs;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using SPExtended;

namespace RimShips
{
    public class IncidentWorker_TraderCaravanBoats : IncidentWorker_NeutralGroup
    {
        protected override PawnGroupKindDef PawnGroupKindDef => PawnGroupKindDefOf.Trader;

        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return base.FactionCanBeGroupSource(f, map, desperate) && f.def.caravanTraderKinds.Any<TraderKindDef>();
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if(!base.CanFireNowSub(parms))
                return false;
            Map map = (Map)parms.target;
            return parms.faction is null || !NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, parms.faction);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            return false; //Disabled for now
            Map map = (Map)parms.target;
            if (!base.TryResolveParms(parms))
                return false;
            if (parms.faction.HostileTo(Faction.OfPlayer))
                return false;
            List<Pawn> list = BoatStrategyWorker.GeneratePawnsForBoats(parms, PawnGroupKindDef);
            if (list.Count <= 0)
                return false;
            foreach (Pawn p in list)
            {
                if (p.needs != null && p.needs.food != null)
                    p.needs.food.CurLevel = p.needs.food.MaxLevel;

            }
            TraderKindDef traderKind = null;
            foreach (Pawn p in list)
            {
                if (p.TraderKind != null)
                {
                    traderKind = p.TraderKind;
                    break;
                }
            }

            List<Pawn> ships = BoatStrategyWorker.GenerateBoatsForIncident(parms, PawnGroupKindDef, ref list);
            if (!ships.Any())
                return false;
            List<IntVec3> usedCells = new List<IntVec3>();
            Predicate<IntVec3> validator = (IntVec3 c) => GenGridShips.Standable(c, map, MapExtensionUtility.GetExtensionToMap(map)) && !c.Fogged(map);
            IntVec3 root = CellFinderExtended.MiddleEdgeCell(Find.World.CoastDirectionAt(map.Tile), map, ships.MaxBy(x => x.def.size.z), validator);
            List<Thing> thingShips = new List<Thing>();
            foreach(Pawn s in ships)
            {
                IntVec3 loc = !usedCells.Any() ? CellFinderExtended.MiddleEdgeCell(Find.World.CoastDirectionAt(map.Tile), map, s, validator) : 
                    CellFinderExtended.RandomEdgeCell(Find.World.CoastDirectionAt(map.Tile), map, validator, usedCells, s);
                usedCells.Add(loc);
                Thing shipSpawned = GenSpawn.Spawn(s, loc, map, WipeMode.Vanish);
                shipSpawned.Rotation = Find.World.CoastDirectionAt(map.Tile).Opposite;
                thingShips.Add(shipSpawned);
            }
            List<Pawn> pawnsLeft = list;
            pawnsLeft.SortBy(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
            foreach(Thing t in thingShips)
            {
                for (int i = 0; i < t.TryGetComp<CompShips>().PawnCountToOperate; i++)
                {
                    if (pawnsLeft.Count <= 0)
                        break;
                    t.TryGetComp<CompShips>().BoardDirectly(pawnsLeft.Pop(), t.TryGetComp<CompShips>().NextAvailableHandler.handlers);
                }
                if (pawnsLeft.Count <= 0)
                    break;
            }

            int iter = 0;
            for(int i = 0; i < pawnsLeft.Count; i++)
            {
                Thing ship = thingShips[iter];
                Pawn p = pawnsLeft.Pop();
                ship.TryGetComp<CompShips>().BoardDirectly(p, ship.TryGetComp<CompShips>().NextAvailableHandler.handlers);
                iter = iter + 1 >= thingShips.Count ? 0 : iter + 1;
            }
            foreach(Thing s in thingShips)
            {
                (s as Pawn).drafter.Drafted = true;

            }

            string label = "LetterLabelTraderCaravanArrival".Translate(parms.faction.Name, traderKind.label).CapitalizeFirst();
            string text = "LetterTraderCaravanArrival".Translate(parms.faction.Name, traderKind.label).CapitalizeFirst();
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(ships, ref label, ref text, "LetterRelatedPawnsNeutralGroup".Translate(Faction.OfPlayer.def.pawnsPlural), true, true);
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, ships[0], parms.faction, null);

            RCellFinder.TryFindRandomSpotJustOutsideColony(ships[0], out IntVec3 chillSpot);
            LordJob_TradeWithColony lordJob = new LordJob_TradeWithColony(parms.faction, chillSpot);
            
            foreach(Pawn s in ships)
            {
                Job job = new Job(JobDefOf_Ships.DisembarkLord, FindShoreline(s));
                s.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                s.GetComp<CompShips>().lordJobToAssign = lordJob;
                s.GetComp<CompShips>().lordShipGroup.AddRange(ships);
            }
            return true;
        }

        protected override void ResolveParmsPoints(IncidentParms parms)
        {
            parms.points = TraderCaravanUtility.GenerateGuardPoints();
        }

        private IntVec3 FindShoreline(Pawn p)
        {
            IntVec3 curCell = p.Position;
            int xSize = p.Map.Size.x - p.Position.x;
            int zSize = p.Map.Size.z - p.Position.z;
            
            IntVec3 nextCell;
            if(p.Rotation == Rot4.North)
            {
            }
            else if(p.Rotation == Rot4.East)
            {
                int alternate = 0;
                int sign = 1;
                while(alternate < p.Map.Size.z / 2)
                {
                    sign *= -1;
                    for(int i = 0; i < xSize; i++)
                    {
                        nextCell = new IntVec3(p.Position.x + i, p.Position.y, p.Position.z + (alternate * sign));
                        
                        TerrainDef terrain = p.Map.terrainGrid.TerrainAt(nextCell);
                        if(!terrain.IsWater && nextCell.Standable(p.Map))
                            return curCell;
                        curCell = nextCell;
                        if(!curCell.InBoundsShip(p.Map) || !GenGridShips.Standable(curCell, p.Map, MapExtensionUtility.GetExtensionToMap(p.Map)))
                            break;
                    }
                    if(sign > 0)
                        alternate++;
                }
            }
            else if(p.Rotation == Rot4.South)
            {
            }
            else if(p.Rotation == Rot4.West)
            {
            }

            Log.Error("Unable to find location to disembark " + p.LabelShort);
            return IntVec3.Invalid;
        }
    }
}
