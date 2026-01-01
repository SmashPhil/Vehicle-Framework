using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreLib.Performance;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

public class VehicleNodeReservation : Reservation<ThingDefCountClass>
{
	private readonly ObjectPool<ThingDefCountList> countListPool = new(10);

	private Dictionary<Pawn, ThingDefCountClass> claimants;

	public VehicleNodeReservation()
	{
	}

	public VehicleNodeReservation(VehiclePawn vehicle, Job job, int maxClaimants) : base(vehicle,
		job, maxClaimants)
	{
		claimants = new Dictionary<Pawn, ThingDefCountClass>();
	}

	public override int TotalClaimants => claimants.Count;

	public override bool RemoveNow => !claimants.Any();

	private bool AnyMissingIngredients
	{
		get
		{
			foreach (ThingDefCountClass thingDefCount in vehicle.CompUpgradeTree.NodeUnlocking.MaterialsRequired(Vehicle))
			{
				int count = 0;
				foreach (ThingDefCountClass reserved in claimants.Values)
				{
					if (reserved.thingDef == thingDefCount.thingDef)
					{
						count += reserved.count;
					}
				}
				if (count < thingDefCount.count)
					return true;
			}
			return false;
		}
	}

	public override bool AddClaimant(Pawn pawn, ThingDefCountClass target)
	{
		if (!claimants.TryAdd(pawn, target))
		{
			Trace.Fail(
				$"Attempting to reserve Vehicle with {pawn.LabelShort}. Target {target} is already reserved.");
			return false;
		}
		return true;
	}

	public override bool CanReserve(Pawn pawn, ThingDefCountClass target,
		StringBuilder stringBuilder = null)
	{
		return !claimants.ContainsKey(pawn) && claimants.Count < maxClaimants &&
			vehicle.CompUpgradeTree.Upgrading && AnyMissingIngredients;
	}

	public override bool ReservedBy(Pawn pawn, ThingDefCountClass target)
	{
		return claimants.TryGetValue(pawn, out ThingDefCountClass thingDefs) && thingDefs == target;
	}

	public override void ReleaseAllReservations()
	{
		foreach (Pawn p in claimants.Keys)
		{
			p.jobs.EndCurrentJob(JobCondition.InterruptForced);
			p.ClearMind_NewTemp();
		}
	}

	public override void ReleaseReservationBy(Pawn pawn)
	{
		claimants.Remove(pawn);
	}

	public override void VerifyAndValidateClaimants()
	{
		foreach (Pawn actor in claimants.Keys.ToList())
		{
			//Fail if job def changes, vehicle target changes, thingDef is no longer available, or vehicle gets drafted
			if (actor.CurJob.def != jobDef || actor.Drafted || vehicle.Drafted)
			{
				claimants.Remove(actor);
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref claimants, nameof(claimants), LookMode.Reference, LookMode.Reference);
	}
}