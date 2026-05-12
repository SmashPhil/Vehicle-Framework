using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.Testing;

[LoadIfOdysseyActive]
[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class Test_MapRemoval_SpaceMapParent : Test_MapRemoval<SpaceMapParent>
{
  private readonly WorldObjectDef spaceMapDef = DefDatabase<WorldObjectDef>.GetNamed("Space");

  protected override WorldObjectDef WorldObjectDef => spaceMapDef;
}