using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles
{
    public class AerialVehicleArrivalAction_Trade : AerialVehicleArrivalAction_VisitSettlement
    {
        public AerialVehicleArrivalAction_Trade()
        {

        }

        public AerialVehicleArrivalAction_Trade(VehiclePawn vehicle, Settlement settlement) : base(vehicle, settlement)
        {
        }

        public override bool Arrived(int tile)
        {
            base.Arrived(tile);
            if (GetValidNegotiator(vehicle, settlement) == null)
			{
                return false;
			}

            Pawn negotiator = WorldHelper.FindBestNegotiator(vehicle, settlement.Faction, settlement.TraderKind);

            if (negotiator == null)
			{
                return false;
			}
            Find.WindowStack.Add(new Dialog_Trade(negotiator, settlement));
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(settlement.Goods.OfType<Pawn>(),
                "LetterRelatedPawnsTradingWithSettlement".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, false, true);

            return true;
        }

        public override FloatMenuAcceptanceReport StillValid(int destinationTile) => base.StillValid(destinationTile) && CanTradeWith(vehicle, settlement);

        public static bool ValidGiftOrTradePartner(Settlement settlement)
        {
            return settlement != null && settlement.Spawned && !settlement.HasMap && settlement.Faction != null && settlement.Faction != Faction.OfPlayer
                && !settlement.Faction.def.permanentEnemy && settlement.CanTradeNow;
        }

        public static FloatMenuAcceptanceReport CanTradeWith(VehiclePawn vehicle, Settlement settlement)
        {
            return ValidGiftOrTradePartner(settlement) && !settlement.Faction.HostileTo(Faction.OfPlayer) && GetValidNegotiator(vehicle, settlement) != null;
        }

        public static Pawn GetValidNegotiator(VehiclePawn vehicle, Settlement settlement)
		{
            foreach (Pawn pawn in vehicle.AllPawnsAboard)
            {
                if (!pawn.Dead && !pawn.Downed && !pawn.InMentalState && !StatDefOf.TradePriceImprovement.Worker.IsDisabledFor(pawn) && FactionUtility.CanTradeWith(pawn, settlement.Faction, settlement.TraderKind))
                {
                    return pawn;
				}
            }
            return null;
        }
    }
}
