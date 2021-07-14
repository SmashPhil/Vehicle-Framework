using System.Collections.Generic;
using System.Text;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Region links cache by hash code
	/// </summary>
	public class VehicleRegionLinkDatabase
	{
		private readonly Dictionary<ulong, VehicleRegionLink> links = new Dictionary<ulong, VehicleRegionLink>();

		/// <summary>
		/// Region link between <paramref name="span"/>
		/// </summary>
		/// <param name="span"></param>
		public VehicleRegionLink LinkFrom(EdgeSpan span)
		{
			ulong key = span.UniqueHashCode();
			if (!links.TryGetValue(key, out VehicleRegionLink regionLink))
			{
				regionLink = new VehicleRegionLink();
				regionLink.span = span;
				links.Add(key, regionLink);
			}
			return regionLink;
		}

		/// <summary>
		/// Remove region link from cache
		/// </summary>
		/// <param name="link"></param>
		public void Notify_LinkHasNoRegions(VehicleRegionLink link)
		{
			links.Remove(link.UniqueHashCode());
		}
	}
}
