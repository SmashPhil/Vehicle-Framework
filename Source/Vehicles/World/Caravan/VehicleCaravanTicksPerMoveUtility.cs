using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public static class VehicleCaravanTicksPerMoveUtility
{
	private const int MaxPawnTicksPerMove = 150;
	private const int DownedPawnMoveTicks = 450;
	private const float CellToTilesConversionRatio = 340f;
	private const float MoveSpeedFactorAtZeroMass = 2f;

	public const int DefaultTicksPerMove = 3300;

	private static readonly List<int> MoveSpeedTicks = [];
	private static readonly StringBuilder TicksExplanation = new();

	[MustUseReturnValue]
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

	[MustUseReturnValue]
	public static int GetTicksPerMove(VehicleCaravanInfo caravanInfo,
		StringBuilder explanation = null)
	{
		return GetTicksPerMove(caravanInfo.vehiclesAndDismountedPawns, caravanInfo.massUsage, caravanInfo.massCapacity,
			explanation);
	}

	[MustUseReturnValue]
	public static int GetTicksPerMove(List<Pawn> pawns, float massUsage, float massCapacity,
		StringBuilder explanation = null)
	{
		bool immobile = false;
		if (!pawns.NullOrEmpty())
		{
			using ClearOnDispose<int> cod = new(MoveSpeedTicks);
			using ClearStringOnDispose csb = new(TicksExplanation);
			foreach (Pawn pawn in pawns)
			{
				bool ticksExplanation = explanation != null;
				if (pawn is VehiclePawn vehicle)
				{
					float worldSpeedMultiplier = vehicle.WorldSpeedMultiplier;
					float moveSpeed = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) *
						worldSpeedMultiplier / 60;
					if (moveSpeed > 0)
					{
						int ticksPerTile = TicksFromMoveSpeed(moveSpeed);
						MoveSpeedTicks.Add(ticksPerTile);

						if (ticksExplanation)
							TicksExplanation.AppendLine(
								$"  {vehicle.LabelCap}: {GenDate.TicksPerDay / ticksPerTile:0.#} {"TilesPerDay".Translate()}");
					}
					else
					{
						immobile = true;
						if (ticksExplanation)
							TicksExplanation.AppendLine($"  {vehicle.LabelCap}: 0 {"TilesPerDay".Translate()}");
					}
				}
				else if (!pawn.InVehicle())
				{
					float moveSpeed = ThingDefOf.Human.GetStatValueAbstract(StatDefOf.MoveSpeed) / 60f;
					int ticksPerTile = TicksFromMoveSpeed(moveSpeed);
					MoveSpeedTicks.Add(ticksPerTile);

					if (ticksExplanation)
						TicksExplanation.AppendLine(
							$"  {pawn.LabelCap}: {GenDate.TicksPerDay / ticksPerTile:0.#} {"TilesPerDay".Translate()}");
				}
			}
			float averageVehicleSpeed = float.MaxValue;
			if (MoveSpeedTicks.Count > 0 && !immobile)
			{
				averageVehicleSpeed = (float)MoveSpeedTicks.Average();
			}
			int averageVehicleTicks = Mathf.RoundToInt(averageVehicleSpeed);
			if (explanation != null)
			{
				explanation.AppendLine($"{"CaravanMovementSpeedFull".Translate()}:");
				explanation.AppendLine(TicksExplanation.ToString());
				if (massUsage > massCapacity)
				{
					explanation.AppendLine($"  {"MultiplierForCarriedMass".Translate()}");
				}
				explanation.AppendLine();
				float ticksPerDay = (float)GenDate.TicksPerDay / averageVehicleTicks;
				explanation.AppendLine(
					$"  {"Average".Translate()}: {ticksPerDay:0.#} {"TilesPerDay".Translate()}");
			}
			return averageVehicleTicks;
		}
		if (explanation != null)
		{
			AppendUsingDefaultTicksPerMoveInfo(explanation);
		}
		return DefaultTicksPerMove;
	}

	[MustUseReturnValue]
	public static int GetTicksPerMove(List<VehicleDef> vehicleDefs, StringBuilder explanation = null)
	{
		using ClearOnDispose<int> cod = new(MoveSpeedTicks);
		using ClearStringOnDispose csb = new(TicksExplanation);
		foreach (VehicleDef vehicleDef in vehicleDefs)
		{
			float worldSpeedMultiplier = vehicleDef.properties.worldSpeedMultiplier;
			float moveSpeed = vehicleDef.GetStatValueAbstract(VehicleStatDefOf.MoveSpeed) *
				worldSpeedMultiplier / 60;
			if (moveSpeed > 0)
			{
				int ticksPerTile = TicksFromMoveSpeed(moveSpeed);
				MoveSpeedTicks.Add(ticksPerTile);

				TicksExplanation.AppendLine(
					$"  {vehicleDef.LabelCap}: {GenDate.TicksPerDay / ticksPerTile:0.#} {"TilesPerDay".Translate()}");
			}
			else
			{
				TicksExplanation.AppendLine($"  {vehicleDef.LabelCap}: 0 {"TilesPerDay".Translate()}");
			}
		}
		float averageVehicleSpeed = float.MaxValue;
		if (MoveSpeedTicks.Count > 0)
		{
			averageVehicleSpeed = (float)MoveSpeedTicks.Average();
		}
		int averageVehicleTicks = Mathf.RoundToInt(averageVehicleSpeed);

		explanation?.AppendLine($"{"CaravanMovementSpeedFull".Translate()}:");
		explanation?.AppendLine(TicksExplanation.ToString());
		explanation?.AppendLine();
		float ticksPerDay = (float)GenDate.TicksPerDay / averageVehicleTicks;
		explanation?.AppendLine(
			$"  {"Average".Translate()}: {ticksPerDay:0.#} {"TilesPerDay".Translate()}");

		if (explanation != null)
			AppendUsingDefaultTicksPerMoveInfo(explanation);

		return averageVehicleTicks;
	}


	[MustUseReturnValue]
	public static int TicksFromMoveSpeed(float moveSpeedNormalized)
	{
		float moveSpeedRatio = 1f / moveSpeedNormalized;
		float tickSpeed = moveSpeedRatio * CellToTilesConversionRatio;
		int ticksPerTile = Mathf.Max(Mathf.RoundToInt(tickSpeed), 1);
		return ticksPerTile;
	}

	private static void AppendUsingDefaultTicksPerMoveInfo(StringBuilder explanation)
	{
		const float DefaultTilesPerDay = (float)GenDate.TicksPerDay / DefaultTicksPerMove;

		explanation.Append($"{"CaravanMovementSpeedFull".Translate()}:");
		explanation.AppendLine();
		explanation.Append(
			$"  {"Default".Translate()}: {DefaultTilesPerDay:0.#} {"TilesPerDay".Translate()}");
	}

	[MustUseReturnValue]
	public static float ApproxTilesPerDay(VehicleCaravan caravan, StringBuilder explanation = null)
	{
		if (caravan.AerialVehicle)
		{
			return 0;
		}
		return ApproxTilesPerDay(caravan.UniqueVehicleDefsInCaravan().ToList(), caravan.TicksPerMove,
			caravan.Tile, caravan.vehiclePather.Moving ? caravan.vehiclePather.NextTile : -1,
			explanation, explanation != null ? caravan.TicksPerMoveExplanation : null);
	}

	[MustUseReturnValue]
	public static float ApproxTilesPerDay(List<VehicleDef> vehicleDefs, int ticksPerMove,
		PlanetTile tile, PlanetTile nextTile,
		StringBuilder explanation = null,
		string caravanTicksPerMoveExplanation = null)
	{
		if (!nextTile.Valid)
		{
			nextTile = Find.WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(tile);
		}
		int ticksPerDay = Mathf.CeilToInt(VehicleCaravan_PathFollower.CostToMove(vehicleDefs,
			ticksPerMove, tile, nextTile, ticksAbs: null, explanation: explanation,
			caravanTicksPerMoveExplanation: caravanTicksPerMoveExplanation));
		return ticksPerDay > 0 ? 60000f / ticksPerDay : 0;
	}
}