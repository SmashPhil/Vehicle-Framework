using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;

namespace RimShips.Defs
{
    [DefOf]
    public static class StatCategoryDefOf_Ships
    {
        static StatCategoryDefOf_Ships()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StatCategoryDefOf_Ships));
        }

        public static StatCategoryDef BasicsShip;

        public static StatCategoryDef CombatShip;
    }
}
