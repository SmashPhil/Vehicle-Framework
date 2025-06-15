using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

public class DeferredGridGeneration
{
  private const int DaysUnusedForRemoval = 3;

  private readonly VehiclePathingSystem mapping;

  private readonly GridCounter pathGridCounter = new();

  private bool PassDisabled { get; set; }

  public DeferredGridGeneration(VehiclePathingSystem mapping)
  {
    this.mapping = mapping;
  }

  private bool GridGenIsValid()
  {
    return !mapping.map.Disposed;
  }

  internal void GenerateAllPathGrids()
  {
    // We don't want this to change mid execution, either queue all or none
    bool deferred = mapping.ThreadAvailable;
    AsyncLongOperationAction longOperation =
      deferred ? AsyncPool<AsyncLongOperationAction>.Get() : null;
    bool anyRequest = false;
    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
    {
      if (deferred)
        anyRequest |= TryAddPathGridRequest(vehicleDef, longOperation);
      else
        GeneratePathGridFor(vehicleDef);
    }

    if (deferred)
    {
      if (anyRequest)
        FinalizeAndSendLongOp(longOperation);
      else
        longOperation.ReturnToPool();
    }
  }

  internal void GenerateAllRegionGrids()
  {
    // We don't want this to change mid execution, either queue all or none
    bool deferred = mapping.ThreadAvailable;
    AsyncLongOperationAction longOperation =
      deferred ? AsyncPool<AsyncLongOperationAction>.Get() : null;
    bool anyRequest = false;
    foreach (VehicleDef ownerDef in mapping.GridOwners.AllOwners)
    {
      if (deferred)
        anyRequest |= TryAddRegionGridRequest(ownerDef, longOperation);
      else
        GenerateRegionGridFor(ownerDef);
    }

    if (deferred)
    {
      if (anyRequest)
        FinalizeAndSendLongOp(longOperation);
      else
        longOperation.ReturnToPool();
    }
  }

  public void RequestGridsFor(VehicleDef vehicleDef, Urgency urgency)
  {
    if (urgency == Urgency.None)
      return;

    if (mapping.ThreadAvailable && urgency == Urgency.Deferred)
    {
      AsyncLongOperationAction longOperation = AsyncPool<AsyncLongOperationAction>.Get();

      bool anyRequest = TryAddPathGridRequest(vehicleDef, longOperation);
      anyRequest |= TryAddRegionGridRequest(vehicleDef, longOperation);

      if (anyRequest)
        FinalizeAndSendLongOp(longOperation);
      else
        longOperation.ReturnToPool();
      return;
    }
    Debug.Message($"Skipped deferred grid generation for {vehicleDef}");
    GeneratePathGridFor(vehicleDef);
    GenerateRegionGridFor(vehicleDef);
  }

  private void FinalizeAndSendLongOp(AsyncLongOperationAction longOperation)
  {
    if (!longOperation.IsValid)
    {
      Trace.Fail("Trying to send long op to thread but it's already invalid.");
      longOperation.ReturnToPool();
      return;
    }

    longOperation.OnValidate += GridGenIsValid;
    mapping.dedicatedThread.Enqueue(longOperation);
  }

  /// <summary>
  /// Runs DoIncrementalPass multiple times to reach minimum days unused for removal on all unused
  /// vehicles.
  /// </summary>
  internal void DoPass()
  {
    for (int i = 0; i < DaysUnusedForRemoval; i++)
    {
      DoIncrementalPass();
    }
  }

  internal void DoPassExpectClear()
  {
    DoPass();
    Assert.IsTrue(
      DefDatabase<VehicleDef>.AllDefsListForReading.All(
        def => !mapping[def].VehiclePathGrid.Enabled));
    Assert.IsTrue(
      DefDatabase<VehicleDef>.AllDefsListForReading.All(
        def => mapping[def].Suspended));
  }

  internal void DoIncrementalPass()
  {
    if (PassDisabled)
      return;

    Assert.IsTrue(pathGridCounter.Count == 0);
    foreach (Pawn pawn in mapping.map.mapPawns.AllPawns)
    {
      if (pawn is VehiclePawn vehicle)
      {
        pathGridCounter.SetUsed(vehicle.VehicleDef);
      }
    }

    foreach (Pawn pawn in Find.World.worldPawns.AllPawnsAlive)
    {
      if (pawn is VehiclePawn vehicle && vehicle.Faction.IsPlayerSafe())
      {
        pathGridCounter.SetUsed(vehicle.VehicleDef);
      }
    }

    // Release region set for vehicles not actively in use
    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
    {
      if (!pathGridCounter.IsUsed(vehicleDef))
      {
        pathGridCounter.IncrementUnused(vehicleDef);
        if (pathGridCounter.ShouldRemoveGrid(vehicleDef))
        {
          ReleasePathGrid(vehicleDef);
        }
      }
    }

    foreach (VehicleDef vehicleDef in mapping.GridOwners.AllOwners)
    {
      VehiclePathingSystem.VehiclePathData pathData = mapping[vehicleDef];
      if (!pathData.VehiclePathGrid.Enabled && !mapping.GridOwners.TryForfeitOwnership(vehicleDef))
      {
        ReleaseRegionGrid(vehicleDef);
      }
    }

    pathGridCounter.OnPassComplete();
  }

