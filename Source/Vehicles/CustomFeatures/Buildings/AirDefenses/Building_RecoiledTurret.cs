using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public class Building_RecoiledTurret : Building_Artillery
	{
		public Building_RecoiledTurret()
		{
			top = (TurretTop)Activator.CreateInstance(typeof(TurretTop_Recoiled), new object[] { this });
		}

		protected TurretTop_Recoiled TopRecoiled => top as TurretTop_Recoiled;

		public virtual void Notify_Recoiled() => TopRecoiled.Notify_TurretRecoil();

		public override void NotifyTargetInRange(GlobalTargetInfo target)
		{
			base.NotifyTargetInRange(target);
			if (AttackVerb is Verb_ShootWorldRecoiled shootWorldRecoiled)
			{
				if (CanExtractShell && MannedByColonist)
				{
					CompChangeableProjectile compChangeableProjectile = gun.TryGetComp<CompChangeableProjectile>();
					if (!compChangeableProjectile.allowedShellsSettings.AllowedToAccept(compChangeableProjectile.LoadedShell))
					{
						ExtractShell();
					}
				}
				if (forcedWorldTarget.IsValid && !CanSetForcedTarget)
				{
					ResetForcedTarget();
				}
				if (!CanToggleHoldFire)
				{
					AccessTools.Field(typeof(Building_TurretGun), "holdFire")?.SetValue(this, false);
				}
				if (forcedWorldTarget.ThingDestroyed)
				{
					ResetForcedTarget();
				}
				if (Active && (mannableComp == null || mannableComp.MannedNow) && !stunner.Stunned && Spawned)
				{
					GunCompEq.verbTracker.VerbsTick();
					if (AttackVerb.state != VerbState.Bursting)
					{
						if (WarmingUp)
						{
							burstWarmupTicksLeft--;
							if (burstWarmupTicksLeft == 0)
							{
								BeginBurst();
							}
						}
						else
						{
							if (burstCooldownTicksLeft > 0)
							{
								burstCooldownTicksLeft--;
								if (IsMortar)
								{
									if (progressBarEffecter == null)
									{
										progressBarEffecter = EffecterDefOf.ProgressBar.Spawn();
									}
									progressBarEffecter.EffectTick(this, TargetInfo.Invalid);
									MoteProgressBar mote = ((SubEffecter_ProgressBar)progressBarEffecter.children[0]).mote;
									mote.progress = 1f - Math.Max(burstCooldownTicksLeft, 0) / (float)BurstCooldownTime().SecondsToTicks();
									mote.offsetZ = -0.8f;
								}
							}
							if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
							{
								TryStartShootSomething(true);
							}
						}
						top.TurretTopTick();
						return;
					}
				}
				else
				{
					ResetCurrentTarget();
				}
				if (WarmingUp)
				{
					burstWarmupTicksLeft--;
					if (burstWarmupTicksLeft == 0)
					{
						BeginBurst();
					}
				}
			}
		}
	}
}
