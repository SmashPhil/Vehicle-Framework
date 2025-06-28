using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Vehicles.World;

public static class CaravanFormation
{
  private static readonly List<Thing> TmpPackingSpots = [];

  public static FormationInfo formation;

  public static bool TryShowConfirmLeaveVehiclesDialog(Dialog_FormCaravan formCaravan)
  {
    Assert.IsNotNull(formation);
    formation.RecacheTransferables();
    if (formation.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).HasVehicle())
    {
      List<Pawn> pawns = TransferableUtility.GetPawnsFromTransferables(formCaravan.transferables);

      string vehicles = "";
      foreach (Pawn pawn in pawns.Where(p => p is VehiclePawn))
      {
        vehicles += pawn.LabelShort;
      }

      Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
        "VF_LeaveVehicleBehindCaravan".Translate(vehicles), delegate
        {
          if (!CheckForErrors())
            return;

          formation.AddItemsFromTransferablesToRandomInventories(pawns);
          VehicleCaravan caravan = CaravanHelper.ExitMapAndCreateVehicleCaravan(pawns,
            Faction.OfPlayer, formCaravan.CurrentTile, formCaravan.CurrentTile,
            formation.DestinationTile,
            false);
          formation.Map.Parent.CheckRemoveMapNow();
          TaggedString taggedString = "MessageReformedCaravan".Translate();
          if (caravan.vehiclePather.Moving && caravan.vehiclePather.ArrivalAction != null)
          {
            taggedString += " " + "MessageFormedCaravan_Orders".Translate() + ": " +
              caravan.vehiclePather.ArrivalAction.Label + ".";
          }
          Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion, false);
        }));
      return true;
    }
    return false;
  }

  public static void TrySendVehicleCaravan(Dialog_FormCaravan formCaravan)
  {
    Assert.IsNotNull(formation);
    formation.RecacheTransferables();
    StringBuilder warningBuilder = new();
    (float days, float tillRot) daysWorthOfFood = formation.DaysWorthOfFood;
    if (daysWorthOfFood.days < 5f)
    {
      warningBuilder.AppendLine(daysWorthOfFood.days < 0.1f ?
        "DaysWorthOfFoodWarningDialog_NoFood".Translate().ToString() :
        "DaysWorthOfFoodWarningDialog".Translate(daysWorthOfFood.days.ToString("0.#")).Resolve());
    }
    else if (formation.MostFoodWillRotSoon)
    {
      warningBuilder.AppendLine("CaravanFoodWillRotSoonWarningDialog".Translate());
    }
    if (!Enumerable.Any(formation.pawns, pawn => CaravanUtility.IsOwner(pawn, Faction.OfPlayer) &&
      !pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled))
    {
      warningBuilder.AppendLine("CaravanIncapableOfSocial".Translate());
    }
    if (formation.ShouldShowWarningForUndesirableFood())
    {
      warningBuilder.AppendLine("DaysWorthOfFoodDietWarningDialog".Translate());
    }
    if (formation.ShouldShowWarningForMechWithoutMechanitor())
    {
      warningBuilder.AppendLine("CaravanLacksMechMechanitorWarning".Translate());
    }
    if (ModsConfig.BiotechActive)
    {
      bool header = false;
      foreach (Pawn pawn in formation.pawns)
      {
        Hediff_PsychicBond bond =
          pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) as Hediff_PsychicBond;
        if (bond == null)
          continue;
        if (!ThoughtWorker_PsychicBondProximity.NearPsychicBondedPerson(pawn, bond))
          continue;
        if (formation.pawns.Contains(bond.target))
          continue;

        if (!header)
        {
          header = true;
          warningBuilder.AppendLine("PsychicBondDistanceWillBeActive_Caravan".Translate() + ":");
        }
        warningBuilder.AppendLine(
          $"  - {pawn.NameFullColored.Resolve()} ({"Partner".Translate(bond.target).Resolve().CapitalizeFirst()})");
      }
    }
    if (warningBuilder.Length > 0 && CheckForErrors())
    {
      warningBuilder.AppendLine("CaravanAreYouSure".Translate());
      Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(warningBuilder.ToString(), delegate
      {
        if (TryFormAndSendCaravan())
        {
          formCaravan.Close(false);
        }
      }));
    }
    else if (TryFormAndSendCaravan())
    {
      SoundDefOf.Tick_High.PlayOneShotOnCamera();
      formCaravan.Close(false);
    }
    return;

    bool TryFormAndSendCaravan()
    {
      foreach (Pawn pawn in formation.pawns)
      {
        if (pawn is VehiclePawn vehicle)
        {
          vehicle.DisembarkAll();
        }
      }
      if (!CheckForErrors())
      {
        return false;
      }
      Direction8Way direction8WayFromTo =
        Find.WorldGrid.GetDirection8WayFromTo(formation.Dialog.CurrentTile, formation.StartingTile);
      if (!TryFindExitSpot(formation.pawns, true, out IntVec3 intVec))
      {
        if (!TryFindExitSpot(formation.pawns, false, out intVec))
        {
          Messages.Message(
            "CaravanCouldNotFindExitSpot".Translate(direction8WayFromTo.LabelShort()),
            MessageTypeDefOf.RejectInput, false);
          return false;
        }
        Messages.Message(
          "CaravanCouldNotFindReachableExitSpot".Translate(direction8WayFromTo.LabelShort()),
          new GlobalTargetInfo(intVec, formation.Map), MessageTypeDefOf.CautionInput, false);
        return false;
      }
      if (!TryFindRandomPackingSpot(intVec, out IntVec3 meetingPoint))
      {
        Messages.Message(
          "CaravanCouldNotFindPackingSpot".Translate(direction8WayFromTo.LabelShort()),
          new GlobalTargetInfo(intVec, formation.Map), MessageTypeDefOf.RejectInput, false);
        return false;
      }
      formation.RecacheTransferables();
      VehicleCaravanFormingUtility.StartFormingCaravan(formation.Dialog.transferables,
        meetingPoint, intVec, formation.StartingTile, formation.DestinationTile);
      Messages.Message("CaravanFormationProcessStarted".Translate(), formation.pawns[0],
        MessageTypeDefOf.PositiveEvent, false);
      return true;
    }
  }

  private static bool CheckForErrors()
  {
    Assert.IsNotNull(formation);
    if (formation.MustChooseRoute && !formation.DestinationTile.Valid)
    {
      Messages.Message("MessageMustChooseRouteFirst".Translate(), MessageTypeDefOf.RejectInput,
        false);
      return false;
    }
    if (!formation.Reform && !formation.StartingTile.Valid)
    {
      Messages.Message("MessageNoValidExitTile".Translate(), MessageTypeDefOf.RejectInput, false);
      return false;
    }
    if (!formation.pawns.Any(pawn =>
      CaravanUtility.IsOwner(pawn, Faction.OfPlayer) && !pawn.Downed))
    {
      Messages.Message(
        ModsConfig.IdeologyActive ?
          "CaravanMustHaveAtLeastOneNonSlaveColonist".Translate() :
          "CaravanMustHaveAtLeastOneColonist".Translate(),
        MessageTypeDefOf.RejectInput, false);
      return false;
    }
    if (!formation.Reform && formation.Dialog.MassUsage > formation.Dialog.MassCapacity)
    {
      formation.FlashMass();
      Messages.Message("TooBigCaravanMassUsage".Translate(), MessageTypeDefOf.RejectInput, false);
      return false;
    }

    if (!CaravanHelper.CanStartCaravan(formation.pawns))
      return false;

    if (!formation.pawns.NullOrEmpty())
    {
      foreach (VehiclePawn vehicle in formation.vehicles)
      {
        foreach (Pawn pawn in formation.pawns)
        {
          if (pawn.Spawned && pawn.IsColonist &&
            !pawn.CanReach(vehicle, PathEndMode.Touch, Danger.Deadly))
          {
            Messages.Message("CaravanPawnIsUnreachable".Translate(pawn.LabelShort, pawn), pawn,
              MessageTypeDefOf.RejectInput, historical: false);
            return false;
          }
        }
      }
    }

    if (formation.vehicles.Any(v => v.CountAssignedToVehicle() < v.PawnCountToOperate))
    {
    }
    foreach (TransferableOneWay transferable in formation.Dialog.transferables)
    {
      if (transferable.ThingDef.category == ThingCategory.Item)
      {
        int countToTransfer = transferable.CountToTransfer;
        int countAvailable = 0;
        if (countToTransfer <= 0)
          continue;

        foreach (Thing thing in transferable.things)
        {
          if (!thing.Spawned || formation.pawns.NotNullAndAny(pawn => pawn.IsColonist &&
            (pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly) ||
              CanReachUnspawned(pawn, thing))))
          {
            countAvailable += thing.stackCount;
            if (countAvailable >= countToTransfer)
              break;
          }
        }
        if (countAvailable >= countToTransfer)
          continue;

        Messages.Message(countToTransfer == 1 ?
            "CaravanItemIsUnreachableSingle".Translate(transferable.ThingDef.label) :
            "CaravanItemIsUnreachableMulti".Translate(countToTransfer, transferable.ThingDef.label),
          MessageTypeDefOf.RejectInput, false);
        return false;
      }
    }
    return true;

    static bool CanReachUnspawned(Pawn pawn, Thing thing, PathEndMode peMode = PathEndMode.Touch,
      TraverseMode mode = TraverseMode.PassDoors, Danger maxDanger = Danger.Deadly)
    {
      VehiclePawn vehicle = pawn.GetVehicle();
      if (vehicle is null)
        return false;

      return formation.Map.reachability.CanReach(vehicle.Position, thing.Position, peMode,
        new TraverseParms
        {
          maxDanger = maxDanger,
          mode = mode,
          canBashDoors = false,
          canBashFences = false,
          alwaysUseAvoidGrid = false,
          fenceBlocked = false
        });
    }
  }

  private static bool TryFindExitSpot(List<Pawn> pawns, bool reachableForEveryColonist,
    out IntVec3 spot)
  {
    CaravanExitMapUtility.GetExitMapEdges(Find.WorldGrid, formation.Dialog.CurrentTile,
      formation.StartingTile,
      out Rot4 primary, out Rot4 secondary);


    bool result = primary != Rot4.Invalid &&
      TryFindExitSpot(pawns, reachableForEveryColonist, primary, out spot) ||
      secondary != Rot4.Invalid &&
      TryFindExitSpot(pawns, reachableForEveryColonist, secondary, out spot) ||
      TryFindExitSpot(pawns, reachableForEveryColonist,
        primary.Rotated(RotationDirection.Clockwise), out spot) ||
      TryFindExitSpot(pawns, reachableForEveryColonist,
        primary.Rotated(RotationDirection.Counterclockwise), out spot);
    formation.LeadVehicle.ClampToMap(ref spot, formation.Map);
    return result;
  }

  private static bool TryFindExitSpot(List<Pawn> pawns, bool reachableForEveryColonist,
    Rot4 exitDirection, out IntVec3 spot)
  {
    spot = IntVec3.Invalid;
    if (formation.StartingTile < 0)
    {
      Log.Error("Can't find exit spot because startingTile is not set.");
      return spot.IsValid;
    }
    return TryFindExitSpot(formation.Map, pawns, reachableForEveryColonist, exitDirection,
      out spot);
  }

  private static bool TryFindExitSpot(Map map, List<Pawn> pawns,
    bool reachableForEveryColonist,
    Rot4 exitDirection, out IntVec3 spot, bool debug = false)
  {
    IntVec3 root = formation.LeadVehicle.Position;
    if (reachableForEveryColonist)
    {
      return CellFinderExtended.TryFindRandomEdgeCellWith(delegate(IntVec3 cell)
        {
          if (formation.vehicles.Any(vehicle => !ValidVehicleExitSpot(cell, vehicle, map)))
          {
            if (debug)
            {
              DebugDrawCell(root, cell, false);
            }
            return false;
          }
          foreach (Pawn pawn in pawns)
          {
            if (pawn.IsColonist && !pawn.Downed &&
              !pawn.CanReach(cell, PathEndMode.Touch, Danger.Deadly))
            {
              if (debug)
                DebugDrawCell(root, cell, false);
              return false;
            }
          }
          if (debug)
            DebugDrawCell(root, cell, true);
          return true;
        }, map, exitDirection, formation.LeadVehicle.VehicleDef, CellFinder.EdgeRoadChance_Always,
        out spot);
    }
    // Finding exit point that might not reachable for everyone
    IntVec3 cell = IntVec3.Invalid;
    int numberCanReach = -1;
    foreach (IntVec3 edgeCell in CellRect.WholeMap(map).GetEdgeCells(exitDirection).InRandomOrder())
    {
      IntVec3 paddedCell = edgeCell.PadForHitbox(map, formation.LeadVehicle);
      if (formation.vehicles.All(vehicle => ValidVehicleExitSpot(paddedCell, vehicle, map)))
      {
        int currentCount = 0;
        foreach (Pawn pawn in pawns)
        {
          if (pawn.IsColonist && !pawn.Downed &&
            pawn.CanReach(paddedCell, PathEndMode.Touch, Danger.Deadly))
          {
            currentCount++;
          }
        }
        if (currentCount > numberCanReach)
        {
          numberCanReach = currentCount;
          cell = paddedCell;
        }
        if (debug)
          DebugDrawCell(root, paddedCell, true);
      }
      else
      {
        if (debug)
          DebugDrawCell(root, paddedCell, false);
      }
    }
    spot = cell;
    return cell.IsValid;

    void DebugDrawCell(IntVec3 debugRoot, IntVec3 debugCell, bool reachable)
    {
      float colorPct = 0.5f;
      SimpleColor lineColor = SimpleColor.Green;
      if (!reachable)
      {
        colorPct = 0;
        lineColor = SimpleColor.Red;
      }
      map.debugDrawer.FlashCell(debugCell, colorPct, duration: 360);
      map.debugDrawer.FlashLine(debugCell, debugRoot, duration: 360, color: lineColor);
    }
  }

  private static bool ValidVehicleExitSpot(IntVec3 cell, VehiclePawn vehicle, Map map)
  {
    return !cell.Fogged(map) &&
      vehicle.CanReachVehicle(cell, PathEndMode.OnCell, Danger.Deadly) &&
      vehicle.DrivableRectOnCell(cell, maxPossibleSize: true);
  }

  private static bool TryFindRandomPackingSpot(IntVec3 exitSpot, out IntVec3 packingSpot)
  {
    const int SqrRadiusSmall = 15;
    const int SqrRadiusMed = 25;

    TmpPackingSpots.Clear();
    List<Thing> packingSpots =
      formation.Map.listerThings.ThingsOfDef(ThingDefOf.CaravanPackingSpot);
    if (formation.Dialog.transferables.NotNullAndAny(x =>
      x.ThingDef.category is ThingCategory.Pawn && x.AnyThing.IsBoat()))
    {
      TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors);
      foreach (Thing packingSpotThing in packingSpots)
      {
        foreach (VehiclePawn vehicle in formation.vehicles)
        {
          if (formation.Map.reachability.CanReach(vehicle.Position, packingSpotThing,
            PathEndMode.OnCell,
            traverseParms))
            TmpPackingSpots.Add(packingSpotThing);
        }
      }
      if (TmpPackingSpots.Count > 0)
      {
        Thing thing = TmpPackingSpots.RandomElement();
        TmpPackingSpots.Clear();
        packingSpot = thing.Position;
        return true;
      }

      bool found = CellFinder.TryFindRandomCellNear(formation.LeadVehicle.Position, formation.Map,
        SqrRadiusSmall, Validator, out packingSpot);
      if (!found)
      {
        found = CellFinder.TryFindRandomCellNear(formation.LeadVehicle.Position, formation.Map,
          SqrRadiusMed, ValidatorRelaxed, out packingSpot);
      }

      if (!found)
      {
        Messages.Message("VF_PackingSpotNotFound".Translate(), MessageTypeDefOf.CautionInput,
          false);
        found = RCellFinder.TryFindRandomSpotJustOutsideColony(formation.LeadVehicle.Position,
          formation.Map, out packingSpot);
      }
      return found;
    }
    TraverseParms traverseParams = TraverseParms.For(TraverseMode.PassDoors);
    foreach (Thing packingSpotThing in packingSpots)
    {
      if (formation.Map.reachability.CanReach(exitSpot, packingSpotThing, PathEndMode.OnCell,
        traverseParams))
      {
        TmpPackingSpots.Add(packingSpotThing);
      }
    }
    if (TmpPackingSpots.Count > 0)
    {
      Thing packingSpotThing = TmpPackingSpots.RandomElement();
      TmpPackingSpots.Clear();
      packingSpot = packingSpotThing.Position;
      return true;
    }
    return RCellFinder.TryFindRandomSpotJustOutsideColony(exitSpot, formation.Map, out packingSpot);

    static bool Validator(IntVec3 cell)
    {
      return cell.InBounds(formation.Map) && cell.Standable(formation.Map) &&
        NotUnderVehicle(cell) && !formation.Map.terrainGrid.TerrainAt(cell).IsWater;
    }

    static bool ValidatorRelaxed(IntVec3 cell)
    {
      return cell.InBounds(formation.Map) && cell.Standable(formation.Map);
    }

    static bool NotUnderVehicle(IntVec3 cell)
    {
      List<Thing> thingList = cell.GetThingList(formation.Map);
      if (thingList == null)
        return false;
      return thingList.Exists(ThingIsVehicle);

      static bool ThingIsVehicle(Thing thing)
      {
        return thing is VehiclePawn;
      }
    }
  }
}

