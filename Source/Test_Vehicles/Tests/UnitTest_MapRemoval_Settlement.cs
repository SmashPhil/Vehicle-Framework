using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.UnitTesting;

[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class UnitTest_MapRemoval_Settlement : UnitTest_MapRemoval<Settlement>
{
  protected override WorldObjectDef WorldObjectDef => WorldObjectDefOf.Settlement;

  protected override Faction Faction => Faction.OfPirates;
}