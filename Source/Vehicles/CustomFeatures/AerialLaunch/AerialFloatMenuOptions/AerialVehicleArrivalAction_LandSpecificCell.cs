using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class AerialVehicleArrivalAction_LandSpecificCell : AerialVehicleArrivalAction_LandInMap
	{
		protected IntVec3 landingCell;
		protected Rot4 landingRot;

		public AerialVehicleArrivalAction_LandSpecificCell()
		{
		}

		public AerialVehicleArrivalAction_LandSpecificCell(VehiclePawn vehicle, MapParent mapParent, int tile, IntVec3 landingCell, Rot4 landingRot) : base(vehicle, mapParent, tile)
		{
			this.tile = tile;
			this.mapParent = mapParent;
			this.landingCell = landingCell;
			this.landingRot = landingRot;
		}

		public override FloatMenuAcceptanceReport StillValid(int destinationTile)
		{
			return WorldVehiclePathGrid.Instance.Passable(tile, vehicle.VehicleDef);
		}

		public override void Arrived(int tile)
		{
			base.Arrived(tile);
			VehicleSkyfaller_Arriving skyfaller = (VehicleSkyfaller_Arriving)VehicleSkyfallerMaker.MakeSkyfaller(vehicle.CompVehicleLauncher.Props.skyfallerIncoming, vehicle);
			Rot4 vehicleRotation = vehicle.CompVehicleLauncher.launchProtocol.landingProperties.forcedRotation ?? landingRot;
			GenSpawn.Spawn(skyfaller, landingCell, mapParent.Map, vehicleRotation);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref landingCell, "landingCell");
			Scribe_Values.Look(ref landingRot, "landingRot");
		}
	}
}
