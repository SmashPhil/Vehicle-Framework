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

		public static VehicleEventDef DraftOn;
		public static VehicleEventDef DraftOff;
		public static VehicleEventDef MoveStart;
		public static VehicleEventDef MoveStop;
		
		public static VehicleEventDef DamageTaken;
		public static VehicleEventDef Immobilized;
		public static VehicleEventDef Spawned;
		public static VehicleEventDef Despawned;
		public static VehicleEventDef Destroyed;
		
		public static VehicleEventDef PawnEntered;
		public static VehicleEventDef PawnExited;

		public static VehicleEventDef PawnChangedSeats;
		public static VehicleEventDef PawnCapacitiesDirty;
		public static VehicleEventDef PawnKilled;

		public static VehicleEventDef AerialLaunch;
		public static VehicleEventDef AerialLanding;
		public static VehicleEventDef AerialCrashLanding;
	}
}
