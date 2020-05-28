using Verse;

namespace Vehicles.Jobs
{
    public class Bill_BoardShip : IExposable
    {
        public ShipHandler handler;
        public Pawn pawnToBoard;

        public Bill_BoardShip()
        {

        }

        public Bill_BoardShip(Pawn newBoard, ShipHandler newHandler)
        {
            pawnToBoard = newBoard;
            handler = newHandler;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawnToBoard, "pawnToBoard");
            Scribe_References.Look(ref handler, "handler");
        }
    }
}