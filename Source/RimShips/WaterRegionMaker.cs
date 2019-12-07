using System.Collections.Generic;
using RimShips.AI;
using Verse;

namespace RimShips
{
    public class WaterRegionMaker
    {
        public WaterRegionMaker(Map map)
        {
            this.map = map;
        }

        public WaterRegion TryGenerateRegionFrom(IntVec3 root)
        {
            RegionType expectedRegionType = WaterRegionTypeUtility.GetExpectedRegionType(root, this.map);
            if (expectedRegionType == RegionType.None)
                return null;
            if(this.working)
            {
                Log.Error("Trying to generate a new water region but we are currently generating one. Nested calls are not allowed.", false);
                return null;
            }
            this.working = true;
            WaterRegion result;
            try
            {
                this.regionGrid = MapExtensionUtility.GetExtensionToMap(this.map).getWaterRegionGrid;
                this.newReg = WaterRegion.MakeNewUnfilled(root, this.map);
                this.newReg.type = expectedRegionType;
                //Portal type?
                this.FloodFillAndAddCells(root);
                this.CreateLinks();
                this.RegisterThingsInRegionListers();
                result = this.newReg;
            }
            finally
            {
                this.working = false;
            }
            return result;
        }

        private void FloodFillAndAddCells(IntVec3 root)
        {
            this.newRegCells.Clear();
            if (this.newReg.type.IsOneCellRegion())
                this.AddCell(root);
            else
            {
                this.map.floodFiller.FloodFill(root, (IntVec3 x) => this.newReg.extentsLimit.Contains(x) && WaterRegionTypeUtility.GetExpectedRegionType(x, this.map) == this.newReg.type, delegate (IntVec3 x)
                {
                    this.AddCell(x);
                }, int.MaxValue, false, null);
            }
        }

        private void AddCell(IntVec3 c)
        {
            this.regionGrid.SetRegionAt(c, this.newReg);
            this.newRegCells.Add(c);
            if (this.newReg.extentsClose.minX > c.x)
            {
                this.newReg.extentsClose.minX = c.x;
            }
            if (this.newReg.extentsClose.maxX < c.x)
            {
                this.newReg.extentsClose.maxX = c.x;
            }
            if (this.newReg.extentsClose.minZ > c.z)
            {
                this.newReg.extentsClose.minZ = c.z;
            }
            if (this.newReg.extentsClose.maxZ < c.z)
            {
                this.newReg.extentsClose.maxZ = c.z;
            }
            if (c.x == 0 || c.x == this.map.Size.x - 1 || c.z == 0 || c.z == this.map.Size.z - 1)
            {
                this.newReg.touchesMapEdge = true;
            }
        }

        private void CreateLinks()
        {
            for(int i = 0; i <  this.linksProcessedAt.Length; i++)
            {
                this.linksProcessedAt[i].Clear();
            }
            for (int j = 0; j < this.newRegCells.Count; j++)
            {
                IntVec3 c = this.newRegCells[j];
                this.SweepInTwoDirectionsAndTryToCreateLink(Rot4.North, c);
                this.SweepInTwoDirectionsAndTryToCreateLink(Rot4.South, c);
                this.SweepInTwoDirectionsAndTryToCreateLink(Rot4.East, c);
                this.SweepInTwoDirectionsAndTryToCreateLink(Rot4.West, c);
            }
        }

