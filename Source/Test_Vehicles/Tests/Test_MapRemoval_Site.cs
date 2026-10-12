using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.Tests;

[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class Test_MapRemoval_Site : Test_MapRemoval<Site>
{
  protected override WorldObjectDef WorldObjectDef => WorldObjectDefOf.Site;
}