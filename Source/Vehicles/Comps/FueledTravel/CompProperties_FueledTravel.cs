using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	[HeaderTitle(Label = "FueledTravelPropertes", Translate = true)]
	public class CompProperties_FueledTravel : VehicleCompProperties
	{
		public ThingDef fuelType;
		public ThingDef leakDef;

		public bool electricPowered;

		[PostToSettings(Label = "VehicleDischargePerTick", Translate = true, UISettingsType = UISettingsType.FloatBox)]
		public float dischargeRate = 2;
		[PostToSettings(Label = "VehicleTicksPerCharge", Translate = true, UISettingsType = UISettingsType.FloatBox)]
		public float chargeRate;

		[PostToSettings(Label = "VehicleFuelConsumptionRate", Translate = true, UISettingsType = UISettingsType.FloatBox)]
		public float fuelConsumptionRate;
		[PostToSettings(Label = "VF_VehicleFuelCapacity", Translate = true, UISettingsType = UISettingsType.IntegerBox)]
		public int fuelCapacity;
		[PostToSettings(Label = "VehicleFuelConsumptionRateWorldMultiplier", Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(Increment = 0.1f, MinValue = 0, MaxValue = 2)]
		public float fuelConsumptionWorldMultiplier = 1;

		public FuelConsumptionCondition fuelConsumptionCondition;

		public List<OffsetMote> motesGenerated;

		public ThingDef MoteDisplayed;

		public int ticksToSpawnMote;

		public CompProperties_FueledTravel()
		{
			compClass = typeof(CompFueledTravel);
		}
	}
}
