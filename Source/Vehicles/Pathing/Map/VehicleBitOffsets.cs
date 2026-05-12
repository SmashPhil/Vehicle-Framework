using JetBrains.Annotations;
using SmashTools.Burst;
using Unity.Mathematics;
using Verse;

namespace Vehicles;

[PublicAPI, MeansImplicitUse]
public abstract class VehicleBitOffsets : PathBoolGrid, IPathCostGrid
{
  protected readonly Map map;
  protected readonly PathFinderManager manager;

  protected VehicleBitOffsets(Map map, [NotNull] PathFinderManager manager)
    : base(new int2(map.Size.x, map.Size.z), manager)
  {
    this.map = map;
    this.manager = manager;
  }

  int IPathCostGrid.Index { get => Index; set => Index = value; }

  public int Index { get; private set; }

  protected abstract Cost CostFor(in PathSettings settings);

  internal BitOffset OffsetFor(in PathSettings settings)
  {
    return new BitOffset
    {
      boolGrid = this,
      cost = CostFor(settings)
    };
  }

  public abstract bool ShouldApplyFor(in PathSettings settings);

  public virtual void Update(int index)
  {
  }

  protected void SetDirty(IntVec3 cell)
  {
    manager.SetDirty(this, cell);
  }

  protected void SetDirty(CellRect rect)
  {
    foreach (IntVec3 cell in rect)
    {
      SetDirty(cell);
    }
  }
}
