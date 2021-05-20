using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class StrafeProjectile : Thing
	{
		/// <summary>
		/// Do Not Modify
		/// </summary>
		private static ThingDef strafeProjectileDef;

		public int ticksToImpact;
		public VehicleSkyfaller_Strafe aerialVehicle;
		public ThingDef projectileDef;

		public Vector3 origin;
		public Vector3 destination;

		public override Vector3 DrawPos => ExactPosition;

		public override void Draw()
		{
			Graphics.DrawMesh(MeshPool.GridPlane(projectileDef.graphicData.drawSize), DrawPos, ExactRotation, projectileDef.DrawMatSingle, 0);
		}

		public virtual Vector3 ExactPosition
		{
			get
			{
				Vector3 b = (destination - origin) * Mathf.Clamp01(1f - ticksToImpact / ProjectileTicksToImpact);
				return origin + b + Vector3.up * def.Altitude;
			}
		}

		public virtual Quaternion ExactRotation
		{
			get
			{
				return Quaternion.LookRotation((origin - destination).Yto0());
			}
		}

		protected float ProjectileTicksToImpact
		{
			get
			{
				float num = (origin - destination).magnitude / projectileDef.projectile.SpeedTilesPerTick;
				if (num <= 0f)
				{
					num = 0.001f;
				}
				return num;
			}
		}

		public static ThingDef StrafeProjectileDef
		{
			get
			{
				if (strafeProjectileDef is null)
				{
					strafeProjectileDef = DefDatabase<ThingDef>.GetNamed("StrafeProjectile");
				}
				return strafeProjectileDef;
			}
		}

		public override void Tick()
		{
			ticksToImpact--;
			if (ticksToImpact <= 0)
			{
				Impact();
			}
		}

		protected virtual void Impact()
		{
			if (Position.InBounds(Map))
			{
				Projectile quickProj = (Projectile)GenSpawn.Spawn(projectileDef, Position, Map);
				quickProj.Launch(aerialVehicle, destination.ToIntVec3().ToVector3Shifted(), destination.ToIntVec3(), destination.ToIntVec3(), quickProj.HitFlags, aerialVehicle);
			}
			Destroy();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref origin, "origin");
			Scribe_Values.Look(ref destination, "destination");
			Scribe_Values.Look(ref ticksToImpact, "ticksToImpact");
			Scribe_References.Look(ref aerialVehicle, "aerialVehicle");
			Scribe_Defs.Look(ref projectileDef, "projectileDef");
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				ticksToImpact = Mathf.CeilToInt(ProjectileTicksToImpact);
			}
		}
	}
}
