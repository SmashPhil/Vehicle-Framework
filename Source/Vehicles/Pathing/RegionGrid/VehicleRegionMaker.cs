using System;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Verse;
using LudeonTK;
using SmashTools;
using System.Diagnostics;

namespace Vehicles
{
	public class VehicleRegionMaker : VehicleRegionManager
	{
		//Utilized in 1 method only called from the main thread
		private readonly HashSet<Thing> tmpProcessedThings = new HashSet<Thing>();

		private VehicleRegionGrid regionGrid;

		private static ThreadLocal<HashSet<IntVec3>> regionCells = new ThreadLocal<HashSet<IntVec3>>(() => { return new HashSet<IntVec3>(); });

		/// <summary>
		/// Contains hashset for 4 rotations
		/// </summary>
		private static readonly HashSet<IntVec3>[] linksProcessedAt = new HashSet<IntVec3>[]
		{
			new HashSet<IntVec3>(),
			new HashSet<IntVec3>(),
			new HashSet<IntVec3>(),
			new HashSet<IntVec3>()
		};

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
		public VehicleRegion TryGenerateRegionFrom(IntVec3 root)
		{
			RegionType expectedRegionType = VehicleRegionTypeUtility.GetExpectedRegionType(root, mapping, createdFor);
			if (expectedRegionType == RegionType.None)
			{
				return null;
			}
			if (CreatingRegions)
			{
				Log.Error("Trying to generate a new water region but we are currently generating one. Nested calls are not allowed.");
				return null;
			}
			CreatingRegions = true;

			VehicleRegion region;
			try
			{
				region = VehicleRegion.MakeNewUnfilled(root, mapping.map, createdFor);
				region.type = expectedRegionType;
#if DEBUG
				DeepProfiler.Start("Floodfilling");
#endif
				FloodFillAndAddCells(region, root);
#if DEBUG
				DeepProfiler.End();
#endif

#if DEBUG
				DeepProfiler.Start("Floodfilling");
#endif
				CreateLinks(region);
#if DEBUG
				DeepProfiler.End();
#endif

#if DEBUG
				DeepProfiler.Start("Floodfilling");
#endif
				region.RecalculateWeights();
#if DEBUG
				DeepProfiler.End();
#endif
#if DEBUG
				DeepProfiler.Start("Floodfilling");
#endif
				RegisterThingsInRegionListers(region);
#if DEBUG
				DeepProfiler.End();
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
				regionCells.Value.Clear();
			}
			return region;
		}

		/// <summary>
		/// Regenerate region at <paramref name="root"/>
		/// </summary>
		/// <param name="region"></param>
		/// <param name="root"></param>
		public void TryRegenerateRegionFrom(VehicleRegion region, IntVec3 root)
		{
			if (CreatingRegions)
			{
				Log.Error("Trying to regenerate a current water region but we are currently generating one. Nested calls are not allowed.");
				return;
			}
			CreatingRegions = true;
			try
			{
				FloodFillAndAddCells(region, root);
				CreateLinks(region);
				RegisterThingsInRegionListers(region);
			}
			finally
			{
				CreatingRegions = false;
				regionCells.Value.Clear();
			}
		}

		/// <summary>
		/// Floodfill from <paramref name="root"/> and calculate valid neighboring cells to form a new region
		/// </summary>
		/// <param name="root"></param>
		private void FloodFillAndAddCells(VehicleRegion region, IntVec3 root)
		{
			regionCells.Value.Clear();
			if (region.type.IsOneCellRegion())
			{
				AddCell(region, root);
			}
			else
			{
				mapping.map.floodFiller.FloodFill(root, PassCheck, Processor);
			}

			bool PassCheck(IntVec3 cell)
			{
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
			regionCells.Value.Add(cell);
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
			foreach (IntVec3 cell in regionCells.Value)
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
			if (facingCell.InBounds(mapping.map) && regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(facingCell) == region)
			{
				return;
			}

			RegionType expectedRegionType = VehicleRegionTypeUtility.GetExpectedRegionType(facingCell, mapping, createdFor);
			if (expectedRegionType == RegionType.None || expectedRegionType == RegionType.Portal)
			{
				return;
			}

			Rot4 rotClockwise = potentialOtherRegionDir;
			rotClockwise.Rotate(RotationDirection.Clockwise);
			linksProcessed.Add(cell);

			int spanRight = 0;
			int spanUp = 0;

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
			VehicleRegionLink regionLink = mapping[createdFor].VehicleRegionLinkDatabase.LinkFrom(span);
			regionLink.Register(region);
			region.AddLink(regionLink);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool InvalidForLinking(VehicleRegion region, IntVec3 cell, Rot4 rot, RegionType expectedRegionType)
		{
			//Not in bounds || Region at cell != this || Region Type != expected
			return !cell.InBounds(mapping.map) || regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(cell) != region ||
						VehicleRegionTypeUtility.GetExpectedRegionType(cell + rot.FacingCell, mapping, createdFor) != expectedRegionType;
		}

		/// <summary>
		/// Register all Things inside region currently being generated
		/// </summary>
		private void RegisterThingsInRegionListers(VehicleRegion region)
		{
			CellRect cellRect = region.extentsClose;
			cellRect = cellRect.ExpandedBy(1);
			cellRect.ClipInsideMap(mapping.map);
			tmpProcessedThings.Clear();
			foreach (IntVec3 intVec in cellRect)
			{
				bool flag = false;
				for (int i = 0; i < 9; i++)
				{
					IntVec3 c = intVec + GenAdj.AdjacentCellsAndInside[i];
					if (c.InBounds(mapping.map))
					{
						if (regionGrid.GetValidRegionAt(c) == region)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					VehicleRegionListersUpdater.RegisterAllAt(intVec, mapping, createdFor, tmpProcessedThings);
				}
			}
			tmpProcessedThings.Clear();
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
