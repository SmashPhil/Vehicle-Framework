using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class LaunchRestriction_ComponentHealth : LaunchRestriction
	{
		private SimpleDictionary<string, float> components = new SimpleDictionary<string, float>();

		public override bool CanStartProtocol(VehiclePawn vehicle, Map map, IntVec3 position, Rot4 rot)
		{
			if (!components.NullOrEmpty())
			{
				foreach ((string key, float percent) in components)
				{
					if (vehicle.statHandler.GetComponentHealthPercent(key) < percent)
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}
