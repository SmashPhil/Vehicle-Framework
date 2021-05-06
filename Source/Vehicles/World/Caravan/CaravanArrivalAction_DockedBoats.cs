using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public class CaravanArrivalAction_DockedBoats : CaravanArrivalAction
	{
		private DockedBoat dockedBoat;

		public CaravanArrivalAction_DockedBoats()
		{
		}

		public CaravanArrivalAction_DockedBoats(DockedBoat dockedBoat)
		{
			this.dockedBoat = dockedBoat;
		}

		public override string Label => "CommandUndockShip".Translate(dockedBoat.Label);

		public override string ReportString => "CaravanVisiting".Translate(dockedBoat.Label);

		public override FloatMenuAcceptanceReport StillValid(Caravan caravan, int destinationTile)
		{
			FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
			if(!floatMenuAcceptanceReport) return floatMenuAcceptanceReport;
			if(dockedBoat != null && dockedBoat.Tile != destinationTile) return false;
			return CanVisit(caravan, dockedBoat);
		}

		public override void Arrived(Caravan caravan)
		{
			dockedBoat.Notify_CaravanArrived(caravan);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref dockedBoat, "dockedBoat", false);
		}

		public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, DockedBoat dockedBoat)
		{
			return dockedBoat != null && dockedBoat.Spawned;
		}

		public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, DockedBoat dockedBoat)
		{
			return CaravanArrivalActionUtility.GetFloatMenuOptions(() => CanVisit(caravan, dockedBoat), () => new CaravanArrivalAction_DockedBoats(dockedBoat),
				"CommandUndockShip".Translate(dockedBoat.Label), caravan, dockedBoat.Tile, dockedBoat);
		}
	}
}