  private bool TryAddPathGridRequest(VehicleDef vehicleDef, AsyncLongOperationAction longOperation)
  {
    // Path grid has already been initialized
    if (mapping[vehicleDef].VehiclePathGrid.Enabled)
      return false;
    longOperation.OnInvoke += () => GeneratePathGridFor(vehicleDef);
    return true;
  }

  private bool TryAddRegionGridRequest(VehicleDef vehicleDef,
    AsyncLongOperationAction longOperation)
  {
    // Region grid has already been initialized
    if (!mapping[vehicleDef].Suspended)
      return false;
    longOperation.OnInvoke += () => GenerateRegionGridFor(vehicleDef);
    return true;
  }

  private void GeneratePathGridFor(VehicleDef vehicleDef)
  {
    VehiclePathingSystem.VehiclePathData pathData = mapping[vehicleDef];

    if (pathData.VehiclePathGrid.Enabled)
      return;

    pathData.VehiclePathGrid.RecalculateAllPerceivedPathCosts();
  }

  private void GenerateRegionGridFor(VehicleDef vehicleDef)
  {
    VehicleDef ownerDef = mapping.GridOwners.GetOwner(vehicleDef);

    // Region grid has already been initialized
    if (!mapping[vehicleDef].Suspended)
      return;

    VehiclePathingSystem.VehiclePathData pathData = mapping[ownerDef];
    pathData.VehicleRegionAndRoomUpdater.Init();
    pathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
  }

  private void ReleasePathGrid(VehicleDef ownerDef)
  {
    VehiclePathingSystem.VehiclePathData pathData = mapping[ownerDef];
    if (!pathData.VehiclePathGrid.Enabled)
      return;

    pathData.VehiclePathGrid.Release();

#if DEBUG
    Ext_Messages.Message($"Released PathGrid for {ownerDef}", MessageTypeDefOf.SilentInput,
      time: 1f, historical: false);
#endif
  }

  private void ReleaseRegionGrid(VehicleDef ownerDef)
  {
    VehiclePathingSystem.VehiclePathData pathData = mapping[ownerDef];
    if (pathData.Suspended)
      return;

    pathData.VehicleRegionAndRoomUpdater.Release();

#if DEBUG
    Ext_Messages.Message($"Released ReionGrid for {ownerDef}", MessageTypeDefOf.SilentInput,
      time: 1f, historical: false);
#endif
  }

  public static Urgency UrgencyFor(VehiclePawn vehicle)
  {
    // If null faction, vehicle should be immovable
    if (vehicle.Faction == null)
      return Urgency.None;

    // Non-player factions need grid urgently for incidents
    if (!vehicle.Faction.IsPlayer)
      return Urgency.Urgent;

    return Urgency.Deferred;
  }

  [DebugAction(VehicleHarmony.VehiclesLabel, "Force Remove Unused Regions")]
  private static void DoPassOnAllMaps()
  {
    foreach (Map map in Find.Maps)
    {
      VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
      mapping.deferredGridGeneration.DoPass();
    }
  }

  private class GridCounter
  {
    private readonly Dictionary<VehicleDef, int> countdownToRemoval = [];
    private readonly HashSet<VehicleDef> activelyUsed = [];

    public int Count => activelyUsed.Count;

    public bool IsUsed(VehicleDef vehicleDef)
    {
      return activelyUsed.Contains(vehicleDef);
    }

    public void SetUsed(VehicleDef vehicleDef)
    {
      countdownToRemoval.Remove(vehicleDef);
      activelyUsed.Add(vehicleDef);
    }

    public void IncrementUnused(VehicleDef vehicleDef)
    {
      Assert.IsFalse(activelyUsed.Contains(vehicleDef));
      if (!countdownToRemoval.ContainsKey(vehicleDef))
        countdownToRemoval.Add(vehicleDef, 0);

      countdownToRemoval[vehicleDef]++;
    }

    /// <returns>VehicleDef grid has been unused past threshold and can be removed.</returns>
    public bool ShouldRemoveGrid(VehicleDef vehicleDef)
    {
      // Remove when exceeds days unused
      return countdownToRemoval[vehicleDef] >= DaysUnusedForRemoval;
    }

    public void OnPassComplete()
    {
      activelyUsed.Clear();
    }
  }

  public readonly struct PassDisabler : IDisposable
  {
    private readonly DeferredGridGeneration gridGeneration;

    public PassDisabler(DeferredGridGeneration gridGeneration)
    {
      this.gridGeneration = gridGeneration;
      this.gridGeneration.PassDisabled = true;
    }

    void IDisposable.Dispose()
    {
      gridGeneration.PassDisabled = false;
    }
  }

  public enum Urgency
  {
    None,
    Deferred, // Queue request to thread
    Urgent, // Generate immediately
  }
}