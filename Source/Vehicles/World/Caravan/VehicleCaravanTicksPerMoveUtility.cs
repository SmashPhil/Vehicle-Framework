using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CoreLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Performance;
using UnityEngine;
using UnityEngine.Assertions;
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

  private static readonly StringBuilder VehicleTicksExplanation = new();

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static float BaseHumanlikeTileSpeed()
  {
    float speed = GenTicks.TicksPerRealSecond / ThingDefOf.Human.GetStatValueAbstract(StatDefOf.MoveSpeed);
    return Mathf.RoundToInt(speed) * CaravanTicksPerMoveUtility.CellToTilesConversionRatio;
  }

  [Obsolete("Use MoveSpeedToTicks with the raw pawn speed instead of hardcoded normalization.")]
  [MustUseReturnValue, MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int TicksFromMoveSpeed(float moveSpeedNormalized)
  {
    float moveSpeedRatio = 1f / moveSpeedNormalized;
    float tickSpeed = moveSpeedRatio * CellToTilesConversionRatio;
    int ticksPerTile = Mathf.Max(Mathf.RoundToInt(tickSpeed), 1);
    return ticksPerTile;
  }

  [MustUseReturnValue]
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float MoveSpeedToTileSpeed(float moveSpeed)
  {
    return Mathf.RoundToInt(GenTicks.TicksPerRealSecond / moveSpeed) *
           CaravanTicksPerMoveUtility.CellToTilesConversionRatio;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static float GetMoveSpeedFactorFromMass(float massUsage, float massCapacity)
  {
    if (massCapacity <= 0f)
      return 1f;

    float t = massUsage / massCapacity;
    return Mathf.Lerp(2f, 1f, t);
  }

  // TODO 1.7 - Remove null case for caravan, it's unused
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
  public static int GetTicksPerMove(VehicleCaravanInfo caravanInfo, StringBuilder explanation = null)
  {
    return GetTicksPerMove(caravanInfo.vehiclesAndDismountedPawns, caravanInfo.massUsage, caravanInfo.massCapacity,
      explanation);
  }

  [MustUseReturnValue]
  public static int GetTicksPerMove(List<Pawn> pawns, float massUsage, float massCapacity,
    StringBuilder explanation = null)
  {
    if (pawns.NullOrEmpty())
    {
      if (explanation != null)
      {
        AppendUsingDefaultTicksPerMoveInfo(explanation);
      }
      return DefaultTicksPerMove;
    }

    CaravanSummary summary = GetTicksPerMoveSummary(pawns, massUsage, massCapacity);
    if (explanation != null)
    {
      explanation.Append($"{"CaravanMovementSpeedFull".Translate()}:");
      foreach (VehicleSummary vehicle in summary.vehicles)
      {
        float vehicleTicksPerDay = vehicle.immobile ? 0 : GenDate.TicksPerDay / vehicle.ticksToMove;
        explanation.AppendInNewLine(
          $"  {vehicle.label}: {vehicleTicksPerDay:0.#} {"TilesPerDay".Translate()}");
      }

      bool hasExtraBonus = !Mathf.Approximately(summary.extraSpeedBonus, 1f);
      if (hasExtraBonus)
      {
        explanation.AppendInNewLine(
          $"  {"MultiplierFromMoveSpeedBonus".Translate()}: {summary.extraSpeedBonus.ToStringPercent()}");
      }
      
      if (summary.dismounted > 0)
      {
        explanation.AppendLine();
        explanation.AppendInNewLine($"{"CaravanColonists".Translate()}:");
        float baseValue = GenDate.TicksPerDay / summary.dismountedSpeed;
        explanation.AppendInNewLine(
          $"  {"StatsReport_BaseValue".Translate()}: {baseValue:0.#} {"TilesPerDay".Translate()}");
        if (summary.rideableAnimals > 0)
        {
          explanation.AppendInNewLine(
            $"  {"RideableAnimalsPerPeople".Translate()}: {summary.dismounted} / {summary.rideableAnimals}");
          explanation.AppendInNewLine(
            $"  {"MultiplierFromRiddenAnimals".Translate()}: {summary.animalSpeedBonus.ToStringPercent()}");
        }
        if (hasExtraBonus)
        {
          explanation.AppendInNewLine(
            $"  {"MultiplierFromMoveSpeedBonus".Translate()}: {summary.extraSpeedBonus.ToStringPercent()}");
        }
        if (massUsage <= massCapacity)
        {
          explanation.AppendInNewLine(
            $"  {"MultiplierForCarriedMass".Translate(summary.massUsageSpeedBonus.ToStringPercent())}");
        }
      }

      float ticksPerDay = !summary.immobile ? (float)GenDate.TicksPerDay / summary.TicksPerMove : 0;
      explanation.AppendLine();
      explanation.AppendInNewLine(
        $"{"Average".Translate()}: {ticksPerDay:0.#} {"TilesPerDay".Translate()}");
    }
    return summary.TicksPerMove;
  }

  [MustUseReturnValue]
  public static int GetTicksPerMove(List<VehicleDef> vehicleDefs, StringBuilder explanation = null)
  {
    (int count, float total) speed = new();
    using ClearStringOnDispose csb = new(VehicleTicksExplanation);
    foreach (VehicleDef vehicleDef in vehicleDefs)
    {
      float moveSpeed = vehicleDef.GetStatValueAbstract(VehicleStatDefOf.MoveSpeed) *
                        vehicleDef.properties.worldSpeedMultiplier;
      if (moveSpeed > 0)
      {
        float ticksPerTile = MoveSpeedToTileSpeed(moveSpeed);
        speed.total += ticksPerTile;
        speed.count++;

        VehicleTicksExplanation.AppendLine(
          $"  {vehicleDef.LabelCap}: {GenDate.TicksPerDay / ticksPerTile:0.#} {"TilesPerDay".Translate()}");
      }
      else
      {
        VehicleTicksExplanation.AppendLine($"  {vehicleDef.LabelCap}: 0 {"TilesPerDay".Translate()}");
      }
    }
    float averageVehicleSpeed = float.MaxValue;
    if (speed.count > 0)
    {
      averageVehicleSpeed = speed.total / speed.count;
    }
    int averageVehicleTicks = Mathf.RoundToInt(averageVehicleSpeed);

    explanation?.AppendLine($"{"CaravanMovementSpeedFull".Translate()}:");
    explanation?.AppendLine(VehicleTicksExplanation.ToString());
    explanation?.AppendLine();
    float ticksPerDay = (float)GenDate.TicksPerDay / averageVehicleTicks;
    explanation?.AppendLine(
      $"  {"Average".Translate()}: {ticksPerDay:0.#} {"TilesPerDay".Translate()}");

    if (explanation != null)
      AppendUsingDefaultTicksPerMoveInfo(explanation);

    return averageVehicleTicks;
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
    TicksPerMoveData data = new()
    {
      ticksPerMove = caravan.TicksPerMove,
      tile = caravan.Tile,
      nextTile = caravan.vehiclePather.Moving ? caravan.vehiclePather.NextTile : PlanetTile.Invalid,
      explanation = explanation,
      caravanTicksPerMoveExplanation = explanation != null ? caravan.TicksPerMoveExplanation : null
    };
    return ApproxTilesPerDay(caravan.VehiclesListForReading, data);
  }

  [MustUseReturnValue, Obsolete]
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
    return ticksPerDay > 0 ? (float)GenDate.TicksPerDay / ticksPerDay : 0;
  }

  [MustUseReturnValue]
  public static float ApproxTilesPerDay(List<VehiclePawn> vehicles, TicksPerMoveData data)
  {
    if (!data.nextTile.Valid)
    {
      data.nextTile = FindReasonableAdjacentTile(vehicles, data.tile);
    }
    int ticksPerDay = Mathf.CeilToInt(VehicleCaravan_PathFollower.CostToMove(vehicles, in data));
    return ticksPerDay > 0 ? (float)GenDate.TicksPerDay / ticksPerDay : 0;

    static PlanetTile FindReasonableAdjacentTile(List<VehiclePawn> vehicles, PlanetTile tile)
    {
      SurfaceTile surfaceTile = (SurfaceTile)Find.WorldGrid[tile];
      using var ls = GlobalObjectPool.Get(out List<PlanetTile> neighborTiles);
      Find.WorldGrid.GetTileNeighbors(tile, neighborTiles);
      foreach (PlanetTile neighbor in neighborTiles)
      {
        bool passableAll = true;
        foreach (VehiclePawn vehicle in vehicles)
        {
          passableAll &= WorldVehiclePathGrid.Instance.PassableFast(neighbor, vehicle.VehicleDef);
        }

        if (passableAll)
        {
          return neighbor;
        }
      }
      return tile;
    }
  }

  private static float AverageTopAnimalSpeedFactors(List<float> animalSpeedFactors, int riderCount)
  {
    if (animalSpeedFactors.Count == 0 || riderCount <= 0)
    {
      return 1f;
    }
    animalSpeedFactors.Sort(static (left, right) => right.CompareTo(left));
    int usableAnimals = Mathf.Min(animalSpeedFactors.Count, riderCount);
    float total = riderCount - usableAnimals;
    for (int i = 0; i < usableAnimals; i++)
    {
      total += animalSpeedFactors[i];
    }
    return total / riderCount;
  }

  public static CaravanSummary GetTicksPerMoveSummary(List<Pawn> pawns, float massUsage, float massCapacity)
  {
    CaravanSummary summary = new();
    (int count, float total) extraOffset = new();
    using var ls = GlobalObjectPool.Get(out List<float> animalSpeedFactors);
    foreach (Pawn pawn in pawns)
    {
      if (pawn is VehiclePawn vehicle)
      {
        foreach (Pawn occupant in vehicle.AllPawnsAboard)
        {
          if (!occupant.RaceProps.Humanlike || pawn.Downed)
            continue;

          float speedFactor = occupant.GetStatValue(StatDefOf.CaravanBonusSpeedFactor);
          if (speedFactor > 1)
          {
            extraOffset.total += speedFactor;
            extraOffset.count++;
          }
        }

        VehicleSummary vehicleSummary = new(vehicle);
        summary.immobile |= vehicleSummary.immobile;
        if (!vehicleSummary.immobile)
        {
          summary.vehicles.Add(vehicleSummary);
          summary.vehicleSpeed += vehicleSummary.ticksToMove;
        }
      }
      else if (!pawn.InVehicle())
      {
        if (!CaravanHelper.assignedSeats.IsAssigned(pawn))
        {
          if (pawn.IsCaravanRideable())
          {
            animalSpeedFactors.Add(pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor));
          }
          else
          {
            summary.dismounted++;
          }
        }
        float speedFactor = pawn.GetStatValue(StatDefOf.CaravanBonusSpeedFactor);
        if (speedFactor > 1)
        {
          extraOffset.total += speedFactor;
          extraOffset.count++;
        }
      }
    }

    summary.rideableAnimals = animalSpeedFactors.Count;
    if (animalSpeedFactors.Count > 0 && summary.dismounted > 0)
    {
      summary.animalSpeedBonus = AverageTopAnimalSpeedFactors(animalSpeedFactors, summary.dismounted);
    }
    summary.dismountedSpeed = MoveSpeedToTileSpeed(ThingDefOf.Human.GetStatValueAbstract(StatDefOf.MoveSpeed));
    if (extraOffset.total > 0)
    {
      summary.extraSpeedBonus = extraOffset.total / extraOffset.count;
    }
    summary.massUsageSpeedBonus = GetMoveSpeedFactorFromMass(massUsage, massCapacity);
    return summary;
  }

  public struct CaravanSummary()
  {
    public bool immobile;
    public int dismounted;
    public List<VehicleSummary> vehicles = [];
    public int rideableAnimals;

    public float dismountedSpeed;
    public float vehicleSpeed;

    public float extraSpeedBonus = 1;
    public float animalSpeedBonus = 1;
    public float massUsageSpeedBonus = 1;

    public float DismountedFinalSpeed
    {
      get
      {
        if (dismounted == 0)
          return 0;

        return dismountedSpeed / (extraSpeedBonus * animalSpeedBonus * massUsageSpeedBonus);
      }
    }

    public float VehicleFinalSpeed
    {
      get
      {
        if (vehicles.Count == 0)
          return 0;

        return vehicleSpeed / extraSpeedBonus;
      }
    }

    public float AverageFinalSpeed
    {
      get
      {
        int count = dismounted + vehicles.Count;
        if (count == 0)
          return 0;

        return (DismountedFinalSpeed * dismounted + VehicleFinalSpeed) / count;
      }
    }

    public int TicksPerMove
    {
      get
      {
        if (immobile)
          return 0;

        return Mathf.RoundToInt(Mathf.Max(AverageFinalSpeed, 1));
      }
    }
  }

  public struct VehicleSummary
  {
    public string label;
    public float ticksToMove;
    public bool immobile;

    internal VehicleSummary(VehiclePawn vehicle)
    {
      label = vehicle.LabelCap;
      float moveSpeed = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) *
                        vehicle.WorldSpeedMultiplier;
      ticksToMove = MoveSpeedToTileSpeed(moveSpeed);
      immobile = moveSpeed <= 0 || !vehicle.CanMove;
    }
  }
}