using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class ListerVehiclesRepairable : MapComponent
	{
		private Dictionary<Faction, VehicleRepairsCollection> vehiclesToRepair = new Dictionary<Faction, VehicleRepairsCollection>();

		public ListerVehiclesRepairable(Map map) : base(map)
		{
		}

		public HashSet<VehiclePawn> RepairsForFaction(Faction faction)
		{
			if (vehiclesToRepair.TryGetValue(faction, out VehicleRepairsCollection vehicles))
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
			if (vehicle.statHandler.NeedsRepairs && !Mathf.Approximately(vehicle.GetStatValue(VehicleStatDefOf.BodyIntegrity), 0))
			{
				if (vehiclesToRepair.TryGetValue(vehicle.Faction, out var vehicles))
				{
					if (vehicle.Spawned)
					{
						vehicles.Add(vehicle);
					}
					else 
					{
						vehicles.Remove(vehicle);
					}
				}
				else if (vehicle.Spawned)
				{
					vehiclesToRepair[vehicle.Faction] = new VehicleRepairsCollection();
					vehiclesToRepair[vehicle.Faction].Add(vehicle);
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

		//TODO - revisit for saving
		//Note: May not actually need to be saved, vehicles spawning in will recache status
		public override void ExposeData()
		{
			base.ExposeData();
			//Scribe_Collections.Look(ref vehiclesToRepair, nameof(vehiclesToRepair), LookMode.Reference, LookMode.Deep, ref factions_tmp, ref vehicleRepairs_tmp);
		}

		private class VehicleRepairsCollection : IExposable
		{
			private HashSet<VehiclePawn> requests = new HashSet<VehiclePawn>();

			public static implicit operator HashSet<VehiclePawn>(VehicleRepairsCollection collection)
			{
				return collection.requests;
			}

			public bool Add(VehiclePawn vehicle) => requests.Add(vehicle);

			public bool Remove(VehiclePawn vehicle) => requests.Remove(vehicle);

			public void ExposeData()
			{
				Scribe_Collections.Look(ref requests, nameof(requests), LookMode.Reference);
			}
		}
	}
}
