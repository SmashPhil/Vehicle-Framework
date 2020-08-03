using RimWorld;

namespace Vehicles.Defs
{
    [DefOf]
    public static class WorldObjectDefOfVehicles
    {
        static WorldObjectDefOfVehicles()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WorldObjectDefOfVehicles));
        }

        public static WorldObjectDef DebugSettlement;

        public static WorldObjectDef DockedBoat;

        public static WorldObjectDef VehicleCaravan;
    }
}
