using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles;

public struct TurretShotReport
{
  private TargetInfo target;
  private float distance;
  private List<CoverInfo> covers;

  private float coversOverallBlockChance;
  private float factorFromShooterAndDist;
  private float factorFromTurret;
  private float factorFromTargetSize;
  private float factorFromWeather;
  private float forcedMissRadius;
  private float offsetFromDarkness;
  private float factorFromCoveringGas;

  private ShootLine shootLine;

  public float AimOnTargetChance
  {
    get
    {
      float num = factorFromShooterAndDist * factorFromTurret * factorFromWeather *
        factorFromCoveringGas;
      num += offsetFromDarkness;
      if (num < 0.0201f)
      {
        num = 0.0201f;
      }
      return num;
    }
  }

  public float AimOnTargetChanceWithSize => AimOnTargetChance * factorFromTargetSize;

  public float PassCoverChance => 1f - coversOverallBlockChance;

  public float TotalEstimatedHitChance => Mathf.Clamp01(AimOnTargetChance * PassCoverChance);

  public ShootLine ShootLine => shootLine;

  public static TurretShotReport HitReportFor(VehiclePawn vehicle, VehicleTurret turret,
    LocalTargetInfo target, Pawn caster = null)
  {
    Map map = vehicle.Map;
    IntVec3 cell = target.Cell;
    Vector3 cellPos = cell.ToVector3Shifted();
    Vector3 turretPos = turret.DrawPosition(vehicle.FullRotation) + vehicle.DrawPos;
    IntVec3 turretCell = turretPos.ToIntVec3();
    TurretShotReport result = new()
    {
      distance = Vector2.Distance(new Vector2(cellPos.x, cellPos.z),
        new Vector2(turretPos.x, turretPos.z)),
      target = target.ToTargetInfo(map)
    };
    result.factorFromShooterAndDist = turret.CurrentFireMode.canMiss ?
      HitFactorFromShooter(caster, result.distance) :
      1;
    result.factorFromTurret = turret.CurrentFireMode.GetHitChanceFactor(result.distance);

    // Cover
    result.covers = CoverUtility.CalculateCoverGiverSet(target, turretCell, map);
    result.coversOverallBlockChance =
      CoverUtility.CalculateOverallBlockChance(target, turretCell, map);

    // Gas
    result.factorFromCoveringGas = 1f;
    if (turret.TryFindShootLineFromTo(turretCell, target, out result.shootLine))
    {
      foreach (IntVec3 item in result.shootLine.Points())
      {
        if (item.InBounds(map) && item.AnyGas(map, GasType.BlindSmoke))
        {
          result.factorFromCoveringGas = 0.7f;
          break;
        }
      }
    }
    else
    {
      result.shootLine = new ShootLine(IntVec3.Invalid, IntVec3.Invalid);
    }

    // Weather
    result.factorFromWeather =
      turretCell.Roofed(map) && target.Cell.Roofed(map) ?
        1 :
        map.weatherManager.CurWeatherAccuracyMultiplier;
    if (target.HasThing)
    {
      if (target.Thing is Pawn pawn)
      {
        result.factorFromTargetSize = pawn.BodySize;
      }
      else
      {
        result.factorFromTargetSize = target.Thing.def.fillPercent *
          target.Thing.def.size.x * target.Thing.def.size.z * 2.5f;
      }
      result.factorFromTargetSize = Mathf.Clamp(result.factorFromTargetSize, 0.5f, 2f);
    }
    else
    {
      result.factorFromTargetSize = 1f;
    }
    result.forcedMissRadius = turret.CurrentFireMode.forcedMissRadius;

    // Lighting
    result.offsetFromDarkness = 0f;
    if (ModsConfig.IdeologyActive && target.HasThing && caster != null)
    {
      if (DarknessCombatUtility.IsOutdoorsAndLit(target.Thing))
      {
        result.offsetFromDarkness =
          caster.GetStatValue(StatDefOf.ShootingAccuracyOutdoorsLitOffset);
      }
      else if (DarknessCombatUtility.IsOutdoorsAndDark(target.Thing))
      {
        result.offsetFromDarkness =
          caster.GetStatValue(StatDefOf.ShootingAccuracyOutdoorsDarkOffset);
      }
      else if (DarknessCombatUtility.IsIndoorsAndDark(target.Thing))
      {
        result.offsetFromDarkness =
          caster.GetStatValue(StatDefOf.ShootingAccuracyIndoorsDarkOffset);
      }
      else if (DarknessCombatUtility.IsIndoorsAndLit(target.Thing))
      {
        result.offsetFromDarkness = caster.GetStatValue(StatDefOf.ShootingAccuracyIndoorsLitOffset);
      }
    }
    return result;
  }

