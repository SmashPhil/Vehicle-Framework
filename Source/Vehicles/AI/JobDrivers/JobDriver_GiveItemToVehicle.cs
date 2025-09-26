using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles;

// TODO 1.7 - Could be renamed to better indicate it's only for auto-loading turrets
public class JobDriver_GiveItemToVehicle : JobDriverGetItemForVehicleBase
{
	protected override string ListerTag => ReservationType.LoadTurret;

	protected override IEnumerable<ThingDefCountClass> ThingsToLoad => FindThingDefsToPack(Vehicle, pawn);

	public static IEnumerable<ThingDefCountClass> FindThingDefsToPack(VehiclePawn vehicle, Pawn pawn)
	{
		if (vehicle.CompVehicleTurrets.GetTurretToFill(out VehicleTurret turret, out int quota))
		{
			List<ThingDefCountClass> thingDefCounts = [];
			foreach (Thing thing in FindThingsToPack(vehicle, pawn, turret, quota))
			{
				if (quota <= 0)
					break;

				int countToTake = Mathf.Min(quota, thing.stackCount);
				quota -= countToTake;
				ThingDefCountClass thingDefCount = thingDefCounts.FirstOrDefault(defCount => defCount.thingDef == thing.def);
				if (thingDefCount == null)
				{
					thingDefCount = new ThingDefCountClass(thing.def, 0);
					thingDefCounts.Add(thingDefCount);
				}
				thingDefCount.count += countToTake;
			}
			return thingDefCounts;
		}
		return null;
	}

	public static List<Thing> FindThingsToPack(VehiclePawn vehicle, Pawn pawn, VehicleTurret turret, int count)
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