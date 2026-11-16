using System;
using System.Collections.Generic;
using System.Linq;
using DevTools.Testing;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;
using ScopedPathStatus =
  DevTools.Testing.ScopedFieldRollback<Vehicles.VehiclePathFollower, Vehicles.VehiclePathFollower.PathingStatus>;

namespace Vehicles.Tests;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.Events)]
[TestDescription("Mechanics related to fuel for vehicle pawns.")]
internal sealed class Test_CompFueledTravel
{
  private const int FuelCapacity = 50;
  private const float FuelConsumptionRate = 25;
  private const string FuelLeakComponentKey = "MockComp";
  private const int FuelLeakMaxHealth = 100;
  private const float FuelLeakHealthPercent = 0.5f;
  private const float FuelLeakHealth = FuelLeakMaxHealth * FuelLeakHealthPercent;

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

  private static VehicleGroup CreateFuelGroup(int capacity = FuelCapacity,
    FuelConsumptionCondition fuelCondition = FuelConsumptionCondition.All)
  {
    return VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      comps =
      [
        new CompProperties_FueledTravel
        {
          compClass = typeof(CompFueledTravel),
          fuelCapacity = capacity,
          fuelType = ThingDefOf.Chemfuel,
          fuelConsumptionRate = FuelConsumptionRate,
          fuelConsumptionCondition = fuelCondition
        }
      ]
    });
  }

  private static VehicleGroup CreateLauncherFuelGroup(FuelConsumptionCondition fuelCondition)
  {
    return VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
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
          fuelConsumptionCondition = fuelCondition
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
  }

  private static VehicleGroup CreateFuelLeakGroup()
  {
    return VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      components =
      [
        new VehicleComponentProperties
        {
          key = FuelLeakComponentKey,
          health = FuelLeakMaxHealth,
          reactors =
          [
            new Reactor_FuelLeak
            {
              healthPercent = FuelLeakHealthPercent
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
  }

  [Test]
  [TestDescription("Refuel adds a fractional fuel amount to the vehicle.")]
  private void Refuel()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();

    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.IsTrue(compFuel.EmptyTank);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.FuelPercent);
    compFuel.Refuel(2.5f);
    Expect.AreApproximatelyEqual(expected: 2.5f, compFuel.Fuel);
    Expect.AreApproximatelyEqual(2.5f / compFuel.FuelCapacity, compFuel.FuelPercent);
  }

  [Test]
  [TestDescription("Refueling a partially filled vehicle to capacity stops at capacity.")]
  private void RefuelToCapacity()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(2.5f);

    compFuel.Refuel(compFuel.FuelCapacity);
    Expect.AreApproximatelyEqual(compFuel.FuelCapacity, compFuel.Fuel);
    Expect.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);
  }

  [Test]
  [TestDescription("Excess refuel amount is clamped to fuel capacity.")]
  private void RefuelClamped()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();

    const float ExcessRefuel = 1000;
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.IsTrue(compFuel.EmptyTank);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.FuelPercent);
    Assert.IsTrue(ExcessRefuel > compFuel.FuelCapacity);
    compFuel.Refuel(ExcessRefuel);
    Expect.AreApproximatelyEqual(compFuel.FuelCapacity, compFuel.Fuel);
    Expect.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);
  }

  [Test]
  [TestDescription("Refuel rejects a negative amount.")]
  private void RefuelNegativeAmount()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);

    Expect.Throws<ArgumentException>(() => compFuel.Refuel(-1000));
  }

  [Test]
  [TestDescription("ConsumeFuel subtracts correct fuel amount from vehicle.")]
  private void ConsumeFuel()
  {
    using VehicleGroup group = CreateFuelGroup(fuelCondition: FuelConsumptionCondition.Always);
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);

    const float ConsumeAmount = 5;
    compFuel.Refuel(compFuel.FuelCapacity);
    compFuel.ConsumeFuel(ConsumeAmount);
    Expect.AreApproximatelyEqual(compFuel.FuelCapacity - ConsumeAmount, compFuel.Fuel);
  }

  [Test]
  [TestDescription("Consuming fuel from an empty tank leaves it empty.")]
  private void ConsumeFuelEmptyTank()
  {
    using VehicleGroup group = CreateFuelGroup(fuelCondition: FuelConsumptionCondition.Always);
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.IsTrue(compFuel.EmptyTank);

    compFuel.ConsumeFuel(999);
    Expect.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Expect.AreApproximatelyEqual(expected: 0, compFuel.FuelPercent);
  }

  [Test]
  [TestDescription("Fuel in inventory is directly usable for refueling.")]
  private void FuelFromInventory()
  {
    ThingDef fuelType = ThingDefOf.Chemfuel;
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.FuelPercent);
    Assert.AreEqual(expected: 0, group.vehicle.inventory.Count(fuelType));
    int refuelCount = Mathf.CeilToInt(compFuel.FuelCapacity);
    AddFuelToInventory(group.vehicle, refuelCount);
    Expect.AreEqual(refuelCount, group.vehicle.inventory.Count(fuelType));
    int fuelCount = CompFueledTravel.AllFuelFromInventory(group.vehicle).Sum(thing => thing.stackCount);
    Expect.AreEqual(refuelCount, fuelCount);
  }

  [Test]
  [TestDescription("Refueling from inventory adds to fuel count and substracts fuel from inventory.")]
  private void RefuelFullFromInventory()
  {
    ThingDef fuelType = ThingDefOf.Chemfuel;
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.FuelPercent);
    Assert.AreEqual(expected: 0, group.vehicle.inventory.Count(fuelType));
    int refuelCount = Mathf.CeilToInt(compFuel.FuelCapacity / 2f);
    AddFuelToInventory(group.vehicle, refuelCount);
    Expect.AreEqual(refuelCount, group.vehicle.inventory.Count(fuelType));
    compFuel.ConsumeFuelFromInventory(refuelCount);
    Expect.AreApproximatelyEqual(refuelCount, compFuel.Fuel);
    Expect.AreApproximatelyEqual(expected: 0.5f, compFuel.FuelPercent);
    Expect.AreEqual(expected: 0, group.vehicle.inventory.Count(fuelType));
  }

  [Test]
  [TestDescription("Refueling removes the correct amount from the inventory.")]
  private void RefuelPartialFromInventory()
  {
    ThingDef fuelType = ThingDefOf.Chemfuel;
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.FuelPercent);
    Assert.AreEqual(expected: 0, group.vehicle.inventory.Count(fuelType));

    const int ExtraFuelAmount = 5;
    int refuelCount = Mathf.CeilToInt(compFuel.FuelCapacity / 2f);
    AddFuelToInventory(group.vehicle, refuelCount + ExtraFuelAmount);
    Expect.AreEqual(refuelCount + ExtraFuelAmount, group.vehicle.inventory.Count(fuelType));
    compFuel.ConsumeFuelFromInventory(refuelCount);
    Expect.AreApproximatelyEqual(refuelCount, compFuel.Fuel);
    Expect.AreApproximatelyEqual(expected: 0.5f, compFuel.FuelPercent);
    Expect.AreEqual(ExtraFuelAmount, group.vehicle.inventory.Count(fuelType));
  }

  [Test]
  [TestDescription("Fuel ejected removes fuel count and spawns fuel type on the ground with matching fuel count.")]
  private void Eject()
  {
    const int ThingRadialSearch = 5;

    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();

    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);
    int fuelBefore = FuelNearVehicle();
    compFuel.TargetFuelPercent = 0;
    compFuel.EjectFuel();
    Expect.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Expect.AreApproximatelyEqual(expected: 0, compFuel.FuelPercent);
    int fuelAfter = FuelNearVehicle();
    Expect.AreApproximatelyEqual(compFuel.FuelCapacity, fuelAfter - fuelBefore);
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
    using VehicleGroup group = CreateFuelGroup(fuelCondition: FuelConsumptionCondition.Drafted);
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);

    compFuel.CompTick();
    Assert.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(FuelAtTick(compFuel, 1), compFuel.Fuel);
  }

  [Test]
  [TestDescription("Fuel is consumed while moving.")]
  private void FuelConsumptionMoving()
  {
    using VehicleGroup group = CreateFuelGroup(fuelCondition: FuelConsumptionCondition.Moving);
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);

    compFuel.CompTick();
    Assert.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);

    using (new ScopedPathStatus(group.vehicle.vehiclePather, "status",
             VehiclePathFollower.PathingStatus.Moving))
    {
      Assert.IsTrue(group.vehicle.vehiclePather.Moving);
      compFuel.CompTick();
      Assert.AreApproximatelyEqual(FuelAtTick(compFuel, 1), compFuel.Fuel);
    }
    Assert.IsFalse(group.vehicle.vehiclePather.Moving);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(FuelAtTick(compFuel, 1), compFuel.Fuel);
  }

  [Test]
  [TestDescription("Fuel is consumed while flying in an aerial vehicle.")]
  private void FuelConsumptionFlying()
  {
    using VehicleGroup group = CreateLauncherFuelGroup(FuelConsumptionCondition.Flying);
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
    Assert.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);

    compFuel.CompTick();
    Assert.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);

    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 1);
    Assert.IsNotNull(aerialVehicle);
    using ScopeWorldObject swo = new(aerialVehicle);
    aerialVehicle.SpendFuel();
    float worldMultiplier = compLauncher.FuelConsumptionWorldMultiplier;
    Assert.IsTrue(worldMultiplier > 0);
    Expect.AreApproximatelyEqual(FuelAtTick(compFuel, 1, worldMultiplier), compFuel.Fuel);
  }

  [Test]
  [TestDescription("Fuel is always consumed, regardless of other conditions.")]
  private void FuelConsumptionAlways()
  {
    using VehicleGroup group = CreateFuelGroup(fuelCondition: FuelConsumptionCondition.Always);
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreApproximatelyEqual(expected: 1, compFuel.FuelPercent);

    compFuel.CompTick();
    Assert.AreApproximatelyEqual(FuelAtTick(compFuel, 1), compFuel.Fuel);

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(FuelAtTick(compFuel, 2), compFuel.Fuel);

    using (new ScopedPathStatus(group.vehicle.vehiclePather, "status",
             VehiclePathFollower.PathingStatus.Moving))
    {
      Assert.IsTrue(group.vehicle.vehiclePather.Moving);
      compFuel.CompTick();
      Assert.AreApproximatelyEqual(FuelAtTick(compFuel, 3), compFuel.Fuel);
    }
    Assert.IsFalse(group.vehicle.vehiclePather.Moving);
    compFuel.CompTick();
    Assert.AreApproximatelyEqual(FuelAtTick(compFuel, 4), compFuel.Fuel);
  }

  [Test]
  [TestDescription("Fuel consumption is discounted while drafted and idle.")]
  private void FuelConsumptionDraftedIdleDiscounted()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    float expectedRate = compFuel.ConsumptionRatePerTickRaw * CompFueledTravel.EfficiencyIdleMultiplier;
    Assert.AreApproximatelyEqual(expectedRate, compFuel.ConsumptionRatePerTick);
    compFuel.CompTick();
    Expect.AreApproximatelyEqual(compFuel.FuelCapacity - expectedRate, compFuel.Fuel);
  }

  [Test]
  [TestDescription("Fuel consumption is not discounted while moving.")]
  private void FuelConsumptionMovingNotDiscounted()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    group.vehicle.ignition.Drafted = true;

    using (new ScopedPathStatus(group.vehicle.vehiclePather, "status",
             VehiclePathFollower.PathingStatus.Moving))
    {
      Assert.IsTrue(group.vehicle.vehiclePather.Moving);
      Assert.AreApproximatelyEqual(compFuel.ConsumptionRatePerTickRaw,
        compFuel.ConsumptionRatePerTick);
      compFuel.CompTick();
      Expect.AreApproximatelyEqual(compFuel.FuelCapacity - compFuel.ConsumptionRatePerTickRaw,
        compFuel.Fuel);
    }
  }

  [Test]
  [TestDescription("Flying fuel consumption is not discounted.")]
  private void FuelConsumptionFlyingNotDiscounted()
  {
    using VehicleGroup group = CreateLauncherFuelGroup(FuelConsumptionCondition.All);
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    CompVehicleLauncher compLauncher = group.vehicle.CompVehicleLauncher;
    Assert.IsNotNull(compLauncher);
    compFuel.Refuel(compFuel.FuelCapacity);
    group.vehicle.DeSpawn();
    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 1);
    Assert.IsNotNull(aerialVehicle);
    using ScopeWorldObject swo = new(aerialVehicle);
    Assert.AreApproximatelyEqual(compFuel.ConsumptionRatePerTickRaw,
      compFuel.ConsumptionRatePerTick);
    aerialVehicle.SpendFuel();
    float worldMultiplier = compLauncher.FuelConsumptionWorldMultiplier;
    Assert.IsTrue(worldMultiplier > 0);
    Expect.AreApproximatelyEqual(
      compFuel.FuelCapacity - compFuel.ConsumptionRatePerTickRaw * worldMultiplier,
      compFuel.Fuel);
  }

  [Test]
  [TestDescription("Fuel is clamped to 0 so consumption doesn't result in negative carry.")]
  private void FuelConsumptionEmpty()
  {
    using VehicleGroup group = CreateFuelGroup(fuelCondition: FuelConsumptionCondition.Always);
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.FuelPercent);

    compFuel.CompTick();
    Assert.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Assert.AreApproximatelyEqual(expected: 0, compFuel.FuelPercent);
  }

  [Test]
  [TestDescription("Fuel is always consumed, regardless of other conditions.")]
  private void OutOfFuelEvent()
  {
    using VehicleGroup group = CreateFuelGroup(fuelCondition: FuelConsumptionCondition.Always);
    group.Spawn();
    Assert.IsFalse(group.vehicle.Drafted);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    // Prep 1 tick worth of fuel
    compFuel.Refuel(compFuel.ConsumptionRatePerTick);
    Assert.AreApproximatelyEqual(compFuel.ConsumptionRatePerTick, compFuel.Fuel);

    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.OutOfFuel);
    compFuel.CompTick();
    Expect.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Expect.AreEqual(expected: 1, listener.CountRaised);
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
    Expect.AreEqual(expected: -1, ticksPerLeak);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(0.75f, FuelLeakPercent, leakRate);
    Expect.AreEqual(expected: -1, ticksPerLeak);

    ticksPerLeak = CompFueledTravel.TicksPerLeak(FuelLeakPercent, FuelLeakPercent, leakRate);
    Expect.AreEqual(TickLeakMin, ticksPerLeak);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(0, FuelLeakPercent, leakRate);
    Expect.AreEqual(TickLeakMax, ticksPerLeak);

    // Edge cases
    FloatRange edgeCaseRate = new(0, leakRate.max);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(0.5f, FuelLeakPercent, edgeCaseRate);
    Expect.AreEqual(expected: -1, ticksPerLeak);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(-100, FuelLeakPercent, edgeCaseRate);
    Expect.AreEqual(TickLeakMax, ticksPerLeak);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(100, FuelLeakPercent, edgeCaseRate);
    Expect.AreEqual(expected: -1, ticksPerLeak);
    ticksPerLeak = CompFueledTravel.TicksPerLeak(0, 0, edgeCaseRate);
    Expect.AreEqual(expected: -1, ticksPerLeak);
  }

  [Test]
  [TestDescription("Component damage enables fuel leaking.")]
  private void FuelLeakAfterDamage()
  {
    using VehicleGroup group = CreateFuelLeakGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    VehicleComponent component = group.vehicle.statHandler.GetComponent(FuelLeakComponentKey);
    Assert.IsNotNull(component);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.AreEqual(expected: 1, component.HealthPercent);
    Assert.IsFalse(compFuel.FuelLeaking);

    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.DamageTaken);
    DamageInfo damageInfo = new(DamageDefOf.Vaporize, FuelLeakMaxHealth - FuelLeakHealth);
    component.TakeDamage(group.vehicle, damageInfo, ignoreArmor: true);
    Assert.AreEqual(FuelLeakHealth, component.Health);
    Assert.AreEqual(expected: 1, listener.CountRaised);
    Expect.IsTrue(compFuel.FuelLeaking);
  }

  [Test]
  [TestDescription("Repairing a damaged component stops fuel leaking.")]
  private void FuelLeakAfterRepair()
  {
    using VehicleGroup group = CreateFuelLeakGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    VehicleComponent component = group.vehicle.statHandler.GetComponent(FuelLeakComponentKey);
    Assert.IsNotNull(component);
    compFuel.Refuel(compFuel.FuelCapacity);
    component.SetHealth(FuelLeakHealth);
    Assert.IsTrue(compFuel.FuelLeaking);

    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.Repaired);
    component.HealComponent(FuelLeakMaxHealth);
    Assert.AreEqual(FuelLeakMaxHealth, component.Health);
    Assert.AreEqual(expected: 1, listener.CountRaised);
    Expect.IsFalse(compFuel.FuelLeaking);
  }

  [Test]
  [TestDescription("Direct health changes update fuel leaking in both directions.")]
  private void FuelLeakAfterHealthChange()
  {
    using VehicleGroup group = CreateFuelLeakGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    VehicleComponent component = group.vehicle.statHandler.GetComponent(FuelLeakComponentKey);
    Assert.IsNotNull(component);
    compFuel.Refuel(compFuel.FuelCapacity);
    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.HealthChanged);

    component.SetHealth(FuelLeakHealth);
    Assert.AreEqual(FuelLeakHealth, component.Health);
    Assert.AreEqual(expected: 1, listener.CountRaised);
    Assert.IsTrue(compFuel.FuelLeaking);

    component.SetHealth(FuelLeakMaxHealth);
    Assert.AreEqual(FuelLeakMaxHealth, component.Health);
    Assert.AreEqual(expected: 2, listener.CountRaised);
    Expect.IsFalse(compFuel.FuelLeaking);
  }

  [Test]
  [TestDescription("Whole fuel items satisfy a fractional target without requesting another refuel.")]
  private void RefuelFractionalTarget()
  {
    const int Capacity = 250;
    const int FuelPerStack = 47;
    using VehicleGroup group = CreateFuelGroup(Capacity);
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.TargetFuelPercent = 0.75f;
    Assert.AreApproximatelyEqual(expected: 187.5f, compFuel.TargetFuelLevel);
    Assert.AreEqual(expected: 188, compFuel.FuelCountToFull);
    Assert.IsFalse(compFuel.FullTank);
    Assert.IsTrue(ThingDefOf.Chemfuel.stackLimit >= FuelPerStack);

    Thing fuel1 = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
    using ScopeEntity scopeFuel1 = new(fuel1);
    fuel1.stackCount = FuelPerStack;
    Thing fuel2 = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
    using ScopeEntity scopeFuel2 = new(fuel2);
    fuel2.stackCount = FuelPerStack;
    Thing fuel3 = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
    using ScopeEntity scopeFuel3 = new(fuel3);
    fuel3.stackCount = FuelPerStack;
    Thing fuel4 = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
    using ScopeEntity scopeFuel4 = new(fuel4);
    fuel4.stackCount = FuelPerStack;

    compFuel.Refuel([fuel1, fuel2, fuel3, fuel4]);
    Expect.AreApproximatelyEqual(expected: 188, compFuel.Fuel);
    Expect.AreEqual(expected: 0, compFuel.FuelCountToFull);
    Expect.IsTrue(compFuel.FullTank);
    Expect.IsTrue(fuel1.Destroyed && fuel2.Destroyed && fuel3.Destroyed && fuel4.Destroyed);
  }

  [Test]
  [TestDescription("Fuel need and FullTank status agree below, at, and above an integer target.")]
  private void FuelTargetBoundaries()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.TargetFuelPercent = 0.5f;
    Assert.AreApproximatelyEqual(expected: 25, compFuel.TargetFuelLevel);

    compFuel.Refuel(24.5f);
    Assert.AreEqual(expected: 1, compFuel.FuelCountToFull);
    Assert.IsFalse(compFuel.FullTank);

    compFuel.Refuel(0.5f);
    Assert.AreEqual(expected: 0, compFuel.FuelCountToFull);
    Assert.IsTrue(compFuel.FullTank);

    compFuel.Refuel(1);
    Expect.AreEqual(expected: -1, compFuel.FuelCountToFull);
    Expect.IsTrue(compFuel.FullTank);
  }

  [Test]
  [TestDescription("Lowering the target below current fuel does not create a refuel need.")]
  private void FuelTargetLowered()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    Assert.IsTrue(compFuel.FullTank);

    compFuel.TargetFuelPercent = 0.5f;
    Expect.AreApproximatelyEqual(expected: 25, compFuel.TargetFuelLevel);
    Expect.AreEqual(expected: -25, compFuel.FuelCountToFull);
    Expect.IsTrue(compFuel.FullTank);
  }

  [Test]
  [TestDescription("Lowering the fuel target makes whole excess fuel eligible for ejection.")]
  private void FuelEjectionAfterTargetLowered()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(compFuel.FuelCapacity);
    compFuel.TargetFuelPercent = 0.5f;

    Expect.IsTrue(compFuel.CanEjectFuel);
  }

  [Test]
  [TestDescription("A tiny fuel deficit still requires one whole fuel count.")]
  private void FuelTinyDeficit()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.TargetFuelPercent = 0.5f;
    compFuel.Refuel(24.99999f);

    Expect.AreEqual(expected: 1, compFuel.FuelCountToFull);
    Expect.IsFalse(compFuel.FullTank);
  }

  [Test]
  [TestDescription("Refueling from multiple stacks leaves excess items.")]
  private void RefuelFuelThings()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.TargetFuelPercent = 0.5f;

    Thing partialStack = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
    using ScopeEntity scopePartialStack = new(partialStack);
    partialStack.stackCount = 15;
    Thing fullStack = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
    using ScopeEntity scopeFullStack = new(fullStack);
    fullStack.stackCount = 20;

    compFuel.Refuel([partialStack, fullStack]);
    Assert.AreApproximatelyEqual(expected: 25, compFuel.Fuel);
    Assert.AreEqual(expected: 0, compFuel.FuelCountToFull);
    Assert.IsTrue(fullStack.Destroyed);
    Expect.AreEqual(expected: 10, partialStack.stackCount);
  }

  [Test]
  [TestDescription("Refueling with fuel things when full consumes no items.")]
  private void RefuelFuelThingsWhenFull()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.TargetFuelPercent = 0.5f;
    compFuel.Refuel(25);
    Assert.IsTrue(compFuel.FullTank);
    Thing fuel = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
    using ScopeEntity scopeFuel = new(fuel);
    fuel.stackCount = 10;

    compFuel.Refuel([fuel]);
    Expect.AreApproximatelyEqual(expected: 25, compFuel.Fuel);
    Expect.AreEqual(expected: 10, fuel.stackCount);
    Expect.IsFalse(fuel.Destroyed);
  }

  [Test]
  [TestDescription("A rounded-up fractional target does not offer another refuel action.")]
  private void RefuelAvailabilityAtFractionalTarget()
  {
    using VehicleGroup group = CreateFuelGroup(250);
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.TargetFuelPercent = 0.75f;
    Assert.IsTrue(compFuel.CompFloatMenuOptions(group.pawns[0]).Any());

    compFuel.Refuel(188);
    Assert.AreEqual(expected: 0, compFuel.FuelCountToFull);
    Expect.IsTrue(compFuel.FullTank);
    Expect.IsFalse(compFuel.CompFloatMenuOptions(group.pawns[0]).Any());
  }

  [Test]
  [TestDescription("Automatic refueling stops after reaching a fractional target.")]
  private void AutoRefuelUnavailableAtFractionalTarget()
  {
    using VehicleGroup group = CreateFuelGroup(250);
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.TargetFuelPercent = 0.75f;
    compFuel.Refuel(188);

    Expect.IsFalse(compFuel.ShouldAutoRefuelNow);
  }

  [Test]
  [TestDescription("Automatic refueling starts at or below its threshold.")]
  private void AutoRefuelThreshold()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Props.autoRefuelPercent = 0.5f;
    compFuel.TargetFuelPercent = 0.8f;
    Assert.AreApproximatelyEqual(expected: 40, compFuel.TargetFuelLevel);
    Assert.IsTrue(compFuel.ShouldAutoRefuelNow);

    compFuel.Refuel(20);
    Assert.IsTrue(compFuel.ShouldAutoRefuelNow);
    compFuel.Refuel(1);
    Assert.IsFalse(compFuel.ShouldAutoRefuelNow);

    compFuel.ConsumeFuel(2);
    Expect.IsTrue(compFuel.ShouldAutoRefuelNow);
  }

  [Test]
  [TestDescription("Disabling automatic refueling prevents an otherwise needed refuel.")]
  private void AutoRefuelDisabled()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.IsTrue(compFuel.ShouldAutoRefuelNow);

    compFuel.allowAutoRefuel = false;
    Expect.IsFalse(compFuel.ShouldAutoRefuelNow);
  }

  [Test]
  [TestDescription("A zero fuel target never requests automatic refueling.")]
  private void AutoRefuelZeroTarget()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    Assert.IsTrue(compFuel.ShouldAutoRefuelNow);

    compFuel.TargetFuelPercent = 0;
    Expect.IsFalse(compFuel.ShouldAutoRefuelNow);
  }

  [Test]
  [TestDescription("Less than one excess fuel item cannot be ejected.")]
  private void FractionalFuelEjection()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.Spawn();
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.TargetFuelPercent = 0.75f;
    Assert.AreApproximatelyEqual(expected: 37.5f, compFuel.TargetFuelLevel);

    compFuel.Refuel(38);
    Assert.IsFalse(compFuel.CanEjectFuel);
    compFuel.EjectFuel();
    Assert.AreApproximatelyEqual(expected: 38, compFuel.Fuel);

    compFuel.Refuel(1);
    Expect.IsTrue(compFuel.CanEjectFuel);
  }

  [Test]
  [TestDescription("An idle vehicle caravan consumes fuel at the idle rate.")]
  private void FuelConsumptionWorldIdle()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.BoardAll();
    VehicleCaravan caravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    using ScopeWorldObject scopeCaravan = new(caravan);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(10);
    float fuelPerTick = compFuel.ConsumptionRateWorldPerTick;
    Assert.IsTrue(fuelPerTick > 0);
    Assert.IsFalse(caravan.vehiclePather.Moving);

    compFuel.ConsumeFuelWorld();
    Expect.AreApproximatelyEqual(10 - fuelPerTick * CompFueledTravel.EfficiencyIdleMultiplier,
      compFuel.Fuel);
  }

  [Test]
  [TestDescription("A moving vehicle caravan consumes fuel at the full world rate.")]
  private void FuelConsumptionWorldMoving()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.BoardAll();
    VehicleCaravan caravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    using ScopeWorldObject scopeCaravan = new(caravan);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    compFuel.Refuel(10);
    float fuelPerTick = compFuel.ConsumptionRateWorldPerTick;
    Assert.IsTrue(fuelPerTick > 0);

    using (new ScopedFieldRollback<VehicleCaravan_PathFollower, bool>(caravan.vehiclePather, "moving", newValue: true))
    {
      Assert.IsTrue(caravan.vehiclePather.Moving);
      compFuel.ConsumeFuelWorld();
    }
    Expect.AreApproximatelyEqual(10 - fuelPerTick, compFuel.Fuel);
  }

  [Test]
  [TestDescription("World fuel consumption stops at zero and raises the OutOfFuel event once.")]
  private void FuelConsumptionWorldEmpty()
  {
    using VehicleGroup group = CreateFuelGroup();
    group.BoardAll();
    VehicleCaravan caravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    using ScopeWorldObject scopeCaravan = new(caravan);
    CompFueledTravel compFuel = group.vehicle.CompFueledTravel;
    Assert.IsNotNull(compFuel);
    float fuelPerTick = compFuel.ConsumptionRateWorldPerTick;
    Assert.IsTrue(fuelPerTick > 0);
    compFuel.Refuel(fuelPerTick / 4);
    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.OutOfFuel);
    compFuel.ConsumeFuelWorld();
    Assert.AreApproximatelyEqual(expected: 0, compFuel.Fuel);
    Assert.AreEqual(expected: 1, listener.CountRaised);
    compFuel.ConsumeFuelWorld();
    Expect.AreEqual(expected: 1, listener.CountRaised);
  }
}
