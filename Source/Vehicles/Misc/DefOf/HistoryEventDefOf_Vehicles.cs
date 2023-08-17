using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public static class HistoryEventDefOf_Vehicles
    {
		static HistoryEventDefOf_Vehicles()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(HistoryEventDefOf_Vehicles));
		}
		[MayRequireIdeology]
		public static HistoryEventDef VF_BoardLandVehicle;
        [MayRequireIdeology]
        public static HistoryEventDef VF_BoardAirVehicle;
        [MayRequireIdeology]
        public static HistoryEventDef VF_BoardSeaVehicle;
        [MayRequireIdeology]
        public static HistoryEventDef VF_BoardUniversalVehicle;

        [MayRequireIdeology]
        public static HistoryEventDef VF_BoardedLandVehicle;
        [MayRequireIdeology]
        public static HistoryEventDef VF_BoardedAirVehicle;
        [MayRequireIdeology]
        public static HistoryEventDef VF_BoardedSeaVehicle;
        [MayRequireIdeology]
        public static HistoryEventDef VF_BoardedUniversalVehicle;

    }
}