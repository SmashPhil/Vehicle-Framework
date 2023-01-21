using System;
using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleRegionMaker
	{
		private static readonly HashSet<Thing> tmpProcessedThings = new HashSet<Thing>();

		private readonly VehicleMapping mapping;
		private readonly VehicleDef createdFor;

		private VehicleRegion newRegion;
		private VehicleRegionGrid regionGrid;

		private List<IntVec3> newRegCells = new List<IntVec3>();

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
			RegionType expectedRegionType = VehicleRegionTypeUtility.GetExpectedRegionType(root, mapping.map, createdFor);
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
				lastRegionProcess = "Registering things to region lister";
				RegisterThingsInRegionListers();
				lastRegionProcess = "Finalizing region";
				result = newRegion;
			}
			catch (Exception ex)
			{
				SmashLog.ErrorLabel(VehicleHarmony.LogLabel, $"Exception thrown while generating region at {root}. Step={lastRegionProcess} Ex ={ex.Message}");
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
				mapping.map.floodFiller.FloodFill(root, (IntVec3 x) => newRegion.extentsLimit.Contains(x) && VehicleRegionTypeUtility.GetExpectedRegionType(x, mapping.map, createdFor) == newRegion.type,
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
			for (int j = 0; j < newRegCells.Count; j++)
			{
				IntVec3 c = newRegCells[j];
				SweepInTwoDirectionsAndTryToCreateLink(Rot4.North, c);
				SweepInTwoDirectionsAndTryToCreateLink(Rot4.South, c);
				SweepInTwoDirectionsAndTryToCreateLink(Rot4.East, c);
				SweepInTwoDirectionsAndTryToCreateLink(Rot4.West, c);
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

			IntVec3 c2 = cell + potentialOtherRegionDir.FacingCell;
			if (c2.InBounds(mapping.map) && regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c2) == newRegion)
			{
				return;
			}

			RegionType expectedRegionType = VehicleRegionTypeUtility.GetExpectedRegionType(c2, mapping.map, createdFor);
			if (expectedRegionType == RegionType.None || expectedRegionType == RegionType.Portal)
			{
				return;
			}

			Rot4 rot = potentialOtherRegionDir;
			rot.Rotate(RotationDirection.Clockwise);
			int num = 0;
			int num2 = 0;
			hashSet.Add(cell);

			for (; ; )
			{
				IntVec3 intVec = cell + rot.FacingCell * (num + 1);
				if (!intVec.InBounds(mapping.map) || regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(intVec) != newRegion ||
					VehicleRegionTypeUtility.GetExpectedRegionType(intVec + potentialOtherRegionDir.FacingCell, mapping.map, createdFor) != expectedRegionType)
				{
					break;
				}
				if (!hashSet.Add(intVec))
				{
					Log.Error("We've processed the same cell twice.");
				}
				num++;
			}
			for (; ; )
			{
				IntVec3 intVec2 = cell - rot.FacingCell * (num2 + 1);
				if (!intVec2.InBounds(mapping.map) || regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(intVec2) != newRegion ||
					VehicleRegionTypeUtility.GetExpectedRegionType(intVec2 + potentialOtherRegionDir.FacingCell, mapping.map, createdFor) != expectedRegionType)
				{
					break;
				}
				if (!hashSet.Add(intVec2))
				{
					Log.Error("We've processed the same cell twice.");
				}
				num2++;
			}

			int length = num + num2 + 1;
			SpanDirection dir;
			IntVec3 root;
			if (potentialOtherRegionDir == Rot4.North)
			{
				dir = SpanDirection.East;
				root = cell - rot.FacingCell * num2;
				root.z++;
			}
			else if (potentialOtherRegionDir == Rot4.South)
			{
				dir = SpanDirection.East;
				root = cell + rot.FacingCell * num;
			}
			else if (potentialOtherRegionDir == Rot4.East)
			{
				dir = SpanDirection.North;
				root = cell + rot.FacingCell * num;
				root.x++;
			}
			else
			{
				dir = SpanDirection.North;
				root = cell - rot.FacingCell * num2;
			}
			EdgeSpan span = new EdgeSpan(root, dir, length);
			VehicleRegionLink regionLink = mapping[createdFor].VehicleRegionLinkDatabase.LinkFrom(span);
			regionLink.Register(newRegion);
			newRegion.links.Add(regionLink);
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
					VehicleRegionListersUpdater.RegisterAllAt(intVec, mapping.map, createdFor, tmpProcessedThings);
				}
			}
			tmpProcessedThings.Clear();
		}
	}
}
