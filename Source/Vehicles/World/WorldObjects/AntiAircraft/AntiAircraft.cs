using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public abstract class AntiAircraft : DynamicDrawnWorldObject
	{
		protected float transition;
		protected float speedPctPerTick;

		protected Vector3 directionFacing;
		protected Vector3 destination;
		protected Vector3 source;

		protected AerialVehicleInFlight target;
		protected WorldObject firedFrom;

		protected Graphic explosionGraphic;

		protected int explosionFrame = 0;

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

		public abstract void Initialize(WorldObject firedFrom, AerialVehicleInFlight target, Vector3 source);

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
			if (Rand.Chance(AADef.accuracy) && (target?.vehicle.CompVehicleLauncher.inFlight ?? false))
			{
				IntVec3 randomHit = target.vehicle.OccupiedRect().RandomCell;
				target.TakeDamage(new DamageInfo(DamageDefOf.Bomb, AADef.damage), randomHit.ToIntVec2);
			}
			base.Destroy();
		}

		protected virtual void InitializeFacing()
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
