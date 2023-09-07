using System;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleRegionMaker
	{
		//Instance based, and VehicleRegionMaker exists 1 per owner per dedicated thread and only used for temporary caching within the same method, so this is thread safe.
		private readonly HashSet<Thing> tmpProcessedThings = new HashSet<Thing>();

		private readonly VehicleMapping mapping;
		private readonly VehicleDef createdFor;

		private VehicleRegion newRegion;
		private VehicleRegionGrid regionGrid;

		private ConcurrentBag<IntVec3> newRegCells = new ConcurrentBag<IntVec3>();

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

		public VehicleRegionMaker(VehicleMapping mapping, VehicleDef createdFor)
		{
			this.mapping = mapping;
			this.createdFor = createdFor;
		}

		public bool CreatingRegions { get; private set; }

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
			VehicleRegion result = null;
			string lastRegionProcess = $"Beginning region creation at {root}";
			try
			{
				lastRegionProcess = "Retrieving region grid";
				regionGrid = mapping[createdFor].VehicleRegionGrid;
				lastRegionProcess = "Creating new unfilled region";
				newRegion = VehicleRegion.MakeNewUnfilled(root, mapping.map, createdFor);
				newRegion.type = expectedRegionType;
				lastRegionProcess = "Flood filling all valid cells";
				FloodFillAndAddCells(root);
				lastRegionProcess = "Creating links";
				CreateLinks();
				lastRegionProcess = "Calculating weights";
				newRegion.RecalculateWeights();
				lastRegionProcess = "Registering things to region lister";
				RegisterThingsInRegionListers();
				lastRegionProcess = "Finalizing region";
				result = newRegion;
			}
			catch (Exception ex)
			{
				SmashLog.ErrorLabel(VehicleHarmony.LogLabel, $"Exception thrown while generating region at {root}. Step={lastRegionProcess} Exception={ex}");
			}
			finally
			{
				CreatingRegions = false;
			}
			return result;
		}

		/// <summary>
		/// Regenerate region at <paramref name="root"/>
		/// </summary>
		/// <param name="root"></param>
		public void TryRegenerateRegionFrom(IntVec3 root)
		{
			if (CreatingRegions)
			{
				Log.Error("Trying to regenerate a current water region but we are currently generating one. Nested calls are not allowed.");
				return;
			}
			CreatingRegions = true;
			try
			{
				FloodFillAndAddCells(root);
				CreateLinks();
				RegisterThingsInRegionListers();
			}
			finally
			{
				CreatingRegions = false;
			}
		}

		/// <summary>
		/// Floodfill from <paramref name="root"/> and calculate valid neighboring cells to form a new region
		/// </summary>
		/// <param name="root"></param>
		private void FloodFillAndAddCells(IntVec3 root)
		{
			newRegCells.Clear();
			if (newRegion.type.IsOneCellRegion())
			{
				AddCell(root);
			}
			else
			{
				mapping.map.floodFiller.FloodFill(root, (IntVec3 x) => newRegion.extentsLimit.Contains(x) && 
				VehicleRegionTypeUtility.GetExpectedRegionType(x, mapping, createdFor) == newRegion.type,
					delegate (IntVec3 x)
					{
						AddCell(x);
					}, int.MaxValue, false, null);
			}
		}

		/// <summary>
		/// Add cell to region currently being created
		/// </summary>
		/// <param name="cell"></param>
		private void AddCell(IntVec3 cell)
		{
			regionGrid.SetRegionAt(cell, newRegion);
			newRegCells.Add(cell);
			if (newRegion.extentsClose.minX > cell.x)
			{
				newRegion.extentsClose.minX = cell.x;
			}
			if (newRegion.extentsClose.maxX < cell.x)
			{
				newRegion.extentsClose.maxX = cell.x;
			}
			if (newRegion.extentsClose.minZ > cell.z)
			{
				newRegion.extentsClose.minZ = cell.z;
			}
			if (newRegion.extentsClose.maxZ < cell.z)
			{
				newRegion.extentsClose.maxZ = cell.z;
			}
			if (cell.x == 0 || cell.x == mapping.map.Size.x - 1 || cell.z == 0 || cell.z == mapping.map.Size.z - 1)
			{
				newRegion.touchesMapEdge = true;
			}
		}

		/// <summary>
		/// Generate region links for region currently being created
		/// </summary>
		private void CreateLinks()
		{
			for (int i = 0; i < linksProcessedAt.Length; i++)
			{
				linksProcessedAt[i].Clear();
			}
			foreach (IntVec3 cell in newRegCells)
			{
				SweepInTwoDirectionsAndTryToCreateLink(Rot4.North, cell);
				SweepInTwoDirectionsAndTryToCreateLink(Rot4.South, cell);
				SweepInTwoDirectionsAndTryToCreateLink(Rot4.East, cell);
				SweepInTwoDirectionsAndTryToCreateLink(Rot4.West, cell);
			}
		}

		/// <summary>
		/// Try to make region link with neighboring rotations as fallback
		/// </summary>
		/// <param name="potentialOtherRegionDir"></param>
		/// <param name="c"></param>
		private void SweepInTwoDirectionsAndTryToCreateLink(Rot4 potentialOtherRegionDir, IntVec3 cell)
		{
			if (!potentialOtherRegionDir.IsValid)
			{
				return;
			}

			HashSet<IntVec3> hashSet = linksProcessedAt[potentialOtherRegionDir.AsInt];
			if (hashSet.Contains(cell))
			{
				return;
			}

			IntVec3 facingCell = cell + potentialOtherRegionDir.FacingCell;
			if (facingCell.InBounds(mapping.map) && regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(facingCell) == newRegion)
			{
				return;
			}

			RegionType expectedRegionType = facingCell.GetExpectedRegionType(mapping, createdFor);
			if (expectedRegionType == RegionType.None || expectedRegionType == RegionType.Portal)
			{
				return;
			}

			Rot4 rotClockwise = potentialOtherRegionDir;
			rotClockwise.Rotate(RotationDirection.Clockwise);
			hashSet.Add(cell);

			int spanRight = 0;
			int spanUp = 0;

			if (!expectedRegionType.IsOneCellRegion())
			{
				for (spanRight = 0; spanRight <= VehicleRegion.GridSize; spanRight++)
				{
					IntVec3 sweepRight = cell + rotClockwise.FacingCell * (spanRight + 1);
					if (InvalidForLinking(sweepRight, potentialOtherRegionDir, expectedRegionType))
					{
						break;
					}
					if (!hashSet.Add(sweepRight))
					{
						Log.Error("Attempting to process the same cell twice.");
					}
				}
				for (spanUp = 0; spanUp <= VehicleRegion.GridSize; spanUp++)
				{
					IntVec3 sweepUp = cell - rotClockwise.FacingCell * (spanUp + 1);
					if (InvalidForLinking(sweepUp, potentialOtherRegionDir, expectedRegionType))
					{
						break;
					}
					if (!hashSet.Add(sweepUp))
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
			regionLink.Register(newRegion);
			newRegion.AddLink(regionLink);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool InvalidForLinking(IntVec3 cell, Rot4 rot, RegionType expectedRegionType)
		{
			//Not in bounds || Region at cell != this || Region Type != expected
			return !cell.InBounds(mapping.map) || regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(cell) != newRegion ||
						(cell + rot.FacingCell).GetExpectedRegionType(mapping, createdFor) != expectedRegionType;
		}

		/// <summary>
		/// Register all Things inside region currently being generated
		/// </summary>
		private void RegisterThingsInRegionListers()
		{
			CellRect cellRect = newRegion.extentsClose;
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
						if (regionGrid.GetValidRegionAt(c) == newRegion)
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
