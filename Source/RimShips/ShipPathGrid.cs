using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Verse;

namespace Vehicles.AI
{
    public sealed class ShipPathGrid
    {
        public ShipPathGrid(Map map)
        {
            this.map = map;
            this.ResetPathGrid();
        }

        public void ResetPathGrid()
        {
            this.pathGrid = new int[this.map.cellIndices.NumGridCells];
        }

        public bool Walkable(IntVec3 loc)
        {
            return loc.InBoundsShip(this.map) && this.pathGrid[this.map.cellIndices.CellToIndex(loc)] < 10000;
        }

        public bool WalkableFast(IntVec3 loc)
        {
            return this.pathGrid[this.map.cellIndices.CellToIndex(loc)] < 10000;
        }

        public bool WalkableFast(int x, int z)
        {
            return this.pathGrid[this.map.cellIndices.CellToIndex(x, z)] < 10000;
        }

        public bool WalkableFast(int index)
        {
            return this.pathGrid[index] < 10000;
        }

        public int PerceivedPathCostAt(IntVec3 loc)
        {
            return this.pathGrid[this.map.cellIndices.CellToIndex(loc)];
        }

        public void RecalculatePerceivedPathCostUnderThing(Thing t)
        {
            if (t.def.size == IntVec2.One)
            {
                this.RecalculatePerceivedPathCostAt(t.Position);
            }
            else
            {
                CellRect cellRect = t.OccupiedRect();
                for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
                {
                    for (int j = cellRect.minX; j <= cellRect.maxX; j++)
                    {
                        IntVec3 c = new IntVec3(j, 0, i);
                        this.RecalculatePerceivedPathCostAt(c);
                    }
                }
            } 
        }

        public void RecalculatePerceivedPathCostAt(IntVec3 c)
        {
            if(!c.InBoundsShip(this.map))
            {
                return;
            }
            bool flag = this.WalkableFast(c);
            this.pathGrid[this.map.cellIndices.CellToIndex(c)] = this.CalculatedCostAt(c);
            if (this.WalkableFast(c) != flag)
            {
                MapExtensionUtility.GetExtensionToMap(this.map).getShipReachability.ClearCache();
                AccessTools.Method(type: typeof(RegionDirtyer), name: "Notify_WalkabilityChanged").Invoke(this.map.regionDirtyer, new object[] { c });
            }
        }

        public void RecalculateAllPerceivedPathCosts()
        {
            foreach (IntVec3 c in this.map.AllCells)
            {
                this.RecalculatePerceivedPathCostAt(c);
            }
        }

        public int CalculatedCostAt(IntVec3 c)
        {
            TerrainDef terrainDef = this.map.terrainGrid.TerrainAt(c);
            if (terrainDef is null || (terrainDef.passability == Traversability.Impassable && !terrainDef.IsWater) || !terrainDef.IsWater)
            {
                return ImpassableCost;
            }
            List<Thing> list = this.map.thingGrid.ThingsListAt(c);
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
            List<Thing> list = this.map.thingGrid.ThingsListAt(c);
            foreach(Thing t in list)
            {
                if(ShipPathGrid.IsPathCostIgnoreRepeater(t.def))
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
                if (ShipPathGrid.IsPathCostIgnoreRepeater(thingDef) && thingDef.passability != Traversability.Impassable)
                {
                    stringBuilder.AppendLine(thingDef.defName + " " + thingDef.pathCost);
                }
            }
            stringBuilder.AppendLine("===============NON-SHIPPATH COST IGNORE REPEATERS that are buildings with >0 pathCost ==============");
            foreach (ThingDef thingDef2 in DefDatabase<ThingDef>.AllDefs)
            {
                if (!ShipPathGrid.IsPathCostIgnoreRepeater(thingDef2) && thingDef2.passability != Traversability.Impassable && thingDef2.category == ThingCategory.Building && thingDef2.pathCost > 0)
                {
                    stringBuilder.AppendLine(thingDef2.defName + " " + thingDef2.pathCost);
                }
            }
            Log.Message(stringBuilder.ToString(), false);
        }

        private Map map;

        public int[] pathGrid;

        public const int ImpassableCost = 10000;

        public const int WaterCost = 2;
    }
}