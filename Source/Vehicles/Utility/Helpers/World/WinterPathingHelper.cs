using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles
{
	public static class WinterPathingHelper
	{
		public const float MaxTempForWinterOffset = 5f;

		public static float GetWinterPercent(int tile, int? ticksAbs = null)
		{
			Vector2 vector = Find.WorldGrid.LongLatOf(tile);
			int ticks = ticksAbs ?? GenTicks.TicksAbs;
			SeasonUtility.GetSeason(GenDate.YearPercent(ticks, vector.x), vector.y, out _, out _, out _, out float winter, out _, out float permaWinter);
			float totalWinter = winter + permaWinter;
			totalWinter *= Mathf.InverseLerp(MaxTempForWinterOffset, 0f, GenTemperature.GetTemperatureFromSeasonAtTile(ticks, tile));
			return totalWinter;
		}

		public static float GetCurrentWinterMovementDifficultyOffset(List<VehiclePawn> vehicles, int tile, StringBuilder explanation = null)
		{
			float winter = WorldVehiclePathGrid.Instance.WinterPercentAt(tile);
			if (winter > 0.01f)
			{
				float winterSpeedMultiplier = HighestWinterOffset(vehicles);
				float finalCost = winter * winterSpeedMultiplier;
				if (explanation != null)
				{
					WinterExplanation(explanation, winter, winterSpeedMultiplier, finalCost);
				}
				return finalCost;
			}
			return 0;
		}

		public static float GetCurrentWinterMovementDifficultyOffset(List<VehicleDef> vehicleDefs, int tile, StringBuilder explanation = null)
		{
			float winter = WorldVehiclePathGrid.Instance.WinterPercentAt(tile);
			if (winter > 0.01f)
			{
				float winterCost = HighestWinterOffset(vehicleDefs);
				float finalCost = winter * winterCost;
				if (explanation != null)
				{
					WinterExplanation(explanation, winter, winterCost, finalCost);
				}
				return finalCost;
			}
			return 0f;
		}

		private static void WinterExplanation(StringBuilder explanation, float winter, float winterCost, float finalCost)
		{
			explanation.AppendLine();
			explanation.Append($"{"Winter".Translate()}: {finalCost.ToStringWithSign("0.#")}");
		}

		private static float HighestWinterOffset(List<VehicleDef> vehicleDefs)
		{
			float winterCost = 0.01f;
			foreach (VehicleDef vehicleDef in vehicleDefs)
			{
				float vehicleDefWinterCost = SettingsCache.TryGetValue(vehicleDef, typeof(VehicleProperties), nameof(VehicleProperties.winterCost), vehicleDef.properties.winterCost);
				if (vehicleDefWinterCost > winterCost)
				{
					winterCost = vehicleDefWinterCost;
				}
			}
			return winterCost;
		}

		private static float HighestWinterOffset(List<VehiclePawn> vehicles)
		{
			float winterCost = 0.01f;
			foreach (VehiclePawn vehicle in vehicles)
			{
				float settingsWinterCostMultiplier = SettingsCache.TryGetValue(vehicle.VehicleDef, typeof(VehicleProperties), nameof(VehicleProperties.winterCost), vehicle.VehicleDef.properties.winterCost);
				float vehicleDefWinterCost = vehicle.statHandler.GetStatOffset(VehicleStatUpgradeCategoryDefOf.WinterCostMultiplier, settingsWinterCostMultiplier);
				if (vehicleDefWinterCost > winterCost)
				{
					winterCost = vehicleDefWinterCost;
				}
			}
			return winterCost;
		}
	}
}
