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
	public class ProjectileSkyfaller : Thing
	{
		public int ticksToImpact;
		public Thing caster;
		public ThingDef projectileDef;

		public Vector3 origin;
		public Vector3 destination;
		public bool reverseDraw = false;
		public float speedTilesPerTick = 1;

		public override Vector3 DrawPos => ExactPosition;

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			Graphics.DrawMesh(MeshPool.GridPlane(projectileDef.graphicData.drawSize), drawLoc, ExactRotation, projectileDef.DrawMatSingle, 0);
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
				Vector3 forward = reverseDraw ? destination - origin : origin - destination;
				return Quaternion.LookRotation(forward.Yto0());
			}
		}

		protected float ProjectileTicksToImpact
		{
			get
			{
				float num = (origin - destination).magnitude / speedTilesPerTick;
				if (num <= 0f)
				{
					num = 0.001f;
				}
				return num;
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
				quickProj.Launch(caster, destination.ToIntVec3().ToVector3Shifted(), destination.ToIntVec3(), destination.ToIntVec3(), quickProj.HitFlags, false, caster);
			}
			Destroy();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref origin, "origin");
			Scribe_Values.Look(ref destination, "destination");
			Scribe_Values.Look(ref reverseDraw, "reverseDraw");
			Scribe_Values.Look(ref ticksToImpact, "ticksToImpact");
			Scribe_Values.Look(ref speedTilesPerTick, "speedTilesPerTick");
			Scribe_References.Look(ref caster, "caster");
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
