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

		[StartupAction(Category = "Performance", Name = "Permanent Weather (Heavy Snow)", GameState = GameState.Playing)]
		private static void StartupAction_SetPermanentWeather()
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

		[StartupAction(Category = "Performance", Name = "Component Caching", GameState = GameState.Playing)]
		private static void StartupAction_ProfileComponentCaching()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Messages.Message("Patching ComponentCaching for unit testing.", MessageTypeDefOf.NeutralEvent);

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
				ProfilerWatch.Start("Component Caching");
				{
					//GetComponent
					ProfilerWatch.Start("Vanilla");
					{
						_ = map.GetComponent<VehicleMapping>();
					}
					ProfilerWatch.Stop();

					//ComponentCache - inline
					ProfilerWatch.Start("ComponentCache inlined");
					{
						_ = map.GetCachedMapComponent<VehicleMapping>();
					}
					ProfilerWatch.Stop();

					ProfilerWatch.Start("MapComponentCache");
					{
						_ = MapComponentCache<VehicleMapping>.GetComponent(map);
					}
					ProfilerWatch.Stop();
				}
				ProfilerWatch.Stop();
			}
		}
	}
}
