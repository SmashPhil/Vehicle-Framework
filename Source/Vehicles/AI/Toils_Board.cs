using System.Linq;
using Vehicles.Lords;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles.Jobs
{
    internal class Toils_Board
    {
        public static Toil BoardShip(Pawn pawnBoarding)
        {
            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                VehiclePawn vehicle = toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing as VehiclePawn;
                Pair<VehiclePawn, VehicleHandler> assignedSeat;
                if(pawnBoarding.GetLord()?.LordJob is LordJob_FormAndSendVehicles lordJob)
                {
                    assignedSeat = lordJob.GetVehicleAssigned(pawnBoarding);
                }
                else
                {
                    VehicleHandler handler = vehicle.GetCachedComp<CompVehicle>().bills.FirstOrDefault(b => b.pawnToBoard == pawnBoarding).handler;
                    if (handler is null)
                    {
                        handler = vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().GetReservation<VehicleHandlerReservation>(vehicle)?.ReservedHandler(pawnBoarding);
                        if (handler is null)
                        {
                            Log.Error("Could not find assigned spot for " + pawnBoarding.LabelShort + " to board.");
                            return;
                        }
                    }
                    assignedSeat = new Pair<VehiclePawn, VehicleHandler>(vehicle, handler);
                }
                

                //REDO - place nonhumanlike in cargo area

                if (assignedSeat.Second is null)
                {
                    Log.Error($"{VehicleHarmony.LogLabel} VehicleHandler is null. This should never happen as assigned seating either handles arrangements or instructs pawns to follow rather than board.");
                }
                assignedSeat.First.GetCachedComp<CompVehicle>().GiveLoadJob(pawnBoarding, assignedSeat.Second);
                assignedSeat.First.GetCachedComp<CompVehicle>().Notify_Boarded(pawnBoarding);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
