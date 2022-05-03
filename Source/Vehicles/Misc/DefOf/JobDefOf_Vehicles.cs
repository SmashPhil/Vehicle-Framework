using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public static class JobDefOf_Vehicles
	{
		static JobDefOf_Vehicles()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf_Vehicles));
		}

		public static JobDef IdleVehicle;

		public static JobDef Board;

		public static JobDef PrepareCaravan_GatheringVehicle;

		public static JobDef RopeAnimalToVehicle;

		public static JobDef CarryPawnToVehicle;

		public static JobDef RepairVehicle;

		public static JobDef CarryItemToVehicle;

		public static JobDef LoadUpgradeMaterials;

		public static JobDef RefuelVehicle;

		public static JobDef RefuelVehicleAtomic;

		public static JobDef UpgradeVehicle;


		public static JobDef FollowVehicle;

		public static JobDef EscortVehicle;
	}
}