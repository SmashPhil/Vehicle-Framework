using System;

namespace Vehicles
{
	[Flags]
	public enum VehicleCategory 
	{
		Transport = 1 << 0, 
		Trader = 1 << 1, 
		Combat = 1 << 2,
	}
}
