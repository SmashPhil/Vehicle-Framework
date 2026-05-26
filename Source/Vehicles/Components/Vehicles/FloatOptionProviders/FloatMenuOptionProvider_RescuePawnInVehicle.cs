using JetBrains.Annotations;
using RimWorld;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles;

[UsedWithReflection]
public class FloatMenuOptionProvider_RescuePawnInVehicle : FloatMenuOptionProvider
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

    if (!HealthAIUtility.CanRescueNow(actor, carriedPawn, forced: true))
      return null;

    if (carriedPawn.Faction is {} faction && faction.HostileTo(Faction.OfPlayer))
      return null;

    if (!actor.CanReach(vehicle, PathEndMode.ClosestTouch, Danger.Deadly))
    {
      return new FloatMenuOption($"{"VF_CantLoadInVehicle".Translate(carriedPawn, vehicle)}: " +
                                 "NoPath".Translate().CapitalizeFirst(), action: null);
    }
    if (!carriedPawn.CanBoardVehicle(vehicle) && !carriedPawn.CanAddToCargo(vehicle))
    {
      return new FloatMenuOption($"{"CannotRescue".Translate()}: " +
                                 "VF_NoRoleAvailable".Translate().CapitalizeFirst(), action: null);
    }
    if (ChildcareUtility.CanSuckle(clickedPawn, out _) ||
        !(HealthAIUtility.ShouldSeekMedicalRest(clickedPawn) || !clickedPawn.ageTracker.CurLifeStage.alwaysDowned))
    {
      return null;
    }
    if (clickedPawn.playerSettings is { medCare: MedicalCareCategory.NoCare })
    {
      return new FloatMenuOption(
        $"{"CannotRescuePawn".Translate(clickedPawn.Named("PAWN"))}: {"MedicalCareDisabled".Translate()}", action: null);
    }

    TaggedString label = "Rescue".Translate(carriedPawn.LabelCap, carriedPawn);
    FloatMenuOption option = new(label, delegate
    {
      Job job = JobMaker.MakeJob(JobDefOf_Vehicles.RescuePawnToVehicle, carriedPawn, vehicle);
      job.count = 1;
      bool taken = actor.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
      Assert.IsTrue(taken);
      PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
    }, MenuOptionPriority.RescueOrCapture);
    return option;
  }
}