using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
	public class JobGiver_RangedSupport : JobGiver_CombatFormation
	{
		protected override bool TryFindCombatPosition(VehiclePawn vehicle, out IntVec3 dest)
		{
			return CombatPositionFinder.TryFindCastPosition(CastPositionRequest.For(vehicle), out dest);
		}

		protected override void UpdateEnemyTarget(VehiclePawn vehicle)
		{
			TargetScanFlags scanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedReachableIfCantHitFromMyPos
									  | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
			Thing thing = vehicle.mindState.enemyTarget;
			if (thing != null && ShouldLoseTarget(vehicle))
			{
				thing = null;
			}
			if (thing == null)
			{
				thing = CombatTargetFinder.FindAttackTarget(vehicle, scanFlags, validator: (Thing target) => ExtraTargetValidator(vehicle, target),	
					minDistance: vehicle.CompVehicleTurrets.MinRange, maxDistance: vehicle.CompVehicleTurrets.MaxRange, onlyRanged: true);
				if (thing != null)
				{
					Notify_EngagedTarget(vehicle.mindState);
					Lord lord = vehicle.GetLord();
					lord?.Notify_PawnAcquiredTarget(vehicle, thing);
				}
			}
			else
			{
				//Thing thing2 = CombatTargetFinder.FindAttackTarget(vehicle, scanFlags, validator: (Thing target) => ExtraTargetValidator(vehicle, target),
				//	minDistance: vehicle.CompVehicleTurrets.MinRange, maxDistance: vehicle.CompVehicleTurrets.MaxRange, onlyRanged: true);
				//if (thing2 == null && !vehicle.VehicleDef.npcProperties.runDownTargets)
				//{
				//	thing = null;
				//}
				//else if (thing2 != null && thing2 != thing)
				//{
				//	Notify_EngagedTarget(vehicle.mindState);
				//	thing = thing2;
				//}
			}
			vehicle.mindState.enemyTarget = thing;
			if (thing is Pawn && thing.Faction == Faction.OfPlayer && vehicle.Position.InHorDistOf(thing.Position, 60f))
			{
				Find.TickManager.slower.SignalForceNormalSpeed();
			}
		}
	}
}
