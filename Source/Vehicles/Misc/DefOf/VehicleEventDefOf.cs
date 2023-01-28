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
		public static VehicleEventDef DraftOn;
		public static VehicleEventDef DraftOff;
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
