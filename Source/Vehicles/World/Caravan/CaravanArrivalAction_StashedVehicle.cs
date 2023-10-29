using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public class CaravanArrivalAction_StashedVehicle : CaravanArrivalAction
	{
		private StashedVehicle stashedVehicle;

		public CaravanArrivalAction_StashedVehicle()
		{
		}

		public CaravanArrivalAction_StashedVehicle(StashedVehicle stashedVehicle)
		{
			this.stashedVehicle = stashedVehicle;
		}

		public override string Label => "CommandUndockShip".Translate(stashedVehicle.Label);

		public override string ReportString => "CaravanVisiting".Translate(stashedVehicle.Label);

		public override FloatMenuAcceptanceReport StillValid(Caravan caravan, int destinationTile)
		{
			FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
			if (!floatMenuAcceptanceReport)
			{
				return floatMenuAcceptanceReport;
			}
			if(stashedVehicle != null && stashedVehicle.Tile != destinationTile)
			{
				return false;
			}
			return CanVisit(caravan, stashedVehicle);
		}

		public override void Arrived(Caravan caravan)
		{
			stashedVehicle.Notify_CaravanArrived(caravan);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref stashedVehicle, "stashedVehicle", false);
		}

		public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, StashedVehicle stashedVehicle)
		{
			return stashedVehicle != null && stashedVehicle.Spawned;
		}

		public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, StashedVehicle stashedVehicle)
		{
			return CaravanArrivalActionUtility.GetFloatMenuOptions(() => CanVisit(caravan, stashedVehicle), () => new CaravanArrivalAction_StashedVehicle(stashedVehicle),
				"CommandUndockShip".Translate(stashedVehicle.Label), caravan, stashedVehicle.Tile, stashedVehicle);
		}
	}
}
