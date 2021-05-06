using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public abstract class AerialVehicleArrivalModeWorker
	{
		public AerialVehicleArrivalModeDef def;

		public abstract void VehicleArrived(AerialVehicleInFlight aerialVehicle, LaunchProtocol protocol, Map map);

		public abstract bool TryResolveRaidSpawnCenter(IncidentParms parms);

		public virtual bool CanUseWith(IncidentParms parms)
		{
			return (parms.faction == null || def.minTechLevel == TechLevel.Undefined || parms.faction.def.techLevel >= def.minTechLevel) && 
				(!parms.raidArrivalModeForQuickMilitaryAid || def.forQuickMilitaryAid);
		}

		public virtual float GetSelectionWeight(IncidentParms parms)
		{
			if (def.selectionWeightCurve != null)
			{
				return def.selectionWeightCurve.Evaluate(parms.points);
			}
			return 0f;
		}
	}
}
