using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleComp : ThingComp
	{
		public virtual IEnumerable<AnimationDriver> Animations { get; }

		public virtual void PostLoad()
		{
		}
		
		public virtual void PostGenerationSetup()
		{
		}

		public virtual void SpawnedInGodMode()
		{
		}
	}
}
