using System.Linq;
using Vehicles.AI;
using Verse;

namespace Vehicles
{
    public class WaterRegionLink
    {
        public WaterRegion RegionA
        {
            get
            {
                return this.regions[0];
            }
            set
            {
                this.regions[0] = value;
            }
        }

        public WaterRegion RegionB
        {
            get
            {
                return this.regions[1];
            }
            set
            {
                this.regions[1] = value;
            }
        }

        public void Register(WaterRegion reg)
        {
            if(this.regions[0] == reg || this.regions[1] == reg)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to double-register water region ",
                    reg.ToString(), " in ", this
                }), false);
                return;
            }
            if (this.RegionA is null || !this.RegionA.valid)
                this.RegionA = reg;
            else if (this.RegionB is null || !this.RegionB.valid)
                this.RegionB = reg;
            else
            {
                Log.Error(string.Concat(new object[]
                {
                    "Count not register water region ",
                    reg.ToString(), " in link ", this,
                    ": > 2 water regiosn on link!\nWaterRegionA: ", this.RegionA.DebugString,
                    "\nRegionB: ", this.RegionB.DebugString
                }), false);
            }
        }

        public void Deregister(WaterRegion reg)
        {
            if(this.RegionA == reg)
            {
                this.RegionA = null;
                if(this.RegionB is null)
                {
                    MapExtensionUtility.GetExtensionToMap(reg.Map).getWaterRegionLinkDatabase.Notify_LinkHasNoRegions(this);
                }
            }
            else if(this.RegionB == reg)
            {
                this.RegionB = null;
                if(this.RegionA is null)
                {
                    MapExtensionUtility.GetExtensionToMap(reg.Map).getWaterRegionLinkDatabase.Notify_LinkHasNoRegions(this);
                }
            }
        }

        public WaterRegion GetOtherRegion(WaterRegion reg)
        {
            return (reg != this.RegionA) ? this.RegionA : this.RegionB;
        }

        public ulong UniqueHashCode()
        {
            return this.span.UniqueHashCode();
        }

        public override string ToString()
        {
            string text = (from r in this.regions
                           where !(r is null)
                           select r.id.ToString()).ToCommaList(false);
            string text2 = string.Concat(new object[]
            {
                "spawn=", this.span.ToString(), " hash=", this.UniqueHashCode()
            });
            return string.Concat(new string[]
            {
                "(", text2, ", water regions=", text, ")"
            });
        }

        public WaterRegion[] regions = new WaterRegion[2];

        public EdgeSpan span;
    }
}
