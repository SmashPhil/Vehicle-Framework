using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class VehicleNodeReservation : Reservation<ThingDefCountClass>
	{
		private Dictionary<Pawn, ThingDefCountClass> claimants;

		public VehicleNodeReservation()
		{
		}

		public VehicleNodeReservation(VehiclePawn vehicle, Job job, int maxClaimants) : base(vehicle, job, maxClaimants)
		{
			claimants = new Dictionary<Pawn, ThingDefCountClass>();
		}

		public override int TotalClaimants => claimants.Count;

		public override bool RemoveNow => !claimants.Any();

		public override bool AddClaimant(Pawn pawn, ThingDefCountClass target)
		{
			if(claimants.ContainsKey(pawn))
			{
				Log.Error($"Attempting to reserve Vehicle with {pawn.LabelShort}. Target {target} is already reserved.");
				return false;
			}
			claimants.Add(pawn, target);
			return true;
		}

		public override bool CanReserve(Pawn pawn, ThingDefCountClass target, StringBuilder stringBuilder = null)
		{
			return !claimants.ContainsKey(pawn) && claimants.Count < maxClaimants && vehicle.CompUpgradeTree.Upgrading && MaterialsLeft().NotNullAndAny();
		}

		public override bool ReservedBy(Pawn pawn, ThingDefCountClass target)
		{
			return claimants.TryGetValue(pawn, out ThingDefCountClass thingDefs) && thingDefs == target;
		}

		public List<ThingDefCountClass> MaterialsLeft()
		{
			var materials = vehicle.CompUpgradeTree.NodeUnlocking.MaterialsRequired(Vehicle);
			Dictionary<ThingDef, int> cachedTd = new Dictionary<ThingDef, int>();
			foreach(ThingDefCountClass td in materials)
			{
				if(claimants.Any(c => c.Value.thingDef == td.thingDef))
				{
					var td2 = claimants.Values.Where(m => m.thingDef == td.thingDef);
					if(td.count - td2.Sum(t => t.count) > 0)
					{
						if(cachedTd.ContainsKey(td.thingDef))
						{
							cachedTd[td.thingDef] += td.count - td2.Sum(t => t.count);
						}
						else
						{
							cachedTd.Add(td.thingDef, td.count - td2.Sum(t => t.count));
						}
					}
				}
				else
				{
					if(cachedTd.ContainsKey(td.thingDef))
					{
						cachedTd[td.thingDef] += td.count;
					}
					else
					{
						cachedTd.Add(td.thingDef, td.count);
					}
				}
			}
			var tdList = new List<ThingDefCountClass>();
			cachedTd.ForEach(t => tdList.Add(new ThingDefCountClass(t.Key, t.Value)));
			return tdList;
		}

		public override void ReleaseAllReservations()
		{
			foreach(Pawn p in claimants.Keys)
			{
				p.jobs.EndCurrentJob(JobCondition.InterruptForced);
				p.ClearMind();
			}
		}

		public override void ReleaseReservationBy(Pawn pawn)
		{
			if (claimants.ContainsKey(pawn))
				claimants.Remove(pawn);
		}

		public override void VerifyAndValidateClaimants()
		{
			var mats = vehicle.CompUpgradeTree.NodeUnlocking.MaterialsRequired(Vehicle);
			var claims = new List<Pawn>(claimants.Keys);
			foreach(Pawn actor in claims)
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
			Scribe_Collections.Look(ref claimants, "claimants", LookMode.Reference, LookMode.Reference);
		}
	}
}
