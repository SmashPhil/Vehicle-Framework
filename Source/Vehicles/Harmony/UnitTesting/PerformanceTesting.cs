using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.Profile;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	/// <summary>
	/// Unit Testing
	/// </summary>
	public static class PerformanceTesting
	{
		private const int SampleSize = 180;

		private static WeatherDef permanentWeatherDef;
		private static double[][] averageMilliseconds;

		[UnitTest(Category = "Performance", Name = "Permanent Weather (Heavy Snow)", GameState = GameState.Playing)]
		private static void UnitTest_SetPermanentWeather()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Messages.Message("Patching PermanentWeatherTick for unit testing.", MessageTypeDefOf.NeutralEvent);
				permanentWeatherDef = DefDatabase<WeatherDef>.GetNamed("SnowHard");
				VehicleHarmony.Patch(AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.GameComponentTick)),
					postfix: new HarmonyMethod(typeof(PerformanceTesting), nameof(PermanentWeatherTick)));
				VehicleHarmony.Patch(AccessTools.PropertyGetter(typeof(MapTemperature), nameof(MapTemperature.OutdoorTemp)),
					postfix: new HarmonyMethod(typeof(PerformanceTesting), nameof(PermanentFreezing)));
			});
		}

		[UnitTest(Category = "Performance", Name = "Component Caching", GameState = GameState.Playing)]
		private static void UnitTest_ProfileComponentCaching()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Messages.Message("Patching ComponentCaching for unit testing.", MessageTypeDefOf.NeutralEvent);
				averageMilliseconds = new double[3][];
				averageMilliseconds.Populate(new double[SampleSize]);

				VehicleHarmony.Patch(AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.GameComponentUpdate)),
					postfix: new HarmonyMethod(typeof(PerformanceTesting), nameof(ComponentCacheProfile)));
			});
		}

		private static void PermanentWeatherTick()
		{
			if (permanentWeatherDef != null)
			{
				if (Current.ProgramState == ProgramState.Playing && !Find.Maps.NullOrEmpty())
				{
					foreach (Map map in Find.Maps)
					{
						if (map.weatherManager.curWeather != permanentWeatherDef)
						{
							map.weatherManager.TransitionTo(permanentWeatherDef);
						}
					}
				}
			}
		}

		private static void PermanentFreezing(ref float __result)
		{
			__result = -5;
		}

		private static void ComponentCacheProfile()
		{
			if (Find.CurrentMap is Map map)
			{
				Log.Clear();
				ProfilerWatch.Start();

				//GetComponent
				_ = map.GetComponent<VehicleMapping>();
				TimeSpan vanillaSample = ProfilerWatch.Get();
				averageMilliseconds[0][Find.TickManager.TicksGame % SampleSize] = vanillaSample.TotalMilliseconds;

				//ComponentCache
				_ = map.GetCachedMapComponent<VehicleMapping>();
				TimeSpan componentCacheSample = ProfilerWatch.Get();
				averageMilliseconds[1][Find.TickManager.TicksGame % SampleSize] = componentCacheSample.TotalMilliseconds;

				//MapComponentCache
				_ = MapComponentCache<VehicleMapping>.GetComponent(map);
				TimeSpan genericCacheSample = ProfilerWatch.Get();
				averageMilliseconds[2][Find.TickManager.TicksGame % SampleSize] = genericCacheSample.TotalMilliseconds;
				
				ProfilerWatch.End();

				double vanillaAverage = averageMilliseconds[0].Average();
				double componentAverage = averageMilliseconds[1].Average();
				double genericAverage = averageMilliseconds[2].Average();

				Log.Message($"GetComponent: {vanillaAverage:0.0000}ms");
				Log.Message($"ComponentCache: {componentAverage:0.0000}ms");
				Log.Message($"MapComponentCache: {genericAverage:0.0000}ms");

				string winner = "GetComponent";
				if (componentAverage < vanillaAverage || genericAverage < vanillaAverage)
				{
					winner = genericAverage < componentAverage ? "MapComponentCache" : "ComponentCache";
				}
				else if (componentAverage == vanillaAverage && genericAverage == vanillaAverage)
				{
					winner = "Roughly Equal";
				}
				Log.Message($"Fastest: {winner}");
			}
		}
	}
}
