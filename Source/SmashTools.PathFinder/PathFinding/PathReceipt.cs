using CoreLib;
using CoreLib.PathFinding;
using CoreLib.Performance;
using JetBrains.Annotations;
using Unity.Jobs;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public class PathReceipt : IPathPromise
  {
    private PathData pathData;

    private IObjectPool<PathData> dataPool;

    internal PathReceipt(PathRequest request, PathData pathData)
    {
      PathRequest = request;
      this.pathData = pathData;
    }

    internal PathReceipt(PathRequest request, PathData pathData, IObjectPool<PathData> dataPool)
      : this(request, pathData)
    {
      this.dataPool = dataPool;
    }

    public bool IsCompleted => JobHandle.IsCompleted;

    internal JobHandle JobHandle { get; set; }

    public PathRequest PathRequest { get; private set; }

    internal PathData PathData => pathData;

    public Path GetPath()
    {
      JobHandle.Complete();
      Path path = new();
      if (!pathData.path.IsCreated)
      {
        Logger.Error("Claiming path after path data was destroyed.");
        return path;
      }
      path.Populate(pathData.path, pathData.mapSize.x);
      path.Validate(PathRequest.start, PathRequest.end);
      return path;
    }

    public void Cancel()
    {
      // Jobs cannot be canceled, the best we can do is complete and dispose
      JobHandle.Complete();
      Dispose();
    }

    public void Dispose()
    {
      JobHandle.Complete();
      if (dataPool != null)
      {
        pathData.Reset();
        dataPool.Return(pathData);
      }
      else
      {
        pathData.Dispose();
      }
    }
  }
}