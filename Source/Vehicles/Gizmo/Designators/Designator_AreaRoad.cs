using System.Collections.Generic;
using RimWorld;
using SmashTools;
using SmashTools.Burst;
using UnityEngine;
using Verse;
using static Vehicles.Config.FeatureFlags;

namespace Vehicles;

public abstract class Designator_AreaRoad : Designator_Cells
{
  private readonly DesignateMode mode;
  private static RoadType roadType = RoadType.Prioritize;

  protected Designator_AreaRoad(DesignateMode mode)
  {
    this.mode = mode;
    useMouseIcon = true;
  }

  public override bool DragDrawMeasurements => true;

  public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;

  private RoadPreferGrid RoadPreferGrid => Map.GetCachedMapComponent<VehiclePathingSystem>()
    .PathFinderManager.GetSource<RoadPreferGrid>();

  private RoadAvoidGrid RoadAvoidGrid => Map.GetCachedMapComponent<VehiclePathingSystem>()
    .PathFinderManager.GetSource<RoadAvoidGrid>();

  private RoadHeuristic HeuristicGrid => Map.GetCachedMapComponent<VehiclePathingSystem>()
    .PathFinderManager.HeuristicGrid;

  private PathGridScalar ScalarGrid => Map.GetCachedMapComponent<VehiclePathingSystem>()
    .PathFinderManager.ScalarGrid;

  public override void ProcessInput(Event ev)
  {
    if (!CheckCanInteract())
    {
      return;
    }
    if (mode == DesignateMode.Add)
    {
      List<FloatMenuOption> options =
      [
        RoadTypeOption("VF_RoadType_Prioritize".Translate(), RoadType.Prioritize),
        RoadTypeOption("VF_RoadType_Avoid".Translate(), RoadType.Avoid)
      ];
      Find.WindowStack.Add(new FloatMenu(options));
      return;
    }
    base.ProcessInput(ev);
    return;

    FloatMenuOption RoadTypeOption(string label, RoadType type)
    {
      return new FloatMenuOption(label, delegate
      {
        roadType = type;
        base.ProcessInput(ev);
      }, priority: MenuOptionPriority.Low);
    }
  }

  public override void DesignateSingleCell(IntVec3 cell)
  {
    int index = CellIndicesUtility.CellToIndex(cell, Map.Size.x);
    if (mode == DesignateMode.Add)
    {
      if (roadType == RoadType.Prioritize)
      {
        RemoveAvoid(index);
        SetRoad(index);
      }
      else if (roadType == RoadType.Avoid)
      {
        RemoveRoad(index);
        SetAvoid(index);
      }
    }
    else if (mode == DesignateMode.Remove)
    {
      RemoveRoad(index);
      RemoveAvoid(index);
    }
  }

  private void SetRoad(int index)
  {
    Area_Road area = Map.areaManager.Get<Area_Road>();
    if (!area[index])
    {
      area[index] = true;
      if (IsFeatureEnabled(PathFinderV2))
      {
        RoadPreferGrid[index] = true;
        HeuristicGrid[index] = HeuristicWeight.Prefer;
        ScalarGrid[index] = true;
      }
    }
  }

  private void SetAvoid(int index)
  {
    Area_RoadAvoidal area = Map.areaManager.Get<Area_RoadAvoidal>();
    if (!area[index])
    {
      area[index] = true;
      if (IsFeatureEnabled(PathFinderV2))
      {
        RoadAvoidGrid[index] = true;
        HeuristicGrid[index] = HeuristicWeight.Avoid;
      }
    }
  }

  private void RemoveRoad(int index)
  {
    Area_Road area = Map.areaManager.Get<Area_Road>();
    if (area[index])
    {
      area[index] = false;
      if (IsFeatureEnabled(PathFinderV2))
      {
        RoadPreferGrid[index] = false;
        HeuristicGrid[index] = HeuristicWeight.Normal;
        ScalarGrid[index] = false;
      }
    }
  }

  private void RemoveAvoid(int index)
  {
    Area_RoadAvoidal area = Map.areaManager.Get<Area_RoadAvoidal>();
    if (area[index])
    {
      area[index] = false;
      if (IsFeatureEnabled(PathFinderV2))
      {
        RoadAvoidGrid[index] = false;
        HeuristicGrid[index] = HeuristicWeight.Normal;
      }
    }
  }

  public override AcceptanceReport CanDesignateCell(IntVec3 cell)
  {
    if (!cell.InBounds(Map))
    {
      return false;
    }
    bool road = Map.areaManager.Get<Area_Road>()[cell];
    bool avoidal = Map.areaManager.Get<Area_RoadAvoidal>()[cell];
    if (mode == DesignateMode.Add)
    {
      return roadType switch
      {
        RoadType.Prioritize => !road,
        RoadType.Avoid => !avoidal,
        _ => true,
      };
    }
    return road || avoidal;
  }

  public override void SelectedUpdate()
  {
    GenUI.RenderMouseoverBracket();
    Map.areaManager.Get<Area_Road>().MarkForDraw();
    Map.areaManager.Get<Area_RoadAvoidal>().MarkForDraw();
  }

  public enum RoadType : byte
  {
    None,
    Prioritize,
    Avoid
  }
}