using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class WorldVehiclePathGrid : WorldComponent
	{
		public const float ImpassableMovementDifficulty = 1000f;
		public const float WinterMovementDifficultyOffset = 2f;
		public const float MaxTempForWinterOffset = 5f;

		public Dictionary<VehicleDef, float[]> movementDifficulty;

		private int allPathCostsRecalculatedDayOfYear = -1;

		public WorldVehiclePathGrid(World world) : base(world)
		{
			this.world = world;
			ResetPathGrid();
			Instance = this;
		}

		public static WorldVehiclePathGrid Instance { get; private set; }

		private static int DayOfYearAt0Long => GenDate.DayOfYear(GenTicks.TicksAbs, 0f);

		public static bool ImpassableCost(float cost) => cost >= ImpassableMovementDifficulty || cost < 0;

		public override void FinalizeInit()
		{
			base.FinalizeInit();
			if (VehicleMod.settings.debug.debugGenerateWorldPathCostTexts)
			{
				LongEventHandler.QueueLongEvent(delegate ()
				{
					WorldPathTextMeshGenerator.GenerateTextMeshObjects();
				}, "VehiclesTextMeshBiomeGeneration", false, (Exception ex) => Log.Error($"{VehicleHarmony.LogLabel} Exception thrown while trying to generate TextMesh GameObjects for world map debugging. Please report to mod page."));
			}
		}

		public void ResetPathGrid()
		{
			movementDifficulty = new Dictionary<VehicleDef, float[]>();
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				if (!movementDifficulty.ContainsKey(vehicleDef))
				{
					movementDifficulty.Add(vehicleDef, new float[Find.WorldGrid.TilesCount]);
				}
			}
		}

		public override void WorldComponentTick()
		{
			base.WorldComponentTick();
			if (allPathCostsRecalculatedDayOfYear != DayOfYearAt0Long)
			{
				RecalculateAllPerceivedPathCosts();
			}
		}

		public bool Passable(int tile, VehicleDef vehicleDef)
		{
			return Find.WorldGrid.InBounds(tile) && movementDifficulty[vehicleDef][tile] < ImpassableMovementDifficulty;
		}

		public bool PassableFast(int tile, VehicleDef vehicleDef)
		{
			return movementDifficulty[vehicleDef][tile] < ImpassableMovementDifficulty;
		}

		public float PerceivedMovementDifficultyAt(int tile, VehicleDef vehicleDef)
		{
			return movementDifficulty[vehicleDef][tile];
		}

		public void RecalculatePerceivedMovementDifficultyAt(int tile, VehicleDef vehicleDef, int? ticksAbs = null)
		{
			if (!Find.WorldGrid.InBounds(tile))
			{
				return;
			}
			bool flag = PassableFast(tile, vehicleDef);
			movementDifficulty[vehicleDef][tile] = CalculatedMovementDifficultyAt(tile, vehicleDef, ticksAbs, null);
			if (flag != PassableFast(tile, vehicleDef))
			{
				WorldVehicleReachability.Instance.ClearCache();
			}
		}

		public void RecalculateAllPerceivedPathCosts()
		{
			RecalculateAllPerceivedPathCosts(null);
			allPathCostsRecalculatedDayOfYear = DayOfYearAt0Long;
		}

		public void RecalculateAllPerceivedPathCosts(int? ticksAbs)
		{
			allPathCostsRecalculatedDayOfYear = -1;
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				if (!movementDifficulty.ContainsKey(vehicleDef))
				{
					movementDifficulty.Add(vehicleDef, new float[Find.WorldGrid.TilesCount]);
				}
				for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
				{
					RecalculatePerceivedMovementDifficultyAt(i, vehicleDef, ticksAbs);
				}
			}
		}

		public static float CalculatedMovementDifficultyAt(int tile, VehicleDef vehicleDef, int? ticksAbs = null, StringBuilder explanation = null)
		{
			Tile worldTile = Find.WorldGrid[tile];
			
			if (explanation != null && explanation.Length > 0)
			{
				explanation.AppendLine();
			}

			if (vehicleDef.CoastalTravel(tile))
			{
				return vehicleDef.properties.customBiomeCosts[BiomeDefOf.Ocean] / vehicleDef.properties.worldSpeedMultiplier;
			}
			else if (vehicleDef.vehicleType == VehicleType.Sea)
			{
				return WorldHelper.WaterCovered(tile) ? vehicleDef.properties.customBiomeCosts[worldTile.biome] / vehicleDef.properties.worldSpeedMultiplier : ImpassableMovementDifficulty;
			}
			float biomeCost = vehicleDef.properties.customBiomeCosts.TryGetValue(worldTile.biome, WorldPathGrid.CalculatedMovementDifficultyAt(tile, false, ticksAbs, explanation));
			float hillinessCost = vehicleDef.properties.customHillinessCosts.TryGetValue(worldTile.hilliness, HillinessMovementDifficultyOffset(worldTile.hilliness));
			if (ImpassableCost(biomeCost) || ImpassableCost(hillinessCost))
			{
				if (explanation != null)
				{
					explanation.Append("Impassable".Translate());
				}
				return ImpassableMovementDifficulty;
			}

			float finalBiomeCost = biomeCost / vehicleDef.properties.worldSpeedMultiplier;
			
			if (explanation != null)
			{
				explanation.Append(worldTile.biome.LabelCap + ": " + biomeCost.ToStringWithSign("0.#"));
			}
			
			float num3 = finalBiomeCost + hillinessCost;
			if (explanation != null && hillinessCost != 0f)
			{
				explanation.AppendLine();
				explanation.Append(worldTile.hilliness.GetLabelCap() + ": " + hillinessCost.ToStringWithSign("0.#"));
			}
			return num3 + GetCurrentWinterMovementDifficultyOffset(tile, vehicleDef, new int?(ticksAbs ?? GenTicks.TicksAbs), explanation);
		}

		public static float GetCurrentWinterMovementDifficultyOffset(int tile, VehicleDef vehicleDef, int? ticksAbs = null, StringBuilder explanation = null)
		{
			if (ticksAbs == null)
			{
				ticksAbs = new int?(GenTicks.TicksAbs);
			}
			Vector2 vector = Find.WorldGrid.LongLatOf(tile);
			SeasonUtility.GetSeason(GenDate.YearPercent(ticksAbs.Value, vector.x), vector.y, out float num, out float num2, out float num3, out float num4, out float num5, out float num6);
			float num7 = num4 + num6;
			num7 *= Mathf.InverseLerp(MaxTempForWinterOffset, 0f, GenTemperature.GetTemperatureFromSeasonAtTile(ticksAbs.Value, tile));
			if (num7 > 0.01f)
			{
				float num8 = WinterMovementDifficultyOffset * num7;
				if (explanation != null)
				{
					explanation.AppendLine();
					explanation.Append("Winter".Translate());
					if (num7 < 0.999f)
					{
						explanation.Append(" (" + num7.ToStringPercent("F0") + ")");
					}
					explanation.Append(": ");
					explanation.Append(num8.ToStringWithSign("0.#")); //REDO - Add translated text for winter path cost multiplier
				}
				return num8 * vehicleDef.properties.winterPathCostMultiplier;
			}
			return 0f;
		}

		public static float HillinessMovementDifficultyOffset(Hilliness hilliness)
		{
			return hilliness switch
			{
				Hilliness.Flat => 0f,
				Hilliness.SmallHills => 0.5f,
				Hilliness.LargeHills => 1.5f,
				Hilliness.Mountainous => 3f,
				Hilliness.Impassable => ImpassableMovementDifficulty,
				_ => 0f,
			};
		}
	}
}