/// <summary>
/// Helper class to merge accessors from duplicate code in <see cref="Dialog_FormCaravan"/> and <see cref="Dialog_SplitCaravan"/>
/// </summary>
[PublicAPI]
public class FormationInfo
{
  // Properties
  private static readonly MethodInfo MustChooseRouteGetter;
  private static readonly MethodInfo DaysWorthOfFoodGetter;
  private static readonly MethodInfo MostFoodWillRotSoonGetter;

  // Methods
  private static readonly MethodInfo FlashMassMethod;
  private static readonly MethodInfo AddItemsFromTransferablesToRandomInventoriesMethod;
  private static readonly MethodInfo ShouldShowWarningForUndesirableFoodMethod;
  private static readonly MethodInfo ShouldShowWarningForMechWithoutMechanitorMethod;
  private static readonly MethodInfo Notify_TransferablesChangedMethod;
  private static readonly MethodInfo SelectApproximateBestTravelSuppliesMethod;

  private static readonly AccessTools.FieldRef<Dialog_FormCaravan, bool>
    ChoosingRouteFieldRef;

  private static readonly AccessTools.FieldRef<Dialog_FormCaravan, bool>
    ReformFieldRef;

  private static readonly AccessTools.FieldRef<Dialog_FormCaravan, PlanetTile>
    StartingTileFieldRef;

