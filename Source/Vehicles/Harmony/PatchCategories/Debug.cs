using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection.Emit;
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

			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(CameraJumper), "TryJump", parameters: new Type[] { typeof(GlobalTargetInfo), typeof(CameraJumper.MovementMode) }),
			//	prefix: new HarmonyMethod(typeof(Debug),
			//	nameof(TestPrefix)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_PathFollower), "AtDestinationPosition"),
			//	postfix: new HarmonyMethod(typeof(Debug),
			//	nameof(TestPostfix)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(Thing), "ExposeData"),
			//	finalizer: new HarmonyMethod(typeof(Debug),
			//	nameof(ExceptionCatcher)));
		}

		public static void TestPrefix(GlobalTargetInfo target)
		{
			try
			{
				var adjusted = CameraJumper.GetAdjustedTarget(target);
				Log.Message($"Jumping to {adjusted} HasThing={adjusted.HasThing} HasWorldObject={adjusted.HasWorldObject} CellValid={adjusted.Cell.IsValid}");
			}
			catch (Exception ex)
			{
				Log.Error($"[Test Prefix] Exception Thrown.\n{ex.Message}\n{ex.InnerException}\n{ex.StackTrace}");
			}
		}

		public static void TestPostfix(Pawn ___pawn, LocalTargetInfo ___destination, PathEndMode ___peMode, ref bool __result)
		{
			try
			{
				//Log.Message($"Finished");
				if (!___pawn.NonHumanlikeOrWildMan())
				{
					bool result = ___pawn.CanReachImmediate(___destination, ___peMode);
					Log.Message($"Result={result} for {___destination} with {___peMode}");
				}
			}
			catch (Exception ex)
			{
				Log.Error($"[Test Postfix] Exception Thrown.\n{ex.Message}\n{ex.InnerException}\n{ex.StackTrace}");
			}
		}

		public static Exception ExceptionCatcher(Thing __instance, Exception __exception)
		{
			if (__exception != null)
			{
				SmashLog.Message($"Exception caught! <error>Ex={__exception.Message}</error> Instance: {__instance}");
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
		}

		public static void DebugLandAerialVehicle(AerialVehicleInFlight aerialVehicleInFlight)
		{
			List<Settlement> playerSettlements = Find.WorldObjects.Settlements.Where(s => s.Faction == Faction.OfPlayer).ToList();
			Settlement nearestSettlement = playerSettlements.MinBy(s => Ext_Math.SphericalDistance(s.DrawPos, aerialVehicleInFlight.DrawPos));

			LaunchProtocol launchProtocol = aerialVehicleInFlight.vehicle.CompVehicleLauncher.launchProtocol;
			Rot4 vehicleRotation = launchProtocol.LandingProperties?.forcedRotation ?? Rot4.Random;
			IntVec3 cell = CellFinderExtended.RandomCenterCell(nearestSettlement.Map, (IntVec3 cell) => !MapHelper.VehicleBlockedInPosition(aerialVehicleInFlight.vehicle, Current.Game.CurrentMap, cell, vehicleRotation));
			VehicleSkyfaller_Arriving skyfaller = (VehicleSkyfaller_Arriving)ThingMaker.MakeThing(aerialVehicleInFlight.vehicle.CompVehicleLauncher.Props.skyfallerIncoming);
			skyfaller.vehicle = aerialVehicleInFlight.vehicle;

			GenSpawn.Spawn(skyfaller, cell, nearestSettlement.Map, vehicleRotation);
			aerialVehicleInFlight.Destroy();
		}
	}
}
