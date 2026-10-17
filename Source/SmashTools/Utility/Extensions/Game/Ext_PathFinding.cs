using CoreLib.PathFinding;
using JetBrains.Annotations;
using Unity.Mathematics;
using Verse;

namespace SmashTools;

[PublicAPI]
public static class Ext_PathFinding
{
  public static Path.Node ToPathNode(this IntVec3 cell)
  {
    return new Path.Node(cell.x, cell.z);
  }

  public static IntVec3 ToIntVec3(this Path.Node node)
  {
    return new IntVec3(node.x, 0, node.y);
  }

  public static IntVec3 ToInt3(this Path.Node node)
  {
    return new int3(node.x, 0, node.y);
  }
}
