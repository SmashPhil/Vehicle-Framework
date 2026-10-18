using Unity.Collections;
using Unity.Jobs;

namespace SmashTools.PathFinder;

internal struct SumDiagonalJob : IJob
{
  private readonly int width;
  private readonly int height;

  private NativeArray<int> table;
  private NativeArray<int> blocked;

  [ReadOnly]
  private NativeArray<int>.ReadOnly pathGrid;

  public SumDiagonalJob(int width, int height, in SumAreaTable.JobData data)
  {
    this.width = width;
    this.height = height;

    table = data.table;
    blocked = data.blocked;
    pathGrid = data.tableCosts;
  }

  public void Execute()
  {
    int stride = width + height;
    int size = stride - 1;
    for (int i = 0; i < stride * stride; i++)
    {
      table[i] = 0;
      blocked[i] = 0;
    }

    int vShift = height - 1;
    for (int y = 0; y < height; y++)
    {
      int gridRow = y * width;
      for (int x = 0; x < width; x++)
      {
        int cost = pathGrid[gridRow + x];
        int blockedValue = cost is >= PathFinder.ImpassableCost or < 0 ? 1 : 0;

        int u = x + y;
        int v = x - y + vShift;

        int index = (u + 1) * stride + v + 1;
        table[index] = cost;
        blocked[index] = blockedValue;
      }
    }

    for (int u = 1; u <= size; u++)
    {
      int row = u * stride;
      int prevRow = (u - 1) * stride;

      int sum = 0;
      int impassable = 0;

      table[row] = 0;
      blocked[row] = 0;

      for (int v = 1; v <= size; v++)
      {
        sum += table[row + v];
        impassable += blocked[row + v];

        table[row + v] = table[prevRow + v] + sum;
        blocked[row + v] = blocked[prevRow + v] + impassable;
      }
    }
  }
}