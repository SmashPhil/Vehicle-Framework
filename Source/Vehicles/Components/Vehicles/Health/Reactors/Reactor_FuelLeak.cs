using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class Reactor_FuelLeak : Reactor
	{
		public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration)
		{
		}
	}
}
