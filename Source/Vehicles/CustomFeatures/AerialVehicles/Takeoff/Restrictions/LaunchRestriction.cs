using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles
{
	public abstract class LaunchRestriction
	{
		public abstract bool CanStartProtocol(VehiclePawn vehicle, Map map, IntVec3 position, Rot4 rot);

		public virtual void DrawRestrictionsTargeter(VehiclePawn vehicle, Map map, IntVec3 position, Rot4 rot)
		{
		}
	}
}
