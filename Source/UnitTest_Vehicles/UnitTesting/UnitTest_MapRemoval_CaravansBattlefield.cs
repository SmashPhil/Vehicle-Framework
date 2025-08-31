using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.UnitTesting;

[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class UnitTest_MapRemoval_CaravansBattlefield : UnitTest_MapRemoval<CaravansBattlefield>
{
  protected override WorldObjectDef WorldObjectDef => WorldObjectDefOf.Ambush;
}