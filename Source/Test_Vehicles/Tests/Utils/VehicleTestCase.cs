using System;
using Verse;

namespace Vehicles.Tests;

public readonly struct VehicleTestCase : IDisposable
{
	private readonly VehiclePawn vehicle;

	public VehicleTestCase(VehiclePawn vehicle, Map map, CellRect testArea)
	{
		this.vehicle = vehicle;
		VehicleDef vehicleDef = vehicle.VehicleDef;
		TestUtils.PrepareArea(map, testArea, vehicleDef);
	}

	void IDisposable.Dispose()
	{
		// Ensure vehicles are completely cleared from caches to not interfere with other tests.
    if (!vehicle.Destroyed)
    {
      vehicle.DestroyVehicleAndPawns();
    }
	}
}