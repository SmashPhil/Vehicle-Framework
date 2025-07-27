using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.UnitTesting;

[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class UnitTest_MapRemoval_DestroyedSettlement : UnitTest_MapRemoval<DestroyedSettlement>
{
  protected override WorldObjectDef WorldObjectDef => WorldObjectDefOf.DestroyedSettlement;

  protected override Faction Faction => Faction.OfPirates;
}