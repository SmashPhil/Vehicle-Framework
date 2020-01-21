using Verse;
using Verse.AI;

namespace RimShips.Jobs
{
    internal class Toils_Board
    {
        public static Toil BoardShip(Pawn pawnBoarding, TargetIndex index)
        {
            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                CompShips ship = toil.actor.jobs.curJob.GetTarget(index).Thing.TryGetComp<CompShips>();
                ship.Notify_Boarded(pawnBoarding);
                bool flag = !pawnBoarding.IsColonist;

                if(!flag)
                {
                    foreach(ShipHandler handler in ship.handlers)
                    {
                        if(handler.AreSlotsAvailable)
                        {
                            ship.GiveLoadJob(pawnBoarding, handler);
                            ship.ReserveSeat(pawnBoarding, handler);
                            break;
                        }
                    }
                }
                else
                {
                    ShipHandler handler = ship.handlers.Find(x => x.role.handlingType == HandlingTypeFlags.None && x.AreSlotsAvailable);
                    if(handler is null)
                        handler = ship.handlers.Find(x => x.AreSlotsAvailable);
                    if(handler is null) Log.Error("Could not find spot for " + pawnBoarding.LabelShort + " to board.");
                    ship.GiveLoadJob(pawnBoarding, handler);
                    ship.ReserveSeat(pawnBoarding, handler);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
