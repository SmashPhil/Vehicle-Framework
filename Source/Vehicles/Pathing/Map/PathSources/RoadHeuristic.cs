using JetBrains.Annotations;
using SmashTools.PathFinder;
using Unity.Mathematics;
using Verse;

namespace Vehicles;

public sealed class RoadHeuristic : HeuristicGrid
{
  private readonly Map map;
  private readonly PathFinderManager manager;

  public RoadHeuristic(Map map, [NotNull] PathFinderManager manager)
    : base(new int2(map.Size.x, map.Size.z), manager)
  {
    this.map = map;
    this.manager = manager;
  }
}
