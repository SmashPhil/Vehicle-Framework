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
		protected List<CompVehicleTurrets.TurretData> turrets = new List<CompVehicleTurrets.TurretData>();
		protected Dictionary<VehicleTurret, int> shotsFromTurret = new Dictionary<VehicleTurret, int>();

		private List<VehicleTurret> turretsTmp;
		private List<int> shotsTmp;

		protected float StrafeAreaDistance => Vector3.Distance(start.ToVector3Shifted(), end.ToVector3Shifted());

		protected virtual Vector3 Target(VehicleTurret turret)
		{
			int shots = shotsFromTurret[turret];
			float distFromStart = StrafeAreaDistance * shots / turret.MaxShotsCurrentFireMode;
			Vector3 target = start.ToVector3Shifted().PointFromAngle(distFromStart, angle);

			Vector2 turretLoc = RenderHelper.TurretDrawOffset(turret.vehicle.FullRotation, turret.renderProperties, 0, turret.attachedTo);
			return new Vector3(target.x + turretLoc.x, target.y + turret.drawLayer, target.z + turretLoc.y);
		}

		protected virtual Vector3 TurretLocation(VehicleTurret turret)
		{
			float locationRotation = 0f;
			if (turret.attachedTo != null)
			{
				locationRotation = turret.attachedTo.TurretRotation;
			}
			Vector3 calcPosition = DistanceAtMin;
			Vector2 turretLoc = RenderHelper.TurretDrawOffset(turret.vehicle.FullRotation, turret.renderProperties, locationRotation, turret.attachedTo);
			return new Vector3(calcPosition.x + turretLoc.x, calcPosition.y + turret.drawLayer, calcPosition.z + turretLoc.y);
		}

		protected virtual void TurretTick()
		{
			if (!turrets.NullOrEmpty())
			{
				for (int i = 0; i < turrets.Count; i++)
				{
					CompVehicleTurrets.TurretData turretData = turrets[i];
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
				float speedTicksPerTile = projectile.projectile.SpeedTilesPerTick;
				if (turret.turretDef.projectileSpeed > 0)
				{
					speedTicksPerTile = turret.turretDef.projectileSpeed;
				}
				ProjectileSkyfaller projectile2 = ProjectileSkyfallerMaker.WrapProjectile(ProjectileSkyfallerDefOf.ProjectileSkyfaller, 
					projectile, this, launchPos, target.ToVector3Shifted(), speedTicksPerTile); //REDO - RANDOMIZE TARGETED CELLS
				GenSpawn.Spawn(projectile2, target.ClampInsideMap(Map), Map);
				if (turret.turretDef.ammunition != null)
				{
					turret.ConsumeShellChambered();
				}
				if (turret.turretDef.shotSound != null)
				{
					turret.turretDef.shotSound.PlayOneShot(new TargetInfo(Position, Map, false));
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
