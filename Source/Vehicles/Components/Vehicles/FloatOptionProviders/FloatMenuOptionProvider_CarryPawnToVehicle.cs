using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;

namespace Vehicles;

[UsedWithReflection]
public class FloatMenuOptionProvider_CarryPawnToVehicle : FloatMenuOptionProvider
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

    if (carriedPawn.Faction != Faction.OfPlayer)
      return null;

    if (!actor.CanReach(vehicle, PathEndMode.ClosestTouch, Danger.Deadly))
    {
      return new FloatMenuOption($"{"VF_CantLoadInVehicle".Translate(carriedPawn, vehicle)}: " +
                                 "NoPath".Translate().CapitalizeFirst(), action: null);
    }
    if (!carriedPawn.CanBoardVehicle(vehicle) && !carriedPawn.CanAddToCargo(vehicle))
    {
      return new FloatMenuOption($"{"VF_CantLoadInVehicle".Translate(carriedPawn, vehicle)}: " +
                                 "VF_NoRoleAvailable".Translate().CapitalizeFirst(), action: null);
    }

    var option = FloatMenuUtility.DecoratePrioritizedTask(
      new FloatMenuOption("VF_CarryToVehicle".Translate(carriedPawn, vehicle), delegate
      {
        Job job = JobMaker.MakeJob(JobDefOf_Vehicles.CarryPawnToVehicle, carriedPawn, vehicle);
        job.ignoreForbidden = true;
        job.count = 1;
        actor.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
      }, MenuOptionPriority.GoHere), actor, vehicle);
    option.targetsDespawned = !carriedPawn.Spawned;
    return option;
  }
}