using RimWorld;

namespace Vehicles
{
	[DefOf]
	public static class WorldObjectDefOfVehicles
	{
		static WorldObjectDefOfVehicles()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(WorldObjectDefOfVehicles));
		}

		public static WorldObjectDef DebugSettlement;

		public static WorldObjectDef StashedVehicle;

		public static WorldObjectDef VehicleCaravan;

		public static WorldObjectDef AerialVehicle;

		public static WorldObjectDef CrashedShipSite;
	}
}
