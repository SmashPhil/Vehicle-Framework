using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace RimShips
{
    /*public class IncidentWorker_RaidBoats : IncidentWorker_Raid
    {
        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return false;// base.FactionCanBeGroupSource(f, map, desperate);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!base.TryExecuteWorker(parms))
                return false;
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            Find.StoryWatcher.statsRecord.numRaidsEnemy++;
            return true;
        }

        protected override bool TryResolveRaidFaction(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (parms.faction != null)
                return true;
            float num = parms.points;
            if (num <= 0f)
                num = 999999f;
            return PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroup(num, out parms.faction, (Faction f) => this.FactionCanBeGroupSource(f, map, false), true, true, true, true) 
                || PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroup(num, out parms.faction, (Faction f) => this.FactionCanBeGroupSource(f, map, true), true, true, true, true);
        }

        protected override void ResolveRaidPoints(IncidentParms parms)
        {
            if(parms.points <= 0f)
            {
                Log.Error("RaidEnemy is resolving raid points. They should always be set before initiating the incident.", false);
                parms.points = StorytellerUtility.DefaultThreatPointsNow(parms.target);
            }
        }

        protected override void ResolveRaidStrategy(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            if (parms.raidStrategy != null)
                return;
            Map map = (Map)parms.target;

            if(!(from d in DefDatabase<BoatStrategyDef>.AllDefs
                 where d.Worker.CanUseWith()))
        }
    }*/
}
