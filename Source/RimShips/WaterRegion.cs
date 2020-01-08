using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using RimShips.AI;

namespace RimShips
{
    public sealed class WaterRegion
    {
        private WaterRegion() { }

        public Map Map => ((int)this.mapIndex >= 0) ? Find.Maps[(int)this.mapIndex] : null;

        public IEnumerable<IntVec3> Cells
        {
            get
            {
                WaterRegionGrid regions = MapExtensionUtility.GetExtensionToMap(this.Map).getWaterRegionGrid;
                for(int z = this.extentsClose.minZ; z <= this.extentsClose.maxX; z++)
                {
                    for(int x = this.extentsClose.minX; x <= this.extentsClose.maxX; x++)
                    {
                        IntVec3 c = new IntVec3(x, 0, z);
                        if (regions.GetRegionAt_NoRebuild_InvalidAllowed(c) == this)
                            yield return c;
                    }
                }
                yield break;
            }
        }

        public int CellCount
        {
            get
            {
                if (this.cachedCellCount == -1)
                {
                    this.cachedCellCount = this.Cells.Count<IntVec3>();
                }
                return this.cachedCellCount;
            }
        }

        public IEnumerable<WaterRegion> Neighbors
        {
            get
            {
                for (int li = 0; li < this.links.Count; li++)
                {
                    WaterRegionLink link = this.links[li];
                    for (int ri = 0; ri < 2; ri++)
                    {
                        if (link.regions[ri] != null && link.regions[ri] != this && link.regions[ri].valid)
                        {
                            yield return link.regions[ri];
                        }
                    }
                }
                yield break;
            }
        }

        public IEnumerable<WaterRegion> NeighborsOfSameType
        {
            get
            {
                for (int li = 0; li < this.links.Count; li++)
                {
                    WaterRegionLink link = this.links[li];
                    for (int ri = 0; ri < 2; ri++)
                    {
                        if (link.regions[ri] != null && link.regions[ri] != this && link.regions[ri].type == this.type && link.regions[ri].valid)
                        {
                            yield return link.regions[ri];
                        }
                    }
                }
                yield break;
            }
        }

        public WaterRoom Room
        {
            get
            {
                return this.roomInt;
            }
            set
            {
                if (value == this.roomInt)
                    return;
                if(!(this.roomInt is null))
                    this.roomInt.RemoveRegion(this);
                this.roomInt = value;
                if (!(this.roomInt is null))
                    this.roomInt.AddRegion(this);
            }
        }

        public IntVec3 RandomCell
        {
            get
            {
                Map map = this.Map;
                CellIndices cellIndices = map.cellIndices;
                WaterRegion[] directGrid = MapExtensionUtility.GetExtensionToMap(map).getWaterRegionGrid.DirectGrid;
                for (int i = 0; i < 1000; i++)
                {
                    IntVec3 randomCell = this.extentsClose.RandomCell;
                    if (directGrid[cellIndices.CellToIndex(randomCell)] == this)
                    {
                        return randomCell;
                    }
                }
                return this.AnyCell;
            }
        }

        public IntVec3 AnyCell
        {
            get
            {
                Map map = this.Map;
                CellIndices cellIndices = map.cellIndices;
                WaterRegion[] directGrid = MapExtensionUtility.GetExtensionToMap(map).getWaterRegionGrid.DirectGrid;
                CellRect.CellRectIterator iterator = this.extentsClose.GetIterator();
                while(!iterator.Done())
                {
                    IntVec3 intVec = iterator.Current;
                    if (directGrid[cellIndices.CellToIndex(intVec)] == this)
                    {
                        return intVec;
                    }
                    iterator.MoveNext();
                }
                Log.Error("Couldn't find any cell in region " + this.ToString(), false);
                return this.extentsClose.RandomCell;
            }
        }

