using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using SmashTools.Debugging;
using UnityEngine;
using Verse;

namespace Vehicles.Testing
{
	internal abstract class UnitTestMapTest : UnitTest
	{
		public override TestType ExecuteOn => TestType.GameLoaded;

		public virtual bool ShouldTest(VehicleDef vehicleDef)
		{
			return true;
		}

		public virtual CellRect TestArea(VehicleDef vehicleDef, IntVec3 root)
		{
			int maxSize = Mathf.Max(vehicleDef.Size.x, vehicleDef.Size.z);
			return CellRect.CenteredOn(root, maxSize).ExpandedBy(5);
		}

		public override IEnumerable<UTResult> Execute()
		{
			CameraJumper.TryHideWorld();
			Map map = Find.CurrentMap;
			Assert.IsNotNull(map);
			Assert.IsTrue(DefDatabase<VehicleDef>.AllDefsListForReading.Count > 0, "No vehicles to test with");

			// Should always be at least 1 vehicle for unit tests to execute assuming debug vehicle is enabled
			foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
			{
				if (ShouldTest(vehicleDef))
				{
					VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
					TerrainDef terrainDef = DefDatabase<TerrainDef>.AllDefsListForReading
						.FirstOrDefault(def => VehiclePathGrid.PassableTerrainCost(vehicleDef, def, out _));

					IntVec3 root = map.Center;
					DebugHelper.DestroyArea(TestArea(vehicleDef, root), map, terrainDef);

					CameraJumper.TryJump(root, map, mode: CameraJumper.MovementMode.Cut);
					yield return TestVehicle(vehicle, map, root);

					if (!vehicle.Destroyed)
					{
						vehicle.DestroyVehicleAndPawns();
					}
				}
			}
		}

		protected abstract UTResult TestVehicle(VehiclePawn vehicle, Map map, IntVec3 root);

		protected class HitboxTester<T>
		{
			private readonly Map map;
			private readonly VehiclePawn vehicle;
			private Func<IntVec3, T> valueGetter;
			private Func<T, bool> validator;
			private Action<IntVec3> reset;

			public CellRect rect;

			public HitboxTester(VehiclePawn vehicle, Map map, IntVec3 root, Func<IntVec3, T> valueGetter, Func<T, bool> validator, Action<IntVec3> reset = null)
			{
				this.map = map;
				this.vehicle = vehicle;
				this.valueGetter = valueGetter;
				this.validator = validator;
				this.reset = reset;

				int radius = Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);
				rect = CellRect.CenteredOn(root, radius);
			}

			public void Start()
			{
				Reset();
			}

			public void Reset()
			{
				if (reset != null)
				{
					foreach (IntVec3 cell in rect)
					{
						reset.Invoke(cell);
					}
				}
			}

			public bool All(bool value)
			{
				return IsTrue(_ => value);
			}

			public bool Hitbox(bool value)
			{
				return IsTrue(cell => value ^ !vehicle.OccupiedRect().Contains(cell));
			}

			public bool IsTrue(Func<IntVec3, bool> expected)
			{
				foreach (IntVec3 cell in rect)
				{
					if (!Valid(cell, expected(cell)))
					{
						Valid(cell, expected(cell));
						return false;
					}
				}
				return true;
			}

			private bool Valid(IntVec3 cell, bool expected)
			{
				T current = valueGetter(cell);
				bool value = validator(current);
				bool result = value == expected;
				return result;
			}
		}
	}
}
