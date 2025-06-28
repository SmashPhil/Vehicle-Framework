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
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;
using Verse.Sound;

namespace Vehicles;

internal class Patch_FormCaravanDialog : IPatchCategory
{
  // Starting at a high offset just to avoid any int value clashing with the underlying enum
  // type. It will require changes anyways since the TabRecord will be missing the translation key
  // but at least it won't cause the tab to be completely hidden.
  private const int TabVehicles = 10;

  private const string VehiclesTabLabelKey = "VF_Vehicles";
  private const string PawnsTabLabelKey = "PawnsTab";
  private const string ItemsTabLabelKey = "ItemsTab";
  private const string TravelSuppliesTabLabelKey = "TravelSupplies";

  private static readonly string[] TabKeys =
    [PawnsTabLabelKey, ItemsTabLabelKey, TravelSuppliesTabLabelKey];

  private static readonly Type FormCaravanTabEnumType;
  private static readonly Type SplitCaravanTabEnumType;

  private static readonly List<Pawn> TmpPawns = [];

  private static TransferableVehicleWidget vehiclesTransfer;
  private static int selectedTab;

  static Patch_FormCaravanDialog()
  {
    FormCaravanTabEnumType = GenTypes.GetTypeInAnyAssembly("Dialog_FormCaravan+Tab", "RimWorld");
    SplitCaravanTabEnumType = GenTypes.GetTypeInAnyAssembly("Dialog_SplitCaravan+Tab", "RimWorld");
  }

  PatchSequence IPatchCategory.PatchAt => PatchSequence.Mod;

