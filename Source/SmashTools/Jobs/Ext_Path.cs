using CoreLib.PathFinding;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace SmashTools;

[PublicAPI]
public static class Ext_Path
{
  public static void DrawPath(this Path path, [CanBeNull] Thing thing)
  {
    if (!path.IsValid || path.IsFinished)
      return;

    float drawOffset = AltitudeLayer.Item.AltitudeFor();

    for (int i = 0; i < path.NodesLeft - 1; i++)
    {
      IntVec3 pos = path.Peek(i).ToIntVec3();
      Vector3 from = pos.ToVector3Shifted();
      from.y = drawOffset;
      IntVec3 nextPos = path.Peek(i + 1).ToIntVec3();
      Vector3 to = nextPos.ToVector3Shifted();
      to.y = drawOffset;
      GenDraw.DrawLineBetween(from, to);
    }
    if (thing is not null)
    {
      Vector3 curFrom = thing.DrawPos;
      curFrom.y = drawOffset;
      IntVec3 curPos = path.Peek(0).ToIntVec3();
      Vector3 curTo = curPos.ToVector3Shifted();
      curTo.y = drawOffset;
      if ((curFrom - curTo).sqrMagnitude > 0.01f)
      {
        GenDraw.DrawLineBetween(curFrom, curTo);
      }
    }
  }
}
