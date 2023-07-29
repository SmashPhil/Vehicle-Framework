using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles
{
    public class AerialVehicleArrivalAction_OfferGifts : AerialVehicleArrivalAction_VisitSettlement
    {
        public AerialVehicleArrivalAction_OfferGifts()
        {

        }

        public AerialVehicleArrivalAction_OfferGifts(VehiclePawn vehicle, Settlement settlement) : base(vehicle, settlement)
        {
        }

        public override bool Arrived(int tile)
        {
            base.Arrived(tile);
            if (AerialVehicleArrivalAction_Trade.GetValidNegotiator(vehicle, settlement) == null)
			{
                return false;
			}

            Pawn negotiator = WorldHelper.FindBestNegotiator(vehicle, settlement.Faction, settlement.TraderKind);

            if (negotiator == null)
			{
                return false;
			}

            Find.WindowStack.Add(new Dialog_Trade(negotiator, settlement, giftsOnly: true));
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(settlement.Goods.OfType<Pawn>(),
                "LetterRelatedPawnsTradingWithSettlement".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, false, true);

            return true;
        }

        public override FloatMenuAcceptanceReport StillValid(int destinationTile) => base.StillValid(destinationTile) && AerialVehicleArrivalAction_Trade.CanTradeWith(vehicle, settlement);

        /// <summary>
        /// AerialVehicle <paramref name="vehicle"/> can offer gifts to <paramref name="settlement"/>
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="settlement"></param>
        public static FloatMenuAcceptanceReport CanOfferGiftsTo(VehiclePawn vehicle, Settlement settlement)
        {
            return AerialVehicleArrivalAction_Trade.ValidGiftOrTradePartner(settlement) && settlement.Faction.HostileTo(Faction.OfPlayer) && AerialVehicleArrivalAction_Trade.GetValidNegotiator(vehicle, settlement) != null;
        }
    }
}
