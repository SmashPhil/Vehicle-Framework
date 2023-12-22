using System;

namespace Vehicles
{
	//TODO - 1.5, needs better name choices so it's explicit what it should be used for
	[Flags]
	public enum VehicleCategory 
	{ 
		Misc = 0,
		Transport = 1, 
		Trader = 2, 
		Combat = 4, 
		Hybrid = 6
	}
}
