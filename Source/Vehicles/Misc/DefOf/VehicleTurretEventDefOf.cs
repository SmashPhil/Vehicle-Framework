using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	[DefOf]
	public static class VehicleTurretEventDefOf
	{
		static VehicleTurretEventDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VehicleTurretEventDefOf));
		}

		public static VehicleTurretEventDef Queued;
		public static VehicleTurretEventDef Dequeued;
		public static VehicleTurretEventDef ShotFired;
		public static VehicleTurretEventDef Reload;
		public static VehicleTurretEventDef Warmup;
		public static VehicleTurretEventDef Cooldown;
	}
}
