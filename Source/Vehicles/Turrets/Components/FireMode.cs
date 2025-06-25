using System;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

/// <summary>
/// FireMode selection option for VehicleTurret
/// </summary>
/// <remarks>XML Notation: (shotsPerBurst, ticksBetweenShots, ticksBetweenBursts, label, texPath)</remarks>
[PublicAPI]
public record FireMode
{
  public const float DistanceTouch = 3;
  public const float DistanceShort = 12;
  public const float DistanceMedium = 25;
  public const float DistanceLong = 40;

  public string label;
  public string texPath;

  [TweakField(SettingsType = UISettingsType.IntegerBox)]
  public IntRange shotsPerBurst;

  [TweakField(SettingsType = UISettingsType.IntegerBox)]
  public int ticksBetweenShots;

  [TweakField(SettingsType = UISettingsType.IntegerBox)]
  public IntRange ticksBetweenBursts;

  [TweakField(SettingsType = UISettingsType.IntegerBox)]
  public int burstsTillWarmup = 1;


  [TweakField(SettingsType = UISettingsType.Checkbox)]
  public bool canMiss = true;

  [TweakField(SettingsType = UISettingsType.Checkbox)]
  public bool applyShooterAccuracy;

  [TweakField(SettingsType = UISettingsType.FloatBox)]
  [LoadAlias("spreadRadius")]
  public float forcedMissRadius = -1;

  [TweakField(SettingsType = UISettingsType.SliderPercent)]
  public float accuracyTouch = 1;

  [TweakField(SettingsType = UISettingsType.SliderPercent)]
  public float accuracyShort = 1;

  [TweakField(SettingsType = UISettingsType.SliderPercent)]
  public float accuracyMedium = 1;

  [TweakField(SettingsType = UISettingsType.SliderPercent)]
  public float accuracyLong = 1;

  [Unsaved]
  private Texture2D icon;

  public Texture2D Icon
  {
    get
    {
      if (icon is null && !string.IsNullOrEmpty(texPath))
      {
        icon = ContentFinder<Texture2D>.Get(texPath);
        icon ??= BaseContent.BadTex;
      }
      return icon;
    }
  }

  public int RoundsPerMinute
  {
    get
    {
      if (ticksBetweenBursts.TrueMin > ticksBetweenShots)
      {
        float roundsPerSecond = 60f / ticksBetweenShots;
        float secondsPerBurst = shotsPerBurst.Average / roundsPerSecond;
        float totalBurstCycle = secondsPerBurst + ticksBetweenBursts.TrueMin.TicksToSeconds();
        float burstsPerMinute = 60f / totalBurstCycle;
        return Mathf.RoundToInt(burstsPerMinute * shotsPerBurst.Average);
      }
      return Mathf.RoundToInt(3600f / ticksBetweenShots);
    }
  }

  public bool IsValid
  {
    get { return shotsPerBurst.TrueMin > 0; }
  }

  public float GetHitChanceFactor(float distance)
  {
    return distance switch
    {
      < 0  => throw new ArgumentOutOfRangeException(nameof(distance)),
      <= 3 => accuracyTouch,
      <= 12 => Mathf.Lerp(accuracyTouch, accuracyShort,
        (distance - DistanceTouch) / (DistanceShort - DistanceTouch)),
      <= 25 => Mathf.Lerp(accuracyShort, accuracyMedium,
        (distance - DistanceShort) / (DistanceMedium - DistanceShort)),
      <= 40 => Mathf.Lerp(accuracyMedium, accuracyLong,
        (distance - DistanceMedium) / (DistanceLong - DistanceMedium)),
      _ => accuracyLong
    };
  }
}