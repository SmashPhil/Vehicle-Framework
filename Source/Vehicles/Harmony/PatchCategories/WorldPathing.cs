using System;
using System.Text;
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
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(TilesPerDayCalculator), nameof(TilesPerDayCalculator.ApproxTilesPerDay), new Type[] { typeof(Caravan), typeof(StringBuilder) }),
				prefix: new HarmonyMethod(typeof(WorldPathing),
				nameof(ApproxTilesForShips)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)),
				prefix: new HarmonyMethod(typeof(WorldPathing),
				nameof(VehicleRoutePlannerUpdateHook)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.WorldRoutePlannerOnGUI)),
				prefix: new HarmonyMethod(typeof(WorldPathing),
				nameof(VehicleRoutePlannerOnGUIHook)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.DoRoutePlannerButton)), prefix: null,
				postfix: new HarmonyMethod(typeof(WorldPathing),
				nameof(VehicleRoutePlannerButton)));
		}

		/// <summary>
		/// Intercept AutoOrderToTileNow method to StartPath on VehicleCaravan_PathFollower
		/// Necessary due to CaravanUtility.BestGotoDestNear returning incorrect positions based on custom tile values for vehicles
		/// </summary>
		/// <param name="c"></param>
		/// <param name="tile"></param>
		/// <returns></returns>
		public static bool AutoOrderVehicleCaravanPathing(Caravan c, int tile)
		{
			if(c is VehicleCaravan caravan && caravan.HasVehicle())
			{
				if (tile < 0 || (tile == caravan.Tile && !caravan.vPather.Moving))
				{
					return false;
				}
				int num = WorldHelper.BestGotoDestForVehicle(caravan, tile);
				if (num >= 0)
				{
					caravan.vPather.StartPath(num, null, true, true);
					caravan.gotoMote.OrderedToTile(num);
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
		/// <returns></returns>
		public static bool StartVehicleCaravanPath(int destTile, CaravanArrivalAction arrivalAction, Caravan ___caravan, bool repathImmediately = false, bool resetPauseStatus = true)
		{
			if(___caravan is VehicleCaravan vehicleCaravan && vehicleCaravan.HasVehicle())
			{
				vehicleCaravan.vPather.StartPath(destTile, arrivalAction, repathImmediately, resetPauseStatus);
			}
			return true;
		}

		//REDO
		public static bool ApproxTilesForShips(Caravan caravan, StringBuilder explanation = null)
		{
			//Continue here
			return true;
		}

		/* --------------- VehicleRoutePlanner Hook --------------- */
		public static void VehicleRoutePlannerUpdateHook()
		{
			Find.World.GetCachedWorldComponent<VehicleRoutePlanner>().WorldRoutePlannerUpdate();
		}

		public static void VehicleRoutePlannerOnGUIHook()
		{
			Find.World.GetCachedWorldComponent<VehicleRoutePlanner>().WorldRoutePlannerOnGUI();
		}

		public static void VehicleRoutePlannerButton(ref float curBaseY)
		{
			Find.World.GetCachedWorldComponent<VehicleRoutePlanner>().DoRoutePlannerButton(ref curBaseY);
		}
		/* ------------------------------------------------------- */
	}
}
