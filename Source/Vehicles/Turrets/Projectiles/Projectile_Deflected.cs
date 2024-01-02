using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using SmashTools;
using UnityEngine;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class Projectile_Deflected : Projectile
	{
		private Projectile projectile;

		private static MethodInfo impactMethod;
		private static Material shadowMaterialRef;

		static Projectile_Deflected()
		{
			impactMethod = AccessTools.Method(typeof(Projectile), "Impact");
			shadowMaterialRef = (Material)AccessTools.Field(typeof(Projectile), "shadowMaterial").GetValue(null);
		}

		public override float ArmorPenetration => projectile.ArmorPenetration;

		public override int DamageAmount => projectile.DamageAmount;

		public override Material DrawMat => projectile.DrawMat;

		public float DeflectedArcHeight
		{
			get
			{
				return 0;
			}
		}

		public override void Draw()
		{
			float arc = DeflectedArcHeight * GenMath.InverseParabola(DistanceCoveredFraction);
			Vector3 pos = DrawPos;
			pos += Vector3.forward * arc;
			if (projectile.def.projectile.shadowSize > 0)
			{
				DrawShadow(pos, arc);
			}
			Graphics.DrawMesh(MeshPool.GridPlane(projectile.def.graphicData.drawSize), pos, ExactRotation, DrawMat, 0);
			Comps_PostDraw();
		}

		private void DrawShadow(Vector3 drawLoc, float height)
		{
			if (shadowMaterialRef == null)
			{
				return;
			}
			float num = projectile.def.projectile.shadowSize * Mathf.Lerp(1f, 0.6f, height);
			Vector3 s = new Vector3(num, 1f, num);
			Vector3 depth = new Vector3(0f, -0.01f, 0f);
			Matrix4x4 matrix = new Matrix4x4();
			matrix.SetTRS(drawLoc + depth, Quaternion.identity, s);
			Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterialRef, 0);
		}

		public void Deflect(Projectile projectile, Thing deflectedOff, float distance, float angle, float arcHeight = 0)
		{
			this.projectile = projectile;
			Vector3 origin = projectile.ExactPosition;
			Vector3 destination = origin.PointFromAngle(distance, angle);
			Map map = projectile.Map;
			projectile.DeSpawn();
			GenSpawn.Spawn(this, origin.ToIntVec3(), map);
			Launch(deflectedOff, origin, destination.ToIntVec3(), destination.ToIntVec3(), ProjectileHitFlags.IntendedTarget);
		}

		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			GenSpawn.Spawn(projectile, Position, Map);
			impactMethod.Invoke(projectile, new object[] { hitThing, blockedByShield });
			Destroy();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref projectile, nameof(projectile));
		}
	}
}
