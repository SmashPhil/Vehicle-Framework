using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;

namespace Vehicles
{
	public partial class VehiclePawn
	{
		public bool beached = false;

		public VehicleStatHandler statHandler;

		public VehicleMovementStatus movementStatus = VehicleMovementStatus.Online;
		//public NavigationCategory navigationCategory = NavigationCategory.Opportunistic;

		internal VehicleComponent HighlightedComponent { get; set; }

		public VehiclePermissions MovementPermissions => SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef), nameof(VehicleDef.vehicleMovementPermissions), VehicleDef.vehicleMovementPermissions);
		public bool CanMove => GetStatValue(VehicleStatDefOf.MoveSpeed) > 0.1f && MovementPermissions > VehiclePermissions.NotAllowed && movementStatus == VehicleMovementStatus.Online;
		public bool CanMoveFinal => CanMove && (CanMoveWithOperators || VehicleMod.settings.debug.debugDraftAnyVehicle);

		public CellRect Hitbox { get; private set; }

		public IEnumerable<IntVec3> SurroundingCells
		{
			get
			{
				return this.OccupiedRect().ExpandedBy(1).EdgeCells;
			}
		}

		public float GetStatValue(VehicleStatDef statDef)
		{
			//Cached in VehicleStatHandler, can fetch fresh calculation from VehicleStatDef.Worker if necessary, or mark statDef dirty in cache before retrieving
			return statHandler.GetStatValue(statDef);
		}

		public IEnumerable<IntVec3> InhabitedCells(int expandedBy = 0)
		{
			return InhabitedCellsProjected(Position, FullRotation, expandedBy);
		}

		public IEnumerable<IntVec3> InhabitedCellsProjected(IntVec3 projectedCell, Rot8 rot, int expandedBy = 0)
		{
			bool maxSizePossible = !rot.IsValid;
			return this.VehicleRect(projectedCell, rot, maxSizePossible: maxSizePossible).ExpandedBy(expandedBy).Cells; //REDO FOR DIAGONALS
		}

		private void InitializeHitbox()
		{
			Hitbox = this.VehicleRect(IntVec3.Zero, Rot4.North);
			statHandler.InitializeHitboxCells();
		}

		public virtual void Notify_TookDamage()
		{
			if (Spawned)
			{
				Map.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleTookDamage(this);
			}
		}

		public bool TryTakeDamage(DamageInfo dinfo, IntVec3 position, out DamageWorker.DamageResult result)
		{
			result = new DamageWorker.DamageResult();
			if (this.OccupiedRect().Contains(position))
			{
				IntVec2 hitCell = new IntVec2(position.x - Position.x, position.z - Position.z);
				result = TakeDamage(dinfo, hitCell);
				return true;
			}
			return false;
		}

		public virtual DamageWorker.DamageResult TakeDamage(DamageInfo dinfo, IntVec2 cell)
		{
			statHandler.TakeDamage(dinfo, cell);
			var damageResult = new DamageWorker.DamageResult(); //Add relevant data (total damage dealt, filth spawning, etc.)
			return damageResult;
		}

		///Divert damage calculations to <see cref="VehicleStatHandler"/>
		public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			statHandler.TakeDamage(dinfo);
			absorbed = true;
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			if (vPather != null)
			{
				vPather.StopDead();
			}
			Map.GetCachedMapComponent<VehiclePositionManager>().ReleaseClaimed(this);
			//Map.GetCachedMapComponent<VehicleRegionUpdateCatalog>().Notify_VehicleDespawned(this);
			VehicleReservationManager reservationManager = Map.GetCachedMapComponent<VehicleReservationManager>();
			reservationManager.ClearReservedFor(this);
			reservationManager.RemoveAllListerFor(this);
			Map.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleDespawned(this);
			EventRegistry[VehicleEventDefOf.Despawned].ExecuteEvents();
			base.DeSpawn(mode);
			SoundCleanup();
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			if (Spawned)
			{
				DisembarkAll();
			}
			sustainers.EndAll();
			//Null check in the event that this vehicle is a world pawn but not in any AerialVehicleInFlight, VehicleCaravan, or spawned on map
			EventRegistry?[VehicleEventDefOf.Destroyed].ExecuteEvents();
			base.Destroy(mode);
		}

		public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
		{
			Kill(dinfo, spawnWreckage: false);
		}

		public virtual void Kill(DamageInfo? dinfo, DestroyMode destroyMode = DestroyMode.KillFinalize, bool spawnWreckage = false)
		{
			IntVec3 position = PositionHeld;
			Rot4 rotation = Rotation;

			Map map = Map;
			Map mapHeld = MapHeld;
			bool spawned = Spawned;
			bool worldPawn = this.IsWorldPawn();
			VehicleCaravan caravan = this.GetCaravan() as VehicleCaravan;
			ThingDef vehicleDef = VehicleDef.buildDef;

			if (Current.ProgramState == ProgramState.Playing)
			{
				Find.Storyteller.Notify_PawnEvent(this, AdaptationEvent.Died, null);
			}
			if (dinfo != null && dinfo.Value.Instigator != null)
			{
				if (dinfo.Value.Instigator is Pawn pawn)
				{
					RecordsUtility.Notify_PawnKilled(this, pawn);
				}
			}

			if (this.GetLord() != null)
			{
				this.GetLord().Notify_PawnLost(this, PawnLostCondition.Killed, dinfo);
			}
			if (spawned)
			{
				DropAndForbidEverything(false);
				if (destroyMode == DestroyMode.Deconstruct)
				{
					SoundDefOf.Building_Deconstructed.PlayOneShot(new TargetInfo(Position, map, false));
				}
				else if (destroyMode == DestroyMode.KillFinalize)
				{
					DoDestroyEffects(map);
				}
			}

			VehicleBuilding wreckage = (VehicleBuilding)ThingMaker.MakeThing(vehicleDef);
			wreckage.SetFactionDirect(Faction);
			wreckage.vehicle = this;
			wreckage.HitPoints = wreckage.MaxHitPoints / 10;

			meleeVerbs.Notify_PawnKilled();
			if (spawned)
			{
				if (map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterOceanDeep || map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterDeep)
				{
					StringBuilder downWithShipString = new StringBuilder();
					bool pawnsLostAtSea = false;
					foreach (Pawn pawn in AllPawnsAboard)
					{
						if (HediffHelper.AttemptToDrown(pawn))
						{
							pawnsLostAtSea = true;
							downWithShipString.AppendLine(pawn.LabelCap);
							//pawn.Destroy(DestroyMode.Vanish);
						}
						else
						{
							DisembarkPawn(pawn);
						}
					}
					string desc = pawnsLostAtSea ? "VF_BoatSunkDesc".Translate(LabelShort) : "VF_BoatSunkWithPawnsDesc".Translate(LabelShort, downWithShipString.ToString());
					Find.LetterStack.ReceiveLetter("VF_BoatSunk".Translate(), desc, LetterDefOf.NegativeEvent, new TargetInfo(Position, map, false), null, null);
					Destroy(DestroyMode.KillFinalize);
					return;
				}
				else
				{
					Destroy(DestroyMode.KillFinalize);
				}

				if (spawnWreckage)
				{
					GenSpawn.Spawn(wreckage, position, map, rotation, WipeMode.FullRefund);
				}
			}
			return;
		}

		public virtual void Notify_DamageImpact(VehicleComponent.DamageResult damageResult)
		{
			if (Spawned)
			{
				EffecterDef effecterDef = null;
				switch (damageResult.penetration)
				{
					case VehicleComponent.Penetration.Deflected:
						{
							if (damageResult.damageInfo.Def == DamageDefOf.Bullet)
							{
								effecterDef = VehicleDef.BodyType.deflectionEffectBullet;
							}
							else
							{
								effecterDef = VehicleDef.BodyType.deflectionEffect;
							}
						}
						break;
					case VehicleComponent.Penetration.Diminished:
						{
							effecterDef = VehicleDef.BodyType.diminishedEffect;
						}
						break;
					case VehicleComponent.Penetration.NonPenetrated:
						{
							effecterDef = VehicleDef.BodyType.nonPenetrationEffect;
						}
						break;
					///Penetration and unhandled cases default to <see cref="FleshTypeDef.damageEffecter"/>
					case VehicleComponent.Penetration.Penetrated:
						{
							effecterDef = VehicleDef.BodyType.damageEffecter;
						}
						break;
					default:
						throw new NotImplementedException("Unhandled Penetration result.");
				}
				if (effecterDef != null && (health.deflectionEffecter == null || health.deflectionEffecter.def != effecterDef))
				{
					if (health.deflectionEffecter != null)
					{
						health.deflectionEffecter.Cleanup();
						health.deflectionEffecter = null;
					}
					health.deflectionEffecter = effecterDef.Spawn();
				}
				IntVec2 effectCell = damageResult.cell.RotatedBy(Rotation);
				IntVec3 onMapCell = new IntVec3(Position.x + effectCell.x, 0, Position.z + effectCell.z);
				health.deflectionEffecter?.Trigger(new TargetInfo(onMapCell, Map), damageResult.damageInfo.Instigator ?? new TargetInfo(onMapCell, Map));
				this.PlayImpactSound(damageResult);
			}
		}

		protected virtual void DoDestroyEffects(Map map)
		{
			if (VehicleDef.buildDef.building.destroyEffecter != null)
			{
				Effecter effecter = VehicleDef.buildDef.building.destroyEffecter.Spawn(Position, map, 1f);
				effecter.Trigger(new TargetInfo(Position, map), TargetInfo.Invalid);
				effecter.Cleanup();
				return;
			}
			SoundDef destroySound = GetDestroySound();
			if (destroySound != null)
			{
				destroySound.PlayOneShot(new TargetInfo(Position, map));
			}
			foreach (IntVec3 intVec in this.OccupiedRect())
			{
				int num = VehicleDef.buildDef.building.isNaturalRock ? 1 : Rand.RangeInclusive(3, 5);
				for (int i = 0; i < num; i++)
				{
					FleckMaker.ThrowDustPuffThick(intVec.ToVector3Shifted(), map, Rand.Range(1.5f, 2f), Color.white);
				}
			}
			if (Find.CurrentMap == map)
			{
				float num2 = VehicleDef.buildDef.building.destroyShakeAmount;
				if (num2 < 0f)
				{
					num2 = VehicleDef.buildDef.shakeAmountPerAreaCurve?.Evaluate(VehicleDef.buildDef.Size.Area) ?? 0;
				}
				CompLifespan compLifespan = GetCachedComp<CompLifespan>();
				if (compLifespan == null || compLifespan.age < compLifespan.Props.lifespanTicks)
				{
					Find.CameraDriver.shaker.DoShake(num2);
				}
			}
		}

		public virtual SoundDef GetDestroySound()
		{
			if (!VehicleDef.buildDef.building.destroySound.NullOrUndefined())
			{
				return VehicleDef.buildDef.building.destroySound;
			}

			if (VehicleDef.buildDef.CostList.NullOrEmpty() || !VehicleDef.buildDef.CostList[0].thingDef.IsStuff || VehicleDef.buildDef.CostList[0].thingDef.stuffProps.categories.NullOrEmpty())
			{
				return null;
			}
			StuffCategoryDef stuffCategoryDef = VehicleDef.buildDef.CostList[0].thingDef.stuffProps.categories[0];

			switch (VehicleDef.buildDef.building.buildingSizeCategory)
			{
				case BuildingSizeCategory.None:
					{
						int area = VehicleDef.buildDef.Size.Area;
						if (area <= 1 && !stuffCategoryDef.destroySoundSmall.NullOrUndefined())
						{
							return stuffCategoryDef.destroySoundSmall;
						}
						if (area <= 4 && !stuffCategoryDef.destroySoundMedium.NullOrUndefined())
						{
							return stuffCategoryDef.destroySoundMedium;
						}
						if (!stuffCategoryDef.destroySoundLarge.NullOrUndefined())
						{
							return stuffCategoryDef.destroySoundLarge;
						}
						break;
					}
				case BuildingSizeCategory.Small:
					if (!stuffCategoryDef.destroySoundSmall.NullOrUndefined())
					{
						return stuffCategoryDef.destroySoundSmall;
					}
					break;
				case BuildingSizeCategory.Medium:
					if (!stuffCategoryDef.destroySoundMedium.NullOrUndefined())
					{
						return stuffCategoryDef.destroySoundMedium;
					}
					break;
				case BuildingSizeCategory.Large:
					if (!stuffCategoryDef.destroySoundLarge.NullOrUndefined())
					{
						return stuffCategoryDef.destroySoundLarge;
					}
					break;
			}
			return null;
		}
	}
}
