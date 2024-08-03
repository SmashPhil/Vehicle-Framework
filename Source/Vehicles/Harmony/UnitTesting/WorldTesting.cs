using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.Profile;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Verse.Noise;

namespace Vehicles
{
	/// <summary>
	/// Unit Testing
	/// </summary>
	public static class WorldTesting
	{
		[UnitTest(Category = "Map", Name = "Strafe Targeting", GameState = GameState.Playing)]
		private static void UnitTest_StrafeTargeting()
		{
			Prefs.DevMode = true;
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Settlement settlement = Find.WorldObjects.Settlements.FirstOrDefault(settlement => settlement.Faction.IsPlayer);
				if (settlement == null)
				{
					SmashLog.Error($"Unable to execute unit test {nameof(WorldTesting)}. No map to form player caravan from.");
					return;
				}
				Map map = Find.CurrentMap;
				VehiclePawn vehicle = (VehiclePawn)map.mapPawns.AllPawns.FirstOrDefault(p => p is VehiclePawn vehicle && vehicle.CompVehicleLauncher != null);
				CameraJumper.TryJump(vehicle);
				StrafeTargeter.Instance.BeginTargeting(vehicle, vehicle.CompVehicleLauncher.launchProtocol, delegate (IntVec3 start, IntVec3 end)
				{
				}, null, null, null, true);
			});
		}

		[UnitTest(Category = "World", Name = "Caravan Formation", GameState = GameState.Playing)]
		private static void UnitTest_CaravanFormation()
		{
			Prefs.DevMode = true;
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				CameraJumper.TryShowWorld();
				Settlement settlement = Find.WorldObjects.Settlements.FirstOrDefault(settlement => settlement.Faction.IsPlayer);
				if (settlement == null)
				{
					SmashLog.Error($"Unable to execute unit test {nameof(WorldTesting)}. No map to form player caravan from.");
					return;
				}
				Find.WindowStack.Add(new Dialog_FormVehicleCaravan(settlement.Map));
			});
		}

		/// <summary>
		/// Load up game, open route planner
		/// </summary>
		[UnitTest(Category = "World", Name = "World Route Planner", GameState = GameState.Playing)]
		private static void UnitTest_RoutePlanner()
		{
			Prefs.DevMode = true;
			CameraJumper.TryShowWorld();
			VehicleRoutePlanner.Instance.Start();
		}

		[UnitTest(Category = "World", Name = "New Game", GameState = GameState.OnStartup)]
		private static void GenerateNewWorld()
		{
			LongEventHandler.QueueLongEvent(delegate ()
			{
				Current.ProgramState = ProgramState.Entry;
				Current.Game = new Game();
				Current.Game.InitData = new GameInitData();
				Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
				Find.Scenario.PreConfigure();
				Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
				Current.Game.World = WorldGenerator.GenerateWorld(0.3f, GenText.RandomSeedString(), OverallRainfall.Normal, OverallTemperature.Normal, OverallPopulation.Normal, GetFactionsForWorldGen(), ModsConfig.BiotechActive ? 0.05f : 0);
				
				Find.Scenario.PostIdeoChosen();

				PageUtility.InitGameStart();

				LongEventHandler.ExecuteWhenFinished(delegate
				{
					MemoryUtility.UnloadUnusedUnityAssets();
					Find.World.renderer.RegenerateAllLayersNow();
				});
			}, "GeneratingWorld", true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
		}

		private static List<FactionDef> GetFactionsForWorldGen()
		{
			List<FactionDef> factions = new List<FactionDef>();
			foreach (FactionDef factionDef in FactionGenerator.ConfigurableFactions)
			{
				if (factionDef.startingCountAtWorldCreation > 0)
				{
					for (int i = 0; i < factionDef.startingCountAtWorldCreation; i++)
					{
						factions.Add(factionDef);
					}
				}
			}
			foreach (FactionDef factionDef in FactionGenerator.ConfigurableFactions)
			{
				if (factionDef.replacesFaction != null)
				{
					factions.RemoveAll((FactionDef replacedFactionDef) => replacedFactionDef == factionDef.replacesFaction);
				}
			}
			return factions;
		}
	}
}
