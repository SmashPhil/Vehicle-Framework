using RimWorld;

namespace RimShips.Defs
{
    [DefOf]
    public static class WorldObjectDefOfShips
    {
        static WorldObjectDefOfShips()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WorldObjectDefOfShips));
        }

        public static WorldObjectDef DebugSettlement;

        public static WorldObjectDef DockedBoat;
    }
}
