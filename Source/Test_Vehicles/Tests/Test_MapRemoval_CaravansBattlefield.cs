using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.Testing;

[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class Test_MapRemoval_CaravansBattlefield : Test_MapRemoval<CaravansBattlefield>
{
  protected override WorldObjectDef WorldObjectDef => WorldObjectDefOf.Ambush;
}