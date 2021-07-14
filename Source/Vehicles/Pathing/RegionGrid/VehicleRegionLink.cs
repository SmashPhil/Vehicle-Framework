using System.Linq;
using Verse;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	/// <summary>
	/// Link between regions for reachability determination
	/// </summary>
	public class VehicleRegionLink
	{
		public VehicleRegion[] regions = new VehicleRegion[2];

		public EdgeSpan span;

		/// <summary>
		/// Region A of link
		/// </summary>
		public VehicleRegion RegionA
		{
			get
			{
				return regions[0];
			}
			set
			{
				regions[0] = value;
			}
		}

		/// <summary>
		/// Region B of link
		/// </summary>
		public VehicleRegion RegionB
		{
			get
			{
				return regions[1];
			}
			set
			{
				regions[1] = value;
			}
		}

		/// <summary>
		/// Register region for region linking
		/// </summary>
		/// <param name="reg"></param>
		public void Register(VehicleRegion reg)
		{
			if (regions[0] == reg || regions[1] == reg)
			{
				Log.Error($"Tried to double-register vehicle region {reg} in {this}");
				return;
			}
			if (RegionA is null || !RegionA.valid)
			{
				RegionA = reg;
			}
			else if (RegionB is null || !RegionB.valid)
			{
				RegionB = reg;
			}
			else
			{
				Log.Error($"Cannot register vehicle region {reg} in link {this}: > 2 vehicle regions on link.\nRegionA={RegionA.DebugString} RegionB: {RegionB.DebugString}");
			}
		}

		/// <summary>
		/// Deregister and recache region for region linking
		/// </summary>
		/// <param name="reg"></param>
		public void Deregister(VehicleRegion region, VehicleDef vehicleDef)
		{
			if(RegionA == region)
			{
				RegionA = null;
				if (RegionB is null)
				{
					region.Map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionLinkDatabase.Notify_LinkHasNoRegions(this);
				}
			}
			else if (RegionB == region)
			{
				RegionB = null;
				if(RegionA is null)
				{
					region.Map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionLinkDatabase.Notify_LinkHasNoRegions(this);
				}
			}
		}

		/// <summary>
		/// Get opposite region linking to <paramref name="region"/>
		/// </summary>
		/// <param name="reg"></param>
		public VehicleRegion GetOtherRegion(VehicleRegion region)
		{
			return (region != RegionA) ? RegionA : RegionB;
		}

		/// <summary>
		/// Hashcode for cache data
		/// </summary>
		public ulong UniqueHashCode()
		{
			return span.UniqueHashCode();
		}

		/// <summary>
		/// String output with data
		/// </summary>
		public override string ToString()
		{
			return $"({regions.Where(region => region != null).Select(region => region.id.ToString()).ToCommaList(false)}, regions=[spawn={span}, hash={UniqueHashCode()}])";
		}
	}
}
