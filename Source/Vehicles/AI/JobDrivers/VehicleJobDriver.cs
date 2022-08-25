using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public abstract class VehicleJobDriver : JobDriver
	{
		protected virtual VehiclePawn Vehicle => TargetA.Thing as VehiclePawn;

		public virtual IntVec3 JobCell => TargetB.Cell;

		protected abstract JobDef JobDef { get; }

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!JobCell.IsValid)
			{
				return false;
			}
			VehicleReservationManager reservationManager = pawn.Map.GetCachedMapComponent<VehicleReservationManager>();
			if (!reservationManager.CanReserve(Vehicle, pawn, JobDef))
			{
				return false;
			}
			return reservationManager.Reserve<LocalTargetInfo, VehicleTargetReservation>(Vehicle, pawn, job, JobCell);
		}
	}
}
