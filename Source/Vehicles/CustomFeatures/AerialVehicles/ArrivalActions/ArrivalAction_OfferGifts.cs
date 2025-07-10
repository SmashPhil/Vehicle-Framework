using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.World;

public class ArrivalAction_OfferGifts : ArrivalAction_VisitSettlement
{
  public ArrivalAction_OfferGifts()
  {
  }

  public ArrivalAction_OfferGifts(VehiclePawn vehicle) : base(vehicle)
  {
  }

  public override void Arrived(GlobalTargetInfo target)
  {
    base.Arrived(target);
    Settlement settlement = target.WorldObject as Settlement;
    Assert.IsNotNull(settlement);
    if (ArrivalAction_Trade.GetValidNegotiator(vehicle, settlement) == null)
    {
      Log.Warning($"No valid negotiator to trade with for {vehicle}.");
      return;
    }
    // TODO - refactor the way we pull negotiator
    // Valid negotiator already verified, should never not find someone to take the role
    Pawn negotiator = vehicle.FindBestNegotiator(settlement.Faction, settlement.TraderKind);
    Assert.IsNotNull(negotiator);

    Find.WindowStack.Add(new Dialog_Trade(negotiator, settlement, giftsOnly: true));
    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(settlement.Goods.OfType<Pawn>(),
      "LetterRelatedPawnsTradingWithSettlement".Translate(Faction.OfPlayer.def.pawnsPlural),
      LetterDefOf.NeutralEvent);
  }

  /// <summary>
  /// AerialVehicle <paramref name="vehicle"/> can offer gifts to <paramref name="settlement"/>
  /// </summary>
  /// <param name="vehicle"></param>
  /// <param name="settlement"></param>
  public static FloatMenuAcceptanceReport CanOfferGiftsTo(VehiclePawn vehicle,
    Settlement settlement)
  {
    return ArrivalAction_Trade.ValidGiftOrTradePartner(settlement) &&
      settlement.Faction.HostileTo(Faction.OfPlayer) &&
      ArrivalAction_Trade.GetValidNegotiator(vehicle, settlement) != null;
  }
}