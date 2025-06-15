using System.Linq;
using DevTools.UnitTesting;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
internal sealed class UnitTest_VehicleTurret
{
  private VehiclePawn vehicle;
  private VehicleTurret rootTurret;
  private VehicleTurret childTurret;

  [SetUp]
  private void AlignAllRotations()
  {
    VehicleDef testVehicleDef = DefDatabase<VehicleDef>.AllDefsListForReading.FirstOrDefault(def =>
      def.GetCompProperties<CompProperties_VehicleTurrets>() is not null);
    Assert.IsNotNull(testVehicleDef);
    vehicle = VehicleSpawner.GenerateVehicle(testVehicleDef, Faction.OfPlayer);
    Assert.IsNotNull(vehicle.CompVehicleTurrets);

    // Isolate turret test case
    for (int i = vehicle.CompVehicleTurrets.Turrets.Count - 1; i >= 0; i--)
      vehicle.CompVehicleTurrets.RemoveTurret(vehicle.CompVehicleTurrets.Turrets[i]);

    vehicle.CompVehicleTurrets.AddTurret(new VehicleTurret(vehicle)
    {
      def = new VehicleTurretDef
      {
        defName = "TurretDef",
        turretType = TurretType.Rotatable,
      },
      key = nameof(rootTurret),
      renderProperties = new VehicleTurretRender
      {
        north = new Vector2(0, 2),
        east = new Vector2(2, 0.25f),
        south = new Vector2(0, -1.5f)
      }
    });
    rootTurret = vehicle.CompVehicleTurrets.Turrets.FirstOrDefault();
    Assert.IsNotNull(rootTurret);

    vehicle.CompVehicleTurrets.AddTurret(new VehicleTurret(vehicle)
    {
      def = new VehicleTurretDef
      {
        defName = "TurretDef2",
        turretType = TurretType.Rotatable,
      },
      key = nameof(childTurret),
      parentKey = rootTurret.key,
      renderProperties = new VehicleTurretRender
      {
        north = new Vector2(1, 1),
      }
    });
    childTurret =
      vehicle.CompVehicleTurrets.Turrets.FirstOrDefault(turret => turret.attachedTo != null);
    Assert.IsNotNull(childTurret);

    rootTurret.renderProperties.RecacheOffsets();
    childTurret.renderProperties.RecacheOffsets();
  }

  [TearDown]
  private void CleanUpReferences()
  {
    vehicle.Destroy(); // Will propogate to turrets
    vehicle = null;
    rootTurret = null;
    childTurret = null;
  }

  [Test]
  private void VehicleOrientation()
  {
    vehicle.FullRotation = Rot8.North;
    Assert.AreApproximatelyEqual(rootTurret.TurretRotation, rootTurret.defaultAngleRotated);

    // North > East > South > West
    Vector3[] rootPositions =
    [
      new(0, 0, 2), // N
      new(2, 0, 0.25f), // E
      new(0, 0, -1.5f), // S
      new(-2, 0, 0.25f), // W
      new(1.414214f, 0, 1.414214f), // NE
      new(1.060660f, 0, -1.060660f), // SE
      new(-1.060660f, 0, -1.060660f), // SW
      new(-1.414214f, 0, 1.414214f), // NW
    ];
    Assert.IsTrue(rootPositions.Length == 8);

    Vector3[] childPositions =
    [
      new(1, 0, 3), // N
      new(3, 0, -0.75f), // E
      new(-1, 0, -2.5f), // S
      new(-3, 0, 1.25f), // W
      new(2.828427f, 0, 1.414214f), // NE
      new(1.060660f, 0, -2.474874f), // SE
      new(-2.474874f, 0, -1.060660f), // SW
      new(-1.414214f, 0, 2.828427f), // NW
    ];
    Assert.IsTrue(childPositions.Length == 8);

    // Vehicle Orientation
    for (int i = 0; i < 8; i++)
    {
      Rot8 rot = new(i);
      vehicle.FullRotation = rot;

      // Root
      {
        Vector3 expected = rootPositions[i];
        Vector3 actual = rootTurret.DrawPosition(rot);
        Expect.AreApproximatelyEqual(expected.x, actual.x,
          $"DrawOffset.x_{rot.ToStringNamed()}");
        Expect.AreApproximatelyEqual(expected.z, actual.z,
          $"DrawOffset.z_{rot.ToStringNamed()}");
        Expect.AreApproximatelyEqual(rootTurret.TurretRotation,
          rootTurret.defaultAngleRotated + rot.AsAngle, $"DefaultAngle_{rot.ToStringNamed()}");
      }

      // Child
      {
        Vector3 expected = childPositions[i];
        Vector3 actual = childTurret.DrawPosition(rot);
        Expect.AreApproximatelyEqual(expected.x, actual.x,
          $"DrawOffset.x_{rot.ToStringNamed()}");
        Expect.AreApproximatelyEqual(expected.z, actual.z,
          $"DrawOffset.z_{rot.ToStringNamed()}");
        Expect.AreApproximatelyEqual(childTurret.TurretRotation,
          childTurret.defaultAngleRotated + rot.AsAngle, $"DefaultAngle_{rot.ToStringNamed()}");
      }
    }
  }

