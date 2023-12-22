using System;

namespace Vehicles
{
	[Flags]
	public enum VehicleEnabledFor
	{
		None = 0,
		Player = 1,
		Raiders = 2,
		Everyone = 3,
	}
}
