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

		public virtual bool CanArriveInMap => mapParent?.Map != null;

		public override FloatMenuAcceptanceReport StillValid(int destinationTile)
		{
			return WorldVehiclePathGrid.Instance.Passable(tile, vehicle.VehicleDef);
		}

		public override bool Arrived(int tile)
		{
			if (!base.Arrived(tile))
			{
				return false;
			}
			if (!CanArriveInMap)
			{
				return false;
			}
			SpawnSkyfaller();
			ExecuteEvents();
			return true;
		}

		protected virtual void SpawnSkyfaller()
		{
			VehicleSkyfaller_Arriving skyfaller = (VehicleSkyfaller_Arriving)VehicleSkyfallerMaker.MakeSkyfaller(vehicle.CompVehicleLauncher.Props.skyfallerIncoming, vehicle);
			skyfaller.rotatePostLanding = landingRot;
			Rot4 vehicleRotation = vehicle.CompVehicleLauncher.launchProtocol.LandingProperties?.forcedRotation ?? landingRot;
			GenSpawn.Spawn(skyfaller, landingCell, mapParent.Map, vehicleRotation);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref landingCell, nameof(landingCell));
			Scribe_Values.Look(ref landingRot, nameof(landingRot));
		}
	}
}
