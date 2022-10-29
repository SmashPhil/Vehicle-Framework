using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles
{
	public class VehiclePositionManager : MapComponent
	{
		private readonly Dictionary<VehiclePawn, HashSet<IntVec3>> occupiedRect = new Dictionary<VehiclePawn, HashSet<IntVec3>>();

		public VehiclePositionManager(Map map) : base(map)
		{
			occupiedRect = new Dictionary<VehiclePawn, HashSet<IntVec3>>();
		}

		public bool PositionClaimed(IntVec3 cell) => occupiedRect.Values.Any(h => h.Contains(cell));

		public VehiclePawn ClaimedBy(IntVec3 cell) => occupiedRect.FirstOrDefault(v => v.Value.Contains(cell)).Key;

		public void ClaimPosition(VehiclePawn vehicle)
		{
			CellRect newRect = vehicle.VehicleRect();
			HashSet<IntVec3> hash = newRect.Cells.ToHashSet();
			occupiedRect[vehicle] = hash;

			vehicle.RecalculateFollowerCell();
			if (ClaimedBy(vehicle.FollowerCell) is VehiclePawn blockedVehicle)
			{
				blockedVehicle.RecalculateFollowerCell();
			}
		}

		public bool ReleaseClaimed(VehiclePawn vehicle)
		{
			return occupiedRect.Remove(vehicle);
		}
	}
}
