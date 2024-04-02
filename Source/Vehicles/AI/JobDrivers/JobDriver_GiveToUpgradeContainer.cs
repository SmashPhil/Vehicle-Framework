using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class JobDriver_GiveToUpgradeContainer : JobDriver_LoadVehicle
	{
		public override string ListerTag => ReservationType.LoadUpgradeMaterials;

		public override bool FailJob()
		{
			if (!Vehicle.CompUpgradeTree.CurrentlyUpgrading || !Vehicle.CompUpgradeTree.NodeUnlocking.AvailableSpace(Vehicle, Item))
			{
				Log.Message($"Failed Job");
				return true;
			}
			return base.FailJob();
		}
		
		protected override Toil GiveAsMuchToVehicleAsPossible()
		{
			return new Toil
			{
				initAction = delegate ()
				{
					if (Item is null || Item.stackCount <= 0)
					{
						Log.Message($"No item");
						pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
					}
					else
					{
						ThingDefCountClass materialRequired = Vehicle.CompUpgradeTree.NodeUnlocking.MaterialsRequired(Vehicle).FirstOrDefault(x => x.thingDef == Item.def);
						
						if (materialRequired is null || materialRequired.count <= 0)
						{
							Log.Message($"No material");
							pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
						}
						else
						{
							int count = Mathf.Min(materialRequired.count, Item.stackCount); //Check back here
							pawn.carryTracker.innerContainer.TryTransferToContainer(Item, Vehicle.CompUpgradeTree.upgradeContainer, count, true);
						}
					}
				}
			};
		}
	}
}
