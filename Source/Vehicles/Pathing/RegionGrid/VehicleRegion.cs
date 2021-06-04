using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public sealed class VehicleRegion
	{
		public const int GridSize = 12;

		public RegionType type = RegionType.Normal;

		public int id = -1;

		public sbyte mapIndex = -1;

		private VehicleRoom roomInt;

		public List<VehicleRegionLink> links = new List<VehicleRegionLink>();

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

		public int newRegionGroupIndex = -1;

		private Dictionary<Area, AreaOverlap> cachedAreaOverlaps;

		public int mark;

		private List<KeyValuePair<Pawn, Danger>> cachedDangers = new List<KeyValuePair<Pawn, Danger>>();

		private int cachedDangersForFrame;

		private float cachedBaseDesiredPlantsCount;

		private int cachedBaseDesiredPlantsCountForTick = -999999;

		private int debug_makeTick = -1000;

		private int debug_lastTraverseTick = -1000;

		private static int nextId = 1;

		private VehicleRegion() 
		{ 
		}

		public Map Map => (mapIndex >= 0) ? Find.Maps[mapIndex] : null;

		public IEnumerable<IntVec3> Cells
		{
			get
			{
				VehicleRegionGrid regions = Map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid;
				for(int z = extentsClose.minZ; z <= extentsClose.maxX; z++)
				{
					for(int x = extentsClose.minX; x <= extentsClose.maxX; x++)
					{
						IntVec3 c = new IntVec3(x, 0, z);
						if (regions.GetRegionAt_NoRebuild_InvalidAllowed(c) == this)
						{
							yield return c;
						}
					}
				}
				yield break;
			}
		}

		public int CellCount
		{
			get
			{
				if (cachedCellCount == -1)
				{
					cachedCellCount = Cells.Count();
				}
				return cachedCellCount;
			}
		}

		public IEnumerable<VehicleRegion> Neighbors
		{
			get
			{
				for (int li = 0; li < links.Count; li++)
				{
					VehicleRegionLink link = links[li];
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

		public IEnumerable<VehicleRegion> NeighborsOfSameType
		{
			get
			{
				for (int li = 0; li < links.Count; li++)
				{
					VehicleRegionLink link = links[li];
					for (int ri = 0; ri < 2; ri++)
					{
						if (link.regions[ri] != null && link.regions[ri] != this && link.regions[ri].type == type && link.regions[ri].valid)
						{
							yield return link.regions[ri];
						}
					}
				}
				yield break;
			}
		}

		public VehicleRoom Room
		{
			get
			{
				return roomInt;
			}
			set
			{
				if (value == roomInt) return;
				if (!(roomInt is null))
				{
					roomInt.RemoveRegion(this);
				}
				roomInt = value;
				if (!(roomInt is null))
				{
					roomInt.AddRegion(this);
				}
			}
		}

		public IntVec3 RandomCell
		{
			get
			{
				Map map = Map;
				CellIndices cellIndices = map.cellIndices;
				VehicleRegion[] directGrid = map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid.DirectGrid;
				for (int i = 0; i < 1000; i++)
				{
					IntVec3 randomCell = extentsClose.RandomCell;
					if (directGrid[cellIndices.CellToIndex(randomCell)] == this)
					{
						return randomCell;
					}
				}
				return AnyCell;
			}
		}

		public IntVec3 AnyCell
		{
			get
			{
				Map map = Map;
				CellIndices cellIndices = map.cellIndices;
				VehicleRegion[] directGrid = map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid.DirectGrid;
				foreach (IntVec3 intVec in extentsClose)
				{
					if (directGrid[cellIndices.CellToIndex(intVec)] == this)
					{
						return intVec;
					}
				}
				Log.Error("Couldn't find any cell in region " + ToString());
				return extentsClose.RandomCell;
			}
		}

		public string DebugString
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("id: " + id);
				stringBuilder.AppendLine("mapIndex: " + mapIndex);
				stringBuilder.AppendLine("links count: " + links.Count);
				foreach (VehicleRegionLink regionLink in links)
				{
					stringBuilder.AppendLine("  --" + regionLink.ToString());
				}
				stringBuilder.AppendLine("valid: " + valid.ToString());
				stringBuilder.AppendLine("makeTick: " + debug_makeTick);
				stringBuilder.AppendLine("extentsClose: " + extentsClose);
				stringBuilder.AppendLine("extentsLimit: " + extentsLimit);
				stringBuilder.AppendLine("ListerThings:");
				if (listerThings.AllThings != null)
				{
					for (int i = 0; i < listerThings.AllThings.Count; i++)
					{
						stringBuilder.AppendLine("  --" + listerThings.AllThings[i]);
					}
				}
				return stringBuilder.ToString();
			}
		}

		public bool DebugIsNew
		{
			get
			{
				return debug_makeTick > Find.TickManager.TicksGame - 60;
			}
		}

		public ListerThings ListerThings
		{
			get
			{
				return listerThings;
			}
		}

		public bool IsDoorway
		{
			get
			{
				return door != null;
			}
		}

		public static VehicleRegion MakeNewUnfilled(IntVec3 root, Map map)
		{
			VehicleRegion region = new VehicleRegion();
			region.debug_makeTick = Find.TickManager.TicksGame;
			region.id = nextId;
			nextId++;
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
			if (tp.mode != TraverseMode.PassAllDestroyableThings && tp.mode != TraverseMode.PassAllDestroyableThingsNotWater && !type.Passable())
			{
				return false;
			}
			if (tp.maxDanger < Danger.Deadly && tp.pawn != null)
			{
				Danger danger = DangerFor(tp.pawn);
				if (isDestination || danger == Danger.Deadly)
				{
					VehicleRegion region = VehicleRegionAndRoomQuery.GetRegion(tp.pawn, RegionType.Set_All);
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
						if (door == null)
						{
							return true;
						}
						ByteGrid avoidGrid = tp.pawn.GetAvoidGrid(true);
						if (avoidGrid != null && avoidGrid[door.Position] == 255)
						{
							return false;
						}
						if (tp.pawn.HostileTo(door))
						{
							return door.CanPhysicallyPass(tp.pawn) || tp.canBash;
						}
						return door.CanPhysicallyPass(tp.pawn) && !door.IsForbiddenToPass(tp.pawn);
					}
				case TraverseMode.PassDoors:
					return true;
				case TraverseMode.NoPassClosedDoors:
					return door == null || door.FreePassage;
				case TraverseMode.PassAllDestroyableThings:
					return true;
				case TraverseMode.NoPassClosedDoorsOrWater:
					return door == null || door.FreePassage;
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
				if(cachedDangersForFrame != Time.frameCount)
				{
					cachedDangers.Clear();
					cachedDangersForFrame = Time.frameCount;
				}
				else
				{
					for(int i = 0; i < cachedDangers.Count; i++)
					{
						if(cachedDangers[i].Key == p)
						{
							return cachedDangers[i].Value;
						}
					}
				}
			}
			return Danger.None; //Vehicles don't need danger detection
		}

		public float GetBaseDesiredPlantsCount(bool allowCache = true)
		{
			int ticksGame = Find.TickManager.TicksGame;
			if(allowCache && ticksGame - cachedBaseDesiredPlantsCountForTick < 2500)
			{
				return cachedBaseDesiredPlantsCount;
			}
			cachedBaseDesiredPlantsCount = 0f;
			Map map = Map;
			foreach(IntVec3 c in Cells)
			{
				cachedBaseDesiredPlantsCount += map.wildPlantSpawner.GetBaseDesiredPlantsCountAt(c);
			}
			cachedBaseDesiredPlantsCountForTick = ticksGame;
			return cachedBaseDesiredPlantsCount;
		}

		public AreaOverlap OverlapWith(Area a)
		{
			if (a.TrueCount == 0)
			{
				return AreaOverlap.None;
			}
			if (Map != a.Map)
			{
				return AreaOverlap.None;
			}
			if (cachedAreaOverlaps == null)
			{
				cachedAreaOverlaps = new Dictionary<Area, AreaOverlap>();
			}
			if(!cachedAreaOverlaps.TryGetValue(a, out AreaOverlap areaOverlap))
			{
				int num = 0;
				int num2 = 0;
				foreach(IntVec3 c in Cells)
				{
					num2++;
					if (a[c])
					{
						num++;
					}
				}
				if (num == 0)
				{
					areaOverlap = AreaOverlap.None;
				}
				else if (num == num2)
				{
					areaOverlap = AreaOverlap.Entire;
				}
				else
				{
					areaOverlap = AreaOverlap.Partial;
				}
				cachedAreaOverlaps.Add(a, areaOverlap);
			}
			return areaOverlap;
		}

		public void Notify_AreaChanged(Area a)
		{
			if (cachedAreaOverlaps is null) return;
			if (cachedAreaOverlaps.ContainsKey(a))
			{
				cachedAreaOverlaps.Remove(a);
			}
		}

		public void DecrementMapIndex()
		{
			if(mapIndex <= 0)
			{
				Log.Warning(string.Concat(new object[]
				{
					"Tried to decrement map index for water region ",
					id, ", but mapIndex=", mapIndex
				}));
				return;
			}
			mapIndex = (sbyte)(mapIndex - 1);
		}

		public void Notify_MyMapRemoved()
		{
			listerThings.Clear();
			mapIndex = -1;
		}

		public override string ToString()
		{
			string str;
			if (door != null)
			{
				str = door.ToString();
			}
			else
			{
				str = "null";
			}
			return string.Concat(new object[]
			{
				"Water Region(id=",
				id,
				", mapIndex=",
				mapIndex,
				", center=",
				extentsClose.CenterCell,
				", links=",
				links.Count,
				", cells=",
				CellCount,
				(door == null) ? null : (", portal=" + str),
				")"
			});
		}

		public void DebugDraw()
		{
			if(VehicleHarmony.debug && Find.TickManager.TicksGame < debug_lastTraverseTick + 60)
			{
				float a = 1f - (Find.TickManager.TicksGame - debug_lastTraverseTick) / 60f;
				GenDraw.DrawFieldEdges(Cells.ToList(), new Color(0f, 0f, 1f, a));
			}
		}

		public void DebugDrawMouseover()
		{
			int num = Mathf.RoundToInt(Time.realtimeSinceStartup * 2f) % 2;
			if(VehicleMod.settings.debug.debugDrawRegions)
			{
				Color color;
				if (!valid)
				{
					color = Color.red;
				}
				else if (DebugIsNew)
				{
					color = Color.yellow;
				}
				else
				{
					color = Color.green;
				}

				GenDraw.DrawFieldEdges(Cells.ToList(), color);
				foreach(VehicleRegion region in Neighbors)
				{
					GenDraw.DrawFieldEdges(region.Cells.ToList(), Color.grey);
				}

				if(VehicleMod.settings.debug.debugDrawRegionLinks)
				{
					foreach (VehicleRegionLink regionLink in links)
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
				if(VehicleMod.settings.debug.debugDrawRegionThings)
				{
					foreach (Thing thing in listerThings.AllThings)
					{
						CellRenderer.RenderSpot(thing.TrueCenter(), (thing.thingIDNumber % 256) / 256f);
					}
				}
			}
		}

		public void Debug_Notify_Traversed()
		{
			debug_lastTraverseTick = Find.TickManager.TicksGame;
		}

		public override int GetHashCode()
		{
			return precalculatedHashCode;
		}

		public override bool Equals(object obj)
		{
			return obj is VehicleRegion region && region.id == id;
		}
	}
}
