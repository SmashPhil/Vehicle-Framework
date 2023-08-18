using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class WorkGiver_RefuelVehicleTurret : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.GetCachedMapComponent<VehicleReservationManager>().VehicleListers(ReservationType.LoadTurret);
		}

		public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			return thing is VehiclePawn vehicle && vehicle.CompVehicleTurrets != null && !vehicle.vPather.Moving && CanTryRefill(pawn, vehicle, forced: forced) 
				&& base.HasJobOnThing(pawn, thing, forced: forced);
		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			if (thing is VehiclePawn vehicle && vehicle.CompVehicleTurrets != null && vehicle.CompVehicleTurrets.GetTurretToFill(out VehicleTurret turret, out int count))
			{
				if (vehicle.IsForbidden(pawn) || !pawn.CanReserve(vehicle, ignoreOtherReservations: forced))
				{
					return null;
				}
				if (pawn.CanReach(vehicle, PathEndMode.Touch, Danger.Deadly))
				{
					List<Thing> packables = FindThingsToPack(vehicle, pawn, turret, count);
					if (packables.NullOrEmpty())
					{
						return null;
					}
					Thing packable = packables.FirstOrDefault();
					Job job = JobMaker.MakeJob(JobDefOf_Vehicles.CarryItemToVehicle, packable, vehicle);
					job.count = Mathf.Min(count, packable.stackCount);
					return job;
				}
			}
			return null;
		}

		private static bool CanTryRefill(Pawn pawn, VehiclePawn vehicle, bool forced = false)
		{
			if (vehicle.IsForbidden(pawn) || !pawn.CanReserve(vehicle, ignoreOtherReservations: forced))
			{
				return false;
			}
			if (vehicle.Faction != pawn.Faction)
			{
				return false;
			}
			return true;
		}

		public static List<Thing> FindThingsToPack(VehiclePawn vehicle, Pawn pawn, VehicleTurret turret, int count)
		{
			List<Thing> ammo = RefuelWorkGiverUtility.FindEnoughReservableThings(pawn, vehicle.Position, new IntRange(turret.turretDef.chargePerAmmoCount, count), delegate (Thing thing)
			{
				if (turret.turretDef.ammunition is null)
				{
					return false;
				}
				if (turret.loadedAmmo is null)
				{
					return turret.turretDef.ammunition.Allows(thing);
				}
				return turret.loadedAmmo == thing.def;
			});
			return ammo;
		}
	}
}
