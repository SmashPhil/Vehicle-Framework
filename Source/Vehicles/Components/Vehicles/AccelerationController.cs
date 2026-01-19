using System;
using JetBrains.Annotations;
using SmashTools;
using SmashTools.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Config;
using Verse;
using Verse.Sound;
using static Vehicles.Config.FeatureFlags;

namespace Vehicles;

internal enum Throttle
{
  Idle,
  Decelerate,
  Coast,
  Accelerate
}

internal enum ThrottleSpeed
{
  Normal,
  Fast,
  Urgent
}

internal sealed class AccelerationController : IExposable
{
  public const float MinSpeed = 0.5f;

  private readonly VehiclePathFollower pathFollower;
  private readonly VehiclePawn vehicle;

  private bool decelerated;
  private float moveSpeed;
  private int moveTick;
  private int nodesToDecelerate;
  private Throttle throttle = Throttle.Idle;
  private ThrottleSpeed accelSpeed = ThrottleSpeed.Normal;
  private ThrottleSpeed decelSpeed = ThrottleSpeed.Normal;

  private Path path;
  private float acceleration;
  private Sustainer brakesSustainer;

  public AccelerationController(VehiclePathFollower pathFollower)
  {
    this.pathFollower = pathFollower;
    vehicle = pathFollower.vehicle;
  }

  private float TargetMoveSpeed { get; set; }

  public int NodesToDecelerate => nodesToDecelerate;

  private ThrottleSpeed ThrottleSpeed => throttle == Throttle.Decelerate ? decelSpeed : accelSpeed;

  public float MoveSpeed
  {
    get
    {
      return moveSpeed;
    }
    private set
    {
      moveSpeed = value;
    }
  }

