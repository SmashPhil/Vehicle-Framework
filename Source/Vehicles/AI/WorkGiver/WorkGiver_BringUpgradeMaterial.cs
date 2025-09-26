using System.Collections.Generic;
using System.Linq;
using SmashTools;
using Verse;

namespace Vehicles;

public class WorkGiver_BringUpgradeMaterial : WorkGiver_CarryToVehicle<ThingDefCountClass>
{
	public override string ReservationName => ReservationType.LoadUpgradeMaterials;

	public override JobDef JobDef => JobDefOf_Vehicles.LoadUpgradeMaterials;

	protected override bool JobAvailable(Pawn pawn, VehiclePawn vehicle)
	{
		if (!vehicle.Spawned)
			return false;
		if (vehicle.CompUpgradeTree is null or { Upgrading: false })
			return false;
		if (vehicle.CompUpgradeTree.upgrade.Removal)
			return false;

		if (!vehicle.Map.GetCachedMapComponent<VehicleReservationManager>()
		 .VehicleListed(vehicle, ReservationType.LoadUpgradeMaterials))
		{
			return false;
		}
		return !vehicle.CompUpgradeTree.StoredCostSatisfied;
	}

	protected override List<ThingDefCountClass> GetThingsToLoad(VehiclePawn vehicle, Pawn pawn)
	{
		if (!vehicle.CompUpgradeTree.Upgrading)
			return null;

		return vehicle.CompUpgradeTree.upgrade.node.MaterialsRequired(vehicle).ToList();
	}

	protected override Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn, List<ThingDefCountClass> things)
	{
		return JobDriverGetItemForVehicleBase.FindThingToPack(vehicle, pawn, things);
	}
}