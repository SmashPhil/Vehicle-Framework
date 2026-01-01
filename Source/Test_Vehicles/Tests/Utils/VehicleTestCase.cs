using System;
using DevTools.Testing;
using Verse;

namespace Vehicles.UnitTesting;

public readonly struct VehicleTestCase : IDisposable
{
	private readonly VehiclePawn vehicle;
	private readonly Test.Group group;

	public VehicleTestCase(VehiclePawn vehicle, Map map, CellRect testArea)
	{
		this.vehicle = vehicle;
		VehicleDef vehicleDef = vehicle.VehicleDef;
		this.group = new Test.Group(vehicleDef.defName);
		TestUtils.PrepareArea(map, testArea, vehicleDef);
	}

	void IDisposable.Dispose()
	{
		group.Dispose();

		// Ensure vehicles are completely cleared from caches to not interfere with other tests.
		if (!vehicle.Destroyed)
			vehicle.DestroyVehicleAndPawns();
	}
}