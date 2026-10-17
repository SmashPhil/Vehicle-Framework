using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  internal readonly struct NormalHeuristic : IHeuristicProvider
  {
    public int4 GetHeuristic(int4 x, int4 y, int xEnd, int yEnd)
    {
      int4 dx = math.abs(x - xEnd);
      int4 dy = math.abs(y - yEnd);
      int4 max = math.max(dx, dy);
      int4 min = math.min(dx, dy);
      return PathFinder.CostCardinal * max + (PathFinder.CostDiagonal - PathFinder.CostCardinal) * min;
    }
  }
}