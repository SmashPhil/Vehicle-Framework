using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles
{
    public class AerialVehicleArrivalAction_Trade : AerialVehicleArrivalAction_VisitSettlement
    {
        public override void Arrived(int tile)
        {
            base.Arrived(tile);
            var aerialVehicleInFlight = vehicle.GetAerialVehicle();
            var negotiator = vehicle.AllPawnsAboard
                .Where(p => p.CanTradeWith(settlement.Faction, settlement.TraderKind) && !p.Dead && !p.Downed && !p.InMentalState &&
                                                               !StatDefOf.TradePriceImprovement.Worker.IsDisabledFor(p))
                .MaxBy(p => p.GetStatValue(StatDefOf.TradePriceImprovement));
            Find.WindowStack.Add(new Dialog_TradeAerialVehicle(aerialVehicleInFlight, negotiator, settlement));
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(settlement.Goods.OfType<Pawn>(),
                "LetterRelatedPawnsTradingWithSettlement".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, false, true);
        }

        public override FloatMenuAcceptanceReport StillValid(int destinationTile) => base.StillValid(destinationTile) && CanTradeWith(vehicle, settlement);

        public static bool CanTradeWith(VehiclePawn vehicle, Settlement settlement) =>
            settlement.Faction != null && settlement.Faction != Faction.OfPlayer &&
            vehicle.AllPawnsAboard.Any(pawn => pawn.CanTradeWith(settlement.Faction, settlement.TraderKind)) &&
            !settlement.HasMap && !settlement.Faction.def.permanentEnemy &&
            !settlement.Faction.HostileTo(Faction.OfPlayer) && settlement.CanTradeNow;

        public AerialVehicleArrivalAction_Trade()
        {

        }
        public AerialVehicleArrivalAction_Trade(VehiclePawn vehicle, Settlement settlement) : base(vehicle, settlement)
        {
        }
    }
}
