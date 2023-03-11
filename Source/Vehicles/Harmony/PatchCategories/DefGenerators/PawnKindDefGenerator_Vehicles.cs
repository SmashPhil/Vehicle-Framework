using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public static class PawnKindDefGenerator_Vehicles
	{
		public static bool GenerateImpliedPawnKindDef(VehicleDef vehicleDef, out PawnKindDef kindDef)
		{
			kindDef = vehicleDef.kindDef;
			if (kindDef == null)
			{
				kindDef = new PawnKindDef()
				{
					defName = vehicleDef.defName + "_PawnKind",
					label = vehicleDef.label,
					description = vehicleDef.description,
					combatPower = vehicleDef.combatPower,
					race = vehicleDef,
					lifeStages = new List<PawnKindLifeStage>()
					{
						new PawnKindLifeStage()
						{
							bodyGraphicData = vehicleDef.graphicData
						}
					}
				};
				vehicleDef.kindDef = kindDef;
				return true;
			}
			return false;
		}
	}
}
