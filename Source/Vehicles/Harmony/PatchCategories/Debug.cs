using System;
using System.Collections.Generic;
using System.Linq;
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

			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanArrivalTimeEstimator), nameof(CaravanArrivalTimeEstimator.EstimatedTicksToArriveToEvery)),
			//	prefix: new HarmonyMethod(typeof(Debug),
			//	nameof(TestMethod)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldGrid), nameof(WorldGrid.GetTileCenter)),
			//	finalizer: new HarmonyMethod(typeof(Debug),
			//	nameof(ExceptionCatcher)));
		}

		public static void TestMethod()
		{
            try
            {
                Log.Message($"");
            }
            catch (Exception ex)
            {
                Log.Error($"[Test Method] Exception Thrown.\n{ex.Message}\n{ex.InnerException}\n{ex.StackTrace}");
            }
		}

		public static Exception ExceptionCatcher(Exception __exception)
		{
			if (__exception != null)
			{
				SmashLog.Message($"Exception caught! <error>Ex={__exception.Message}</error>");
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
	}
}
