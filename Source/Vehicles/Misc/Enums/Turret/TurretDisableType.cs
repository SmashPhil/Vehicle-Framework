using System;
using Verse;

namespace Vehicles
{
	[Flags]
	public enum TurretDisableType
	{
		Always = 1 << 0,
		InFlight = 1 << 1,
		Strafing = 1 << 2,
		Grounded = 1 << 3
	}
}
