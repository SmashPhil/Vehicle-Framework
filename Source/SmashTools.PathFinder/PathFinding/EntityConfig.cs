using JetBrains.Annotations;
using Unity.Collections;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public struct EntityConfig
  {
    private const int DefaultOverlapCost = 250;

    /// <summary>
    /// ID of entity requesting a path.
    /// </summary>
    /// <remarks>
    /// This id will be ignored while adding additional overlap costs.
    /// The requester should not care if they overlap with themselves.
    /// </remarks>
    public int entityId;

    /// <summary>
    /// Cost added to path grid if another entity overlaps at a node.
    /// </summary>
    public int costToOverlap;

    /// <summary>
    /// Grid of ids marking positions reserved by other entities.
    /// </summary>
    public NativeArray<int>.ReadOnly entityGrid;

    public EntityConfig(int entityId, NativeArray<int>.ReadOnly entityGrid)
    {
      this.entityId = entityId;
      this.entityGrid = entityGrid;
      costToOverlap = DefaultOverlapCost;
    }

    public EntityConfig(int entityId, NativeArray<int>.ReadOnly entityGrid, int costToOverlap)
    {
      this.entityId = entityId;
      this.entityGrid = entityGrid;
      this.costToOverlap = costToOverlap;
    }

    /// <summary>
    /// EntityConfig should be snapshotted onto the path grid before pathfinding.
    /// </summary>
    public bool IsValid => entityGrid.IsCreated;
  }
}