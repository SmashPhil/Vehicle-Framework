using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
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

		public override void Arrived(AerialVehicleInFlight aerialVehicle, int tile)
		{
			base.Arrived(aerialVehicle, tile);
			if (AerialVehicleArrivalAction_Trade.GetValidNegotiator(vehicle, settlement) == null)
			{
				Log.Warning($"No valid negotiator to trade with for {vehicle}.");
				return;
			}

			// Valid negotiator already verified, should never not find someone to take the role
			Pawn negotiator = WorldHelper.FindBestNegotiator(vehicle, settlement.Faction, settlement.TraderKind);
			Assert.IsNotNull(negotiator);

			Find.WindowStack.Add(new Dialog_Trade(negotiator, settlement, giftsOnly: true));
			PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(settlement.Goods.OfType<Pawn>(),
					"LetterRelatedPawnsTradingWithSettlement".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, false, true);
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
