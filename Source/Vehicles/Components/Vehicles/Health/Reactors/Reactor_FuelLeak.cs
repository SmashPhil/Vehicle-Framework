using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class Reactor_FuelLeak : Reactor
	{
		/// <summary>
		/// Health percent in which the component will start to leak.
		/// </summary>
		public float maxHealth = 0.8f;
		/// <summary>
		/// Rate of fuel leak at unit / second.
		/// </summary>
		/// <remarks><see cref="FloatRange.min"/> is the rate at <see cref="maxHealth"/> while <see cref="FloatRange.max"/> is the rate at 0% health.</remarks>
		public FloatRange rate = new FloatRange(1, 10);

		public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration)
		{
		}
	}
}
