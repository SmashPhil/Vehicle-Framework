using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// WorldGrid for vehicles
	/// </summary>
	public class WorldVehiclePathGrid : WorldComponent
	{
		public const float ImpassableMovementDifficulty = 1000f;
		public const float MaxTempForWinterOffset = 5f;

		/// <summary>
		/// Store entire pathGrid for each <see cref="VehicleDef"/>
		/// </summary>
		public Dictionary<VehicleDef, float[]> movementDifficulty;

		private int allPathCostsRecalculatedDayOfYear = -1;

		public WorldVehiclePathGrid(World world) : base(world)
		{
			this.world = world;
			ResetPathGrid();
			Instance = this;
		}

		/// <summary>
		/// Singleton getter
		/// </summary>
		public static WorldVehiclePathGrid Instance { get; private set; }

		/// <summary>
		/// Day of year at 0 longitude for recalculating pathGrids
		/// </summary>
		private static int DayOfYearAt0Long => GenDate.DayOfYear(GenTicks.TicksAbs, 0f);

		private static bool Recalculating { get; set; }

		/// <summary>
		/// <paramref name="cost"/> is &gt; <see cref="ImpassableMovementDifficulty"/> or &lt; 0
		/// </summary>
		/// <param name="cost"></param>
		/// <returns><paramref name="cost"/> is impassable</returns>
		public static bool ImpassableCost(float cost) => cost >= ImpassableMovementDifficulty;

		/// <summary>
		/// Reset all cached pathGrids for VehicleDefs
		/// </summary>
		public void ResetPathGrid()
		{
			movementDifficulty = new Dictionary<VehicleDef, float[]>();
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				movementDifficulty[vehicleDef] = new float[Find.WorldGrid.TilesCount];
			}
		}

		/// <summary>
		/// Recalculate all perceived path costs at <see cref="DayOfYearAt0Long"/>
		/// </summary>
		public override void WorldComponentTick()
		{
			base.WorldComponentTick();
			if (!Recalculating && allPathCostsRecalculatedDayOfYear != DayOfYearAt0Long)
			{
				RecalculateAllPerceivedPathCosts();
			}
			if (DebugHelper.World.VehicleDef != null && Find.WorldSelector.selectedTile >= 0 && Find.TickManager.TicksGame % 10 == 0)
			{
				int tile = Find.WorldSelector.selectedTile;
				List<int> neighbors = new List<int>();
				Find.WorldGrid.GetTileNeighbors(tile, neighbors);

				Find.World.debugDrawer.FlashTile(tile, text: movementDifficulty[DebugHelper.World.VehicleDef][tile].ToString(), duration: 10);
				foreach (int neighborTile in neighbors)
				{
					Find.World.debugDrawer.FlashTile(neighborTile, text: movementDifficulty[DebugHelper.World.VehicleDef][neighborTile].ToString(), duration: 10);
				}
			}
		}

		/// <summary>
		/// Flash all path costs for <paramref name="vehicleDef"/> on the world grid
		/// </summary>
		/// <param name="vehicleDef"></param>
		public static void PushVehicleToDraw(VehicleDef vehicleDef)
		{
			for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
			{
				float pathCost = Instance.PerceivedMovementDifficultyAt(i, vehicleDef).RoundTo(0.1f);
				Find.World.debugDrawer.FlashTile(i, 0.01f, pathCost.RoundTo(0.1f).ToString(), 600);
			}
		}

		/// <summary>
		/// <paramref name="tile"/> is passable for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="vehicleDef"></param>
		public bool Passable(int tile, VehicleDef vehicleDef)
		{
			return Find.WorldGrid.InBounds(tile) && movementDifficulty[vehicleDef][tile] < ImpassableMovementDifficulty;
		}

		/// <summary>
		/// <paramref name="tile"/> is passable for <paramref name="vehicleDef"/> (no bounds check)
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="vehicleDef"></param>
		public bool PassableFast(int tile, VehicleDef vehicleDef)
		{
			return movementDifficulty[vehicleDef][tile] < ImpassableMovementDifficulty;
		}

		/// <summary>
		/// pathCost for <paramref name="vehicleDef"/> at <paramref name="tile"/>
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="vehicleDef"></param>
		public float PerceivedMovementDifficultyAt(int tile, VehicleDef vehicleDef)
		{
			return movementDifficulty[vehicleDef][tile];
		}

		/// <summary>
		/// Recalculate pathCost at <paramref name="tile"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="ticksAbs"></param>
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

		/// <summary>
		/// Recalculate all path costs for all VehicleDefs
		/// </summary>
		public void RecalculateAllPerceivedPathCosts()
		{
			Log.Message($"Async Recalculation");
			TaskManager.RunAsync(RecalculateAllPerceivedPathCosts_Async);
		}

		private void RecalculateAllPerceivedPathCosts_Async()
		{
			RecalculateAllPerceivedPathCosts(null);
		}

		/// <summary>
		/// Recalculate all path costs for all VehicleDefs
		/// </summary>
		/// <param name="ticksAbs"></param>
		public void RecalculateAllPerceivedPathCosts(int? ticksAbs)
		{
			Recalculating = true;
			{
				foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
				{
					if (!movementDifficulty.ContainsKey(vehicleDef))
					{
						movementDifficulty[vehicleDef] = new float[Find.WorldGrid.TilesCount];
					}
					for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
					{
						RecalculatePerceivedMovementDifficultyAt(i, vehicleDef, ticksAbs);
					}
				}
				allPathCostsRecalculatedDayOfYear = DayOfYearAt0Long;
			}
			Recalculating = false;
		}

		/// <summary>
		/// Calculate path cost for <paramref name="vehicleDef"/> at <paramref name="tile"/>
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="ticksAbs"></param>
		/// <param name="explanation"></param>
		/// <returns></returns>
		public static float CalculatedMovementDifficultyAt(int tile, VehicleDef vehicleDef, int? ticksAbs = null, StringBuilder explanation = null, bool coastalTravel = true)
		{
			Tile worldTile = Find.WorldGrid[tile];
			
			if (explanation != null && explanation.Length > 0)
			{
				explanation.AppendLine();
			}

			float defaultBiomeCost = vehicleDef.properties.defaultBiomesImpassable ? ImpassableMovementDifficulty : WorldPathGrid.CalculatedMovementDifficultyAt(tile, false, ticksAbs, explanation);

			if (coastalTravel && vehicleDef.CoastalTravel(tile))
			{
				defaultBiomeCost = Mathf.Min(defaultBiomeCost, vehicleDef.properties.customBiomeCosts[BiomeDefOf.Ocean]);
			}
			//float roadMultiplier = VehicleCaravan_PathFollower.GetRoadMovementDifficultyMultiplier(vehicleDefs, tile, neighbor, null)
			float biomeCost = vehicleDef.properties.customBiomeCosts.TryGetValue(worldTile.biome, defaultBiomeCost);
			float hillinessCost = vehicleDef.properties.customHillinessCosts.TryGetValue(worldTile.hilliness, HillinessMovementDifficultyOffset(worldTile.hilliness));
			if (ImpassableCost(biomeCost) || ImpassableCost(hillinessCost))
			{
				if (explanation != null)
				{
					explanation.Append("Impassable".Translate());
				}
				return ImpassableMovementDifficulty;
			}
			
			if (explanation != null)
			{
				explanation.Append(worldTile.biome.LabelCap + ": " + biomeCost.ToStringWithSign("0.#"));
			}
			
			float totalCost = biomeCost + hillinessCost;
			if (explanation != null && hillinessCost != 0f)
			{
				explanation.AppendLine();
				explanation.Append(worldTile.hilliness.GetLabelCap() + ": " + hillinessCost.ToStringWithSign("0.#"));
			}
			return totalCost + GetCurrentWinterMovementDifficultyOffset(tile, vehicleDef, new int?(ticksAbs ?? GenTicks.TicksAbs), explanation);
		}

		/// <summary>
		/// Max cost on <paramref name="tile"/> given neighbor tile <paramref name="neighbor"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <remarks>
		/// <paramref name="tile"/> must have coast
		/// </remarks>
		/// <param name="tile"></param>
		/// <param name="neighbor"></param>
		/// <param name="vehicleDef"></param>
		public static float ConsistentDirectionCost(int tile, int neighbor, VehicleDef vehicleDef)
		{
			return Mathf.Max(CalculatedMovementDifficultyAt(tile, vehicleDef, null, null, false), CalculatedMovementDifficultyAt(neighbor, vehicleDef, null, null, false));
		}

		/// <summary>
		/// Winter path cost multiplier for <paramref name="vehicleDef"/> at <paramref name="tile"/>
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="ticksAbs"></param>
		/// <param name="explanation"></param>
		public static float GetCurrentWinterMovementDifficultyOffset(int tile, VehicleDef vehicleDef, int? ticksAbs = null, StringBuilder explanation = null)
		{
			if (ticksAbs == null)
			{
				ticksAbs = new int?(GenTicks.TicksAbs);
			}
			Vector2 vector = Find.WorldGrid.LongLatOf(tile);
			SeasonUtility.GetSeason(GenDate.YearPercent(ticksAbs.Value, vector.x), vector.y, out _, out _, out _, out float winter, out _, out float permaWinter);
			float totalWinter = winter + permaWinter;
			totalWinter *= Mathf.InverseLerp(MaxTempForWinterOffset, 0f, GenTemperature.GetTemperatureFromSeasonAtTile(ticksAbs.Value, tile));
			if (totalWinter > 0.01f)
			{
				float finalCost = totalWinter * vehicleDef.properties.winterSpeedMultiplier;
				if (explanation != null)
				{
					explanation.AppendLine();
					explanation.Append("Winter".Translate());
					if (totalWinter < 0.999f)
					{
						explanation.Append($" ({totalWinter.ToStringPercent("F0")})");
					}
					if (vehicleDef.properties.winterSpeedMultiplier != 1)
					{
						explanation.Append($" (Offset: {vehicleDef.properties.winterSpeedMultiplier})");
					}
					explanation.Append(": ");
					explanation.Append(finalCost.ToStringWithSign("0.#"));
				}
				return finalCost;
			}
			return 0f;
		}

		/// <summary>
		/// Default hilliness path costs
		/// </summary>
		/// <param name="hilliness"></param>
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
