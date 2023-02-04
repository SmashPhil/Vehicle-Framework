using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Vehicle specific region for improved pathing
	/// </summary>
	public sealed class VehicleRegion
	{
		public const int GridSize = 12;

		public RegionType type = RegionType.Normal;

		public int id = -1;
		public sbyte mapIndex = -1;
		private static int nextId = 1;

		public int mark;

		private readonly VehicleDef vehicleDef;

		private VehicleRoom room;
		public Building_Door door;

		public List<VehicleRegionLink> links = new List<VehicleRegionLink>();
		public Dictionary<int, Weight> weights = new Dictionary<int, Weight>();

		private readonly List<KeyValuePair<Pawn, Danger>> cachedDangers = new List<KeyValuePair<Pawn, Danger>>();

		public uint[] closedIndex = new uint[VehicleRegionTraverser.NumWorkers];

		private readonly ListerThings listerThings = new ListerThings(ListerThingsUse.Region);

		public CellRect extentsClose;
		public CellRect extentsLimit;

		private int precalculatedHashCode;
		private int cachedCellCount = -1;

		public bool touchesMapEdge;
		public bool valid = true;

		public uint reachedIndex;
		public int newRegionGroupIndex = -1;

		private int cachedDangersForFrame;

		private int debug_makeTick = -1000;
		private int debug_lastTraverseTick = -1000;

		private VehicleRegion(VehicleDef vehicleDef) 
		{
			this.vehicleDef = vehicleDef;
		}

		/// <summary>
		/// Map getter with fallback
		/// </summary>
		public Map Map => (mapIndex >= 0) ? Find.Maps[mapIndex] : null;

		/// <summary>
		/// Yield all cells on the map
		/// </summary>
		public IEnumerable<IntVec3> Cells
		{
			get
			{
				VehicleRegionGrid regions = Map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid;
				for (int z = extentsClose.minZ; z <= extentsClose.maxZ; z++)
				{
					for (int x = extentsClose.minX; x <= extentsClose.maxX; x++)
					{
						IntVec3 cell = new IntVec3(x, 0, z);
						if (regions.GetRegionAt_NoRebuild_InvalidAllowed(cell) == this)
						{
							yield return cell;
						}
					}
				}
				yield break;
			}
		}

		/// <summary>
		/// Get total cached cell count in this region
		/// </summary>
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

		/// <summary>
		/// Get neighboring regions
		/// </summary>
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
			}
		}

		/// <summary>
		/// Get neighboring regions of the same region type
		/// </summary>
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
				if (room != null)
				{
					room.RemoveRegion(this);
				}
				room = value;
				if (room != null)
				{
					room.AddRegion(this);
				}
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

		/// <summary>
		/// Debug draw is < 1 second old
		/// </summary>
		public bool DebugIsNew
		{
			get
			{
				return debug_makeTick > Find.TickManager.TicksGame - 60;
			}
		}

		/// <summary>
		/// Get lister things
		/// </summary>
		public ListerThings ListerThings
		{
			get
			{
				return listerThings;
			}
		}

		/// <summary>
		/// Door is valid
		/// </summary>
		public bool IsDoorway
		{
			get
			{
				return door != null;
			}
		}

		public void AddLink(VehicleRegionLink regionLink)
		{
			links.Add(regionLink);
		}

		public bool RemoveLink(VehicleRegionLink regionLink)
		{
			if (links.Remove(regionLink))
			{
				RecalculateWeights();
				return true;
			}
			return false;
		}

		public Weight WeightBetween(VehicleRegionLink linkA, VehicleRegionLink linkB)
		{
			int hash = HashBetween(linkA, linkB);
			if (weights.TryGetValue(hash, out Weight weight))
			{
				return weight;
			}
			Log.Error($"Unable to pull weight between {linkA.anchor} and {linkB.anchor}");
			return new Weight(linkA, linkB, 999);
		}

		public void RecalculateWeights()
		{
			weights = new Dictionary<int, Weight>();
			for (int i = 0; i < links.Count; i++)
			{
				VehicleRegionLink regionLink = links[i];
				foreach (VehicleRegionLink connectingToLink in links)
				{
					if (regionLink == connectingToLink) continue; //Skip matching link
					
					int weight = EuclideanDistance(regionLink.anchor, connectingToLink);
					weights[HashBetween(regionLink,  connectingToLink)] = new Weight(regionLink, connectingToLink, weight);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int HashBetween(VehicleRegionLink linkA, VehicleRegionLink linkB)
		{
			return linkA.anchor.GetHashCode() + linkB.anchor.GetHashCode();
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
		/// Create new region for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="root"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		public static VehicleRegion MakeNewUnfilled(IntVec3 root, Map map, VehicleDef vehicleDef)
		{
			VehicleRegion region = new VehicleRegion(vehicleDef)
			{
				debug_makeTick = Find.TickManager.TicksGame,
				id = nextId,
				mapIndex = (sbyte)map.Index,
				precalculatedHashCode = Gen.HashCombineInt(nextId, vehicleDef.GetHashCode()),
				extentsClose = new CellRect()
				{
					minX = root.x,
					maxX = root.x,
					minZ = root.z,
					maxZ = root.z
				},
				extentsLimit = new CellRect()
				{
					minX = root.x - root.x % GridSize,
					maxX = root.x + GridSize - (root.x + GridSize) % GridSize - 1,
					minZ = root.z - root.z % GridSize,
					maxZ = root.z + GridSize - (root.z + GridSize) % GridSize - 1
				}.ClipInsideMap(map),
			};
			nextId++;
			return region;
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
			if (traverseParms.maxDanger < Danger.Deadly && traverseParms.pawn is VehiclePawn vehicle)
			{
				Danger danger = DangerFor(traverseParms.pawn);
				if (isDestination || danger == Danger.Deadly)
				{
					VehicleRegion region = VehicleRegionAndRoomQuery.GetRegion(traverseParms.pawn, vehicle.VehicleDef, RegionType.Set_All);
					if ((region == null || danger > region.DangerFor(traverseParms.pawn)) && danger > traverseParms.maxDanger)
					{
						return false;
					}
				}
			}
			switch (traverseParms.mode)
			{
				case TraverseMode.ByPawn:
					return door is null; //Vehicles cannot path through doors for now
				case TraverseMode.PassDoors:
					return true;
				case TraverseMode.NoPassClosedDoorsOrWater:
				case TraverseMode.NoPassClosedDoors:
					return door is null;
				case TraverseMode.PassAllDestroyableThings:
					return true;
				case TraverseMode.PassAllDestroyableThingsNotWater:
					return true;
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Danger path constraint for <paramref name="pawn"/>
		/// </summary>
		/// <param name="pawn"></param>
		public Danger DangerFor(Pawn pawn)
		{
			if (Current.ProgramState == ProgramState.Playing)
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
						if(cachedDangers[i].Key == pawn)
						{
							return cachedDangers[i].Value;
						}
					}
				}
			}
			return Danger.None; //Vehicles don't need danger detection right now
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
			listerThings.Clear();
			mapIndex = -1;
		}

		/// <summary>
		/// String output
		/// </summary>
		public override string ToString()
		{
			string portal = door != null ? door.ToString() : "Null";
			return $"VehicleRegion[id={id} mapIndex={mapIndex} center={extentsClose.CenterCell} links={links.Count} cells={CellCount} portal={portal}]";
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
		public void DebugDrawMouseover(DebugRegionType debugRegionType)
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
				foreach (VehicleRegionLink regionLink in links)
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
				for (int i = 0; i < links.Count; i++)
				{
					VehicleRegionLink regionLink = links[i];
					foreach (VehicleRegionLink toRegionLink in links)
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
					for (int i = 0; i < region.links.Count; i++)
					{
						VehicleRegionLink regionLink = region.links[i];
						
						foreach (VehicleRegionLink toRegionLink in region.links)
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
			if (debugRegionType.HasFlag(DebugRegionType.Things))
			{
				foreach (Thing thing in listerThings.AllThings)
				{
					CellRenderer.RenderSpot(thing.TrueCenter(), (thing.thingIDNumber % 256) / 256f, 0.15f);
				}
			}
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
		/// Set last traversal tick
		/// </summary>
		public void Debug_Notify_Traversed()
		{
			debug_lastTraverseTick = Find.TickManager.TicksGame;
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
			return obj is VehicleRegion region && region.id == id;
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
