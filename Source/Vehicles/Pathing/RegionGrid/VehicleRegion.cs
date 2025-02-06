using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Vehicle specific region for improved pathing
	/// </summary>
	public sealed class VehicleRegion
	{
		public const int GridSize = 12;

		public RegionType type = RegionType.Normal;

		private int id = -1;
		private VehicleDef vehicleDef;
		private int referenceCount;
		private int cellCount;

		private VehicleRoom room;

		private sbyte mapIndex = -1;
		private Map map;
		private VehicleMapping mapping;

		public ConcurrentSet<VehicleRegionLink> links = new ConcurrentSet<VehicleRegionLink>();
#if !DISABLE_WEIGHTS
		private Dictionary<int, Weight> weights = new Dictionary<int, Weight>();
#endif

		public ThreadLocal<uint[]> closedIndex = new(() => new uint[VehicleRegionTraverser.WorkerCount]);

		public CellRect extentsClose;
		public CellRect extentsLimit;

		public bool touchesMapEdge;
		public bool valid = true;

		public uint reachedIndex;
		public int newRegionGroupIndex = -1;
		public int mark;

		private int precalculatedHashCode;
		private int debugMakeTick = -1000;

		internal VehicleRegion()
		{
		}

		public int ID => id;

		/// <summary>
		/// Currently added to the buffer, will need replacement during Region update
		/// </summary>
		public bool Suspended { get; internal set; }

		/// <summary>
		/// Map getter with fallback
		/// </summary>
		public Map Map
		{
			get
			{
				return map;
			}
			internal set
			{
				if (map == value) return;

				map = value;
				if (map == null)
				{
					mapIndex = -1;
					return;
				}
				mapIndex = (sbyte)map.Index;
				mapping = map.GetCachedMapComponent<VehicleMapping>();
			}
		}

		public int ReferenceCount => referenceCount;

		public int CellCount
		{
			get
			{
				if (cellCount < 0)
				{
					VehicleRegionGrid regions = mapping[vehicleDef].VehicleRegionGrid;
					for (int z = extentsClose.minZ; z <= extentsClose.maxZ; z++)
					{
						for (int x = extentsClose.minX; x <= extentsClose.maxX; x++)
						{
							IntVec3 cell = new IntVec3(x, 0, z);
							if (regions.GetRegionAt(cell) == this)
							{
								Interlocked.Increment(ref cellCount);
							}
						}
					}
				}
				return cellCount;
			}
		}

		/// <summary>
		/// Yield all cells in the region
		/// </summary>
		public IEnumerable<IntVec3> Cells
		{
			get
			{
				if (Suspended)
				{
					yield break;
				}
				VehicleRegionGrid regions = mapping[vehicleDef].VehicleRegionGrid;
				for (int z = extentsClose.minZ; z <= extentsClose.maxZ; z++)
				{
					for (int x = extentsClose.minX; x <= extentsClose.maxX; x++)
					{
						IntVec3 cell = new IntVec3(x, 0, z);
						if (regions.GetRegionAt(cell) == this)
						{
							yield return cell;
						}
					}
				}
				yield break;
			}
		}

		/// <summary>
		/// Get neighboring regions
		/// </summary>
		public IEnumerable<VehicleRegion> Neighbors
		{
			get
			{
				foreach (VehicleRegionLink link in links.Keys)
				{
					for (int i = 0; i < 2; i++)
					{
						if (link.regions[i] != null && link.regions[i] != this && link.regions[i].valid)
						{
							yield return link.regions[i];
						}
					}
				}
			}
		}

		/// <summary>
		/// Get neighboring regions of the same region type
		/// </summary>
		public IEnumerable<VehicleRegion> NeighborsOfSameType
		{
			get
			{
				foreach (VehicleRegionLink link in links.Keys)
				{
					for (int i = 0; i < 2; i++)
					{
						if (link.regions[i] != null && link.regions[i] != this && link.regions[i].type == type && link.regions[i].valid)
						{
							yield return link.regions[i];
						}
					}
				}
				yield break;
			}
		}

		/// <summary>
		/// Get room associated with this region
		/// </summary>
		public VehicleRoom Room
		{
			get
			{
				return room;
			}
			set
			{
				if (value == room) return;

				room?.RemoveRegion(this);
				room = value;
				room?.AddRegion(this);
			}
		}

		/// <summary>
		/// Get random cell in this region
		/// </summary>
		public IntVec3 RandomCell
		{
			get
			{
				Map map = Map;
				CellIndices cellIndices = map.cellIndices;
				VehicleRegion[] directGrid = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid.DirectGrid;
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

		/// <summary>
		/// Get any cell in this region
		/// </summary>
		public IntVec3 AnyCell
		{
			get
			{
				Map map = Map;
				CellIndices cellIndices = map.cellIndices;
				VehicleRegion[] directGrid = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid.DirectGrid;
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

		/// <summary>
		/// Output debug string for region and region link debugging
		/// </summary>
		public string DebugString
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("id: " + id);
				stringBuilder.AppendLine("mapIndex: " + mapIndex);
				stringBuilder.AppendLine("links count: " + links.Count);
				foreach (VehicleRegionLink regionLink in links.Keys)
				{
					stringBuilder.AppendLine("  --" + regionLink.ToString());
				}
				stringBuilder.AppendLine("valid: " + valid.ToString());
				stringBuilder.AppendLine("makeTick: " + debugMakeTick);
				stringBuilder.AppendLine("extentsClose: " + extentsClose);
				stringBuilder.AppendLine("extentsLimit: " + extentsLimit);
				return stringBuilder.ToString();
			}
		}

		/// <summary>
		/// Debug draw is < 1 second old
		/// </summary>
		public bool DebugIsNew
		{
			get
			{
				return debugMakeTick > Find.TickManager.TicksGame - 60;
			}
		}

		public void Init(VehicleDef vehicleDef, int id)
		{
			this.vehicleDef = vehicleDef;
			this.id = id;
			referenceCount = 0;
			cellCount = -1;
			precalculatedHashCode = Gen.HashCombineInt(id, vehicleDef.GetHashCode());
			debugMakeTick = Find.TickManager.TicksGame;

			type = RegionType.Normal;
			extentsClose = CellRect.Empty;
			extentsLimit = CellRect.Empty;

			touchesMapEdge = false;
			valid = true;

			reachedIndex = 0;
			newRegionGroupIndex = -1;
			
			Suspended = false;
		}

		public void IncrementRefCount()
		{
			Interlocked.Increment(ref referenceCount);
		}

		public void DecrementRefCount()
		{
			Interlocked.Decrement(ref referenceCount);
			if (ReferenceCount == 0)
			{
				VehicleRegionMaker.PushToBuffer(this);
			}
		}

		public void AddLink(VehicleRegionLink regionLink)
		{
			links.Add(regionLink);
#if !DISABLE_WEIGHTS
			RecalculateWeights();
#endif
		}

		public Weight WeightBetween(VehicleRegionLink linkA, VehicleRegionLink linkB)
		{
#if !DISABLE_WEIGHTS
			int hash = HashBetween(linkA, linkB);
			lock (weights)
			{
				if (weights.TryGetValue(hash, out Weight weight))
				{
					return weight;
				}
			}
#endif
			Log.Error($"Unable to pull weight between {linkA.anchor} and {linkB.anchor}");
			return new Weight(linkA, linkB, 999);
		}

		public void Clear()
		{
			valid = false;
			Room = null;
			Map = null;

			ClearLinks();
#if !DISABLE_WEIGHTS
			ClearWeights();
#endif
		}

		public void ClearLinks()
		{
			foreach (VehicleRegionLink link in links.Keys)
			{
				link.RegionA = null;
				link.RegionB = null;
			}
			links.Clear();
		}

		public void ClearWeights()
		{
#if !DISABLE_WEIGHTS
			lock (weights)
			{
				weights.Clear();
			}
#endif
		}

		public void RecalculateWeights()
		{
#if !DISABLE_WEIGHTS
			lock (weights)
			{
				weights.Clear();
				foreach (VehicleRegionLink regionLink in links.Keys)
				{
					foreach (VehicleRegionLink connectingToLink in links.Keys)
					{
						if (regionLink == connectingToLink) continue; //Skip matching link

						int weight = EuclideanDistance(regionLink.anchor, connectingToLink);
						weights[HashBetween(regionLink, connectingToLink)] = new Weight(regionLink, connectingToLink, weight);
						weights[HashBetween(connectingToLink, regionLink)] = new Weight(connectingToLink, regionLink, weight);
					}
				}
			}
#endif
		}

		public static int HashBetween(VehicleRegionLink linkA, VehicleRegionLink linkB)
		{
			return Gen.HashCombine(linkA.anchor.GetHashCode(), linkB.anchor);
		}

		/// <summary>
		/// Doesn't take movement ticks into account
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="link"></param>
		public static int EuclideanDistance(IntVec3 cell, VehicleRegionLink link)
		{
			IntVec3 diff = cell - link.anchor;
			return Mathf.RoundToInt(Mathf.Sqrt(Mathf.Pow(diff.x, 2) + Mathf.Pow(diff.z, 2)));
		}

		/// <summary>
		/// <paramref name="traverseParms"/> allows this region
		/// </summary>
		/// <param name="traverseParms"></param>
		/// <param name="isDestination"></param>
		public bool Allows(TraverseParms traverseParms, bool isDestination)
		{
			if (traverseParms.mode != TraverseMode.PassAllDestroyableThings && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater && !type.Passable())
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Decrement map index when other map has been removed
		/// </summary>
		public void DecrementMapIndex()
		{
			if (mapIndex <= 0)
			{
				Log.Warning($"Tried to decrement map index for vehicle region {id} but mapIndex={mapIndex}");
				return;
			}
			mapIndex = (sbyte)(mapIndex - 1);
		}

		/// <summary>
		/// Clean up data after map has been removed
		/// </summary>
		public void Notify_MyMapRemoved()
		{
			mapIndex = -1;
		}

		/// <summary>
		/// String output
		/// </summary>
		public override string ToString()
		{
			return $"VehicleRegion[id={id} mapIndex={mapIndex} center={extentsClose.CenterCell} links={links.Count} cells={Cells.Count()}]";
		}

		/// <summary>
		/// Debug draw field edges of this region
		/// </summary>
		public void DebugDraw()
		{
			GenDraw.DrawFieldEdges(Cells.ToList(), new Color(0f, 0f, 1f, 0.5f));
		}

		/// <summary>
		/// Debug draw region when mouse is over
		/// </summary>
		public void DebugDraw(DebugRegionType debugRegionType)
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
			else if (!type.Passable())
			{
				color = ColorLibrary.Orange;
			}
			else
			{
				color = Color.green;
			}
			if (debugRegionType.HasFlag(DebugRegionType.Regions))
			{
				GenDraw.DrawFieldEdges(Cells.ToList(), color);
				foreach (VehicleRegion region in Neighbors)
				{
					GenDraw.DrawFieldEdges(region.Cells.ToList(), Color.grey);
				}
			}
			if (debugRegionType.HasFlag(DebugRegionType.Links))
			{
				foreach (VehicleRegionLink regionLink in links.Keys)
				{
					//Flash every other second
					if (Mathf.RoundToInt(Time.realtimeSinceStartup * 2f) % 2 == 1)
					{
						foreach (IntVec3 c in regionLink.Span.Cells)
						{
							CellRenderer.RenderCell(c, DebugSolidColorMats.MaterialOf(Color.magenta));
						}
					}
				}
			}
			if (debugRegionType.HasFlag(DebugRegionType.Weights))
			{
#if !DISABLE_WEIGHTS
				DrawWeights();
#endif
			}
		}

		private void DrawWeights()
		{
#if !DISABLE_WEIGHTS
			lock (weights)
			{
				foreach (VehicleRegionLink regionLink in links.Keys)
				{
					foreach (VehicleRegionLink toRegionLink in links.Keys)
					{
						if (regionLink == toRegionLink) continue;

						float weight = weights[HashBetween(regionLink, toRegionLink)].cost;
						Vector3 from = regionLink.anchor.ToVector3();
						from.y += AltitudeLayer.MapDataOverlay.AltitudeFor();
						Vector3 to = toRegionLink.anchor.ToVector3();
						to.y += AltitudeLayer.MapDataOverlay.AltitudeFor();
						GenDraw.DrawLineBetween(from, to, VehicleRegionLink.WeightColor(weight));
					}
				}

				foreach (VehicleRegion region in Neighbors)
				{
					foreach (VehicleRegionLink regionLink in links.Keys)
					{
						foreach (VehicleRegionLink toRegionLink in region.links.Keys)
						{
							if (regionLink == toRegionLink) continue;
							if (regionLink.RegionA != this && regionLink.RegionB != this) continue;

							float weight = region.weights[HashBetween(regionLink, toRegionLink)].cost;
							Vector3 from = regionLink.anchor.ToVector3();
							from.y += AltitudeLayer.MapDataOverlay.AltitudeFor();
							Vector3 to = toRegionLink.anchor.ToVector3();
							to.y += AltitudeLayer.MapDataOverlay.AltitudeFor();
							GenDraw.DrawLineBetween(from, to, VehicleRegionLink.WeightColor(weight));
						}
					}
				}
			}
#endif
		}

		/// <summary>
		/// Debug draw region path costs when mouse is over
		/// </summary>
		public void DebugOnGUIMouseover(DebugRegionType debugRegionType)
		{
			if ((debugRegionType & DebugRegionType.PathCosts) == DebugRegionType.PathCosts)
			{
				if (Find.CameraDriver.CurrentZoom <= CameraZoomRange.Close)
				{
					foreach (IntVec3 intVec in Cells)
					{
						Vector2 vector = intVec.ToUIPosition();
						Rect rect = new Rect(vector.x - 20f, vector.y - 20f, 40f, 40f);
						if (new Rect(0f, 0f, UI.screenWidth, UI.screenHeight).Overlaps(rect))
						{
							Widgets.Label(rect, Map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehiclePathGrid.PerceivedPathCostAt(intVec).ToString());
						}
					}
				}
			}
		}

		/// <summary>
		/// Hashcode
		/// </summary>
		public override int GetHashCode()
		{
			return precalculatedHashCode;
		}

		/// <summary>
		/// Equate regions by id
		/// </summary>
		/// <param name="obj"></param>
		public override bool Equals(object obj)
		{
			return obj is VehicleRegion region && Equals(region);
		}

		public bool Equals(VehicleRegion region)
		{
			return region?.id == id;
		}

		public static bool operator ==(VehicleRegion lhs, VehicleRegion rhs)
		{
			if (lhs is null)
			{
				return rhs is null;
			}
			return lhs.Equals(rhs);
		}

		public static bool operator !=(VehicleRegion lhs, VehicleRegion rhs)
		{
			return !(lhs == rhs);
		}

		public struct Weight
		{
			public VehicleRegionLink linkA;
			public VehicleRegionLink linkB;
			public int cost;

			public bool IsValid => linkA != null && linkB != null;

			public Weight(VehicleRegionLink linkA, VehicleRegionLink linkB, int cost)
			{
				this.linkA = linkA;
				this.linkB = linkB;
				this.cost = cost;
			}
		}
	}
}
