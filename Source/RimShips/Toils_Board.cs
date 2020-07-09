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
                if(pawnBoarding.GetLord()?.LordJob is LordJob_FormAndSendVehicles)
                {
                    assignedSeat = (pawnBoarding.GetLord().LordJob as LordJob_FormAndSendVehicles).GetShipAssigned(pawnBoarding);
                }
                else
                {
                    VehicleHandler handler = vehicle.TryGetComp<CompVehicle>().handlers.Find(x => !x.role.handlingTypes.NullOrEmpty() && x.AreSlotsAvailable);
                    if (handler is null)
                    {
                        handler = vehicle.TryGetComp<CompVehicle>().ReservedHandler(pawnBoarding);
                        
                        if (handler is null)
                        {
                            Log.Error("Could not find spot for " + pawnBoarding.LabelShort + " to board.");
                            return;
                        }
                    }
                    Log.Message($"Pawn: {pawnBoarding.LabelShort} Handler: {handler.role.label}");
                    assignedSeat = new Pair<VehiclePawn, VehicleHandler>(vehicle, handler);
                }
                assignedSeat.First.GetComp<CompVehicle>().Notify_Boarded(pawnBoarding);

                //REDO - place nonhumanlike in cargo area

                if (assignedSeat.Second is null)
                {
                    Log.Error("[Vehicles] VehicleHandler is null. This should never happen as assigned seating either handles arrangements or instructs pawns to follow rather than board.");
                }

                assignedSeat.First.GetComp<CompVehicle>().GiveLoadJob(pawnBoarding, assignedSeat.Second);
                assignedSeat.First.GetComp<CompVehicle>().ReserveSeat(pawnBoarding, assignedSeat.Second);

            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
