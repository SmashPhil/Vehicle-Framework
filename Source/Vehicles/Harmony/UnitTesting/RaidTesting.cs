using RimWorld;
using SmashTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Vehicles
{
	public static class RaidTesting
	{
		[StartupAction(Category = "Raids", Name = "Raid Enemy (Land | 3000)", GameState = GameState.Playing)]
		private static void UnitTest_CaravanFormation()
		{
			Prefs.DevMode = true;
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Faction raidFaction = null;
				foreach (Faction faction in Find.FactionManager.GetFactions(allowHidden: true, allowDefeated: true, minTechLevel: TechLevel.Industrial))
				{
					Log.Message($"Checking: {faction.Name} Def: {faction.def} Hostile: {FactionUtility.HostileTo(faction, Faction.OfPlayer)} DME: {faction.def.GetModExtension<VehicleRaiderDefModExtension>() != null}");
					if (FactionUtility.HostileTo(faction, Faction.OfPlayer) && faction.def.GetModExtension<VehicleRaiderDefModExtension>() != null)
					{
						raidFaction = faction;
						break;
					}
				}


				if (raidFaction == null)
				{
					Log.Error($"Unable to find hostile faction for raid unit test. Aborting...");
					return;
				}

				IncidentParms incidentParms = new IncidentParms();
				incidentParms.target = Find.CurrentMap;
				incidentParms.points = 3000;
				incidentParms.forced = true;
				incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
				incidentParms.faction = raidFaction;
				incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
				IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);
			});
		}
	}
}
