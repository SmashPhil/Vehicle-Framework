using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace Vehicles
{
	[DefOf]
	public static class VehicleEventDefOf
	{
		static VehicleEventDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VehicleEventDefOf));
		}
		
		//Movement
		public static VehicleEventDef IgnitionOn;
		public static VehicleEventDef IgnitionOff;
		public static VehicleEventDef Braking;
		public static VehicleEventDef MoveStart;
		public static VehicleEventDef MoveStop;

		//Inventory
		public static VehicleEventDef CargoAdded;
		public static VehicleEventDef CargoRemoved;

		//Pawns
		public static VehicleEventDef PawnEntered;
		public static VehicleEventDef PawnExited;
		public static VehicleEventDef PawnChangedSeats;
		public static VehicleEventDef PawnCapacitiesDirty;
		public static VehicleEventDef PawnKilled;
		public static VehicleEventDef PawnRemoved;

		//Comps
		public static VehicleEventDef OutOfFuel;
		public static VehicleEventDef Refueled;
		public static VehicleEventDef Deployed;
		public static VehicleEventDef Undeployed;

		//Stats
		public static VehicleEventDef DamageTaken;
		public static VehicleEventDef Repaired;

		//State
		public static VehicleEventDef Spawned;
		public static VehicleEventDef Despawned;
		public static VehicleEventDef Destroyed;

		//Aerial
		public static VehicleEventDef AerialVehicleLaunch;
		public static VehicleEventDef AerialVehicleLanding;
		public static VehicleEventDef AerialVehicleCrashLanding;
		public static VehicleEventDef AerialVehicleLeftMap;
		public static VehicleEventDef AerialVehicleOrdered;

		//Upgrades
		public static VehicleEventDef VehicleUpgradeEnqueued;
		public static VehicleEventDef VehicleUpgradeCompleted;
		public static VehicleEventDef VehicleUpgradeRefundEnqueued;
		public static VehicleEventDef VehicleUpgradeRefundCompleted;
	}
}
