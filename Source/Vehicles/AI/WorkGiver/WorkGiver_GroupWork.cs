using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public abstract class WorkGiver_GroupWork : WorkGiver_Scanner
{
  protected abstract bool CanMergeWith(IVehicleTaskLord lord);

  protected abstract bool CanUseVehicle(Pawn pawn, VehiclePawn vehicle);

  protected abstract Lord GetLordToJoin(Pawn pawn, VehiclePawn vehicle);

  public sealed override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
  {
    if (thing.Faction != pawn.Faction || thing is not VehiclePawn vehicle)
      return false;

    if (!CanUseVehicle(pawn, vehicle))
      return false;

    if (vehicle.vehiclePather.Moving)
      return false;

    if (!pawn.CanReach(new LocalTargetInfo(thing.Position), PathEndMode.Touch, Danger.Deadly))
      return false;

    Lord lord = vehicle.GetLord();
    if (lord != null)
    {
      if (lord.LordJob is not IVehicleTaskLord taskLord || !CanMergeWith(taskLord))
        return false;
    }

    if (!TaskSpotFinder.TryFindMeetingSpot(vehicle, pawn, out IntVec3 meetingSpot) || !meetingSpot.IsValid)
      return false;

    return pawn.CanReserve(vehicle);
  }

  public sealed override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
  {
    VehiclePawn vehicle = (VehiclePawn)thing;
    Lord lord = GetLordToJoin(pawn, vehicle);
    if (lord != null)
    {
      IVehicleTaskLord lordJob = (IVehicleTaskLord)lord.LordJob;
      IntVec3 meetingSpot = lordJob.MeetingSpot;
      return JobMaker.MakeJob(JobDefOf_Vehicles.JoinVehicleLord, meetingSpot, vehicle);
    }
    return null;
  }
}
