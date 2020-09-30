using System.Collections.Generic;
using System.Linq;
using Vehicles.AI;
using UnityEngine;
using Verse;

namespace Vehicles
{
    public sealed class WaterRoom
    {
        public Map Map => ((int)this.mapIndex >= 0) ? Find.Maps[(int)this.mapIndex] : null;

        public RegionType RegionType => (!this.regions.Any<WaterRegion>()) ? RegionType.None : this.regions[0].type;

        public List<WaterRegion> Regions => this.regions;

        public int RegionCount => this.regions.Count;

        public bool IsHuge => this.regions.Count > 60;

        public bool Dereferenced => this.regions.Count == 0;

        public bool TouchesMapEdge => this.numRegionsTouchingMapEdge > 0;

        //Temperature

        //UsesOutdoorTemperature

        //RoomGroup

        public int CellCount
        {
            get
            {
                if(this.cachedCellCount == -1)
                {
                    this.cachedCellCount = 0;
                    foreach(WaterRegion region in this.regions)
                    {
                        this.cachedCellCount += region.CellCount;
                    }
                }
                return this.cachedCellCount;
            }
        }

        //OpenRoofCount

        //PsychologicallyOutdoors

        //OutdoorsForWork

        //Neighbors

        public List<WaterRoom> Neighbors
        {
            get
            {
                this.uniqueNeighborsSet.Clear();
                this.uniqueNeighbors.Clear();
                foreach(WaterRegion region in this.regions)
                {
                    foreach(WaterRegion region2 in region.Neighbors)
                    {
                        if(this.uniqueNeighborsSet.Add(region.Room) && region.Room != this)
                        {
                            this.uniqueNeighbors.Add(region.Room);
                        }
                    }
                }
                this.uniqueNeighborsSet.Clear();
                return this.uniqueNeighbors;
            }
        }

        public IEnumerable<IntVec3> Cells
        {
            get
            {
                foreach(WaterRegion region in this.regions)
                {
                    foreach(IntVec3 c in region.Cells)
                    {
                        yield return c;
                    }
                }
                yield break;
            }
        }

        //BorderCells

        //Owners
        //ContainedBeds
        //Fogged
        //IsDoorway
        //ContainedAndAdjacentThings
        //Role
        
        public static WaterRoom MakeNew(Map map)
        {
            WaterRoom room = new WaterRoom();
            room.mapIndex = (sbyte)map.Index;
            room.ID = WaterRoom.nextRoomID;
            WaterRoom.nextRoomID++;
            return room;
        }

        public void AddRegion(WaterRegion r)
        {
            if (this.regions.Contains(r))
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to add the same region twice to Room. region=",
                    r,
                    ", room=",
                    this
                }), false);
                return;
            }
            this.regions.Add(r);
            if (r.touchesMapEdge)
            {
                this.numRegionsTouchingMapEdge++;
            }
            if (this.regions.Count == 1)
            {
                WaterMapUtility.GetExtensionToMap(this.Map).WaterRegionGrid.allRooms.Add(this);
            }
        }

        public void RemoveRegion(WaterRegion r)
        {
            if (!this.regions.Contains(r))
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to remove region from Room but this region is not here. region=",
                    r,
                    ", room=",
                    this
                }), false);
                return;
            }
            this.regions.Remove(r);
            if (r.touchesMapEdge)
            {
                this.numRegionsTouchingMapEdge--;
            }
            if (this.regions.Count == 0)
            {
                //this.Group = null;
                /*this.cachedOpenRoofCount = -1;
                this.cachedOpenRoofState = null;
                this.statsAndRoleDirty = true;*/
                WaterMapUtility.GetExtensionToMap(this.Map).WaterRegionGrid.allRooms.Remove(this);
            }
        }

        public override int GetHashCode()
        {
            return Gen.HashCombineInt(this.ID, 1538478890);
        }

        public sbyte mapIndex = -1;

        //private WaterRoomGroup groupInt;

        private List<WaterRegion> regions = new List<WaterRegion>();

        public int ID = -16161616;

        public int lastChangeTick = -1;

        private int numRegionsTouchingMapEdge;

        //private int cachedOpenRoofCount = -1;

        //private IEnumerator<IntVec3> cachedOpenRoofState;

        public bool isPrisonCell;

        private int cachedCellCount = -1;

        //private bool statsAndRoleDirty = true;

        private DefMap<RoomStatDef, float> stats = new DefMap<RoomStatDef, float>();

        //private RoomRoleDef role;

        public int newOrReusedRoomGroupIndex = -1;

        private static int nextRoomID;

        private const int RegionCountHuge = 60;

        private const int MaxRegionsToAssignRoomRole = 36;

        private static readonly Color PrisonFieldColor = new Color(1f, 0.7f, 0.2f);

        private static readonly Color NonPrisonFieldColor = new Color(0.3f, 0.3f, 1f);

        private HashSet<WaterRoom> uniqueNeighborsSet = new HashSet<WaterRoom>();

        private List<WaterRoom> uniqueNeighbors = new List<WaterRoom>();

        private HashSet<Thing> uniqueContainedThingsSet = new HashSet<Thing>();

        private List<Thing> uniqueContainedThings = new List<Thing>();

        private static List<IntVec3> fields = new List<IntVec3>();
    }
}
