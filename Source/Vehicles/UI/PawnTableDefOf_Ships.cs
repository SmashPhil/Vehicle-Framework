using RimWorld;

namespace Vehicles.Defs
{
    [DefOf]
    public static class PawnTableDefOf_Ships
    {
        static PawnTableDefOf_Ships()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PawnTableDefOf_Ships));
        }

        public static PawnTableDef Vehicles;
    }
}
