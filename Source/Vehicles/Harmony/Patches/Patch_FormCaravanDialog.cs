using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Patching;
using SmashTools.Performance;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Compatibility;
using Vehicles.World;
using Verse;
using Verse.Sound;

namespace Vehicles;

[UsedImplicitly]
internal class Patch_FormCaravanDialog : IPatchCategory
{
  // Starting at a high offset just to avoid any int value clashing with the underlying enum
  // type. It will require changes anyway since the TabRecord will be missing the translation key
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
  private static readonly MethodInfo IgnoreInventoryModeProp;
  private static readonly AccessTools.FieldRef<Dialog_FormCaravan, bool> ReformFieldRef;

  private static Type gizmoStateMachineType;

  private static TransferableVehicleWidget vehiclesTransfer;
  private static int selectedTab;

  static Patch_FormCaravanDialog()
  {
    FormCaravanTabEnumType = GenTypes.GetTypeInAnyAssembly("Dialog_FormCaravan+Tab", "RimWorld");
    SplitCaravanTabEnumType = GenTypes.GetTypeInAnyAssembly("Dialog_SplitCaravan+Tab", "RimWorld");
    IgnoreInventoryModeProp = AccessTools.PropertyGetter(typeof(Dialog_FormCaravan), "IgnoreInventoryMode");
    ReformFieldRef = AccessTools.FieldRefAccess<bool>(typeof(Dialog_FormCaravan), "reform");
  }

  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  private static bool HasVehiclesAvailable(Dialog_FormCaravan formCaravan)
  {
    foreach (TransferableOneWay transferable in formCaravan.transferables)
    {
      if (transferable is { AnyThing: VehiclePawn })
        return true;
    }
    return false;
  }

  private static bool VehiclesSelected(List<TransferableOneWay> transferables)
  {
    foreach (TransferableOneWay transferable in transferables)
    {
      if (transferable is { AnyThing: VehiclePawn, CountToTransfer: > 0 })
        return true;
    }
    return false;
  }

