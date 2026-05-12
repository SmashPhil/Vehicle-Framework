using SmashTools.Burst;
using Unity.Mathematics;
using UnityEngine;
using Verse;

namespace Vehicles;

public sealed class BuildingGrid : PathGridMask, IPathCostGrid
{
  private readonly Map map;
  private readonly PathFinderManager manager;

  public BuildingGrid(Map map, PathFinderManager manager) : base(new int2(map.Size.x, map.Size.z), manager)
  {
    this.map = map;
    this.manager = manager;
    map.events.BuildingSpawned += UpdateBuilding;
    map.events.BuildingDespawned += UpdateBuilding;
    map.events.BuildingHitPointsChanged += UpdateBuilding;
  }

  int IPathCostGrid.Index { get => Index; set => Index = value; }

  public int Index { get; private set; }

  public bool ShouldApplyFor(in PathSettings settings)
  {
    return (settings.search & PathSettings.GridSetting.BreachWalls) != 0;
  }

  public void Update(int index)
  {
    Building building = map.edificeGrid[index];
    short score = 0;
    if (building != null)
    {
      score = (short)Mathf.Clamp(building.HitPoints << 1, 0, short.MaxValue);
    }
    this[index] = score;
  }

  private void UpdateBuilding(Building building)
  {
    foreach (IntVec3 cell in building.OccupiedRect())
    {
      SetDirty(cell);
    }
  }

  private void SetDirty(IntVec3 cell)
  {
    manager.SetDirty(this, cell);
  }
}
