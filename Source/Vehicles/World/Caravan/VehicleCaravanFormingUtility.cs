using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;

namespace Vehicles.World;

[PublicAPI]
public static class VehicleCaravanFormingUtility
{
  public static void StartFormingCaravan([NotNull] List<TransferableOneWay> transferables,
    in IntVec3 meetingPoint, in IntVec3 exitSpot, in PlanetTile startingTile,
    in PlanetTile destinationTile)
  {
    // Copy into lists, transferables will be cleared from dialog soon
    List<VehiclePawn> vehicles = [];
    List<Pawn> pawns = [];
    List<TransferableOneWay> transferableThings = [];
    for (int i = transferables.Count - 1; i >= 0; i--)
    {
      TransferableOneWay transferable = transferables[i];
      if (transferable.CountToTransfer == 0)
        continue;

      switch (transferable.AnyThing)
      {
        case VehiclePawn vehicle:
          vehicles.Add(vehicle);
        break;
        case Pawn pawn:
          pawns.Add(pawn);
        break;
        default:
          transferableThings.Add(transferable);
        break;
      }
    }
    // Vehicles are pawns, so this catches both cases
    Assert.IsFalse(transferableThings.Exists(transferable =>
      transferable is { AnyThing: Pawn, CountToTransfer: > 0 }));
    StartFormingCaravan(vehicles, pawns, transferableThings, meetingPoint, exitSpot, startingTile,
      destinationTile);
  }

  public static void StartFormingCaravan(
    [NotNull] List<VehiclePawn> vehicles, [NotNull] List<Pawn> pawns,
    [NotNull] List<TransferableOneWay> transferables,
    in IntVec3 meetingPoint, in IntVec3 exitSpot,
    in PlanetTile startingTile, in PlanetTile destinationTile)
  {
    // All transferables are prefiltered for caravan
    Assert.IsFalse(transferables.Exists(transferable =>
      transferable is { AnyThing: Pawn, CountToTransfer: > 0 }));

    if (!startingTile.Valid)
    {
      Trace.Fail($"Can't start forming caravan because startingTile ({startingTile}) is invalid.");
      return;
    }
    if (vehicles.Count == 0)
    {
      Trace.Fail("Can't start forming caravan with 0 vehicles.");
      return;
    }

    foreach (VehiclePawn vehicle in vehicles)
      vehicle.GetLord()?.Notify_PawnLost(vehicle, PawnLostCondition.ForcedToJoinOtherLord);
    foreach (Pawn pawn in pawns)
      pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);

#if DEBUG || UNSTABLE
    if (vehicles.Exists(Ext_Vehicles.IsBoat))
    {
      int seats = 0;
      foreach (VehiclePawn vehicle in vehicles)
      {
        seats += vehicle.SeatsAvailable;
      }
      if (pawns.Count > seats)
      {
        Trace.Fail("Can't start forming caravan, not enough room for pawns.");
        return;
      }
    }
#endif

    LordJob_FormAndSendVehicles lordJob = new(vehicles, pawns, transferables,
      meetingPoint, exitSpot, startingTile, destinationTile);
    LordMaker.MakeNewLord(Faction.OfPlayer, lordJob, pawns[0].MapHeld, vehicles.Concat(pawns));

    // Disembark pawns, they will immediately join the vehicle's lord job and begin packing the caravan
    foreach (VehiclePawn vehicle in vehicles)
    {
      vehicle.DisembarkAll();
    }

    foreach (Pawn pawn in pawns)
    {
      pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    LookTargets lookTarget = pawns.FirstOrDefault() ?? vehicles.FirstOrDefault();
    Assert.IsNotNull(lookTarget);
    Messages.Message("CaravanFormationProcessStarted".Translate(), lookTarget,
      MessageTypeDefOf.PositiveEvent, false);
    if (ModsConfig.BiotechActive && pawns.Exists(pawn => pawn.RaceProps.IsMechanoid))
    {
      LessonAutoActivator.TeachOpportunity(ConceptDefOf.MechsInCaravans,
        OpportunityType.GoodToKnow);
    }
  }

  public static void RemovePawnFromVehicleCaravan(Pawn pawn, Lord lord, PawnLostCondition condition,
    bool removeFromDowned = true)
  {
    bool isOwner = false;
    bool forcedToJoinOtherLord = condition == PawnLostCondition.ForcedToJoinOtherLord;
    string text = "";
    string textShip = "";
    foreach (Pawn lordPawn in lord.ownedPawns)
    {
      if (lordPawn is not VehiclePawn vehicle)
        continue;

      if (vehicle.AllPawnsAboard.Contains(pawn))
      {
        textShip = "VF_PawnBoardedFormingCaravan".Translate(pawn, vehicle.LabelShort)
         .CapitalizeFirst();
        forcedToJoinOtherLord = true;
        break;
      }
    }
    if (!forcedToJoinOtherLord)
    {
      foreach (Pawn p in lord.ownedPawns)
      {
        if (p != pawn && CaravanUtility.IsOwner(p, Faction.OfPlayer))
        {
          isOwner = true;
          break;
        }
      }
    }
    if (isOwner)
    {
      text += "MessagePawnLostWhileFormingCaravan".Translate(pawn).CapitalizeFirst().ToString();
    }
    else
    {
      text += forcedToJoinOtherLord ?
        textShip :
        ("MessagePawnLostWhileFormingCaravan".Translate(pawn).CapitalizeFirst().ToString() +
          "MessagePawnLostWhileFormingCaravan_AllLost".Translate().ToString());
    }
    bool stillInLord = true;
    if (!forcedToJoinOtherLord && !isOwner)
    {
      CaravanFormingUtility.StopFormingCaravan(lord);
    }
    if (isOwner)
    {
      pawn.inventory.UnloadEverything = true;
      if (lord.ownedPawns.Contains(pawn))
      {
        lord.Notify_PawnLost(pawn, PawnLostCondition.ForcedByPlayerAction);
        stillInLord = false;
      }

      if (lord.LordJob is LordJob_FormAndSendVehicles lordJobCaravanFormation &&
        lordJobCaravanFormation.downedPawns.Contains(pawn))
      {
        if (!removeFromDowned)
        {
          stillInLord = false;
        }
        else
        {
          lordJobCaravanFormation.downedPawns.Remove(pawn);
        }
      }
    }
    if (stillInLord)
    {
      MessageTypeDef msg = forcedToJoinOtherLord ?
        MessageTypeDefOf.SilentInput :
        MessageTypeDefOf.NegativeEvent;
      Messages.Message(text, pawn, msg);
    }
  }
}