  private static readonly AccessTools.FieldRef<Dialog_FormCaravan, PlanetTile>
    DestinationTileFieldRef;

  private static readonly AccessTools.FieldRef<Dialog_FormCaravan, bool>
    TicksToArriveDirtyFieldRef;

  private static readonly AccessTools.FieldRef<Dialog_FormCaravan, bool>
    DaysWorthOfFoodDirtyFieldRef;

  private readonly Func<bool> mustChooseRoute;
  private readonly Func<ValueTuple<float, float>> daysWorthOfFood;
  private readonly Func<bool> mostFoodWillRotSoon;

  private readonly Action flashMass;
  private readonly Func<bool> shouldShowWarningForUndesirableFood;
  private readonly Func<bool> shouldShowWarningForMechWithoutMechanitor;
  private readonly Action<List<Pawn>> addItemsFromTransferablesToRandomInventories;
  private readonly Action notifyTransferablesChanged;
  private readonly Action selectApproximateBestTravelSupplies;

  private VehiclePawn leadVehicle;
  public readonly List<Pawn> pawns = [];
  public readonly List<VehiclePawn> vehicles = [];
  public readonly List<Thing> things = [];

  private readonly Map map;
  private readonly Dialog_FormCaravan formCaravan;

  static FormationInfo()
  {
    MustChooseRouteGetter =
      AccessTools.PropertyGetter(typeof(Dialog_FormCaravan), "MustChooseRoute");
    DaysWorthOfFoodGetter =
      AccessTools.PropertyGetter(typeof(Dialog_FormCaravan), "DaysWorthOfFood");
    MostFoodWillRotSoonGetter =
      AccessTools.PropertyGetter(typeof(Dialog_FormCaravan), "MostFoodWillRotSoon");

    FlashMassMethod = AccessTools.Method(typeof(Dialog_FormCaravan),
      "FlashMass");
    ShouldShowWarningForUndesirableFoodMethod = AccessTools.Method(typeof(Dialog_FormCaravan),
      "ShouldShowWarningForUndesirableFood");
    ShouldShowWarningForMechWithoutMechanitorMethod = AccessTools.Method(typeof(Dialog_FormCaravan),
      "ShouldShowWarningForMechWithoutMechanitor");
    AddItemsFromTransferablesToRandomInventoriesMethod = AccessTools
     .Method(typeof(Dialog_FormCaravan), "AddItemsFromTransferablesToRandomInventories");
    Notify_TransferablesChangedMethod =
      AccessTools.Method(typeof(Dialog_FormCaravan), "Notify_TransferablesChanged");
    SelectApproximateBestTravelSuppliesMethod =
      AccessTools.Method(typeof(Dialog_FormCaravan), "SelectApproximateBestTravelSupplies");

    ChoosingRouteFieldRef =
      AccessTools.FieldRefAccess<bool>(typeof(Dialog_FormCaravan), "choosingRoute");
    ReformFieldRef = AccessTools.FieldRefAccess<bool>(typeof(Dialog_FormCaravan), "reform");
    StartingTileFieldRef =
      AccessTools.FieldRefAccess<PlanetTile>(typeof(Dialog_FormCaravan), "startingTile");
    DestinationTileFieldRef =
      AccessTools.FieldRefAccess<PlanetTile>(typeof(Dialog_FormCaravan), "destinationTile");
    TicksToArriveDirtyFieldRef =
      AccessTools.FieldRefAccess<bool>(typeof(Dialog_FormCaravan), "ticksToArriveDirty");
    DaysWorthOfFoodDirtyFieldRef =
      AccessTools.FieldRefAccess<bool>(typeof(Dialog_FormCaravan), "daysWorthOfFoodDirty");
  }

