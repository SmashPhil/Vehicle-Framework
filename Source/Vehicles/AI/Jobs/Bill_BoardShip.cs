using Verse;

namespace Vehicles
{
	public class Bill_BoardShip : IExposable
	{
		public VehicleHandler handler;
		public Pawn pawnToBoard;

		public Bill_BoardShip()
		{

		}

		public Bill_BoardShip(Pawn newBoard, VehicleHandler newHandler)
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