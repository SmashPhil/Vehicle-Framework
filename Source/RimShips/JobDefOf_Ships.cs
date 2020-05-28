using RimWorld;
using Verse;

namespace Vehicles.Defs
{
    [DefOf]
    public static class JobDefOf_Ships
    {
        static JobDefOf_Ships()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf_Ships));
        }

        public static JobDef IdleShip;

        public static JobDef Board;

        public static JobDef PrepareCaravan_GatheringShip;

        public static JobDef CarryPawnToShip;

        public static JobDef RepairShip;

        public static JobDef CarryItemToShip;

        public static JobDef LoadUpgradeMaterials;

        public static JobDef RefuelBoat;
    }
}