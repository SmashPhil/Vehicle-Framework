using System;
using UnityEngine;
using Verse;

namespace Vehicles;

// CE hooks for compatibility
public partial class VehicleTurret
{
  /// <summary>
  /// (projectileDef, ammoDef, AmmoSetDef, origin, intendedTarget, launcher, shotAngle, shotRotation, shotHeight, shotSpeed)
  /// </summary>
  /// <returns>CE projectile</returns>
  public static
    Func<ThingDef, ThingDef, Def, Vector2, LocalTargetInfo, VehiclePawn, float, float, float, float,
      object> LaunchProjectileCE;

  /// <summary>
  /// (velocity, range, shooter, target, origin, flyOverhead, gravityModifier, sway, spread, recoil)
  /// </summary>
  /// <returns>2-angles</returns>
  public static
    Func<float, float, Thing, LocalTargetInfo, Vector3, bool, float, float, float, float, Vector2>
    ProjectileAngleCE;

  /// <summary>
  /// (ammoset name)
  /// </summary>
  /// <returns>AmmoSetDef</returns>
  public static Func<string, Def> LookupAmmosetCE;

  /// <summary>
  /// (projectileDef, ammoDef, AmmoSetDef, turret, recoilAmount)
  /// </summary>
  public static Action<ThingDef, ThingDef, Def, VehicleTurret, float> NotifyShotFiredCE;


  /// <summary>
  /// ammoDef, AmmoSetDef, spread
  /// </summary>
  /// <returns>(projectileCount, spread)</returns>
  public static Func<ThingDef, Def, float, Tuple<int, float>> LookupProjectileCountAndSpreadCE;

  internal void FireTurretCE(ThingDef projectileDef, Vector3 launchPos)
  {
    float speed = def.projectileSpeed > 0 ?
      def.projectileSpeed :
      projectileDef.projectile.speed;
    float swayAndSpread =
      Mathf.Atan2(CurrentFireMode.forcedMissRadius, MaxRange) * Mathf.Rad2Deg;
    float sway = swayAndSpread * 0.84f;
    float spread = swayAndSpread * 0.16f;
    // recoil should be taken from the def mod extension, this is just an arbitrary default based
    // on the backward recoil for the turret animation, and not related to CE's vertical recoil
    // accuracy factor, defauling to 0 if turret has no recoil
    float recoil = (def.recoil != null) ? def.recoil.distanceTotal : 0.0f;
    float shotHeight = 1f;

    CETurretDataDefModExtension turretData =
      def.GetModExtension<CETurretDataDefModExtension>();
    if (turretData != null)
    {
      if (turretData.speed > 0)
        speed = turretData.speed;

      if (turretData.sway >= 0)
        sway = turretData.sway;

      if (turretData.spread >= 0)
        spread = turretData.spread;

      recoil = turretData.recoil;
      shotHeight = turretData.shotHeight;

      if (turretData._ammoSet == null && turretData.ammoSet != null)
      {
        turretData._ammoSet = LookupAmmosetCE(turretData.ammoSet);
      }
    }

    int projectileCount = 1;
    if (LookupProjectileCountAndSpreadCE != null)
    {
      (projectileCount, spread) =
        LookupProjectileCountAndSpreadCE(loadedAmmo, turretData?._ammoSet, spread);
    }

    float distance = (launchPos - targetInfo.CenterVector3).magnitude;

    Vector2 vce = ProjectileAngleCE(speed, distance, vehicle, targetInfo,
      new Vector3(launchPos.x, shotHeight, launchPos.z), projectileDef.projectile.flyOverhead, 1f,
      sway, 0,
      recoil * CurrentTurretFiring);
    float sa = vce.y;
    float tr = -TurretRotation + vce.x;
    do
    {
      double randomSpread = Rand.Value * spread;
      double spreadDirection = Rand.Value * Math.PI * 2;
      vce.y = (float)(randomSpread * Math.Sin(spreadDirection));
      vce.x = (float)(randomSpread * Math.Cos(spreadDirection));
      LaunchProjectileCE(projectileDef, loadedAmmo, turretData?._ammoSet,
        new Vector2(launchPos.x, launchPos.z), targetInfo, vehicle,
        sa + vce.y * Mathf.Deg2Rad, tr + vce.x, shotHeight, speed);
    } while (--projectileCount > 0);

    NotifyShotFiredCE?.Invoke(projectileDef, loadedAmmo, turretData?._ammoSet, this, recoil);
  }
}