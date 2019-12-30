using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;
using RimWorld;
using SPExtended;

namespace RimShips
{
    public abstract class BoatStrategyWorker
    {
        public virtual float SelectionWeight(Map map, float basePoints)
        {
            return this.def.selectionWeightPerPointsCurve.Evaluate(basePoints);
        }

        protected abstract LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed);

        public virtual void MakeLords(IncidentParms parms, List<Pawn> pawns)
        {
            Map map = (Map)parms.target;
            List<List<Pawn>> list = IncidentParmsUtility.SplitIntoGroups(pawns, parms.pawnGroups);
            int @int = Rand.Int;
            for (int i = 0; i < list.Count; i++)
            {
                List<Pawn> list2 = list[i];
                Lord lord = LordMaker.MakeNewLord(parms.faction, this.MakeLordJob(parms, map, list2, @int), map, list2);
                if (DebugViewSettings.drawStealDebug && parms.faction.HostileTo(Faction.OfPlayer))
                {
                    Log.Message(string.Concat(new object[]
                    {
                        "Market value threshold to start stealing (raiders=",
                        lord.ownedPawns.Count,
                        "): ",
                        StealAIUtility.StartStealingMarketValueThreshold(lord),
                        " (colony wealth=",
                        map.wealthWatcher.WealthTotal,
                        ")"
                    }), false);
                }
            }
        }

        public virtual bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            return parms.points >= this.MinimumPoints(parms.faction, groupKind);
        }

        public virtual float MinimumPoints(Faction faction, PawnGroupKindDef groupKind)
        {
            return faction.def.MinPointsToGeneratePawnGroup(groupKind);
        }

        public virtual float MinMaxAllowedPawnGenOptionCost(Faction faction, PawnGroupKindDef groupKind)
        {
            return 0f;
        }

        public virtual bool CanUsePawnGenOption(PawnGenOption g, List<PawnGenOption> chosenGroups)
        {
            return true;
        }

        public virtual bool CanUsePawn(Pawn p, List<Pawn> otherPawns)
        {
            return true;
        }

        public static List<Pawn> GeneratePawnsForBoats(IncidentParms parms, PawnGroupKindDef group)
        {
            PawnGroupMakerParms pawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(group, parms, true);
            return PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms, false).ToList<Pawn>();
        }

        public static List<Pawn> GenerateBoatsForIncident(IncidentParms parms, PawnGroupKindDef group, ref List<Pawn> pawns)
        {
            List<Pawn> ships = new List<Pawn>();
            if(group == PawnGroupKindDefOf.Trader)
            {
                float pointsToSpend = parms.points;
                int colonistsToHouse = pawns.Count;
                List<PawnKindDef> kindDefs = DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race.HasComp(typeof(CompShips)) && parms.faction.def.techLevel >= x.race.GetCompProperties<CompProperties_Ships>().shipTech).ToList();
                kindDefs.OrderByDescending(x => x.combatPower).ThenBy(x => x.race.GetCompProperties<CompProperties_Ships>().shipCategory);
                
                int i = 0;
                kindDefs.RemoveAll(x => x.combatPower > parms.points);
                while(pointsToSpend > 0 && i < kindDefs.Count)
                {
                    PawnKindDef t = kindDefs[i];
                    if(t.race.GetCompProperties<CompProperties_Ships>().shipCategory <= ShipCategory.Trader)
                    {
                        int pawnCount = 0;
                        int pawnCountOperate = 0;
                        foreach(ShipRole r in t.race.GetCompProperties<CompProperties_Ships>().roles)
                        {
                            if(r.handlingType == HandlingTypeFlags.Movement)
                                pawnCountOperate += r.slotsToOperate;
                            pawnCount += r.slots;
                        }

                        if(colonistsToHouse < pawnCountOperate)
                        {
                            i++;
                            continue;
                        }
                        colonistsToHouse -= pawnCount;
                        pointsToSpend -= t.combatPower;
                        Pawn p = PawnGenerator.GeneratePawn(t, parms.faction);
                        ships.Add(p);
                        if(colonistsToHouse <= 0)
                            return ships;
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            return ships;
        }

        public BoatStrategyDef def;
    }
}
