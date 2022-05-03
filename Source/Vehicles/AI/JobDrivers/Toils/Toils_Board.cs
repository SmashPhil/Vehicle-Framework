using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;

namespace Vehicles
{
	internal class Toils_Board
	{
		public static Toil BoardVehicle(Pawn pawnBoarding)
		{
			Toil toil = new Toil();
			toil.initAction = delegate ()
			{
				VehiclePawn vehicle = toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing as VehiclePawn;
				(VehiclePawn vehicle, VehicleHandler handler) assignedSeat;
				if (pawnBoarding.GetLord()?.LordJob is LordJob_FormAndSendVehicles lordJob)
				{
					assignedSeat = lordJob.GetVehicleAssigned(pawnBoarding);
				}
				else
				{
					VehicleHandler handler = vehicle.bills.FirstOrDefault(b => b.pawnToBoard == pawnBoarding)?.handler;
					if (handler is null)
					{
						handler = vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().GetReservation<VehicleHandlerReservation>(vehicle)?.ReservedHandler(pawnBoarding);
						if (handler is null)
						{
							Log.Error("Could not find assigned spot for " + pawnBoarding.LabelShort + " to board.");
							return;
						}
					}
					assignedSeat = (vehicle, handler);
				}

				//REDO - place nonhumanlike in cargo area
				if (assignedSeat.handler is null)
				{
					Log.Error($"{VehicleHarmony.LogLabel} VehicleHandler is null. This should never happen as assigned seating either handles arrangements or instructs pawns to follow rather than board.");
				}
				assignedSeat.vehicle.GiveLoadJob(pawnBoarding, assignedSeat.handler);
				assignedSeat.vehicle.Notify_Boarded(pawnBoarding);
			};
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			return toil;
		}
	}
}
