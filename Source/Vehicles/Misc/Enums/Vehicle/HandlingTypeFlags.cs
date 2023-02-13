using System;
namespace Vehicles
{
	[Flags]
	public enum HandlingTypeFlags 
	{
		None = 0,
		Movement = 1 << 0,
		Turret = 1 << 1,
	}
}
