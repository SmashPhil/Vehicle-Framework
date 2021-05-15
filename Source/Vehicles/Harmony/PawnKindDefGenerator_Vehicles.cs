using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public static class PawnKindDefGenerator_Vehicles
	{
		public static PawnKindDef GenerateImpliedPawnKindDef(VehicleDef vehicleDef)
		{
			PawnKindDef kindDef = new PawnKindDef()
			{
				defName = vehicleDef.defName + "_PawnKind",
				label = vehicleDef.label,
				description = vehicleDef.description,
				combatPower = vehicleDef.combatPower,
				race = vehicleDef,
				lifeStages = new List<PawnKindLifeStage>()
				{
					new PawnKindLifeStage()
				}
			};
			vehicleDef.VehicleKindDef = kindDef;
			return kindDef;
		}
	}
}
