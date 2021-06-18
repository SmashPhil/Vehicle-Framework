using System;
using Verse;

namespace Vehicles
{
	[Flags]
	public enum TurretDisableType
	{
		Always = 0,
		InFlight = 1,
		Strafing = 2,
		Grounded = 4
	}
}
