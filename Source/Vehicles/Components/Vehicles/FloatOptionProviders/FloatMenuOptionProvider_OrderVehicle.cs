using System.Collections.Generic;
using CoreLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Verse;
using Verse.AI;
using static Vehicles.Config.FeatureFlags;

namespace Vehicles;

public class FloatMenuOptionProvider_OrderVehicle : FloatMenuOptionProvider_Vehicle
{
  private static readonly List<VehiclePawn> MultiSelectVehicles = [];

  protected override bool SelectedVehicleValid(VehiclePawn vehicle, FloatMenuContext context)
  {
    return vehicle.CanMoveFinal && vehicle.ignition.Drafted;
  }

  protected override FloatMenuOption GetSingleOption(FloatMenuContext context)
  {
    // TODO Raiders vehicle.Faction != Faction.OfPlayer needed?

    FloatMenuOption option;
    IntVec3 clickCell = context.ClickedCell;
    if (context.IsMultiselect)
    {
      using ClearOnDispose<VehiclePawn> slr = new(MultiSelectVehicles);
      foreach (Pawn pawn in context.ValidSelectedPawns)
      {
        if (pawn is VehiclePawn vehicle && VehicleCanGoto(vehicle, clickCell).Accepted)
          MultiSelectVehicles.Add(vehicle);
      }
      if (MultiSelectVehicles.Count == 0)
        return null;

      option = new FloatMenuOption("GoHere".Translate(),
        delegate { VehicleOrientationController.StartOrienting(MultiSelectVehicles, clickCell, clickCell); },
        MenuOptionPriority.GoHere);
    }
    else
    {
      Pawn pawn = context.FirstSelectedPawn;
      if (pawn is not VehiclePawn vehicle)
        return null;

      foreach (ThingComp comp in vehicle.AllComps)
      {
        if (comp is VehicleComp vehicleComp)
        {
          AcceptanceReport compReport = vehicleComp.CanMove(context);
          if (!compReport.Accepted)
          {
            Messages.Message(compReport.Reason, MessageTypeDefOf.RejectInput);
            return null;
          }
        }
      }
      bool canReach = PathingHelper.TryFindNearestStandableCell(vehicle, clickCell, out IntVec3 result);
      AcceptanceReport gotoReport = canReach ? VehicleCanGoto(vehicle, result) : "VF_CannotMoveToCell".Translate(vehicle.LabelCap);
      if (!gotoReport.Accepted)
        return new FloatMenuOption("VF_CannotMoveToCell".Translate(vehicle.LabelCap), null);

      option = new FloatMenuOption("GoHere".Translate(),
        delegate { VehicleOrientationController.StartOrienting(vehicle, result, clickCell); },
        MenuOptionPriority.GoHere);
    }

    option.isGoto = true;
    option.autoTakeable = true;
    option.autoTakeablePriority = 10f;
    return option;
  }

  // TODO 1.7 - Merge with TryFindNearestStandableCell check above
  public static AcceptanceReport VehicleCanGoto(VehiclePawn vehicle, IntVec3 gotoLoc)
  {
    return vehicle.CanReachVehicle(gotoLoc, PathEndMode.OnCell, Danger.Deadly) ?
      AcceptanceReport.WasAccepted :
      "VF_CannotMoveToCell".Translate(vehicle.LabelCap);
  }

  internal static void PawnGotoAction(IntVec3 clickCell, VehiclePawn vehicle, IntVec3 gotoLoc, Rot8 rot)
  {
    if (vehicle.Position == gotoLoc)
    {
      vehicle.FullRotation = rot;
      if (vehicle.CurJobDef == JobDefOf.Goto)
      {
        vehicle.jobs.EndCurrentJob(JobCondition.Succeeded);
      }
      FleckMaker.Static(gotoLoc, vehicle.Map, FleckDefOf.FeedbackGoto);
      return;
    }

    if (vehicle.CurJobDef == JobDefOf.Goto && vehicle.CurJob.targetA.Cell == gotoLoc)
    {
      FleckMaker.Static(gotoLoc, vehicle.Map, FleckDefOf.FeedbackGoto);
      return;
    }

    bool isOnEdge = CellRect.WholeMap(vehicle.Map).IsOnEdge(clickCell, 3);
    bool exitCell = vehicle.Map.exitMapGrid.IsExitCell(clickCell);
    bool vehicleCellsOverlapExit = vehicle.InhabitedCellsProjected(clickCell, rot)
      .NotNullAndAny(cell => cell.InBounds(vehicle.Map) &&
                             vehicle.Map.exitMapGrid.IsExitCell(cell));
    bool exitMapOnArrival = exitCell || vehicleCellsOverlapExit;
    if (!exitMapOnArrival && !vehicle.Map.IsPlayerHome && !vehicle.Map.exitMapGrid.MapUsesExitGrid &&
        isOnEdge && vehicle.Map.Parent.GetComponent<FormCaravanComp>() is { } formCaravanComp &&
        MessagesRepeatAvoider.MessageShowAllowed(
          $"MessagePlayerTriedToLeaveMapViaExitGrid-{vehicle.Map.uniqueID}", 60f))
    {
      string text = formCaravanComp.CanFormOrReformCaravanNow ?
        "MessagePlayerTriedToLeaveMapViaExitGrid_CanReform".Translate() :
        "MessagePlayerTriedToLeaveMapViaExitGrid_CantReform".Translate();
      Messages.Message(text, vehicle.Map.Parent, MessageTypeDefOf.RejectInput, false);
    }

    if (!IsFeatureEnabled(PathFinderV2))
    {
      Job job = JobMaker.MakeJob(JobDefOf.Goto, gotoLoc);
      vehicle.jobs.TryTakeOrderedJob(job, JobTag.Misc);
      return;
    }

    bool success = vehicle.vehiclePather.TryOrderMoveTo(new PathOrderData
    {
      destination = gotoLoc,
      endRotation = rot,
      exitMapOnArrival = exitMapOnArrival
    });
    if (success)
    {
      FleckMaker.Static(gotoLoc, vehicle.Map, FleckDefOf.FeedbackGoto);
    }
  }
}