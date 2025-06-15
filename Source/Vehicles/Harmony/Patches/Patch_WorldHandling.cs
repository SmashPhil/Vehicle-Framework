using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Sound;

namespace Vehicles
{
  internal class Patch_WorldHandling : IPatchCategory
  {
    public void PatchMethods()
    {
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(WorldPawns), nameof(WorldPawns.GetSituation)),
        prefix: null,
        postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(SituationBoardedVehicle)));
      //VehicleHarmony.Patch(
      //  original: AccessTools.Method(typeof(WorldPawns),
      //    nameof(WorldPawns.RemoveAndDiscardPawnViaGC)),
      //  prefix: new HarmonyMethod(typeof(Patch_WorldHandling),
      //    nameof(DoNotRemoveVehicleObjects)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(GameEnder), nameof(GameEnder.CheckOrUpdateGameOver)),
        postfix: new HarmonyMethod(typeof(Patch_WorldHandling), nameof(GameEnderWithVehicles)));
      VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), "AddToCache"),
        postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(AddVehicleObjectToCache)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(WorldObjectsHolder), "RemoveFromCache"),
        postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(RemoveVehicleObjectToCache)));
      VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), "Recache"),
        prefix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(RecacheVehicleObjectCache)));
      VehicleHarmony.Patch(
        original: AccessTools.PropertyGetter(typeof(PawnsFinder),
          nameof(PawnsFinder.AllCaravansAndTravellingTransporters_AliveOrDead)),
        postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(AllAerialVehicles_AliveOrDead)));
      // TODO 1.6 - Recheck, banishing from aerial vehicle should not be allowed anymore
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(PawnBanishUtility), nameof(PawnBanishUtility.Banish),
          parameters: [typeof(Pawn), typeof(PlanetTile), typeof(bool)]),
        prefix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(BanishPawnFromAerialVehicle)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(PawnUtility),
          nameof(PawnUtility.IsTravelingInTransportPodWorldObject)),
        postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(AerialVehiclesDontRandomizePrisoners)));

      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(CameraJumper), nameof(CameraJumper.TryShowWorld)),
        prefix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(ForcedTargetingDontShowWorld)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(MainButtonWorker_ToggleWorld),
          nameof(MainButtonWorker_ToggleWorld.Activate)),
        prefix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(ForcedTargetingDontToggleWorld)));

      // TODO 1.6 - REMOVE AFTER TESTING
      //VehicleHarmony.Patch(original: AccessTools.Constructor(typeof(Dialog_Trade), parameters: new Type[] { typeof(Pawn), typeof(ITrader), typeof(bool) }),
      //	postfix: new HarmonyMethod(typeof(WorldHandling),
      //	nameof(SetupPlayerAerialVehicleVariables)));
      //VehicleHarmony.Patch(original: AccessTools.Method(typeof(Dialog_Trade), nameof(Dialog_Trade.DoWindowContents)),
      //	prefix: new HarmonyMethod(typeof(WorldHandling),
      //	nameof(DrawAerialVehicleInfo)));

      /* World Targeter Event Handling */
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(WorldTargeter), nameof(WorldTargeter.TargeterUpdate)),
        postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(WorldTargeterUpdate)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(WorldTargeter), nameof(WorldTargeter.TargeterOnGUI)),
        postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(WorldTargeterOnGUI)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(WorldTargeter),
          nameof(WorldTargeter.ProcessInputEvents)),
        postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
          nameof(WorldTargeterProcessInputEvents)));
    }

    /// <summary>
    /// Prevent RimWorld Garbage Collection from snatching up VehiclePawn inhabitants and VehicleCaravan's VehiclePawn inhabitants by changing
    /// the WorldPawnSituation of pawns onboard vehicles
    /// </summary>
    /// <param name="p"></param>
    /// <param name="__result"></param>
    public static void SituationBoardedVehicle(Pawn p, ref WorldPawnSituation __result)
    {
      if (__result == WorldPawnSituation.Free && p.Faction != null &&
        p.Faction == Faction.OfPlayerSilentFail)
      {
        if (p is VehiclePawn)
        {
          __result = WorldPawnSituation.CaravanMember;
          return;
        }
        if (p.ParentHolder?.ParentHolder is VehiclePawn)
        {
          __result = WorldPawnSituation.CaravanMember;
        }
        if (p.GetAerialVehicle() != null)
        {
          __result = WorldPawnSituation.InTravelingTransportPod;
          return;
        }
      }
    }

    /// <summary>
    /// Prevent RimWorld Garbage Collection from removing pawns inside vehicles on the world map
    /// </summary>
    /// <param name="p"></param>
    public static bool DoNotRemoveVehicleObjects(Pawn p)
    {
      foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects)
      {
        if (worldObject is StashedVehicle stashedVehicle)
        {
          if (stashedVehicle.Vehicles.Contains(p))
          {
            return false;
          }
        }
        else if (worldObject is VehicleCaravan vehicleCaravan)
        {
          foreach (Pawn innerPawn in vehicleCaravan.PawnsListForReading)
          {
            if (innerPawn is VehiclePawn vehicle)
            {
              if (vehicle == p || vehicle.AllPawnsAboard.Contains(p))
              {
                return false;
              }
              foreach (Thing thing in vehicle.inventory.innerContainer)
              {
                if (thing == p)
                {
                  return false;
                }
              }
            }
          }
        }
        else if (worldObject is AerialVehicleInFlight aerialVehicle)
        {
          if (aerialVehicle.vehicle == p || aerialVehicle.vehicle.AllPawnsAboard.Contains(p))
          {
            return false;
          }
          foreach (Thing thing in aerialVehicle.vehicle.inventory.innerContainer)
          {
            if (thing == p)
            {
              return false;
            }
          }
        }
      }
      return true;
    }

    private static void GameEnderWithVehicles(GameEnder __instance, ref int ___ticksToGameOver)
    {
      if (__instance.gameEnding)
      {
        foreach (Map map in Find.Maps)
        {
          VehiclePositionManager positionManager =
            map.GetDetachedMapComponent<VehiclePositionManager>();
          Assert.IsNotNull(positionManager);
          foreach (VehiclePawn vehicle in positionManager.AllClaimants)
          {
            foreach (Pawn pawn in vehicle.AllPawnsAboard)
            {
              if (pawn.IsFreeColonist)
              {
                __instance.gameEnding = false;
                ___ticksToGameOver = -1;
                return;
              }
            }
          }
        }
        foreach (AerialVehicleInFlight aerialVehicle in Find.World
         .GetComponent<VehicleWorldObjectsHolder>().AerialVehicles)
        {
          foreach (Pawn pawn in aerialVehicle.vehicle.AllPawnsAboard)
          {
            if (pawn.IsFreeColonist)
            {
              __instance.gameEnding = false;
              ___ticksToGameOver = -1;
              return;
            }
          }
        }
      }
    }

    public static void AddVehicleObjectToCache(WorldObject o)
    {
      VehicleWorldObjectsHolder.Instance.AddToCache(o);
    }

    public static void RemoveVehicleObjectToCache(WorldObject o)
    {
      VehicleWorldObjectsHolder.Instance.RemoveFromCache(o);
    }

    public static void RecacheVehicleObjectCache()
    {
      VehicleWorldObjectsHolder.Instance.Recache();
    }

    public static void AllAerialVehicles_AliveOrDead(ref List<Pawn> __result)
    {
      if (VehicleWorldObjectsHolder.Instance == null)
        return;
      foreach (AerialVehicleInFlight aerialVehicle in VehicleWorldObjectsHolder.Instance
       .AerialVehicles)
      {
        __result.AddRange(aerialVehicle.vehicle.AllPawnsAboard);
      }
    }

    private static void BanishPawnFromAerialVehicle(Pawn pawn, ref PlanetTile tile)
    {
      if (pawn.GetAerialVehicle() is { } aerialVehicle)
      {
        CaravanInventoryUtility.MoveAllInventoryToSomeoneElse(pawn,
          aerialVehicle.vehicle.AllPawnsAboard.Append(aerialVehicle.vehicle).ToList());
        aerialVehicle.vehicle.RemovePawn(pawn);
      }
    }

    private static void HealIfPossible(Pawn p)
    {
      List<Hediff> hediffs = new List<Hediff>(p.health.hediffSet.hediffs);
      for (int i = 0; i < hediffs.Count; i++)
      {
        Hediff_Injury hediff_Injury = hediffs[i] as Hediff_Injury;
        if (hediff_Injury != null && !hediff_Injury.IsPermanent())
        {
          p.health.RemoveHediff(hediff_Injury);
        }
        else
        {
          ImmunityRecord immunityRecord = p.health.immunity.GetImmunityRecord(hediffs[i].def);
          if (immunityRecord != null)
          {
            immunityRecord.immunity = 1f;
          }
        }
      }
    }

    public static bool ForcedTargetingDontShowWorld(ref bool __result)
    {
      if (LandingTargeter.Instance.ForcedTargeting)
      {
        __result = false;
        return false;
      }
      return true;
    }

    public static bool ForcedTargetingDontToggleWorld()
    {
      if (LandingTargeter.Instance.ForcedTargeting)
      {
        SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
        Messages.Message("MustTargetLanding".Translate(), MessageTypeDefOf.RejectInput);
        return false;
      }
      return true;
    }

    public static void SetupPlayerAerialVehicleVariables(
      ref List<Thing> ___playerCaravanAllPawnsAndItems)
    {
      AerialVehicleTraderHelper.SetupAerialVehicleTrade(ref ___playerCaravanAllPawnsAndItems);
    }

    public static void DrawAerialVehicleInfo(Dialog_Trade __instance, ref Rect inRect)
    {
      Rect rect = new Rect(12f, 0f, inRect.width - 24f, 40f);
      float yUsed = AerialVehicleTraderHelper.DrawAerialVehicleInfo(__instance, rect);
      inRect.yMin += yUsed;
    }

    public static void AerialVehiclesDontRandomizePrisoners(Pawn pawn, ref bool __result)
    {
      if (ThingOwnerUtility.AnyParentIs<VehiclePawn>(pawn) ||
        ThingOwnerUtility.AnyParentIs<AerialVehicleInFlight>(pawn))
      {
        __result = true;
      }
    }

    /* -------------------- World Targeter -------------------- */

    public static void WorldTargeterUpdate()
    {
      Targeters.UpdateWorldTargeter();
    }

    public static void WorldTargeterOnGUI()
    {
      Targeters.OnGUIWorldTargeter();
    }

    public static void WorldTargeterProcessInputEvents()
    {
      Targeters.ProcessWorldTargeterInputEvent();
    }

    /* --------------------------------------------------------- */
  }
}