        private void SweepInTwoDirectionsAndTryToCreateLink(Rot4 potentialOtherRegionDir, IntVec3 c)
        {
            if (!potentialOtherRegionDir.IsValid)
                return;
            HashSet<IntVec3> hashSet = this.linksProcessedAt[potentialOtherRegionDir.AsInt];
            if (hashSet.Contains(c))
                return;
            IntVec3 c2 = c + potentialOtherRegionDir.FacingCell;
            if (c2.InBoundsShip(this.map) && this.regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c2) == this.newReg)
                return;
            RegionType expectedRegionType = WaterRegionTypeUtility.GetExpectedRegionType(c2, this.map);
            if (expectedRegionType == RegionType.None)
                return;
            Rot4 rot = potentialOtherRegionDir;
            rot.Rotate(RotationDirection.Clockwise);
            int num = 0;
            int num2 = 0;
            hashSet.Add(c);
            if(!WaterRegionTypeUtility.IsOneCellRegion(expectedRegionType))
            {
                for(;;)
                {
                    IntVec3 intVec = c + rot.FacingCell * (num + 1);
                    if (!intVec.InBoundsShip(this.map) || this.regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(intVec) != this.newReg ||
                        WaterRegionTypeUtility.GetExpectedRegionType(intVec + potentialOtherRegionDir.FacingCell, this.map) != expectedRegionType)
                        break;
                    if (!hashSet.Add(intVec))
                        Log.Error("We've processed the same cell twice.", false);
                    num++;
                }
                for(; ;)
                {
                    IntVec3 intVec2 = c - rot.FacingCell * (num2 + 1);
                    if (!intVec2.InBoundsShip(this.map) || this.regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(intVec2) != this.newReg ||
                        WaterRegionTypeUtility.GetExpectedRegionType(intVec2 + potentialOtherRegionDir.FacingCell, this.map) != expectedRegionType)
                        break;
                    if (!hashSet.Add(intVec2))
                        Log.Error("We've processed the same cell twice.", false);
                    num2++;
                }
            }
            int length = num + num2 + 1;
            SpanDirection dir;
            IntVec3 root;
            if (potentialOtherRegionDir == Rot4.North)
            {
                dir = SpanDirection.East;
                root = c - rot.FacingCell * num2;
                root.z++;
            }
            else if (potentialOtherRegionDir == Rot4.South)
            {
                dir = SpanDirection.East;
                root = c + rot.FacingCell * num;
            }
            else if (potentialOtherRegionDir == Rot4.East)
            {
                dir = SpanDirection.North;
                root = c + rot.FacingCell * num;
                root.x++;
            }
            else
            {
                dir = SpanDirection.North;
                root = c - rot.FacingCell * num2;
            }
            EdgeSpan span = new EdgeSpan(root, dir, length);
            WaterRegionLink regionLink = MapExtensionUtility.GetExtensionToMap(this.map).getWaterRegionLinkDatabase.LinkFrom(span);
            regionLink.Register(this.newReg);
            this.newReg.links.Add(regionLink);
        }

        private void RegisterThingsInRegionListers()
        {
            CellRect cellRect = this.newReg.extentsClose;
            cellRect = cellRect.ExpandedBy(1);
            cellRect.ClipInsideMap(this.map);
            WaterRegionMaker.tmpProcessedThings.Clear();
            CellRect.CellRectIterator iterator = cellRect.GetIterator();
            while(!iterator.Done())
            {
                IntVec3 intVec = iterator.Current;
                bool flag = false;
                for(int i = 0; i < 9; i++)
                {
                    IntVec3 c = intVec + GenAdj.AdjacentCellsAndInside[i];
                    if(c.InBoundsShip(this.map))
                    {
                        if(this.regionGrid.GetValidRegionAt(c) == this.newReg)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if(flag)
                {
                    WaterRegionListersUpdater.RegisterAllAt(intVec, this.map, WaterRegionMaker.tmpProcessedThings);
                }
                iterator.MoveNext();
            }
            WaterRegionMaker.tmpProcessedThings.Clear();
        }

        private Map map;

        private WaterRegion newReg;

        private List<IntVec3> newRegCells = new List<IntVec3>();

        private bool working;

        private HashSet<IntVec3>[] linksProcessedAt = new HashSet<IntVec3>[]
        {
            new HashSet<IntVec3>(),
            new HashSet<IntVec3>(),
            new HashSet<IntVec3>(),
            new HashSet<IntVec3>()
        };

        private WaterRegionGrid regionGrid;

        private static HashSet<Thing> tmpProcessedThings = new HashSet<Thing>();
    }
}
