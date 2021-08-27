using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public abstract class Graphic_Rotator : Graphic_Single
	{
		public const float MinRotationStep = 0;
		public const float MaxRotationStep = 59;

		public abstract string RegistryKey { get; }

		public virtual float MaxRotationSpeed => MaxRotationStep;
	}
}
