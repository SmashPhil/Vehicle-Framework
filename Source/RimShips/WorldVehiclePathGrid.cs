using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
    public class WorldVehiclePathGrid : WorldComponent
    {
        private static int DayOfYearAt0Long
		{
			get
			{
				return GenDate.DayOfYear(GenTicks.TicksAbs, 0f);
			}
		}

		public WorldVehiclePathGrid(World world) : base(world)
        {
			this.world = world;
			ResetPathGrid();
        }

		public void ResetPathGrid()
		{
			movementDifficulty = new Dictionary<ThingDef, float[]>();
		}

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
			if (allPathCostsRecalculatedDayOfYear != DayOfYearAt0Long)
			{
				RecalculateAllPerceivedPathCosts();
			}
        }

		public bool Passable(int tile, ThingDef vehicleDef)
		{
			return Find.WorldGrid.InBounds(tile) && movementDifficulty[vehicleDef][tile] < ImpassableMovementDifficulty;
		}

		public bool PassableFast(int tile, ThingDef vehicleDef)
		{
			return movementDifficulty[vehicleDef][tile] < ImpassableMovementDifficulty;
		}

		public float PerceivedMovementDifficultyAt(int tile, ThingDef vehicleDef)
		{
			return movementDifficulty[vehicleDef][tile];
		}

		public void RecalculatePerceivedMovementDifficultyAt(int tile, ThingDef vehicleDef, int? ticksAbs = null)
		{
			if (!Find.WorldGrid.InBounds(tile))
			{
				return;
			}
			bool flag = PassableFast(tile, vehicleDef);
			movementDifficulty[vehicleDef][tile] = CalculatedMovementDifficultyAt(tile, vehicleDef, ticksAbs, null);
			if (flag != PassableFast(tile, vehicleDef))
			{
				Find.World.GetComponent<WorldVehicleReachability>().ClearCache();
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
			List<ThingDef> vehicleDefs = DefDatabase<ThingDef>.AllDefs.Where(v => v.GetCompProperties<CompProperties_Vehicle>() != null).ToList();
			foreach(ThingDef vehicleDef in vehicleDefs)
            {
				if (!movementDifficulty.ContainsKey(vehicleDef))
					movementDifficulty.Add(vehicleDef, new float[Find.WorldGrid.TilesCount]);
				for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
				{
					RecalculatePerceivedMovementDifficultyAt(i, vehicleDef, ticksAbs);
				}
            }
		}

		public static float CalculatedMovementDifficultyAt(int tile, ThingDef vehicleDef, int? ticksAbs = null, StringBuilder explanation = null)
		{
			Tile tile2 = Find.WorldGrid[tile];
			
			if (explanation != null && explanation.Length > 0)
			{
				explanation.AppendLine();
			}
			if ( (tile2.biome.impassable && ( (!vehicleDef.GetCompProperties<CompProperties_Vehicle>().customBiomeCosts?.ContainsKey(tile2.biome) ?? true) || vehicleDef.GetCompProperties<CompProperties_Vehicle>().customBiomeCosts[tile2.biome] >= ImpassableMovementDifficulty)) 
				|| (tile2.hilliness == Hilliness.Impassable && ( (!vehicleDef.GetCompProperties<CompProperties_Vehicle>().customHillinessCosts?.ContainsKey(tile2.hilliness) ?? true ) || vehicleDef.GetCompProperties<CompProperties_Vehicle>().customHillinessCosts[tile2.hilliness] >= ImpassableMovementDifficulty)))
			{
				if (explanation != null)
				{
					explanation.Append("Impassable".Translate());
				}
				return ImpassableMovementDifficulty;
			}

			float num = tile2.biome.movementDifficulty;
			if(!vehicleDef.GetCompProperties<CompProperties_Vehicle>().customBiomeCosts.EnumerableNullOrEmpty() && vehicleDef.GetCompProperties<CompProperties_Vehicle>().customBiomeCosts.Keys.Contains(tile2.biome))
            {
				num = vehicleDef.GetCompProperties<CompProperties_Vehicle>().customBiomeCosts[tile2.biome];
            }
			
			if (explanation != null)
			{
				explanation.Append(tile2.biome.LabelCap + ": " + tile2.biome.movementDifficulty.ToStringWithSign("0.#"));
			}
			float num2 = HillinessMovementDifficultyOffset(tile2.hilliness);
			if(!vehicleDef.GetCompProperties<CompProperties_Vehicle>().customHillinessCosts.EnumerableNullOrEmpty() && vehicleDef.GetCompProperties<CompProperties_Vehicle>().customHillinessCosts.Keys.Contains(tile2.hilliness))
            {
				num2 = vehicleDef.GetCompProperties<CompProperties_Vehicle>().customHillinessCosts[tile2.hilliness];
            }
			
			float num3 = num + num2;
			if (explanation != null && num2 != 0f)
			{
				explanation.AppendLine();
				explanation.Append(tile2.hilliness.GetLabelCap() + ": " + num2.ToStringWithSign("0.#"));
			}
			return num3 + GetCurrentWinterMovementDifficultyOffset(tile, vehicleDef, new int?(ticksAbs ?? GenTicks.TicksAbs), explanation);
		}

		public static float GetCurrentWinterMovementDifficultyOffset(int tile, ThingDef vehicleDef, int? ticksAbs = null, StringBuilder explanation = null)
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
					explanation.Append(num8.ToStringWithSign("0.#"));
				}
				return num8 * vehicleDef.GetCompProperties<CompProperties_Vehicle>().winterPathCostMultiplier;
			}
			return 0f;
		}

		public static bool WillWinterEverAffectMovementDifficulty(int tile)
		{
			int ticksAbs = GenTicks.TicksAbs;
			for (int i = 0; i < 3600000; i += 60000)
			{
				if (GenTemperature.GetTemperatureFromSeasonAtTile(ticksAbs + i, tile) < MaxTempForWinterOffset)
				{
					return true;
				}
			}
			return false;
		}

		private static float HillinessMovementDifficultyOffset(Hilliness hilliness)
		{
			switch (hilliness)
			{
			case Hilliness.Flat:
				return 0f;
			case Hilliness.SmallHills:
				return 0.5f;
			case Hilliness.LargeHills:
				return 1.5f;
			case Hilliness.Mountainous:
				return 3f;
			case Hilliness.Impassable:
				return ImpassableMovementDifficulty;
			default:
				return 0f;
			}
		}

		public Dictionary<ThingDef, float[]> movementDifficulty;

		private int allPathCostsRecalculatedDayOfYear = -1;

		private const float ImpassableMovementDifficulty = 1000f;

		public const float WinterMovementDifficultyOffset = 2f;

		public const float MaxTempForWinterOffset = 5f;
    }
}
