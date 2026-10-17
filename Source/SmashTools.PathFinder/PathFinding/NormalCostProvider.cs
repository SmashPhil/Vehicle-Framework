
namespace SmashTools.PathFinder
{
  internal struct NormalCostProvider : IPathCostProvider
  {
    public int GetPathCost(int x, int y, int heading, in PathData pathData)
    {
      int index = x + y * pathData.mapSize.x;
      return pathData.pathGrid[index];
    }
  }
}