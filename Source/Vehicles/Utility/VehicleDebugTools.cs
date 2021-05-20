using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.AI;
using Vehicles.Defs;

namespace Vehicles
{
	public static class VehicleDebugTools
	{
		[DebugAction("Vehicles", null, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void SpawnVehicleRandomized()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs.OrderBy(d => d.defName))
			{
				list.Add(new DebugMenuOption(vehicleDef.defName, DebugMenuOptionMode.Tool, delegate()
				{
					Faction faction = Faction.OfPlayer;
					VehicleSpawner.SpawnVehicleRandomized(vehicleDef, Verse.UI.MouseCell(), Find.CurrentMap, faction);
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
			foreach (Faction faction in Find.World.factionManager.GetFactions(true, false, true, TechLevel.Undefined).OrderBy(f => f.def.defName))
			{
				factions.Add(new DebugMenuOption(faction.def.defName, DebugMenuOptionMode.Action, delegate ()
				{
					factionLocal = faction;

					foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs.OrderBy(d => d.defName))
					{
						vehicles.Add(new DebugMenuOption(vehicleDef.defName, DebugMenuOptionMode.Tool, delegate()
						{
							Faction factionAssigned = faction;
							VehicleSpawner.SpawnVehicleRandomized(vehicleDef, Verse.UI.MouseCell(), Find.CurrentMap, factionLocal is null ? factionAssigned : factionLocal, Rot4.North, true);
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
							List<RaidStrategyDef> source = allDefs.Where(predicate).ToList();
							Log.Message("Available strategies: " + string.Join(", ", (from s in source
							select s.defName).ToArray<string>()));
							parms.raidStrategy = VehicleRaidStrategyDefOf.ArmoredAttack;
							if (parms.raidStrategy != null)
							{
								Log.Message("Strategy: " + parms.raidStrategy.defName);
								IEnumerable<PawnsArrivalModeDef> allDefs2 = DefDatabase<PawnsArrivalModeDef>.AllDefs;
							
								parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;//source2.RandomElement<PawnsArrivalModeDef>();
								Log.Message("Arrival mode: " + parms.raidArrivalMode.defName);
							}
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
						}));
					}
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		[DebugAction("Vehicles", allowedGameStates = AllowedGameStates.Playing)]
		private static void ClearAllListers()
		{
			foreach (Map map in Find.Maps)
			{
				map.GetCachedMapComponent<VehicleReservationManager>().ClearAllListers();
			}
		}

		[DebugAction("Vehicles", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void SpawnCrashingShuttle()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs.OrderBy(d => d.defName))
			{
				list.Add(new DebugMenuOption(vehicleDef.defName, DebugMenuOptionMode.Tool, delegate()
				{
					Faction faction = Faction.OfPlayer;
					VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, faction);
					vehicle.CompVehicleLauncher?.InitializeLaunchProtocols(false);
					AerialVehicleInFlight flyingVehicle = (AerialVehicleInFlight)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.AerialVehicle);
					flyingVehicle.vehicle = vehicle;
					flyingVehicle.vehicle.CompVehicleLauncher.inFlight = true;
					flyingVehicle.Tile = Find.CurrentMap.Tile;
					flyingVehicle.SetFaction(vehicle.Faction);
					flyingVehicle.Initialize();
					(VehicleIncidentDefOf.BlackHawkDown.Worker as IncidentWorker_ShuttleDowned).TryExecuteEvent(flyingVehicle, null, Verse.UI.MouseCell());
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		[DebugAction("Vehicles", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void SpawnStrafeRun()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (PawnKindDef localKindDef2 in from kd in DefDatabase<PawnKindDef>.AllDefs.Where(v => v.race.thingClass == typeof(VehiclePawn) && v.race is VehicleDef def && def.vehicleType == VehicleType.Air)
												  orderby kd.defName
												  select kd)
			{
				PawnKindDef localKindDef = localKindDef2;
				list.Add(new DebugMenuOption(localKindDef.defName, DebugMenuOptionMode.Tool, delegate ()
				{

				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}
	}
}
