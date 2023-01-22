using System;

namespace Vehicles
{
	[Flags]
	public enum FuelConsumptionCondition
	{
		Moving = 1 << 0, 
		Drafted = 1 << 1,
		Flying = 1 << 2,
		Always = 1 << 3
	};
}
