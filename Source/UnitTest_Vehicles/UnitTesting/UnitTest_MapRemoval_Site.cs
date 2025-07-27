using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.UnitTesting;

[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class UnitTest_MapRemoval_Site : UnitTest_MapRemoval<Site>
{
  protected override WorldObjectDef WorldObjectDef => WorldObjectDefOf.Site;
}