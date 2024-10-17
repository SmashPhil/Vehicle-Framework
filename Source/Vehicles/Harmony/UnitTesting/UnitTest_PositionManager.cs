using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using SmashTools;
using SmashTools.Debugging;
using UnityEngine;
using Verse;
using static SmashTools.Debug;

namespace Vehicles.Testing
{
	internal class UnitTest_PositionManager : UnitTest_MapTest
	{
		public override string Name => "PositionManager";

		public override bool ShouldTest(VehicleDef vehicleDef)
		{
			return true;
		}

		protected override UTResult TestVehicle(VehiclePawn vehicle, Map map, IntVec3 root)
		{
			int maxSize = Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);
			UTResult result;
			IntVec3 reposition = root + new IntVec3(maxSize, 0, 0);
			VehicleMapping mapping = map.GetCachedMapComponent<VehicleMapping>();
			VehicleMapping.VehiclePathData pathData = mapping[vehicle.VehicleDef];

			bool success;
			VehiclePositionManager positionManager = map.GetCachedMapComponent<VehiclePositionManager>();
			GenSpawn.Spawn(vehicle, root, map);
			HitboxTester<VehiclePawn> positionTester = new(vehicle, map, root,
				positionManager.ClaimedBy,
				(claimant) => claimant == vehicle);
			positionTester.Start();

			// Validate spawned vehicle claims rect in position manager
			success = positionTester.Hitbox(true);
			result.Add("Position Manager (Spawn)", success);

			// Validate position set updates valid claims
			vehicle.Position = reposition;
			success = positionTester.Hitbox(true);
			vehicle.Position = root;
			result.Add("Position Manager (set_Position)", success);

			// Validate rotation set updates valid claims
			vehicle.Rotation = Rot4.East;
			success = positionTester.Hitbox(true);
			vehicle.Rotation = Rot4.North;
			result.Add("Position Manager (set_Rotation)", success);

			// Validate despawning releases claim in position manager
			vehicle.DeSpawn();
			success = positionTester.All(false);
			result.Add("Position Manager (DeSpawn)", success);
			return result;
		}
	}
}
