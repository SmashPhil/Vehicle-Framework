using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles.Defs
{
    [DefOf]
    public static class DesignationCategoryDefOf_Vehicles
    {
        static DesignationCategoryDefOf_Vehicles()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DesignationCategoryDefOf_Vehicles));
        }

        public static DesignationCategoryDef Vehicles;
    }
}