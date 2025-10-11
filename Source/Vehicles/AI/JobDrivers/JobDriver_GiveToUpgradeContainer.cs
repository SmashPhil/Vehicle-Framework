using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles;

public sealed class JobDriver_GiveToUpgradeContainer : JobDriverGetItemForVehicleBase
{
	protected override string ListerTag => ReservationType.LoadUpgradeMaterials;

	protected override IEnumerable<ThingDefCountClass> ThingsToLoad
	{
		get
		{
			if (!Vehicle.CompUpgradeTree.Upgrading)
				return null;

			return Vehicle.CompUpgradeTree.upgrade.node.MaterialsRequired(Vehicle);
		}
	}

	protected override bool ShouldFailJob()
	{
		if (!Vehicle.CompUpgradeTree.Upgrading || !Vehicle.CompUpgradeTree.NodeUnlocking.AvailableSpace(Vehicle, ToHaul))
			return true;

		return base.ShouldFailJob();
	}

	protected override bool AddItemToVehicle(VehiclePawn vehicle, Thing thing)
	{
		ThingDefCountClass materialRequired = vehicle.CompUpgradeTree.NodeUnlocking.MaterialsRequired(vehicle)
		 .FirstOrDefault(countClass => countClass.thingDef == thing.def);
		if (materialRequired is null || materialRequired.count <= 0)
		{
			pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
			return false;
		}
		int count = Mathf.Min(materialRequired.count, thing.stackCount);
		vehicle.CompUpgradeTree.AddToContainer(thing, count);
		return true;
	}
}