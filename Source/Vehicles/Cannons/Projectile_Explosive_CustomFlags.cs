using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class Projectile_Explosive_CustomFlags : Projectile_CustomFlags
	{
        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_Values.Look(ref ticksToDetonation, "ticksToDetonation", 0, false);
        }

        public override void Tick()
		{
			base.Tick();
			if (ticksToDetonation > 0)
			{
				ticksToDetonation--;
				if (ticksToDetonation <= 0)
				{
					Explode();
				}
			}
		}

		protected override void Impact(Thing hitThing)
		{
			if (def.projectile.explosionDelay == 0)
			{
				Explode();
				return;
			}
			landed = true;
			ticksToDetonation = def.projectile.explosionDelay;
			GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, def.projectile.damageDef, launcher.Faction);
		}

		protected void Explode()
        {
			Map map = Map;
            TerrainDef terrainImpact = map.terrainGrid.TerrainAt(Position);
            if(this.def.projectile.explosionDelay == 0 && terrainImpact.IsWater && !Position.GetThingList(Map).AnyNullified(x => x is VehiclePawn vehicle))
            {
				HelperMethods.Explode(this);
				return;
            }

			Destroy(DestroyMode.Vanish);
			if (this.def.projectile.explosionEffect != null)
			{
				Effecter effecter = this.def.projectile.explosionEffect.Spawn();
				effecter.Trigger(new TargetInfo(Position, map, false), new TargetInfo(Position, map, false));
				effecter.Cleanup();
			}
			IntVec3 position = Position;
			Map map2 = map;
			float explosionRadius = this.def.projectile.explosionRadius;
			DamageDef damageDef = this.def.projectile.damageDef;
			Thing launcher = this.launcher;
			int damageAmount = DamageAmount;
			float armorPenetration = ArmorPenetration;
			SoundDef soundExplode = this.def.projectile.soundExplode;
			ThingDef equipmentDef = this.equipmentDef;
			ThingDef def = this.def;
			Thing thing = intendedTarget.Thing;
			ThingDef postExplosionSpawnThingDef = this.def.projectile.postExplosionSpawnThingDef;
			float postExplosionSpawnChance = this.def.projectile.postExplosionSpawnChance;
			int postExplosionSpawnThingCount = this.def.projectile.postExplosionSpawnThingCount;
			ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
			float preExplosionSpawnChance = this.def.projectile.preExplosionSpawnChance;
			int preExplosionSpawnThingCount = this.def.projectile.preExplosionSpawnThingCount;
			GenExplosion.DoExplosion(position, map2, explosionRadius, damageDef, launcher, damageAmount, 
				armorPenetration, soundExplode, equipmentDef, def, thing, postExplosionSpawnThingDef, 
				postExplosionSpawnChance, postExplosionSpawnThingCount, this.def.projectile.applyDamageToExplosionCellsNeighbors, 
				preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, this.def.projectile.explosionChanceToStartFire, 
				this.def.projectile.explosionDamageFalloff, new float?(origin.AngleToFlat(destination)), null);
        }

		private int ticksToDetonation;
	}
}
