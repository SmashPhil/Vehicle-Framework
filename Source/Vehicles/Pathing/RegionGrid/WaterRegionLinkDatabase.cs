using System.Collections.Generic;
using System.Text;
using Verse;

namespace Vehicles
{
	public class WaterRegionLinkDatabase
	{
		private Dictionary<ulong, WaterRegionLink> links = new Dictionary<ulong, WaterRegionLink>();

		public WaterRegionLink LinkFrom(EdgeSpan span)
		{
			ulong key = span.UniqueHashCode();
			if(!links.TryGetValue(key, out WaterRegionLink regionLink))
			{
				regionLink = new WaterRegionLink();
				regionLink.span = span;
				links.Add(key, regionLink);
			}
			return regionLink;
		}

		public void Notify_LinkHasNoRegions(WaterRegionLink link)
		{
			links.Remove(link.UniqueHashCode());
		}

		public void DebugLog()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach(KeyValuePair<ulong, WaterRegionLink> keyValuePair in links)
			{
				stringBuilder.AppendLine(keyValuePair.ToString());
			}
			Log.Message(stringBuilder.ToString());
		}
	}
}
