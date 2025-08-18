using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles;

public class VehicleAI : IExposable
{
	private VehiclePawn vehicle;

	public VehicleAI(VehiclePawn vehicle)
	{
		this.vehicle = vehicle;
	}

	// TODO Raiders - Should be implemented with interface for VehicleAIComp
	public void AITick()
	{
		foreach (ThingComp comp in vehicle.AllComps)
		{
			if (comp is VehicleAIComp vehicleComp)
				vehicleComp.AITick();
		}
	}

	public void ExposeData()
	{
		Scribe_References.Look(ref vehicle, "vehicle", true);
	}
}