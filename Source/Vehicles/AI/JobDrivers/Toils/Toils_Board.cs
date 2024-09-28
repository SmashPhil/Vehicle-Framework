using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;
using RimWorld;

namespace Vehicles
{
	internal class Toils_Board
	{
		public static Toil BoardVehicle(Pawn pawn)
		{
			Toil toil = new Toil();
			toil.initAction = delegate ()
			{
				VehiclePawn vehicle = toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing as VehiclePawn;
				if (pawn.GetLord()?.LordJob is LordJob_FormAndSendVehicles lordJob)
				{
					(VehiclePawn vehicle, VehicleHandler handler)  assignedSeat = lordJob.GetVehicleAssigned(pawn);
					assignedSeat.vehicle.TryAddPawn(pawn, assignedSeat.handler);
					return;
				}
				vehicle.Notify_Boarded(pawn);
				ThrowAppropriateHistoryEvent(vehicle.VehicleDef.vehicleType, toil.actor);

            };
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			return toil;
		}

		public static void ThrowAppropriateHistoryEvent(VehicleType type,Pawn pawn)
		{
            if (ModsConfig.IdeologyActive)
            {
                switch (type)
                {
                    case VehicleType.Air:
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf_Vehicles.VF_BoardedAirVehicle, pawn.Named(HistoryEventArgsNames.Doer)));
                        break;
                    case VehicleType.Sea:
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf_Vehicles.VF_BoardedSeaVehicle, pawn.Named(HistoryEventArgsNames.Doer)));
                        break;
                    case VehicleType.Land:
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf_Vehicles.VF_BoardedLandVehicle, pawn.Named(HistoryEventArgsNames.Doer)));
                        break;
                    case VehicleType.Universal:
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf_Vehicles.VF_BoardedUniversalVehicle, pawn.Named(HistoryEventArgsNames.Doer)));
                        break;

                }
            }
            
        }
	}
}
