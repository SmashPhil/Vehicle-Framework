using System;
using JetBrains.Annotations;
using Verse;

namespace Vehicles;

[AssignedFromXml]
public class CustomHitFlags : Def
{
  public float minFillPercent = -1f;
  [Obsolete]
  public bool hitThroughPawns;
}