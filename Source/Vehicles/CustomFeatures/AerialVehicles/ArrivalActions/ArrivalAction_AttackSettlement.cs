using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

public class ArrivalAction_AttackSettlement : ArrivalAction_LoadMap
{
  /// <summary>
  /// Required for Xml deserialization
  /// </summary>
  public ArrivalAction_AttackSettlement()
  {
  }

  public ArrivalAction_AttackSettlement(VehiclePawn vehicle, AerialVehicleArrivalModeDef arrivalModeDef)
    : base(vehicle, arrivalModeDef)
  {
  }

  public override bool DestroyOnArrival => true;

  protected override void MapLoaded(Map map, bool generatedMap)
  {
    base.MapLoaded(map, generatedMap);
    TaggedString letterLabel = "LetterLabelCaravanEnteredEnemyBase".Translate();
    TaggedString letterText = "LetterTransportPodsLandedInEnemyBase".Translate(map.Parent.Label)
     .CapitalizeFirst();
    SettlementUtility.AffectRelationsOnAttacked(map.Parent, ref letterText);
    if (generatedMap)
    {
      Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
      PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(map.mapPawns.AllPawns, ref letterLabel,
        ref letterText,
        "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def.pawnsPlural),
        true);
    }
    Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NeutralEvent, vehicle,
      map.Parent.Faction);
  }

  public static FloatMenuAcceptanceReport CanAttack(VehiclePawn vehicle, Settlement settlement)
  {
    if (settlement is null || !settlement.Spawned || !settlement.Attackable)
      return false;

    if (!WorldVehiclePathGrid.Instance.Passable(settlement.Tile, vehicle.VehicleDef))
    {
      return FloatMenuAcceptanceReport.WithFailReason("Impassable".Translate());
    }
    if (settlement.EnterCooldownBlocksEntering())
    {
      return FloatMenuAcceptanceReport.WithFailReasonAndMessage(
        "EnterCooldownBlocksEntering".Translate(),
        "MessageEnterCooldownBlocksEntering".Translate(settlement.EnterCooldownTicksLeft()
         .ToStringTicksToPeriod()));
    }
    return true;
  }
}