  void IPatchCategory.PatchMethods()
  {
    // Transferables
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(TransferableUIUtility),
        "DoCountAdjustInterfaceInternal"),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(CanAdjustPawnTransferable)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(TilesPerDayCalculator),
        nameof(TilesPerDayCalculator.ApproxTilesPerDay),
        [typeof(Caravan), typeof(StringBuilder)]),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(ApproxTilesForVehicles)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(TilesPerDayCalculator),
        nameof(TilesPerDayCalculator.ApproxTilesPerDay),
        [
          typeof(List<TransferableOneWay>), typeof(float), typeof(float), typeof(PlanetTile),
          typeof(PlanetTile), typeof(bool), typeof(StringBuilder)
        ]),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(ApproxTilesForVehicleTransferables)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanUIUtility), "CreateCaravanTransferableWidgets"),
      postfix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(CreateTransferableVehicleWidget)));

    // Form Caravan
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Dialog_FormCaravan), nameof(Dialog_FormCaravan.PostOpen)),
      transpiler: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(FormCaravanPostOpenTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Dialog_FormCaravan),
        nameof(Dialog_FormCaravan.PostClose)),
      postfix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(ClearTabListPostClose)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Dialog_FormCaravan),
        nameof(Dialog_FormCaravan.DoWindowContents)),
      transpiler: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(FormCaravanTabsTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Dialog_FormCaravan), "DoBottomButtons"),
      transpiler: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(StartRoutePlanningForVehiclesTranspiler)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), "TrySend"),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(TryAndSendWithVehicles)));
  }

  /// <summary>
  /// Disable readOnly flag on pawn widget while they are in or assigned to a vehicle.
  /// </summary>
  private static void CanAdjustPawnTransferable(Transferable trad, ref bool readOnly)
  {
    if (trad.AnyThing is Pawn pawn)
      readOnly = CaravanHelper.assignedSeats.IsAssigned(pawn) || pawn.InVehicle();
  }

  /// <summary>
  /// Calculate estimated travel speed for vehicle transferables in Caravans
  /// </summary>
  private static bool ApproxTilesForVehicles(ref float __result, Caravan caravan,
    StringBuilder explanation = null)
  {
    if (caravan is VehicleCaravan vehicleCaravan)
    {
      __result = VehicleCaravanTicksPerMoveUtility.ApproxTilesPerDay(vehicleCaravan, explanation);
      return false;
    }
    return true;
  }

  /// <summary>
  /// Calculate estimated travel speed for vehicle transferables
  /// </summary>
  private static bool ApproxTilesForVehicleTransferables(ref float __result,
    List<TransferableOneWay> transferables, float massUsage, float massCapacity,
    PlanetTile tile, PlanetTile nextTile, StringBuilder explanation = null)
  {
    if (transferables.Exists(transferable =>
      transferable is { AnyThing: VehiclePawn, CountToTransfer: > 0 }))
    {
      Assert.IsTrue(TmpPawns.Count == 0);
      foreach (TransferableOneWay transferable in transferables)
      {
        if (transferable.AnyThing is Pawn pawn && transferable.CountToTransfer > 0 &&
          (pawn is VehiclePawn || !CaravanHelper.assignedSeats.IsAssigned(pawn)))
          TmpPawns.Add(pawn);
      }
      Assert.IsTrue(TmpPawns.Count > 0);
      try
      {
        // Ugly but this is how RimWorld is set up so the patch should just match the flow
        StringBuilder stringBuilder = explanation != null ? new StringBuilder() : null;
        int ticks = VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(TmpPawns, massUsage,
          massCapacity, explanation: stringBuilder);
        __result =
          TilesPerDayCalculator.ApproxTilesPerDay(ticks, tile, nextTile, explanation: explanation,
            caravanTicksPerMoveExplanation: stringBuilder?.ToString());
      }
      finally
      {
        TmpPawns.Clear();
      }
      return false;
    }
    return true;
  }

  /// <summary>
  /// Create and add <see cref="TransferableVehicleWidget"/> from transferables list.
  /// </summary>
  private static void CreateTransferableVehicleWidget(List<TransferableOneWay> transferables,
    PlanetTile tile)
  {
    List<TransferableOneWay> vehicles = [];
    List<TransferableOneWay> pawns = [];
    foreach (TransferableOneWay transferable in transferables)
    {
      switch (transferable.AnyThing)
      {
        case VehiclePawn:
          vehicles.Add(transferable);
        break;
        case Pawn and not VehiclePawn:
          pawns.Add(transferable);
        break;
      }
    }
    vehiclesTransfer =
      new TransferableVehicleWidget("VF_Vehicles".Translate(), vehicles, pawns, tile: tile);
  }

  /// <summary>
  /// Create vehicle tab and inject into tabs list without the need for a new enum value.
  /// Also disables initial route planning since vehicle selection may occur and invalidate the route.
  /// </summary>
  private static IEnumerable<CodeInstruction> FormCaravanPostOpenTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();

    // this
    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
    // this.map
    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
    yield return new CodeInstruction(opcode: OpCodes.Ldfld,
      operand: AccessTools.Field(typeof(Dialog_FormCaravan), "map"));
    // this.tabsList
    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
    yield return new CodeInstruction(opcode: OpCodes.Ldfld,
      AccessTools.Field(typeof(Dialog_FormCaravan), "tabsList"));
    // this.thisWindowInstanceEverOpened
    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
    yield return new CodeInstruction(opcode: OpCodes.Ldfld,
      operand: AccessTools.Field(typeof(Dialog_FormCaravan), "thisWindowInstanceEverOpened"));
    // Patch_FormCaravanDialog::CreateTabListPostOpen(this, map, tabsList, thisWindowInstanceEverOpened);
    yield return new CodeInstruction(opcode: OpCodes.Call,
      operand: AccessTools.Method(typeof(Patch_FormCaravanDialog), nameof(CreateTabListPostOpen)));

    MethodInfo worldRoutePlannerMethod = AccessTools.Method(typeof(WorldRoutePlanner),
      nameof(WorldRoutePlanner.Start), parameters: [typeof(Dialog_FormCaravan)]);
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (!instructionList.OutOfBounds(i + 2))
      {
        CodeInstruction lookAhead = instructionList[i + 2];
        if (lookAhead.Calls(worldRoutePlannerMethod))
        {
          // Call Find::get_WorldRoutePlanner
          // Ldarg.0
          // CallVirt WorldRoutePlanner::Start(Dialog_FormCaravan)
          i += 3;
          Assert.IsFalse(instructionList.OutOfBounds(i));
          instruction = instructionList[i];
        }
      }
      yield return instruction;
    }
  }

  private static void CreateTabListPostOpen(Dialog_FormCaravan formCaravan, Map map,
    List<TabRecord> tabsList, bool thisWindowInstanceEverOpened)
  {
    if (!thisWindowInstanceEverOpened)
    {
      Assert.IsNull(CaravanFormation.formation);
      Assert.IsTrue(tabsList.NullOrEmpty());
      CaravanFormation.formation = new FormationInfo(formCaravan, map);
      selectedTab = TabVehicles;
      tabsList.Add(new TabRecord(VehiclesTabLabelKey.Translate(),
        delegate { selectedTab = TabVehicles; },
        () => selectedTab == TabVehicles));
      foreach (int value in Enum.GetValues(FormCaravanTabEnumType))
      {
        string translationKey = !TabKeys.OutOfBounds(value) ? TabKeys[value] : "Missing Label";
        tabsList.Add(new TabRecord(translationKey.Translate(), delegate { selectedTab = value; },
          () => selectedTab == value));
      }
    }
  }

  /// <summary>
  /// Clear static fields for dialog on close
  /// </summary>
  private static void ClearTabListPostClose(List<TabRecord> ___tabsList, bool ___choosingRoute)
  {
    if (!___choosingRoute)
    {
      CaravanFormation.formation = null;
      ___tabsList.Clear();
      selectedTab = TabVehicles;
      CaravanHelper.assignedSeats.Clear();
    }
  }

  /// <summary>
  /// Override tab drawing so we can insert the vehicles tab without the need to modify the enum
  /// </summary>
  private static IEnumerable<CodeInstruction> FormCaravanTabsTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    // ReSharper disable ExtractCommonBranchingCode
    List<CodeInstruction> instructionList = [.. instructions];

    FieldInfo tabListField = AccessTools.Field(typeof(Dialog_FormCaravan), "tabsList");
    FieldInfo tabField = AccessTools.Field(typeof(Dialog_FormCaravan), "tab");
    MethodInfo clearTabList =
      AccessTools.Method(typeof(List<TabRecord>), nameof(List<TabRecord>.Clear));
    bool tabClearing = false;
    bool switchBlockClearing = false;
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      // NOTE - We search for begin and end of tab init since Ludeon creates the tab list in every
      // single OnGUI event. Since we've created the list once in PostOpen, we skip the entire block
      // and just handle the drawing with our "inserted" enum value.
      if (instruction.LoadsField(tabListField) && instructionList[i + 1].Calls(clearTabList))
      {
        if (!tabClearing)
        {
          // Flag transpiler to start skipping instructions until we reach the 2nd tabsList.Clear()
          tabClearing = true;
        }
        else
        {
          tabClearing = false;
          // ReSharper disable once RedundantAssignment
          instruction = instructionList[++i]; // Ldsfld: Dialog_FormCaravan::tabsList
          instruction = instructionList[++i]; // Callvirt: List`1<TabRecord>::Clear
          // ref inRect
          yield return new CodeInstruction(opcode: OpCodes.Ldarga_S, operand: 1);
          // Dialog_FormCaravan::tabsList
          yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: tabListField);
          // DrawTabList(ref inRect, tabsList);
          yield return new CodeInstruction(opcode: OpCodes.Call,
            operand: AccessTools.Method(typeof(Patch_FormCaravanDialog), nameof(DrawTabList)));
        }
      }
      // Since we're using our own int-based values parallel to the Tab enum, we can skip the entire
      // switch block and just handle the drawing ourselves. If we didn't do this, the enum would never
      // be able to hold our Vehicle tab int value, so it would never be drawn.
      else if (!tabClearing && !switchBlockClearing && instruction.LoadsField(tabField))
      {
        switchBlockClearing = true;
        instruction = instructionList[++i]; // Ldfld: Dialog_FormCaravan::tab
        // transferablesRect
        yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 3);
        // out bool anythingChanged
        yield return new CodeInstruction(opcode: OpCodes.Ldloca_S, operand: 4);
        // this->pawnsTransfer
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: AccessTools.Field(typeof(Dialog_FormCaravan), "pawnsTransfer"));
        // this->pawnsTransfer
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: AccessTools.Field(typeof(Dialog_FormCaravan), "itemsTransfer"));
        // this->pawnsTransfer
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: AccessTools.Field(typeof(Dialog_FormCaravan), "travelSuppliesTransfer"));
        // DrawActiveTab
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_FormCaravanDialog), nameof(DrawActiveTab)));
      }
      if (switchBlockClearing && instructionList[i + 1].opcode == OpCodes.Ldloc_S &&
        instructionList[i + 1].operand is LocalBuilder { LocalIndex: 4 })
      {
        switchBlockClearing = false;
        instruction = instructionList[++i]; // Br_S: IL_02D8  (end of switch block)
      }

      if (!tabClearing && !switchBlockClearing)
        yield return instruction;
    }
    // ReSharper restore ExtractCommonBranchingCode
  }

  private static void DrawTabList(ref Rect inRect, List<TabRecord> tabsList)
  {
    inRect.yMin += 119f;
    Widgets.DrawMenuSection(inRect);
    TabDrawer.DrawTabs(inRect, tabsList);
  }

  private static void DrawActiveTab(Dialog_FormCaravan __instance, Rect transferablesRect,
    out bool anythingChanged, TransferableOneWayWidget pawnsTransfer,
    TransferableOneWayWidget itemsTransfer, TransferableOneWayWidget travelSuppliesTransfer)
  {
    anythingChanged = false;
    switch (selectedTab)
    {
      case 0: // Dialog_FormCaravan.Tab.Pawns
        pawnsTransfer.OnGUI(transferablesRect, out anythingChanged);
      break;
      case 1: // Dialog_FormCaravan.Tab.Items
        itemsTransfer.OnGUI(transferablesRect, out anythingChanged);
      break;
      case 2: // Dialog_FormCaravan.Tab.TravelSupplies
        travelSuppliesTransfer.extraHeaderSpace = 35;
        travelSuppliesTransfer.OnGUI(transferablesRect, out anythingChanged);
        __instance.DrawAutoSelectCheckbox(transferablesRect, ref anythingChanged);
      break;
      case TabVehicles: // Vehicles Tab
        // TODO 1.6 - check if `anythingChanged` is usable in TransferableVehicleWidget
        vehiclesTransfer.OnGUI(transferablesRect /*, out anythingChanged*/);
      break;
      default:
        Log.Error(
          $"Unknown enum type {selectedTab} for patched FormCaravan dialog. Switching back to known tab");
        selectedTab = 0;
      break;
    }
  }

  /// <summary>
  /// Reroutes <see cref="WorldRoutePlanner.Start(Dialog_FormCaravan)"/> to plan for vehicle caravans.
  /// </summary>
  private static IEnumerable<CodeInstruction> StartRoutePlanningForVehiclesTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();
    MethodInfo startPlanningMethod =
      AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.Start),
        parameters: [typeof(Dialog_FormCaravan)]);
    FieldInfo mapField = AccessTools.Field(typeof(Dialog_FormCaravan), "map");
    FieldInfo autoSelectTravelSuppliesField =
      AccessTools.Field(typeof(Dialog_FormCaravan), "autoSelectTravelSupplies");
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(startPlanningMethod))
      {
        // Callvirt WorldRoutePlanner::Start(Dialog_FormCaravan)
        instruction = instructionList[++i];
        // ON STACK - WorldRoutePlanner instance, Dialog_FormCaravan instance
        // this.map
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: mapField);
        // this.autoSelectTravelSupplies
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: autoSelectTravelSuppliesField);
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_FormCaravanDialog),
            nameof(WorldRoutePannerReroute)));
      }
      yield return instruction;
    }
  }

  // NOTE - It's easier to pass in the world route planner since it's already on the stack
  private static void WorldRoutePannerReroute(WorldRoutePlanner routePlanner,
    Dialog_FormCaravan formCaravan, Map map, bool autoSelectTravelSupplies)
  {
    if (formCaravan.transferables.Exists(transferable =>
      transferable is { CountToTransfer: > 0, AnyThing: VehiclePawn }))
    {
      CaravanFormation.formation.ChoosingRoute = true;
      Find.WindowStack.TryRemove(formCaravan, doCloseSound: false);
      VehicleCaravanInfo caravanInfo = new(formCaravan.transferables, formCaravan.MassUsage,
        formCaravan.MassCapacity, formCaravan.CurrentTile)
      {
        caravaning = true,
      };
      Find.World.GetComponent<VehicleRoutePlanner>().Start(caravanInfo, delegate
      {
        Find.WindowStack.Add(formCaravan);
        formCaravan.Notify_NoLongerChoosingRoute();
      }, ChoseVehicleRoute);
    }
    else
    {
      routePlanner.Start(formCaravan);
    }
    return;

    void ChoseVehicleRoute(PlanetTile tile)
    {
      CaravanFormation.formation.DestinationTile = tile;
      List<VehicleDef> vehicleDefs = TransferableUtility
       .GetPawnsFromTransferables(formCaravan.transferables)
       .UniqueVehicleDefsInList();
      CaravanFormation.formation.StartingTile =
        CaravanHelper.BestExitTileToGoTo(vehicleDefs, tile, map);
      CaravanFormation.formation.TicksToArriveDirty = true;
      CaravanFormation.formation.DaysWorthOfFoodDirty = true;

      formCaravan.soundAppear.PlayOneShotOnCamera();
      if (autoSelectTravelSupplies)
      {
        CaravanFormation.formation.SelectApproximateBestTravelSupplies();
      }
    }
  }

  /// <summary>
  /// Reroute caravan send off to create VehicleCaravan or initialize vehicle caravan lord job.
  /// </summary>
  private static bool TryAndSendWithVehicles(Dialog_FormCaravan __instance)
  {
    if (CaravanFormation.formation.Reform &&
      CaravanFormation.TryShowConfirmLeaveVehiclesDialog(__instance))
    {
      return false;
    }
    if (__instance.transferables.Exists(transferable =>
      transferable is { CountToTransfer: > 0, AnyThing: VehiclePawn }))
    {
      CaravanFormation.TrySendVehicleCaravan(__instance);
      return false;
    }
    return true;
  }
}