using RimWorld;
using Verse.AI;

namespace Vehicles
{
	[DefOf]
	public static class DutyDefOf_Vehicles
	{
		static DutyDefOf_Vehicles()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(DutyDefOf_Vehicles));
		}

		// Vehicle Caravans
		public static DutyDef PrepareVehicleCaravan_BoardVehicle;

		public static DutyDef PrepareVehicleCaravan_GatherItems;

		public static DutyDef PrepareVehicleCaravan_WaitVehicle;

		public static DutyDef PrepareVehicleCaravan_GatherDownedPawns;

		public static DutyDef PrepareVehicleCaravan_SendSlavesToVehicle;

		public static DutyDef PrepareVehicleCaravan_RopeAnimalsToVehicle;

		public static DutyDef TravelOrWaitVehicle;

		public static DutyDef FollowVehicle;

		// Vehicle NPC AI
		public static DutyDef VF_RangedAggressive;

		public static DutyDef VF_RangedSupport;

		/* Vehicle NPC AI */
		public static DutyDef VF_ArmoredAssault;

		public static DutyDef VF_EscortVehicle;
	}
}
