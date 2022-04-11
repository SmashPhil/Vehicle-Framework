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

		public static DutyDef PrepareCaravan_BoardVehicle;

		public static DutyDef PrepareVehicleCaravan_GatherItems;

		public static DutyDef PrepareCaravan_WaitVehicle;

		public static DutyDef PrepareCaravan_GatherDownedPawns;

		public static DutyDef PrepareCaravan_SendSlavesToVehicle;

		public static DutyDef TravelOrWaitVehicle;

		public static DutyDef TravelOrLeaveOcean;

		/* AI Duties */
		public static DutyDef ArmoredAssault;

		public static DutyDef FollowVehicle;

		public static DutyDef EscortVehicle;
	}

}
