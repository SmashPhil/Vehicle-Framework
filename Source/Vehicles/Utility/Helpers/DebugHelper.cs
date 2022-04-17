using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class DebugHelper
	{
		public static VehicleDef drawRegionsFor;
		public static DebugRegionType debugRegionType;

		/// <summary>
		/// Draw settlement debug lines that show original locations before settlement was pushed to the coastline
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static void DebugDrawSettlement(int from, int to)
		{
			PeaceTalks o = (PeaceTalks)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.DebugSettlement);
			o.Tile = from;
			o.SetFaction(Faction.OfMechanoids);
			Find.WorldObjects.Add(o);
			if (VehicleHarmony.drawPaths)
			{
				VehicleHarmony.debugLines.Add(Find.WorldPathFinder.FindPath(from, to, null, null));
			}
		}

		/// <summary>
		/// Draw water regions to show if they are valid and initialized
		/// </summary>
		/// <param name="___map"></param>
		public static void DebugDrawVehicleRegion(Map map)
		{
			if (drawRegionsFor != null)
			{
				map.GetCachedMapComponent<VehicleMapping>()[drawRegionsFor].VehicleRegionGrid.DebugDraw(debugRegionType);
			}
		}

		/// <summary>
		/// Draw path costs overlay on GUI
		/// </summary>
		/// <param name="map"></param>
		public static void DebugDrawVehiclePathCostsOverlay(Map map)
		{
			if (drawRegionsFor != null)
			{
				map.GetCachedMapComponent<VehicleMapping>()[drawRegionsFor].VehicleRegionGrid.DebugOnGUI(debugRegionType);
			}
		}
	}
}
