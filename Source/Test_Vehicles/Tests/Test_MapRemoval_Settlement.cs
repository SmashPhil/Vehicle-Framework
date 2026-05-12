using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.Testing;

[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class Test_MapRemoval_Settlement : Test_MapRemoval<Settlement>
{
  protected override WorldObjectDef WorldObjectDef => WorldObjectDefOf.Settlement;

  protected override Faction Faction => Faction.OfPirates;
}