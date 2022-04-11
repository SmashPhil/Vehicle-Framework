using RimWorld;

namespace Vehicles
{
	[DefOf]
	public static class VehicleStatCategoryDefOf
	{
		static VehicleStatCategoryDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VehicleStatCategoryDefOf));
		}

		public static VehicleStatCategoryDef StatCategoryMovement;

		public static VehicleStatCategoryDef StatCategoryArmor;

		public static VehicleStatCategoryDef StatCategoryCargo;

		public static VehicleStatCategoryDef StatCategoryFuelTankIntegrity;

		public static VehicleStatCategoryDef StatCategoryFlightSpeed;

		public static VehicleStatCategoryDef StatCategoryFlightControl;
	}
}
