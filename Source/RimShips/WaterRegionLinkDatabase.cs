using System.Collections.Generic;
using System.Text;
using Verse;

namespace Vehicles
{
    public class WaterRegionLinkDatabase
    {
        public WaterRegionLink LinkFrom(EdgeSpan span)
        {
            ulong key = span.UniqueHashCode();
            WaterRegionLink regionLink;
            if(!this.links.TryGetValue(key, out regionLink))
            {
                regionLink = new WaterRegionLink();
                regionLink.span = span;
                this.links.Add(key, regionLink);
            }
            return regionLink;
        }

        public void Notify_LinkHasNoRegions(WaterRegionLink link)
        {
            this.links.Remove(link.UniqueHashCode());
        }

        public void DebugLog()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach(KeyValuePair<ulong, WaterRegionLink> keyValuePair in this.links)
            {
                stringBuilder.AppendLine(keyValuePair.ToString());
            }
            Log.Message(stringBuilder.ToString(), false);
        }

        private Dictionary<ulong, WaterRegionLink> links = new Dictionary<ulong, WaterRegionLink>();
    }
}
