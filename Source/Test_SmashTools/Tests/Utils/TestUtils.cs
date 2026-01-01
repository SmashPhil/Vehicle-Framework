using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SmashTools.UnitTesting;

internal static class TestUtils
{
  public static PlanetTile FindValidTile(PlanetLayerDef layerDef)
  {
    PlanetLayer layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
    return TileFinder.RandomSettlementTileFor(layer, Faction.OfPirates,
      extraValidator: ValidObjectTile);

    bool ValidObjectTile(PlanetTile tile)
    {
      return !Find.WorldObjects.AnyWorldObjectAt(tile);
    }
  }
}