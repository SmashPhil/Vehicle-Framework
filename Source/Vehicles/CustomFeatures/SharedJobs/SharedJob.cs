using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class SharedJob : IExposable
	{
		public float workDone;
		private JobDef jobDef;
		private HashSet<Pawn> pawnsOnJob = new HashSet<Pawn>();

		public SharedJob()
		{
			pawnsOnJob ??= new HashSet<Pawn>();
		}

		public bool HasJob => jobDef != null;

		public bool SameJob(JobDef jobDef) => this.jobDef == jobDef;

		public virtual void JobStarted(JobDef jobDef, Pawn pawn)
		{
			if (!jobDef.driverClass.IsSubclassOf(typeof(VehicleJobDriver)))
			{
				SmashLog.Error($"Attempting to start SharedJob with incompatible type <type>{jobDef.driverClass}</type>. SharedJobs must use <type>VehicleJobDriver</type> subtypes.");
				return;
			}
			if (!pawnsOnJob.Add(pawn))
			{
				SmashLog.Warning($"Attempting to register shared job {jobDef} multiple times for {pawn}.");
			}
			if (this.jobDef != jobDef)
			{
				this.jobDef = jobDef;
				workDone = 0;
			}
		}

		public virtual void JobEnded(Pawn pawn)
		{
			if (!pawnsOnJob.Remove(pawn))
			{
				pawnsOnJob.RemoveWhere(pawn => pawn == null || !pawn.Spawned || pawn.Dead);
			}
			if (pawnsOnJob.Count == 0)
			{
				jobDef = null;
				workDone = 0;
			}
		}

		public void ExposeData()
		{
			Scribe_Defs.Look(ref jobDef, nameof(jobDef));
			Scribe_Values.Look(ref workDone, nameof(workDone));
			Scribe_Collections.Look(ref pawnsOnJob, nameof(pawnsOnJob), LookMode.Reference);
		}
	}
}
