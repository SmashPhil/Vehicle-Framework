using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class AirDefense : IExposable, ILoadReferenceable
	{
		protected const int TickRareInterval = 250;
		protected const int TickLongInterval = 2000;
		protected const float ConeFadeInterval = 0.005f;

		public int defenseBuildings = 3;
		public AntiAircraftDef antiAircraftDef;

		public WorldObject parent;
		protected AntiAircraftWorker worker;

		public float angle = 0;
		protected Mesh coneMesh;
		protected Vector3 centerTile = Vector3.zero;
		public HashSet<AerialVehicleInFlight> activeTargets = new HashSet<AerialVehicleInFlight>();

		protected int cooldownTimer;
		protected int uniqueId = -1;
		public int searchDirection = 1;
		protected float spotlightAlpha = 1;

		public AirDefense(WorldObject parent)
		{
			this.parent = parent;
			uniqueId = VehicleIdManager.Instance.GetNextAirDefenseId();
			antiAircraftDef = AntiAircraftDefOf.FlakProjectile;
			if (parent.Faction != Faction.OfPlayerSilentFail)
			{
				searchDirection = Rand.Chance(0.5f) ? 1 : -1;
			}
		}

		public virtual float Arc => antiAircraftDef.properties.arc;

		public virtual float MaxDistance => antiAircraftDef.properties.distance + 0.5f;

		public AerialVehicleInFlight CurrentTarget => Worker.CurrentTarget;

		public Vector3 CenterTile
		{
			get
			{
				if (centerTile == Vector3.zero)
				{
					centerTile = Find.WorldGrid.GetTileCenter(parent.Tile);
				}
				return centerTile;
			}
		}

		public virtual AntiAircraftWorker Worker
		{
			get
			{
				worker ??= (AntiAircraftWorker)Activator.CreateInstance(antiAircraftDef.antiAircraftWorker, new object[] { this, antiAircraftDef });
				return worker;
			}
		}
		
		public Mesh SpotlightMesh
		{
			get
			{
				coneMesh ??= RenderHelper.NewConeMesh(Find.WorldGrid.averageTileSize, antiAircraftDef.properties.arc);
				return coneMesh;
			}
		}

		public virtual void DrawSpotlightOverlay()
		{
			if (Worker.ShouldDrawSearchLight)
			{
				if (spotlightAlpha < 1)
				{
					spotlightAlpha = Mathf.Clamp01(spotlightAlpha + ConeFadeInterval);
				}
				//Add 1/2 tile for offset from center of home tile
				float coneDistance = MaxDistance;
				Vector3 normalized = CenterTile.normalized;
				Vector3 direction = Vector3.Cross(normalized, Vector3.up);

				Quaternion quat = Quaternion.LookRotation(direction, normalized) * Quaternion.Euler(0f, angle - 90, 0f);
				Vector3 s = new Vector3(coneDistance, 1, coneDistance);
				Matrix4x4 matrix = default;
				matrix.SetTRS(CenterTile + normalized * AerialVehicleInFlight.TransitionTakeoff, quat, s);
				int layer = WorldCameraManager.WorldLayer;
				Graphics.DrawMesh(SpotlightMesh, matrix, TexData.WorldFullMatRed, layer);
			}
			else if (spotlightAlpha > 0)
			{
				spotlightAlpha = Mathf.Clamp01(spotlightAlpha - ConeFadeInterval);
			}
		}

		public virtual void Attack()
		{
			Worker.Tick();
			if (Find.TickManager.TicksGame % TickRareInterval == 0)
			{
				Worker.TickRare();
			}
			if (Find.TickManager.TicksGame % TickLongInterval == 0)
			{
				Worker.TickLong();
			}
		}

		public string GetUniqueLoadID()
		{
			return $"AirDefense_{GetType()}_{uniqueId}";
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref cooldownTimer, "cooldownTimer");
			Scribe_Values.Look(ref defenseBuildings, "defenseBuildings");
			Scribe_Defs.Look(ref antiAircraftDef, "antiAircraftDef");
			Scribe_Collections.Look(ref activeTargets, "activeTargets", LookMode.Reference);
			Scribe_References.Look(ref parent, "parent");

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				activeTargets ??= new HashSet<AerialVehicleInFlight>();
				antiAircraftDef = AntiAircraftDefOf.FlakProjectile;
			}
		}
	}
}