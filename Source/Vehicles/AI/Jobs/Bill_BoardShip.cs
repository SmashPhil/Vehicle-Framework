using Verse;

namespace Vehicles
{
	public class Bill_BoardVehicle : IExposable
	{
		public VehicleHandler handler;
		public Pawn pawnToBoard;

		public Bill_BoardVehicle()
		{

		}

		public Bill_BoardVehicle(Pawn newBoard, VehicleHandler newHandler)
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