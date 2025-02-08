using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class AerialVehicleArrivalAction_VisitSettlement : AerialVehicleArrivalAction_FormVehicleCaravan
	{
		public Settlement settlement;

		public AerialVehicleArrivalAction_VisitSettlement()
		{
		}

		public AerialVehicleArrivalAction_VisitSettlement(VehiclePawn vehicle, Settlement settlement) 
																											: base(vehicle)
		{
			this.settlement = settlement;
		}

		public override FloatMenuAcceptanceReport StillValid(int destinationTile)
		{
			if (settlement != null && settlement.Tile != destinationTile)
			{
				return false;
			}
			return CanVisit(vehicle, settlement);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref settlement, "settlement");
		}

		public static FloatMenuAcceptanceReport CanVisit(VehiclePawn vehicle, Settlement settlement)
		{
			if (settlement is null || !settlement.Spawned || !settlement.Visitable)
			{
				return false;
			}
			if (!WorldVehiclePathGrid.Instance.Passable(settlement.Tile, vehicle.VehicleDef))
			{

				return false;
			}
			return true;
		}

		public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(VehiclePawn vehicle, Settlement settlement)
		{
			return VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanVisit(vehicle, settlement),
				() => new AerialVehicleArrivalAction_VisitSettlement(vehicle, settlement),
				"VisitSettlement".Translate(settlement.Label), vehicle, settlement.Tile, null);
		}
	}
}
