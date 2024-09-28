using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;
using System.Text;
using static SmashTools.Debug;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class HealthHelper
	{
		private static readonly (int, string)[] cachedVehicleHealths;

		// Vanilla doesn't define these in their DefOf file
		private static readonly DamageArmorCategoryDef bluntArmor;
		private static readonly DamageArmorCategoryDef heatArmor;

		static HealthHelper()
		{
			cachedVehicleHealths = new (int, string)[DefDatabase<VehicleDef>.AllDefsListForReading.Count];

			bluntArmor = DefDatabase<DamageArmorCategoryDef>.GetNamed("Blunt");
			heatArmor = DefDatabase<DamageArmorCategoryDef>.GetNamed("Heat");

			Assert(bluntArmor != null, "'Blunt' DamageArmorCategoryDef.defName spelled incorrectly.");
			Assert(heatArmor != null, "'Heat' DamageArmorCategoryDef.defName spelled incorrectly.");
		}

		public static (int current, int max, string explanation) GetTotalHealth(this VehiclePawn vehicle)
		{
			int current = 0;
			int max = 0;
			StringBuilder explanation = new();
			foreach (VehicleComponent component in vehicle.statHandler.components)
			{
				current += Mathf.RoundToInt(component.health);
				max += component.props.health;
				explanation.AppendLine($"{component.props.label}: {component.health} / {component.props.health}");

				float armorBlunt = component.ArmorRating(bluntArmor, out _);
				explanation.AppendLine($"    {StatDefOf.ArmorRating_Blunt.LabelCap}: {armorBlunt.ToStringPercent()}");
				float armorSharp = component.ArmorRating(DamageArmorCategoryDefOf.Sharp, out _);
				explanation.AppendLine($"    {StatDefOf.ArmorRating_Sharp.LabelCap}: {armorSharp.ToStringPercent()}");
				float armorHeat = component.ArmorRating(heatArmor, out _);
				explanation.AppendLine($"    {StatDefOf.ArmorRating_Heat.LabelCap}: {armorHeat.ToStringPercent()}");
				explanation.AppendLine();
			}
			return (current, max, explanation.ToString());
		}

		public static (int current, int max, string explanation) GetTotalHealth(this VehicleDef vehicleDef)
		{
			(int health, string explanation) result = cachedVehicleHealths[vehicleDef.DefIndex];
			if (result.health == 0)
			{
				result.health = 0;
				StringBuilder explanation = new StringBuilder();
				float vehicleArmorBlunt = vehicleDef.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt);
				float vehicleArmorSharp = vehicleDef.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
				float vehicleArmorHeat = vehicleDef.GetStatValueAbstract(StatDefOf.ArmorRating_Heat);
				foreach (VehicleComponentProperties componentProperties in vehicleDef.components)
				{
					result.health += componentProperties.health;
					explanation.AppendLine($"{componentProperties.label}: {componentProperties.health}");

					float armorBlunt = componentProperties.armor.GetStatValueFromList(StatDefOf.ArmorRating_Blunt, vehicleArmorBlunt);
					explanation.AppendLine($"    {StatDefOf.ArmorRating_Blunt.LabelCap}: {armorBlunt.ToStringPercent()}");
					float armorSharp = componentProperties.armor.GetStatValueFromList(StatDefOf.ArmorRating_Sharp, vehicleArmorSharp);
					explanation.AppendLine($"    {StatDefOf.ArmorRating_Sharp.LabelCap}: {armorSharp.ToStringPercent()}");
					float armorHeat = componentProperties.armor.GetStatValueFromList(StatDefOf.ArmorRating_Heat, vehicleArmorHeat);
					explanation.AppendLine($"    {StatDefOf.ArmorRating_Heat.LabelCap}: {armorHeat.ToStringPercent()}");
					explanation.AppendLine();
				}
				result.explanation = explanation.ToString();
				cachedVehicleHealths[vehicleDef.DefIndex] = result;
			}
			return (result.health, result.health, result.explanation);
		}

		public static bool AttemptToDrown(Pawn pawn)
		{
			if (pawn is VehiclePawn)
			{
				return true;
			}
			float movementCapacity = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving);
			float manipulationCapacity = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
			float capacity = (movementCapacity + manipulationCapacity) / 2;
			if (capacity <= 1.15f)
			{
				return Rand.Chance(InstantDeathChance(capacity));
			}
			return false;
		}

		public static float InstantDeathChance(float movementCapacity)
		{
			return Mathf.Clamp01(movementCapacity - 0.65f);
		}
	}
}
