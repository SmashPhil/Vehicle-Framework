using System.Collections.Generic;
using System.Text;
using Verse;

namespace Vehicles
{
	public class VehicleRegionLinkDatabase
	{
		private Dictionary<ulong, VehicleRegionLink> links = new Dictionary<ulong, VehicleRegionLink>();

		public VehicleRegionLink LinkFrom(EdgeSpan span)
		{
			ulong key = span.UniqueHashCode();
			if(!links.TryGetValue(key, out VehicleRegionLink regionLink))
			{
				regionLink = new VehicleRegionLink();
				regionLink.span = span;
				links.Add(key, regionLink);
			}
			return regionLink;
		}

		public void Notify_LinkHasNoRegions(VehicleRegionLink link)
		{
			links.Remove(link.UniqueHashCode());
		}

		public void DebugLog()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach(KeyValuePair<ulong, VehicleRegionLink> keyValuePair in links)
			{
				stringBuilder.AppendLine(keyValuePair.ToString());
			}
			Log.Message(stringBuilder.ToString());
		}
	}
}
