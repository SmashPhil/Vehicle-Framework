using RimWorld;

namespace Vehicles.Defs
{
    [DefOf]
    public static class FleshTypeDefOf_Ships
    {
        static FleshTypeDefOf_Ships()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FleshTypeDefOf_Ships));
        }

        public static FleshTypeDef WoodenShip;

        public static FleshTypeDef MetalShip;

        public static FleshTypeDef SpacerShip;
    }
}
