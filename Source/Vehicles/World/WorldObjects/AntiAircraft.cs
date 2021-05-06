using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class AntiAircraft : WorldObject
	{
		private const float Spread = 0.1f;

		private float transition;
		private float speedPctPerTick;

		private Vector3 directionFacing;
		private Vector3 destination;
		private Vector3 source;

		private AerialVehicleInFlight target;
		private Settlement firedFrom;

		private Graphic explosionGraphic;

		private int explosionFrame = 0;

		public virtual Vector3 Destination => destination;

		public override Vector3 DrawPos => Vector3.Slerp(source, destination, transition);

		public AntiAircraftDef AADef => def as AntiAircraftDef;

		public Graphic ExplosionGraphic
		{
			get
			{
				if (explosionGraphic is null)
				{
					explosionGraphic = AADef.explosionGraphic?.Graphic;
				}
				return explosionGraphic;
			}
		}

		public virtual void Initialize(Settlement firedFrom, AerialVehicleInFlight target, Vector3 source, float speed)
		{
			this.target = target;
			Vector3 misfire = new Vector3(Rand.Range(-Spread, Spread), Rand.Range(-Spread, Spread), Rand.Range(-Spread, Spread));
			destination = this.target.DrawPosAhead(50) - misfire;
			this.source = source;
			this.firedFrom = firedFrom;
			speedPctPerTick = (AerialVehicleInFlight.PctPerTick / Ext_Math.SphericalDistance(this.source, destination)) * speed;
			InitializeFacing();

			explosionFrame = -1;
		}

		public override void Draw()
		{
			if (!this.HiddenBehindTerrainNow())
			{
				float averageTileSize = Find.WorldGrid.averageTileSize;
				float transitionPct = ExpandableWorldObjectsUtility.TransitionPct;
				float drawPct = 1 + (transitionPct * Find.WorldCameraDriver.AltitudePercent * AerialVehicleInFlight.ExpandingResize);

				bool exploding = (explosionFrame >= 0 && ExplosionGraphic != null);
				float drawSizeMultipler = exploding ? Mathf.Max(AADef.explosionGraphic.drawSize.x, AADef.explosionGraphic.drawSize.y) : AADef.drawSizeMultiplier;

				Vector3 normalized = DrawPos.normalized;
				Quaternion quat = Quaternion.LookRotation(Vector3.Cross(normalized, directionFacing), normalized) * Quaternion.Euler(0f, 90f, 0f);
				Vector3 s = new Vector3(averageTileSize * 0.7f * drawPct * drawSizeMultipler, 5f, averageTileSize * 0.7f * drawPct * drawSizeMultipler);
				Matrix4x4 matrix = default;
				matrix.SetTRS(DrawPos + normalized, quat, s);
				int layer = WorldCameraManager.WorldLayer;

				if (exploding)
				{
					if (explosionGraphic is Graphic_Animate animate)
					{
						Graphics.DrawMesh(MeshPool.plane10, matrix, animate.MatAt(Rot4.North, explosionFrame), layer);
					}
					else
					{
						Graphics.DrawMesh(MeshPool.plane10, matrix, explosionGraphic.MatAt(Rot4.North), layer);
					}
				}
				else
				{
					Graphics.DrawMesh(MeshPool.plane10, matrix, Material, layer);
				}
			}
		}

		public override void Tick()
		{
			base.Tick();
			transition += speedPctPerTick;
			if (transition >= 1)
			{
				if (explosionFrame < 0 && ExplosionGraphic != null)
				{
					explosionFrame = AADef.framesForExplosion;
				}
				else
				{
					explosionFrame--;
					if (explosionFrame < 0)
					{
						Destroy();
					}
				}
			}
		}

		public override void Destroy()
		{
			bool hit = Rand.Range(0f, 1f) <= AADef.accuracy;
			if (hit && (target?.vehicle.inFlight ?? false))
			{
				target.TakeDamage(new DamageInfo(DamageDefOf.Bomb, AADef.damage), firedFrom);
			}
			base.Destroy();
		}

		private void InitializeFacing()
		{
			directionFacing = (DrawPos - destination).normalized;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref transition, "transition");
			Scribe_Values.Look(ref speedPctPerTick, "speedPctPerTick");

			Scribe_Values.Look(ref directionFacing, "directionFacing");
			Scribe_Values.Look(ref destination, "destination");
			Scribe_Values.Look(ref source, "source");

			Scribe_References.Look(ref target, "target");
			Scribe_References.Look(ref firedFrom, "firedFrom");

			Scribe_Values.Look(ref explosionFrame, "explosionFrame");
		}
	}
}
