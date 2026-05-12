using System;
using System.Collections.Generic;
using CoreLib.PathFinding;
using LudeonTK;
using RimWorld;
using SmashTools;
using SmashTools.Targeting;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.Sound;
using Path = CoreLib.PathFinding.Path;

namespace Vehicles.Editor;

internal sealed class PathPreview : ITargeter
{
  private readonly Texture2D mouseAttachment =
    ContentFinder<Texture2D>.Get("UI/Overlays/WaypointMouseAttachment");

  private readonly VehicleDef vehicleDef;
  private readonly Map map;
  private readonly PathSettings settings;
  private readonly Type type;

  private readonly IPathingManager pathingManager;
  private readonly IPathFinder<PathSettings> pathFinder;
  private readonly VehicleReachability reachability;
  private ChunkSearch chunkSearch;

  private IntVec3 start = IntVec3.Invalid;
  private IntVec3 dest = IntVec3.Invalid;
  private IntVec3 shootPosition = IntVec3.Invalid;
  private LocalTargetInfo target = LocalTargetInfo.Invalid;

  private Path path;
  private readonly List<VehicleRegion> regions = [];

  public enum Type { PathFinding, BreachPath, BreachTarget };

  private PathPreview(IPathFinder<PathSettings> pathFinder, Map map, Type type, in PathSettings settings)
  {
    this.pathFinder = pathFinder;
    this.vehicleDef = settings.vehicleDef;
    this.map = map;
    this.type = type;
    this.settings = settings;

    reachability = map.GetCachedMapComponent<VehiclePathingSystem>()[settings.vehicleDef].VehicleReachability;
  }

  public static PathPreview Instance { get; private set; }

  public static void Start(VehicleDef vehicleDef, Map map, Type type, in PathSettings settings)
  {
    Instance?.Stop();
    VehiclePathingSystem pathing = map.GetCachedMapComponent<VehiclePathingSystem>();
    Instance = new PathPreview(pathing.PathFinder, map, type, settings)
    {
      chunkSearch = new ChunkSearch(pathing, vehicleDef, cache: null)
    };
    pathing.RequestGridsFor(vehicleDef, DeferredGridGeneration.Urgency.Urgent);
    Instance.Start();
  }

  void ITargeter.OnStart()
  {
    start = IntVec3.Invalid;
    dest = IntVec3.Invalid;
  }

  void ITargeter.OnStop()
  {
    Instance = null;
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
      if (!reachability.CanReachVehicle(start, cell, PathEndMode.OnCell,
            TraverseParms.For(TraverseMode.ByPawn)))
      {
        SoundDefOf.ClickReject.PlayOneShotOnCamera();
      }

      dest = cell;
      switch (type)
      {
        case Type.PathFinding:
          CalculatePath();
          break;
        case Type.BreachPath:
          CalculateRegionPath();
          break;
        case Type.BreachTarget:
          CalculatePath();
          if (path != null)
          {
            Thing building = PathingHelper.FirstBlockingBuilding(vehicleDef, map, path);
            if (building != null)
            {
              var request = new CombatPositionFinder.Request
              {
                vehicleDef = vehicleDef,
                map = map,
                position = path.FirstNode.ToIntVec3(),
                target = target
              };
              if (CombatPositionFinder.TryFindShootPosition(request, out IntVec3 firingPos))
              {
                target = building;
                shootPosition = firingPos;
              }
              else
              {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
              }
            }
          }
          break;
        default:
          throw new NotImplementedException(type.ToString());
      }
      SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
    }
    Event.current.Use();
  }

  private void SecondaryMouseDown()
  {
    path = null;
    regions.Clear();
    if (dest.IsValid)
    {
      dest = IntVec3.Invalid;
      shootPosition = IntVec3.Invalid;
      target = LocalTargetInfo.Invalid;
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
    if (!cell.InBounds(map))
      return false;
    if (!cell.Walkable(vehicleDef, map))
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
      path.DrawPath(null);
      if (GenTicks.TicksGame % 15 == 0)
      {
        foreach (Path.Node node in path.Nodes)
        {
          map.debugDrawer.FlashCell(node.ToIntVec3(), duration: 15);
        }
      }
    }
    else if (regions.Count > 0)
    {
      foreach (VehicleRegion region in regions)
      {
        region.DebugDraw(DebugRegionType.Regions);
      }
    }
  }

  private void CalculatePath()
  {
    path = pathFinder.FindPath(start.ToPathNode(), dest.ToPathNode(), settings);
  }

  private void CalculateRegionPath()
  {
    ChunkSearch.Data data = new()
    {
      start = start,
      destination = dest,
      traverseParms = TraverseParms.For(TraverseMode.PassAllDestroyableThings)
    };

    if (!chunkSearch.CanReach(data))
    {
      SoundDefOf.ClickReject.PlayOneShotOnCamera();
      return;
    }

    regions.Clear();
    VehicleRegionGrid regionGrid = pathingManager.GetRegionGridManager(vehicleDef)[RegionGridType.Breach];
    foreach (VehicleRegion region in regionGrid.AllRegionsNoRebuildInvalidAllowed)
    {
      if (region.reachedIndex == chunkSearch.ReachedIndex)
      {
        regions.Add(region);
      }
    }
  }

  [DebugAction(VehicleHarmony.VehiclesLabel, "Preview Path", actionType = DebugActionType.Action,
    allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void PathPreviewer()
  {
    if (Find.CurrentMap == null)
    {
      Messages.Message("Trying to preview path with no current map active.", MessageTypeDefOf.RejectInput,
        historical: false);
      return;
    }
    CameraJumper.TryHideWorld();
    List<DebugMenuOption> options = [];
    foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
    {
      options.Add(new DebugMenuOption(vehicleDef.LabelCap, DebugMenuOptionMode.Action, delegate
      {
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(GetPathPreviewTypes(vehicleDef)));
      }));
    }
    Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
    return;

    static IEnumerable<DebugMenuOption> GetPathPreviewTypes(VehicleDef vehicleDef)
    {
      Map map = Find.CurrentMap;
      Assert.IsNotNull(map);
      
      yield return new DebugMenuOption("Normal", DebugMenuOptionMode.Action, delegate
      {
        Start(vehicleDef, map, Type.PathFinding, PathSettings.For(vehicleDef));
      });
      yield return new DebugMenuOption("Breach Path", DebugMenuOptionMode.Action, delegate
      {
        Start(vehicleDef, map, Type.BreachPath, PathSettings.For(vehicleDef) with
        {
          search = PathSettings.GridSetting.BreachWalls | PathSettings.GridSetting.UseAvoidGrid
        });
      });
      yield return new DebugMenuOption("Breach Targeting", DebugMenuOptionMode.Action, delegate
      {
        Start(vehicleDef, map, Type.BreachTarget, PathSettings.For(vehicleDef) with
        {
          search = PathSettings.GridSetting.BreachWalls | PathSettings.GridSetting.UseAvoidGrid
        });
      });
    }
  }
}