using RimWorld;

namespace Vehicles
{
	[DefOf]
	public static class VehicleStatDefOf
	{
		static VehicleStatDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VehicleStatDefOf));
		}

		public static VehicleStatDef MoveSpeed;

		public static VehicleStatDef BodyIntegrity;

		public static VehicleStatDef CargoCapacity;

		public static VehicleStatDef RepairRate;

		public static VehicleStatDef FlightSpeed;

		public static VehicleStatDef FlightControl;
	}
}
