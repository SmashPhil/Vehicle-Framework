using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace RimShips
{
    public class CaravanArrivalAction_DockedBoats : CaravanArrivalAction
    {
        public CaravanArrivalAction_DockedBoats()
        {
        }

        public CaravanArrivalAction_DockedBoats(DockedBoat dockedBoat)
        {
            this.dockedBoat = dockedBoat;
        }

        public override string Label => "CommandUndockShip".Translate(this.dockedBoat.Label);

        public override string ReportString => "CaravanVisiting".Translate(this.dockedBoat.Label);

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, int destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
            if(!floatMenuAcceptanceReport)
                return floatMenuAcceptanceReport;
            if(this.dockedBoat != null && this.dockedBoat.Tile != destinationTile)
                return false;
            return CaravanArrivalAction_DockedBoats.CanVisit(caravan, this.dockedBoat);
        }

        public override void Arrived(Caravan caravan)
        {
            this.dockedBoat.Notify_CaravanArrived(caravan);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<DockedBoat>(ref this.dockedBoat, "dockedBoat", false);
        }

        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, DockedBoat dockedBoat)
        {
            return dockedBoat != null && dockedBoat.Spawned;
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, DockedBoat dockedBoat)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_DockedBoats>(() => CaravanArrivalAction_DockedBoats.CanVisit(caravan, dockedBoat), () => new CaravanArrivalAction_DockedBoats(dockedBoat),
                "CommandUndockShip".Translate(dockedBoat.Label), caravan, dockedBoat.Tile, dockedBoat);
        }

        private DockedBoat dockedBoat;
    }
}
