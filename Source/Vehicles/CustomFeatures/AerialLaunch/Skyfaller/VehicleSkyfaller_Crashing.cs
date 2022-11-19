using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace Vehicles
{
	//REDO - CACHE LAUNCH PROTOCOL
	public class VehicleSkyfaller_Crashing : VehicleSkyfaller_Arriving
	{
		public const float DefaultAngle = -65;
		private const int RoofHitPreDelay = 15;
		private const int LeaveMapAfterTicks = 220;

		public Rot4 rotCrashing;

		public float shrapnelDirection;

		public int ticksToImpact = LeaveMapAfterTicks;

		private bool SpawnTimedMotes
		{
			get
			{
				return def.skyfaller.moteSpawnTime != float.MinValue && Mathf.Approximately(def.skyfaller.moteSpawnTime, vehicle.CompVehicleLauncher.launchProtocol.TimeInAnimation);
			}
		}

		public override Vector3 DrawPos
		{
			get
			{
				switch (def.skyfaller.movementType)
				{
				case SkyfallerMovementType.Accelerate:
					return SkyfallerDrawPosUtility.DrawPos_Accelerate(base.DrawPos, ticksToImpact, angle, CurrentSpeed);
				case SkyfallerMovementType.ConstantSpeed:
					return SkyfallerDrawPosUtility.DrawPos_ConstantSpeed(base.DrawPos, ticksToImpact, angle, CurrentSpeed);
				case SkyfallerMovementType.Decelerate:
					return SkyfallerDrawPosUtility.DrawPos_Decelerate(base.DrawPos, ticksToImpact, angle, CurrentSpeed);
				default:
					Log.ErrorOnce("SkyfallerMovementType not handled: " + def.skyfaller.movementType, thingIDNumber ^ 1948576711);
					return SkyfallerDrawPosUtility.DrawPos_Accelerate(base.DrawPos, ticksToImpact, angle, CurrentSpeed);
				}
			}
		}

		protected virtual float CurrentSpeed
		{
			get
			{
				if (def.skyfaller.speedCurve is null)
				{
					return def.skyfaller.speed;
				}
				return def.skyfaller.speedCurve.Evaluate(vehicle.CompVehicleLauncher.launchProtocol.TimeInAnimation) * def.skyfaller.speed;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref rotCrashing, "rotCrashing", Rot4.East);
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			float num = 0f;
			if (def.skyfaller.rotateGraphicTowardsDirection)
			{
				num = angle;
			}
			if (def.skyfaller.angleCurve != null)
			{
				angle = def.skyfaller.angleCurve.Evaluate(vehicle.CompVehicleLauncher.launchProtocol.TimeInAnimation);
			}
			if (def.skyfaller.rotationCurve != null)
			{
				num += def.skyfaller.rotationCurve.Evaluate(vehicle.CompVehicleLauncher.launchProtocol.TimeInAnimation);
			}
			if (def.skyfaller.xPositionCurve != null)
			{
				drawLoc.x += def.skyfaller.xPositionCurve.Evaluate(vehicle.CompVehicleLauncher.launchProtocol.TimeInAnimation);
			}
			if (def.skyfaller.zPositionCurve != null)
			{
				drawLoc.z += def.skyfaller.zPositionCurve.Evaluate(vehicle.CompVehicleLauncher.launchProtocol.TimeInAnimation);
			}
			vehicle.DrawAt(drawLoc, num + Rotation.AsInt * 90, flip);
			DrawDropSpotShadow();
		}

		public override void Tick()
		{
			if (SpawnTimedMotes)
			{
				CellRect cellRect = this.OccupiedRect();
				for (int i = 0; i < cellRect.Area * def.skyfaller.motesPerCell; i++)
				{
					FleckMaker.ThrowDustPuff(cellRect.RandomVector3, Map, 2f);
				}
			}
			ticksToImpact--;
			if (ticksToImpact == RoofHitPreDelay)
			{
				HitRoof();
			}
			if (!anticipationSoundPlayed && def.skyfaller.anticipationSound != null && ticksToImpact < def.skyfaller.anticipationSoundTicks)
			{
				anticipationSoundPlayed = true;
				def.skyfaller.anticipationSound.PlayOneShot(new TargetInfo(Position, Map, false));
			}
			if (ticksToImpact == 0)
			{
				Impact();
				return;
			}
			if (ticksToImpact < 0)
			{
				Log.Error("ticksToImpact < 0. Was there an exception? Destroying skyfaller.");
				Destroy(DestroyMode.Vanish);
			}
		}

		protected virtual void Impact()
		{
			if (def.skyfaller.CausesExplosion)
			{
				GenExplosion.DoExplosion(Position, Map, def.skyfaller.explosionRadius, def.skyfaller.explosionDamage, null, 
					GenMath.RoundRandom(def.skyfaller.explosionDamage.defaultDamage * def.skyfaller.explosionDamageFactor), ignoredThings: (!def.skyfaller.damageSpawnedThings) ? vehicle.inventory.innerContainer.ToList() : null);
			}
			//this.SpawnThings();
			CellRect cellRect = this.OccupiedRect();
			for (int i = 0; i < cellRect.Area * def.skyfaller.motesPerCell; i++)
			{
				FleckMaker.ThrowDustPuff(cellRect.RandomVector3, Map, 2f);
			}
			if (def.skyfaller.MakesShrapnel)
			{
				SkyfallerShrapnelUtility.MakeShrapnel(Position, Map, shrapnelDirection, def.skyfaller.shrapnelDistanceFactor, def.skyfaller.metalShrapnelCountRange.RandomInRange, def.skyfaller.rubbleShrapnelCountRange.RandomInRange, true);
			}
			if (def.skyfaller.cameraShake > 0f && Map == Find.CurrentMap)
			{
				Find.CameraDriver.shaker.DoShake(def.skyfaller.cameraShake);
			}
			if (def.skyfaller.impactSound != null)
			{
				def.skyfaller.impactSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(Position, Map, false), MaintenanceType.None));
			}
			FinalizeLanding();
		}

		protected virtual void HitRoof()
		{
			if (!def.skyfaller.hitRoof)
			{
				return;
			}
			CellRect cr = this.OccupiedRect();
			if (cr.Cells.Any((IntVec3 x) => x.Roofed(Map)))
			{
				RoofDef roof = cr.Cells.First((IntVec3 x) => x.Roofed(Map)).GetRoof(Map);
				if (!roof.soundPunchThrough.NullOrUndefined())
				{
					roof.soundPunchThrough.PlayOneShot(new TargetInfo(Position, Map, false));
				}
				RoofCollapserImmediate.DropRoofInCells(cr.ExpandedBy(1).ClipInsideMap(Map).Cells.Where(delegate(IntVec3 c)
				{
					if (!c.InBounds(Map))
					{
						return false;
					}
					if (cr.Contains(c))
					{
						return true;
					}
					if (c.GetFirstPawn(Map) != null)
					{
						return false;
					}
					Building edifice = c.GetEdifice(Map);
					return edifice == null || !edifice.def.holdsRoof;
				}), Map, null);
			}
		}

		public override void PostMake()
		{
			base.PostMake();
			if (def.skyfaller.MakesShrapnel)
			{
				shrapnelDirection = Rand.Range(0f, 360f);
			}
		}

		protected override void FinalizeLanding()
		{
			vehicle.CompVehicleLauncher.inFlight = false;
			GenSpawn.Spawn(vehicle, Position, Map, Rotation);
			vehicle.Angle = angle + Rotation.AsAngle;
			Destroy();
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				vehicle.CompVehicleLauncher.launchProtocol.Prepare(map, Position, Rotation);
				vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Landing);
				delayLandingTicks = vehicle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.delayByTicks;

				ticksToImpact = def.skyfaller.ticksToImpactRange.RandomInRange;
				if (def.skyfaller.MakesShrapnel)
				{
					float num = GenMath.PositiveMod(shrapnelDirection, 360f);
					if (num < 270f && num >= 90f)
					{
						angle = Rand.Range(0f, 33f);
					}
					else
					{
						angle = Rand.Range(-33f, 0f);
					}
				}
				else if (def.skyfaller.angleCurve != null)
				{
					angle = def.skyfaller.angleCurve.Evaluate(0f);
				}
				else
				{
					angle = DefaultAngle;
				}
				Rotation = rotCrashing;
			}
		}
	}
}
