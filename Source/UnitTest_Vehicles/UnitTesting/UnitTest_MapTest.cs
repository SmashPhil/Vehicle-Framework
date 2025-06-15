using System;
using System.Collections.Generic;
using DevTools.UnitTesting;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

internal abstract class UnitTest_MapTest
{
  private ThreadDisabler threadDisabler;

  protected Map map;
  protected IntVec3 root;
  protected List<VehiclePawn> vehicles = [];

  protected static Faction Faction => Faction.OfPlayer;

  protected virtual bool ShouldTest(VehicleDef vehicleDef)
  {
    return true;
  }

  protected virtual CellRect TestArea(VehicleDef vehicleDef)
  {
    int maxSize = Mathf.Max(vehicleDef.Size.x, vehicleDef.Size.z);
    return CellRect.CenteredOn(root, maxSize).ExpandedBy(5);
  }

  [TearDown, ExecutionPriority(Priority.Last)]
  protected void EnableDedicatedThreads()
  {
    threadDisabler.Dispose();
    threadDisabler = null;
  }

  [TearDown, ExecutionPriority(Priority.BelowNormal)]
  private void DestroyAllVehicles()
  {
    TestUtils.EmptyWorldAndMapOfVehicles();
  }

  [SetUp]
  protected void GenerateVehicles()
  {
    map = Find.CurrentMap;
    Assert.IsNotNull(map);
    Assert.IsTrue(DefDatabase<VehicleDef>.AllDefsListForReading.Count > 0,
      "No vehicles to test with");
    root = map.Center;

    // All map-based tests should be run synchronously, otherwise we would have race conditions
    // when validating grids.
    threadDisabler = new ThreadDisabler();

    VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
    vehicles.Clear();
    foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
    {
      if (!ShouldTest(vehicleDef))
        continue;

      // Path and region grids should all be initialized before starting any map-based test.
      Assert.IsFalse(mapping[vehicleDef].Suspended);
      Assert.IsTrue(mapping[vehicleDef].VehiclePathGrid.Enabled);
      if (!mapping.GridOwners.IsOwner(vehicleDef))
        Assert.IsTrue(mapping[mapping.GridOwners.GetOwner(vehicleDef)].VehiclePathGrid.Enabled);

      VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction);
      vehicles.Add(vehicle);
    }
  }

  protected readonly struct VehicleTestCase : IDisposable
  {
    private readonly VehiclePawn vehicle;
    private readonly Test.Group group;

    public VehicleTestCase(VehiclePawn vehicle, UnitTest_MapTest test)
    {
      this.vehicle = vehicle;
      VehicleDef vehicleDef = vehicle.VehicleDef;
      this.group = new Test.Group(vehicleDef.defName);
      TestUtils.PrepareArea(test.map, test.TestArea(vehicleDef), vehicleDef);
    }

    void IDisposable.Dispose()
    {
      group.Dispose();

      // Ensure vehicles are completely cleared from caches to not interfere with other tests.
      if (!vehicle.Destroyed)
        vehicle.DestroyVehicleAndPawns();
    }
  }

  /// <summary>
  /// Test class for validating cells within a vehicle's hitbox.
  /// </summary>
  [UsedImplicitly(ImplicitUseTargetFlags.Members)]
  protected class HitboxTester<T>
  {
    private readonly VehiclePawn vehicle;
    private readonly Func<IntVec3, T> valueGetter;
    private readonly Func<T, bool> validator;
    private readonly Action<IntVec3> reset;

    private readonly CellRect rect;

    public HitboxTester(VehiclePawn vehicle, IntVec3 root, Func<IntVec3, T> valueGetter,
      Func<T, bool> validator, Action<IntVec3> reset = null)
    {
      this.vehicle = vehicle;
      this.valueGetter = valueGetter;
      this.validator = validator;
      this.reset = reset;

      int radius = Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);
      rect = CellRect.CenteredOn(root, radius);
    }

    public void Start()
    {
      Reset();
    }

    public void Reset()
    {
      if (reset != null)
      {
        foreach (IntVec3 cell in rect)
        {
          reset.Invoke(cell);
        }
      }
    }

    public bool All(bool value)
    {
      return IsTrue(_ => value);
    }

    public bool Hitbox(bool value)
    {
      return IsTrue(cell => value ^ !vehicle.OccupiedRect().Contains(cell));
    }

    public bool IsTrue(Func<IntVec3, bool> expected)
    {
      foreach (IntVec3 cell in rect)
      {
        if (!Valid(cell, expected(cell)))
        {
          Valid(cell, expected(cell));
          return false;
        }
      }

      return true;
    }

    private bool Valid(IntVec3 cell, bool expected)
    {
      T current = valueGetter(cell);
      bool value = validator(current);
      bool result = value == expected;
      return result;
    }
  }
}