
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  /// <summary>
  /// Path parameters for pathfinding job.
  /// </summary>
  [PublicAPI]
  public struct PathRequest
  {
    /// <summary>
    /// Starting point of path
    /// </summary>
    public int3 start;

    /// <summary>
    /// End point of path
    /// </summary>
    public int3 end;

    /// <summary>
    /// Initial rotation of entity assuming multi-cell hitbox
    /// </summary>
    public int rotation;

    /// <summary>
    /// Configure turn costs for penalizing jagged paths.
    /// </summary>
    public TurnData turnData;

    /// <summary>
    /// Overwrite path costs with the ability to bypass reachability restrictions.
    /// </summary>
    /// <remarks>
    /// If a cost is &gt; 0, it will overwrite the cost of the path grid with no bounds check on whether
    /// it was originally passable / impassable.
    /// <para/>
    /// Specifying this field will force the pathfinder snapshot all path grid costs.
    /// </remarks>
    public PathGridMask mask;

    /// <summary>
    /// Apply conditional scale factor to gCost if bit is set at index.
    /// </summary>
    public PathGridScalar.Data scalar;

    /// <summary>
    /// Configure cell-specific post-snapshot modifiers.
    /// </summary>
    /// <remarks>
    /// Modifiers apply right before the path search starts. Reserve this for final changes to costs.
    /// <para/>
    /// Specifying this field will force the pathfinder snapshot all path grid costs.
    /// </remarks>
    public ModifierGrid modifiers;

    /// <summary>
    /// Configure heuristic costs with cell-specific settings and configurable weights.
    /// </summary>
    public HeuristicGrid.Data heuristic;

    /// <summary>
    /// Apply single-shot path offsets for this request
    /// </summary>
    /// <remarks>
    /// Specifying this field will force the pathfinder snapshot all path grid costs.
    /// </remarks>
    public List<PathOffsetGrid> costOffsets;

    /// <summary>
    /// Apply single-shot path offsets with configurable cost
    /// </summary>
    /// <remarks>
    /// Specifying this field will force the pathfinder snapshot all path grid costs.
    /// </remarks>
    public List<BitOffset> bitOffsets;

    /// <summary>
    /// Configuration settings related to the cost of overlapping other entities.
    /// </summary>
    public EntityConfig entityCost;
  }
}