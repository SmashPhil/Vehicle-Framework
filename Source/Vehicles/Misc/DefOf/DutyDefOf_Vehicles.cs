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

		public static DutyDef PrepareVehicleCaravan_BoardVehicle;

		public static DutyDef PrepareVehicleCaravan_GatherItems;

		public static DutyDef PrepareVehicleCaravan_WaitVehicle;

		public static DutyDef PrepareVehicleCaravan_GatherDownedPawns;

		public static DutyDef PrepareVehicleCaravan_SendSlavesToVehicle;

		public static DutyDef PrepareVehicleCaravan_RopeAnimalsToVehicle;

		public static DutyDef TravelOrWaitVehicle;

		public static DutyDef TravelOrLeaveOcean;

		/* AI Duties */
		public static DutyDef ArmoredAssault;

		public static DutyDef FollowVehicle;

		public static DutyDef EscortVehicle;
	}

}
