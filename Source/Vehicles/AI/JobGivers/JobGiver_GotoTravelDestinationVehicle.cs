using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class JobGiver_GotoTravelDestinationVehicle : JobGiver_GotoTravelDestination
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn is VehiclePawn vehicle)
			{
				IntVec3 cell = pawn.mindState.duty.focus.Cell;
				if (vehicle.Position == cell)
				{
					return null;
				}
				if (!vehicle.CanReachVehicle(cell, PathEndMode.OnCell, PawnUtility.ResolveMaxDanger(pawn, maxDanger), TraverseMode.ByPawn))
				{
					return null;
				}
				Job job = new Job(JobDefOf.Goto, cell)
				{
					locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, locomotionUrgency),
					expiryInterval = jobMaxDuration
				};
				if (vehicle.InhabitedCellsProjected(cell, Rot8.Invalid).NotNullAndAny(cell => pawn.Map.exitMapGrid.IsExitCell(cell)))
				{
					job.exitMapOnArrival = true;
				}
				return job;
			}
			return null;
		}
	}
}