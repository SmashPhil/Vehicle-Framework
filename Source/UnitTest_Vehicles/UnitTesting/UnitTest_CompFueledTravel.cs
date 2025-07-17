using System;
using System.Collections.Generic;
using System.Linq;
using DevTools.UnitTesting;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.Events)]
[TestDescription("Mechanics related to fuel for vehicle pawns.")]
internal sealed class UnitTest_CompFueledTravel
{
  private const int FuelCapacity = 50;
  private const float FuelConsumptionRate = 25;

  private static void AddFuelToInventory(VehiclePawn vehicle, int count)
  {
    ThingDef fuelType = vehicle.CompFueledTravel.Props.fuelType;
    while (count > 0)
    {
      Thing thing = ThingMaker.MakeThing(fuelType);
      thing.stackCount = Mathf.Min(count, fuelType.stackLimit);
      count -= thing.stackCount;
      int transferred = vehicle.AddOrTransfer(thing);
      Assert.AreEqual(thing.stackCount, transferred);
    }
  }

  private static float FuelAtTick(CompFueledTravel compFuel, int tick, float multiplier = 1)
  {
    return compFuel.FuelCapacity - compFuel.ConsumptionRatePerTick * tick * multiplier;
  }

  [Test, ExecutionPriority(Priority.First)]
  [TestDescription("Refuel function adds correct fuel amount to vehicle.")]
  private void Refuel()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate
        }
      ]
    });
    group.Spawn();

    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.IsTrue(compFuel.EmptyTank);
    Assert.AreApproximatelyEqual(compFuel.Fuel, 0);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 0);
    // Fractional unit
    compFuel.Refuel(2.5f);
    Expect.AreApproximatelyEqual(compFuel.Fuel, 2.5f);
    Expect.AreApproximatelyEqual(compFuel.FuelPercent, 2.5f / compFuel.FuelCapacity);
    // Max
    compFuel.Refuel(compFuel.FuelCapacity);
    Expect.AreApproximatelyEqual(compFuel.Fuel, compFuel.FuelCapacity);
    Expect.AreApproximatelyEqual(compFuel.FuelPercent, 1);
  }

  [Test, ExecutionPriority(Priority.First)]
  [TestDescription("Refuel function clamps outliers to 0 and fuel capacity.")]
  private void RefuelClamped()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate
        }
      ]
    });
    group.Spawn();

    const float NegativeRefuel = -1000;
    const float ExcessRefuel = 1000;
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.IsTrue(compFuel.EmptyTank);
    Assert.AreApproximatelyEqual(compFuel.Fuel, 0);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 0);
    // Negative amount
    Expect.Throws<ArgumentException>(() => compFuel.Refuel(NegativeRefuel));
    // Above capacity
    Assert.IsTrue(ExcessRefuel > compFuel.FuelCapacity);
    compFuel.Refuel(ExcessRefuel);
    Expect.AreApproximatelyEqual(compFuel.Fuel, compFuel.FuelCapacity);
    Expect.AreApproximatelyEqual(compFuel.FuelPercent, 1);
  }

  [Test, ExecutionPriority(Priority.AboveNormal)]
  [TestDescription("ConsumeFuel function subtracts correct fuel amount from vehicle.")]
  private void ConsumeFuel()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate,
          fuelConsumptionCondition = FuelConsumptionCondition.Always
        }
      ]
    });
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.AreApproximatelyEqual(compFuel.Fuel, 0);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 0);

    compFuel.ConsumeFuel(999);
    Assert.AreApproximatelyEqual(compFuel.Fuel, 0);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 0);

    const float ConsumeAmount = 5;
    compFuel.Refuel(compFuel.FuelCapacity);
    compFuel.ConsumeFuel(ConsumeAmount);
    Expect.AreApproximatelyEqual(compFuel.Fuel, compFuel.FuelCapacity - ConsumeAmount);
  }

  [Test, ExecutionPriority(Priority.AboveNormal)]
  [TestDescription("Fuel in inventory is detected for use by CompFueledTravel.")]
  private void FuelFromInventory()
  {
    ThingDef fuelType = ThingDefOf.Chemfuel;
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = fuelType,
          fuelConsumptionRate = FuelConsumptionRate
        }
      ]
    });
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.AreApproximatelyEqual(compFuel.Fuel, 0);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 0);
    Assert.AreEqual(group.vehicle.inventory.Count(fuelType), 0);
    int refuelCount = Mathf.CeilToInt(compFuel.FuelCapacity);
    AddFuelToInventory(group.vehicle, refuelCount);
    Expect.AreEqual(group.vehicle.inventory.Count(fuelType), refuelCount);
    int fuelCount = CompFueledTravel.AllFuelFromInventory(group.vehicle).Sum(thing => thing.stackCount);
    Expect.AreEqual(refuelCount, fuelCount);
  }

  [Test]
  [TestDescription("Refueling from inventory adds to fuel count and substracts fuel from inventory.")]
  private void RefuelFullFromInventory()
  {
    ThingDef fuelType = ThingDefOf.Chemfuel;
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = fuelType,
          fuelConsumptionRate = FuelConsumptionRate
        }
      ]
    });
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.AreApproximatelyEqual(compFuel.Fuel, 0);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 0);
    Assert.AreEqual(group.vehicle.inventory.Count(fuelType), 0);
    int refuelCount = Mathf.CeilToInt(compFuel.FuelCapacity / 2f);
    AddFuelToInventory(group.vehicle, refuelCount);
    Expect.AreEqual(group.vehicle.inventory.Count(fuelType), refuelCount);
    compFuel.ConsumeFuelFromInventory(refuelCount);
    Expect.AreApproximatelyEqual(compFuel.Fuel, refuelCount);
    Expect.AreApproximatelyEqual(compFuel.FuelPercent, 0.5f);
    Expect.AreEqual(group.vehicle.inventory.Count(fuelType), 0);
  }

  [Test]
  [TestDescription("Refueling removes the correct amount of items from the inventory.")]
  private void RefuelPartialFromInventory()
  {
    ThingDef fuelType = ThingDefOf.Chemfuel;
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = fuelType,
          fuelConsumptionRate = FuelConsumptionRate
        }
      ]
    });
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.AreApproximatelyEqual(compFuel.Fuel, 0);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 0);
    Assert.AreEqual(group.vehicle.inventory.Count(fuelType), 0);

    const int ExtraFuelAmount = 5;
    int refuelCount = Mathf.CeilToInt(compFuel.FuelCapacity / 2f);
    AddFuelToInventory(group.vehicle, refuelCount + ExtraFuelAmount);
    Expect.AreEqual(group.vehicle.inventory.Count(fuelType), refuelCount + ExtraFuelAmount);
    compFuel.ConsumeFuelFromInventory(refuelCount);
    Expect.AreApproximatelyEqual(compFuel.Fuel, refuelCount);
    Expect.AreApproximatelyEqual(compFuel.FuelPercent, 0.5f);
    Expect.AreEqual(group.vehicle.inventory.Count(fuelType), ExtraFuelAmount);
  }

  [Test]
  [TestDescription("Fuel ejected removes fuel count and spawns fuel type on the ground with matching fuel count.")]
  private void Eject()
  {
    const int ThingRadialSearch = 5;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate
        }
      ]
    });
    group.Spawn();

    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);
    int fuelBefore = FuelNearVehicle();
    compFuel.TargetFuelPercent = 0;
    compFuel.EjectFuel();
    Expect.AreApproximatelyEqual(compFuel.Fuel, 0);
    Expect.AreApproximatelyEqual(compFuel.FuelPercent, 0);
    int fuelAfter = FuelNearVehicle();
    Expect.AreApproximatelyEqual(fuelAfter - fuelBefore, compFuel.FuelCapacity);
    return;

    int FuelNearVehicle()
    {
      int maxSize = Mathf.Max(group.vehicle.def.size.x, group.vehicle.def.size.z);
      int fuelCount = 0;
      foreach (IntVec3 cell in GenRadial.RadialCellsAround(group.vehicle.Position, maxSize + ThingRadialSearch, true))
      {
        List<Thing> thingList = group.vehicle.Map.thingGrid.ThingsListAt(cell);
        if (!thingList.NullOrEmpty())
        {
          foreach (Thing thing in thingList)
          {
            if (thing.def == compFuel.Props.fuelType)
              fuelCount += thing.stackCount;
          }
        }
      }
      return fuelCount;
    }
  }

  [Test]
  [TestDescription("Fuel is consumed while drafted.")]
  private void FuelConsumptionDrafted()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate,
          fuelConsumptionCondition = FuelConsumptionCondition.Drafted
        }
      ]
    });
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 1));
  }

  [Test]
  [TestDescription("Fuel is consumed while moving.")]
  private void FuelConsumptionMoving()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate,
          fuelConsumptionCondition = FuelConsumptionCondition.Moving
        }
      ]
    });
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    using (new ScopedReferenceRollback<VehiclePathFollower, bool>(group.vehicle.vehiclePather, "moving", true))
    {
      Assert.IsTrue(group.vehicle.vehiclePather.Moving);
      compFuel.CompTick();
      Assert.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 1));
    }
    Assert.IsFalse(group.vehicle.vehiclePather.Moving);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 1));
  }

  [Test]
  [TestDescription("Fuel is consumed while flying in an aerial vehicle.")]
  private void FuelConsumptionFlying()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate,
          fuelConsumptionCondition = FuelConsumptionCondition.Flying
        },
        new CompProperties_VehicleLauncher
        {
          compClass = typeof(CompVehicleLauncher),
          launchProtocol = new DefaultTakeoff
          {
            launchProperties = new LaunchProtocolProperties(),
            landingProperties = new LaunchProtocolProperties()
          }
        }
      ]
    });
    // NOTE - PawnComponentsUtilisy::AddAndRemoveDynamicComponents only executes during the pawn's SpawnSetup,
    // meaning ignition will be null if we don't spawn the vehicle before trying to draft / undraft.
    group.Spawn();
    group.vehicle.DeSpawn();

    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    CompVehicleLauncher compLauncher = group.vehicle.CompVehicleLauncher;
    Assert.IsNotNull(compLauncher);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 1);
    Assert.IsNotNull(aerialVehicle);
    using ScopeWorldObject swo = new(aerialVehicle);
    aerialVehicle.SpendFuel();
    float worldMultiplier = compLauncher.FuelConsumptionWorldMultiplier;
    Assert.IsTrue(worldMultiplier > 0);
    Expect.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 1, worldMultiplier));
  }

  [Test, ExecutionPriority(Priority.BelowNormal)]
  [TestDescription("Fuel is always consumed, regardless of other conditions.")]
  private void FuelConsumptionAlways()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate,
          fuelConsumptionCondition = FuelConsumptionCondition.Always
        }
      ]
    });
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 1));

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 2));

    using (new ScopedReferenceRollback<VehiclePathFollower, bool>(group.vehicle.vehiclePather, "moving", true))
    {
      Assert.IsTrue(group.vehicle.vehiclePather.Moving);
      compFuel.CompTick();
      Assert.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 3));
    }
    Assert.IsFalse(group.vehicle.vehiclePather.Moving);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 4));
  }

  [Test]
  [TestDescription("Fuel is discounted while idle but drafted, and not discounted while moving or flying.")]
  private void FuelConsumptionDiscounted()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate,
          fuelConsumptionCondition = FuelConsumptionCondition.All
        },
        new CompProperties_VehicleLauncher
        {
          compClass = typeof(CompVehicleLauncher),
          launchProtocol = new DefaultTakeoff
          {
            launchProperties = new LaunchProtocolProperties(),
            landingProperties = new LaunchProtocolProperties()
          }
        }
      ]
    });
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    CompVehicleLauncher compLauncher = group.vehicle.CompVehicleLauncher;
    Assert.IsNotNull(compLauncher);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);
    // Doesn't consume Always
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    // Drafted is discounted
    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    compFuel.CompTick();
    // idle discount is taken into account by consumption rate, no multiplier needed
    Assert.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 1));
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    // Moving is not discounted
    using (new ScopedReferenceRollback<VehiclePathFollower, bool>(group.vehicle.vehiclePather, "moving", true))
    {
      Assert.IsTrue(group.vehicle.vehiclePather.Moving);
      compFuel.CompTick();
      Assert.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 1));
    }
    Assert.IsFalse(group.vehicle.vehiclePather.Moving);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);

    group.vehicle.DeSpawn();
    // Flying is not discounted
    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 1);
    Assert.IsNotNull(aerialVehicle);
    using ScopeWorldObject swo = new(aerialVehicle);
    aerialVehicle.SpendFuel();
    float worldMultiplier = compLauncher.FuelConsumptionWorldMultiplier;
    Assert.IsTrue(worldMultiplier > 0);
    Expect.AreApproximatelyEqual(compFuel.Fuel, FuelAtTick(compFuel, 1, worldMultiplier));
  }

  [Test, ExecutionPriority(Priority.BelowNormal)]
  [TestDescription("Fuel is clamped to 0 so consumption doesn't result in negative carry.")]
  private void FuelConsumptionEmpty()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate,
          fuelConsumptionCondition = FuelConsumptionCondition.Always
        }
      ]
    });
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.AreApproximatelyEqual(compFuel.Fuel, 0);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 0);

    compFuel.CompTick();
    Assert.AreApproximatelyEqual(compFuel.Fuel, 0);
    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 0);
  }

  [Test, ExecutionPriority(Priority.BelowNormal)]
  [TestDescription("Fuel is always consumed, regardless of other conditions.")]
  private void OutOfFuelEvent()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate,
          fuelConsumptionCondition = FuelConsumptionCondition.Always
        }
      ]
    });
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    // Prep 1 tick worth of fuel
    compFuel.Refuel(compFuel.ConsumptionRatePerTick);
    Assert.AreApproximatelyEqual(compFuel.Fuel, compFuel.ConsumptionRatePerTick);

    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.OutOfFuel);
    compFuel.CompTick();
    Expect.AreApproximatelyEqual(compFuel.Fuel, 0);
    Expect.AreEqual(listener.CountRaised, 1);
  }

  [Test]
  [TestDescription("Fuel leak operates intermittently based on health percent.")]
  private void TicksPerLeak()
  {
    const float FuelLeakPercent = 0.5f;
    const int TickLeakMin = 4;
    const int TickLeakMax = 1;

    // not the same as constants, conversion is TicksPerRealSecond / leakRate so min = 60 ticks, max = 1 tick
    FloatRange leakRate = new((float)GenTicks.TicksPerRealSecond / TickLeakMin, GenTicks.TicksPerRealSecond);
    int ticksPerLeak = CompFueledTravel.TicksPerLeak(1, FuelLeakPercent, leakRate);
    Expect.AreEqual(ticksPerLeak, -1);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(0.75f, FuelLeakPercent, leakRate);
    Expect.AreEqual(ticksPerLeak, -1);

    ticksPerLeak = CompFueledTravel.TicksPerLeak(FuelLeakPercent, FuelLeakPercent, leakRate);
    Expect.AreEqual(ticksPerLeak, TickLeakMin);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(0, FuelLeakPercent, leakRate);
    Expect.AreEqual(ticksPerLeak, TickLeakMax);

    // Edge cases
    FloatRange edgeCaseRate = new(0, leakRate.max);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(0.5f, FuelLeakPercent, edgeCaseRate);
    Expect.AreEqual(ticksPerLeak, -1);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(-100, FuelLeakPercent, edgeCaseRate);
    Expect.AreEqual(ticksPerLeak, TickLeakMax);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(100, FuelLeakPercent, edgeCaseRate);
    Expect.AreEqual(ticksPerLeak, -1);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(0, 0, edgeCaseRate);
    Expect.AreEqual(ticksPerLeak, -1);
  }

  [Test]
  [TestDescription("Fuel leaks when component is damaged.")]
  private void FuelLeakEvents()
  {
    const string ComponentKey = "MockComp";
    const int MaxHealth = 100;
    const float LeakHealthPct = 0.5f;
    const float HealthToLeak = MaxHealth * LeakHealthPct;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      components =
      [
        new VehicleComponentProperties
        {
          key = ComponentKey,
          health = MaxHealth,

          reactors =
          [
            new Reactor_FuelLeak
            {
              healthPercent = LeakHealthPct
            }
          ]
        }
      ],
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = FuelCapacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate,
          fuelConsumptionCondition = FuelConsumptionCondition.Drafted
        }
      ]
    });
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    Assert.IsNotNull(component);
    compFuel.Refuel(compFuel.FuelCapacity);

    Assert.AreApproximatelyEqual(compFuel.FuelPercent, 1);
    Assert.AreEqual(component.HealthPercent, 1);
    Expect.IsFalse(compFuel.FuelLeaking);

    using (EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.DamageTaken))
    {
      DamageInfo damageInfo = new(DamageDefOf.Vaporize, MaxHealth - HealthToLeak);
      component.TakeDamage(group.vehicle, damageInfo, ignoreArmor: true);
      Assert.AreEqual(component.Health, HealthToLeak);
      Assert.AreEqual(listener.CountRaised, 1);
      Expect.IsTrue(compFuel.FuelLeaking);
    }

    using (EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.Repaired))
    {
      component.HealComponent(MaxHealth);
      Assert.AreEqual(component.Health, MaxHealth);
      Assert.AreEqual(listener.CountRaised, 1);
      Expect.IsFalse(compFuel.FuelLeaking);
    }

    using (EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.HealthChanged))
    {
      component.SetHealth(HealthToLeak);
      Assert.AreEqual(component.Health, HealthToLeak);
      Assert.AreEqual(listener.CountRaised, 1);
      Expect.IsTrue(compFuel.FuelLeaking);
      component.SetHealth(MaxHealth);
      Assert.AreEqual(component.Health, MaxHealth);
      Assert.AreEqual(listener.CountRaised, 2);
      Expect.IsFalse(compFuel.FuelLeaking);
    }
  }
}