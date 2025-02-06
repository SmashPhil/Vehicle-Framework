using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public abstract class JobGiver_ExitMap : ThinkNode_JobGiver
	{
		protected LocomotionUrgency defaultLocomotion;
		protected int jobMaxDuration = 999999;
		protected bool forceDitchIfCantReachMapEdge;
		protected bool delayDitchIfActiveThreat;
		protected bool sabotageVehicleOnDitch = true;
		protected bool failIfCantJoinOrCreateCaravan;
		
		protected abstract bool TryFindGoodExitDest(VehiclePawn vehicle, out IntVec3 cell);

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_ExitMap jobGiver_ExitMap = (JobGiver_ExitMap)base.DeepCopy(resolve);
			jobGiver_ExitMap.defaultLocomotion = defaultLocomotion;
			jobGiver_ExitMap.jobMaxDuration = jobMaxDuration;
			jobGiver_ExitMap.delayDitchIfActiveThreat = delayDitchIfActiveThreat;
			jobGiver_ExitMap.forceDitchIfCantReachMapEdge = forceDitchIfCantReachMapEdge;
			jobGiver_ExitMap.failIfCantJoinOrCreateCaravan = failIfCantJoinOrCreateCaravan;
			return jobGiver_ExitMap;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			VehiclePawn vehicle = pawn as VehiclePawn;
			Assert.IsNotNull(vehicle, "Non-vehicle pawn assigned to a vehicle job.");
			Assert.IsTrue(vehicle.Spawned, "Assigning job to despawned vehicle.");

			VehicleMapping mapping = vehicle.Map.GetCachedMapComponent<VehicleMapping>();
			VehicleMapping.VehiclePathData pathData = mapping[vehicle.VehicleDef];
			VehicleReachability reachability = pathData.VehicleReachability;
			bool canReach = !reachability.CanReachMapEdge(vehicle.Position, TraverseParms.For(vehicle));
			bool activeThreat = vehicle.Faction != null && GenHostility.AnyHostileActiveThreatTo(vehicle.Map, vehicle.Faction);
			
			if (!TryFindGoodExitDest(vehicle, out IntVec3 cell))
			{
				bool shouldDitch = !vehicle.CanMoveFinal || !canReach;
				//if (shouldDitch && forceDitchIfCantReachMapEdge && !(delayDitchIfActiveThreat && activeThreat))
				//{
				//	vehicle.DisembarkAll();
				//}
				return null;
			}
			if (vehicle.VehicleDef.npcProperties != null && vehicle.VehicleDef.npcProperties.reverseWhileFleeing)
			{
				// TODO - add conditional reversal to keep frontal armor facing any active threats
				//vehicle.Reverse = true;
			}
			
			Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
			job.exitMapOnArrival = true;
			job.failIfCantJoinOrCreateCaravan = failIfCantJoinOrCreateCaravan;
			job.locomotionUrgency = PawnUtility.ResolveLocomotion(vehicle, defaultLocomotion, LocomotionUrgency.Jog);
			job.expiryInterval = jobMaxDuration;
			return job;
		}
	}
}
