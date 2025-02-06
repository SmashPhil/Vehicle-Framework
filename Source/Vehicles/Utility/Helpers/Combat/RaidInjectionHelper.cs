using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class RaidInjectionHelper
	{
		public static VehicleCategory GetResolvedCategory(PawnGroupMakerParms parms)
		{
			return VehicleCategory.Combat;
		}

		public static VehicleCategory GetResolvedCategory(IncidentParms parms)
		{
			return VehicleCategory.Combat;
		}

		public static bool ValidRaiderVehicle(VehicleDef vehicleDef, VehicleCategory category, PawnsArrivalModeDef arrivalModeDef, Faction faction, float points)
		{
			if (vehicleDef.vehicleType != VehicleType.Land)
			{
				return false;
			}
			if (!vehicleDef.vehicleCategory.HasFlag(category))
			{
				return false;
			}
			if (vehicleDef.combatPower > points)
			{
				return false;
			}
			if (faction.def.techLevel < vehicleDef.techLevel)
			{
				return false;
			}
			if (!vehicleDef.enabled.HasFlag(VehicleEnabled.For.Raiders))
			{
				return false;
			}
			if (vehicleDef.npcProperties == null)
			{
				return false;
			}
			if (vehicleDef.npcProperties.raidParams != null && !vehicleDef.npcProperties.raidParams.Allows(faction, arrivalModeDef))
			{
				return false;
			}
			return true;
		}
	}
}
