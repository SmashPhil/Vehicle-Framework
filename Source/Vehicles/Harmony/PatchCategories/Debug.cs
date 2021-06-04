using System;
using HarmonyLib;
using Verse;
using RimWorld.Planet;
using SmashTools;
using Vehicles.AI;

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
			if(VehicleHarmony.debug)
			{
				VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)), prefix: null,
					postfix: new HarmonyMethod(typeof(Debug),
					nameof(DebugSettlementPaths)));
				VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Add)),
					prefix: new HarmonyMethod(typeof(Debug),
					nameof(DebugWorldObjects)));
				VehicleHarmony.Patch(original: AccessTools.Method(typeof(RegionGrid), nameof(RegionGrid.DebugDraw)), prefix: null,
					postfix: new HarmonyMethod(typeof(Debug),
					nameof(DebugDrawWaterRegion)));
			}

			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.AddMap)),
			//	postfix: new HarmonyMethod(typeof(Debug),
			//	nameof(TestMethod)));
		}

		public static void TestMethod(Map map)
		{
			try
			{

			}
			catch (Exception ex)
			{
				Log.Error($"[Test Method] Exception Thrown.\n{ex.Message}\n{ex.InnerException}\n{ex.StackTrace}");
			}
			finally
			{

			}
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
		/// Draw water regions to show if they are valid and initialized
		/// </summary>
		/// <param name="___map"></param>
		public static void DebugDrawWaterRegion(Map ___map)
		{
			___map.GetCachedMapComponent<VehicleMapping>()?.VehicleRegionGrid?.DebugDraw();
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
