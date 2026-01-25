using System.Collections.Generic;
using RimWorld;
using SmashTools;
using SmashTools.Burst;
using UnityEngine;
using Verse;

namespace Vehicles;

public abstract class Designator_AreaRoad : Designator_Cells
{
  protected const string RoadId = "VehicleRoads";
  protected const int RoadDiscountCost = 50;
  protected const int RoadAvoidCost = 250;

  private readonly DesignateMode mode;
  private static RoadType roadType = RoadType.Prioritize;

  protected Designator_AreaRoad(DesignateMode mode)
  {
    this.mode = mode;
    useMouseIcon = true;
  }

  public override bool DragDrawMeasurements => true;

  public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;

  protected ModifierGrid ModifierGrid => Map.GetCachedMapComponent<VehiclePathingSystem>().ModifierGrid;

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
      ModifierGrid.AddModifier(RoadId, index, new Modifier
      {
        type = ModifierType.Subtract,
        value = RoadDiscountCost
      }, ModifierPriority.Low);
    }
  }

  private void SetAvoid(int index)
  {
    Area_RoadAvoidal area = Map.areaManager.Get<Area_RoadAvoidal>();
    if (!area[index])
    {
      area[index] = true;
      ModifierGrid.AddModifier(RoadId, index, new Modifier
      {
        type = ModifierType.Add,
        value = RoadAvoidCost
      }, ModifierPriority.Low);
    }
  }

  private void RemoveRoad(int index)
  {
    Area_Road area = Map.areaManager.Get<Area_Road>();
    if (area[index])
    {
      area[index] = false;
      ModifierGrid.RemoveModifier(RoadId, index);
    }
  }

  private void RemoveAvoid(int index)
  {
    Area_RoadAvoidal area = Map.areaManager.Get<Area_RoadAvoidal>();
    if (area[index])
    {
      area[index] = false;
      ModifierGrid.RemoveModifier(RoadId, index);
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