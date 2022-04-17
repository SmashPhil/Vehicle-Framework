using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using OpCodes = System.Reflection.Emit.OpCodes;
using UnityEngine;

namespace Vehicles
{
	internal class Upgrades : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn), "TicksPerMove"),
				prefix: new HarmonyMethod(typeof(Upgrades),
				nameof(VehicleMoveSpeedUpgradeModifier)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.Capacity)), prefix: null,
				postfix: new HarmonyMethod(typeof(Upgrades),
				nameof(VehicleCargoCapacity)));
		}

		//REDO - Needs implementation of Weight based speed declination
		/// <summary>
		/// Apply MoveSpeed upgrade stat to vehicles
		/// </summary>
		/// <param name="diagonal"></param>
		/// <param name="__instance"></param>
		/// <param name="__result"></param>
		public static bool VehicleMoveSpeedUpgradeModifier(bool diagonal, Pawn __instance, ref int __result)
		{
			if(__instance is VehiclePawn vehicle)
			{
				float num = vehicle.ActualMoveSpeed / 60;
				float num2 = 1 / num;
				if (vehicle.Spawned && !vehicle.Map.roofGrid.Roofed(vehicle.Position))
					num2 /= vehicle.Map.weatherManager.CurMoveSpeedMultiplier;
				if (diagonal)
					num2 *= 1.41421f;
				__result = Mathf.Clamp(Mathf.RoundToInt(num2), 1, 450);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Apply Cargo capacity upgrade stat to vehicles
		/// </summary>
		/// <param name="p"></param>
		/// <param name="__result"></param>
		public static void VehicleCargoCapacity(Pawn p, ref float __result)
		{
			if(p is VehiclePawn vehicle)
			{
				__result = vehicle.StatCargo + vehicle.CargoCapacity;
			}
		}
	}
}
