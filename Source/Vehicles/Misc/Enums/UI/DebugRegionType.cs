using System;

namespace Vehicles
{
	[Flags]
	public enum DebugRegionType
	{
		None = 1 << 0,
		Regions = 1 << 1,
		Links = 1 << 2,
		Weights = 1 << 3,
		Things = 1 << 4,
		PathCosts = 1 << 5
	}
}
