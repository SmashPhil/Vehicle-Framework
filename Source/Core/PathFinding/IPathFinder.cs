using JetBrains.Annotations;

namespace CoreLib.PathFinding;

[PublicAPI]
public interface IPathFinder<in C>
{
  Path FindPath(Path.Node start, Path.Node end, C context);

  IPathPromise RequestPath(Path.Node start, Path.Node end, C context);
}