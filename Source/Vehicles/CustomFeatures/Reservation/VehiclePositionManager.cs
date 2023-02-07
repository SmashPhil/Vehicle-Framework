using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class VehiclePositionManager : DetachedMapComponent
	{
		private readonly Dictionary<IntVec3, VehiclePawn> occupiedCells = new Dictionary<IntVec3, VehiclePawn>();
		private readonly Dictionary<VehiclePawn, List<IntVec3>> occupiedRect = new Dictionary<VehiclePawn, List<IntVec3>>();

		private object positionLock = new object();

		public VehiclePositionManager(Map map) : base(map)
		{
			occupiedCells = new Dictionary<IntVec3, VehiclePawn>();
			occupiedRect = new Dictionary<VehiclePawn, List<IntVec3>>();
		}

		public bool PositionClaimed(IntVec3 cell) => ClaimedBy(cell) != null;

		public VehiclePawn ClaimedBy(IntVec3 cell) => occupiedCells.TryGetValue(cell, null);

		//Dictionary may be accessed from pathfinder while claiming a position, lock to avoid read / write simultaneously executing
		public void ClaimPosition(VehiclePawn vehicle)
		{
			lock (positionLock)
			{
				ReleaseClaimed(vehicle);
				List<IntVec3> newClaim = vehicle.VehicleRect().ToList();
				occupiedRect[vehicle] = newClaim;
				foreach (IntVec3 cell in newClaim)
				{
					occupiedCells[cell] = vehicle;
				}

				vehicle.RecalculateFollowerCell();
				if (ClaimedBy(vehicle.FollowerCell) is VehiclePawn blockedVehicle)
				{
					blockedVehicle.RecalculateFollowerCell();
				}
			}
		}

		//Only called from MainThread methods, no need for lock
		public bool ReleaseClaimed(VehiclePawn vehicle)
		{
			if (occupiedRect.TryGetValue(vehicle, out List<IntVec3> currentlyOccupied))
			{
				foreach (IntVec3 cell in currentlyOccupied)
				{
					occupiedCells.Remove(cell);
				}
			}
			return occupiedRect.Remove(vehicle);
		}
	}
}
