using RimWorld;
using Verse.AI;

namespace Vehicles.Defs
{
    [DefOf]
    public static class DutyDefOf_Ships
    {
        static DutyDefOf_Ships()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DutyDefOf_Ships));
        }

        public static DutyDef PrepareCaravan_BoardShip;

        public static DutyDef PrepareCaravan_GatherShip;

        public static DutyDef PrepareCaravan_WaitShip;

        public static DutyDef PrepareCaravan_GatherDownedPawns;

        public static DutyDef PrepareCaravan_SendSlavesToShip;

        public static DutyDef TravelOrWaitOcean;

        public static DutyDef TravelOrLeaveOcean;
    }

}
