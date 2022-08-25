using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class Reactor_Explosive : Reactor
	{
		public float chance;
		public int radius;
		public DamageDef damageDef;

		public override void Hit(VehiclePawn vehicle, VehicleComponent component, float damage, bool penetrated)
		{
			if (Rand.Range(0, 1) < chance && component.props.hitbox.cells.TryRandomElement(out IntVec2 offset))
			{
				IntVec3 loc = new IntVec3(vehicle.Position.x + offset.x, 0, vehicle.Position.z + offset.z);
				GenExplosion.DoExplosion(loc, vehicle.Map, radius, damageDef, vehicle, damageDef.defaultDamage, damageDef.defaultArmorPenetration);
			}
		}
	}
}
