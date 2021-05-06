using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class ListerVehiclesRepairable : MapComponent
	{
		private readonly Dictionary<Faction, HashSet<VehiclePawn>> vehiclesToRepair = new Dictionary<Faction, HashSet<VehiclePawn>>();

		public ListerVehiclesRepairable(Map map) : base(map)
		{
		}

		public HashSet<VehiclePawn> RepairsForFaction(Faction faction)
		{
			if (vehiclesToRepair.TryGetValue(faction, out var vehicles))
			{
				return vehicles;
			}
			return new HashSet<VehiclePawn>();
		}

		public void Notify_VehicleSpawned(VehiclePawn vehicle)
		{
			Notify_VehicleRepaired(vehicle);
			Notify_VehicleTookDamage(vehicle);
		}

		public void Notify_VehicleDespawned(VehiclePawn vehicle)
		{
			if (vehiclesToRepair.TryGetValue(vehicle.Faction, out var vehicles))
			{
				vehicles.Remove(vehicle);
			}
		}

		public void Notify_VehicleTookDamage(VehiclePawn vehicle)
		{
			if (vehicle.statHandler.NeedsRepairs)
			{
				if (vehiclesToRepair.TryGetValue(vehicle.Faction, out var vehicles))
				{
					vehicles.Add(vehicle);
				}
				else
				{
					vehiclesToRepair.Add(vehicle.Faction, new HashSet<VehiclePawn>() { vehicle });
				}
			}
		}

		public void Notify_VehicleRepaired(VehiclePawn vehicle)
		{
			if (!vehicle.statHandler.NeedsRepairs)
			{
				if (vehiclesToRepair.TryGetValue(vehicle.Faction, out var vehicles))
				{
					vehicles.Remove(vehicle);
				}
			}
		}

		//REDO - Implement saveability for vehicles needing repairs
		public override void ExposeData()
		{
			base.ExposeData();
			//Scribe_Collections.Look(ref vehiclesToRepair, "vehiclesToRepair", LookMode.Reference, LookMode.Reference);
		}
	}
}
