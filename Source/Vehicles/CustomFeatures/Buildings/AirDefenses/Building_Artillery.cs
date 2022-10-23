using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class Building_Artillery : Building_TurretGun
	{
		public const float WorldObjectOffsetPercent = 0.1f;
		public const float MaxMapDistance = 500;

		private static readonly Material AimPieMaterial = SolidColorMaterials.SimpleSolidColorMaterial(new Color(1f, 1f, 1f, 0.3f), false);

		protected GlobalTargetInfo worldTarget = GlobalTargetInfo.Invalid;

		protected GlobalTargetInfo forcedWorldTarget = GlobalTargetInfo.Invalid;

		protected GlobalTargetInfo lastAttackedWorldTarget;

		protected int lastAttackedWorldTargetTick;

		public Building_Artillery()
		{
			top = (TurretTop)Activator.CreateInstance(typeof(TurretTop_Artillery), new object[] { this });
		}

		protected TurretTop_Artillery TopArtillery => top as TurretTop_Artillery;

		public virtual Verb ActiveVerb { get; protected set; }

		public override Verb AttackVerb => ActiveVerb ?? base.AttackVerb;

		public bool WarmingUp
		{
			get
			{
				return burstWarmupTicksLeft > 0;
			}
		}

		public bool PlayerControlled
		{
			get
			{
				return (Faction == Faction.OfPlayer || MannedByColonist) && !MannedByNonColonist;
			}
		}

		public bool CanSetForcedTarget
		{
			get
			{
				return mannableComp != null && PlayerControlled;
			}
		}

		public bool CanToggleHoldFire
		{
			get
			{
				return PlayerControlled;
			}
		}

		public bool IsMortar
		{
			get
			{
				return def.building.IsMortar;
			}
		}

		public bool IsMortarOrProjectileFliesOverhead
		{
			get
			{
				return AttackVerb.ProjectileFliesOverhead() || IsMortar;
			}
		}

		public bool CanExtractShell
		{
			get
			{
				if (!PlayerControlled)
				{
					return false;
				}
				CompChangeableProjectile compChangeableProjectile = gun.TryGetComp<CompChangeableProjectile>();
				return compChangeableProjectile != null && compChangeableProjectile.Loaded;
			}
		}

		public bool MannedByColonist
		{
			get
			{
				return mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction == Faction.OfPlayer;
			}
		}

		public bool MannedByNonColonist
		{
			get
			{
				return mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction != Faction.OfPlayer;
			}
		}

		public virtual GlobalTargetInfo CurrentWorldTarget
		{
			get
			{
				return worldTarget;
			}
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}

			yield return new Command_Action()
			{
				defaultLabel = ActiveVerb.ToString(),
				action = delegate ()
				{
					var verbs = GunCompEq.AllVerbs;
					int current = verbs.IndexOf(ActiveVerb) + 1;
					if (current >= verbs.Count) { current = 0; }
					ActiveVerb = verbs[current];
				}
			};

			yield return new Command_Action()
			{
				defaultLabel = "Reset Target",
				action = delegate()
				{
					ResetCurrentTarget();
					ResetForcedTarget();
				}
			};
		}

		public override void Draw()
		{
			TopArtillery.DrawTurret();
			if (def.drawerType == DrawerType.RealtimeOnly)
			{
				DrawAt(DrawPos, false);
				return;
			}
			Comps_PostDraw();
		}

		public override void Tick()
		{
			base.Tick();
			if (TopArtillery is TurretTop_Recoiled topRecoiled)
			{
				topRecoiled.RecoilTick();
			}
			if (Active && (mannableComp is null || mannableComp.MannedNow) && !stunner.Stunned && Spawned && AttackVerb.state != VerbState.Bursting)
			{
				TopArtillery.Tick();
			}
		}

		protected virtual void ExtractShell()
		{
			GenPlace.TryPlaceThing(gun.TryGetComp<CompChangeableProjectile>().RemoveShell(), Position, Map, ThingPlaceMode.Near, null, null, default);
		}

		protected virtual void ResetForcedTarget()
		{
			forcedTarget = LocalTargetInfo.Invalid;
			forcedWorldTarget = GlobalTargetInfo.Invalid;
			burstWarmupTicksLeft = 0;
			if (burstCooldownTicksLeft <= 0)
			{
				TryStartShootSomething(false);
			}
		}

		protected virtual void ResetCurrentTarget()
		{
			currentTargetInt = LocalTargetInfo.Invalid;
			worldTarget = GlobalTargetInfo.Invalid;
			burstWarmupTicksLeft = 0;
		}

		public virtual void NotifyTargetInRange(GlobalTargetInfo target)
		{
			worldTarget = target;
		}

		public virtual void NotifyTargetOutOfRange(GlobalTargetInfo target)
		{
			if (worldTarget == target)
			{
				ResetCurrentTarget();
			}
		}

		protected void OnAttackedTarget(GlobalTargetInfo target)
		{
			lastAttackedWorldTargetTick = Find.TickManager.TicksGame;
			lastAttackedWorldTarget = target;
		}

		protected new virtual void BeginBurst()
		{
			if (CurrentWorldTarget.IsValid)
			{
				if (AttackVerb is Verb_ShootWorldRecoiled worldRecoiledVerb)
				{
					worldRecoiledVerb.TryStartCastOn(CurrentWorldTarget, TopArtillery.CurRotation, false, false);
				}
				//Else Condition for Verb_ShootWorld
			}
			if (CurrentTarget.IsValid)
			{
				base.BeginBurst();
			}
		}

		protected new virtual void TryStartShootSomething(bool canBeginBurstImmediately)
		{
			if (progressBarEffecter != null)
			{
				progressBarEffecter.Cleanup();
				progressBarEffecter = null;
			}
			if (!Spawned || ((bool)AccessTools.Field(typeof(Building_TurretGun), "holdFire")?.GetValue(this) && CanToggleHoldFire) || (AttackVerb.ProjectileFliesOverhead() && Map.roofGrid.Roofed(Position)) || !AttackVerb.Available())
			{
				ResetCurrentTarget();
				return;
			}
			bool isValid = currentTargetInt.IsValid || worldTarget.IsValid;
			if (forcedTarget.IsValid)
			{
				currentTargetInt = forcedTarget;
			}
			else
			{
				currentTargetInt = TryFindNewTarget();
			}
			if (forcedWorldTarget.IsValid)
			{
				worldTarget = forcedWorldTarget;
			}
			else
			{
				//Find new target?
			}
			if (!isValid && (currentTargetInt.IsValid || worldTarget.IsValid))
			{
				SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(Position, Map, false));
			}
			if (!currentTargetInt.IsValid && !worldTarget.IsValid)
			{
				ResetCurrentTarget();
				return;
			}
			float randomInRange = this.def.building.turretBurstWarmupTime.RandomInRange;
			if (randomInRange > 0f)
			{
				burstWarmupTicksLeft = randomInRange.SecondsToTicks();
				return;
			}
			if (canBeginBurstImmediately)
			{
				BeginBurst();
				return;
			}
			burstWarmupTicksLeft = 1;
		}

		public override void DrawExtraSelectionOverlays()
		{
			float range = AttackVerb.verbProps.range;
			if (range < 90f)
			{
				GenDraw.DrawRadiusRing(Position, range);
			}
			float num = AttackVerb.verbProps.EffectiveMinRange(true);
			if (num < 90f && num > 0.1f)
			{
				GenDraw.DrawRadiusRing(Position, num);
			}
			if (WarmingUp)
			{
				int degreesWide = (int)(burstWarmupTicksLeft * 0.5f);
				float offset = def.size.x * 0.5f;
				if (CurrentTarget.IsValid || CurrentWorldTarget.IsValid)
				{
					Vector2 pieOffset = Vector2.zero;
					if (AttackVerb.verbProps is VerbProperties_Animated animatedProps)
					{
						Vector3 pieOffsetTmp = new Vector3(animatedProps.shootOffset.x, 0, animatedProps.shootOffset.y).RotatedBy(TopArtillery.CurRotation);
						pieOffset.x = pieOffsetTmp.x;
						pieOffset.y = pieOffsetTmp.z;
					}
					Vector3 root = DrawPos + new Vector3(pieOffset.x, offset, pieOffset.y);
					root += Quaternion.AngleAxis(TopArtillery.CurRotation, Vector3.up) * Vector3.forward * 0.8f;
					Graphics.DrawMesh(MeshPool.pies[degreesWide], root, Quaternion.AngleAxis(TopArtillery.CurRotation + (degreesWide / 2) - 90f, Vector3.up), AimPieMaterial, 0);
				}
			}
			if (forcedTarget.IsValid && (!forcedTarget.HasThing || forcedTarget.Thing.Spawned))
			{
				Vector3 vector;
				if (forcedTarget.HasThing)
				{
					vector = forcedTarget.Thing.TrueCenter();
				}
				else
				{
					vector = forcedTarget.Cell.ToVector3Shifted();
				}
				Vector3 a = this.TrueCenter();
				vector.y = AltitudeLayer.MetaOverlays.AltitudeFor();
				a.y = vector.y;
				GenDraw.DrawLineBetween(a, vector, ForcedTargetLineMat);
			}
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			Map.GetCachedMapComponent<ListerAirDefenses>().Notify_AirDefenseDespawned(this);
			base.DeSpawn(mode);
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			Map.GetCachedMapComponent<ListerAirDefenses>().Notify_AirDefenseDespawned(this);
			base.Destroy(mode);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				TopArtillery.SetRotationFromOrientation();
				burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();
			}
			Map.GetCachedMapComponent<ListerAirDefenses>().Notify_AirDefenseSpawned(this);
			ActiveVerb = base.AttackVerb;
			TopArtillery.PostSpawnSetup();
		}

		public static IEnumerable<Vector3> RandomWorldPosition(int tile, int numPositions)
		{
			var neighborTiles = new List<int>();
			Find.WorldGrid.GetTileNeighbors(tile, neighborTiles);
			WorldObject tileObject = WorldHelper.WorldObjectAt(tile);
			for (int i = 0; i < numPositions; i++)
			{
				int neighborTile = neighborTiles.RandomElement();
				float offset = Rand.Range(0, WorldObjectOffsetPercent);
				WorldObject neighborObject = WorldHelper.WorldObjectAt(tile);
				Vector3 pos = Vector3.Slerp(WorldHelper.GetTilePos(tile, tileObject, out _), WorldHelper.GetTilePos(neighborTile, neighborObject, out _), offset);
				yield return pos;
			}
		}
	}
}
