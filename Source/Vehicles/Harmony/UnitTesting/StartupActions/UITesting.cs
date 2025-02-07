using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.Profile;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Unit Testing
	/// </summary>
	public static class UITesting
	{
		[StartupAction(Category = "UI", Name = "Regions", GameState = GameState.Playing)]
		private static void StartupAction_RegionsOn()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Prefs.DevMode = true;
				CameraJumper.TryHideWorld();
				VehicleMod.settings.debug.RegionDebugMenu();
			});
		}

		/// <summary>
		/// Load up game, find available vehicle with upgrade tree, focus camera on vehicle
		/// </summary>
		[StartupAction(Category = "UI", Name = "Upgrade Menu")]
		private static void StartupAction_UpgradeMenu()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Map map = Find.CurrentMap ?? Find.Maps.FirstOrDefault();
				VehiclePawn vehicle = (VehiclePawn)map.mapPawns.AllPawns.FirstOrDefault(p => p is VehiclePawn vehicle && vehicle.CompUpgradeTree != null);
				if (map is null || vehicle is null)
				{
					SmashLog.Error($"Unable to execute startup action <method>UnitTestUpgradeMenu</method> post load.");
					return;
				}
				CameraJumper.TryJump(vehicle);
				Find.Selector.Select(vehicle);
			});
		}

		[StartupAction(Category = "UI", Name = "Color Dialog", GameState = GameState.Playing)]
		private static void StartupAction_ColorDialog()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Map map = Find.CurrentMap ?? Find.Maps.FirstOrDefault();
				if (map is null)
				{
					SmashLog.Error($"Unable to execute startup action <method>{nameof(StartupAction_ColorDialog)}</method> post load. No map.");
					return;
				}
				VehiclePawn vehicle = (VehiclePawn)map.mapPawns.AllPawns.FirstOrDefault(p => p is VehiclePawn vehicle && 
				vehicle.VehicleGraphic.Shader.SupportsRGBMaskTex());
				if (vehicle is null)
				{
					var vehicleDefs = DefDatabase<VehicleDef>.AllDefsListForReading.Where(vehicleDef => 
						vehicleDef.graphicData.shaderType is RGBShaderTypeDef).ToList();
					if (vehicleDefs.NullOrEmpty())
					{
						SmashLog.Error($"Unable to execute startup action <method>{nameof(StartupAction_ColorDialog)}</method>. No vehicle defs to use as test case.");
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
		[StartupAction(Category = "UI", Name = "Previous Versions Menu", GameState = GameState.Playing)]
		private static void StartupAction_ShowUpdates()
		{
			VehicleMod.settings.debug.ShowAllUpdates();
		}

		/// <summary>
		/// Load up game, open Mod Settings
		/// </summary>
		[StartupAction(Category = "UI", Name = "Mod Settings", GameState = GameState.OnStartup)]
		private static void StartupAction_ModSettings()
		{
			Dialog_ModSettings settings = new Dialog_ModSettings(VehicleMod.mod);
			Find.WindowStack.Add(settings);
		}

		/// <summary>
		/// Load up game, open blank Graph Editor
		/// </summary>
		[StartupAction(Category = "UI", Name = "Graph Editor", GameState = GameState.OnStartup)]
		private static void StartupAction_GraphEditor()
		{
			Dialog_GraphEditor settings = new Dialog_GraphEditor();
			Find.WindowStack.Add(settings);
		}

		/// <summary>
		/// Load up game, spawn vehicle, open Graph Editor for vehicle
		/// </summary>
		[StartupAction(Category = "UI", Name = "Animation Editor", GameState = GameState.Playing)]
		private static void StartupAction_AnimationEditor()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Map map = Find.CurrentMap ?? Find.Maps.FirstOrDefault();
				if (map is null)
				{
					SmashLog.Error($"Unable to execute startup action {nameof(StartupAction_AnimationEditor)}. No map.");
					return;
				}
				VehiclePawn vehicle = (VehiclePawn)map.mapPawns.AllPawns.FirstOrDefault(pawn =>
					pawn is VehiclePawn vehicle && vehicle.animator != null);
				if (vehicle is null)
				{
					VehicleDef vehicleDef = DefDatabase<VehicleDef>.AllDefsListForReading.FirstOrDefault(def => def.drawProperties?.controller != null);
					if (vehicleDef is null)
					{
						SmashLog.Error($"Unable to execute startup action {nameof(StartupAction_AnimationEditor)}. No vehicle defs to use as test case.");
						return;
					}
					vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
					if (!CellFinderExtended.TryFindRandomCenterCell(map, (cell) => 
					!MapHelper.NonStandableOrVehicleBlocked(vehicle, Current.Game.CurrentMap, 
							                                                     cell, Rot4.North), out IntVec3 spawnCell))
					{
						spawnCell = CellFinder.RandomCell(map);
					}
					GenSpawn.Spawn(vehicle, spawnCell, map);
				}
				CameraJumper.TryJump(vehicle);
				Find.Selector.Select(vehicle);
				vehicle.OpenInAnimatorTemp();
			});
		}

		[StartupAction(Category = "UI", Name = "Vehicle Area Manager", GameState = GameState.Playing)]
		private static void StartupAction_VehicleAreaManager()
		{
			Prefs.DevMode = true;
			CameraJumper.TryHideWorld();
			if (Find.CurrentMap is Map map)
			{
				Find.WindowStack.Add(new Dialog_ManageAreas(map));
			}
			else
			{
				SmashLog.Error($"Tried to startup action <type>{nameof(StartupAction_VehicleAreaManager)}</type> with null current map.");
			}
		}
	}
}
