//#define USE_BUFFER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using LudeonTK;
using SmashTools;
using SmashTools.Pathfinding;
using SmashTools.Performance;
using Verse;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class VehicleRegionMaker : VehicleRegionManager
	{
		//Utilized in 1 method only called from the same thread
		private readonly HashSet<Thing> tmpProcessedThings = new HashSet<Thing>();

		private VehicleRegionGrid regionGrid;

		//Call stack is for the process of rebuilding a region, which cannot run asyncronously
		private HashSet<IntVec3> regionCells = new HashSet<IntVec3>();
		private BFS<IntVec3> floodfiller = new BFS<IntVec3>();

		internal static DropOutStack<VehicleRegion> buffer;

		/// <summary>
		/// Contains hashset for 4 rotations
		/// </summary>
		private readonly HashSet<IntVec3>[] linksProcessedAt = new HashSet<IntVec3>[]
		{
			new HashSet<IntVec3>(),
			new HashSet<IntVec3>(),
			new HashSet<IntVec3>(),
			new HashSet<IntVec3>()
		};

		private static int nextId = 1;

		static VehicleRegionMaker()
		{
			buffer = new DropOutStack<VehicleRegion>(10);
		}

		public VehicleRegionMaker(VehicleMapping mapping, VehicleDef createdFor) : base(mapping, createdFor)
		{
		}

		private bool CreatingRegions { get; set; }

		public override void PostInit()
		{
			base.PostInit();
            regionGrid = mapping[createdFor].VehicleRegionGrid;
        }

		/// <summary>
		/// Generate region at <paramref name="root"/>
		/// </summary>
		/// <param name="root"></param>
		public bool TryGenerateRegionFrom(IntVec3 root, out VehicleRegion region)
		{
			region = null;
			RegionType expectedRegionType = VehicleRegionTypeUtility.GetExpectedRegionType(root, mapping, createdFor);
			if (expectedRegionType == RegionType.None)
			{
				return false;
			}
			if (CreatingRegions)
			{
				Log.Error("Trying to generate a new region while already in the process. Nested calls not allowed.");
				return false;
			}
			CreatingRegions = true;
			regionCells.Clear();

			region = GetRegion(root, mapping.map, createdFor);
			try
			{
				region.type = expectedRegionType;

				FloodFillAndAddCells(region, root);

				CreateLinks(region);

#if !DISABLE_WEIGHTS
				region.RecalculateWeights();
#endif
			}
			catch (Exception ex)
			{
				SmashLog.ErrorLabel(VehicleHarmony.LogLabel, $"Exception thrown while generating region at {root}. Exception={ex}");
				region = null;
			}
			finally
			{
				CreatingRegions = false;
				regionCells.Clear();
			}
			return true;
		}

		private IEnumerable<IntVec3> GetFloodFillNeighbors(IntVec3 root)
		{
			IntVec3[] cardinalDirections = GenAdj.CardinalDirectionsAround;
			for (int i = 0; i < cardinalDirections.Length; i++)
			{
				yield return root + cardinalDirections[i];
			}
		}

		/// <summary>
		/// Floodfill from <paramref name="root"/> and calculate valid neighboring cells to form a new region
		/// </summary>
		/// <param name="root"></param>
		private void FloodFillAndAddCells(VehicleRegion region, IntVec3 root)
		{
			regionCells.Clear();
			if (region.type.IsOneCellRegion())
			{
				AddCell(region, root);
			}
			else
			{
				floodfiller.FloodFill(root, GetFloodFillNeighbors, canEnter: Validator, processor: Processor);
			}

			bool Validator(IntVec3 cell)
			{
				if (!cell.InBounds(mapping.map))
				{
					return false;
				}
				if (!region.extentsLimit.Contains(cell))
				{
					return false;
				}
				return VehicleRegionTypeUtility.GetExpectedRegionType(cell, mapping, createdFor) == region.type;
			}

			void Processor(IntVec3 cell)
			{
				AddCell(region, cell);
			}
		}

		/// <summary>
		/// Add cell to region currently being created
		/// </summary>
		/// <param name="cell"></param>
		private void AddCell(VehicleRegion region, IntVec3 cell)
		{
			regionGrid.SetRegionAt(cell, region);
			regionCells.Add(cell);
			if (region.extentsClose.minX > cell.x)
			{
				region.extentsClose.minX = cell.x;
			}
			if (region.extentsClose.maxX < cell.x)
			{
				region.extentsClose.maxX = cell.x;
			}
			if (region.extentsClose.minZ > cell.z)
			{
				region.extentsClose.minZ = cell.z;
			}
			if (region.extentsClose.maxZ < cell.z)
			{
				region.extentsClose.maxZ = cell.z;
			}
			if (cell.x == 0 || cell.x == mapping.map.Size.x - 1 || cell.z == 0 || cell.z == mapping.map.Size.z - 1)
			{
				region.touchesMapEdge = true;
			}
		}

		private void ClearProcessedLinks()
		{
			for (int i = 0; i < linksProcessedAt.Length; i++)
			{
				linksProcessedAt[i].Clear();
			}
		}

		/// <summary>
		/// Generate region links for region currently being created
		/// </summary>
		private void CreateLinks(VehicleRegion region)
		{
			ClearProcessedLinks();
			foreach (IntVec3 cell in regionCells)
			{
				SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.North, cell);
				SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.South, cell);
				SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.East, cell);
				SweepInTwoDirectionsAndTryToCreateLink(region, Rot4.West, cell);
			}
			ClearProcessedLinks();
		}

		/// <summary>
		/// Try to make region link with neighboring rotations as fallback
		/// </summary>
		/// <param name="potentialOtherRegionDir"></param>
		/// <param name="c"></param>
		private void SweepInTwoDirectionsAndTryToCreateLink(VehicleRegion region, Rot4 potentialOtherRegionDir, IntVec3 cell)
		{
			ProfilerWatch.Start("Initial Checks");
			if (!potentialOtherRegionDir.IsValid)
			{
				return;
			}

			HashSet<IntVec3> linksProcessed = linksProcessedAt[potentialOtherRegionDir.AsInt];
			if (linksProcessed.Contains(cell))
			{
				return;
			}

			IntVec3 facingCell = cell + potentialOtherRegionDir.FacingCell;
			if (facingCell.InBounds(mapping.map) && regionGrid.GetRegionAt(facingCell) == region)
			{
				return;
			}

			RegionType expectedRegionType = VehicleRegionTypeUtility.GetExpectedRegionType(facingCell, mapping, createdFor);
			if (expectedRegionType == RegionType.None)
			{
				return;
			}
			ProfilerWatch.Stop();

			Rot4 rotClockwise = potentialOtherRegionDir;
			rotClockwise.Rotate(RotationDirection.Clockwise);
			linksProcessed.Add(cell);

			int spanRight = 0;
			int spanUp = 0;

			ProfilerWatch.Start("Loops");
			if (!expectedRegionType.IsOneCellRegion())
			{
				for (spanRight = 0; spanRight <= VehicleRegion.GridSize; spanRight++)
				{
					IntVec3 sweepRight = cell + rotClockwise.FacingCell * (spanRight + 1);
					if (InvalidForLinking(region,sweepRight, potentialOtherRegionDir, expectedRegionType))
					{
						break;
					}
					if (!linksProcessed.Add(sweepRight))
					{
						Log.Error("Attempting to process the same cell twice.");
					}
				}
				for (spanUp = 0; spanUp <= VehicleRegion.GridSize; spanUp++)
				{
					IntVec3 sweepUp = cell - rotClockwise.FacingCell * (spanUp + 1);
					if (InvalidForLinking(region, sweepUp, potentialOtherRegionDir, expectedRegionType))
					{
						break;
					}
					if (!linksProcessed.Add(sweepUp))
					{
						Log.Error("Attempting to process the same cell twice.");
					}
				}
			}
			ProfilerWatch.Stop();

			ProfilerWatch.Start("Span");
			int length = spanRight + spanUp + 1;
			SpanDirection dir;
			IntVec3 root;
			if (potentialOtherRegionDir == Rot4.North)
			{
				dir = SpanDirection.East;
				root = cell - rotClockwise.FacingCell * spanUp;
				root.z++;
			}
			else if (potentialOtherRegionDir == Rot4.South)
			{
				dir = SpanDirection.East;
				root = cell + rotClockwise.FacingCell * spanRight;
			}
			else if (potentialOtherRegionDir == Rot4.East)
			{
				dir = SpanDirection.North;
				root = cell + rotClockwise.FacingCell * spanRight;
				root.x++;
			}
			else
			{
				dir = SpanDirection.North;
				root = cell - rotClockwise.FacingCell * spanUp;
			}
			EdgeSpan span = new EdgeSpan(root, dir, length);
			ProfilerWatch.Stop();

			ProfilerWatch.Start("Initial Checks");
			VehicleRegionLink regionLink = mapping[createdFor].VehicleRegionLinkDatabase.LinkFrom(span);
			ProfilerWatch.Stop();

			ProfilerWatch.Start("Register & And");
			regionLink.Register(region);
			region.AddLink(regionLink);
			ProfilerWatch.Stop();
		}

		[Conditional("USE_BUFFER")]
		public static void PushToBuffer(VehicleRegion region)
		{
			region.Clear();
			region.Suspended = true;
			buffer.Push(region);
		}

		public static VehicleRegion GetRegion(IntVec3 root, Map map, VehicleDef vehicleDef)
		{
#if USE_BUFFER
			if (buffer.TryPop(out VehicleRegion region))
			{
				SetNew(region, root, map, vehicleDef); //Must overwrite any remaining content, buffer is shared between maps
			}
			else
			{
				region = CreateNew(root, map, vehicleDef);
			}
#else
			VehicleRegion region = CreateNew(root, map, vehicleDef);
#endif
			return region;
		}

		/// <summary>
		/// Create new region for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="root"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		public static VehicleRegion CreateNew(IntVec3 root, Map map, VehicleDef vehicleDef)
		{
			VehicleRegion region = new VehicleRegion();
			SetNew(region, root, map, vehicleDef);
			return region;
		}

		public static void SetNew(VehicleRegion region, IntVec3 root, Map map, VehicleDef vehicleDef)
		{
			if (region == null)
			{
				Log.Warning($"Attempting to populate null region. There should be no null regions pushed to the buffer");
				return;
			}
			int id = GetRegionId();
			region.Init(vehicleDef, id);
			region.Map = map;
			region.extentsClose = new CellRect()
			{
				minX = root.x,
				maxX = root.x,
				minZ = root.z,
				maxZ = root.z
			};
			region.extentsLimit = new CellRect()
			{
				minX = root.x - root.x % VehicleRegion.GridSize,
				maxX = root.x + VehicleRegion.GridSize - (root.x + VehicleRegion.GridSize) % VehicleRegion.GridSize - 1,
				minZ = root.z - root.z % VehicleRegion.GridSize,
				maxZ = root.z + VehicleRegion.GridSize - (root.z + VehicleRegion.GridSize) % VehicleRegion.GridSize - 1
			}.ClipInsideMap(map);
		}

		private static int GetRegionId()
		{
			return Interlocked.Increment(ref nextId);
		}

		private bool InvalidForLinking(VehicleRegion region, IntVec3 cell, Rot4 rot, RegionType expectedRegionType)
		{
			//Not in bounds || Region at cell != this || Region Type != expected
			return !cell.InBounds(mapping.map) || regionGrid.GetRegionAt(cell) != region ||
						VehicleRegionTypeUtility.GetExpectedRegionType(cell + rot.FacingCell, mapping, createdFor) != expectedRegionType;
		}

		[DebugAction(VehicleHarmony.VehiclesLabel, null, allowedGameStates = AllowedGameStates.PlayingOnMap, hideInSubMenu = true)]
		private static List<DebugActionNode> ForceRegenerateRegion()
		{
			List<DebugActionNode> debugActions = new List<DebugActionNode>();
			if (!VehicleHarmony.AllMoveableVehicleDefs.NullOrEmpty())
			{
				foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
				{
					debugActions.Add(new DebugActionNode(vehicleDef.defName, DebugActionType.ToolMap)
					{
						action = delegate ()
						{
							Map map = Find.CurrentMap;
							if (map == null)
							{
								Log.Error($"Attempting to use DebugRegionOptions with null map.");
								return;
							}
							DebugHelper.Local.VehicleDef = vehicleDef;
							DebugHelper.Local.DebugType = DebugRegionType.Regions | DebugRegionType.Links;

							IntVec3 cell = UI.MouseCell();
							map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionDirtyer.Notify_WalkabilityChanged(cell);
						}
					});
				}
			}
			return debugActions;
		}
	}
}
