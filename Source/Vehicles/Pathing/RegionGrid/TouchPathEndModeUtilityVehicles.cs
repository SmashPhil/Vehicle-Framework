using System.Collections.Generic;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Utility methods for touch and corner touch related PathEndModes.
	/// </summary>
	public static class TouchPathEndModeUtilityVehicles
	{
		/// <summary>
		/// Can <paramref name="vehicleDef"/> corner touch building in edifice grid at (<paramref name="cornerX"/>,<paramref name="cornerZ"/>)
		/// </summary>
		/// <param name="cornerX"></param>
		/// <param name="cornerZ"></param>
		/// <param name="adjCardinal1X"></param>
		/// <param name="adjCardinal1Z"></param>
		/// <param name="adjCardinal2X"></param>
		/// <param name="adjCardinal2Z"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		public static bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z, Map map, VehicleDef vehicleDef)
		{
			Building building = map.edificeGrid[new IntVec3(cornerX, 0, cornerZ)];
			if (building != null && MakesOccupiedCellsAlwaysReachableDiagonally(building.def))
			{
				return true;
			}
			IntVec3 intVec = new IntVec3(adjCardinal1X, 0, adjCardinal1Z);
			IntVec3 intVec2 = new IntVec3(adjCardinal2X, 0, adjCardinal2Z);
			VehicleMapping mapping = map.GetCachedMapComponent<VehicleMapping>();
			return (mapping[vehicleDef].VehiclePathGrid.Walkable(intVec) && intVec.GetDoor(map) is null) || (mapping[vehicleDef].VehiclePathGrid.Walkable(intVec2) && intVec2.GetDoor(map) is null);
		}

		/// <summary>
		/// <paramref name="def"/> is reachable through corners
		/// </summary>
		/// <param name="def"></param>
		public static bool MakesOccupiedCellsAlwaysReachableDiagonally(ThingDef def)
		{
			ThingDef thingDef = (!def.IsFrame) ? def : (def.entityDefToBuild as ThingDef);
			return thingDef != null && thingDef.CanInteractThroughCorners;
		}

		/// <summary>
		/// <paramref name="cell"/> is adjacent corner and corner touch is not allowed
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="BL"></param>
		/// <param name="TL"></param>
		/// <param name="TR"></param>
		/// <param name="BR"></param>
		/// <param name="map"></param>
		public static bool IsAdjacentCornerAndNotAllowed(IntVec3 cell, IntVec3 BL, IntVec3 TL, IntVec3 TR, IntVec3 BR, Map map, VehicleDef vehicleDef)
		{
			return (cell == BL && !IsCornerTouchAllowed(BL.x + 1, BL.z + 1, BL.x + 1, BL.z, BL.x, BL.z + 1, map, vehicleDef)) || 
				(cell == TL && !IsCornerTouchAllowed(TL.x + 1, TL.z - 1, TL.x + 1, TL.z, TL.x, TL.z - 1, map, vehicleDef)) || 
				(cell == TR && !IsCornerTouchAllowed(TR.x - 1, TR.z - 1, TR.x - 1, TR.z, TR.x, TR.z - 1, map, vehicleDef)) || 
				(cell == BR && !IsCornerTouchAllowed(BR.x - 1, BR.z + 1, BR.x - 1, BR.z, BR.x, BR.z + 1, map, vehicleDef));
		}

		/// <summary>
		/// Add adjacent regions that are allowed and fit corner touch criteria
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="traverseParams"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="regions"></param>
		public static void AddAllowedAdjacentRegions(LocalTargetInfo dest, TraverseParms traverseParams, Map map, VehicleDef vehicleDef, List<VehicleRegion> regions)
		{
			VehicleMapping mapping = map.GetCachedMapComponent<VehicleMapping>();
			GenAdj.GetAdjacentCorners(dest, out IntVec3 bl, out IntVec3 tl, out IntVec3 tr, out IntVec3 br);
			if (!dest.HasThing || (dest.Thing.def.size.x == 1 && dest.Thing.def.size.z == 1))
			{
				IntVec3 cell = dest.Cell;
				for (int i = 0; i < 8; i++)
				{
					IntVec3 intVec = GenAdj.AdjacentCells[i] + cell;
					if (intVec.InBounds(map) && !IsAdjacentCornerAndNotAllowed(intVec, bl, tl, tr, br, map, vehicleDef))
					{
						VehicleRegion region = VehicleRegionAndRoomQuery.RegionAt(intVec, mapping, vehicleDef, RegionType.Set_Passable);
						if (region != null && region.Allows(traverseParams, true))
						{
							regions.Add(region);
						}
					}
				}
			}
			else
			{
				List<IntVec3> list = GenAdjFast.AdjacentCells8Way(dest);
				for (int j = 0; j < list.Count; j++)
				{
					if (list[j].InBounds(map) && !IsAdjacentCornerAndNotAllowed(list[j], bl, tl, tr, br, map, vehicleDef))
					{
						VehicleRegion region2 = VehicleRegionAndRoomQuery.RegionAt(list[j], mapping, vehicleDef, RegionType.Set_Passable);
						if (region2 != null && region2.Allows(traverseParams, true))
						{
							regions.Add(region2);
						}
					}
				}
			}
		}

		/// <summary>
		/// Is inside or adjacent with corner touch enabled
		/// </summary>
		/// <param name="root"></param>
		/// <param name="target"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		public static bool IsAdjacentOrInsideAndAllowedToTouch(IntVec3 root, LocalTargetInfo target, Map map, VehicleDef vehicleDef)
		{
			GenAdj.GetAdjacentCorners(target, out IntVec3 b1, out IntVec3 t1, out IntVec3 tr, out IntVec3 br);
			return root.AdjacentTo8WayOrInside(target) && !IsAdjacentCornerAndNotAllowed(root, b1, t1, tr, br, map, vehicleDef);
		}
	}
}
