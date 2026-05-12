using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.Testing;

[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class Test_MapRemoval_DestroyedSettlement : Test_MapRemoval<DestroyedSettlement>
{
  protected override WorldObjectDef WorldObjectDef => WorldObjectDefOf.DestroyedSettlement;

  protected override Faction Faction => Faction.OfPirates;
}