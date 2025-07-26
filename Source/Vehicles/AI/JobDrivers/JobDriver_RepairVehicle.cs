using System.Linq;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles
{
  public class JobDriver_RepairVehicle : JobDriver_WorkVehicle
  {
    public const float TicksForRepair = 60;
    private const float VanillaSkillAmount = 0.05f;

    protected override JobDef JobDef => JobDefOf_Vehicles.RepairVehicle;

    protected override float TotalWork => TicksForRepair;

    protected override StatDef Stat => StatDefOf.ConstructionSpeed;

    protected override SkillDef Skill => SkillDefOf.Construction;

    protected override float SkillAmount => VanillaSkillAmount;

    protected override void WorkComplete(Pawn actor)
    {
      if (!Vehicle.statHandler.ComponentsPrioritized.Any(c => c.HealthPercent < 1))
      {
        MapComponentCache<ListerVehiclesRepairable>.GetComponent(Vehicle.Map).NotifyVehicleRepaired(Vehicle);
        actor.records.Increment(RecordDefOf.ThingsRepaired);
        actor.jobs.EndCurrentJob(JobCondition.Succeeded);
        return;
      }
      ResetWork();
      VehicleComponent component = Vehicle.statHandler.ComponentsPrioritized.FirstOrDefault(c => c.HealthPercent < 1);
      component.HealComponent(Vehicle.GetStatValue(VehicleStatDefOf.RepairRate));
      Vehicle.Transform.rotation = 0;
      if (!Vehicle.VehicleDef.graphicData.drawRotated)
        Vehicle.Rotation = Vehicle.VehicleDef.defaultPlacingRot;
    }
  }
}