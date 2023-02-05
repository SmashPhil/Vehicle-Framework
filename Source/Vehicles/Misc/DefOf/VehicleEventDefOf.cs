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

		//Comps
		public static VehicleEventDef OutOfFuel;
		public static VehicleEventDef Refueled;

		//Stats
		public static VehicleEventDef DamageTaken;
		public static VehicleEventDef Repaired;

		//State
		public static VehicleEventDef Spawned;
		public static VehicleEventDef Despawned;
		public static VehicleEventDef Destroyed;

		//Aerial
		public static VehicleEventDef AerialLaunch;
		public static VehicleEventDef AerialLanding;
		public static VehicleEventDef AerialCrashLanding;
	}
}
