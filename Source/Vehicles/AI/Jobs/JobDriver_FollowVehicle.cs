using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace Vehicles.AI
{
	public class JobDriver_FollowVehicle : JobDriver
	{
		private VehiclePawn Vehicle => job.GetTarget(TargetIndex.A).Thing as VehiclePawn;

		public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

		public override void Notify_Starting()
		{
			base.Notify_Starting();
			if (job.followRadius <= 0f)
			{
				Log.Error($"Follow radius is <= 0. pawn=\"{pawn.ToStringSafe()}\" vehicle=\"{Vehicle.ToStringSafe()}\"");
				job.followRadius = 10f;
			}
		}

		public override bool IsContinuation(Job job)
		{
			return this.job.GetTarget(TargetIndex.A) == job.GetTarget(TargetIndex.A);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			float radius = job.followRadius;
			if (radius <= 0 || radius <= (Vehicle.VehicleDef.Size.z / 2f))
			{
				radius = Vehicle.VehicleDef.Size.z * 1.5f;
			}
			yield return new Toil
			{
				tickAction = delegate ()
				{
					if (pawn.Position.InHorDistOf(Vehicle.FollowerCell, radius) && pawn.Position.WithinRegions(Vehicle.FollowerCell, Map, 2, TraverseParms.For(pawn)))
					{
						return;
					}
					if (!pawn.CanReach(Vehicle.FollowerCell, PathEndMode.Touch, Danger.Deadly))
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					if (!pawn.pather.Moving || pawn.pather.Destination != Vehicle.FollowerCell)
					{
						pawn.pather.StartPath(Vehicle.FollowerCell, PathEndMode.Touch);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Never
			};
		}
	}
}
