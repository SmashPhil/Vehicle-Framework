using System;
using System.Collections.Generic;
using DevTools.Testing;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Testing;

internal abstract class Test_MapTest
{
  [UsedImplicitly]
  public static readonly IntVec2[] VehicleSizes = [
    // Tall sizes from 1 to 5
    new(1, 1), new(1, 2), new(1, 3), new(1, 4), new(1, 5),
    new(2, 2), new(2, 3), new(2, 4), new(2, 5),
    new(3, 3), new(3, 4), new(3, 5),
    new(4, 4), new(4, 5),
    new(5, 5),
    // Wider than tall
    new(3, 2), new(4, 2), new(4, 3), new(5, 3),
  ];

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

	[OneTimeSetUp, ExecutionPriority(Priority.First)]
	protected void DisableThreads()
	{
		map = Find.CurrentMap;
		Assert.IsNotNull(map);
		Assert.IsTrue(DefDatabase<VehicleDef>.AllDefsListForReading.Count > 0,
			"No vehicles to test with");
		root = map.Center;

		// All map-based tests should be run synchronously, otherwise we would have race conditions
		// when validating grids.
		threadDisabler = new ThreadDisabler();
	}

	[OneTimeSetUp, ExecutionPriority(Priority.AboveNormal)]
	protected void GenerateVehicles()
	{
		VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
		Assert.IsTrue(mapping.ThreadAlive);
		Assert.IsFalse(mapping.ThreadAvailable);

		foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
		{
			if (!ShouldTest(vehicleDef))
				continue;

      mapping.RequestGridsFor(vehicleDef, DeferredGridGeneration.Urgency.Urgent);
      // Path and region grids should all be initialized before starting any map-based test.
      Assert.IsFalse(mapping[vehicleDef].Suspended);
			Assert.IsTrue(mapping[vehicleDef].VehiclePathGrid.Enabled);
      if (!mapping.GridOwners.IsOwner(vehicleDef))
      {
        Assert.IsTrue(mapping[mapping.GridOwners.GetOwner(vehicleDef)].VehiclePathGrid.Enabled);
      }

			VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction);
			vehicles.Add(vehicle);
		}
	}

	[OneTimeTearDown, ExecutionPriority(Priority.Last)]
	protected void EnableThreads()
	{
		threadDisabler.Dispose();
		threadDisabler = null;
		map = null;
    foreach (VehiclePawn vehicle in vehicles)
    {
      if (!vehicle.Destroyed)
      {
        vehicle.Destroy();
      }
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
		private readonly Func<T, IntVec3, bool> validator;
		private readonly Action<IntVec3> reset;

		private readonly CellRect rect;

		public HitboxTester(VehiclePawn vehicle, IntVec3 root, Func<IntVec3, T> valueGetter,
			Func<T, IntVec3, bool> validator, Action<IntVec3> reset = null)
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
			bool value = validator(current, cell);
			bool result = value == expected;
			return result;
		}
  }
}