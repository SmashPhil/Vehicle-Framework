using RimWorld;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf]
public static class VehicleStatDefOf
{
	// General
	public static VehicleStatDef MoveSpeed;
	public static VehicleStatDef Mass;
	public static VehicleStatDef CargoCapacity;
	public static VehicleStatDef RepairRate;
	public static VehicleStatDef BodyIntegrity;

	// Combat
	public static VehicleStatDef WorkToSabotage;

	// Aerial
	public static VehicleStatDef FlightSpeed;
	public static VehicleStatDef FlightControl;

	static VehicleStatDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(VehicleStatDefOf));
	}
}