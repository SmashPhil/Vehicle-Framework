using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class AerialVehicleArrivalAction_FormVehicleCaravan : AerialVehicleArrivalAction
	{
		public  AerialVehicleArrivalAction_FormVehicleCaravan()
		{
		}
		public AerialVehicleArrivalAction_FormVehicleCaravan(VehiclePawn vehicle) : base(vehicle)
		{
		}

		public override FloatMenuAcceptanceReport StillValid(int destinationTile)
		{
			return !Find.World.Impassable(destinationTile);
		}

		public override bool Arrived(int tile)
		{
			return true;
		}

		public static bool CanFormCaravanAt(VehiclePawn vehicle, int tile)
		{
			return WorldVehiclePathGrid.Instance.Passable(tile, vehicle.VehicleDef);
		}
	}
}
