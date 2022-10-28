using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			return reservationManager.Reserve<LocalTargetInfo, VehicleTargetReservation>(Vehicle, pawn, job, JobCell);
		}
	}
}
