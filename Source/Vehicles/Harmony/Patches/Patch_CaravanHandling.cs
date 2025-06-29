using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Patching;
using UnityEngine;
using Vehicles.World;
using Verse;
using Verse.AI.Group;
using OpCodes = System.Reflection.Emit.OpCodes;


namespace Vehicles;

internal class Patch_CaravanHandling : IPatchCategory
{
  private static readonly List<Thing> TmpAerialVehicleThingsWillToBuy = [];

  PatchSequence IPatchCategory.PatchAt => PatchSequence.Mod;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(CaravanVisibilityCalculator),
        nameof(CaravanVisibilityCalculator.Visibility),
        parameters: [typeof(List<Pawn>), typeof(bool), typeof(StringBuilder)]),
      transpiler: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(VehicleVisibilityInCaravanTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.Capacity)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(CapacityOfVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.CanEverCarryAnything)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(CanCarryIfVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CollectionsMassCalculator),
        nameof(CollectionsMassCalculator.Capacity),
        parameters: [typeof(List<ThingCount>), typeof(StringBuilder)]),
      transpiler: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(PawnCapacityInVehicleTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CollectionsMassCalculator),
        nameof(CollectionsMassCalculator.MassUsage),
        parameters:
        [
          typeof(List<ThingCount>), typeof(IgnorePawnsInventoryMode), typeof(bool), typeof(bool)
        ]),
      transpiler: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(IgnorePawnGearAndInventoryMassTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(InventoryCalculatorsUtility),
        nameof(InventoryCalculatorsUtility.ShouldIgnoreInventoryOf)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(ShouldIgnoreInventoryPawnInVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.CanEverCarryAnything)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(CanCarryIfVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "FillTab"),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(FillTabVehicleCaravan)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimals"),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(DoPeopleAnimalsAndVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Alert_CaravanIdle), "IdleCaravans"),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(IdleVehicleCaravans)));

    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanArrivalAction_VisitSite), "DoEnter"),
      prefix: null, postfix: null,
      transpiler: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(DoEnterWithShipsTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanEnterMapUtility),
        nameof(CaravanEnterMapUtility.Enter),
        [
          typeof(Caravan), typeof(Map), typeof(CaravanEnterMode),
          typeof(CaravanDropInventoryMode), typeof(bool), typeof(Predicate<IntVec3>)
        ]),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(EnterMapVehiclesCatchAll1)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanEnterMapUtility),
        nameof(CaravanEnterMapUtility.Enter), [
          typeof(Caravan), typeof(Map), typeof(Func<Pawn, IntVec3>),
          typeof(CaravanDropInventoryMode), typeof(bool)
        ]),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(EnterMapVehiclesCatchAll2)));

    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.AllOwnersDowned)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(AllOwnersDownedVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Caravan),
        nameof(Caravan.AllOwnersHaveMentalBreak)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(AllOwnersMentalBreakVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.NightResting)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(NoRestForVehicles)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.PawnsListForReading)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(AllPawnsAndVehiclePassengers)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.TicksPerMove)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(VehicleCaravanTicksPerMove)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Caravan),
        nameof(Caravan.TicksPerMoveExplanation)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(VehicleCaravanTicksPerMoveExplanation)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(ForagedFoodPerDayCalculator),
        nameof(ForagedFoodPerDayCalculator.GetBaseForagedNutritionPerDay)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(GetBaseForagedNutritionPerDayInVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Caravan), nameof(Caravan.ContainsPawn)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(ContainsPawnInVehicle)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.AddPawn)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(AddPawnInVehicleCaravan)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Caravan), nameof(Caravan.RemovePawn)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(RemovePawnInVehicleCaravan)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Caravan), nameof(Caravan.RemoveAllPawns)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(ClearAllPawnsInVehicleCaravan)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Caravan), nameof(Caravan.IsOwner)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(IsOwnerOfVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Caravan_PathFollower),
        nameof(Caravan_PathFollower.Moving)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(VehicleCaravanMoving)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanTweenerUtility),
        nameof(CaravanTweenerUtility.PatherTweenedPosRoot)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(VehicleCaravanTweenedPosRoot)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Caravan_PathFollower),
        nameof(Caravan_PathFollower.MovingNow)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(VehicleCaravanMovingNow)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Caravan_Tweener),
        nameof(Caravan_Tweener.TweenerTickInterval)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(VehicleCaravanTweenerTick)));

    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(SettlementDefeatUtility),
        nameof(SettlementDefeatUtility.CheckDefeated)),
      transpiler: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(CheckDefeatedWithVehiclesTranspiler)));

    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Tale_DoublePawn), nameof(Tale_DoublePawn.Concerns)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(ConcernNullThing)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Settlement_TraderTracker),
        nameof(Settlement_TraderTracker.ColonyThingsWillingToBuy)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(AerialVehicleInventoryItems)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(Tradeable), nameof(Tradeable.Interactive)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(AerialVehicleSlaveTradeRoomCheck)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Dialog_Trade), "CountToTransferChanged"),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(AerialVehicleCountPawnsToTransfer)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanInventoryUtility),
        nameof(CaravanInventoryUtility.FindPawnToMoveInventoryTo)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(FindVehicleToMoveInventoryTo)));
    HarmonyPatcher.Patch(
      original: AccessTools.Property(typeof(WITab_Caravan_Health), "Pawns")
       .GetGetMethod(nonPublic: true),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(VehicleHealthTabPawns)));
    HarmonyPatcher.Patch(
      original: AccessTools.Property(typeof(WITab_Caravan_Social), "Pawns")
       .GetGetMethod(nonPublic: true),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(VehicleSocialTabPawns)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanNeedsTabUtility),
        nameof(CaravanNeedsTabUtility.DoRows)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(NoVehiclesNeedNeeds)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanNeedsTabUtility),
        nameof(CaravanNeedsTabUtility.GetSize)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(NoVehiclesNeedNeeds)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(BestCaravanPawnUtility),
        nameof(BestCaravanPawnUtility.FindBestNegotiator)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(FindBestNegotiatorInVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Settlement_TraderTracker),
        nameof(Settlement_TraderTracker.GiveSoldThingToPlayer)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(GiveSoldThingToAerialVehicle)),
      transpiler: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(GiveSoldThingToVehicleTranspiler)));

    // TODO 1.6 - recheck if this is needed
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.GetCaravan)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(GetParentCaravan)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.RandomOwner)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(RandomVehicleOwner)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanMergeUtility), "MergeCaravans"),
      transpiler: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(MergeWithVehicleCaravanTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanMergeUtility),
        nameof(CaravanMergeUtility.MergeCommand)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(DisableMergeForAerialVehicles)));

    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanArrivalAction_Trade),
        nameof(CaravanArrivalAction_Trade.CanTradeWith)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanHandling),
        nameof(NoTradingUndocked)));
  }

  private static IEnumerable<CodeInstruction> VehicleVisibilityInCaravanTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    MethodInfo bodySizeProperty = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.BodySize));
    List<CodeInstruction> instructionList = instructions.ToList();
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(bodySizeProperty))
      {
        // List`1<Pawn>::get_Item
        instruction = instructionList[++i];
        // Call vehicle helper instead
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_CaravanHandling),
            nameof(VehicleVisibilityWeight)));
      }
      yield return instruction;
    }
  }

  private static float VehicleVisibilityWeight(Pawn pawn)
  {
    const float MaxVisibilityWeight = 12 / CaravanVisibilityCalculator.NotMovingFactor;

    if (pawn is VehiclePawn vehicle)
      return Mathf.Clamp(vehicle.VehicleDef.properties.visibilityWeight, 0, MaxVisibilityWeight);
    // Pawns in vehicles don't contribute additional visibility factors
    return pawn.InVehicle() || CaravanHelper.assignedSeats.IsAssigned(pawn) ? 0 : pawn.BodySize;
  }

  /// <summary>
  /// Carry capacity with Vehicles when using MassCalculator
  /// </summary>
  private static bool CapacityOfVehicle(Pawn p, ref float __result,
    StringBuilder explanation = null)
  {
    if (p is VehiclePawn vehicle)
    {
      __result = vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
      if (explanation != null)
      {
        if (explanation.Length > 0)
          explanation.AppendLine();
        explanation.Append($"  - {vehicle.LabelShortCap}: {__result.ToStringMassOffset()}");
      }
      return false;
    }
    return true;
  }

  /// <summary>
  /// Allow vehicles to carry items without being a PackAnimal or ToolUser
  /// </summary>
  private static bool CanCarryIfVehicle(Pawn p, out bool __result)
  {
    // If vehicle, set true and skip, otherwise just call to original
    // so vanilla can evaluate for non-vehicle pawns.
    __result = p is VehiclePawn;
    return !__result;
  }

  private static IEnumerable<CodeInstruction> PawnCapacityInVehicleTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();
    MethodInfo capacityMethod =
      AccessTools.Method(typeof(MassUtility), nameof(MassUtility.Capacity));
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(capacityMethod))
      {
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_CaravanHandling),
            nameof(PawnCapacityInVehicle)));
        instruction = instructionList[++i]; //CALL : MassUtility.Capacity
      }

      yield return instruction;
    }
  }

  private static IEnumerable<CodeInstruction> IgnorePawnGearAndInventoryMassTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();
    MethodInfo capacityMethod =
      AccessTools.Method(typeof(MassUtility), nameof(MassUtility.GearAndInventoryMass));

    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(capacityMethod))
      {
        yield return instruction; //CALL : MassUtility.GearAndInventoryMass
        instruction = instructionList[++i];
        yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 4);
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_CaravanHandling),
            nameof(PawnMassUsageInVehicle)));
      }

      yield return instruction;
    }
  }

  private static float PawnMassUsageInVehicle(float massUsage, Pawn pawn)
  {
    if (pawn.InVehicle() || CaravanHelper.assignedSeats.IsAssigned(pawn))
      return 0;
    return massUsage;
  }

  private static void ShouldIgnoreInventoryPawnInVehicle(ref bool __result, Pawn pawn)
  {
    if (__result)
    {
      // Already ignored from gear and inventory calculation, shouldn't subtract again for negative mass usage.
      __result = !pawn.InVehicle() && !CaravanHelper.assignedSeats.IsAssigned(pawn);
    }
  }

  private static float PawnCapacityInVehicle(Pawn pawn, StringBuilder explanation)
  {
    if (pawn.InVehicle() || CaravanHelper.assignedSeats.IsAssigned(pawn))
    {
      return 0; //pawns in vehicles or assigned to vehicle don't contribute to capacity
    }
    return MassUtility.Capacity(pawn, explanation);
  }

  private static bool FillTabVehicleCaravan(ITab_Pawn_FormingCaravan __instance,
    ref List<Thing> ___thingsToSelect, Vector2 ___size,
    ref float ___lastDrawnHeight, ref Vector2 ___scrollPosition,
    ref List<Thing> ___tmpSingleThing)
  {
    if ((Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob is
      LordJob_FormAndSendVehicles)
    {
      ___thingsToSelect.Clear();
      Rect outRect = new Rect(default, ___size).ContractedBy(10f);
      outRect.yMin += 20f;
      Rect rect = new(0f, 0f, outRect.width - 16f,
        Mathf.Max(___lastDrawnHeight, outRect.height));
      Widgets.BeginScrollView(outRect, ref ___scrollPosition, rect);
      float num = 0f;
      string status =
        ((LordJob_FormAndSendVehicles)(Find.Selector.SingleSelectedThing as Pawn).GetLord()
         .LordJob).Status;
      Widgets.Label(new Rect(0f, num, rect.width, 100f), status);
      num += 22f;
      num += 4f;
      object[] method1Args = [rect, num];
      MethodInfo doPeopleAndAnimals =
        AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimals");
      doPeopleAndAnimals.Invoke(__instance, method1Args);
      num = (float)method1Args[1];
      num += 4f;
      CaravanHelper.DoItemsListForVehicle(rect, ref num, ref ___tmpSingleThing, __instance);
      ___lastDrawnHeight = num;
      Widgets.EndScrollView();
      if (___thingsToSelect.Any())
      {
        ITab_Pawn_FormingCaravan.SelectNow(___thingsToSelect);
        ___thingsToSelect.Clear();
      }
      return false;
    }
    return true;
  }

  public static bool DoPeopleAnimalsAndVehicle(Rect inRect, ref float curY,
    ITab_Pawn_FormingCaravan __instance, ref List<Thing> ___tmpPawns)
  {
    if ((Find.Selector.SingleSelectedThing as Pawn).GetLord().LordJob is
      LordJob_FormAndSendVehicles)
    {
      Widgets.ListSeparator(ref curY, inRect.width, "CaravanMembers".Translate());
      int num = 0;
      int num2 = 0;
      int num3 = 0;
      int num4 = 0;
      int num5 = 0;
      int num6 = 0;
      int num7 = 0;
      int numShip = 0;
      Lord lord = (Find.Selector.SingleSelectedThing as Pawn).GetLord();
      foreach (Pawn pawn in lord.ownedPawns)
      {
        if (pawn.IsFreeColonist)
        {
          num++;
          if (pawn.InMentalState)
          {
            num2++;
          }
        }
        if (pawn is VehiclePawn vehicle)
        {
          if (vehicle.AllPawnsAboard.NotNullAndAny())
          {
            num += vehicle.AllPawnsAboard.FindAll(x => x.IsFreeColonist).Count;
            num2 += vehicle.AllPawnsAboard.FindAll(x => x.IsFreeColonist && x.InMentalState)
             .Count;
            num3 += vehicle.AllPawnsAboard.FindAll(x => x.IsPrisoner).Count;
            num4 += vehicle.AllPawnsAboard.FindAll(x => x.IsPrisoner && x.InMentalState).Count;
            num5 += vehicle.AllPawnsAboard.FindAll(x => x.RaceProps.Animal).Count;
            num6 += vehicle.AllPawnsAboard.FindAll(x => x.RaceProps.Animal && x.InMentalState)
             .Count;
            num7 += vehicle.AllPawnsAboard
             .FindAll(x => x.RaceProps.Animal && x.RaceProps.packAnimal).Count;
          }
          if (!vehicle.beached)
          {
            numShip++;
          }
        }
        else if (pawn.IsPrisoner)
        {
          num3++;
          if (pawn.InMentalState)
          {
            num4++;
          }
        }
        else if (pawn.RaceProps.Animal)
        {
          num5++;
          if (pawn.InMentalState)
          {
            num6++;
          }
          if (pawn.RaceProps.packAnimal)
          {
            num7++;
          }
        }
      }
      MethodInfo getPawnsCountLabel =
        AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "GetPawnsCountLabel");
      string pawnsCountLabel =
        (string)getPawnsCountLabel.Invoke(__instance, [num, num2, -1]);
      string pawnsCountLabel2 =
        (string)getPawnsCountLabel.Invoke(__instance, [num3, num4, -1]);
      string pawnsCountLabel3 =
        (string)getPawnsCountLabel.Invoke(__instance, [num5, num6, num7]);
      string pawnsCountLabelShip =
        (string)getPawnsCountLabel.Invoke(__instance, [numShip, -1, -1]);

      MethodInfo doPeopleAndAnimalsEntry =
        AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "DoPeopleAndAnimalsEntry");

      float y = curY;
      object[] m1Args =
      [
        inRect, Faction.OfPlayer.def.pawnsPlural.CapitalizeFirst(), pawnsCountLabel, curY, null
      ];
      doPeopleAndAnimalsEntry.Invoke(__instance, m1Args);
      curY = (float)m1Args[3];
      float num8 = (float)m1Args[4];

      float yShip = curY;
      object[] mSargs =
        [inRect, "VF_Vehicles".Translate().ToStringSafe(), pawnsCountLabelShip, curY, null];
      doPeopleAndAnimalsEntry.Invoke(__instance, mSargs);
      curY = (float)mSargs[3];
      float numS = (float)mSargs[4];

      float y2 = curY;
      object[] m2Args =
        [inRect, "CaravanPrisoners".Translate().ToStringSafe(), pawnsCountLabel2, curY, null];
      doPeopleAndAnimalsEntry.Invoke(__instance, m2Args);
      curY = (float)m2Args[3];
      float num9 = (float)m2Args[4];

      float y3 = curY;
      object[] m3Args =
        [inRect, "CaravanAnimals".Translate().ToStringSafe(), pawnsCountLabel3, curY, null];
      doPeopleAndAnimalsEntry.Invoke(__instance, m3Args);
      curY = (float)m3Args[3];
      float num10 = (float)m3Args[4];

      float width = Mathf.Max(num8, numS, num9, num10) + 2f;

      Rect rect = new(0f, y, width, 22f);
      if (Mouse.IsOver(rect))
      {
        Widgets.DrawHighlight(rect);
        AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "HighlightColonists")
         .Invoke(__instance, null);
      }
      if (Widgets.ButtonInvisible(rect, false))
      {
        AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "SelectColonistsLater")
         .Invoke(__instance, null);
      }

      Rect rectS = new(0f, yShip, width, 22f);
      if (Mouse.IsOver(rectS))
      {
        Widgets.DrawHighlight(rectS);
        foreach (Pawn p in lord.ownedPawns)
        {
          if (p is VehiclePawn)
          {
            TargetHighlighter.Highlight(p);
          }
        }
      }
      if (Widgets.ButtonInvisible(rectS, false))
      {
        ___tmpPawns.Clear();
        foreach (Pawn p in lord.ownedPawns)
        {
          if (p is VehiclePawn)
          {
            ___tmpPawns.Add(p);
          }
        }
        AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "SelectLater")
         .Invoke(__instance, [___tmpPawns]);
        ___tmpPawns.Clear();
      }

      Rect rect2 = new(0f, y2, width, 22f);
      if (Mouse.IsOver(rect2))
      {
        Widgets.DrawHighlight(rect2);
        AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "HighlightPrisoners")
         .Invoke(__instance, null);
      }
      if (Widgets.ButtonInvisible(rect2, false))
      {
        AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "SelectPrisonersLater")
         .Invoke(__instance, null);
      }

      Rect rect3 = new(0f, y3, width, 22f);
      if (Mouse.IsOver(rect3))
      {
        Widgets.DrawHighlight(rect3);
        AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "HighlightAnimals")
         .Invoke(__instance, null);
      }
      if (Widgets.ButtonInvisible(rect3, false))
      {
        AccessTools.Method(typeof(ITab_Pawn_FormingCaravan), "SelectAnimalsLater")
         .Invoke(__instance, null);
      }
      return false;
    }
    return true;
  }

  public static void IdleVehicleCaravans(ref List<Caravan> __result)
  {
    if (!__result.NullOrEmpty())
    {
      __result.RemoveAll(c =>
        c is VehicleCaravan vehicleCaravan && vehicleCaravan.vehiclePather.MovingNow);
    }
  }

  public static IEnumerable<CodeInstruction> DoEnterWithShipsTranspiler(
    IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
  {
    List<CodeInstruction> instructionList = instructions.ToList();

    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(AccessTools.Method(typeof(CaravanEnterMapUtility),
        nameof(CaravanEnterMapUtility.Enter),
        [
          typeof(Caravan), typeof(Map),
          typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode), typeof(bool),
          typeof(Predicate<IntVec3>)
        ])))
      {
        Label label = ilg.DefineLabel();
        Label brlabel = ilg.DefineLabel();

        yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Ext_Caravan), nameof(Ext_Caravan.HasVehicle),
            [typeof(Caravan)]));
        yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(EnterMapUtilityVehicles),
            nameof(EnterMapUtilityVehicles.EnterAndSpawn)));
        yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

        instruction.labels.Add(label);
        yield return instruction; //CALL : CaravanEnterMapUtility::Enter
        instruction = instructionList[++i];

        instruction.labels.Add(brlabel);
      }
      yield return instruction;
    }
  }

  public static bool EnterMapVehiclesCatchAll1(Caravan caravan, Map map,
    CaravanEnterMode enterMode,
    CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop,
    bool draftColonists = false, Predicate<IntVec3> extraCellValidator = null)
  {
    if (caravan is VehicleCaravan vehicleCaravan)
    {
      EnterMapUtilityVehicles.EnterAndSpawn(vehicleCaravan, map, enterMode, dropInventoryMode,
        draftColonists, extraCellValidator);
      return false;
    }
    return true;
  }

  public static bool EnterMapVehiclesCatchAll2(Caravan caravan, Map map,
    CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop,
    bool draftColonists = false)
  {
    if (caravan is VehicleCaravan vehicleCaravan)
    {
      EnterMapUtilityVehicles.EnterAndSpawn(vehicleCaravan, map, CaravanEnterMode.Edge,
        dropInventoryMode, draftColonists);
      return false;
    }
    return true;
  }

  public static bool AllOwnersDownedVehicle(Caravan __instance, ref bool __result)
  {
    if (__instance is VehicleCaravan caravan)
    {
      foreach (Pawn pawn in caravan.pawns)
      {
        if (caravan.IsOwner(pawn) && !pawn.Downed)
        {
          __result = false;
          return false;
        }
        if (pawn is VehiclePawn vehicle)
        {
          foreach (Pawn innerPawn in vehicle.AllPawnsAboard)
          {
            if (__instance.IsOwner(innerPawn) && !innerPawn.Downed)
            {
              __result = false;
              return false;
            }
          }
        }
      }
      __result = true;
      return false;
    }
    return true;
  }

  public static bool AllOwnersMentalBreakVehicle(Caravan __instance, ref bool __result)
  {
    if (__instance is VehicleCaravan caravan)
    {
      foreach (Pawn pawn in caravan.pawns)
      {
        if (caravan.IsOwner(pawn) && !pawn.InMentalState)
        {
          __result = false;
          return false;
        }
        if (pawn is VehiclePawn vehicle)
        {
          foreach (Pawn innerPawn in vehicle.AllPawnsAboard)
          {
            if (__instance.IsOwner(innerPawn) && !innerPawn.InMentalState)
            {
              __result = false;
              return false;
            }
          }
        }
      }
      __result = true;
      return false;
    }
    return true;
  }

  public static bool NoRestForVehicles(Caravan __instance, ref bool __result)
  {
    if (__instance is VehicleCaravan caravan)
    {
      __result = VehicleCaravanPathingHelper.ShouldRestAt(caravan, caravan.Tile);
      return false;
    }
    return true;
  }

  public static bool AllPawnsAndVehiclePassengers(ref List<Pawn> __result, Caravan __instance)
  {
    if (__instance is VehicleCaravan vehicleCaravan)
    {
      __result = vehicleCaravan.AllPawnsAndVehiclePassengers;
      return false;
    }
    return true;
  }

  public static bool VehicleCaravanTicksPerMove(ref int __result, Caravan __instance)
  {
    if (__instance is VehicleCaravan vehicleCaravan)
    {
      __result = vehicleCaravan.TicksPerMove;
      return false;
    }
    return true;
  }

  public static bool VehicleCaravanTicksPerMoveExplanation(ref string __result,
    Caravan __instance)
  {
    if (__instance is VehicleCaravan vehicleCaravan)
    {
      __result = vehicleCaravan.TicksPerMoveExplanation;
      return false;
    }
    return true;
  }

  public static bool GetBaseForagedNutritionPerDayInVehicle(Pawn p, out bool skip,
    ref float __result)
  {
    skip = false;
    if (p.InVehicle() || CaravanHelper.assignedSeats.IsAssigned(p))
    {
      skip = true;
      __result = 0;
      return false;
    }
    return true;
  }

  //REDO - Need better transpiler to retrieve all map pawns
  public static IEnumerable<CodeInstruction> CheckDefeatedWithVehiclesTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();

    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(AccessTools.Property(typeof(MapPawns), nameof(MapPawns.FreeColonists))
       .GetGetMethod()))
      {
        yield return new CodeInstruction(opcode: OpCodes.Callvirt,
          operand: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.AllPawnsSpawned))
           .GetGetMethod());
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(CaravanHelper),
            nameof(CaravanHelper.GrabPawnsFromMapPawnsInVehicle)));
        instruction = instructionList[++i];
      }
      yield return instruction;
    }
  }

  //REDO
  public static bool ConcernNullThing(Thing th, Tale_DoublePawn __instance, ref bool __result)
  {
    if (th is null || __instance is null || __instance.secondPawnData is null ||
      __instance.firstPawnData is null)
    {
      __result = false;
      return false;
    }
    return true;
  }

  public static bool AerialVehicleInventoryItems(Pawn playerNegotiator,
    ref IEnumerable<Thing> __result)
  {
    AerialVehicleInFlight aerialVehicle = playerNegotiator.GetAerialVehicle();
    if (aerialVehicle != null)
    {
      TmpAerialVehicleThingsWillToBuy.Clear();
      foreach (Thing thing in aerialVehicle.vehicle.inventory.innerContainer)
      {
        TmpAerialVehicleThingsWillToBuy.Add(thing);
      }
      List<Pawn> pawns = aerialVehicle.vehicle.AllPawnsAboard;
      foreach (Pawn pawn in pawns)
      {
        if (!CaravanUtility.IsOwner(pawn, aerialVehicle.Faction))
        {
          TmpAerialVehicleThingsWillToBuy.Add(pawn);
        }
      }
      __result = TmpAerialVehicleThingsWillToBuy;
      return false;
    }
    return true;
  }

  public static void AerialVehicleSlaveTradeRoomCheck(ref bool __result, Tradeable __instance)
  {
    if (__instance.AnyThing is Pawn pawn && pawn.RaceProps.Humanlike &&
      __instance.CountToTransfer == 0)
    {
      Pawn negotiator = TradeSession.playerNegotiator;
      AerialVehicleInFlight aerialVehicle = negotiator.GetAerialVehicle();
      if (aerialVehicle is not null)
      {
        __result &= CaravanHelper.CanFitInVehicle(aerialVehicle);
      }
    }
  }

  public static void AerialVehicleCountPawnsToTransfer(List<Tradeable> ___cachedTradeables)
  {
    CaravanHelper.CountPawnsBeingTraded(___cachedTradeables);
  }

  public static bool FindVehicleToMoveInventoryTo(ref Pawn __result, List<Pawn> candidates,
    List<Pawn> ignoreCandidates, Pawn currentItemOwner = null)
  {
    if (candidates.HasVehicle())
    {
      if (candidates.Where(pawn => pawn is VehiclePawn &&
          (ignoreCandidates == null || !ignoreCandidates.Contains(pawn))
          && currentItemOwner != pawn && !MassUtility.IsOverEncumbered(pawn))
       .TryRandomElement(out __result))
      {
        return false;
      }
    }
    return true;
  }

  public static bool VehicleHealthTabPawns(ref List<Pawn> __result)
  {
    if (Find.WorldSelector.SingleSelectedObject is Caravan caravan && caravan.HasVehicle())
    {
      List<Pawn> pawns = [];
      foreach (Pawn p in caravan.PawnsListForReading)
      {
        if (p is not VehiclePawn)
        {
          pawns.Add(p);
        }
      }
      __result = pawns;
      return false;
    }
    return true;
  }

  public static bool VehicleSocialTabPawns(ref List<Pawn> __result)
  {
    if (Find.WorldSelector.SingleSelectedObject is VehicleCaravan caravan && caravan.HasVehicle())
    {
      List<Pawn> pawns = [];
      foreach (Pawn p in caravan.PawnsListForReading)
      {
        if (p is not VehiclePawn)
        {
          pawns.Add(p);
        }
      }
      __result = pawns;
      return false;
    }
    return true;
  }

  public static void NoVehiclesNeedNeeds(ref List<Pawn> pawns)
  {
    pawns.RemoveAll(pawn => pawn is VehiclePawn);
  }

  public static bool FindBestNegotiatorInVehicle(Caravan caravan, ref Pawn __result,
    Faction negotiatingWith = null, TraderKindDef trader = null)
  {
    if (caravan is VehicleCaravan vehicleCaravan)
    {
      __result =
        WorldHelper.FindBestNegotiator(vehicleCaravan, faction: negotiatingWith, trader: trader);
      return false;
    }
    return true;
  }

  public static void ContainsPawnInVehicle(Pawn p, Caravan __instance, ref bool __result)
  {
    if (!__result)
    {
      __result = __instance.PawnsListForReading.Any(v =>
        v is VehiclePawn vehicle && vehicle.AllPawnsAboard.Contains(p));
    }
  }

  private static void AddPawnInVehicleCaravan(Caravan __instance)
  {
    if (__instance is VehicleCaravan vehicleCaravan)
      vehicleCaravan.RecacheVehiclesOrConvertCaravan();
  }

  private static void RemovePawnInVehicleCaravan(Caravan __instance)
  {
    if (__instance is VehicleCaravan vehicleCaravan)
      vehicleCaravan.RecacheVehiclesOrConvertCaravan();
  }

  private static void ClearAllPawnsInVehicleCaravan(Caravan __instance)
  {
    if (__instance is VehicleCaravan vehicleCaravan)
      vehicleCaravan.RecacheVehiclesOrConvertCaravan();
  }

  public static void IsOwnerOfVehicle(Pawn p, Caravan __instance, ref bool __result)
  {
    if (!__result)
    {
      VehiclePawn vehicle = p.GetVehicle();
      __result = vehicle is not null && __instance.pawns.Contains(vehicle) &&
        CaravanUtility.IsOwner(p, __instance.Faction);
    }
  }

  public static void VehicleCaravanMoving(ref bool __result, Caravan ___caravan)
  {
    if (___caravan is VehicleCaravan vehicleCaravan)
    {
      __result = vehicleCaravan.vehiclePather.Moving;
    }
  }

  public static bool VehicleCaravanTweenedPosRoot(Caravan caravan, ref Vector3 __result)
  {
    if (caravan is VehicleCaravan)
    {
      __result = Find.WorldGrid.GetTileCenter(caravan.Tile);
      return false;
    }
    return true;
  }

  public static void VehicleCaravanMovingNow(ref bool __result, Caravan ___caravan)
  {
    if (___caravan is VehicleCaravan vehicleCaravan)
    {
      __result = vehicleCaravan.vehiclePather.MovingNow;
    }
  }

  public static bool VehicleCaravanTweenerTick(Caravan ___caravan)
  {
    if (___caravan is VehicleCaravan vehicleCaravan)
    {
      vehicleCaravan.vehicleTweener.TweenerTick();
      return false;
    }
    return true;
  }

  public static bool GiveSoldThingToAerialVehicle(Thing toGive, int countToGive,
    Pawn playerNegotiator, Settlement ___settlement)
  {
    AerialVehicleInFlight aerial = playerNegotiator.GetAerialVehicle();
    if (aerial != null)
    {
      Thing thing = toGive.SplitOff(countToGive);
      thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, ___settlement);
      if (thing is Pawn pawn && pawn.RaceProps.Humanlike)
      {
        VehicleRoleHandler handler = aerial.vehicle.GetNextAvailableHandler(HandlingType.None);
        if (handler == null)
        {
          Log.Error(
            $"Unable to locate available handler for {toGive}. Squeezing into other role to avoid aborted trade.");
          handler = aerial.vehicle.GetAnyAvailableHandler();
          handler ??= aerial.vehicle.handlers.RandomElementWithFallback(fallback: null);

          if (handler == null)
          {
            Log.Error(
              $"Unable to find other role to squeeze {pawn} into. Tossing into inventory.");
            return true;
          }
        }
        aerial.vehicle.TryAddPawn(pawn, handler);
        return false;
      }
      if (aerial.vehicle.AddOrTransfer(thing) <= 0)
      {
        Log.Error("Could not add sold thing to inventory.");
        thing.Destroy();
      }
      return false;
    }
    return true;
  }

  public static IEnumerable<CodeInstruction> GiveSoldThingToVehicleTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();

    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.opcode == OpCodes.Ldnull && instructionList[i + 1].opcode == OpCodes.Ldnull)
      {
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_3);
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(CaravanUtility), nameof(CaravanUtility.GetCaravan)));
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Ext_Caravan),
            nameof(Ext_Caravan.GrabPawnsFromVehicleCaravanSilentFail)));
        instruction = instructionList[++i];
      }

      yield return instruction;
    }
  }

  public static void GetParentCaravan(Thing thing, ref Caravan __result)
  {
    if (__result == null && thing is Pawn pawn)
    {
      __result = pawn.GetVehicleCaravan();
    }
  }

  public static bool RandomVehicleOwner(Caravan caravan, ref Pawn __result)
  {
    if (caravan.HasVehicle())
    {
      __result = caravan.GrabPawnsFromVehicleCaravanSilentFail().Where(caravan.IsOwner)
       .RandomElement();
      return false;
    }
    return true;
  }

  private static IEnumerable<CodeInstruction> MergeWithVehicleCaravanTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    foreach (CodeInstruction instruction in instructions)
    {
      if (instruction.opcode == OpCodes.Stloc_0)
      {
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(CaravanHelper),
            nameof(CaravanHelper.CaravanForMerging)));
      }

      yield return instruction;
    }
  }

  private static void DisableMergeForAerialVehicles(ref Command __result, Caravan caravan)
  {
    if (__result != null && caravan is VehicleCaravan vehicleCaravan)
    {
      foreach (WorldObject worldObject in Find.WorldSelector.SelectedObjects)
      {
        if (worldObject is VehicleCaravan selectedCaravan)
        {
          if (selectedCaravan.AerialVehicle || vehicleCaravan.AerialVehicle)
          {
            __result.Disable("VF_CantMergeAerialVehicle".Translate());
          }
        }
      }
    }
  }

  private static void NoTradingUndocked(Caravan caravan, ref FloatMenuAcceptanceReport __result)
  {
    if (__result.Accepted && caravan.HasBoat() &&
      !caravan.PawnsListForReading.NotNullAndAny(p => !p.IsBoat()))
    {
      __result = false;
    }
  }
}