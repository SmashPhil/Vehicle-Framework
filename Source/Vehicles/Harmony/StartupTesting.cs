using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Unit Testing
	/// </summary>
	public static class StartupTesting
	{
		/// <summary>
		/// Load up game, get first settlement, find available vehicle, initiate strafing run
		/// </summary>
		//[UnitTest(Category = "Aerial", Name = "Strafe Run")]
		private static void UnitTest_Strafing()
		{
			Map sourceMap = null;
			Map targetMap = null;
			VehiclePawn vehicle = null;
			foreach (Settlement settlement in Find.WorldObjects.Settlements.Where(s => s.Faction == Faction.OfPlayer))
			{
				Map map = GetOrGenerateMapUtility.GetOrGenerateMap(settlement.Tile, null);
				if (map.spawnedThings.FirstOrDefault(t => t is VehiclePawn vehicleCheck && vehicleCheck.CompVehicleLauncher != null) is VehiclePawn vehicleTarget)
				{
					sourceMap = map;
					vehicle = vehicleTarget;
				}
				else
				{
					targetMap = map;
				}
			}
			if (sourceMap is null || targetMap is null)
			{
				SmashLog.Error($"Unable to execute unit test <method>UnitTestStrafing</method> post load.");
				return;
			}
			Current.Game.CurrentMap = targetMap;
			LaunchTargeter.Instance.RegisterActionOnTile(targetMap.Tile, new AerialVehicleArrivalAction_StrafeMap(vehicle, targetMap.Parent));
		}

		[UnitTest(Category = "UI", Name = "Regions", GameState = GameState.Playing)]
		private static void UnitTest_RegionsOn()
		{
			Prefs.DevMode = true;
			CameraJumper.TryHideWorld();
			VehicleMod.settings.debug.RegionDebugMenu();
		}

		/// <summary>
		/// Load up game, find available vehicle with upgrade tree, focus camera on vehicle
		/// </summary>
		//[UnitTest(Category = "UI", Name = "Upgrade Menu")]
		private static void UnitTest_UpgradeMenu()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Map map = Find.CurrentMap ?? Find.Maps.FirstOrDefault();
				VehiclePawn vehicle = (VehiclePawn)map.mapPawns.AllPawns.FirstOrDefault(p => p is VehiclePawn vehicle && vehicle.CompUpgradeTree != null);
				if (map is null || vehicle is null)
				{
					SmashLog.Error($"Unable to execute unit test <method>UnitTestUpgradeMenu</method> post load.");
					return;
				}
				CameraJumper.TryJump(vehicle);
				Find.Selector.Select(vehicle);
			});
		}

		[UnitTest(Category = "UI", Name = "Color Dialog", GameState = GameState.Playing)]
		private static void UnitTest_ColorDialog()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Map map = Find.CurrentMap ?? Find.Maps.FirstOrDefault();
				if (map is null)
				{
					SmashLog.Error($"Unable to execute unit test <method>{nameof(UnitTest_ColorDialog )}</method> post load. No map.");
					return;
				}
				VehiclePawn vehicle = (VehiclePawn)map.mapPawns.AllPawns.FirstOrDefault(p => p is VehiclePawn vehicle && vehicle.VehicleGraphic.Shader.SupportsRGBMaskTex());
				if (vehicle is null)
				{
					var vehicleDefs = DefDatabase<VehicleDef>.AllDefsListForReading.Where(vehicleDef => vehicleDef.graphicData.shaderType is RGBShaderTypeDef).ToList();
					if (vehicleDefs.NullOrEmpty())
					{
						SmashLog.Error($"Unable to execute unit test <method>{nameof(UnitTest_ColorDialog)}</method>. No vehicle defs to use as test case.");
						return;
					}
					VehicleDef vehicleDef = vehicleDefs.FirstOrDefault();
					vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
				}
				CameraJumper.TryJump(vehicle);
				Find.Selector.Select(vehicle);
				vehicle.ChangeColor();
			});
		}

		/// <summary>
		/// Load up game, open update menu for all previous versions
		/// </summary>
		[UnitTest(Category = "UI", Name = "Previous Versions Menu", GameState = GameState.Playing)]
		private static void UnitTest_ShowUpdates()
		{
			VehicleMod.settings.debug.ShowAllUpdates();
		}

		/// <summary>
		/// Load up game, open Mod Settings
		/// </summary>
		[UnitTest(Category = "UI", Name = "Mod Settings", GameState = GameState.OnStartup)]
		private static void UnitTest_ModSettings()
		{
			Dialog_ModSettings settings = new Dialog_ModSettings(VehicleMod.mod);
			Find.WindowStack.Add(settings);
		}

		/// <summary>
		/// Load up game, open blank Graph Editor
		/// </summary>
		[UnitTest(Category = "UI", Name = "Graph Editor", GameState = GameState.OnStartup)]
		private static void UnitTest_GraphEditor()
		{
			Dialog_GraphEditor settings = new Dialog_GraphEditor();
			Find.WindowStack.Add(settings);
		}

		/// <summary>
		/// Load up game, spawn vehicle, open Graph Editor for vehicle
		/// </summary>
		[UnitTest(Category = "UI", Name = "Animation Editor", GameState = GameState.Playing)]
		private static void UnitTest_AnimationEditor()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Map map = Find.CurrentMap ?? Find.Maps.FirstOrDefault();
				if (map is null)
				{
					SmashLog.Error($"Unable to execute unit test {nameof(UnitTest_AnimationEditor)}. No map.");
					return;
				}
				VehiclePawn vehicle = (VehiclePawn)map.mapPawns.AllPawns.FirstOrDefault(p => p is VehiclePawn vehicle);
				if (vehicle is null)
				{
					VehicleDef vehicleDef = GetVehicleDefAnimator();
					if (vehicleDef is null)
					{
						SmashLog.Error($"Unable to execute unit test {nameof(UnitTest_AnimationEditor)}. No vehicle defs to use as test case.");
						return;
					}
					vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
					IntVec3 cell = CellFinderExtended.RandomCenterCell(map, (IntVec3 cell) => !MapHelper.VehicleBlockedInPosition(vehicle, Current.Game.CurrentMap, cell, Rot4.North));
					GenSpawn.Spawn(vehicle, cell, map);
				}
				CameraJumper.TryJump(vehicle);
				Find.Selector.Select(vehicle);
				vehicle.OpenInAnimator();
			});

			VehicleDef GetVehicleDefAnimator()
			{
				List<VehicleDef> vehicleDefs = DefDatabase<VehicleDef>.AllDefsListForReading.ToList();
				foreach (VehicleDef vehicleDef in vehicleDefs)
				{
					foreach (CompProperties compProperties in vehicleDef.comps)
					{
						if (compProperties.compClass.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Any(fieldInfo => fieldInfo.HasAttribute<GraphEditableAttribute>()))
						{
							return vehicleDef;
						}
					}
				}
				return null;
			}
		}

		[UnitTest(Category = "UI", Name = "Vehicle Area Manager", GameState = GameState.Playing)]
		private static void UnitTest_VehicleAreaManager()
		{
			Prefs.DevMode = true;
			CameraJumper.TryHideWorld();
			if (Find.CurrentMap is Map map)
			{
				Find.WindowStack.Add(new Dialog_ManageAreas(map));
			}
			else
			{
				SmashLog.Error($"Tried to unit test <type>{nameof(UnitTest_VehicleAreaManager)}</type> with null current map.");
			}
		}

		[UnitTest(Category = "World", Name = "Caravan Formation", GameState = GameState.Playing)]
		private static void UnitTest_CaravanFormation()
		{
			Prefs.DevMode = true;
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				CameraJumper.TryShowWorld();
				Settlement settlement = Find.WorldObjects.Settlements.FirstOrDefault(settlement => settlement.Faction.IsPlayer);
				if (settlement == null)
				{
					SmashLog.Error($"Unable to execute unit test {nameof(UnitTest_AnimationEditor)}. No map to form player caravan from.");
					return;
				}
				Find.WindowStack.Add(new Dialog_FormVehicleCaravan(settlement.Map));
			});
		}

		/// <summary>
		/// Load up game, open route planner
		/// </summary>
		[UnitTest(Category = "World", Name = "World Route Planner", GameState = GameState.Playing)]
		private static void UnitTest_RoutePlanner()
		{
			Prefs.DevMode = true;
			CameraJumper.TryShowWorld();
			VehicleRoutePlanner.Instance.Start();
		}
	}
}
