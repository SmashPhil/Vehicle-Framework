using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public class Reactor_Explosive : Reactor, ITweakFields
	{
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 1, Increment = 0.01f, RoundDecimalPlaces = 2)]
		public float chance = 1;
		[TweakField(SettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 1, Increment = 0.05f, RoundDecimalPlaces = 2)]
		public float maxHealth = 1;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int damage = -1;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public float armorPenetration = -1;
		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int radius;

		public DamageDef damageDef;

		[TweakField(SettingsType = UISettingsType.IntegerBox)]
		public int wickTicks = 180;

		[TweakField]
		public DrawOffsets drawOffsets;

		string ITweakFields.Category => string.Empty;

		string ITweakFields.Label => nameof(Reactor_Explosive);

		public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration)
		{
			if (component.health > 0 && (component.health / component.MaxHealth) <= maxHealth && Rand.Chance(chance))
			{
				Explode(vehicle, component, dinfo);
			}
		}

		internal void Explode(VehiclePawn vehicle, VehicleComponent component, DamageInfo dinfo)
		{
			if (!component.props.hitbox.cells.TryRandomElement(out IntVec2 offset))
			{
				offset = IntVec2.Zero;
			}
			vehicle.AddTimedExplosion(offset, wickTicks, radius, damageDef, damage, armorPenetration: armorPenetration, drawOffsets: drawOffsets);
		}

		void ITweakFields.OnFieldChanged()
		{
		}
	}
}