  private static float HitFactorFromShooter(Pawn caster, float distance)
  {
    if (caster == null)
      return 1;
    float statValue = caster.GetStatValue(StatDefOf.ShootingAccuracyPawn);
    statValue *= distance switch
    {
      < 0  => throw new ArgumentOutOfRangeException(nameof(distance)),
      <= 3 => caster.GetStatValue(StatDefOf.ShootingAccuracyFactor_Touch),
      <= 12 => Mathf.Lerp(caster.GetStatValue(StatDefOf.ShootingAccuracyFactor_Touch),
        caster.GetStatValue(StatDefOf.ShootingAccuracyFactor_Short),
        (distance - FireMode.DistanceTouch) / (FireMode.DistanceShort - FireMode.DistanceTouch)),
      <= 25 => Mathf.Lerp(caster.GetStatValue(StatDefOf.ShootingAccuracyFactor_Short),
        caster.GetStatValue(StatDefOf.ShootingAccuracyFactor_Medium),
        (distance - FireMode.DistanceShort) / (FireMode.DistanceMedium - FireMode.DistanceShort)),
      <= 40 => Mathf.Lerp(caster.GetStatValue(StatDefOf.ShootingAccuracyFactor_Medium),
        caster.GetStatValue(StatDefOf.ShootingAccuracyFactor_Long),
        (distance - FireMode.DistanceMedium) / (FireMode.DistanceLong - FireMode.DistanceMedium)),
      _ => caster.GetStatValue(StatDefOf.ShootingAccuracyFactor_Long)
    };
    return HitFactorFromShooter(statValue, distance);
  }

  private static float HitFactorFromShooter(float accRating, float distance)
  {
    return Mathf.Max(Mathf.Pow(accRating, distance), 0.02f);
  }

  internal string GetTextReadout()
  {
    StringBuilder stringBuilder = new();
    if (forcedMissRadius > 0.5f)
    {
      stringBuilder.AppendLine();
      stringBuilder.AppendLine($"{"ForcedMissRadius".Translate()}: {forcedMissRadius:F1}");
      stringBuilder.AppendLine(
        $"{"DirectHitChance".Translate()}: {(1f / GenRadial.NumCellsInRadius(forcedMissRadius)).ToStringPercent()}");
    }
    else
    {
      stringBuilder.AppendLine(TotalEstimatedHitChance.ToStringPercent());
      stringBuilder.AppendLine("   " + "ShootReportShooterAbility".Translate() + ": " +
        factorFromShooterAndDist.ToStringPercent());
      stringBuilder.AppendLine(
        $"   {"ShootReportWeapon".Translate()}: {factorFromTurret.ToStringPercent()}");
      if (target.HasThing && !Mathf.Approximately(factorFromTargetSize, 1f))
      {
        stringBuilder.AppendLine(
          $"   {"TargetSize".Translate()}: {factorFromTargetSize.ToStringPercent()}");
      }
      if (factorFromWeather < 0.99f)
      {
        stringBuilder.AppendLine(
          $"   {"Weather".Translate()}: {factorFromWeather.ToStringPercent()}");
      }
      if (factorFromCoveringGas < 0.99f)
      {
        stringBuilder.AppendLine(
          $"   {"BlindSmoke".Translate().CapitalizeFirst()}: {factorFromCoveringGas.ToStringPercent()}");
      }

      if (ModsConfig.IdeologyActive && target.HasThing &&
        !Mathf.Approximately(offsetFromDarkness, 0))
      {
        if (DarknessCombatUtility.IsOutdoorsAndLit(target.Thing))
        {
          stringBuilder.AppendLine(
            $"   {StatDefOf.ShootingAccuracyOutdoorsLitOffset.LabelCap}: {offsetFromDarkness.ToStringPercent()}");
        }
        else if (DarknessCombatUtility.IsOutdoorsAndDark(target.Thing))
        {
          stringBuilder.AppendLine(
            $"   {StatDefOf.ShootingAccuracyOutdoorsDarkOffset.LabelCap}: {offsetFromDarkness.ToStringPercent()}");
        }
        else if (DarknessCombatUtility.IsIndoorsAndDark(target.Thing))
        {
          stringBuilder.AppendLine(
            $"   {StatDefOf.ShootingAccuracyIndoorsDarkOffset.LabelCap}: {offsetFromDarkness.ToStringPercent()}");
        }
        else if (DarknessCombatUtility.IsIndoorsAndLit(target.Thing))
        {
          stringBuilder.AppendLine(
            $"   {StatDefOf.ShootingAccuracyIndoorsLitOffset.LabelCap}: {offsetFromDarkness.ToStringPercent()}");
        }
      }
      if (PassCoverChance < 1f)
      {
        stringBuilder.AppendLine(
          $"   {"ShootingCover".Translate()}: {PassCoverChance.ToStringPercent()}");
        foreach (CoverInfo coverInfo in covers)
        {
          if (coverInfo.BlockChance > 0f)
          {
            stringBuilder.AppendLine($"     {"CoverThingBlocksPercentOfShots"
             .Translate(coverInfo.Thing.LabelCap, coverInfo.BlockChance.ToStringPercent(),
                new NamedArgument(coverInfo.Thing.def, "COVER")).CapitalizeFirst()}");
          }
        }
      }
      else
      {
        stringBuilder.AppendLine($"   ({"NoCoverLower".Translate()})");
      }
    }
    return stringBuilder.ToString();
  }

  public Thing GetRandomCoverToMissInto()
  {
    return covers.TryRandomElementByWeight(cover => cover.BlockChance, out CoverInfo result) ?
      result.Thing :
      null;
  }
}