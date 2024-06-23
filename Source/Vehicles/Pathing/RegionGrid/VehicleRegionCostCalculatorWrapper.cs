using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Region cost calculator inner data
	/// </summary>
	public class VehicleRegionCostCalculatorWrapper
	{
		private readonly VehicleMapping mapping;
		private readonly VehicleDef vehicleDef;
		private IntVec3 endCell;

		private float moveTicksCardinal;
		private float moveTicksDiagonal;

		private VehicleRegionCostCalculator vehicleRegionCostCalculator;
		private VehicleRegion cachedRegion;
		private VehicleRegionLink cachedBestLink;
		private VehicleRegionLink cachedSecondBestLink;

		private readonly HashSet<VehicleRegion> destRegions = new HashSet<VehicleRegion>();
		
		private int cachedBestLinkCost;
		private int cachedSecondBestLinkCost;
		private bool cachedRegionIsDestination;

		public VehicleRegionCostCalculatorWrapper(VehicleMapping mapping, VehicleDef vehicleDef)
		{
			this.mapping = mapping;
			this.vehicleDef = vehicleDef;
			vehicleRegionCostCalculator = new VehicleRegionCostCalculator(mapping, this.vehicleDef);
		}

		/// <summary>
		/// Initialize cost calculator for region link traversal
		/// </summary>
		/// <param name="end"></param>
		/// <param name="traverseParms"></param>
		/// <param name="moveTicksCardinal"></param>
		/// <param name="moveTicksDiagonal"></param>
		/// <param name="avoidGrid"></param>
		/// <param name="drafted"></param>
		/// <param name="disallowedCorners"></param>
		public void Init(CellRect end, TraverseParms traverseParms, float moveTicksCardinal, float moveTicksDiagonal, ByteGrid avoidGrid, bool drafted, List<int> disallowedCorners)
		{
			this.moveTicksCardinal = moveTicksCardinal;
			this.moveTicksDiagonal = moveTicksDiagonal;
			endCell = end.CenterCell;
			cachedRegion = null;
			cachedBestLink = null;
			cachedSecondBestLink = null;
			cachedBestLinkCost = 0;
			cachedSecondBestLinkCost = 0;
			cachedRegionIsDestination = false;
			destRegions.Clear();
			if (end.Width == 1 && end.Height == 1)
			{
				VehicleRegion region = VehicleRegionAndRoomQuery.RegionAt(endCell, mapping, vehicleDef, RegionType.Set_Passable);
				if (region != null)
				{
					destRegions.Add(region);
				}
			}
			else
			{
				foreach (IntVec3 intVec in end)
				{
					if (intVec.InBounds(mapping.map) && !disallowedCorners.Contains(mapping.map.cellIndices.CellToIndex(intVec)))
					{
						VehicleRegion region2 = VehicleRegionAndRoomQuery.RegionAt(intVec, mapping, vehicleDef, RegionType.Set_Passable);
						if (region2 != null)
						{
							if (region2.Allows(traverseParms, true))
							{
								destRegions.Add(region2);
							}
						}
					}
				}
			}
			if (destRegions.Count == 0)
			{
				Log.Error("Couldn't find any destination regions. This shouldn't ever happen because we've checked reachability.");
			}
			vehicleRegionCostCalculator.Init(end, destRegions, traverseParms, moveTicksCardinal, moveTicksDiagonal, avoidGrid, drafted);
		}

		/// <summary>
		/// Calculate approximate total path cost through regions from <paramref name="cellIndex"/> to <see cref="endCell"/>
		/// </summary>
		/// <param name="cellIndex"></param>
		public int GetPathCostFromDestToRegion(int cellIndex)
		{
			VehicleRegion region = mapping[vehicleDef].VehicleRegionGrid.DirectGrid[cellIndex];
			IntVec3 cell = mapping.map.cellIndices.IndexToCell(cellIndex);
			if (region != cachedRegion)
			{
				cachedRegionIsDestination = destRegions.Contains(region);
				if (cachedRegionIsDestination)
				{
					return OctileDistanceToEnd(cell);
				}
				cachedBestLinkCost = vehicleRegionCostCalculator.GetRegionBestDistances(region, out cachedBestLink, out cachedSecondBestLink, out cachedSecondBestLinkCost);
				cachedRegion = region;
			}
			else if (cachedRegionIsDestination)
			{
				return OctileDistanceToEnd(cell);
			}
			if (cachedBestLink != null)
			{
				int num = vehicleRegionCostCalculator.RegionLinkDistance(cell, cachedBestLink, 1);
				if (cachedSecondBestLink != null)
				{
					int num2 = vehicleRegionCostCalculator.RegionLinkDistance(cell, cachedSecondBestLink, 1);
					return Mathf.Min(cachedSecondBestLinkCost + num2, cachedBestLinkCost + num) + OctileDistanceToEndEps(cell);
				}
				return cachedBestLinkCost + num + OctileDistanceToEndEps(cell);
			}
			return VehiclePathGrid.ImpassableCost;
		}

		/// <summary>
		/// Octile distance from <paramref name="cell"/> to <see cref="endCell"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <returns></returns>
		private int OctileDistanceToEnd(IntVec3 cell)
		{
			int dx = Mathf.Abs(cell.x - endCell.x);
			int dz = Mathf.Abs(cell.z - endCell.z);
			return GenMath.OctileDistance(dx, dz, Mathf.RoundToInt(moveTicksCardinal), Mathf.RoundToInt(moveTicksDiagonal));
		}

		/// <summary>
		/// Octile distance from <paramref name="cell"/> to <see cref="endCell"/> estimate
		/// </summary>
		/// <param name="cell"></param>
		private int OctileDistanceToEndEps(IntVec3 cell)
		{
			int dx = Mathf.Abs(cell.x - endCell.x);
			int dz = Mathf.Abs(cell.z - endCell.z);
			return GenMath.OctileDistance(dx, dz, 2, 3);
		}
	}
}
