using RimWorld;
using Verse;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf]
public static class JobDefOf_Vehicles
{
  static JobDefOf_Vehicles()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf_Vehicles));
  }

  // General
  public static JobDef IdleVehicle;

  public static JobDef IdleVehicleDeSpawned;

  public static JobDef DeployVehicle;

  public static JobDef Board;

  public static JobDef PrepareCaravan_GatheringVehicle;

  public static JobDef RopeAnimalToVehicle;

  public static JobDef CarryPawnToVehicle;

  [DefAlias("VF_CapturePawnToVehicle")]
  public static JobDef CapturePawnToVehicle;

  [DefAlias("VF_RescuePawnToVehicle")]
  public static JobDef RescuePawnToVehicle;

  public static JobDef RepairVehicle;

  public static JobDef DisassembleVehicle;

  public static JobDef PaintVehicle;

  public static JobDef LoadVehicle;

  public static JobDef CarryItemToVehicle;

  public static JobDef LoadUpgradeMaterials;

  public static JobDef RemoveFuelFromVehicle;

  public static JobDef RefuelVehicle;

  public static JobDef RefuelVehicleAtomic;

  public static JobDef UpgradeVehicle;

  public static JobDef FollowVehicle;

  public static JobDef EscortVehicle;

  [DefAlias("VF_JoinVehicleLord")]
  public static JobDef JoinVehicleLord;

  // Raiders
  public static JobDef SabotageVehicle;
}