  [Test]
  private void ParentRotation()
  {
    vehicle.FullRotation = Rot8.North;
    rootTurret.UpdateRotationLock();
    rootTurret.TurretRotation = 0;

    Assert.AreApproximatelyEqual(rootTurret.TurretRotation, 0);

    // North > East > South > West
    Vector3[] rootPositions =
    [
      new(0, 0, 2), // N
      new(2, 0, 0.25f), // E
      new(0, 0, -1.5f), // S
      new(-2, 0, 0.25f), // W
      new(1.414214f, 0, 1.414214f), // NE
      new(1.060660f, 0, -1.060660f), // SE
      new(-1.060660f, 0, -1.060660f), // SW
      new(-1.414214f, 0, 1.414214f), // NW
    ];
    Assert.IsTrue(rootPositions.Length == 8);

    // Parent turret does not shift while rotating
    for (int i = 0; i < 8; i++)
    {
      Rot8 rot = new(i);
      vehicle.FullRotation = rot;
      rootTurret.UpdateRotationLock();
      for (int theta = 0; theta < 360; theta += 10)
      {
        rootTurret.TurretRotation = theta;
        Assert.AreApproximatelyEqual(rootTurret.TurretRotation, theta);
        Vector3 actual = rootTurret.DrawPosition(rot);
        Vector3 expected = rootPositions[rot.AsInt];
        Expect.AreApproximatelyEqual(actual.x, expected.x, "RootTurret.x");
        // No need to test y, we aren't validating altitude layer math here.
        Expect.AreApproximatelyEqual(actual.z, expected.z, "RootTurret.z");
      }
    }

    Vector3[,] childPositions = new Vector3[,]
    {
      // North
      {
        new(1, 0, 3), // 0
        new(1.366025f, 0, 2.366025f),
        new(1.366025f, 0, 1.633975f),
        new(1, 0, 1), // 90
        new(0.366025f, 0, 0.633975f),
        new(-0.366025f, 0, 0.633975f),
        new(-1, 0, 1), // 180
        new(-1.366025f, 0, 1.633975f),
        new(-1.366025f, 0, 2.366025f),
        new(-1, 0, 3), // 270
        new(-0.366025f, 0, 3.366025f),
        new(0.366025f, 0, 3.366025f),
      },
      // East
      {
        new(3f, 0, 1.25f), // 0
        new(3.366025f, 0, 0.616025f),
        new(3.366025f, 0, -0.116025f),
        new(3f, 0, -0.75f), // 90
        new(2.366025f, 0, -1.116025f),
        new(1.633975f, 0, -1.116025f),
        new(1f, 0, -0.75f), // 180
        new(0.633975f, 0, -0.116025f),
        new(0.633975f, 0, 0.616025f),
        new(1f, 0, 1.25f), // 270
        new(1.633975f, 0, 1.616025f),
        new(2.366025f, 0, 1.616025f),
      },
      // South
      {
        new(1f, 0, -0.5f), // 0
        new(1.366025f, 0, -1.133975f),
        new(1.366025f, 0, -1.866025f),
        new(1f, 0, -2.5f), // 90
        new(0.366025f, 0, -2.866025f),
        new(-0.366025f, 0, -2.866025f),
        new(-1f, 0, -2.5f), // 180
        new(-1.366025f, 0, -1.866025f),
        new(-1.366025f, 0, -1.133975f),
        new(-1f, 0, -0.5f), // 270
        new(-0.366025f, 0, -0.133975f),
        new(0.366025f, 0, -0.133975f),
      },
      // West
      {
        new(-1f, 0, 1.25f), // 0
        new(-0.633975f, 0, 0.616025f),
        new(-0.633975f, 0, -0.116025f),
        new(-1f, 0, -0.75f), // 90
        new(-1.633975f, 0, -1.116025f),
        new(-2.366025f, 0, -1.116025f),
        new(-3f, 0, -0.75f), // 180
        new(-3.366025f, 0, -0.116025f),
        new(-3.366025f, 0, 0.616025f),
        new(-3f, 0, 1.25f), // 270
        new(-2.366025f, 0, 1.616025f),
        new(-1.633975f, 0, 1.616025f),
      },
      // NorthEast
      {
        new(2.414214f, 0, 2.414214f), // 0
        new(2.780239f, 0, 1.780239f),
        new(2.780239f, 0, 1.048189f),
        new(2.414214f, 0, 0.414214f), // 90
        new(1.780239f, 0, 0.048189f),
        new(1.048189f, 0, 0.048189f),
        new(0.414214f, 0, 0.414214f), // 180
        new(0.048189f, 0, 1.048189f),
        new(0.048189f, 0, 1.780239f),
        new(0.414214f, 0, 2.414214f), // 270
        new(1.048189f, 0, 2.780239f),
        new(1.780239f, 0, 2.780239f),
      },
      // SouthEast
      {
        new(2.06066f, 0, -0.06066f), // 0
        new(2.426685f, 0, -0.694635f),
        new(2.426685f, 0, -1.426685f),
        new(2.06066f, 0, -2.06066f), // 90
        new(1.426685f, 0, -2.426685f),
        new(0.694635f, 0, -2.426685f),
        new(0.06066f, 0, -2.06066f), // 180
        new(-0.305365f, 0, -1.426685f),
        new(-0.305365f, 0, -0.694635f),
        new(0.06066f, 0, -0.06066f), // 270
        new(0.694635f, 0, 0.305365f),
        new(1.426685f, 0, 0.305365f),
      },
      // SouthWest
      {
        new(-0.06066f, 0, -0.06066f), // 0
        new(0.305365f, 0, -0.694635f),
        new(0.305365f, 0, -1.426685f),
        new(-0.06066f, 0, -2.06066f), // 90
        new(-0.694635f, 0, -2.426685f),
        new(-1.426685f, 0, -2.426685f),
        new(-2.06066f, 0, -2.06066f), // 180
        new(-2.426685f, 0, -1.426685f),
        new(-2.426685f, 0, -0.694635f),
        new(-2.06066f, 0, -0.06066f), // 270
        new(-1.426685f, 0, 0.305365f),
        new(-0.694635f, 0, 0.305365f),
      },
      // NorthWest
      {
        new(-0.414214f, 0, 2.414214f), // 0
        new(-0.048189f, 0, 1.780239f),
        new(-0.048189f, 0, 1.048189f),
        new(-0.414214f, 0, 0.414214f), // 90
        new(-1.048189f, 0, 0.048189f),
        new(-1.780239f, 0, 0.048189f),
        new(-2.414214f, 0, 0.414214f), // 180
        new(-2.780239f, 0, 1.048189f),
        new(-2.780239f, 0, 1.780239f),
        new(-2.414214f, 0, 2.414214f), // 270
        new(-1.780239f, 0, 2.780239f),
        new(-1.048189f, 0, 2.780239f),
      },
    };

    const int RotationInc = 30;

    Assert.AreEqual(childPositions.GetLength(0), 8);
    Assert.AreEqual(childPositions.GetLength(1), 360 / RotationInc);

    // Child turret position rotates with parent
    for (int i = 0; i < 4; i++)
    {
      Rot8 rot = new(i);
      vehicle.FullRotation = rot;
      rootTurret.UpdateRotationLock();
      int index = 0;
      for (int theta = 0; theta < 360; theta += RotationInc, index++)
      {
        rootTurret.TurretRotation = theta;
        Assert.AreApproximatelyEqual(rootTurret.TurretRotation, theta);
        Vector3 actual = childTurret.DrawPosition(rot);
        Vector3 expected = childPositions[rot.AsInt, index];
        Expect.AreApproximatelyEqual(actual.x, expected.x,
          $"ChildTurret.x_{rot.ToStringNamed()}_{theta:0}");
        // No need to test y, we aren't validating altitude layer math here.
        Expect.AreApproximatelyEqual(actual.z, expected.z,
          $"ChildTurret.z_{rot.ToStringNamed()}_{theta:0}");
      }
    }
  }

