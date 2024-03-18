using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse.AI;
using Verse;

namespace Vehicles
{
	public abstract class JobGiver_VehicleAI : ThinkNode_JobGiver
	{
		protected virtual bool RequireLOS
		{
			get
			{
				return true;
			}
		}

		protected virtual VehicleStrategy GetStrategy(VehiclePawn vehicle)
		{
			if (vehicle.CompVehicleTurrets != null && !vehicle.CompVehicleTurrets.turrets.NullOrEmpty())
			{
				return VehicleStrategy.Ranged;
			}
			return VehicleStrategy.DropOff;
		}

		protected abstract bool TryFindShootingPosition(VehiclePawn vehicle, out IntVec3 position);

		protected virtual Thing FindAttackTarget(Pawn pawn)
		{
			return null;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			throw new NotImplementedException();
		}
	}
}
