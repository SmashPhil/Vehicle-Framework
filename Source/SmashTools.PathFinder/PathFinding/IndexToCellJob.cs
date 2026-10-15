using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder;

public struct IndexToCellJob : IJob
{
  private readonly int2 size;
  [WriteOnly]
  private NativeArray<uint> indexToCell;

  public IndexToCellJob(int2 size, NativeArray<uint> indexToCell)
  {
    this.size = size;
    this.indexToCell = indexToCell;
  }

  public void Execute()
  {
    for (int y = 0; y < size.y; y++)
    {
      int row = y * size.x;
      for (int x = 0; x < size.x; x++)
      {
        indexToCell[row + x] = (uint)x | ((uint)y << 16);
      }
    }
  }
}
