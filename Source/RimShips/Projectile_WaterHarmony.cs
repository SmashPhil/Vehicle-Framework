using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using Harmony;
using RimWorld;
using UnityEngine;

namespace RimShips
{
    [HarmonyPatch(typeof(Projectile_Explosive), "Impact")]
    public class Projectile_ExplosivePatch : Projectile
    {
        [HarmonyPrefix]
        protected static bool Prefix(Thing hitThing, ref Projectile __instance)
        {
            Map map = __instance.Map;
            if(__instance.def.projectile.explosionDelay == 0 && map.terrainGrid.TerrainAt(__instance.Position).IsWater)
            { 
                __instance.Destroy(DestroyMode.Vanish);
                if (__instance.def.projectile.explosionEffect != null)
                {
                    Effecter effecter = __instance.def.projectile.explosionEffect.Spawn();
                    effecter.Trigger(new TargetInfo(__instance.Position, map, false), new TargetInfo(__instance.Position, map, false));
                    effecter.Cleanup();
                }
                IntVec3 position = __instance.Position;
                Map map2 = map;
                float explosionRadius = (__instance.def.projectile.explosionRadius / 5.0f);
                DamageDef damageDef = __instance.def.projectile.damageDef;
                Thing launcher = null;
                int damageAmount = __instance.DamageAmount;
                float armorPenetration = __instance.ArmorPenetration;
                SoundDef soundExplode;
                if (__instance.def.HasModExtension<Projectile_Water>()) { soundExplode = __instance.def.GetModExtension<Projectile_Water>().soundExplodeWater; }
                else { soundExplode = __instance.def.projectile.soundHitThickRoof; Log.Message("Missing Water Explosion sound from " + __instance); }
                Verse.Sound.SoundStarter.PlayOneShot(__instance.def.projectile.soundExplode, new TargetInfo(__instance.Position, map, false));
                ThingDef equipmentDef = null;
                ThingDef def = __instance.def;
                Thing thing = null;
                ThingDef postExplosionSpawnThingDef = __instance.def.projectile.postExplosionSpawnThingDef;
                float postExplosionSpawnChance = 0.0f;
                float chanceToStartFire = __instance.def.projectile.explosionChanceToStartFire * 0.0f;
                int postExplosionSpawnThingCount = __instance.def.projectile.postExplosionSpawnThingCount;
                ThingDef preExplosionSpawnThingDef = __instance.def.projectile.preExplosionSpawnThingDef;
                GenExplosion.DoExplosion(position, map2, explosionRadius, damageDef, launcher, damageAmount, armorPenetration, soundExplode,
                    equipmentDef, def, thing, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount,
                    __instance.def.projectile.applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, __instance.def.projectile.preExplosionSpawnChance,
                    __instance.def.projectile.preExplosionSpawnThingCount, chanceToStartFire, __instance.def.projectile.explosionDamageFalloff);
                return false;
            }
            return true;
        }
    }

}