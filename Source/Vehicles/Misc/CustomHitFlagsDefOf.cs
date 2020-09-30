using RimWorld;

namespace Vehicles.Defs
{
    [DefOf]
    public static class CustomHitFlagsDefOf
    {
        static CustomHitFlagsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CustomHitFlagsDefOf));
        }

        public static CustomHitFlags Vanilla;
    }
}