  [MustDisposeResource]
  private static GlobalObjectPool.CollectionReceipt<List<VehiclePawn>, VehiclePawn> GetVehiclesToTransfer(
    List<TransferableOneWay> transferables, out List<VehiclePawn> vehicles)
  {
    var scope = GlobalObjectPool.Get(out vehicles);
    foreach (TransferableOneWay transferable in transferables)
    {
      if (transferable is { AnyThing: VehiclePawn, CountToTransfer: > 0 })
      {
        vehicles.Add(transferable.AnyThing as VehiclePawn);
      }
    }
    return scope;
  }

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
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanFormingUtility),
        nameof(CaravanFormingUtility.AllSendablePawns)),
      postfix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(AllSendablePawnsInVehicles)));

    // Form Caravan
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Dialog_FormCaravan), nameof(Dialog_FormCaravan.PostOpen)),
      postfix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(InitVehicleAssignments)),
      transpiler: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(FormCaravanPostOpenTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Dialog_FormCaravan),
        nameof(Dialog_FormCaravan.PostClose)),
      postfix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(FormCaravanPostClose)));
    HarmonyPatcher.Patch(original: AccessTools.PropertyGetter(typeof(Dialog_FormCaravan), "DaysWorthOfFood"),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog), nameof(DaysOfWorthOfFoodWithVehicles)));
    HarmonyPatcher.Patch(original: AccessTools.PropertyGetter(typeof(Dialog_FormCaravan), "TicksToArrive"),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog), nameof(TicksToArriveWithVehicles)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Dialog_FormCaravan), "DoBottomButtons"),
      transpiler: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(StartRoutePlanningForVehiclesTranspiler)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Dialog_FormCaravan), "TrySend"),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(TryAndSendWithVehicles)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Dialog_FormCaravan), "DebugTryFormCaravanInstantly"),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog), nameof(TryFormCaravanInstantly)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(WorldGizmoUtility),
        nameof(WorldGizmoUtility.TryGetCaravanGizmo)),
      transpiler: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(TryGetCaravanForVehicles)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(FormCaravanComp),
        nameof(FormCaravanComp.CanReformNow)),
      postfix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(CanReformNowWithVehicles)));

    // Split Caravan
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Dialog_SplitCaravan),
        nameof(Dialog_SplitCaravan.PostOpen)),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(SplitCaravanPostOpen)));
    HarmonyPatcher.Patch(original: AccessTools.PropertyGetter(typeof(Dialog_SplitCaravan), "DestDaysWorthOfFood"),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog), nameof(SplitDaysOfWorthOfFoodWithVehicles)));
    HarmonyPatcher.Patch(original: AccessTools.PropertyGetter(typeof(Dialog_SplitCaravan), "TicksToArrive"),
      prefix: new HarmonyMethod(typeof(Patch_FormCaravanDialog), nameof(SplitTicksToArriveWithVehicles)));


    if (!Ext_Mods.HasActiveMod(ModPackageIds.CaravanItemSelectionEnhanced))
    {
      HarmonyPatcher.Patch(
        original: AccessTools.Method(typeof(Dialog_FormCaravan),
          nameof(Dialog_FormCaravan.DoWindowContents)),
        transpiler: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
          nameof(FormCaravanTabsTranspiler)));
      HarmonyPatcher.Patch(
        original: AccessTools.Method(typeof(Dialog_SplitCaravan),
          nameof(Dialog_SplitCaravan.DoWindowContents)),
        transpiler: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
          nameof(SplitCaravanTabsTranspiler)));
    }

    MethodInfo getGizmosMethod = AccessTools.Method(typeof(FormCaravanComp),
      nameof(FormCaravanComp.GetGizmos));
    gizmoStateMachineType = getGizmosMethod.GetStateMachineType();
    // Only fetches the first delegate, will need checking on major updates.
    MethodBase reformDelegate = gizmoStateMachineType.GetIteratorMethod();
    HarmonyPatcher.Patch(original: reformDelegate,
      transpiler: new HarmonyMethod(typeof(Patch_FormCaravanDialog),
        nameof(ReformCaravanWithVehiclesGizmoTranspiler)));
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
    if (VehiclesSelected(transferables))
    {
      using var cps = GlobalObjectPool.Get(out List<Pawn> pawns);
      foreach (TransferableOneWay transferable in transferables)
      {
        if (transferable is not { AnyThing: Pawn, CountToTransfer: > 0 })
          continue;

        pawns.Add(transferable.AnyThing as Pawn);
      }
      Assert.IsTrue(pawns.Count > 0);
      // Ugly but this is how RimWorld is set up so the patch should just match the flow
      StringBuilder stringBuilder = explanation != null ? new StringBuilder() : null;
      int ticks = VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(pawns, massUsage, massCapacity,
        explanation: stringBuilder);

      TicksPerMoveData data = new()
      {
        ticksPerMove = ticks,
        tile = tile,
        nextTile = nextTile,
        explanation = explanation,
        caravanTicksPerMoveExplanation = stringBuilder?.ToString()
      };
      __result = VehicleCaravanTicksPerMoveUtility.ApproxTilesPerDay(pawns.VehiclesInList(), data);
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
  /// Append passengers to 'sendable pawns' list in form caravan dialog.
  /// </summary>
  private static List<Pawn> AllSendablePawnsInVehicles(List<Pawn> __result, Map map)
  {
    VehiclePositionManager positionManager = map.GetDetachedMapComponent<VehiclePositionManager>();
    Assert.IsNotNull(positionManager);
    foreach (VehiclePawn vehicle in positionManager.AllClaimants)
    {
      if (vehicle.AllPawnsAboard.Count > 0)
        __result.AddRange(vehicle.AllPawnsAboard);
    }
    return __result;
  }

  /// <summary>
  /// Initializes vehicle seat assignments for all vehicles when dialog is opened for reform.
  /// </summary>
  private static void InitVehicleAssignments(Dialog_FormCaravan __instance)
  {
    if (ReformFieldRef(__instance))
    {
      foreach (TransferableOneWay transferable in __instance.transferables)
      {
        if (transferable.AnyThing is not VehiclePawn vehicle)
          continue;

        foreach (VehicleRoleHandler handler in vehicle.Handlers)
        {
          foreach (Pawn pawn in handler.thingOwner)
          {
            CaravanHelper.assignedSeats.SetAssignment(new AssignedSeat(pawn, handler));
          }
        }
      }
    }
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

          // SetInitialTab(this)
          yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
          yield return new CodeInstruction(opcode: OpCodes.Call,
            operand: AccessTools.Method(typeof(Patch_FormCaravanDialog), nameof(SetInitialTab)));
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
      Assert.IsTrue(tabsList.Count == 0);
      CaravanFormation.formation = new FormationInfo(formCaravan, map);
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

  private static void SetInitialTab(Dialog_FormCaravan formCaravan)
  {
    const int TabPawns = 0;

    selectedTab = HasVehiclesAvailable(formCaravan) ? TabVehicles : TabPawns;
  }

  /// <summary>
  /// Clear static fields for dialog on close
  /// </summary>
  private static void FormCaravanPostClose(List<TabRecord> ___tabsList,
    bool ___choosingRoute)
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
  /// Calculate days worth of food for vehicle caravan which may path differently (or on impassable terrain)
  /// relative to normal caravans.
  /// </summary>
  private static bool DaysOfWorthOfFoodWithVehicles(Dialog_FormCaravan __instance,
    ref (float days, float tillRot) ___cachedDaysWorthOfFood,
    ref bool ___daysWorthOfFoodDirty, PlanetTile ___destinationTile)
  {
    using var cps = GetVehiclesToTransfer(__instance.transferables, out List<VehiclePawn> vehicles);
    if (___daysWorthOfFoodDirty && vehicles.Count > 0)
    {
      float days;
      float tillRot;
      ___daysWorthOfFoodDirty = false;
      IgnorePawnsInventoryMode inventoryMode =
        (IgnorePawnsInventoryMode)IgnoreInventoryModeProp.Invoke(__instance, null);

      if (___destinationTile.Valid)
      {
        using WorldPath path = Find.World.GetComponent<WorldVehiclePathfinder>()
         .FindPath(__instance.CurrentTile, ___destinationTile, vehicles);
        int ticksPerMove = VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(new VehicleCaravanInfo(__instance));
        days = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(__instance.transferables, __instance.CurrentTile,
          inventoryMode, Faction.OfPlayer, path, nextTileCostLeft: 0f, ticksPerMove);
        tillRot = DaysUntilRotCalculator.ApproxDaysUntilRot(__instance.transferables, __instance.CurrentTile,
          inventoryMode, path, nextTileCostLeft: 0f, ticksPerMove);
      }
      else
      {
        days = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(__instance.transferables, __instance.CurrentTile,
          inventoryMode, Faction.OfPlayer);
        tillRot = DaysUntilRotCalculator.ApproxDaysUntilRot(__instance.transferables, __instance.CurrentTile,
          inventoryMode);
      }
      ___cachedDaysWorthOfFood = (days, tillRot);
      return false;
    }
    return true;
  }

  /// <summary>
  /// Calculate ticks to arrive at destination tile for vehicle caravan.
  /// </summary>
  private static bool TicksToArriveWithVehicles(Dialog_FormCaravan __instance,
    ref int ___cachedTicksToArrive, ref bool ___ticksToArriveDirty, PlanetTile ___destinationTile)
  {
    if (!___destinationTile.Valid)
      return true;

    using var cps = GetVehiclesToTransfer(__instance.transferables, out List<VehiclePawn> vehicles);
    if (___ticksToArriveDirty && vehicles.Count > 0)
    {
      ___ticksToArriveDirty = false;
      using WorldPath path = Find.World.GetComponent<WorldVehiclePathfinder>()
       .FindPath(__instance.CurrentTile, ___destinationTile, vehicles);
      VehicleCaravanInfo caravanInfo = new(__instance);
      int ticksPerMove = VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(caravanInfo);
      ___cachedTicksToArrive = VehicleCaravanPathingHelper.EstimatedTicksToArrive(
        caravanInfo.vehiclesAndDismountedPawns.UniqueVehicleDefsInList(), __instance.CurrentTile, ___destinationTile,
        path, nextTileCostLeft: 0, caravanTicksPerMove: ticksPerMove, Find.TickManager.TicksAbs);
      return false;
    }
    return true;
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
      AccessTools.Method(typeof(List<TabRecord>), nameof(List<>.Clear));
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
        // this->itemsTransfer
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: AccessTools.Field(typeof(Dialog_FormCaravan), "itemsTransfer"));
        // this->travelSuppliesTransfer
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: AccessTools.Field(typeof(Dialog_FormCaravan), "travelSuppliesTransfer"));
        // DrawActiveTab
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_FormCaravanDialog), nameof(DrawActiveTab)));
      }
      if (switchBlockClearing && instructionList[i].opcode == OpCodes.Ldloc_S &&
        instructionList[i].operand is LocalBuilder { LocalIndex: 4 })
      {
        // Br_S: IL_02DB  (end of switch block)
        switchBlockClearing = false;
      }

      if (!tabClearing && !switchBlockClearing)
        yield return instruction;
    }
    // ReSharper restore ExtractCommonBranchingCode
  }

  private static void DrawTabList(ref Rect inRect, List<TabRecord> tabsList)
  {
    if (Ext_Mods.HasActiveMod(ModPackageIds.CaravanItemSelectionEnhanced))
      return;

    inRect.yMin += 119f;
    Widgets.DrawMenuSection(inRect);
    TabDrawer.DrawTabs(inRect, tabsList);
  }

  private static void DrawActiveTab(Dialog_FormCaravan __instance, Rect transferablesRect,
    out bool anythingChanged, TransferableOneWayWidget pawnsTransfer,
    TransferableOneWayWidget itemsTransfer, TransferableOneWayWidget travelSuppliesTransfer)
  {
    anythingChanged = false;
    if (Ext_Mods.HasActiveMod(ModPackageIds.CaravanItemSelectionEnhanced))
      return;

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
        __instance?.DrawAutoSelectCheckbox(transferablesRect, ref anythingChanged);
        break;
      case TabVehicles: // Vehicles Tab
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
    if (VehiclesSelected(formCaravan.transferables))
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
    if (VehiclesSelected(__instance.transferables))
    {
      CaravanFormation.TrySendVehicleCaravan(__instance);
      return false;
    }
    return true;
  }

  private static bool TryFormCaravanInstantly(Dialog_FormCaravan __instance, Map ___map,
    PlanetTile ___startingTile, PlanetTile ___destinationTile)
  {
    if (CaravanFormation.formation == null)
      return true;
    CaravanFormation.formation.RecacheTransferables();
    if (CaravanFormation.formation.vehicles.NullOrEmpty())
      return true;

    if (CaravanFormation.formation.vehicles.Exists(vehicle =>
      !Find.World.GetComponent<WorldVehiclePathGrid>()
       .PassableFast(___map.Tile, vehicle.VehicleDef)))
    {
      Messages.Message("MessageNoValidExitTile".Translate(), MessageTypeDefOf.RejectInput, false);
      return false;
    }
    if (!CaravanFormation.formation.AllPawnsAndVehicles.Any(pawn => CaravanUtility.IsOwner(pawn, Faction.OfPlayer)))
    {
      Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(),
        MessageTypeDefOf.RejectInput, false);
      return false;
    }
    CaravanHelper.BoardAllAssignedPawns();
    CaravanFormation.formation.AddItemsFromTransferablesToRandomInventories(CaravanFormation.formation
     .AllPawnsAndVehicles);

    PlanetTile exitTile = ___startingTile;
    if (!exitTile.Valid)
      exitTile = CaravanExitMapUtility.RandomBestExitTileFrom(___map);
    if (!exitTile.Valid)
      exitTile = __instance.CurrentTile;

    CaravanHelper.ExitMapAndCreateVehicleCaravan(CaravanFormation.formation.AllPawnsAndVehicles, Faction.OfPlayer,
      __instance.CurrentTile, exitTile, ___destinationTile);
    SoundDefOf.Tick_High.PlayOneShotOnCamera();
    __instance.Close(doCloseSound: false);
    return false;
  }

  private static IEnumerable<CodeInstruction> TryGetCaravanForVehicles(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();
    FieldInfo mapPawnsField =
      AccessTools.Field(typeof(Map), nameof(Map.mapPawns));
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.LoadsField(mapPawnsField))
      {
        // ReSharper disable once RedundantAssignment
        instruction = instructionList[++i]; // ldfld Map::mapPawns
        instruction = instructionList[++i]; // callvirt MapPawns::get_ColonistCount
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_FormCaravanDialog),
            nameof(PawnsOrAutonomousVehicles)));
      }

      yield return instruction;
    }
  }

  private static int PawnsOrAutonomousVehicles(Map map)
  {
    int count = map.mapPawns.ColonistCount;
    if (count > 0)
      return count;
    // If there are no colonists registered in the map, we need to perform a check in vehicles and
    // for autonomous vehicles.
    VehiclePositionManager positionManager = map.GetDetachedMapComponent<VehiclePositionManager>();
    Assert.IsNotNull(positionManager);
    foreach (VehiclePawn vehicle in positionManager.AllClaimants)
    {
      if (vehicle.MovementPermissions == VehiclePermissions.Autonomous)
      {
        count++;
      }
      count += vehicle.AllPawnsAboard.Count;
    }
    return count;
  }

  private static void CanReformNowWithVehicles(ref bool __result, FormCaravanComp __instance,
    WorldObject ___parent)
  {
    if (__result)
      return;
    if (___parent is not MapParent { HasMap: true } mapParent || !__instance.Reform)
      return;
    if (!__instance.CanFormOrReformCaravanNow)
      return;

    Assert.IsNotNull(mapParent);
    VehiclePositionManager positionManager =
      mapParent.Map.GetDetachedMapComponent<VehiclePositionManager>();
    Assert.IsNotNull(positionManager);
    foreach (VehiclePawn vehicle in positionManager.AllClaimants)
    {
      if (vehicle.MovementPermissions == VehiclePermissions.Autonomous ||
          vehicle.AllPawnsAboard.Exists(static pawn => pawn.IsColonist && !pawn.InMentalState))
      {
        __result = true;
        return;
      }
    }
  }

  /// <summary>
  /// Create vehicle tab and inject into tabs list without the need for a new enum value.
  /// Also disables initial route planning since vehicle selection may occur and invalidate the route.
  /// </summary>
  private static void SplitCaravanPostOpen(Dialog_SplitCaravan __instance, List<TabRecord> ___tabsList,
    Caravan ___caravan)
  {
    selectedTab = TabVehicles;
    CaravanFormation.splitter = new SplitInfo(__instance, ___caravan);
    ___tabsList.Clear();
    ___tabsList.Add(new TabRecord(VehiclesTabLabelKey.Translate(),
      delegate { selectedTab = TabVehicles; },
      () => selectedTab == TabVehicles));
    foreach (int value in Enum.GetValues(SplitCaravanTabEnumType))
    {
      string translationKey = !TabKeys.OutOfBounds(value) ? TabKeys[value] : "Missing Label";
      ___tabsList.Add(new TabRecord(translationKey.Translate(), delegate { selectedTab = value; },
        () => selectedTab == value));
    }
  }

  /// <summary>
  /// Calculate days worth of food for post vehicle caravan split which may path differently (or on impassable terrain)
  /// relative to normal caravans.
  /// </summary>
  private static bool SplitDaysOfWorthOfFoodWithVehicles(List<TransferableOneWay> ___transferables, Caravan ___caravan,
    ref (float days, float tillRot) ___cachedDestDaysWorthOfFood, ref bool ___destDaysWorthOfFoodDirty)
  {
    if (___destDaysWorthOfFoodDirty && ___caravan is VehicleCaravan vehicleCaravan)
    {
      const IgnorePawnsInventoryMode InventoryMode = IgnorePawnsInventoryMode.Ignore;

      float days;
      float tillRot;
      ___destDaysWorthOfFoodDirty = false;
      if (vehicleCaravan.vehiclePather.Moving)
      {
        days = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(___transferables, vehicleCaravan.Tile,
          InventoryMode, vehicleCaravan.Faction, vehicleCaravan.vehiclePather.curPath,
          vehicleCaravan.vehiclePather.nextTileCostLeft, vehicleCaravan.TicksPerMove);
        tillRot = DaysUntilRotCalculator.ApproxDaysUntilRot(___transferables, vehicleCaravan.Tile,
          InventoryMode, vehicleCaravan.vehiclePather.curPath, vehicleCaravan.vehiclePather.nextTileCostLeft,
          vehicleCaravan.TicksPerMove);
      }
      else
      {
        days = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(___transferables, vehicleCaravan.Tile, InventoryMode,
          vehicleCaravan.Faction);
        tillRot = DaysUntilRotCalculator.ApproxDaysUntilRot(___transferables, vehicleCaravan.Tile, InventoryMode);
      }
      ___cachedDestDaysWorthOfFood = (days, tillRot);
      return false;
    }
    return true;
  }

  /// <summary>
  /// Calculate ticks to arrive after splitting vehicle caravan.
  /// </summary>
  private static bool SplitTicksToArriveWithVehicles(Caravan ___caravan,
    ref int __result, ref int ___cachedTicksToArrive, ref bool ___ticksToArriveDirty)
  {
    if (___caravan is not VehicleCaravan vehicleCaravan)
      return true;
    if (!vehicleCaravan.vehiclePather.Moving)
    {
      __result = 0;
      return false;
    }
    if (___ticksToArriveDirty)
    {
      ___ticksToArriveDirty = false;
      ___cachedTicksToArrive = VehicleCaravanPathingHelper.EstimatedTicksToArrive(vehicleCaravan, allowCaching: false);
    }
    return false;
  }

  private static IEnumerable<CodeInstruction> SplitCaravanTabsTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    // ReSharper disable ExtractCommonBranchingCode
    List<CodeInstruction> instructionList = [.. instructions];

    FieldInfo tabListField = AccessTools.Field(typeof(Dialog_SplitCaravan), "tabsList");
    FieldInfo tabField = AccessTools.Field(typeof(Dialog_SplitCaravan), "tab");
    MethodInfo clearTabList =
      AccessTools.Method(typeof(List<TabRecord>), nameof(List<>.Clear));
    MethodInfo drawTabs = typeof(TabDrawer).GetMethods(BindingFlags.Static | BindingFlags.Public)
     .Where(method => method.Name == nameof(TabDrawer.DrawTabs))
     .FirstOrDefault(method => method.GetParameters().Length == 3)?
     .MakeGenericMethod(typeof(TabRecord));
    bool tabClearing = false;
    bool switchBlockClearing = false;
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.LoadsField(tabListField) && instructionList[i + 1].Calls(clearTabList))
      {
        // Flag transpiler to start skipping instructions until we reach the TabDrawer::DrawTabs call
        tabClearing = true;
      }
      else if (instruction.Calls(drawTabs))
      {
        tabClearing = false;
        // ReSharper disable once RedundantAssignment
        instruction = instructionList[++i]; // Callvirt: TabDrawer::DrawTabs
        instruction = instructionList[++i]; // Pop
                                            // ref inRect
        yield return new CodeInstruction(opcode: OpCodes.Ldarga_S, operand: 1);
        // Dialog_SplitCaravan::tabsList
        yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: tabListField);
        // DrawTabList(ref inRect, tabsList);
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_FormCaravanDialog), nameof(DrawTabList)));
      }
      // Since we're using our own int-based values parallel to the Tab enum, we can skip the entire
      // switch block and just handle the drawing ourselves.
      else if (!tabClearing && !switchBlockClearing && instruction.LoadsField(tabField))
      {
        switchBlockClearing = true;
        instruction = instructionList[++i]; // Ldfld: Dialog_SplitCaravan::tab
                                            // null
        yield return new CodeInstruction(opcode: OpCodes.Pop);
        yield return new CodeInstruction(opcode: OpCodes.Ldnull);
        // transferablesRect
        yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 2);
        // out bool anythingChanged
        yield return new CodeInstruction(opcode: OpCodes.Ldloca_S, operand: 3);
        // this->pawnsTransfer
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: AccessTools.Field(typeof(Dialog_SplitCaravan), "pawnsTransfer"));
        // this->itemsTransfer
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: AccessTools.Field(typeof(Dialog_SplitCaravan), "itemsTransfer"));
        // this->foodAndMedicineTransfer
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: AccessTools.Field(typeof(Dialog_SplitCaravan), "foodAndMedicineTransfer"));

        // DrawActiveTab
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_FormCaravanDialog), nameof(DrawActiveTab)));
      }
      if (switchBlockClearing && instructionList[i + 1].opcode == OpCodes.Ldloc_3)
      {
        switchBlockClearing = false;
        instruction = instructionList[++i]; // brfalse.s IL_029A  (end of switch block)
      }

      if (!tabClearing && !switchBlockClearing)
        yield return instruction;
    }
    // ReSharper restore ExtractCommonBranchingCode
  }

  private static IEnumerable<CodeInstruction> ReformCaravanWithVehiclesGizmoTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();
    FieldInfo mapPawnsField = AccessTools.Field(typeof(Map), nameof(Map.mapPawns));
    FieldInfo containerField = gizmoStateMachineType.GetIteratorDataField(field => field.Name == "<>8__1");
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.LoadsField(mapPawnsField))
      {
        // ReSharper disable once RedundantAssignment
        yield return instruction;
        instruction = instructionList[++i]; // ldfld Map::mapPawns
        yield return instruction;
        instruction = instructionList[++i]; // callvirt MapPawns::get_FreeColonistsSpawnedCount

        // this.stateMachine.container.mapParent
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: containerField);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: AccessTools.Field(containerField.FieldType, "mapParent"));
        // count += AppendMapPawnsInVehicles(count, mapParent)
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_FormCaravanDialog),
            nameof(AppendMapPawnsInVehicles)));
      }
      yield return instruction;
    }
  }

  private static int AppendMapPawnsInVehicles(int count, MapParent mapParent)
  {
    if (count == 0)
    {
      VehiclePositionManager positionManager = mapParent.Map.GetDetachedMapComponent<VehiclePositionManager>();
      count = positionManager.AllClaimants.Sum(vehicle => vehicle.AllPawnsAboard.Count);
    }
    return count;
  }
}