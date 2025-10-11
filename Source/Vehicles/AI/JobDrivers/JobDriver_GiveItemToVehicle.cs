using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using Verse;

namespace Vehicles;

// TODO 1.7 - Could be renamed to better indicate it's only for auto-loading turrets
public sealed class JobDriver_GiveItemToVehicle : JobDriverGetItemForVehicleBase
{
	protected override string ListerTag => ReservationType.LoadTurret;

	protected override IEnumerable<ThingDefCountClass> ThingsToLoad => FindThingDefsToPack(Vehicle, pawn);

	protected override bool ShouldFailJob()
	{
		if (MassUtility.IsOverEncumbered(Vehicle))
			return true;

		return base.ShouldFailJob();
	}

	[Profile]
	public static List<ThingDefCountClass> FindThingDefsToPack(VehiclePawn vehicle, Pawn pawn)
	{
		int massAvailable = Mathf.RoundToInt(vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity) -
			MassUtility.InventoryMass(vehicle));

		ThingDefCountList countList = [];
		foreach (VehicleTurret turret in vehicle.CompVehicleTurrets.Turrets)
		{
			ThingDef reloadDef = turret.loadedAmmo;
			reloadDef ??= turret.def.ammunition?.AllowedThingDefs.FirstOrDefault();
			if (reloadDef != null)
			{
				int desiredCount = vehicle.CompVehicleTurrets.GetQuotaLevel(turret);
				int maxCount = Mathf.RoundToInt(massAvailable /
					Mathf.Max(reloadDef.GetStatValueAbstract(StatDefOf.Mass), 0.1f));
				int existingCount = vehicle.inventory.Count(reloadDef);
				int availableCount = desiredCount - existingCount;
				if (availableCount <= 0)
					continue;

				int quota = Mathf.Min(maxCount, availableCount);
				AddThingDefsToPackForTurret(vehicle, pawn, turret, quota, countList);
			}
		}
		return countList.InnerListForReading;
	}

	[Profile]
	private static void AddThingDefsToPackForTurret(VehiclePawn vehicle, Pawn pawn, VehicleTurret turret,
		int quota, ThingDefCountList countList)
	{
		List<Thing> thingsToPack = FindThingsToPack(vehicle, pawn, turret, quota);
		if (thingsToPack.NullOrEmpty())
			return;

		foreach (Thing thing in thingsToPack)
		{
			if (quota <= 0)
				break;

			int countToTake = Mathf.Min(quota, thing.stackCount);
			quota -= countToTake;
			ThingDefCountClass thingDefCount = countList.Find(thing.def);
			if (thingDefCount == null)
			{
				thingDefCount = new ThingDefCountClass(thing.def, 0);
				countList.Add(thingDefCount);
			}
			thingDefCount.count += countToTake;
		}
	}

	[Profile]
	private static List<Thing> FindThingsToPack(VehiclePawn vehicle, Pawn pawn, VehicleTurret turret, int count)
	{
		return RefuelWorkGiverUtility.FindEnoughReservableThings(pawn, vehicle.Position, new IntRange(1, count),
			IsValidThing);

		bool IsValidThing(Thing thing)
		{
			if (turret.def.ammunition is null)
			{
				return false;
			}
			if (turret.loadedAmmo is null)
			{
				return turret.def.ammunition.Allows(thing);
			}
			return turret.loadedAmmo == thing.def;
		}
	}
}