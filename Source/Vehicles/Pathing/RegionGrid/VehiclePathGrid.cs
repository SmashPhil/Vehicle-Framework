using System.Collections.Generic;
using System.Text;
using Verse;
using SmashTools;

namespace Vehicles.AI
{
	public sealed class VehiclePathGrid
	{
		public const int ImpassableCost = 10000;
		public const int WaterCost = 2;

		private readonly Map map;
		public int[] pathGrid;

		public VehiclePathGrid(Map map)
		{
			this.map = map;
			ResetPathGrid();
		}

		public void ResetPathGrid()
		{
			pathGrid = new int[map.cellIndices.NumGridCells];
		}

		public bool Walkable(IntVec3 loc)
		{
			return loc.InBoundsShip(map) && pathGrid[map.cellIndices.CellToIndex(loc)] < 10000;
		}

		public bool WalkableFast(IntVec3 loc)
		{
			return pathGrid[map.cellIndices.CellToIndex(loc)] < 10000;
		}

		public bool WalkableFast(int x, int z)
		{
			return pathGrid[map.cellIndices.CellToIndex(x, z)] < 10000;
		}

		public bool WalkableFast(int index)
		{
			return pathGrid[index] < 10000;
		}

		public int PerceivedPathCostAt(IntVec3 loc)
		{
			return pathGrid[map.cellIndices.CellToIndex(loc)];
		}

		public void RecalculatePerceivedPathCostUnderThing(Thing t)
		{
			if (t.def.size == IntVec2.One)
			{
				RecalculatePerceivedPathCostAt(t.Position);
			}
			else
			{
				CellRect cellRect = t.OccupiedRect();
				for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
				{
					for (int j = cellRect.minX; j <= cellRect.maxX; j++)
					{
						IntVec3 c = new IntVec3(j, 0, i);
						RecalculatePerceivedPathCostAt(c);
					}
				}
			} 
		}

		public void RecalculatePerceivedPathCostAt(IntVec3 c)
		{
			if(!c.InBoundsShip(map))
			{
				return;
			}
			bool flag = WalkableFast(c);
			pathGrid[map.cellIndices.CellToIndex(c)] = CalculatedCostAt(c);
			if (WalkableFast(c) != flag)
			{
				map.GetCachedMapComponent<VehicleMapping>().VehicleReachability.ClearCache();
				map.GetCachedMapComponent<VehicleMapping>().VehicleRegionDirtyer.Notify_WalkabilityChanged(c);
			}
		}

		public void RecalculateAllPerceivedPathCosts()
		{
			foreach (IntVec3 c in map.AllCells)
			{
				RecalculatePerceivedPathCostAt(c);
			}
		}

		public int CalculatedCostAt(IntVec3 c)
		{
			TerrainDef terrainDef = map.terrainGrid.TerrainAt(c);
			if (terrainDef is null || (terrainDef.passability == Traversability.Impassable && !terrainDef.IsWater) || !terrainDef.IsWater)
			{
				return ImpassableCost;
			}
			List<Thing> list = map.thingGrid.ThingsListAt(c);
			foreach(Thing t in list)
			{
				if(t.def.passability == Traversability.Impassable)
				{
					return ImpassableCost;
				}
			}
			//Need More?
			return WaterCost;
		}

		private bool ContainsPathCostIgnoreRepeater(IntVec3 c)
		{
			List<Thing> list = map.thingGrid.ThingsListAt(c);
			foreach(Thing t in list)
			{
				if(IsPathCostIgnoreRepeater(t.def))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsPathCostIgnoreRepeater(ThingDef def)
		{
			return def.pathCost >= 25 && def.pathCostIgnoreRepeat;
		}

		[DebugOutput]
		public static void ThingPathCostsIgnoreRepeaters()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("===============SHIP PATH COST IGNORE REPEATERS==============");
			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
			{
				if (IsPathCostIgnoreRepeater(thingDef) && thingDef.passability != Traversability.Impassable)
				{
					stringBuilder.AppendLine(thingDef.defName + " " + thingDef.pathCost);
				}
			}
			stringBuilder.AppendLine("===============NON-SHIPPATH COST IGNORE REPEATERS that are buildings with >0 pathCost ==============");
			foreach (ThingDef thingDef2 in DefDatabase<ThingDef>.AllDefs)
			{
				if (!IsPathCostIgnoreRepeater(thingDef2) && thingDef2.passability != Traversability.Impassable && thingDef2.category == ThingCategory.Building && thingDef2.pathCost > 0)
				{
					stringBuilder.AppendLine(thingDef2.defName + " " + thingDef2.pathCost);
				}
			}
		}
	}
}