        public string DebugString
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("id: " + this.id);
                stringBuilder.AppendLine("mapIndex: " + this.mapIndex);
                stringBuilder.AppendLine("links count: " + this.links.Count);
                foreach (WaterRegionLink regionLink in this.links)
                {
                    stringBuilder.AppendLine("  --" + regionLink.ToString());
                }
                stringBuilder.AppendLine("valid: " + this.valid.ToString());
                stringBuilder.AppendLine("makeTick: " + this.debug_makeTick);
                stringBuilder.AppendLine("extentsClose: " + this.extentsClose);
                stringBuilder.AppendLine("extentsLimit: " + this.extentsLimit);
                stringBuilder.AppendLine("ListerThings:");
                if (this.listerThings.AllThings != null)
                {
                    for (int i = 0; i < this.listerThings.AllThings.Count; i++)
                    {
                        stringBuilder.AppendLine("  --" + this.listerThings.AllThings[i]);
                    }
                }
                return stringBuilder.ToString();
            }
        }

        public bool DebugIsNew
        {
            get
            {
                return this.debug_makeTick > Find.TickManager.TicksGame - 60;
            }
        }

        public ListerThings ListerThings
        {
            get
            {
                return this.listerThings;
            }
        }

        public bool IsDoorway
        {
            get
            {
                return this.door != null;
            }
        }

        public static WaterRegion MakeNewUnfilled(IntVec3 root, Map map)
        {
            WaterRegion region = new WaterRegion();
            region.debug_makeTick = Find.TickManager.TicksGame;
            region.id = WaterRegion.nextId;
            WaterRegion.nextId++;
            region.mapIndex = (sbyte)map.Index;
            region.precalculatedHashCode = Gen.HashCombineInt(region.id, 1295813358);
            region.extentsClose.minX = root.x;
            region.extentsClose.maxX = root.x;
            region.extentsClose.minZ = root.z;
            region.extentsClose.maxZ = root.z;
            region.extentsLimit.minX = root.x - root.x % GridSize;
            region.extentsLimit.maxX = root.x + GridSize - (root.x + GridSize) % GridSize - 1;
            region.extentsLimit.minZ = root.z - root.z % GridSize;
            region.extentsLimit.maxZ = root.z + GridSize - (root.z + GridSize) % GridSize - 1;
            region.extentsLimit.ClipInsideMap(map);
            return region;
        }

        public bool Allows(TraverseParms tp, bool isDestination)
        {
            if (tp.mode != TraverseMode.PassAllDestroyableThings && tp.mode != TraverseMode.PassAllDestroyableThingsNotWater && !this.type.Passable())
            {
                return false;
            }
            if (tp.maxDanger < Danger.Deadly && tp.pawn != null)
            {
                Danger danger = this.DangerFor(tp.pawn);
                if (isDestination || danger == Danger.Deadly)
                {
                    WaterRegion region = WaterRegionAndRoomQuery.GetRegion(tp.pawn, RegionType.Set_All);
                    if ((region == null || danger > region.DangerFor(tp.pawn)) && danger > tp.maxDanger)
                    {
                        return false;
                    }
                }
            }
            switch (tp.mode)
            {
                case TraverseMode.ByPawn:
                    {
                        if (this.door == null)
                        {
                            return true;
                        }
                        ByteGrid avoidGrid = tp.pawn.GetAvoidGrid(true);
                        if (avoidGrid != null && avoidGrid[this.door.Position] == 255)
                        {
                            return false;
                        }
                        if (tp.pawn.HostileTo(this.door))
                        {
                            return this.door.CanPhysicallyPass(tp.pawn) || tp.canBash;
                        }
                        return this.door.CanPhysicallyPass(tp.pawn) && !this.door.IsForbiddenToPass(tp.pawn);
                    }
                case TraverseMode.PassDoors:
                    return true;
                case TraverseMode.NoPassClosedDoors:
                    return this.door == null || this.door.FreePassage;
                case TraverseMode.PassAllDestroyableThings:
                    return true;
                case TraverseMode.NoPassClosedDoorsOrWater:
                    return this.door == null || this.door.FreePassage;
                case TraverseMode.PassAllDestroyableThingsNotWater:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        public Danger DangerFor(Pawn p)
        {
            if(Current.ProgramState == ProgramState.Playing)
            {
                if(this.cachedDangersForFrame != Time.frameCount)
                {
                    this.cachedDangers.Clear();
                    this.cachedDangersForFrame = Time.frameCount;
                }
                else
                {
                    for(int i = 0; i < this.cachedDangers.Count; i++)
                    {
                        if(this.cachedDangers[i].Key == p)
                        {
                            return this.cachedDangers[i].Value;
                        }
                    }
                }
            }
            return Danger.None; //Ships don't need danger detection
        }

        public float GetBaseDesiredPlantsCount(bool allowCache = true)
        {
            int ticksGame = Find.TickManager.TicksGame;
            if(allowCache && ticksGame - this.cachedBaseDesiredPlantsCountForTick < 2500)
            {
                return this.cachedBaseDesiredPlantsCount;
            }
            this.cachedBaseDesiredPlantsCount = 0f;
            Map map = this.Map;
            foreach(IntVec3 c in this.Cells)
            {
                this.cachedBaseDesiredPlantsCount += map.wildPlantSpawner.GetBaseDesiredPlantsCountAt(c);
            }
            this.cachedBaseDesiredPlantsCountForTick = ticksGame;
            return this.cachedBaseDesiredPlantsCount;
        }

        public AreaOverlap OverlapWith(Area a)
        {
            if (a.TrueCount == 0)
                return AreaOverlap.None;
            if (this.Map != a.Map)
                return AreaOverlap.None;
            if (this.cachedAreaOverlaps == null)
                this.cachedAreaOverlaps = new Dictionary<Area, AreaOverlap>();
            AreaOverlap areaOverlap;
            if(!this.cachedAreaOverlaps.TryGetValue(a, out areaOverlap))
            {
                int num = 0;
                int num2 = 0;
                foreach(IntVec3 c in this.Cells)
                {
                    num2++;
                    if (a[c])
                        num++;
                }
                if (num == 0)
                    areaOverlap = AreaOverlap.None;
                else if (num == num2)
                    areaOverlap = AreaOverlap.Entire;
                else
                    areaOverlap = AreaOverlap.Partial;

                this.cachedAreaOverlaps.Add(a, areaOverlap);
            }
            return areaOverlap;
        }

        public void Notify_AreaChanged(Area a)
        {
            if (this.cachedAreaOverlaps is null)
                return;
            if (this.cachedAreaOverlaps.ContainsKey(a))
                this.cachedAreaOverlaps.Remove(a);
        }

        public void DecrementMapIndex()
        {
            if((int)this.mapIndex <= 0)
            {
                Log.Warning(string.Concat(new object[]
                {
                    "Tried to decrement map index for water region ",
                    this.id, ", but mapIndex=", this.mapIndex
                }), false);
                return;
            }
            this.mapIndex = (sbyte)((int)this.mapIndex - 1);
        }

        public void Notify_MyMapRemoved()
        {
            this.listerThings.Clear();
            this.mapIndex = -1;
        }

        public override string ToString()
        {
            string str;
            if (this.door != null)
            {
                str = this.door.ToString();
            }
            else
            {
                str = "null";
            }
            return string.Concat(new object[]
            {
                "Water Region(id=",
                this.id,
                ", mapIndex=",
                this.mapIndex,
                ", center=",
                this.extentsClose.CenterCell,
                ", links=",
                this.links.Count,
                ", cells=",
                this.CellCount,
                (this.door == null) ? null : (", portal=" + str),
                ")"
            });
        }

        public void DebugDraw()
        {
            if(ShipHarmony.debug && Find.TickManager.TicksGame < this.debug_lastTraverseTick + 60)
            {
                float a = 1f - (float)(Find.TickManager.TicksGame - this.debug_lastTraverseTick) / 60f;
                GenDraw.DrawFieldEdges(this.Cells.ToList<IntVec3>(), new Color(0f, 0f, 1f, a));
            }
        }

        public void DebugDrawMouseover()
        {
            int num = Mathf.RoundToInt(Time.realtimeSinceStartup * 2f) % 2;
            if(RimShipMod.mod.settings.debugDrawRegions)
            {
                Color color;
                if (!this.valid)
                    color = Color.red;
                else if (this.DebugIsNew)
                    color = Color.yellow;
                else
                    color = Color.green;

                GenDraw.DrawFieldEdges(this.Cells.ToList<IntVec3>(), color);
                foreach(WaterRegion region in this.Neighbors)
                {
                    GenDraw.DrawFieldEdges(region.Cells.ToList<IntVec3>(), Color.grey);
                }

                if(RimShipMod.mod.settings.debugDrawRegionLinks)
                {
                    foreach (WaterRegionLink regionLink in this.links)
                    {
                        if (num == 1)
                        {
                            foreach (IntVec3 c in regionLink.span.Cells)
                            {
                                CellRenderer.RenderCell(c, DebugSolidColorMats.MaterialOf(Color.magenta));
                            }
                        }
                    }
                }
                if(RimShipMod.mod.settings.debugDrawRegionThings)
                {
                    foreach (Thing thing in this.listerThings.AllThings)
                    {
                        CellRenderer.RenderSpot(thing.TrueCenter(), (float)(thing.thingIDNumber % 256) / 256f);
                    }
                }
            }
        }

        public void Debug_Notify_Traversed()
        {
            this.debug_lastTraverseTick = Find.TickManager.TicksGame;
        }

        public override int GetHashCode()
        {
            return this.precalculatedHashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            WaterRegion region = obj as WaterRegion;
            return !(region is null) && region.id == this.id;
        }

        public RegionType type = RegionType.Normal;

        public int id = -1;

        public sbyte mapIndex = -1;

        private WaterRoom roomInt;

        public List<WaterRegionLink> links = new List<WaterRegionLink>();

        public CellRect extentsClose;

        public CellRect extentsLimit;

        public Building_Door door;

        private int precalculatedHashCode;

        public bool touchesMapEdge;

        private int cachedCellCount = -1;

        public bool valid = true;

        private ListerThings listerThings = new ListerThings(ListerThingsUse.Region);

        public uint[] closedIndex = new uint[WaterRegionTraverser.NumWorkers];

        public uint reachedIndex;

        public int newRegionGroupInt = -1;

        private Dictionary<Area, AreaOverlap> cachedAreaOverlaps;

        public int mark;

        private List<KeyValuePair<Pawn, Danger>> cachedDangers = new List<KeyValuePair<Pawn, Danger>>();

        private int cachedDangersForFrame;

        private float cachedBaseDesiredPlantsCount;

        private int cachedBaseDesiredPlantsCountForTick = -999999;

        private int debug_makeTick = -1000;

        private int debug_lastTraverseTick = -1000;

        private static int nextId = 1;

        public const int GridSize = 12;
    }
}
