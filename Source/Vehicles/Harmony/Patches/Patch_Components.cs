using HarmonyLib;
using RimWorld;
using SmashTools.Patching;
using UnityEngine;
using Verse;

namespace Vehicles;

internal class Patch_Components : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(original: AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Drafted)),
      postfix: new HarmonyMethod(typeof(Patch_Components),
        nameof(VehicleIsDrafted)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.CanTakeOrder)),
      postfix: new HarmonyMethod(typeof(Patch_Components),
        nameof(CanVehicleTakeOrder)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(FloatMenuUtility),
        nameof(FloatMenuUtility.GetMeleeAttackAction)),
      prefix: new HarmonyMethod(typeof(Patch_Components),
        nameof(NoMeleeForVehicles)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(PawnComponentsUtility),
        nameof(PawnComponentsUtility.CreateInitialComponents)),
      prefix: new HarmonyMethod(typeof(Patch_Components),
        nameof(CreateInitialVehicleComponents)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(PawnComponentsUtility),
        nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents)),
      prefix: new HarmonyMethod(typeof(Patch_Components),
        nameof(AddAndRemoveVehicleComponents)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Pawn_MeleeVerbs), "ChooseMeleeVerb"),
      prefix: new HarmonyMethod(typeof(Patch_Components),
        nameof(VehiclesDontMeleeThings)));
  }

  /// <summary>
  /// Divert draft status check to <see cref="VehicleIgnitionController"/>
  /// </summary>
  public static void VehicleIsDrafted(Pawn __instance, ref bool __result)
  {
    if (__instance is VehiclePawn vehicle)
    {
      // May trigger prematurely from PrepareCarefully
      __result = vehicle.ignition?.Drafted ?? false;
    }
  }

  /// <summary>
  /// Allow vehicles to take orders despite them not being categorized as humanlike
  /// </summary>
  public static void CanVehicleTakeOrder(Pawn __instance, ref bool __result)
  {
    if (__result is false)
    {
      __result = __instance is VehiclePawn;
    }
  }

  /// <summary>
  /// Disable melee attacks for vehicles, which don't work anyways due to not having Manipulation capacity and only cause errors
  /// </summary>
  public static bool NoMeleeForVehicles(Pawn pawn, LocalTargetInfo target, out string failStr)
  {
    if (pawn is VehiclePawn)
    {
      failStr = "VF_IsIncapableOfRamming".Translate(target.Thing.LabelShort);
      //Add more to string or Action if ramming is implemented
      return false;
    }
    failStr = string.Empty;
    return true;
  }

  /// <summary>
  /// Initialize vehicle specific components
  /// </summary>
  /// <param name="pawn"></param>
  public static void CreateInitialVehicleComponents(Pawn pawn)
  {
    // TODO - check Initialized instead ?
    if (pawn is VehiclePawn { vehiclePather: null } vehicle)
    {
      vehicle.vehiclePather = new VehiclePathFollower(vehicle);
      vehicle.vehicleAI = new VehicleAI(vehicle);
      vehicle.statHandler = new VehicleStatHandler(vehicle);
      vehicle.sharedJob = new SharedJob();

      if (!VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicle.VehicleDef.defName,
        out PatternData defaultPatternData))
      {
        defaultPatternData = vehicle.VehicleDef.graphicData != null ?
          new PatternData(vehicle.VehicleDef.graphicData) :
          new PatternData(Color.white, Color.white, Color.white,
            PatternDefOf.Default, Vector2.zero, 0);
      }
      vehicle.patternData = new PatternData(defaultPatternData);
      if (vehicle.Stuff != null)
      {
        vehicle.DrawColor = vehicle.VehicleDef.GetColorForStuff(vehicle.Stuff);
      }
    }
  }

  /// <summary>
  /// Ensure that vehicles are given the right components when terminating from the main method
  /// </summary>
  /// <param name="pawn"></param>
  /// <param name="actAsIfSpawned"></param>
  public static void AddAndRemoveVehicleComponents(Pawn pawn, bool actAsIfSpawned = false)
  {
    if (pawn is VehiclePawn vehicle && (vehicle.Spawned || actAsIfSpawned) &&
      vehicle.ignition is null)
    {
      vehicle.ignition = new VehicleIgnitionController(vehicle);
      vehicle.trader = null; // new Pawn_TraderTracker(vehicle);
      vehicle.story = new Pawn_StoryTracker(vehicle);
      vehicle.playerSettings = new Pawn_PlayerSettings(vehicle);
      vehicle.training = null;
    }
  }

  /// <summary>
  /// Ensure that vehicles do not perform melee jobs
  /// </summary>
  public static bool VehiclesDontMeleeThings(Pawn ___pawn)
  {
    // Explicit bool as this is a destructive prefix. This isn't returning that pawn isn't a vehicle,
    // it's skipping the original method if the pawn is a vehicle.
    if (___pawn is VehiclePawn)
      return false;
    return true;
  }
}