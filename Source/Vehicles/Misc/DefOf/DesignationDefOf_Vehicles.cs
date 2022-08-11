using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public static class DesignationDefOf_Vehicles
	{
		public static DesignationDef DisassembleVehicle;

		static DesignationDefOf_Vehicles()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(DesignationDefOf_Vehicles));
		}
	}
}