  [Test]
  private void ChildRotation()
  {
    Vector3[] childPositions =
    [
      new(1, 0, 3), // N
      new(3, 0, -0.75f), // E
      new(-1, 0, -2.5f), // S
      new(-3, 0, 1.25f), // W
      new(2.828427f, 0, 1.414214f), // NE
      new(1.060660f, 0, -2.474874f), // SE
      new(-2.474874f, 0, -1.060660f), // SW
      new(-1.414214f, 0, 2.828427f), // NW
    ];
    Assert.IsTrue(childPositions.Length == 8);

    vehicle.FullRotation = Rot8.North;
    rootTurret.UpdateRotationLock();
    rootTurret.TurretRotation = 0;

    // Child turret does not shift while rotating
    for (int i = 0; i < 8; i++)
    {
      Rot8 rot = new(i);
      vehicle.FullRotation = rot;
      childTurret.UpdateRotationLock();
      for (int theta = 0; theta < 360; theta += 30)
      {
        childTurret.TurretRotation = theta;
        Assert.AreApproximatelyEqual(childTurret.TurretRotation, theta + rot.AsAngle);
        Vector3 actual = childTurret.DrawPosition(rot);
        Vector3 expected = childPositions[rot.AsInt];
        Expect.AreApproximatelyEqual(actual.x, expected.x,
          $"ChildTurret.x_{rot.ToStringNamed()}_{theta:0}");
        // No need to test y, we aren't validating altitude layer math here.
        Expect.AreApproximatelyEqual(actual.z, expected.z,
          $"ChildTurret.z_{rot.ToStringNamed()}_{theta:0}");
      }
    }
  }

