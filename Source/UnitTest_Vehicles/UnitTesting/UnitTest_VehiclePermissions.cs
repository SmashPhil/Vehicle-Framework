using DevTools.UnitTesting;
using UnityEngine.Assertions;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePermissions)]
[TestDescription("Vehicle permissions enable specific vehicle behavior with pawns on board.")]
internal sealed class UnitTest_VehiclePermissions
{
	[Test]
	private void Manual()
	{
		using VehicleGroup manualVehicle = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 2
		});

		manualVehicle.Spawn();
		Assert.IsTrue(manualVehicle.vehicle.Spawned);

		// Can move when role requirements satisifed
		Expect.IsTrue(manualVehicle.vehicle.CanMove);
		Expect.IsTrue(manualVehicle.vehicle.HasEnoughOperators);

		// Cannot move when role requirements not satisfied
		manualVehicle.DisembarkAll();
		Expect.IsTrue(manualVehicle.vehicle.CanMove);
		Expect.IsFalse(manualVehicle.vehicle.HasEnoughOperators);

		// Cannot move unless operator count is satisfied
		manualVehicle.BoardOne();

		Expect.IsTrue(manualVehicle.vehicle.CanMove);
		Expect.IsFalse(manualVehicle.vehicle.HasEnoughOperators);

		manualVehicle.BoardAll();

		manualVehicle.vehicle.DeSpawn();
	}

	[Test]
	private void Autonomous()
	{
		VehicleGroup autonomousVehicle = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile | VehiclePermissions.Autonomous,
			passengers = 1
		});

		autonomousVehicle.Spawn();
		Assert.IsTrue(autonomousVehicle.vehicle.Spawned);

		// Can move by default
		Expect.IsTrue(autonomousVehicle.vehicle.CanMove);
		Expect.IsTrue(autonomousVehicle.vehicle.HasEnoughOperators);

		// Can move even without any passengers
		autonomousVehicle.DisembarkAll();
		Expect.IsTrue(autonomousVehicle.vehicle.CanMove);
		Expect.IsTrue(autonomousVehicle.vehicle.HasEnoughOperators);

		// Boarding does not invalidate any movement permissions
		autonomousVehicle.BoardOne();
		Expect.IsTrue(autonomousVehicle.vehicle.CanMove);
		Expect.IsTrue(autonomousVehicle.vehicle.HasEnoughOperators);

		autonomousVehicle.BoardAll();

		autonomousVehicle.vehicle.DeSpawn();
	}

	[Test]
	private void Immobile()
	{
		using VehicleGroup immobileVehicle = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Autonomous,
			passengers = 1
		});

		immobileVehicle.Spawn();
		Assert.IsTrue(immobileVehicle.vehicle.Spawned);

		// Cannot move by default
		Expect.IsFalse(immobileVehicle.vehicle.CanMove);
		Expect.IsTrue(immobileVehicle.vehicle.HasEnoughOperators);

		// Disembarking does not enable movement permissions
		immobileVehicle.DisembarkAll();
		Expect.IsFalse(immobileVehicle.vehicle.CanMove);
		Expect.IsTrue(immobileVehicle.vehicle.HasEnoughOperators);

		// Sanity check for single boarding event, should be the same as before
		immobileVehicle.BoardOne();
		Expect.IsFalse(immobileVehicle.vehicle.CanMove);
		Expect.IsTrue(immobileVehicle.vehicle.HasEnoughOperators);

		immobileVehicle.BoardAll();

		immobileVehicle.vehicle.DeSpawn();
	}

	[Test]
	private void ImmobileAerialVehicle()
	{
		using VehicleGroup aerialVehicle = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.None,
			drivers = 1,
			comps =
			[
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

		aerialVehicle.Spawn();
		Assert.IsTrue(aerialVehicle.vehicle.Spawned);

		// Cannot move by default, but can launch
		Expect.IsFalse(aerialVehicle.vehicle.CanMove);
		Expect.IsTrue(aerialVehicle.vehicle.HasEnoughOperators);
		Expect.IsTrue(aerialVehicle.vehicle.CompVehicleLauncher.CanLaunchWithCargoCapacity(out _));

		// Disembarking does not enable movement permissions, and disables launch capability
		aerialVehicle.DisembarkAll();
		Expect.IsFalse(aerialVehicle.vehicle.CanMove);
		Expect.IsFalse(aerialVehicle.vehicle.HasEnoughOperators);
		Expect.IsFalse(aerialVehicle.vehicle.CompVehicleLauncher.CanLaunchWithCargoCapacity(out _));

		// Boarding event properly re-enables all permissions
		aerialVehicle.BoardOne();
		Expect.IsFalse(aerialVehicle.vehicle.CanMove);
		Expect.IsTrue(aerialVehicle.vehicle.HasEnoughOperators);
		Expect.IsTrue(aerialVehicle.vehicle.CompVehicleLauncher.CanLaunchWithCargoCapacity(out _));

		aerialVehicle.BoardAll();

		aerialVehicle.vehicle.DeSpawn();
	}
}