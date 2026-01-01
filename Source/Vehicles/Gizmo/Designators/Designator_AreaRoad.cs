using System.Collections.Generic;
using RimWorld;
using SmashTools;
using SmashTools.Burst;
using UnityEngine;
using Verse;

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
    const int RoadCostShift = 1;
    const int RoadAvoidCost = 250;

    int index = CellIndicesUtility.CellToIndex(cell, Map.Size.x);
    if (mode == DesignateMode.Add)
    {
      switch (roadType)
      {
        case RoadType.Prioritize:
          Map.areaManager.Get<Area_Road>()[cell] = true;
          Map.areaManager.Get<Area_RoadAvoidal>()[cell] = false;
          ModifierGrid?[index] = new Modifier
          {
            type = ModifierType.ShiftRight,
            value = RoadCostShift
          };
          break;
        case RoadType.Avoid:
          Map.areaManager.Get<Area_Road>()[cell] = false;
          Map.areaManager.Get<Area_RoadAvoidal>()[cell] = true;
          ModifierGrid?[index] = new Modifier
          {
            type = ModifierType.Add,
            value = RoadAvoidCost
          };
          break;
      }
      return;
    }
    Map.areaManager.Get<Area_Road>()[cell] = false;
    Map.areaManager.Get<Area_RoadAvoidal>()[cell] = false;
    ModifierGrid?[index] = new Modifier { type = ModifierType.None };
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