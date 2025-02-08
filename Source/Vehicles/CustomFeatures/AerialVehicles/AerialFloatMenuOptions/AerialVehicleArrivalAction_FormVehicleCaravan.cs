using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class AerialVehicleArrivalAction_FormVehicleCaravan : AerialVehicleArrivalAction
	{
		public AerialVehicleArrivalAction_FormVehicleCaravan()
		{
		}
		public AerialVehicleArrivalAction_FormVehicleCaravan(VehiclePawn vehicle) : base(vehicle)
		{
		}

		public override FloatMenuAcceptanceReport StillValid(int destinationTile)
		{
			return !Find.World.Impassable(destinationTile);
		}

		public override void Arrived(AerialVehicleInFlight aerialVehicle, int tile)
		{
			// SwitchToCaravan handles destroying aerial vehicle object
			aerialVehicle.SwitchToCaravan();
		}

		public static bool CanFormCaravanAt(VehiclePawn vehicle, int tile)
		{
			return WorldVehiclePathGrid.Instance.Passable(tile, vehicle.VehicleDef);
		}
	}
}
