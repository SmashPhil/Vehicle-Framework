using JetBrains.Annotations;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public sealed class PathGridScalar : PathBoolGrid
  {
    internal const int Precision = 16;

    public struct Data
    {
      internal PathGridScalar grid;
      internal int factor;
    }

    public PathGridScalar(int2 mapSize, [CanBeNull] IPathGridSync sync = null) :
      base(mapSize, sync)
    {
    }

    public Data ToData(float factor)
    {
      return new Data
      {
        grid = this,
        factor = (int)(factor * (1 << Precision))
      };
    }
  }
}