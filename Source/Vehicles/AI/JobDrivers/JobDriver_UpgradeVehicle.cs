using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class JobDriver_UpgradeVehicle : JobDriver_WorkVehicle
	{
		protected override JobDef JobDef => JobDefOf_Vehicles.UpgradeVehicle;

		protected override StatDef Stat => StatDefOf.ConstructionSpeed;

		protected override float TotalWork => Vehicle.CompUpgradeTree.NodeUnlocking.work;

		protected override float Work
		{
			get
			{
				if (Vehicle == null)
				{
					return 0;
				}
				return Vehicle.CompUpgradeTree.upgrade.WorkLeft;
			}
			set
			{
				if (Vehicle != null)
				{
					Vehicle.CompUpgradeTree.upgrade.WorkLeft = value;
				}
			}
		}

		protected override void WorkComplete(Pawn actor)
		{
			Vehicle.CompUpgradeTree.FinishUnlock(Vehicle.CompUpgradeTree.NodeUnlocking);
			Vehicle.CompUpgradeTree.ClearUpgrade();
			actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
		}

		protected override void ResetWork()
		{
		}
	}
}
