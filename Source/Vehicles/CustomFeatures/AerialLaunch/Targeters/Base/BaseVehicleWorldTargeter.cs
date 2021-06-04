using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public abstract class BaseVehicleWorldTargeter : BaseWorldTargeter
	{
		protected VehiclePawn vehicle;
		protected AerialVehicleInFlight aerialVehicle;

		public abstract void RegisterActionOnTile(int tile, AerialVehicleArrivalAction arrivalAction);
	}
}
