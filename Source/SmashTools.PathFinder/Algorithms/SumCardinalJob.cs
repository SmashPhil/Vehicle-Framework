
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SmashTools.PathFinder;

internal struct SumCardinalJob : IJob
{
  private readonly int width;
  private readonly int height;

  private NativeArray<int> table;
  private NativeArray<int> blocked;

  [ReadOnly]
  private NativeArray<int>.ReadOnly pathGrid;

  public SumCardinalJob(int width, int height, in SumAreaTable.JobData data)
  {
    this.width = width;
    this.height = height;

    table = data.table;
    blocked = data.blocked;
    pathGrid = data.tableCosts;
  }

  public void Execute()
  {
    for (int i = 0; i <= width; i++)
    {
      table[i] = 0;
      blocked[i] = 0;
    }

    int stride = width + 1;
    for (int i = 1; i <= height; i++)
    {
      int row = i * stride;
      int prevRow = (i - 1) * stride;
      int gridRow = (i - 1) * width;
      table[row] = 0;
      blocked[row] = 0;

      int sum = 0;
      int impassable = 0;
      for (int j = 1; j <= width; j++)
      {
        int cost = pathGrid[gridRow + j - 1];
        sum += cost;
        impassable += cost is >= PathFinder.ImpassableCost or < 0 ? 1 : 0;
        table[row + j] = table[prevRow + j] + sum;
        blocked[row + j] = blocked[prevRow + j] + impassable;
      }
    }
  }
}