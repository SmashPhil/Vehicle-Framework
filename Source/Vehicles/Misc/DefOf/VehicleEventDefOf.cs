using RimWorld;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf]
public static class VehicleEventDefOf
{
	// Movement
	public static VehicleEventDef IgnitionOn;
	public static VehicleEventDef IgnitionOff;
	public static VehicleEventDef Braking;
	public static VehicleEventDef MoveStart;
	public static VehicleEventDef MoveStop;

	// Inventory
	public static VehicleEventDef CargoAdded;
	public static VehicleEventDef CargoRemoved;

	// Pawns
	public static VehicleEventDef PawnEntered;
	public static VehicleEventDef PawnExited;
	public static VehicleEventDef PawnChangedSeats;
	public static VehicleEventDef PawnCapacitiesDirty;
	public static VehicleEventDef PawnKilled;
	public static VehicleEventDef PawnRemoved;

	// Ticking
	public static VehicleEventDef ScanShort; // 60 ticks
	public static VehicleEventDef ScanRare; // 250 ticks

	// Comps
	public static VehicleEventDef OutOfFuel;
	public static VehicleEventDef Refueled;
	public static VehicleEventDef Deployed;
	public static VehicleEventDef Undeployed;

	// Stats
	public static VehicleEventDef HealthChanged;
	public static VehicleEventDef DamageTaken;
	public static VehicleEventDef Repaired;

	// State
	public static VehicleEventDef Spawned;
	public static VehicleEventDef Despawned;
	public static VehicleEventDef Destroyed;

	// Aerial
	public static VehicleEventDef AerialVehicleLaunch;
	public static VehicleEventDef AerialVehicleLanding;
	public static VehicleEventDef AerialVehicleCrashLanding;
	public static VehicleEventDef AerialVehicleLeftMap;
	public static VehicleEventDef AerialVehicleOrdered;

	// Upgrades
	public static VehicleEventDef UpgradeEnqueued;
	public static VehicleEventDef UpgradeCompleted;
	public static VehicleEventDef UpgradeCanceled;
	public static VehicleEventDef UpgradeRefundEnqueued;
	public static VehicleEventDef UpgradeRefundCompleted;

	// Rendering
	public static VehicleEventDef ColorChanged;

	static VehicleEventDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(VehicleEventDefOf));
	}
}