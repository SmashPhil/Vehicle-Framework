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
		protected LaunchProtocol launchProtocol;

		public AerialVehicleArrivalAction_LandInMap()
		{
		}

		public AerialVehicleArrivalAction_LandInMap(VehiclePawn vehicle, MapParent mapParent, int tile, LaunchProtocol launchProtocol) : base(vehicle)
		{
			this.tile = tile;
			this.mapParent = mapParent;
			this.launchProtocol = launchProtocol;
		}

		public override bool DestroyOnArrival => true;

		public override void Arrived(int tile)
		{
			vehicle.EventRegistry[VehicleEventDefOf.AerialLanding].ExecuteEvents();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref tile, "tile");
			Scribe_References.Look(ref mapParent, "mapParent");
			Scribe_Deep.Look(ref launchProtocol, "launchProtocol");
		}
	}
}
