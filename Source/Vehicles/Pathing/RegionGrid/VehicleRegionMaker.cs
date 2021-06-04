using System.Collections.Generic;
using Verse;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public class VehicleRegionMaker
	{
		private Map map;

		private VehicleRegion newReg;

		private List<IntVec3> newRegCells = new List<IntVec3>();

		private bool working;

		private VehicleRegionGrid regionGrid;

		private static HashSet<Thing> tmpProcessedThings = new HashSet<Thing>();

		private HashSet<IntVec3>[] linksProcessedAt = new HashSet<IntVec3>[]
		{
			new HashSet<IntVec3>(),
			new HashSet<IntVec3>(),
			new HashSet<IntVec3>(),
			new HashSet<IntVec3>()
		};

		public VehicleRegionMaker(Map map)
		{
			this.map = map;
		}

		public VehicleRegion TryGenerateRegionFrom(IntVec3 root)
		{
			RegionType expectedRegionType = WaterRegionTypeUtility.GetExpectedRegionType(root, this.map);
			if (expectedRegionType == RegionType.None)
			{
				return null;
			}
			if (working)
			{
				Log.Error("Trying to generate a new water region but we are currently generating one. Nested calls are not allowed.");
				return null;
			}
			working = true;
			VehicleRegion result;
			try
			{
				regionGrid = map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid;
				newReg = VehicleRegion.MakeNewUnfilled(root, map);
				newReg.type = expectedRegionType;
				//Add portal type?
				FloodFillAndAddCells(root);
				CreateLinks();
				RegisterThingsInRegionListers();
				result = newReg;
			}
			finally
			{
				working = false;
			}
			return result;
		}

		public void TryRegenerateRegionFrom(VehicleRegion region, IntVec3 root)
		{
			if (working)
			{
				Log.Error("Trying to regenerate a current water region but we are currently generating one. Nested calls are not allowed.");
				return;
			}
			working = true;
			try
			{
				FloodFillAndAddCells(root);
				CreateLinks();
				RegisterThingsInRegionListers();
			}
			finally
			{
				working = false;
			}
		}

		private void FloodFillAndAddCells(IntVec3 root)
		{
			newRegCells.Clear();
			if (newReg.type.IsOneCellRegion())
			{
				AddCell(root);
			}
			else
			{
				map.floodFiller.FloodFill(root, (IntVec3 x) => newReg.extentsLimit.Contains(x) && WaterRegionTypeUtility.GetExpectedRegionType(x, map) == newReg.type, delegate (IntVec3 x)
				{
					AddCell(x);
				}, int.MaxValue, false, null);
			}
		}

		private void AddCell(IntVec3 c)
		{
			regionGrid.SetRegionAt(c, newReg);
			newRegCells.Add(c);
			if (newReg.extentsClose.minX > c.x)
			{
				newReg.extentsClose.minX = c.x;
			}
			if (newReg.extentsClose.maxX < c.x)
			{
				newReg.extentsClose.maxX = c.x;
			}
			if (newReg.extentsClose.minZ > c.z)
			{
				newReg.extentsClose.minZ = c.z;
			}
			if (newReg.extentsClose.maxZ < c.z)
			{
				newReg.extentsClose.maxZ = c.z;
			}
			if (c.x == 0 || c.x == map.Size.x - 1 || c.z == 0 || c.z == map.Size.z - 1)
			{
				newReg.touchesMapEdge = true;
			}
		}

		private void CreateLinks()
		{
			for(int i = 0; i <  linksProcessedAt.Length; i++)
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

		private void SweepInTwoDirectionsAndTryToCreateLink(Rot4 potentialOtherRegionDir, IntVec3 c)
		{
			if (!potentialOtherRegionDir.IsValid) return;
			HashSet<IntVec3> hashSet = linksProcessedAt[potentialOtherRegionDir.AsInt];
			if (hashSet.Contains(c)) return;
			IntVec3 c2 = c + potentialOtherRegionDir.FacingCell;
			if (c2.InBoundsShip(map) && regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c2) == newReg) return;
			RegionType expectedRegionType = WaterRegionTypeUtility.GetExpectedRegionType(c2, map);
			if (expectedRegionType == RegionType.None) return;
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
					if (!intVec.InBoundsShip(map) || regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(intVec) != newReg ||
						WaterRegionTypeUtility.GetExpectedRegionType(intVec + potentialOtherRegionDir.FacingCell, map) != expectedRegionType) break;
					if (!hashSet.Add(intVec))
					{
						Log.Error("We've processed the same cell twice.");
					}
					num++;
				}
				for(; ;)
				{
					IntVec3 intVec2 = c - rot.FacingCell * (num2 + 1);
					if (!intVec2.InBoundsShip(map) || regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(intVec2) != newReg ||
						WaterRegionTypeUtility.GetExpectedRegionType(intVec2 + potentialOtherRegionDir.FacingCell, map) != expectedRegionType) break;
					if (!hashSet.Add(intVec2))
					{
						Log.Error("We've processed the same cell twice.");
					}
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
			VehicleRegionLink regionLink = map.GetCachedMapComponent<VehicleMapping>().VehicleRegionLinkDatabase.LinkFrom(span);
			regionLink.Register(newReg);
			newReg.links.Add(regionLink);
		}

		private void RegisterThingsInRegionListers()
		{
			CellRect cellRect = newReg.extentsClose;
			cellRect = cellRect.ExpandedBy(1);
			cellRect.ClipInsideMap(map);
			tmpProcessedThings.Clear();
			foreach (IntVec3 intVec in cellRect)
			{
				bool flag = false;
				for (int i = 0; i < 9; i++)
				{
					IntVec3 c = intVec + GenAdj.AdjacentCellsAndInside[i];
					if (c.InBoundsShip(map))
					{
						if (regionGrid.GetValidRegionAt(c) == newReg)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					WaterRegionListersUpdater.RegisterAllAt(intVec, map, tmpProcessedThings);
				}
			}
			tmpProcessedThings.Clear();
		}
	}
}
