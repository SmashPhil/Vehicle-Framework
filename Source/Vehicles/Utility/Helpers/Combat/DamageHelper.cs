using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;
using UnityEngine;

namespace Vehicles
{
	public static class DamageHelper
	{
		/// <summary>
		/// Explode projectile on water with modified effects 
		/// </summary>
		/// <param name="proj"></param>
		public static void Explode(Projectile proj)
		{
			Map map = proj.Map;
			proj.Destroy(DestroyMode.Vanish);
			if (proj.def.projectile.explosionEffect != null)
			{
				Effecter effecter = proj.def.projectile.explosionEffect.Spawn();
				effecter.Trigger(new TargetInfo(proj.Position, map, false), new TargetInfo(proj.Position, map, false));
				effecter.Cleanup();
			}
			IntVec3 position = proj.Position;
			Map map2 = map;

			float waterDepth = map.terrainGrid.TerrainAt(proj.Position) == TerrainDefOf.WaterDeep ||
				map.terrainGrid.TerrainAt(proj.Position) == TerrainDefOf.WaterMovingChestDeep || map.terrainGrid.TerrainAt(proj.Position) == TerrainDefOf.WaterOceanDeep ? 2.5f : 1.5f;
			float explosionRadius = proj.def.projectile.explosionRadius / waterDepth;
			if (explosionRadius < 1)
			{
				explosionRadius = 1f;
			}
			DamageDef damageDef = proj.def.projectile.damageDef;
			Thing launcher = null;
			int damageAmount = proj.DamageAmount;
			float armorPenetration = proj.ArmorPenetration;
			SoundDef soundExplode;
			soundExplode = SoundDefOf_Vehicles.Explode_BombWater;
			ThingDef equipmentDef = null;
			ThingDef def = proj.def;
			Thing thing = null;
			ThingDef postExplosionSpawnThingDef = proj.def.projectile.postExplosionSpawnThingDef;
			float postExplosionSpawnChance = 0.0f;
			float chanceToStartFire = proj.def.projectile.explosionChanceToStartFire * 0.0f;
			int postExplosionSpawnThingCount = proj.def.projectile.postExplosionSpawnThingCount;
			ThingDef preExplosionSpawnThingDef = proj.def.projectile.preExplosionSpawnThingDef;
			GenExplosion.DoExplosion(position, map2, explosionRadius, damageDef, launcher, 
				damAmount: damageAmount, armorPenetration: armorPenetration, explosionSound: soundExplode,
				weapon: equipmentDef, projectile: def, intendedTarget: thing, 
				postExplosionSpawnThingDef: postExplosionSpawnThingDef, postExplosionSpawnChance: postExplosionSpawnChance, postExplosionSpawnThingCount: postExplosionSpawnThingCount, 
				applyDamageToExplosionCellsNeighbors: proj.def.projectile.applyDamageToExplosionCellsNeighbors,
				preExplosionSpawnThingDef: preExplosionSpawnThingDef, preExplosionSpawnChance: proj.def.projectile.preExplosionSpawnChance, preExplosionSpawnThingCount: proj.def.projectile.preExplosionSpawnThingCount, 
				chanceToStartFire: chanceToStartFire, damageFalloff: proj.def.projectile.explosionDamageFalloff);
		}

		public static float EMPChanceToStun(VehicleEMPSeverity severity)
		{
			return severity switch
			{
				VehicleEMPSeverity.Tiny => 0.075f,
				VehicleEMPSeverity.Minor => 0.125f,
				VehicleEMPSeverity.Moderate => 0.15f,
				VehicleEMPSeverity.Severe => 0.25f,
				_ => 0f,
			};
		}

		/// <returns>Length of stun measured in ticks</returns>
		public static int EMPStunLength(VehicleEMPSeverity severity, float damage)
		{
			return severity switch
			{
				VehicleEMPSeverity.Tiny => Mathf.RoundToInt(damage * Rand.Range(0.5f, 1.5f)),
				VehicleEMPSeverity.Minor => Mathf.RoundToInt(damage * Rand.Range(2f, 3f)),
				VehicleEMPSeverity.Moderate => Mathf.RoundToInt(damage * Rand.Range(2f, 3f)),
				VehicleEMPSeverity.Severe => Mathf.RoundToInt(damage * Rand.Range(2.5f, 4f)),
				_ => 0,
			};
		}

		public static float EMPStunDamage(VehicleEMPSeverity severity)
		{
			return severity switch
			{
				VehicleEMPSeverity.Tiny => 0,
				VehicleEMPSeverity.Minor => 0.01f,
				VehicleEMPSeverity.Moderate => 0.025f,
				VehicleEMPSeverity.Severe => 0.075f,
				_ => 0,
			};
		}
	}
}
