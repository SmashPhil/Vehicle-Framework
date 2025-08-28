using DevTools.UnitTesting;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing), Disabled]
[TestCategory(TestCategoryNames.VehiclePawn)]
[TestDescription("MapPawns properly fetches pawns in vehicles.")]
internal sealed class UnitTest_MapPawns
{
	// TODO - Patches need to be fixed such that vehicles get picked up automatically by thing lister and internal pawns
	// are added without additional patches to MapPawns or PawnsFinder. This comes with a lot of risks such as duplicating
	// pawn entries so this will need to be thoroughly vetted before being pushed to release.

	[Test]
	private void AllPawnsUnspawned()
	{
	}

	[Test]
	private void AllPawnsSpawned()
	{
	}
}