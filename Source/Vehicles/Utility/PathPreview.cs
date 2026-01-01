using System;
using System.Threading;
using RimWorld;
using SmashTools;
using SmashTools.Targeting;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Vehicles.Editor;

internal sealed class PathPreview : ITargeter, IDisposable
{
  private readonly Texture2D mouseAttachment =
    ContentFinder<Texture2D>.Get("UI/Overlays/WaypointMouseAttachment");

  private readonly VehiclePawn vehicle;
  private readonly VehiclePathingSystem pathingSystem;
  private readonly VehiclePathFinder pathFinder;
  private readonly VehicleReachability reachability;

  private readonly TraverseParms parms;

  private IntVec3 start = IntVec3.Invalid;
  private IntVec3 dest = IntVec3.Invalid;

  private VehiclePath path;

  public PathPreview(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
    this.pathingSystem = vehicle.Map.GetCachedMapComponent<VehiclePathingSystem>();

    VehiclePathingSystem.VehiclePathData pathData = pathingSystem[vehicle.VehicleDef];
    pathFinder = pathData.VehiclePathFinder;
    reachability = pathData.VehicleReachability;

    parms = TraverseParms.For(vehicle, alwaysUseAvoidGrid: true,
      mode: TraverseMode.PassAllDestroyablePlayerOwnedThings);
  }

  void ITargeter.OnStart()
  {
    start = IntVec3.Invalid;
    dest = IntVec3.Invalid;
  }

  void ITargeter.OnStop()
  {
    Dispose();
  }

  void ITargeter.OnGUI()
  {
    if (KeyBindingDefOf.Cancel.KeyDownEvent)
    {
      this.Stop();
      Event.current.Use();
      return;
    }
    GenUI.DrawMouseAttachment(mouseAttachment);

    if (Event.current is not { type: EventType.MouseDown })
      return;

    switch (Event.current.button)
    {
      case 0:
        IntVec3 mouseCell = UI.MouseCell();
        if (!IsValidLocation(mouseCell))
        {
          SoundDefOf.ClickReject.PlayOneShotOnCamera();
        }
        PrimaryMouseDown(mouseCell);
        break;
      case 1:
        SecondaryMouseDown();
        break;
    }
  }

  private void PrimaryMouseDown(IntVec3 cell)
  {
    if (!start.IsValid)
    {
      start = cell;
    }
    else
    {
      if (!reachability.CanReachVehicle(start, cell, PathEndMode.OnCell, parms))
      {
        SoundDefOf.ClickReject.PlayOneShotOnCamera();
      }
      else
      {
        dest = cell;
        CalculatePath();
        SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
      }
    }
    Event.current.Use();
  }

  private void SecondaryMouseDown()
  {
    path?.Dispose();
    path = null;
    if (dest.IsValid)
    {
      dest = IntVec3.Invalid;
    }
    else
    {
      start = IntVec3.Invalid;
    }
    Event.current.Use();
    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
  }

  private bool IsValidLocation(IntVec3 cell)
  {
    if (!cell.InBounds(vehicle.Map))
      return false;
    if (!cell.Walkable(vehicle.VehicleDef, pathingSystem))
      return false;

    return true;
  }

  void ITargeter.Update()
  {
    if (start.IsValid)
    {
      GenDraw.DrawRadiusRing(start, 1);
    }
    if (dest.IsValid)
    {
      GenDraw.DrawRadiusRing(dest, 1);
    }
    if (path != null)
    {
      path.DrawPath(vehicle);
      if (GenTicks.TicksGame % 15 == 0)
      {
        foreach (IntVec3 cell in path.Nodes)
        {
          vehicle.Map.debugDrawer.FlashCell(cell, duration: 15);
        }
      }
    }
  }

  private void CalculatePath()
  {
    path = pathFinder.FindPath(start, dest, parms, CancellationToken.None);
  }

  public void Dispose()
  {
    path?.Dispose();
    path = null;
  }
}