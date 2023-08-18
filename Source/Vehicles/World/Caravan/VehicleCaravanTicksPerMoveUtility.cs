using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using SmashTools;

namespace Vehicles
{
	public static class VehicleCaravanTicksPerMoveUtility
	{
		private const int MaxPawnTicksPerMove = 150;
		private const int DownedPawnMoveTicks = 450;
		public const float CellToTilesConversionRatio = 340f;
		public const int DefaultTicksPerMove = 3300;
		private const float MoveSpeedFactorAtZeroMass = 2f;

		private static List<int> moveSpeedTicks = new List<int>();

		public static int GetTicksPerMove(Caravan caravan, StringBuilder explanation = null)
		{
			if (caravan == null)
			{
				if (explanation != null)
				{
					AppendUsingDefaultTicksPerMoveInfo(explanation);
				}
				return DefaultTicksPerMove;
			}
			return GetTicksPerMove(new VehicleCaravanInfo(caravan), explanation);
		}

		public static int GetTicksPerMove(VehicleCaravanInfo caravanInfo, StringBuilder explanation = null)
		{
			return GetTicksPerMove(caravanInfo.pawns, caravanInfo.massUsage, caravanInfo.massCapacity, explanation);
		}

		public static int GetTicksPerMove(List<Pawn> pawns, float massUsage, float massCapacity, StringBuilder explanation = null)
		{
			if (!pawns.NullOrEmpty())
			{
				moveSpeedTicks.Clear();
				StringBuilder ticksExplanation = null;
				if (explanation != null)
				{
					ticksExplanation = new StringBuilder();
				}
				foreach (Pawn pawn in pawns)
				{
					if (pawn is VehiclePawn vehicle)
					{
						float worldSpeedMultiplier = vehicle.WorldSpeedMultiplier;
						float moveSpeed = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) * worldSpeedMultiplier / 60;
						int ticksPerTile = TicksFromMoveSpeed(moveSpeed);
						moveSpeedTicks.Add(ticksPerTile);
						ticksExplanation?.AppendLine($"  {vehicle.LabelCap}: {GenDate.TicksPerDay / ticksPerTile:0.#} {"TilesPerDay".Translate()}");
					}
					else if (!pawn.IsInVehicle())
					{
						float moveSpeed = ThingDefOf.Human.GetStatValueAbstract(StatDefOf.MoveSpeed) / 60f;
						int ticksPerTile = TicksFromMoveSpeed(moveSpeed);
						moveSpeedTicks.Add(ticksPerTile);
						ticksExplanation?.AppendLine($"  {pawn.LabelCap}: {GenDate.TicksPerDay / ticksPerTile:0.#} {"TilesPerDay".Translate()}");
					}
				}
				float averageVehicleSpeed = (float)moveSpeedTicks.Average();
				int averageVehicleTicks = Mathf.RoundToInt(averageVehicleSpeed);
				if (explanation != null)
				{
					explanation.AppendLine($"{"CaravanMovementSpeedFull".Translate()}:");
					explanation.AppendLine(ticksExplanation.ToString());
					if (massUsage > massCapacity)
					{
						explanation.AppendLine($"  {"MultiplierForCarriedMass".Translate()}");
					}
					explanation.AppendLine();
					explanation.AppendLine($"  {"Average".Translate()}: {GenDate.TicksPerDay / averageVehicleTicks:0.#} {"TilesPerDay".Translate()}");
				}
				return averageVehicleTicks;
			}
			if (explanation != null)
			{
				AppendUsingDefaultTicksPerMoveInfo(explanation);
			}
			return DefaultTicksPerMove;

			int TicksFromMoveSpeed(float moveSpeed)
			{
				int moveSpeedRatio = Mathf.RoundToInt(1 / moveSpeed);
				float tickSpeed = moveSpeedRatio * CellToTilesConversionRatio;
				int ticksPerTile = Mathf.Max(Mathf.RoundToInt(tickSpeed), 1);
				return ticksPerTile;
			}
		}

		private static float GetMoveSpeedFactorFromMass(float massUsage, float massCapacity)
		{
			if (massCapacity <= 0f)
			{
				return 1f;
			}
			float t = massUsage / massCapacity;
			return Mathf.Lerp(MoveSpeedFactorAtZeroMass, 1f, t);
		}

		private static void AppendUsingDefaultTicksPerMoveInfo(StringBuilder explanation)
		{
			explanation.Append($"{"CaravanMovementSpeedFull".Translate()}:");
			explanation.AppendLine();
			explanation.Append($"  {"Default".Translate()}: {18.181818f:0.#} {"TilesPerDay".Translate()}");
		}

		public static float ApproxTilesPerDay(VehicleCaravan caravan, StringBuilder explanation = null)
		{
			return ApproxTilesPerDay(caravan.UniqueVehicleDefsInCaravan().ToList(), caravan.TicksPerMove, caravan.Tile, caravan.vPather.Moving ? caravan.vPather.nextTile : -1, explanation, explanation != null ? caravan.TicksPerMoveExplanation : null);
		}

		public static float ApproxTilesPerDay(List<VehicleDef> vehicleDefs, int ticksPerMove, int tile, int nextTile, StringBuilder explanation = null, string caravanTicksPerMoveExplanation = null)
		{
			if (nextTile == -1)
			{
				nextTile = Find.WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(tile);
			}
			int num = Mathf.CeilToInt(VehicleCaravan_PathFollower.CostToMove(vehicleDefs, ticksPerMove, tile, nextTile, ticksAbs: null, explanation: explanation, caravanTicksPerMoveExplanation: caravanTicksPerMoveExplanation));
			if (num == 0)
			{
				return 0f;
			}
			return 60000f / num;
		}

		public struct VehicleCaravanInfo
		{
			public List<Pawn> pawns;
			public float massUsage;
			public float massCapacity;

			public VehicleCaravanInfo(List<Pawn> pawns)
			{
				this.pawns = pawns;
				massUsage = pawns.Sum(pawn => MassUtility.GearAndInventoryMass(pawn));
				massCapacity = pawns.Sum(pawn => pawn is VehiclePawn vehicle ? vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity) : (pawn.IsInVehicle() ? 0 : MassUtility.Capacity(pawn)));
			}

			public VehicleCaravanInfo(Caravan caravan)
			{
				pawns = caravan.PawnsListForReading;
				massUsage = caravan.MassUsage;
				massCapacity = caravan.MassCapacity;
			}

			public VehicleCaravanInfo(Dialog_FormVehicleCaravan formCaravanDialog)
			{
				pawns = TransferableUtility.GetPawnsFromTransferables(formCaravanDialog.transferables);
				massUsage = formCaravanDialog.MassUsage;
				massCapacity = formCaravanDialog.MassCapacity;
			}
		}
	}
}
