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

        public static FleshTypeDef WoodenVehicle;

        public static FleshTypeDef MetalVehicle;

        public static FleshTypeDef SpacerVehicle;
    }
}
