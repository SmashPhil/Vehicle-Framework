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
	}
}
