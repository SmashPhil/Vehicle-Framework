﻿using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using SmashTools;
using System.Collections.Concurrent;

namespace Vehicles
{
	/// <summary>
	/// Reservation manager for positions of vehicle, reserves entire hitbox of vehicle
	/// </summary>
	/// <remarks>Only ever read / written to from MainThread</remarks>
	public class VehiclePositionManager : DetachedMapComponent
	{
		private readonly ConcurrentDictionary<IntVec3, VehiclePawn> occupiedCells = new ConcurrentDictionary<IntVec3, VehiclePawn>();
		private readonly ConcurrentDictionary<VehiclePawn, CellRect> occupiedRect = new ConcurrentDictionary<VehiclePawn, CellRect>();

		public VehiclePositionManager(Map map) : base(map)
		{
			occupiedCells = new ConcurrentDictionary<IntVec3, VehiclePawn>();
			occupiedRect = new ConcurrentDictionary<VehiclePawn, CellRect>();
		}

		public List<VehiclePawn> AllClaimants => occupiedRect.Keys.ToList();

		public bool PositionClaimed(IntVec3 cell) => ClaimedBy(cell) != null;

		public VehiclePawn ClaimedBy(IntVec3 cell) => occupiedCells.TryGetValue(cell, null);

		public CellRect ClaimedBy(VehiclePawn vehicle) => occupiedRect.TryGetValue(vehicle, default);

		public void ClaimPosition(VehiclePawn vehicle)
		{
			ReleaseClaimed(vehicle);
			CellRect occupiedRect = vehicle.VehicleRect();
			this.occupiedRect[vehicle] = occupiedRect;
			foreach (IntVec3 cell in occupiedRect)
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
			if (occupiedRect.TryGetValue(vehicle, out CellRect rect))
			{
				foreach (IntVec3 cell in rect)
				{
					occupiedCells.TryRemove(cell, out _);
				}
			}
			return occupiedRect.TryRemove(vehicle, out _);
		}
	}
}
