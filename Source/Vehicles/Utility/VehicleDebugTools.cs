using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;
using RimWorld;

namespace Vehicles
{
    public static class VehicleDebugTools
    {
        [DebugAction("Vehicles", null, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnVehicleRandomized()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (PawnKindDef localKindDef2 in from kd in DefDatabase<PawnKindDef>.AllDefs.Where(v => v.race.thingClass == typeof(VehiclePawn))
			orderby kd.defName
			select kd)
			{
				PawnKindDef localKindDef = localKindDef2;
				list.Add(new DebugMenuOption(localKindDef.defName, DebugMenuOptionMode.Tool, delegate()
				{
					Faction faction = FactionUtility.DefaultFactionFrom(localKindDef.defaultFactionType);
					if (faction is null)
						faction = Faction.OfPlayer;
					VehicleSpawner.SpawnVehicleRandomized(localKindDef, Verse.UI.MouseCell(), Find.CurrentMap, faction);
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

		[DebugAction("Vehicles", null, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void SpawnVehicleWithFaction()
        {
			List<DebugMenuOption> vehicles = new List<DebugMenuOption>();
			List<DebugMenuOption> factions = new List<DebugMenuOption>();
			Faction factionLocal = null;
			foreach (Faction faction in from fd in Find.World.factionManager.GetFactions_NewTemp(true, false, true, TechLevel.Undefined)
					orderby fd.def.defName
					select fd)
			{
				factions.Add(new DebugMenuOption(faction.def.defName, DebugMenuOptionMode.Action, delegate ()
				{
					factionLocal = faction;

					foreach (PawnKindDef localKindDef2 in from kd in DefDatabase<PawnKindDef>.AllDefs.Where(v => v.race.thingClass == typeof(VehiclePawn))
						orderby kd.defName
						select kd)
					{
						PawnKindDef localKindDef = localKindDef2;
						vehicles.Add(new DebugMenuOption(localKindDef.defName, DebugMenuOptionMode.Tool, delegate()
						{
							Faction factionAssigned = FactionUtility.DefaultFactionFrom(localKindDef.defaultFactionType);
							if (factionAssigned is null)
								factionAssigned = Faction.OfPlayer;
							VehicleSpawner.SpawnVehicleRandomized(localKindDef, Verse.UI.MouseCell(), Find.CurrentMap, factionLocal is null ? factionAssigned : factionLocal, Rot4.North, true);
						}));
					}
			
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(vehicles));
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(factions));
        }

		[DebugAction("Vehicles", "Execute Raid with Vehicles", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void ExecuteRaidWithFaction()
		{
			StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
			IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			Func<RaidStrategyDef, bool> func1;
			Func<PawnsArrivalModeDef, bool> func2;
			foreach (Faction localFac2 in Find.FactionManager.AllFactions)
			{
				Faction localFac = localFac2;
				list.Add(new DebugMenuOption(localFac.Name + " (" + localFac.def.defName + ")", DebugMenuOptionMode.Action, delegate()
				{
					parms.faction = localFac;
					List<DebugMenuOption> list2 = new List<DebugMenuOption>();
					foreach (float num in DebugActionsUtility.PointsOptions(true))
					{
						float localPoints = num;
						list2.Add(new DebugMenuOption(num + " points", DebugMenuOptionMode.Action, delegate()
						{
							parms.points = localPoints;
							IEnumerable<RaidStrategyDef> allDefs = DefDatabase<RaidStrategyDef>.AllDefs;
							Func<RaidStrategyDef, bool> predicate = ((RaidStrategyDef s) => s.Worker.CanUseWith(parms, PawnGroupKindDefOf.Combat));
							List<RaidStrategyDef> source = allDefs.Where(predicate).ToList<RaidStrategyDef>();
							Log.Message("Available strategies: " + string.Join(", ", (from s in source
							select s.defName).ToArray<string>()), false);
							parms.raidStrategy = source.RandomElement<RaidStrategyDef>();
							if (parms.raidStrategy != null)
							{
								Log.Message("Strategy: " + parms.raidStrategy.defName, false);
								IEnumerable<PawnsArrivalModeDef> allDefs2 = DefDatabase<PawnsArrivalModeDef>.AllDefs;
								Func<PawnsArrivalModeDef, bool> predicate2;
								//if ((predicate2 = <>9__5) == null)
								//{
								//	predicate2 = (<>9__5 = ((PawnsArrivalModeDef a) => a.Worker.CanUseWith(parms) && parms.raidStrategy.arriveModes.Contains(a)));
								//}
								//List<PawnsArrivalModeDef> source2 = allDefs2.Where(predicate2).ToList<PawnsArrivalModeDef>();
								//Log.Message("Available arrival modes: " + string.Join(", ", (from s in source2
								//select s.defName).ToArray()), false);
								parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;//source2.RandomElement<PawnsArrivalModeDef>();
								Log.Message("Arrival mode: " + parms.raidArrivalMode.defName, false);
							}
							DoRaid(parms);
						}));
					}
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		private static void DoRaid(IncidentParms parms)
		{
			IncidentDef incidentDef;
			if (parms.faction.HostileTo(Faction.OfPlayer))
			{
				incidentDef = IncidentDefOf.RaidEnemy;
			}
			else
			{
				incidentDef = IncidentDefOf.RaidFriendly;
			}
			incidentDef.Worker.TryExecute(parms);
		}
    }
}
