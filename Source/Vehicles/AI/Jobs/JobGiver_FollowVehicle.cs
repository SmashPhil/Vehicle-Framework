using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles.AI
{
	public class JobGiver_FollowVehicle : JobGiver_AIFollowPawn
	{
		protected override int FollowJobExpireInterval
		{
			get
			{
				return 100;
			}
		}

		protected override Pawn GetFollowee(Pawn pawn)
		{
			return (Pawn)pawn.mindState.duty.focus.Thing;
		}

		protected override float GetRadius(Pawn pawn)
		{
			return pawn.mindState.duty.radius;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			VehiclePawn vehicle = GetFollowee(pawn) as VehiclePawn;
			if (vehicle == null)
			{
				Log.Error($"{GetType()} has null followee vehicle. pawn=\"{pawn.ToStringSafe()}\"");
				return null;
			}
			if (!vehicle.Spawned)
			{
				return null;
			}
			if (!pawn.CanReach(vehicle.FollowerCell, PathEndMode.OnCell, Danger.Deadly))
			{
				vehicle.RecalculateFollowerCell(); //Try after updating follower cell
				if (!pawn.CanReach(vehicle.FollowerCell, PathEndMode.OnCell, Danger.Deadly))
				{
					return null;
				}
			}
			float radius = GetRadius(pawn);
			//if (!JobDriver_FollowClose.FarEnoughAndPossibleToStartJob(pawn, vehicle, radius))
			//{
			//	return null;
			//}
			Job job = JobMaker.MakeJob(JobDefOf_Vehicles.FollowVehicle, vehicle);
			job.expiryInterval = FollowJobExpireInterval;
			job.checkOverrideOnExpire = true;
			job.followRadius = radius;
			return job;
		}
	}
}
