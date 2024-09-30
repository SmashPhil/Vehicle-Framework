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
	internal class UnitTest_MapGrids : UnitTest
	{
		public override string Name => "Map";

		public override TestType ExecuteOn => TestType.GameLoaded;

		public override IEnumerable<Func<UTResult>> Execute()
		{
			CameraJumper.TryHideWorld();
			Map map = Find.CurrentMap;
			Assert(map != null, "Map is null");
			Assert(DefDatabase<VehicleDef>.AllDefsListForReading.Count > 0, "No vehicles to test with");

			// Should always be at least 1 vehicle for unit tests to execute assuming debug vehicle is enabled
			foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
			{
				if (vehicleDef.vehicleType == VehicleType.Land && VehiclePathGrid.PassableTerrainCost(vehicleDef, TerrainDefOf.PackedDirt, out _))
				{
					yield return () => TestVehicleDef(vehicleDef, map);
				}
			}
		}

		private UTResult TestVehicleDef(VehicleDef vehicleDef, Map map)
		{
			UTResult result;

			VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
			TerrainDef terrainDef = TerrainDefOf.Concrete;

			bool success = CellFinderExtended.TryFindRandomCenterCell(map, Validator, out IntVec3 cell)
				|| CellFinder.TryFindRandomCell(map, Validator, out cell);
			result.Add($"Spawned {vehicleDef}", success);

			if (!success) return result;

			int maxSize = Mathf.Max(vehicleDef.Size.x, vehicleDef.Size.z);
			CellRect testArea = CellRect.CenteredOn(cell, maxSize).ExpandedBy(5);
			GenDebug.ClearArea(testArea, map);
			foreach (IntVec3 terrainCell in testArea)
			{
				map.terrainGrid.SetTerrain(terrainCell, terrainDef);
			}

			IntVec3 reposition = cell + new IntVec3(maxSize, 0, 0);
			VehicleMapping mapping = map.GetCachedMapComponent<VehicleMapping>();
			VehicleMapping.VehiclePathData pathData = mapping[vehicleDef];

			#region PositionManager

			VehiclePositionManager positionManager = map.GetCachedMapComponent<VehiclePositionManager>();
			{
				GenSpawn.Spawn(vehicle, cell, map);
				HitboxTester<VehiclePawn> positionTester = new(vehicle, map,
					positionManager.ClaimedBy,
					(claimant) => claimant == vehicle);
				positionTester.Start();

				// Validate spawned vehicle claims rect in position manager
				success = positionTester.Hitbox(true);
				result.Add("Position Manager (Spawn)", success);

				// Validate position set updates valid claims
				vehicle.Position = reposition;
				success = positionTester.Hitbox(true);
				vehicle.Position = cell;
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
			}

			#endregion PositionManager

			#region ThingGrid

			ThingGrid thingGrid = map.thingGrid;
			{
				GenSpawn.Spawn(vehicle, cell, map);
				HitboxTester<VehiclePawn> positionTester = new(vehicle, map,
					(cell) => thingGrid.ThingAt(cell, ThingCategory.Pawn) as VehiclePawn,
					(thing) => thing == vehicle);
				positionTester.Start();

				// Validate spawned vehicle registers in thingGrid
				success = positionTester.Hitbox(true);
				result.Add("ThingGrid (Spawn)", success);

				// Validate position set updates thingGrid
				vehicle.Position = reposition;
				success = positionTester.Hitbox(true);
				vehicle.Position = cell;
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
			}

			#endregion

			#region PathGrid

			VehiclePathGrid pathGrid = pathData.VehiclePathGrid;
			{
				//GenSpawn.Spawn(vehicle, cell, map);
				//HitboxTester<int> positionTester = new(vehicle, map,
				//	(cell) => pathGrid.CalculatedCostAt(cell),
				//	(cost) => cost == VehiclePathGrid.TerrainCostAt(vehicleDef, terrainDef));
				//positionTester.Start();

				//// Validate spawned vehicle registers in thingGrid
				//success = positionTester.Hitbox(true);
				//result.Add("ThingGrid (Spawn)", success);

				//// Validate position set updates thingGrid
				//vehicle.Position = reposition;
				//success = positionTester.Hitbox(true);
				//vehicle.Position = cell;
				//result.Add("ThingGrid (set_Position)", success);

				//// Validate rotation set updates thingGrid
				//vehicle.Rotation = Rot4.East;
				//success = positionTester.Hitbox(true);
				//vehicle.Rotation = Rot4.North;
				//result.Add("ThingGrid (set_Rotation)", success);

				//// Validate despawning deregisters from thingGrid
				//vehicle.DeSpawn();
				//success = positionTester.All(false);
				//result.Add("ThingGrid (DeSpawn)", success);
			}

			#endregion

			#region CoverGrid

			CoverGrid coverGrid = map.coverGrid;
			{
				GenSpawn.Spawn(vehicle, cell, map);
				HitboxTester<Thing> coverTester = new(vehicle, map,
					(IntVec3 cell) => coverGrid[cell],
					(Thing thing) => thing == vehicle);
				coverTester.Start();

				// Validate spawned vehicle shows up in cover grid
				success = coverTester.Hitbox(true);
				result.Add("Cover Grid (Spawn)", success);

				// Validate position set moves vehicle in cover grid
				vehicle.Position = reposition;
				success = coverTester.Hitbox(true);
				vehicle.Position = cell;
				result.Add("Cover Grid (set_Position)", success);

				// Validate rotation set moves vehicle in cover grid
				vehicle.Rotation = Rot4.East;
				success = coverTester.Hitbox(true);
				vehicle.Rotation = Rot4.North;
				result.Add("Cover Grid (set_Rotation)", success);

				// Validate despawning reverts back to thing before vehicle was spawned
				vehicle.DeSpawn();
				success = coverTester.All(false);
				result.Add("Cover Grid (DeSpawn)", success);
			}

			#endregion CoverGrid

			#region GasGrid

			GasGrid gasGrid = map.gasGrid;
			{
				bool blocksGas = vehicle.VehicleDef.Fillage == FillCategory.Full;
				HitboxTester<bool> gasTester = new(vehicle, map,
					gasGrid.AnyGasAt,
					// Gas should only remain if vehicle fillage is Full
					(bool gasAt) => gasAt == !blocksGas,
					(_) => gasGrid.Debug_ClearAll());
				gasTester.Start();

				gasGrid.Debug_FillAll();
				Assert(testArea.All(gasGrid.AnyGasAt));

				// Validate spawned vehicle removes gas if Fillage = Full
				GenSpawn.Spawn(vehicle, cell, map);
				success = blocksGas ? gasTester.Hitbox(true) : gasTester.All(true);
				result.Add("Gas Grid (Spawn)", success);
				gasTester.Reset();

				// Validate position set updates gas grid blockage without artifacts
				vehicle.Position = reposition;
				gasGrid.Debug_FillAll();
				success = blocksGas ? gasTester.Hitbox(true) : gasTester.All(true);
				vehicle.Position = cell;
				result.Add("Gas Grid (set_Position)", success);
				gasTester.Reset();

				// Validate rotation set updates gas grid blockage without artifacts
				vehicle.Rotation = Rot4.East;
				gasGrid.Debug_FillAll();
				success = blocksGas ? gasTester.Hitbox(true) : gasTester.All(true);
				vehicle.Rotation = Rot4.North;
				result.Add("Gas Grid (set_Rotation)", success);
				gasTester.Reset();

				// Validate despawning removes vehicle from potential gas grid blockage
				vehicle.DeSpawn();
				gasGrid.Debug_FillAll();
				success = blocksGas ? gasTester.Hitbox(false) : gasTester.All(true);
				result.Add("Gas Grid (DeSpawn)", success);
				gasTester.Reset();
			}

			#endregion GasGrid

			return result;

			bool Validator(IntVec3 cell)
			{
				int maxSize = Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);
				IntVec3 moveCell = cell + new IntVec3(maxSize, 0, 0);
				return !MapHelper.ImpassableOrVehicleBlocked(vehicle, map, cell, Rot4.North) &&
					   !MapHelper.ImpassableOrVehicleBlocked(vehicle, map, moveCell, Rot4.North);
			}
		}

		private class HitboxTester<T>
		{
			private readonly Map map;
			private readonly VehiclePawn vehicle;
			private Func<IntVec3, T> valueGetter;
			private Func<T, bool> validator;
			private Action<IntVec3> reset;

			public CellRect rect;

			public HitboxTester(VehiclePawn vehicle, Map map, Func<IntVec3, T> valueGetter, Func<T, bool> validator, Action<IntVec3> reset = null)
			{
				this.map = map;
				this.vehicle = vehicle;
				this.valueGetter = valueGetter;
				this.validator = validator;
				this.reset = reset;

				int radius = Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);
				rect = CellRect.CenteredOn(vehicle.Position, radius);
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
				return IsTrue(cell => value && vehicle.OccupiedRect().Contains(cell));
			}

			public bool IsTrue(Func<IntVec3, bool> expected)
			{
				foreach (IntVec3 cell in rect)
				{
					if (!Valid(cell, expected(cell)))
					{
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
