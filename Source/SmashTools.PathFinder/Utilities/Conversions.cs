
using CoreLib.PathFinding;
using CoreLib.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder;

internal static class Conversions
{
  public static int2 ToInt2(this Path.Node node)
  {
    return new int2(node.x, node.y);
  }

  public static int3 ToInt3(this Path.Node node)
  {
    return new int3(node.x, 0, node.y);
  }

  public static Path.Node ToPathNode(this int3 pos)
  {
    return new Path.Node(pos.x, pos.z);
  }

  public static void Populate(this Path path, in NativeFixedList<int> list, int width)
  {
    path.Clear();
    for (int i = 0; i < list.Count; i++)
    {
      int index = list[i];
      path.Add(index, width);
    }
    path.IsValid = true;
    path.ResetPathToStart();
  }

  public static void Validate(this Path path, int3 start, int3 end)
  {
    Validate(path, start.ToPathNode(), end.ToPathNode());
  }

  public static void Validate(this Path path, Path.Node start, Path.Node end)
  {
    path.IsValid = path.Nodes.Count > 0 && start == path.FirstNode && end == path.LastNode;
  }
}
