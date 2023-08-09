using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	[HeaderTitle(Label = "VF_FueledTravelPropertes", Translate = true)]
	public class CompProperties_FueledTravel : VehicleCompProperties
	{
		public ThingDef fuelType;
		public ThingDef leakDef;

		public bool electricPowered;

		//[PostToSettings(Label = "VF_DischargePerTick", Translate = true, UISettingsType = UISettingsType.FloatBox)]
		public float dischargeRate = 2;
		//[PostToSettings(Label = "VF_TicksPerCharge", Translate = true, UISettingsType = UISettingsType.FloatBox)]
		public float chargeRate;

		[PostToSettings(Label = "VF_FuelConsumptionRate", Translate = true, UISettingsType = UISettingsType.FloatBox)]
		public float fuelConsumptionRate;
		[PostToSettings(Label = "VF_FuelCapacity", Translate = true, UISettingsType = UISettingsType.IntegerBox)]
		public int fuelCapacity;
		
		[PostToSettings(Label = "VF_FuelConsumptionRateWorldMultiplier", Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(Increment = 0.1f, MinValue = 0, MaxValue = 2)]
		public float fuelConsumptionWorldMultiplier = 1;

		[PostToSettings(Label = "VF_AutoRefuelPercent", Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(Increment = 0.05f, MinValue = 0, MaxValue = 1, RoundDecimalPlaces = 2)]
		public float autoRefuelPercent = 1;
		
		public FuelConsumptionCondition fuelConsumptionCondition = FuelConsumptionCondition.Drafted | FuelConsumptionCondition.Moving | FuelConsumptionCondition.Flying;

		public List<OffsetMote> motesGenerated;

		public ThingDef MoteDisplayed;

		public int ticksToSpawnMote;

		public string fuelIconPath;

		private Texture2D fuelIcon;

		public CompProperties_FueledTravel()
		{
			compClass = typeof(CompFueledTravel);
		}

		public Texture2D FuelIcon
		{
			get
			{
				if (fuelIcon == null)
				{
					if (!fuelIconPath.NullOrEmpty())
					{
						fuelIcon = ContentFinder<Texture2D>.Get(fuelIconPath);
					}
					else
					{
						//implement ThingFilter using AnyAllowedDef -> CompProperties_Refuelable for example
						fuelIcon = fuelType.uiIcon;
					}
				}
				return fuelIcon;
			}
		}
	}
}
