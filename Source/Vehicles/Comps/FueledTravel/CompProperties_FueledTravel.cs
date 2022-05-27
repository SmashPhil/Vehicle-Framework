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

		public bool electricPowered;
		public float ticksPerCharge = 50f;

		[PostToSettings(Label = "VehicleFuelConsumptionRate", Translate = true, UISettingsType = UISettingsType.FloatBox)]
		public float fuelConsumptionRate;
		[PostToSettings(Label = "VehicleFuelCapacity", Translate = true, UISettingsType = UISettingsType.IntegerBox)]
		public int fuelCapacity;

		public FuelConsumptionCondition fuelConsumptionCondition;

		public List<OffsetMote> motesGenerated;

		public ThingDef MoteDisplayed;

		public int TicksToSpawnMote;

		public CompProperties_FueledTravel()
		{
			compClass = typeof(CompFueledTravel);
		}
	}
}