  public FormationInfo(Dialog_FormCaravan formCaravan, Map map)
  {
    this.formCaravan = formCaravan;
    this.map = map;

    mustChooseRoute =
      (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), formCaravan, MustChooseRouteGetter);
    daysWorthOfFood =
      (Func<ValueTuple<float, float>>)Delegate.CreateDelegate(
        typeof(Func<ValueTuple<float, float>>), formCaravan, DaysWorthOfFoodGetter);
    mostFoodWillRotSoon =
      (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), formCaravan,
        MostFoodWillRotSoonGetter);

    flashMass = (Action)Delegate.CreateDelegate(typeof(Action), formCaravan, FlashMassMethod);
    shouldShowWarningForUndesirableFood =
      (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>),
        formCaravan, ShouldShowWarningForUndesirableFoodMethod);
    shouldShowWarningForMechWithoutMechanitor =
      (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>),
        formCaravan, ShouldShowWarningForMechWithoutMechanitorMethod);
    addItemsFromTransferablesToRandomInventories = (Action<List<Pawn>>)Delegate.CreateDelegate(
      typeof(Action<List<Pawn>>),
      formCaravan, AddItemsFromTransferablesToRandomInventoriesMethod);
    notifyTransferablesChanged =
      (Action)Delegate.CreateDelegate(typeof(Action), formCaravan,
        Notify_TransferablesChangedMethod);
    selectApproximateBestTravelSupplies =
      (Action)Delegate.CreateDelegate(typeof(Action), formCaravan,
        SelectApproximateBestTravelSuppliesMethod);
  }

  public Dialog_FormCaravan Dialog => formCaravan;

  public Map Map => map;

  public VehiclePawn LeadVehicle => leadVehicle;

  public bool Reform => ReformFieldRef.Invoke(formCaravan);

  public bool ChoosingRoute
  {
    get { return ChoosingRouteFieldRef.Invoke(formCaravan); }
    set { ChoosingRouteFieldRef.Invoke(formCaravan) = value; }
  }

  public PlanetTile StartingTile
  {
    get { return StartingTileFieldRef.Invoke(formCaravan); }
    set { StartingTileFieldRef.Invoke(formCaravan) = value; }
  }

  public PlanetTile DestinationTile
  {
    get { return DestinationTileFieldRef.Invoke(formCaravan); }
    set { DestinationTileFieldRef.Invoke(formCaravan) = value; }
  }

  public bool TicksToArriveDirty
  {
    get { return TicksToArriveDirtyFieldRef.Invoke(formCaravan); }
    set { TicksToArriveDirtyFieldRef.Invoke(formCaravan) = value; }
  }

  public bool DaysWorthOfFoodDirty
  {
    get { return DaysWorthOfFoodDirtyFieldRef.Invoke(formCaravan); }
    set { DaysWorthOfFoodDirtyFieldRef.Invoke(formCaravan) = value; }
  }

  public bool MustChooseRoute => mustChooseRoute();

  public (float days, float tillRot) DaysWorthOfFood => daysWorthOfFood();

  public bool MostFoodWillRotSoon => mostFoodWillRotSoon();

  public void FlashMass()
  {
    flashMass();
  }

  public bool ShouldShowWarningForUndesirableFood()
  {
    return shouldShowWarningForUndesirableFood();
  }

  public bool ShouldShowWarningForMechWithoutMechanitor()
  {
    return shouldShowWarningForMechWithoutMechanitor();
  }

  public void AddItemsFromTransferablesToRandomInventories(List<Pawn> pawns)
  {
    addItemsFromTransferablesToRandomInventories(pawns);
  }

  public void SelectApproximateBestTravelSupplies()
  {
    selectApproximateBestTravelSupplies();
  }

  public void NotifyTransferablesChanged()
  {
    notifyTransferablesChanged();
  }

  internal void RecacheTransferables()
  {
    leadVehicle = null;
    int largestMagnitude = -1;
    foreach (TransferableOneWay transferable in formCaravan.transferables)
    {
      if (transferable.AnyThing is null || transferable.CountToTransfer == 0)
        continue;

      foreach (Thing thing in transferable.things)
      {
        switch (thing)
        {
          case VehiclePawn vehicle:
            if (vehicle.def.size.Magnitude > largestMagnitude)
            {
              leadVehicle = vehicle;
              largestMagnitude = vehicle.def.size.MagnitudeManhattan;
            }
            vehicles.Add(vehicle);
          break;
          case Pawn pawn:
            pawns.Add(pawn);
          break;
          default:
            things.Add(thing);
          break;
        }
      }
    }
  }
}