using System.Collections.Generic;
using Verse;

namespace RimShips
{
    public class CompProperties_Cannons : CompProperties
    {
        public CompProperties_Cannons()
        {
            this.compClass = typeof(CompCannons);
        }

        public List<CannonHandler> cannons = new List<CannonHandler>();
    }
}
