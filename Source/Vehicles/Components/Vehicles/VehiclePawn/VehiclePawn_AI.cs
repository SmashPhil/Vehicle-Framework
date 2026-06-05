using System;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

public partial class VehiclePawn
{
  [Unsaved]
  public VehicleAI vehicleAI;

  public VehiclePathFollower vehiclePather;
  public VehicleIgnitionController ignition;

  public SharedJob sharedJob;

  public FishingTracker fishingTracker;

  [Obsolete("Use fishingTracker.IsFishing instead.")]
  public bool IsFishing => fishingTracker.IsFishing;

  public virtual bool DeconstructibleBy(Faction faction)
  {
    return DebugSettings.godMode || Faction == faction;
  }

  // TODO - clean up and add gizmo for claiming to VehiclePawn class
  public override AcceptanceReport ClaimableBy(Faction faction)
  {
    if (!def.Claimable)
    {
      return false;
    }
    if (Faction != null)
    {
      if (Faction == faction)
      {
        return false;
      }
      if (faction == Faction.OfPlayer)
      {
        if (Faction == Faction.OfInsects)
        {
          if (HiveUtility.AnyHivePreventsClaiming(this))
          {
            return false;
          }
        }
        else
        {
          if (Faction == Faction.OfMechanoids)
          {
            return false;
          }
          if (Spawned && AnyHostileToolUserOfFaction(Faction))
          {
            return false;
          }
        }
      }
    }
    else if (Spawned && Map.ParentFaction != null && Map.ParentFaction != Faction.OfPlayer &&
      Map.ParentFaction.def.humanlikeFaction && AnyHostileToolUserOfFaction(Map.ParentFaction))
    {
      return false;
    }
    return true;

    bool AnyHostileToolUserOfFaction(Faction ofFaction)
    {
      if (!Spawned)
      {
        return false;
      }
      foreach (Pawn pawn in Map.mapPawns.SpawnedPawnsInFaction(ofFaction))
      {
        if (pawn.RaceProps.ToolUser && GenHostility.IsPotentialThreat(pawn))
        {
          return true;
        }
      }
      return false;
    }
  }

  /// <summary>
  /// Postfixed to <see cref="Pawn.ThreatDisabled(IAttackTargetSearcher)"/>, call that method instead. It will fall through to this one should it still evaluate to false
  /// </summary>
  internal bool IsThreatToAttackTargetSearcher(IAttackTargetSearcher attackTargetSearcher)
  {
    if (AllPawnsAboard.Count > 0)
    {
      return true;
    }
    foreach (ThingComp thingComp in AllComps)
    {
      if (thingComp is VehicleComp vehicleComp && vehicleComp.IsThreat(attackTargetSearcher))
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Claim vehicle's position and all cells within its hitbox.
  /// </summary>
  /// <remarks>Shorthand for <see cref="VehiclePositionManager.ClaimPosition(VehiclePawn)"/></remarks>
  public void ReclaimPosition()
  {
    Map.GetDetachedMapComponent<VehiclePositionManager>().ClaimPosition(this);
  }

  // TODO 1.7 - Remove and use EventManager instead. These types of notify functions are nonsense,
  // and old carryovers from RimWorld
  public new virtual void Notify_Teleported(bool endCurrentJob = true, bool resetTweenedPos = true)
  {
    if (resetTweenedPos)
    {
      DrawTracker.tweener.ResetTweenedPosToRoot();
    }
    vehiclePather.Notify_Teleported();
    this.CalculateAngle();
    if (endCurrentJob && jobs != null && jobs.curJob != null)
    {
      jobs.EndCurrentJob(JobCondition.InterruptForced);
    }
  }

  public virtual AcceptanceReport CanDraft()
  {
    foreach (ThingComp thingComp in AllComps)
    {
      if (thingComp is VehicleComp vehicleComp)
      {
        AcceptanceReport report = vehicleComp.CanDraft();
        if (!report.Accepted)
        {
          return report;
        }
      }
    }
    if (!HasEnoughOperators)
    {
      return "VF_NotEnoughToOperate".Translate(this);
    }
    return true;
  }

  public int TotalAllowedFor(JobDef jobDef)
  {
    if (!VehicleMod.settings.main.multiplePawnsPerJob)
    {
      return 1;
    }

    foreach (VehicleJobLimitations jobLimit in VehicleDef.properties.vehicleJobLimitations)
    {
      if (jobLimit.defName == jobDef.defName)
      {
        return jobLimit.maxWorkers;
      }
    }
    return 1;
  }

  public void BeachShip()
  {
    movementStatus = VehicleMovementStatus.Offline;
    beached = true;
  }

  public void RemoveBeachedStatus()
  {
    movementStatus = VehicleMovementStatus.Online;
    beached = false;
  }
}