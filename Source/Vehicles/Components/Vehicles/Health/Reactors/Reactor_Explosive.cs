using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class Reactor_Explosive : Reactor
	{
		public float chance = 1;
		public float maxHealth = 0;
		public int radius;
		public DamageDef damageDef;

		public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration)
		{
			if (component.health <= maxHealth && Rand.Chance(chance))
			{
				if (!component.props.hitbox.cells.TryRandomElement(out IntVec2 offset))
				{
					offset = IntVec2.Zero;
				}
				IntVec3 loc = new IntVec3(vehicle.Position.x + offset.x, 0, vehicle.Position.z + offset.z);
				GenExplosion.DoExplosion(loc, vehicle.Map, radius, damageDef, vehicle, damageDef.defaultDamage, damageDef.defaultArmorPenetration);
			}
		}
	}
}
