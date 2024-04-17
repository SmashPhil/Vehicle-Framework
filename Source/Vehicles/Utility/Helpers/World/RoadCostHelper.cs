using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public static class RoadCostHelper
	{
		public static float GetRoadMovementDifficultyMultiplier(List<VehiclePawn> vehicles, int fromTile, int toTile, StringBuilder explanation = null)
		{
			List<Tile.RoadLink> roads = Find.WorldGrid.tiles[fromTile].Roads;
			if (roads == null)
			{
				return MaxRoadMultiplier(vehicles, VehicleOffRoadMultiplier);
			}
			if (toTile == -1)
			{
				toTile = Find.WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(fromTile);
			}
			for (int i = 0; i < roads.Count; i++)
			{
				if (roads[i].neighbor == toTile)
				{
					float roadMultiplier = GetRoadMovementDifficultyMultiplier(vehicles, roads[i].road);

					if (explanation != null)
					{
						if (explanation.Length > 0)
						{
							explanation.AppendLine();
						}
						explanation.Append($"{roads[i].road.LabelCap}: {roadMultiplier.ToStringPercent()}");
					}
					return roadMultiplier;
				}
			}
			return 1f;
		}

		public static float GetRoadMovementDifficultyMultiplier(List<VehicleDef> vehicleDefs, int fromTile, int toTile, StringBuilder explanation = null)
		{
			List<Tile.RoadLink> roads = Find.WorldGrid.tiles[fromTile].Roads;
			if (roads == null)
			{
				return MaxRoadMultiplier(vehicleDefs, VehicleDefOffRoadMultiplier);
			}
			if (toTile == -1)
			{
				toTile = Find.WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(fromTile);
			}
			for (int i = 0; i < roads.Count; i++)
			{
				if (roads[i].neighbor == toTile)
				{
					float roadMultiplier = GetRoadMovementDifficultyMultiplier(vehicleDefs, roads[i].road);

					if (explanation != null)
					{
						if (explanation.Length > 0)
						{
							explanation.AppendLine();
						}
						explanation.Append($"{roads[i].road.LabelCap}: {roadMultiplier.ToStringPercent()}");
					}
					return roadMultiplier;
				}
			}
			return 1f;
		}

		private static float MaxRoadMultiplier<T>(List<T> list, Func<T, float> selector)
		{
			return Mathf.Clamp(list.Max(selector), 0.01f, 100);
		}

		public static float VehicleOffRoadMultiplier(VehiclePawn vehicle)
		{
			float offRoadMultiplier = VehicleDefOffRoadMultiplier(vehicle.VehicleDef);
			offRoadMultiplier = vehicle.statHandler.GetStatOffset(VehicleStatUpgradeCategoryDefOf.OffRoadMultiplier, offRoadMultiplier);
			return offRoadMultiplier;
		}

		public static float VehicleDefOffRoadMultiplier(VehicleDef vehicleDef)
		{
			return SettingsCache.TryGetValue(vehicleDef, typeof(VehicleProperties), nameof(VehicleProperties.offRoadMultiplier), vehicleDef.properties.offRoadMultiplier);
		}

		public static float GetRoadMovementDifficultyMultiplier(List<VehicleDef> vehicleDefs, RoadDef roadDef)
		{
			float roadMultiplier = roadDef.movementCostMultiplier;
			bool customRoadCosts = false;
			foreach (VehicleDef vehicleDef in vehicleDefs)
			{
				if (vehicleDef.properties.customRoadCosts.TryGetValue(roadDef, out float movementCostMultiplier) && (!customRoadCosts || movementCostMultiplier < roadMultiplier))
				{
					customRoadCosts = true;
					roadMultiplier = movementCostMultiplier;
				}
			}
			return roadMultiplier;
		}

		public static float GetRoadMovementDifficultyMultiplier(List<VehiclePawn> vehicles, RoadDef roadDef)
		{
			float roadMultiplier = roadDef.movementCostMultiplier;
			bool customRoadCosts = false;
			foreach (VehiclePawn vehicle in vehicles)
			{
				if (vehicle.VehicleDef.properties.customRoadCosts.TryGetValue(roadDef, out float movementCostMultiplier) && (!customRoadCosts || movementCostMultiplier < roadMultiplier))
				{
					customRoadCosts = true;
					roadMultiplier = movementCostMultiplier;
				}
			}
			return roadMultiplier;
		}
	}
}
