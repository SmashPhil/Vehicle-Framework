using JetBrains.Annotations;
using Unity.Mathematics;
using Verse;

namespace SmashTools;

[PublicAPI]
public static class Ext_IntVec
{
  public static IntVec2 ToIntVec2(this int2 pos)
  {
    return new IntVec2(pos.x, pos.y);
  }

  public static IntVec3 ToIntVec3(this int3 pos)
  {
    return new IntVec3(pos.x, pos.y, pos.z);
  }

  public static IntVec3 ToIntVec3(this int2 pos)
  {
    return new IntVec3(pos.x, 0, pos.y);
  }

  public static int2 ToInt2(this IntVec2 pos)
  {
    return new int2(pos.x, pos.z);
  }

  public static int3 ToIntVec3(this IntVec3 pos)
  {
    return new int3(pos.x, pos.y, pos.z);
  }
}
