using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Xml;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	internal class Debug : IPatchCategory
	{
		public static void Message(string text)
		{
			if (VehicleMod.settings.debug.debugLogging)
			{
				SmashLog.Message(text);
			}
		}

		public static void Warning(string text)
		{
			if (VehicleMod.settings.debug.debugLogging)
			{
				SmashLog.Warning(text);
			}
		}

		public static void Error(string text)
		{
			if (VehicleMod.settings.debug.debugLogging)
			{
				SmashLog.Error(text);
			}
		}

		public void PatchMethods()
		{
			if (VehicleHarmony.debug)
			{
				VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)), prefix: null,
					postfix: new HarmonyMethod(typeof(Debug),
					nameof(DebugSettlementPaths)));
				VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Add)),
					prefix: new HarmonyMethod(typeof(Debug),
					nameof(DebugWorldObjects)));
			}

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(DebugToolsSpawning), "SpawnPawn"),
				postfix: new HarmonyMethod(typeof(Debug),
				nameof(DebugHideVehiclesFromPawnSpawner)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(Caravan_NeedsTracker), "TrySatisfyPawnNeeds", parameters: new Type[] { typeof(Pawn) }),
			//	prefix: new HarmonyMethod(typeof(Debug),
			//	nameof(TestPrefix)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(XmlInheritance), nameof(XmlInheritance.TryRegister)),
			//	postfix: new HarmonyMethod(typeof(Debug),
			//	nameof(TestPostfix)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(Thing), "ExposeData"),
			//	finalizer: new HarmonyMethod(typeof(Debug),
			//	nameof(ExceptionCatcher)));
		}

		public static void TestPrefix(Pawn pawn)
		{
			try
			{
				Log.Message($"Satisfying: {pawn}");
			}
			catch (Exception ex)
			{
				Log.Error($"[Test Prefix] Exception Thrown.\nException={ex}\nInnerException={ex.InnerException}\n");
			}
		}

		public static void TestPostfix(XmlNode node, ModContentPack mod)
		{
			try
			{
				XmlAttribute xmlAttribute = node.Attributes["Name"];
				if (xmlAttribute != null && xmlAttribute.Value == "DrugBaseTest" && mod.PackageId == "SmashPhil.VehicleFramework")
				{
					Log.Message($"Registering {xmlAttribute.Name} = {xmlAttribute.Value}");
				}
			}
			catch (Exception ex)
			{
				Log.Error($"[Test Postfix] Exception Thrown.\nException={ex}\nInnerException={ex.InnerException}\n");
			}
		}

		public static Exception ExceptionCatcher(Thing __instance, Exception __exception)
		{
			if (__exception != null)
			{
				SmashLog.Message($"Exception caught! <error>Ex={__exception}</error> Instance: {__instance}");
			}
			return __exception;
		}

		/// <summary>
		/// Show original settlement positions before being moved to the coast
		/// </summary>
		/// <param name="o"></param>
		public static void DebugWorldObjects(WorldObject o)
		{
			if(o is Settlement)
			{
				VehicleHarmony.tiles.Add(new Pair<int, int>(o.Tile, 0));
			}
		}

		/// <summary>
		/// Removes Vehicle entries from Spawn Pawn menu, as that uses vanilla Pawn Generation whereas vehicles need special handling
		/// </summary>
		/// <param name="__result"></param>
		public static void DebugHideVehiclesFromPawnSpawner(List<DebugActionNode> __result)
		{
			for (int i = __result.Count - 1; i >= 0; i--)
			{
				string defName = __result[i].label;
				PawnKindDef pawnKindDef = DefDatabase<PawnKindDef>.GetNamed(defName);
				if (pawnKindDef?.race is VehicleDef)
				{
					__result.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Draw paths from original settlement position to new position when moving settlement to coastline
		/// </summary>
		public static void DebugSettlementPaths()
		{
			if (VehicleHarmony.drawPaths && VehicleHarmony.debugLines.NullOrEmpty())
			{
				return;
			}
			if (VehicleHarmony.drawPaths)
			{
				foreach (WorldPath wp in VehicleHarmony.debugLines)
				{
					wp.DrawPath(null);
				}
			}
			foreach (Pair<int, int> t in VehicleHarmony.tiles)
			{
				GenDraw.DrawWorldRadiusRing(t.First, t.Second);
			}
		}

		[DebugAction(VehicleHarmony.VehiclesLabel, "Draw Hitbox Size", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void DebugDrawHitbox()
		{
			DebugTool tool = null;
			IntVec3 first;
			tool = new DebugTool("first corner...", delegate ()
			{
				first = UI.MouseCell();
				string label = "second corner...";
				Action action = delegate ()
				{
					IntVec3 second = UI.MouseCell();
					CellRect cellRect = CellRect.FromLimits(first, second).ClipInsideMap(Find.CurrentMap);
					IntVec3 center = cellRect.ThingPositionFromRect();
					foreach (IntVec3 cell in cellRect)
					{
						IntVec3 diff = cell - center;
						Current.Game.CurrentMap.debugDrawer.FlashCell(cell, 0.75f, diff.ToIntVec2.ToString(), 3600);
					}
					DebugTools.curTool = tool;
				};
				DebugTools.curTool = new DebugTool(label, action, first);
			});
			DebugTools.curTool = tool;
		}

		[DebugAction(VehicleHarmony.VehiclesLabel, "Regenerate WorldPathGrid", allowedGameStates = AllowedGameStates.WorldRenderedNow)]
		public static void DebugRegenerateWorldPathGrid()
		{
			Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().RecalculateAllPerceivedPathCosts();
		}

		[DebugAction(VehicleHarmony.VehiclesLabel, "Ground All Aerial Vehicles", allowedGameStates = AllowedGameStates.Playing)]
		public static void DebugGroundAllAerialVehicles()
		{
			foreach (AerialVehicleInFlight aerialVehicle in VehicleWorldObjectsHolder.Instance.AerialVehicles)
			{
				DebugLandAerialVehicle(aerialVehicle);
			}
			
			foreach (Map map in Find.Maps)
			{
				foreach (Thing thing in map.spawnedThings.ToList())
				{
					if (thing is VehicleSkyfaller vehicleSkyfaller)
					{
						vehicleSkyfaller.vehicle.CompVehicleLauncher.launchProtocol.Release();
						vehicleSkyfaller.vehicle.CompVehicleLauncher.inFlight = false;
						GenSpawn.Spawn(vehicleSkyfaller.vehicle, vehicleSkyfaller.Position, vehicleSkyfaller.Map, vehicleSkyfaller.Rotation);
						if (VehicleMod.settings.main.deployOnLanding)
						{
							vehicleSkyfaller.vehicle.CompVehicleLauncher.SetTimedDeployment();
						}
						vehicleSkyfaller.Destroy();
					}
				}
			}
		}

		public static void DebugLandAerialVehicle(AerialVehicleInFlight aerialVehicleInFlight)
		{
			List<Settlement> playerSettlements = Find.WorldObjects.Settlements.Where(s => s.Faction == Faction.OfPlayer).ToList();
			Settlement nearestSettlement = playerSettlements.MinBy(s => Ext_Math.SphericalDistance(s.DrawPos, aerialVehicleInFlight.DrawPos));
			if (nearestSettlement == null)
			{
				Log.Error($"Attempting to force land aerial vehicle without a valid settlement.");
				return;
			}
			LaunchProtocol launchProtocol = aerialVehicleInFlight.vehicle.CompVehicleLauncher.launchProtocol;
			Rot4 vehicleRotation = launchProtocol.LandingProperties?.forcedRotation ?? Rot4.Random;
			if (!CellFinderExtended.TryFindRandomCenterCell(nearestSettlement.Map, (IntVec3 cell) => !MapHelper.ImpassableOrVehicleBlocked(aerialVehicleInFlight.vehicle, nearestSettlement.Map, cell, vehicleRotation), out IntVec3 cell))
			{
				if (!CellFinderExtended.TryRadialSearchForCell(nearestSettlement.Map.Center, nearestSettlement.Map, 50, (IntVec3 cell) => !MapHelper.ImpassableOrVehicleBlocked(aerialVehicleInFlight.vehicle, nearestSettlement.Map, cell, vehicleRotation), out cell))
				{
					Log.Warning($"Could not find cell to spawn aerial vehicle.  Picking random cell.");
					cell = CellFinder.RandomCell(nearestSettlement.Map);
				}
			}
			VehicleSkyfaller_Arriving skyfaller = (VehicleSkyfaller_Arriving)ThingMaker.MakeThing(aerialVehicleInFlight.vehicle.CompVehicleLauncher.Props.skyfallerIncoming);
			skyfaller.vehicle = aerialVehicleInFlight.vehicle;

			GenSpawn.Spawn(skyfaller, cell, nearestSettlement.Map, vehicleRotation);
			aerialVehicleInFlight.Destroy();
		}
	}
}
