using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public abstract class AerialVehicleArrivalAction_LandInMap : AerialVehicleArrivalAction
	{
		protected int tile;
		protected MapParent mapParent;

		public AerialVehicleArrivalAction_LandInMap()
		{
		}

		public AerialVehicleArrivalAction_LandInMap(VehiclePawn vehicle, MapParent mapParent, int tile) : base(vehicle)
		{
			this.tile = tile;
			this.mapParent = mapParent;
		}

		public override bool DestroyOnArrival => true;

		public override bool Arrived(int tile)
		{
			ExecuteEvents();
			return true;
		}

		protected virtual void ExecuteEvents()
		{
			vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLanding].ExecuteEvents();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref tile, "tile");
			Scribe_References.Look(ref mapParent, "mapParent");
		}
	}
}
