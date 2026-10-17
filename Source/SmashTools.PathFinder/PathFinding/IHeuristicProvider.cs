using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  internal interface IHeuristicProvider
  {
    int4 GetHeuristic(int4 x, int4 y, int xEnd, int yEnd);
  }
}