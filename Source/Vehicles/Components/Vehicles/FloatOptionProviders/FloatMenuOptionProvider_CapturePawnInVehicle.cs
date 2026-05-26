using JetBrains.Annotations;
using RimWorld;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles;

[UsedWithReflection]
public class FloatMenuOptionProvider_CapturePawnInVehicle : FloatMenuOptionProvider
{
  protected override bool Drafted => true;

  protected override bool Undrafted => false;

  protected override bool Multiselect => false;

  protected override bool RequiresManipulation => true;

  protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
  {
    if (clickedPawn is not VehiclePawn vehicle)
      return null;

    Pawn actor = context.FirstSelectedPawn;
    if (actor.carryTracker.CarriedThing is not Pawn carriedPawn)
      return null;

    if (!carriedPawn.CanBeCaptured())
      return null;

    if (!HealthAIUtility.CanRescueNow(actor, carriedPawn, forced: true))
      return null;

    if (!actor.CanReach(vehicle, PathEndMode.ClosestTouch, Danger.Deadly))
    {
      return new FloatMenuOption($"{"CannotCapture".Translate()}: " +
                                 "NoPath".Translate().CapitalizeFirst(), action: null);
    }
    if (!carriedPawn.CanBoardVehicle(vehicle) && !carriedPawn.CanAddToCargo(vehicle))
    {
      return new FloatMenuOption($"{"CannotCapture".Translate()}: " +
                                 "VF_NoRoleAvailable".Translate().CapitalizeFirst(), action: null);
    }

    TaggedString label = "Capture".Translate(carriedPawn.LabelCap, carriedPawn);
    if (!carriedPawn.guest.Recruitable)
    {
      label += $" ({"Unrecruitable".Translate()})";
    }
    if (carriedPawn.Faction != null && carriedPawn.Faction != Faction.OfPlayer && !carriedPawn.Faction.Hidden
        && !carriedPawn.Faction.HostileTo(Faction.OfPlayer) && !carriedPawn.IsPrisonerOfColony)
    {
      label += $": {"AngersFaction".Translate().CapitalizeFirst()}";
    }

    FloatMenuOption option = new(label, delegate
    {
      Job job = JobMaker.MakeJob(JobDefOf_Vehicles.CapturePawnToVehicle, carriedPawn, vehicle);
      job.count = 1;
      bool taken = actor.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
      Assert.IsTrue(taken);
      PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
      if (carriedPawn.Faction != null && carriedPawn.Faction != Faction.OfPlayer && !carriedPawn.Faction.Hidden &&
          !carriedPawn.Faction.HostileTo(Faction.OfPlayer) && !carriedPawn.IsPrisonerOfColony)
      {
        Messages.Message("MessageCapturingWillAngerFaction".Translate(carriedPawn.Named("PAWN"))
          .AdjustedFor(carriedPawn), carriedPawn, MessageTypeDefOf.CautionInput, historical: false);
      }
    }, MenuOptionPriority.RescueOrCapture);
    return option;
  }
}