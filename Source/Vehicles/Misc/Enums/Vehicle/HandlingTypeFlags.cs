using System;
namespace Vehicles
{
	[Flags]
	public enum HandlingTypeFlags 
	{
		Cannon = 1 << 0,
		Turret = 1 << 1,
		Movement = 1 << 2
	}
}
