using System;

namespace Vehicles;

[Flags]
public enum VehicleCategory
{
	None = 0,
	Transport = 1 << 0,
	Trader = 1 << 1,
	Combat = 1 << 2,
	Work = 1 << 3
}