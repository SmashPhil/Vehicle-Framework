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

		//General
		public static VehicleStatDef MoveSpeed;
		public static VehicleStatDef Mass;
		public static VehicleStatDef CargoCapacity;
		public static VehicleStatDef RepairRate;
		public static VehicleStatDef BodyIntegrity;

		//Combat

		//Aerial
		public static VehicleStatDef FlightSpeed;
		public static VehicleStatDef FlightControl;
	}
}
