using System;

namespace Vehicles
{
	[Flags]
	public enum DebugRegionType
	{
		None = 0,
		Regions = 1,
		Links = 2,
		Things = 4,
		PathCosts = 8
	}
}
