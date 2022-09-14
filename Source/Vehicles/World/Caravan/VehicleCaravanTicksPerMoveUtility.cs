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

		private static List<Pawn> outerPawns = new List<Pawn>();
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
			return GetTicksPerMove(new VehicleInfo(caravan), explanation);
		}

		public static int GetTicksPerMove(VehicleInfo caravanInfo, StringBuilder explanation = null)
		{
			return GetTicksPerMove(caravanInfo.pawns, caravanInfo.massUsage, caravanInfo.massCapacity, explanation);
		}

		public static int GetTicksPerMove(List<Pawn> pawns, float massUsage, float massCapacity, StringBuilder explanation = null)
		{
			if (!pawns.NullOrEmpty())
			{
				moveSpeedTicks.Clear();
				outerPawns.Clear();
				StringBuilder vehicleTicksExplanation = null;
				if (explanation != null)
				{
					vehicleTicksExplanation = new StringBuilder();
				}
				foreach (Pawn pawn in pawns)
				{
					if (pawn is VehiclePawn vehicle)
					{
						float moveSpeed = (vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) * vehicle.VehicleDef.properties.worldSpeedMultiplier) / 60;
						int moveSpeedRatio = Mathf.RoundToInt(1 / moveSpeed);
						float tickSpeed = moveSpeedRatio * CellToTilesConversionRatio;
						int ticksPerTile = Mathf.Max(Mathf.RoundToInt(tickSpeed), 1);
						moveSpeedTicks.Add(ticksPerTile);
						vehicleTicksExplanation?.AppendLine($"  {vehicle.LabelCap}: {GenDate.TicksPerDay / ticksPerTile:0.#} {"TilesPerDay".Translate()}");
					}
					else if (!pawn.IsInVehicle())
					{
						outerPawns.Add(pawn);
					}
				}
				int pawnTickAverage = int.MinValue;
				if (!outerPawns.NullOrEmpty())
				{
					pawnTickAverage = CaravanTicksPerMoveUtility.GetTicksPerMove(outerPawns, 0, 100); //REDO - add proper usage + capacity for dismounted pawns
				}
				float averageVehicleTicks = (float)moveSpeedTicks.Average();
				int minTicks = Mathf.Max(Mathf.RoundToInt(averageVehicleTicks), pawnTickAverage);
				if (explanation != null)
				{
					explanation.AppendLine($"{"CaravanMovementSpeedFull".Translate()}:");
					explanation.AppendLine($"  {vehicleTicksExplanation}");
					if (massUsage > massCapacity)
					{
						explanation.AppendLine($"  {"MultiplierForCarriedMass".Translate()}");
					}
					if (pawnTickAverage > averageVehicleTicks)
					{
						explanation.AppendLine($"  {"VF_DismountedPawns".Translate(pawnTickAverage)}");
					}
					explanation.AppendLine();
					explanation.AppendLine($"  {"Average".Translate()}: {GenDate.TicksPerDay / minTicks:0.#} {"TilesPerDay".Translate()}");
				}
				return minTicks;
			}
			if (explanation != null)
			{
				AppendUsingDefaultTicksPerMoveInfo(explanation);
			}
			return DefaultTicksPerMove;
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

		public struct VehicleInfo
		{
			public List<Pawn> pawns;
			public float massUsage;
			public float massCapacity;

			public VehicleInfo(List<Pawn> pawns)
			{
				this.pawns = pawns;
				massUsage = pawns.Sum(pawn => MassUtility.GearAndInventoryMass(pawn));
				massCapacity = pawns.Sum(pawn => pawn is VehiclePawn vehicle ? vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity) : (pawn.IsInVehicle() ? 0 : MassUtility.Capacity(pawn)));
			}

			public VehicleInfo(Caravan caravan)
			{
				pawns = caravan.PawnsListForReading;
				massUsage = caravan.MassUsage;
				massCapacity = caravan.MassCapacity;
			}

			public VehicleInfo(Dialog_FormVehicleCaravan formCaravanDialog)
			{
				pawns = TransferableUtility.GetPawnsFromTransferables(formCaravanDialog.transferables);
				massUsage = formCaravanDialog.MassUsage;
				massCapacity = formCaravanDialog.MassCapacity;
			}
		}
	}
}
