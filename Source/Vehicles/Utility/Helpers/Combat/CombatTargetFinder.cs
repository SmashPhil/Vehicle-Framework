using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public static class CombatTargetFinder
	{
		public static Thing FindAttackTarget(VehiclePawn vehicle, TargetScanFlags scanFlags, Func<Thing, bool> validator = null, 
			float minDistance = 0f, float maxDistance = float.MaxValue, IntVec3? locus = null, 
			float maxTravelRadiusFromLocus = float.MaxValue, bool onlyRanged = false)
		{
			// TODO - Use VehicleRegionTraverser for reachability search
			Thing attackTarget = GenClosest.ClosestThingReachable(vehicle.Position, vehicle.Map, 
				ThingRequest.ForGroup(ThingRequestGroup.AttackTarget), PathEndMode.Touch, 
				TraverseParms.For(vehicle, Danger.Deadly, TraverseMode.ByPawn), maxDistance: maxDistance,
				validator: (Thing target) => validator(target) && ShouldIgnoreNoncombatant(vehicle, target, scanFlags), 
				searchRegionsMax: maxDistance > 800 ? -1 : 40);

			return attackTarget;
		}

		private static bool ShouldIgnoreNoncombatant(VehiclePawn vehicle, Thing thing, TargetScanFlags scanFlags)
		{
			if (thing is not Pawn pawn)
			{
				return false;
			}
			if (pawn.IsCombatant())
			{
				return false;
			}
			if (scanFlags.HasFlag(TargetScanFlags.IgnoreNonCombatants))
			{
				return true;
			}
			if (GenSight.LineOfSightToThing(vehicle.Position, pawn, vehicle.Map))
			{
				return false;
			}
			return true;
		}
	}
}
