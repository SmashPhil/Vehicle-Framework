using Verse;
using Verse.AI;

namespace Vehicles;

public class JobGiver_AwaitOrdersDeSpawned : ThinkNode_JobGiver
{
  protected override Job TryGiveJob(Pawn pawn)
  {
    return JobMaker.MakeJob(JobDefOf_Vehicles.IdleVehicleDeSpawned);
  }
}