using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class Reactor_FuelLeak : Reactor, ITweakFields
	{
		/// <summary>
		/// Health percent in which the component will start to leak.
		/// </summary>
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 1, Increment = 0.05f, RoundDecimalPlaces = 2)]
		public float maxHealth = 0.8f;
		/// <summary>
		/// Rate of fuel leak at unit / second.
		/// </summary>
		/// <remarks><see cref="FloatRange.min"/> is the rate at <see cref="maxHealth"/> while <see cref="FloatRange.max"/> is the rate at 0% health.</remarks>
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		[NumericBoxValues(MinValue = 0)]
		public FloatRange rate = new FloatRange(1, 10);

		string ITweakFields.Category => string.Empty;

		string ITweakFields.Label => nameof(Reactor_FuelLeak);

		public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration)
		{
		}

		void ITweakFields.OnFieldChanged()
		{
		}
	}
}
