using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public class ArrivalAction_Trade : ArrivalAction_VisitSettlement
{
  public ArrivalAction_Trade()
  {
  }

  public ArrivalAction_Trade(VehiclePawn vehicle) : base(vehicle)
  {
  }

  public override void Arrived(GlobalTargetInfo target)
  {
    base.Arrived(target);
    Settlement settlement = target.WorldObject as Settlement;
    Assert.IsNotNull(settlement);

    if (GetValidNegotiator(vehicle, settlement) is null)
      return;

    Pawn negotiator =
      vehicle.FindBestNegotiator(settlement.Faction, settlement.TraderKind);
    if (negotiator == null)
      return;

    Find.WindowStack.Add(new Dialog_Trade(negotiator, settlement));
    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(settlement.Goods.OfType<Pawn>(),
      "LetterRelatedPawnsTradingWithSettlement".Translate(Faction.OfPlayer.def.pawnsPlural),
      LetterDefOf.NeutralEvent);
  }

  public static bool ValidGiftOrTradePartner(Settlement settlement)
  {
    return settlement != null && settlement.Spawned && !settlement.HasMap &&
      settlement.Faction != null && settlement.Faction != Faction.OfPlayer
      && !settlement.Faction.def.permanentEnemy && settlement.CanTradeNow;
  }

  public static FloatMenuAcceptanceReport CanTradeWith(VehiclePawn vehicle, Settlement settlement)
  {
    return ValidGiftOrTradePartner(settlement) && !settlement.Faction.HostileTo(Faction.OfPlayer) &&
      GetValidNegotiator(vehicle, settlement) != null;
  }

  public static Pawn GetValidNegotiator(VehiclePawn vehicle, Settlement settlement)
  {
    foreach (Pawn pawn in vehicle.AllPawnsAboard)
    {
      if (pawn.Dead || pawn.Downed || pawn.InMentalState)
        continue;
      if (StatDefOf.TradePriceImprovement.Worker.IsDisabledFor(pawn))
        continue;

      if (pawn.CanTradeWith(settlement.Faction, settlement.TraderKind))
        return pawn;
    }
    return null;
  }
}