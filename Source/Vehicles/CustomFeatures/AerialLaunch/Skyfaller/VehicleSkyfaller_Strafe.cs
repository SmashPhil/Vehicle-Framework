using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleSkyfaller_Strafe : VehicleSkyfaller_FlyOver
	{
		protected bool shotsFired = false;
		protected List<CompCannons.TurretData> turrets = new List<CompCannons.TurretData>();
		protected Dictionary<VehicleTurret, int> shotsFromTurret = new Dictionary<VehicleTurret, int>();

		private List<VehicleTurret> turretsTmp;
		private List<int> shotsTmp;

		protected float StrafeAreaDistance => Vector3.Distance(start.ToVector3Shifted(), end.ToVector3Shifted());

		protected virtual Vector3 Target(VehicleTurret turret)
		{
			int shots = shotsFromTurret[turret];
			float distFromStart = StrafeAreaDistance * shots / turret.MaxShotsCurrentFireMode;
			Vector3 target = start.ToVector3Shifted().PointFromAngle(distFromStart, angle);

			Pair<float, float> turretLoc = RenderHelper.TurretDrawOffset(angle, turret.turretRenderLocation.x, turret.turretRenderLocation.y, out Pair<float, float> renderOffsets, 0, turret.attachedTo);
			return new Vector3(target.x + turretLoc.First + renderOffsets.First, target.y + turret.drawLayer, target.z + turretLoc.Second + renderOffsets.Second);
		}

		protected virtual Vector3 TurretLocation(VehicleTurret turret)
		{
			float locationRotation = 0f;
			if (turret.attachedTo != null)
			{
				locationRotation = turret.attachedTo.TurretRotation;
			}
			Vector3 calcPosition = DistanceAtMin;
			Pair<float, float> turretLoc = RenderHelper.TurretDrawOffset(angle, turret.turretRenderLocation.x, turret.turretRenderLocation.y, out Pair<float, float> renderOffsets, locationRotation, turret.attachedTo);
			return new Vector3(calcPosition.x + turretLoc.First + renderOffsets.First, calcPosition.y + turret.drawLayer, calcPosition.z + turretLoc.Second + renderOffsets.Second);
		}

		protected virtual void TurretTick()
		{
			if (!turrets.NullOrEmpty())
			{
				for (int i = 0; i < turrets.Count; i++)
				{
					CompCannons.TurretData turretData = turrets[i];
					VehicleTurret turret = turretData.turret;
					if (!turret.HasAmmo && !DebugSettings.godMode)
					{
						turrets.Remove(turretData);
						shotsFired = turrets.NullOrEmpty();
						continue;
					}
					if (turret.OnCooldown)
					{
						turret.SetTarget(LocalTargetInfo.Invalid);
						turrets.Remove(turretData);
						shotsFired = turrets.NullOrEmpty();
						continue;
					}
					turrets[i].turret.AlignToTargetRestricted();
					if (turrets[i].ticksTillShot <= 0)
					{
						FireTurret(turret);
						int shotsIncrement = shotsFromTurret[turret] + 1;
						shotsFromTurret[turret] = shotsIncrement;
						turret.CurrentTurretFiring++;
						turretData.shots--;
						turretData.ticksTillShot = turret.TicksPerShot;
						if (turret.OnCooldown || turretData.shots == 0 || (turret.turretDef.ammunition != null && turret.shellCount <= 0))
						{
							turret.SetTarget(LocalTargetInfo.Invalid);
							turrets.RemoveAll(t => t.turret == turret);
							shotsFired = turrets.NullOrEmpty();
							continue;
						}
					}
					else
					{
						turretData.ticksTillShot--;
					}
					turrets[i] = turretData;
				}
			}
		}

		protected virtual void FireTurret(VehicleTurret turret)
		{
			float horizontalOffset = turret.turretDef.projectileShifting.NotNullAndAny() ? turret.turretDef.projectileShifting[turret.CurrentTurretFiring] : 0;
			Vector3 launchPos = TurretLocation(turret) + new Vector3(horizontalOffset, 1f, turret.turretDef.projectileOffset);

			Vector3 targetPos = Target(turret);
			float range = Vector3.Distance(TurretLocation(turret), targetPos);
			IntVec3 target = targetPos.ToIntVec3() + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(turret.CurrentFireMode.spreadRadius * (range / turret.turretDef.maxRange)))];
			if (turret.CurrentTurretFiring >= turret.turretDef.projectileShifting.Count)
			{
				turret.CurrentTurretFiring = 0;
			}
			
			ThingDef projectile;
			if (turret.turretDef.ammunition != null && !turret.turretDef.genericAmmo)
			{
				projectile = turret.loadedAmmo?.projectileWhenLoaded;
			}
			else
			{
				projectile = turret.turretDef.projectile;
			}
			try
			{
				StrafeProjectile projectile2 = ProjectileSkyfallerMaker.WrapProjectile(projectile, this, launchPos, target.ToVector3Shifted()); //RANDOMIZE TARGETED CELLS
				GenSpawn.Spawn(projectile2, target.ClampInsideMap(Map), Map);
				if (turret.turretDef.ammunition != null)
				{
					turret.ConsumeShellChambered();
				}
				if (turret.turretDef.cannonSound != null)
				{
					turret.turretDef.cannonSound.PlayOneShot(new TargetInfo(Position, Map, false));
				}
				turret.PostTurretFire();
			}
			catch (Exception ex)
			{
				Log.Error($"Exception when firing Cannon: {turret.turretDef.LabelCap} on Pawn: {vehicle.LabelCap}. Exception: {ex.Message}");
			}
		}

		public override void Tick()
		{
			TurretTick();
			if (shotsFired)
			{
				base.Tick();
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (vehicle.CompVehicleLauncher != null && !respawningAfterLoad)
			{
				foreach (VehicleTurret turret in vehicle.CompVehicleLauncher.StrafeTurrets)
				{
					var turretData = turret.GenerateTurretData();
					turretData.shots = turretData.turret.MaxShotsCurrentFireMode;
					turrets.Add(turretData);
					shotsFromTurret.Add(turret, 0);
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref shotsFired, "shotsFired");
			Scribe_Collections.Look(ref turrets, "turrets");
			Scribe_Collections.Look(ref shotsFromTurret, "shotsFromTurret", LookMode.Reference, LookMode.Value, ref turretsTmp, ref shotsTmp);
		}
	}
}