  public float MoveSpeedPct
  {
    get
    {
      float maxMoveSpeed = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed);
      if (maxMoveSpeed <= 0)
        return 0;

      return MoveSpeed / maxMoveSpeed;
    }
  }

  private float Acceleration
  {
    get
    {
      if (throttle == Throttle.Decelerate)
      {
        return acceleration * -SpeedFactor(ThrottleSpeed);
      }
      return acceleration * SpeedFactor(ThrottleSpeed);
    }
    set
    {
      acceleration = value;
    }
  }

  private bool ShouldDecelerateNow()
  {
    if (path == null || throttle is Throttle.Decelerate or Throttle.Idle)
      return false;
    if (NodesToDecelerate == 0)
      return false;

    float cost = CostToAccelerateAt(MoveSpeed, target: 0, decelSpeed);
    return pathFollower.PathCostLeft < cost || Mathf.Abs(cost - pathFollower.PathCostLeft) < 1;
  }

  private ThrottleSpeed GetSpeedToDecelerate(ThrottleSpeed targetSpeed)
  {
    if (acceleration == 0)
      return ThrottleSpeed.Normal;

    if (targetSpeed <= ThrottleSpeed.Normal && CanDecelerateAt(ThrottleSpeed.Normal))
      return ThrottleSpeed.Normal;
    if (targetSpeed <= ThrottleSpeed.Fast && CanDecelerateAt(ThrottleSpeed.Fast))
      return ThrottleSpeed.Fast;

    return ThrottleSpeed.Urgent;
  }

  private bool CanDecelerateAt(ThrottleSpeed speedEst)
  {
    float costLeft = pathFollower.PathCostLeft;
    // Anticipate stopping with a little bit of speed left so the vehicle isn't crawling for the last few ticks.
    const float MarginOfError = 2;

    float cost = CostToAccelerateAt(MoveSpeed, target: 0, speedEst);
    if (cost < costLeft)
      return true;

    return cost - costLeft < MarginOfError;
  }

  private float CostToAccelerateAt(float speed, float target, ThrottleSpeed speedEst)
  {
    // (V_0^2 - V_f^2) / (2 * a * V_max)
    float rate = vehicle.GetStatValue(VehicleStatDefOf.AccelerationRate) * SpeedFactor(speedEst);
    return (Mathf.Pow(speed, 2) - Mathf.Pow(target, 2)) /
           (2 * rate * vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed));
  }

  private static float SpeedFactor(ThrottleSpeed speed)
  {
    return speed switch
    {
      ThrottleSpeed.Normal => 1,
      ThrottleSpeed.Fast => 2,
      ThrottleSpeed.Urgent => 4,
      _ => throw new NotImplementedException()
    };
  }

  public void Accelerate([NotNull] Path curPath, ThrottleSpeed accelThrottle, ThrottleSpeed decelThrottle)
  {
    path = curPath;
    throttle = Throttle.Accelerate;
    accelSpeed = accelThrottle;
    decelSpeed = GetSpeedToDecelerate(decelThrottle);
    Recalculate();
    CalculateDecelerationFromEnd();
  }

  public void DecelerateNow(ThrottleSpeed speedEst)
  {
    decelSpeed = GetSpeedToDecelerate(speedEst);
    Decelerate();
    CalculateDecelerationFromCurrent();
  }

  private void Decelerate()
  {
    Assert.IsNotNull(path);
    throttle = Throttle.Decelerate;
    if (decelSpeed > ThrottleSpeed.Fast)
    {
      SpawnBrakeSustainer();
    }
    Recalculate();
  }

  private void Coast()
  {
    MoveSpeed = TargetMoveSpeed;
    throttle = Throttle.Coast;
  }

  private void SpawnBrakeSustainer()
  {
    if (!VehicleMod.settings.main.useHandBrakes)
      return;

    if (brakesSustainer is not { Ended: false })
    {
      SoundInfo soundInfo = SoundInfo.InMap(vehicle, MaintenanceType.PerTick);
      brakesSustainer = SoundDefOf_Vehicles.TireScreech.TrySpawnSustainer(soundInfo);
    }
  }

  private void Reset()
  {
    path = null;
    throttle = Throttle.Idle;
    accelSpeed = ThrottleSpeed.Normal;
    decelSpeed = ThrottleSpeed.Normal;
    Recalculate();
  }

  private void Recalculate()
  {
    Acceleration = IsFeatureEnabled(FeatureFlags.Acceleration) ?
      vehicle.GetStatValue(VehicleStatDefOf.AccelerationRate) :
      0;
    switch (throttle)
    {
      case Throttle.Accelerate:
        TargetMoveSpeed = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed);
        break;
      case Throttle.Decelerate:
        TargetMoveSpeed = 0;
        break;
      case Throttle.Idle:
      default:
        TargetMoveSpeed = 0;
        moveTick = 0;
        MoveSpeed = 0;
        acceleration = 0;
        break;
    }

    if (Mathf.Approximately(Acceleration, 0))
    {
      Coast();
    }
  }

  private void CalculateDecelerationFromCurrent()
  {
    if (acceleration <= 0)
    {
      nodesToDecelerate = 0;
      return;
    }
    if (path.NodesLeft <= 2)
    {
      nodesToDecelerate = path.NodesLeft;
      return;
    }

    float maxMoveSpeed = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed);
    float simulatedSpeed = MoveSpeed;
    int3 from = vehicle.Position;
    for (int i = path.Current; i >= 0; i--)
    {
      int3 to = path.Nodes[i];
      float cost = pathFollower.CostToMoveIntoCell(from, to);
      from = to;

      while (cost > 0 && simulatedSpeed > 0)
      {
        simulatedSpeed -= acceleration * SpeedFactor(decelSpeed);
        float costToPay = simulatedSpeed / maxMoveSpeed;
        cost -= costToPay;
      }
      // 'Next cell' transition in path follower applies 1 tick of acceleration
      simulatedSpeed -= acceleration;

      if (simulatedSpeed <= 0)
      {
        nodesToDecelerate = path.Current - i - 1;
        break;
      }
    }
  }

  private void CalculateDecelerationFromEnd()
  {
    if (acceleration <= 0)
    {
      nodesToDecelerate = 0;
      return;
    }
    if (path.NodesLeft <= 2)
    {
      nodesToDecelerate = path.NodesLeft;
      return;
    }

    float maxMoveSpeed = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed);
    float simulatedSpeed = 0;
    int3 from = path.LastNode;
    for (int i = 1; i <= path.Current; i++)
    {
      int3 to = path.Nodes[i];
      float cost = pathFollower.CostToMoveIntoCell(from, to);
      from = to;

      while (cost > 0)
      {
        simulatedSpeed += acceleration * SpeedFactor(decelSpeed);
        float costToPay = simulatedSpeed / maxMoveSpeed;
        cost -= costToPay;
      }
      // 'Next cell' transition in path follower applies 1 tick of acceleration
      simulatedSpeed += acceleration;

      if (simulatedSpeed >= TargetMoveSpeed)
      {
        nodesToDecelerate = i - 1;
        break;
      }
    }
  }

  public void Tick()
  {
    if (throttle == Throttle.Idle)
      return;

    if (ShouldDecelerateNow())
    {
      Decelerate();
    }

    moveTick++;
    if (throttle != Throttle.Coast)
    {
      MoveSpeed = Mathf.Clamp(MoveSpeed + Acceleration, min: MinSpeed, vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed));
      if (MoveSpeed > TargetMoveSpeed && throttle != Throttle.Decelerate)
      {
        Coast();
      }
    }

    if (brakesSustainer is { Ended: false })
    {
      brakesSustainer.Maintain();
      if (Mathf.Approximately(MoveSpeed, MinSpeed))
      {
        brakesSustainer.End();
        brakesSustainer = null;
      }
    }
  }

  internal void RegisterEvents()
  {
    vehicle.AddEvent(VehicleEventDefOf.MoveStop, Reset);
  }

  void IExposable.ExposeData()
  {
    Scribe_Values.Look(ref decelerated, nameof(decelerated));
    Scribe_Values.Look(ref moveSpeed, nameof(moveSpeed));
    Scribe_Values.Look(ref moveTick, nameof(moveTick));
    Scribe_Values.Look(ref nodesToDecelerate, nameof(nodesToDecelerate));
    Scribe_Values.Look(ref throttle, nameof(throttle));
    Scribe_Values.Look(ref accelSpeed, nameof(accelSpeed));
    Scribe_Values.Look(ref decelSpeed, nameof(decelSpeed));

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      Recalculate();
    }
  }
}