using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	internal class Components : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted)).GetSetMethod(),
				prefix: new HarmonyMethod(typeof(Components),
				nameof(DraftedVehiclesCanMove)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "CanTakeOrder"), prefix: null,
				postfix: new HarmonyMethod(typeof(Components),
				nameof(CanVehicleTakeOrder)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetMeleeAttackAction)),
				prefix: new HarmonyMethod(typeof(Components),
				nameof(NoMeleeForVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.CreateInitialComponents)), prefix: null,
				postfix: new HarmonyMethod(typeof(Components),
				nameof(CreateInitialVehicleComponents)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents)), prefix: null,
				postfix: new HarmonyMethod(typeof(Components),
				nameof(AddAndRemoveVehicleComponents)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_MeleeVerbs), "ChooseMeleeVerb"),
				prefix: new HarmonyMethod(typeof(Components),
				nameof(VehiclesDontMeleeThings)));
		}

		/// <summary>
		/// Allow vehicles to be drafted under the right conditions
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool DraftedVehiclesCanMove(Pawn_DraftController __instance, bool value)
		{
			if(__instance.pawn is VehiclePawn vehicle)
			{
				if (VehicleMod.settings.debug.debugDisableWaterPathing && vehicle.beached)
				{
					vehicle.RemoveBeachedStatus();
				}
				if (value && !__instance.Drafted)
				{
					if (!VehicleMod.settings.debug.debugDraftAnyShip && (vehicle.CompFueledTravel?.EmptyTank ?? false))
					{
						Messages.Message("Vehicles_OutOfFuel".Translate(), MessageTypeDefOf.RejectInput);
						return false;
					}
					if (vehicle.CompUpgradeTree?.CurrentlyUpgrading ?? false)
					{
						Messages.Message("Vehicles_UpgradeInProgress".Translate(), MessageTypeDefOf.RejectInput);
						return false;
					}
					if (!vehicle.CanMoveFinal)
					{
						Messages.Message("Vehicles_NotEnoughToOperate".Translate(), MessageTypeDefOf.RejectInput);
						return false;
					}
					vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().ClearReservedFor(vehicle);
				}
				else if(!value && vehicle.vPather.curPath != null)
				{
					vehicle.vPather.PatherFailed();
				}
				if (!VehicleMod.settings.main.fishingPersists)
				{
					vehicle.currentlyFishing = false;
				}
			}
			return true;
		}

		/// <summary>
		/// Allow vehicles to take orders despite them not being categorized as humanlike
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="__result"></param>
		public static void CanVehicleTakeOrder(Pawn pawn, ref bool __result)
		{
			if(__result is false)
			{
				__result = pawn is VehiclePawn;
			}
		}

		/// <summary>
		/// Disable melee attacks for vehicles, which don't work anyways due to not having Manipulation capacity and only cause errors
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="target"></param>
		/// <param name="failStr"></param>
		/// <returns></returns>
		public static bool NoMeleeForVehicles(Pawn pawn, LocalTargetInfo target, out string failStr)
		{
			if (pawn is VehiclePawn)
			{
				failStr = "IsIncapableOfRamming".Translate(target.Thing.LabelShort);
				//Add more to string or Action if ramming is implemented
				return false;
			}
			failStr = string.Empty;
			return true;
		}

		/// <summary>
		/// Initialize vehicle specific components
		/// </summary>
		/// <param name="pawn"></param>
		public static void CreateInitialVehicleComponents(Pawn pawn)
		{
			if (pawn is VehiclePawn vehicle && vehicle.vPather is null)
			{
				vehicle.vPather = new Vehicle_PathFollower(vehicle);
				vehicle.vehicleAI = new VehicleAI(vehicle);
				vehicle.statHandler = new VehicleStatHandler(vehicle);
				vehicle.graphicOverlay = new VehicleGraphicOverlay(vehicle);
				PatternData defaultPatternData = VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicle.VehicleDef.defName, vehicle.VehicleDef.graphicData);
				vehicle.patternData = new PatternData(defaultPatternData);
			}
		}

		/// <summary>
		/// Ensure that vehicles are given the right components when terminating from the main method
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="actAsIfSpawned"></param>
		public static void AddAndRemoveVehicleComponents(Pawn pawn, bool actAsIfSpawned = false)
		{
			if (pawn is VehiclePawn vehicle && (vehicle.Spawned || actAsIfSpawned) && vehicle.drafter is null)
			{
				vehicle.drafter = new Pawn_DraftController(pawn);
				vehicle.trader = new Pawn_TraderTracker(pawn);
				vehicle.story = new Pawn_StoryTracker(pawn);
				vehicle.playerSettings = new Pawn_PlayerSettings(pawn);
				vehicle.training = null;
			}
		}

		/// <summary>
		/// Ensure that vehicles do not perform melee jobs
		/// </summary>
		/// <param name="target"></param>
		/// <param name="___pawn"></param>
		public static bool VehiclesDontMeleeThings(Thing target, Pawn ___pawn)
		{
			if (___pawn is VehiclePawn)
			{
				return false;
			}
			return true;
		}
	}
}
