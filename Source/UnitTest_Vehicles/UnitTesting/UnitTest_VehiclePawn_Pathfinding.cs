using System.Collections;
using System.Linq;
using DevTools.UnitTesting;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.Pathing)]
[TestDescription(
  "Validate pathfinding behavior for multi-cell vehicles, ensuring reachability correctness based on vehicle size.")]
internal sealed class UnitTest_VehiclePawn_Pathing
{
  private static CellRect GetGotoArea(VehiclePawn vehicle, out IntVec3 dest)
  {
    Assert.IsTrue(vehicle.Spawned);
    CellRect testArea = TestUtils.GetTestArea(vehicle.Position, vehicle.def.Size);
    dest = vehicle.Position + new IntVec3(0, 0, testArea.Height);
    CellRect adjustedArea = TestUtils.GetTestArea(dest, vehicle.def.size);
    TestUtils.PrepareArea(vehicle.Map, dest, vehicle.VehicleDef);
    return adjustedArea;
  }

  [Test]
  private void OrderGoto()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1
    });
    group.Spawn();
    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    Assert.IsTrue(group.vehicle.CanMoveFinal);
    IntVec3 gotoLoc = group.vehicle.Position + new IntVec3(0, 0, 1);
    Assert.IsTrue(gotoLoc.Walkable(group.vehicle.VehicleDef, group.vehicle.Map));
    FloatMenuOptionProvider_OrderVehicle.PawnGotoAction(gotoLoc, group.vehicle, gotoLoc,
      Rot8.North);
    Assert.IsTrue(group.vehicle.jobs.curJob.def == JobDefOf.Goto);
  }

  [Test]
  private void OrderCorridorEvenWidth()
  {
    using ThreadDisabler td = new();
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1,
      size = new IntVec2(2, 4)
    });
    group.Spawn();

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    Assert.IsTrue(group.vehicle.CanMoveFinal);
    CellRect testArea = GetGotoArea(group.vehicle, out IntVec3 gotoLoc);
    using DebugHelper.DestroyAreaScope das = new(group.vehicle.Map, testArea);
    DebugHelper.FillArea(testArea, group.vehicle.Map, ThingDefOf.Wall);
    CellRect corridor = CellRect.CenteredOn(gotoLoc, group.vehicle.def.size.x, testArea.Height);
    DebugHelper.DestroyArea(corridor, group.vehicle.Map);
    Expect.IsFalse(group.vehicle.CanReachVehicle(gotoLoc, PathEndMode.OnCell, Danger.Deadly));
  }

  [Test]
  private void OrderUnreachable()
  {
    using ThreadDisabler td = new();
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1
    });
    group.Spawn();

    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    Assert.IsTrue(group.vehicle.CanMoveFinal);
    CellRect testArea = GetGotoArea(group.vehicle, out IntVec3 gotoLoc);
    using DebugHelper.DestroyAreaScope das = new(group.vehicle.Map, testArea);
    DebugHelper.FillEdge(testArea, group.vehicle.Map, ThingDefOf.Wall);
    Expect.IsFalse(group.vehicle.CanReachVehicle(gotoLoc, PathEndMode.OnCell, Danger.Deadly));
  }
}