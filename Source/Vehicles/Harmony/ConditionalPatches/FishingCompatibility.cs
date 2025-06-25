using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles.Compatibility;

internal static class FishingCompatibility
{
  private static readonly List<ThingDef> tmpFishes = [];

  public static bool Active { get; private set; }

  internal static void EnableFishing()
  {
    Active = true;

    foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading.Where(thingDef =>
      thingDef.ingestible != null))
    {
      tmpFishes.Add(thingDef);
    }
  }

  internal static ThingDef FetchViableFish(BiomeDef biomeDef, TerrainDef terrainDef)
  {
    return tmpFishes.RandomElement();
  }
}