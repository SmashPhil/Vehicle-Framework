using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public static class Ext_Toils
	{
		public static T FailOnMoving<T>(this T jobEndable, TargetIndex index) where T : IJobEndable
		{
			jobEndable.AddEndCondition(delegate ()
			{
				if (((Pawn)jobEndable.GetActor().jobs.curJob.GetTarget(index).Thing as VehiclePawn).vPather.Moving)
				{
					return JobCondition.InterruptForced;
				}
				return JobCondition.Ongoing;
			});
			return jobEndable;
		}
	}
}
