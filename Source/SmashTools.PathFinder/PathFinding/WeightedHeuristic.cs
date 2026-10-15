using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  internal struct WeightedHeuristic : IHeuristicProvider
  {
    private readonly Heuristic settings;

    private readonly int width;
    private readonly int height;

    [ReadOnly]
    private NativeArray<HeuristicWeight>.ReadOnly weights;

    public WeightedHeuristic(int2 mapSize, Heuristic settings, in NativeArray<HeuristicWeight>.ReadOnly weights)
    {
      width = mapSize.x;
      height = mapSize.y;
      this.settings = settings;
      this.weights = weights;
    }

    public int4 GetHeuristic(int4 x, int4 y, int xEnd, int yEnd)
    {
      int4 dx = math.abs(x - xEnd);
      int4 dy = math.abs(y - yEnd);
      int4 max = math.max(dx, dy);
      int4 min = math.min(dx, dy);
      int4 heuristic = PathFinder.CostCardinal * max + (PathFinder.CostDiagonal - PathFinder.CostCardinal) * min;
      return heuristic * WeightMultipliers(x, y) / 10;
    }

    private int WeightFor(HeuristicWeight weight)
    {
      return weight switch
      {
        HeuristicWeight.Prefer => settings.prefer,
        HeuristicWeight.Avoid => settings.avoid,
        HeuristicWeight.Normal => settings.normal,
        _ => 1
      };
    }

    private int4 WeightMultipliers(int4 x, int4 y)
    {
      HeuristicWeight weight0 = WeightAt(x.x, y.x);
      HeuristicWeight weight1 = WeightAt(x.y, y.y);
      HeuristicWeight weight2 = WeightAt(x.z, y.z);
      HeuristicWeight weight3 = WeightAt(x.w, y.w);
      return new int4(WeightFor(weight0), WeightFor(weight1), WeightFor(weight2), WeightFor(weight3));
    }

    private HeuristicWeight WeightAt(int x, int y)
    {
      if (!MapUtils.InBounds(x, y, width, height))
        return HeuristicWeight.Normal;

      return weights[x + y * width];
    }
  }
}