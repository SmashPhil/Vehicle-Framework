using System.Collections.Generic;
using SmashTools;
using Verse;

namespace Vehicles;

public sealed class SharedJob : IExposable
{
	public float workDone;
	private JobDef jobDef;
	private List<Pawn> pawnsOnJob = [];

	public bool HasJob => jobDef != null;

	public bool SameJob(JobDef jobDef)
	{
		return this.jobDef == jobDef;
	}

	public void JobStarted(JobDef jobDef, Pawn pawn)
	{
		if (!jobDef.driverClass.IsSubclassOf(typeof(VehicleJobDriver)))
		{
			SmashLog.Error(
				$"Attempting to start SharedJob with incompatible type <type>{jobDef.driverClass}</type>. SharedJobs must use <type>VehicleJobDriver</type> subtypes.");
			return;
		}
		pawnsOnJob.Add(pawn);
		if (!SameJob(jobDef))
		{
			this.jobDef = jobDef;
			workDone = 0;
		}
	}

	public void JobEnded(Pawn pawn)
	{
		if (!pawnsOnJob.Remove(pawn))
		{
			pawnsOnJob.RemoveWhere(InvalidPawn);
		}
		if (pawnsOnJob.Count == 0)
		{
			jobDef = null;
			workDone = 0;
		}
		return;

		static bool InvalidPawn(Pawn pawn)
		{
			return pawn is not { Spawned: true, Dead: false };
		}
	}

	public void ExposeData()
	{
		Scribe_Defs.Look(ref jobDef, nameof(jobDef));
		Scribe_Values.Look(ref workDone, nameof(workDone));
		Scribe_Collections.Look(ref pawnsOnJob, nameof(pawnsOnJob), LookMode.Reference);
	}
}