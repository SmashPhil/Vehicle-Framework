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
		private static readonly HashSet<PawnsArrivalModeDef> defaultArrivalModes = new HashSet<PawnsArrivalModeDef>();

		static RaidInjectionHelper()
		{
			defaultArrivalModes.Add(PawnsArrivalModeDefOf.EdgeWalkIn);
			defaultArrivalModes.Add(PawnsArrivalModeDefOf.EdgeWalkInGroups);
		}

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
			if (vehicleDef.npcProperties != null)
			{
				if (!vehicleDef.npcProperties.restrictedTo.NullOrEmpty() && !vehicleDef.npcProperties.restrictedTo.Contains(faction.def))
				{
					return false;
				}
				if (arrivalModeDef != null)
				{
					if (!vehicleDef.npcProperties.arrivalModes.NullOrEmpty())
					{
						if (!vehicleDef.npcProperties.arrivalModes.Contains(arrivalModeDef))
						{
							return false;
						}
					}
					else if (!defaultArrivalModes.Contains(arrivalModeDef))
					{
						return false;
					}
				}
			}
			return vehicleDef.enabled.HasFlag(VehicleEnabledFor.Raiders);
		}
	}
}
