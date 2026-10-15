namespace SmashTools.PathFinder
{
  internal interface IPathCostProvider
  {
    int GetPathCost(int x, int y, int heading, in PathData pathData);
  }
}