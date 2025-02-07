using RimWorld;
using SmashTools;
using SmashTools.Debugging;
using UnityEngine;
using Verse;

namespace Vehicles.Testing
{
	internal class UnitTestThingGrid : UnitTestMapTest
	{
		public override string Name => "ThingGrid";

		public override bool ShouldTest(VehicleDef vehicleDef)
		{
			return vehicleDef.vehicleType == VehicleType.Land && VehiclePathGrid.PassableTerrainCost(vehicleDef, TerrainDefOf.Concrete, out _);
		}

		protected override UTResult TestVehicle(VehiclePawn vehicle, Map map, IntVec3 root)
		{
			int maxSize = Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);
			UTResult result;
			IntVec3 reposition = root + new IntVec3(maxSize, 0, 0);
			VehicleMapping mapping = map.GetCachedMapComponent<VehicleMapping>();
			VehicleMapping.VehiclePathData pathData = mapping[vehicle.VehicleDef];

			bool success;
			ThingGrid thingGrid = map.thingGrid;
			HitboxTester<VehiclePawn> positionTester = new(vehicle, map, root,
				(cell) => thingGrid.ThingAt(cell, ThingCategory.Pawn) as VehiclePawn,
				(thing) => thing == vehicle);
			positionTester.Start();

			GenSpawn.Spawn(vehicle, root, map);
			// Validate spawned vehicle registers in thingGrid
			success = positionTester.Hitbox(true);
			result.Add("ThingGrid (Spawn)", success);

			// Validate position set updates thingGrid
			vehicle.Position = reposition;
			success = positionTester.Hitbox(true);
			vehicle.Position = root;
			result.Add("ThingGrid (set_Position)", success);

			// Validate rotation set updates thingGrid
			vehicle.Rotation = Rot4.East;
			success = positionTester.Hitbox(true);
			vehicle.Rotation = Rot4.North;
			result.Add("ThingGrid (set_Rotation)", success);

			// Validate despawning deregisters from thingGrid
			vehicle.DeSpawn();
			success = positionTester.All(false);
			result.Add("ThingGrid (DeSpawn)", success);
			return result;
		}
	}
}
