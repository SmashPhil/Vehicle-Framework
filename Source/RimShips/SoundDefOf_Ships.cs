using RimWorld;
using Verse;
namespace Vehicles.Defs
{
    [DefOf]
    public static class SoundDefOf_Ships
    {
        static SoundDefOf_Ships()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SoundDefOf_Ships));
        }

        public static SoundDef Explode_BombWater;

        public static SoundDef Explosion_PirateCannon;
    }
}
