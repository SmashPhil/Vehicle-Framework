using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Debugging;
using Verse;

namespace Vehicles.Testing
{
	internal class UnitTest_StashedVehicle : UnitTest
	{
		public override TestType ExecuteOn => TestType.GameLoaded;

		public override string Name => "StashedVehicle";

		public override IEnumerable<UTResult> Execute()
		{
			CameraJumper.TryShowWorld();
			Map map = Find.CurrentMap;
			World world = Find.World;

			VehicleDef vehicleDef = DefDatabase<VehicleDef>.AllDefsListForReading.RandomOrDefault(def => def.vehicleType == VehicleType.Land);

			if (world == null || map == null || vehicleDef == null)
			{
				yield return UTResult.For("UnitTest Setup", false);
				yield break;
			}

			VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);

			Pawn colonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
			Assert.IsTrue(colonist != null && colonist.Faction == Faction.OfPlayer, "Unable to generate colonist");
			Pawn animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Alphabeaver, Faction.OfPlayer);
			Assert.IsTrue(animal != null && animal.Faction == Faction.OfPlayer, "Unable to generate pet");

			VehicleHandler handler = vehicle.handlers.FirstOrDefault();
			Assert.IsNotNull(handler, "Testing with vehicle which has no roles");
			Assert.IsTrue(vehicle.TryAddPawn(colonist, handler), "Unable to add colonist to vehicle");
			Assert.IsTrue(vehicle.inventory.innerContainer.TryAddOrTransfer(animal, canMergeWithExistingStacks: false), "Unable to add pet to vehicle inventory");
			Assert.IsTrue(!vehicle.Destroyed && !vehicle.Discarded);

			VehicleCaravan vehicleCaravan = CaravanHelper.MakeVehicleCaravan([vehicle], Faction.OfPlayer, map.Tile, true);
			vehicleCaravan.Tile = map.Tile;

			StashedVehicle stashedVehicle = StashedVehicle.Create(vehicleCaravan, out Caravan caravan);
			VehicleCaravan mergedVehicleCaravan = null;

			bool result = stashedVehicle.Vehicles.Contains(vehicle);
			yield return UTResult.For("Vehicle Added", result);

			Assert.IsNotNull(caravan);
			result = caravan.PawnsListForReading.Contains(colonist) && caravan.PawnsListForReading.Contains(animal);
			result &= vehicle.AllPawnsAboard.NullOrEmpty() && !vehicle.inventory.innerContainer.Contains(animal);
			yield return UTResult.For("Caravan Created", result);

			Find.WorldPawns.gc.CancelGCPass();
			_ = Find.WorldPawns.gc.PawnGCPass();

			if (vehicle.Destroyed || vehicle.Discarded)
			{
				Find.WorldPawns.gc.LogGC();
				yield return UTResult.For($"Vehicle WorldPawnGC", false);
			}
			foreach (Pawn pawn in caravan.PawnsListForReading)
			{
				if (pawn.Destroyed || pawn.Discarded)
				{
					Find.WorldPawns.gc.LogGC();
					yield return UTResult.For($"Caravan WorldPawnGC", false);
				}
			}
			yield return UTResult.For("WorldPawnGC", true);

			mergedVehicleCaravan = stashedVehicle.Notify_CaravanArrived(caravan);
			Assert.IsNotNull(mergedVehicleCaravan, "Null vehicle caravan post-retrieval of stashed vehicle.");
			result = caravan.Destroyed && stashedVehicle.Destroyed;
			result &= mergedVehicleCaravan.ContainsPawn(vehicle);
			yield return UTResult.For("StashedVehicle Retrieved", result);

			Find.WorldPawns.gc.CancelGCPass();
			_ = Find.WorldPawns.gc.PawnGCPass();

			if (vehicle.Destroyed || vehicle.Discarded)
			{
				Find.WorldPawns.gc.LogGC();
				yield return UTResult.For($"Vehicle WorldPawnGC", false);
			}
			foreach (Pawn pawn in mergedVehicleCaravan.PawnsListForReading)
			{
				if (pawn.Destroyed || pawn.Discarded)
				{
					Find.WorldPawns.gc.LogGC();
					yield return UTResult.For($"VehicleCaravan WorldPawnGC", false);
				}
			}
			yield return UTResult.For("WorldPawnGC", true);
		}
	}
}
