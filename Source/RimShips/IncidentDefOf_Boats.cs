using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;

namespace RimShips
{
    [DefOf]
    public class IncidentDefOf_Boats
    {
        static IncidentDefOf_Boats()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(IncidentDefOf_Boats));
        }

        public IncidentDef TraderBoats;
    }
}
