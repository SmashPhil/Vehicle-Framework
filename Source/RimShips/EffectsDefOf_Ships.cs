using RimWorld;
using Verse;

namespace Vehicles.Defs
{
    [DefOf]
    public static class EffectsDefOf_Ships
    {
        static EffectsDefOf_Ships()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(EffectsDefOf_Ships));
        }

        public static ThingDef Mote_Smoke_CannonSmall;
    }
}
