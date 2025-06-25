using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
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

  private static readonly List<int> moveSpeedTicks = [];

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
    return GetTicksPerMove(caravanInfo.pawns, caravanInfo.massUsage, caravanInfo.massCapacity,
      explanation);
  }

  [MustUseReturnValue]
  public static int GetTicksPerMove(List<Pawn> pawns, float massUsage, float massCapacity,
    StringBuilder explanation = null)
  {
    bool immobile = false;
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
          float moveSpeed = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) *
            worldSpeedMultiplier / 60;
          if (moveSpeed > 0)
          {
            int ticksPerTile = TicksFromMoveSpeed(moveSpeed);
            moveSpeedTicks.Add(ticksPerTile);
            ticksExplanation?.AppendLine(
              $"  {vehicle.LabelCap}: {GenDate.TicksPerDay / ticksPerTile:0.#} {"TilesPerDay".Translate()}");
          }
          else
          {
            immobile = true;
            ticksExplanation?.AppendLine($"  {vehicle.LabelCap}: 0 {"TilesPerDay".Translate()}");
          }
        }
        else if (!pawn.InVehicle())
        {
          float moveSpeed = ThingDefOf.Human.GetStatValueAbstract(StatDefOf.MoveSpeed) / 60f;
          int ticksPerTile = TicksFromMoveSpeed(moveSpeed);
          moveSpeedTicks.Add(ticksPerTile);
          ticksExplanation?.AppendLine(
            $"  {pawn.LabelCap}: {GenDate.TicksPerDay / ticksPerTile:0.#} {"TilesPerDay".Translate()}");
        }
      }
      float averageVehicleSpeed = float.MaxValue;
      if (moveSpeedTicks.Count > 0 && !immobile)
      {
        averageVehicleSpeed = (float)moveSpeedTicks.Average();
      }
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
      caravan.Tile, caravan.vehiclePather.Moving ? caravan.vehiclePather.nextTile : -1,
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
      ticksPerMove,
      tile, nextTile, ticksAbs: null, explanation: explanation,
      caravanTicksPerMoveExplanation: caravanTicksPerMoveExplanation));
    return ticksPerDay > 0 ? 60000f / ticksPerDay : 0;
  }
}