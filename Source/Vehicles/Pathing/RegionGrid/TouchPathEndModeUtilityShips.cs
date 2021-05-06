using System.Collections.Generic;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles.AI
{
	public static class TouchPathEndModeUtilityShips
	{
		public static bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z, Map map)
		{
			Building building = map.edificeGrid[new IntVec3(cornerX, 0, cornerZ)];
			if (!(building is null) && MakesOccupiedCellsAlwaysReachableDiagonally(building.def))
				return true;
			IntVec3 intVec = new IntVec3(adjCardinal1X, 0, adjCardinal1Z);
			IntVec3 intVec2 = new IntVec3(adjCardinal2X, 0, adjCardinal2Z);
			WaterMap mapE = map.GetCachedMapComponent<WaterMap>();
			return (mapE.ShipPathGrid.Walkable(intVec) && intVec.GetDoor(map) is null) || (mapE.ShipPathGrid.Walkable(intVec2) && intVec2.GetDoor(map) is null);
		}

		public static bool MakesOccupiedCellsAlwaysReachableDiagonally(ThingDef def)
		{
			ThingDef thingDef = (!def.IsFrame) ? def : (def.entityDefToBuild as ThingDef);
			return !(thingDef is null) && thingDef.CanInteractThroughCorners;
		}

		public static bool IsAdjacentCornerAndNotAllowed(IntVec3 cell, IntVec3 BL, IntVec3 TL, IntVec3 TR, IntVec3 BR, Map map)
		{
			return (cell == BL && !IsCornerTouchAllowed(BL.x + 1, BL.z + 1, BL.x + 1, BL.z, BL.x, BL.z + 1, map)) || 
				(cell == TL && !IsCornerTouchAllowed(TL.x + 1, TL.z - 1, TL.x + 1, TL.z, TL.x, TL.z - 1, map)) || 
				(cell == TR && !IsCornerTouchAllowed(TR.x - 1, TR.z - 1, TR.x - 1, TR.z, TR.x, TR.z - 1, map)) || 
				(cell == BR && !IsCornerTouchAllowed(BR.x - 1, BR.z + 1, BR.x - 1, BR.z, BR.x, BR.z + 1, map));
		}

		public static void AddAllowedAdjacentRegions(LocalTargetInfo dest, TraverseParms traverseParams, Map map, List<WaterRegion> regions)
		{
			GenAdj.GetAdjacentCorners(dest, out IntVec3 bl, out IntVec3 tl, out IntVec3 tr, out IntVec3 br);
			if (!dest.HasThing || (dest.Thing.def.size.x == 1 && dest.Thing.def.size.z == 1))
			{
				IntVec3 cell = dest.Cell;
				for (int i = 0; i < 8; i++)
				{
					IntVec3 intVec = GenAdj.AdjacentCells[i] + cell;
					if (intVec.InBoundsShip(map) && !TouchPathEndModeUtility.IsAdjacentCornerAndNotAllowed(intVec, bl, tl, tr, br, map))
					{
						WaterRegion region = WaterGridsUtility.GetRegion(intVec, map, RegionType.Set_Passable);
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
					if (list[j].InBoundsShip(map) && !TouchPathEndModeUtility.IsAdjacentCornerAndNotAllowed(list[j], bl, tl, tr, br, map))
					{
						WaterRegion region2 = WaterGridsUtility.GetRegion(list[j], map, RegionType.Set_Passable);
						if (region2 != null && region2.Allows(traverseParams, true))
						{
							regions.Add(region2);
						}
					}
				}
			}
		}

		public static bool IsAdjacentOrInsideAndAllowedToTouch(IntVec3 root, LocalTargetInfo target, Map map)
		{
			GenAdj.GetAdjacentCorners(target, out IntVec3 b1, out IntVec3 t1, out IntVec3 tr, out IntVec3 br);
			return root.AdjacentTo8WayOrInside(target) && !IsAdjacentCornerAndNotAllowed(root, b1, t1, tr, br, map);
		}
	}
}
