using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public class VehicleWorldObjectsHolder : WorldComponent
	{
		private static List<AerialVehicleInFlight> aerialVehicles = new List<AerialVehicleInFlight>();

		private static List<VehicleCaravan> vehicleCaravans = new List<VehicleCaravan>();

		private static List<DockedBoat> dockedBoats = new List<DockedBoat>();

		public VehicleWorldObjectsHolder(World world) : base(world)
		{ 
			if (aerialVehicles is null)
			{
				aerialVehicles = new List<AerialVehicleInFlight>();
			}
			if (vehicleCaravans is null)
			{
				vehicleCaravans = new List<VehicleCaravan>();
			}
			if (dockedBoats is null)
			{
				dockedBoats = new List<DockedBoat>();
			}
			aerialVehicles.RemoveAll(a => a is null);
			vehicleCaravans.RemoveAll(c => c is null);
			dockedBoats.RemoveAll(b => b is null);
		}

		public List<AerialVehicleInFlight> AerialVehicles => aerialVehicles;
		public List<VehicleCaravan> VehicleCaravans => vehicleCaravans;
		public List<DockedBoat> DockedBoats => dockedBoats;

		public AerialVehicleInFlight AerialVehicleObject(VehiclePawn vehicle)
		{
			return AerialVehicles.FirstOrDefault(a => a.vehicle == vehicle);
		}

		public VehicleCaravan VehicleCaravanObject(VehiclePawn vehicle)
		{
			return VehicleCaravans.FirstOrDefault(c => c.PawnsListForReading.Contains(vehicle));
		}

		public DockedBoat DockedBoatsObject(VehiclePawn vehicle)
		{
			return DockedBoats.FirstOrDefault(d => d.dockedBoats.Contains(vehicle));
		}

		public void Recache()
		{
			aerialVehicles.Clear();
			vehicleCaravans.Clear();
			dockedBoats.Clear();
		}

		public void AddToCache(WorldObject o)
		{ 
			if (o is AerialVehicleInFlight aerial)
			{
				aerialVehicles.Add(aerial);
			}
			else if (o is VehicleCaravan caravan)
			{
				vehicleCaravans.Add(caravan);
			}
			else if (o is DockedBoat dockedBoat)
			{
				dockedBoats.Add(dockedBoat);
			}
		}

		public void RemoveFromCache(WorldObject o)
		{
			if (o is AerialVehicleInFlight aerial)
			{
				aerialVehicles.Remove(aerial);
			}
			else if (o is VehicleCaravan caravan)
			{
				vehicleCaravans.Remove(caravan);
			}
			else if (o is DockedBoat dockedBoat)
			{
				dockedBoats.Remove(dockedBoat);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref aerialVehicles, "aerialVehicles", LookMode.Reference);
			Scribe_Collections.Look(ref vehicleCaravans, "vehicleCaravans", LookMode.Reference);
			Scribe_Collections.Look(ref dockedBoats, "dockedBoats", LookMode.Reference);
		}
	}
}
