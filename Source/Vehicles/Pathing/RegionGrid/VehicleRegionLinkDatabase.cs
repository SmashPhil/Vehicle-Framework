using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Region links cache by hash code
	/// </summary>
	public class VehicleRegionLinkDatabase : VehicleRegionManager
    {
		private readonly ConcurrentDictionary<ulong, VehicleRegionLink> links = new ConcurrentDictionary<ulong, VehicleRegionLink>();

		public VehicleRegionLinkDatabase(VehicleMapping mapping, VehicleDef createdFor) : base(mapping, createdFor)
		{
		}

		/// <summary>
		/// Region link between <paramref name="span"/>
		/// </summary>
		/// <param name="span"></param>
		public VehicleRegionLink LinkFrom(EdgeSpan span)
		{
			ulong key = span.UniqueHashCode();
			if (!links.TryGetValue(key, out VehicleRegionLink regionLink))
			{
				regionLink = new VehicleRegionLink()
				{
					Span = span
				};
				links[key] = regionLink;
			}
			return regionLink;
		}

		/// <summary>
		/// Remove region link from cache
		/// </summary>
		/// <param name="link"></param>
		public void Notify_LinkHasNoRegions(VehicleRegionLink link)
		{
			links.TryRemove(link.UniqueHashCode(), out _);
		}
	}
}
