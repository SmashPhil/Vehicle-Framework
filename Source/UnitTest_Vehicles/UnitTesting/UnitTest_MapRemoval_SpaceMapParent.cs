using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.UnitTesting;

[LoadIfOdysseyActive]
[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class UnitTest_MapRemoval_SpaceMapParent : UnitTest_MapRemoval<SpaceMapParent>
{
  private readonly WorldObjectDef spaceMapDef = DefDatabase<WorldObjectDef>.GetNamed("Space");

  protected override WorldObjectDef WorldObjectDef => spaceMapDef;
}