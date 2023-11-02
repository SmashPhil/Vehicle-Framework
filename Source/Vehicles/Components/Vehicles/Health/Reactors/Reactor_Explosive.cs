using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class Reactor_Explosive : Reactor
	{
		public float chance = 1;
		public float maxHealth = 1;
		public int damage = -1;
		public float armorPenetration = -1;
		public int radius;
		public DamageDef damageDef;

		public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration)
		{
			if ((component.health / component.props.health) <= maxHealth && Rand.Chance(chance))
			{
				if (!component.props.hitbox.cells.TryRandomElement(out IntVec2 offset))
				{
					offset = IntVec2.Zero;
				}
				IntVec2 loc = new IntVec2(offset.x, offset.z);
				IntVec2 adjustedLoc = loc.RotatedBy(vehicle.Rotation, vehicle.VehicleDef.Size);
				IntVec3 explosionCell = new IntVec3(adjustedLoc.x + vehicle.Position.x, 0, adjustedLoc.z + vehicle.Position.z);
				int damage = this.damage > 0 ? this.damage : damageDef.defaultDamage;
				float armorPenetration = this.armorPenetration > 0 ? this.armorPenetration : damageDef.defaultArmorPenetration;
				GenExplosion.DoExplosion(explosionCell, vehicle.Map, radius, damageDef, vehicle, damage, armorPenetration);
			}
		}
	}
}
