using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

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

			int waterDepth = map.terrainGrid.TerrainAt(proj.Position).IsWater ? map.terrainGrid.TerrainAt(proj.Position) == TerrainDefOf.WaterOceanShallow ||
				map.terrainGrid.TerrainAt(proj.Position) == TerrainDefOf.WaterShallow || map.terrainGrid.TerrainAt(proj.Position) == TerrainDefOf.WaterMovingShallow ? 1 : 2 : 0;
			if (waterDepth == 0)
			{
				SmashLog.Error("<field>waterDepth</field> is 0, but terrain is water.");
			}
			float explosionRadius = (proj.def.projectile.explosionRadius / (2f * waterDepth));
			if (explosionRadius < 1)
			{
				explosionRadius = 1f;
			}
			DamageDef damageDef = proj.def.projectile.damageDef;
			Thing launcher = null;
			int damageAmount = proj.DamageAmount;
			float armorPenetration = proj.ArmorPenetration;
			SoundDef soundExplode;
			soundExplode = SoundDefOf_Vehicles.Explode_BombWater; //Changed for current issues
			SoundStarter.PlayOneShot(soundExplode, new TargetInfo(proj.Position, map, false));
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
	}
}
