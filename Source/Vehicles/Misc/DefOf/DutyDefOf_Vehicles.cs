using RimWorld;
using Verse.AI;

namespace Vehicles.Defs
{
	[DefOf]
	public static class DutyDefOf_Vehicles
	{
		static DutyDefOf_Vehicles()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(DutyDefOf_Vehicles));
		}

		public static DutyDef PrepareCaravan_BoardShip;

		public static DutyDef PrepareVehicleCaravan_GatherItems;

		public static DutyDef PrepareCaravan_WaitShip;

		public static DutyDef PrepareCaravan_GatherDownedPawns;

		public static DutyDef PrepareCaravan_SendSlavesToShip;

		public static DutyDef TravelOrWaitVehicle;

		public static DutyDef TravelOrLeaveOcean;

		/* AI Duties */
		public static DutyDef ArmoredAssault;
	}

}
