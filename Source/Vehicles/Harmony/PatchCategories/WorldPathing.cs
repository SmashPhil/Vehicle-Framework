using System;
using System.Text;
using System.Linq;
using HarmonyLib;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	internal class WorldPathing : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldSelector), "AutoOrderToTileNow"),
				prefix: new HarmonyMethod(typeof(WorldPathing),
				nameof(AutoOrderVehicleCaravanPathing)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Caravan_PathFollower), "StartPath"),
				prefix: new HarmonyMethod(typeof(WorldPathing),
				nameof(StartVehicleCaravanPath)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)),
				prefix: new HarmonyMethod(typeof(WorldPathing),
				nameof(VehicleRoutePlannerUpdateHook)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerOnGUI)),
				prefix: new HarmonyMethod(typeof(WorldPathing),
				nameof(VehicleRoutePlannerOnGUIHook)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.DoRoutePlannerButton)), 
				postfix: new HarmonyMethod(typeof(WorldPathing),
				nameof(VehicleRoutePlannerButton)));
		}

		/// <summary>
		/// Intercept AutoOrderToTileNow method to StartPath on VehicleCaravan_PathFollower
		/// Necessary due to CaravanUtility.BestGotoDestNear returning incorrect positions based on custom tile values for vehicles
		/// </summary>
		/// <param name="c"></param>
		/// <param name="tile"></param>
		public static bool AutoOrderVehicleCaravanPathing(Caravan c, int tile)
		{
			if (c is VehicleCaravan vehicleCaravan)
			{
				if (tile < 0 || (tile == vehicleCaravan.Tile && !vehicleCaravan.vehiclePather.Moving))
				{
					return false;
				}
				if (vehicleCaravan.VehiclesListForReading.NullOrEmpty())
				{
					return false;
				}
				foreach (VehiclePawn vehicle in vehicleCaravan.VehiclesListForReading)
				{
					if (!WorldVehiclePathGrid.Instance.Passable(tile, vehicle.VehicleDef) || vehicle.VehicleDef.vehicleType == VehicleType.Air)
					{
						return false;
					}
				}
				int bestTile = WorldHelper.BestGotoDestForVehicle(vehicleCaravan, tile);
				if (bestTile >= 0)
				{
					vehicleCaravan.vehiclePather.StartPath(bestTile, null, true, true);
					vehicleCaravan.gotoMote.OrderedToTile(bestTile);
					SoundDefOf.ColonistOrdered.PlayOneShotOnCamera(null);
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Catch-All for Caravan_PathFollower.StartPath, redirect to VehicleCaravan_PathFollower
		/// </summary>
		/// <param name="destTile"></param>
		/// <param name="arrivalAction"></param>
		/// <param name="___caravan"></param>
		/// <param name="repathImmediately"></param>
		/// <param name="resetPauseStatus"></param>
		public static bool StartVehicleCaravanPath(int destTile, CaravanArrivalAction arrivalAction, Caravan ___caravan, bool repathImmediately = false, bool resetPauseStatus = true)
		{
			if (___caravan is VehicleCaravan vehicleCaravan)
			{
				vehicleCaravan.vehiclePather.StartPath(destTile, arrivalAction, repathImmediately, resetPauseStatus);
				return false;
			}
			return true;
		}

		/* --------------- VehicleRoutePlanner Hook --------------- */
		public static void VehicleRoutePlannerUpdateHook()
		{
			VehicleRoutePlanner.Instance?.WorldRoutePlannerUpdate();
		}

		public static void VehicleRoutePlannerOnGUIHook()
		{
			VehicleRoutePlanner.Instance?.WorldRoutePlannerOnGUI();
		}

		public static void VehicleRoutePlannerButton(ref float curBaseY)
		{
			VehicleRoutePlanner.Instance?.DoRoutePlannerButton(ref curBaseY);
		}
		/* ------------------------------------------------------- */
	}
}
