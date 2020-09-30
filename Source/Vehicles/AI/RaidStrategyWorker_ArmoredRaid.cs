using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Verse.AI.Group;

namespace Vehicles
{
    public class RaidStrategyWorker_ArmoredRaid : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            IntVec3 originCell = parms.spawnCenter.IsValid ? parms.spawnCenter : pawns[0].PositionHeld;
			if (parms.faction.HostileTo(Faction.OfPlayer))
			{
				return new LordJob_ArmoredAssault(parms.faction, true, true, false, false, true);
			}
			IntVec3 fallbackLocation;
			RCellFinder.TryFindRandomSpotJustOutsideColony(originCell, map, out fallbackLocation);
			return new LordJob_AssistColony(parms.faction, fallbackLocation); //REDO
        }
    }
}
