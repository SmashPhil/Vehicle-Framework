using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Tracks which region sets need to be updated
	/// </summary>
	public class VehicleRegionUpdateCatalog //: DetachedMapComponent
	{
		private int[] vehicleCounts;
		private HashSet<VehicleDef> updateWanters = new HashSet<VehicleDef>();

		public VehicleRegionUpdateCatalog(Map map) //: base(map)
		{
			vehicleCounts = new int[DefDatabase<VehicleDef>.AllDefsListForReading.Count];
		}

		public void Notify_VehicleSpawned(VehiclePawn vehicle)
		{
			vehicleCounts[vehicle.VehicleDef.DefIndex]++;
			if (vehicleCounts[vehicle.VehicleDef.DefIndex] > 0)
			{
				updateWanters.Add(vehicle.VehicleDef);
			}
		}

		public void Notify_VehicleDespawned(VehiclePawn vehicle)
		{
			vehicleCounts[vehicle.VehicleDef.DefIndex]--;
			if (vehicleCounts[vehicle.VehicleDef.DefIndex] == 0)
			{
				updateWanters.Remove(vehicle.VehicleDef);
			}
		}
	}
}