  [Test]
  private void VehicleTransformRotation()
  {
    const int RotationInc = 30;

    Vector3[] rootPositions =
    [
      new(0f, 0, 2f), // 0
      new(1f, 0, 1.732051f),
      new(1.732051f, 0, 1f),
      new(2f, 0, 0f), // 90
      new(1.732051f, 0, -1f),
      new(1f, 0, -1.732051f),
      new(0f, 0, -2f), // 180
      new(-1f, 0, -1.732051f),
      new(-1.732051f, 0, -1f),
      new(-2f, 0, 0f), // 270
      new(-1.732051f, 0, 1f),
      new(-1f, 0, 1.732051f),
    ];
    Assert.IsTrue(rootPositions.Length == 360 / RotationInc);

    Vector3[] childPositions =
    [
      new(1f, 0, 3f), // 0
      new(2.366025f, 0, 2.098076f),
      new(3.098076f, 0, 0.633975f),
      new(3f, 0, -1f), // 90
      new(2.098076f, 0, -2.366025f),
      new(0.633975f, 0, -3.098076f),
      new(-1f, 0, -3f), // 180
      new(-2.366025f, 0, -2.098076f),
      new(-3.098076f, 0, -0.633975f),
      new(-3f, 0, 1f), // 270
      new(-2.098076f, 0, 2.366025f),
      new(-0.633975f, 0, 3.098076f),
    ];
    Assert.IsTrue(childPositions.Length == 360 / RotationInc);

    vehicle.FullRotation = Rot8.North;
    rootTurret.UpdateRotationLock();
    childTurret.UpdateRotationLock();
    rootTurret.TurretRotation = 0;
    childTurret.TurretRotation = 0;

    int index = 0;
    // Turret positions rotate relative to vehicle transform
    for (int theta = 0; theta < 360; theta += 30, index++)
    {
      vehicle.Transform.rotation = theta;
      Assert.AreApproximatelyEqual(vehicle.Transform.rotation, theta);
      Assert.AreApproximatelyEqual(rootTurret.TurretRotation, 0);

      // Parent turret rotates relative to vehicle transform
      {
        Vector3 actual = rootTurret.DrawPosition(Rot8.North);
        Vector3 expected = rootPositions[index];
        Expect.AreApproximatelyEqual(actual.x, expected.x,
          $"RootTurret.x_{theta:0}");
        // No need to test y, we aren't validating altitude layer math here.
        Expect.AreApproximatelyEqual(actual.z, expected.z,
          $"RootTurret.z_{theta:0}");
      }

      // Child turret rotates relative to parent's rotated position
      {
        Vector3 actual = childTurret.DrawPosition(Rot8.North);
        Vector3 expected = childPositions[index];
        Expect.AreApproximatelyEqual(actual.x, expected.x,
          $"ChildTurret.x_{theta:0}");
        // No need to test y, we aren't validating altitude layer math here.
        Expect.AreApproximatelyEqual(actual.z, expected.z,
          $"ChildTurret.z_{theta:0}");
      }
    }
  }
}