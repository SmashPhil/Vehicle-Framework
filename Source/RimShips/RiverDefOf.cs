using RimWorld;

namespace Vehicles
{
    [DefOf]
    public static class RiverDefOf
    {
        static RiverDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RiverDefOf));
        }

        public static RiverDef Creek;

        public static RiverDef River;

        public static RiverDef LargeRiver;

        public static RiverDef HugeRiver;
    }
}
