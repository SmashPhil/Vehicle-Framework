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

		public override void Arrived(int tile)
		{
			Messages.Message($"ARRIVED AT {tile}", MessageTypeDefOf.PositiveEvent);
		}

		public static bool CanFormCaravanAt(VehiclePawn vehicle, int tile)
		{
			return vehicle.AllPawnsAboard.Count > 0 && Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().Passable(tile, vehicle.VehicleDef);
		}
	}
}
