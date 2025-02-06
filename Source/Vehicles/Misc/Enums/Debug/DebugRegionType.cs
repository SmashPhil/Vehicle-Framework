using System;

namespace Vehicles
{
	[Flags]
	public enum DebugRegionType
	{
		None = 1 << 0,
		Regions = 1 << 1,
		Rooms = 1 << 2,
		Links = 1 << 3,
		Weights = 1 << 4,
		PathCosts = 1 << 5
	}
}
