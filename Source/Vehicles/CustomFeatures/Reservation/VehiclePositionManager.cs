using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Reservation manager for positions of vehicle, reserves entire hitbox of vehicle
	/// </summary>
	/// <remarks>Only ever read / written to from MainThread</remarks>
	public class VehiclePositionManager : DetachedMapComponent
	{
		private readonly Dictionary<IntVec3, VehiclePawn> occupiedCells = new Dictionary<IntVec3, VehiclePawn>();
		private readonly Dictionary<VehiclePawn, List<IntVec3>> occupiedRect = new Dictionary<VehiclePawn, List<IntVec3>>();

		public VehiclePositionManager(Map map) : base(map)
		{
			occupiedCells = new Dictionary<IntVec3, VehiclePawn>();
			occupiedRect = new Dictionary<VehiclePawn, List<IntVec3>>();
		}

		public bool PositionClaimed(IntVec3 cell) => ClaimedBy(cell) != null;

		public VehiclePawn ClaimedBy(IntVec3 cell) => occupiedCells.TryGetValue(cell, null);

		public List<IntVec3> ClaimedBy(VehiclePawn vehicle) => occupiedRect.TryGetValue(vehicle, null);

		public void ClaimPosition(VehiclePawn vehicle)
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
