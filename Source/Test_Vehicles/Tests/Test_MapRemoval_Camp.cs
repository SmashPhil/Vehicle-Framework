using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.Testing;

[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class Test_MapRemoval_Camp : Test_MapRemoval<Camp>
{
  protected override WorldObjectDef WorldObjectDef => WorldObjectDefOf.Camp;
}