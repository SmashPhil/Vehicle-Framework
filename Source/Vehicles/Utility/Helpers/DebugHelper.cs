using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.Defs;
using Vehicles.AI;

namespace Vehicles
{
	public static class DebugHelper
	{
		public static VehicleDef drawRegionsFor;
		public static DebugRegionType debugRegionType;

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

		public static void DebugDrawVehiclePathCostsOverlay(Map map)
		{
			if (drawRegionsFor != null)
			{
				map.GetCachedMapComponent<VehicleMapping>()[drawRegionsFor].VehicleRegionGrid.DebugOnGUI(debugRegionType);
			}
		}
	}
}
