using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public abstract class Graphic_Rotator : Graphic_RGB
	{
		public abstract int RegistryKey { get; }

		public virtual float ModifyIncomingRotation(float rotation)
		{
			return rotation;
		}
	}
}
