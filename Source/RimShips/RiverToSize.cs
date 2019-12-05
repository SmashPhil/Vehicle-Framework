using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;

namespace RimShips
{
    internal static class RiverToSize
    {
        public static int GetRiverSize(this RiverDef river)
        {
            if(river is null)
                return 0;
            if(river == RiverDefOf.Creek)
                return 1;
            if (river == RiverDefOf.River)
                return 2;
            if (river == RiverDefOf.LargeRiver)
                return 3;
            if (river == RiverDefOf.HugeRiver)
                return 4;
            throw new NotImplementedException("RiverDefSize");
        }
    }
}
