using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;
using Vehicles.Defs;

namespace Vehicles
{
	public class VehicleTurret : IExposable, ILoadReferenceable
	{
		protected const int AutoTargetInterval = 50;
		protected const int TicksPerOverheatingFrame = 15;
		protected const int TicksTillBeginCooldown = 60;
		public const float MaxHeatCapacity = 100;

		protected bool autoTargetingActive;

		public int reloadTicks;
		public int burstTicks;
		public string groupKey;

		public int uniqueID = -1;
		public string parentKey;
		public string key;

		[Unsaved]
		public VehicleTurret attachedTo;
		[Unsaved]
		public List<VehicleTurret> childCannons = new List<VehicleTurret>();

		protected float restrictedTheta;

		protected Texture2D cannonTex;
		protected Material cannonMaterialCache;

		public Texture2D currentFireIcon;

		protected Texture2D gizmoIcon;
		protected Texture2D mainMaskTex;
		protected Graphic_Cannon cannonGraphic;

		protected GraphicDataRGB cachedGraphicData;
		private RotatingList<Texture2D> overheatIcons;

		public VehicleTurretDef turretDef;

		public bool targetPersists = true;
		public bool autoTargeting = true;
		public bool manualTargeting = true;

		protected int currentFireMode;
		public float currentHeatRate;
		protected bool triggeredCooldown;
		protected int ticksSinceLastShot;

		protected MaterialPropertyBlock mtb;

		public Vector2 turretRenderOffset;
		public Vector2 turretRenderLocation;

		public Vector2 aimPieOffset = Vector2.zero;

		public Vector2 angleRestricted = Vector2.zero;
		public float defaultAngleRotated = 0f;

		public LocalTargetInfo cannonTarget;
		public int drawLayer = 1;

		protected float currentRotation = 0f;
		protected float rotationTargeted = 0f;

		[Unsaved]
		public VehiclePawn vehicle;

		public ThingDef loadedAmmo;
		public ThingDef savedAmmoType;
		public int shellCount;
		public bool ammoWindowOpened;
		public string gizmoLabel;

		protected Rot4 parentRotCached = default;
		protected float parentAngleCached = 0f;

		public Turret_RecoilTracker rTracker;

		private List<VehicleTurret> groupTurrets;

		public VehicleTurret()
		{
			rTracker = new Turret_RecoilTracker(this);
		}

		public VehicleTurret(VehiclePawn vehicle, VehicleTurret reference)
		{
			this.vehicle = vehicle;

			uniqueID = Find.UniqueIDsManager.GetNextThingID();
			turretDef = reference.turretDef;

			turretRenderOffset = reference.turretRenderOffset;
			turretRenderLocation = reference.turretRenderLocation;
			defaultAngleRotated = reference.defaultAngleRotated;

			aimPieOffset = reference.aimPieOffset;
			angleRestricted = reference.angleRestricted;

			drawLayer = reference.drawLayer;

			gizmoLabel = reference.gizmoLabel;

			key = reference.key;
			parentKey = reference.parentKey;
			groupKey = reference.groupKey;

			targetPersists = reference.targetPersists;
			autoTargeting = reference.autoTargeting;
			manualTargeting = reference.manualTargeting;

			currentFireMode = 0;
			currentFireIcon = OverheatIcons.FirstOrDefault();
			ticksSinceLastShot = 0;

			childCannons = new List<VehicleTurret>();
			if(!string.IsNullOrEmpty(parentKey))
			{
				foreach (VehicleTurret cannon in vehicle.CompCannons.Cannons.Where(c => c.key == parentKey))
				{
					attachedTo = cannon;
					cannon.childCannons.Add(this);
				}
			}

			ResolveCannonGraphics(vehicle);
			rTracker = new Turret_RecoilTracker(this);

			restrictedTheta = (int)Math.Abs(angleRestricted.x - (angleRestricted.y + 360)).ClampAngle();

			ResetCannonAngle();
		}

		public bool GizmoHighlighted { get; set; }

		public bool TargetLocked { get; private set; }

		public int PrefireTickCount { get; private set; }

		public int CurrentTurretFiring { get; set; }

		public PawnStatusOnTarget CachedPawnTargetStatus { get; set; }

		public bool IsTargetable => turretDef?.weaponType == TurretType.Rotatable;

		public bool RotationIsValid => currentRotation == rotationTargeted;

		public virtual bool CannonDisabled => RelatedHandlers.NotNullAndAny(h => h.handlers.Count < h.role.slotsToOperate);

		public bool NoGraphic => turretDef.graphicData is null;

		public int MaxTicks => Mathf.CeilToInt(turretDef.reloadTimer * 60f);

		public int WarmupTicks => Mathf.CeilToInt(turretDef.warmUpTimer * 60f);

		public bool OnCooldown => triggeredCooldown;

		public bool Recoils => turretDef.recoil != null;

		public List<VehicleHandler> RelatedHandlers => vehicle.handlers.FindAll(h => !h.role.cannonIds.NullOrEmpty() && h.role.cannonIds.Contains(key));

		public bool HasAmmo => turretDef.ammunition is null || shellCount > 0;

		public bool ReadyToFire => groupKey.NullOrEmpty() ? (burstTicks <= 0 && reloadTicks <= 0 && !CannonDisabled) : GroupTurrets.Any(t => t.burstTicks <= 0 && t.reloadTicks <= 0 && !t.CannonDisabled);

		public bool FullAuto => CurrentFireMode.ticksBetweenBursts == CurrentFireMode.ticksBetweenShots;

		public Texture2D FireIcon
		{
			get
			{
				if (Find.TickManager.TicksGame % TicksPerOverheatingFrame == 0)
				{
					currentFireIcon = OverheatIcons.Next;
				}
				return currentFireIcon;
			}
		}

		protected RotatingList<Texture2D> OverheatIcons
		{
			get
			{
				if (overheatIcons.NullOrEmpty())
				{
					overheatIcons = VehicleTex.FireIcons.ToRotatingList();
				}
				return overheatIcons;
			}
		}

		public MaterialPropertyBlock MatPropertyBlock
		{
			get
			{
				if (mtb is null)
				{
					mtb = new MaterialPropertyBlock();
				}
				return mtb;
			}
			set
			{
				mtb = value;
			}
		}

		public List<VehicleTurret> GroupTurrets
		{
			get
			{
				if (groupTurrets is null)
				{
					if (groupKey.NullOrEmpty())
					{
						groupTurrets = new List<VehicleTurret>() { this };
					}
					else
					{
						groupTurrets = vehicle.CompCannons.Cannons.Where(t => t.groupKey == groupKey).ToList();
					}
				}
				return groupTurrets;
			}
		}

		public virtual int MaxShotsCurrentFireMode
		{
			get
			{
				if (FullAuto)
				{
					if (turretDef.cooldown is null)
					{
						return CurrentFireMode.shotsPerBurst * 3;
					}
					return Mathf.CeilToInt(MaxHeatCapacity / turretDef.cooldown.heatPerShot);
				}
				return CurrentFireMode.shotsPerBurst;
			}
		}

		public int TicksPerShot
		{
			get
			{
				return CurrentFireMode.ticksBetweenShots;
			}
		}

		public float CannonIconAlphaTicked
		{
			get
			{
				if (reloadTicks <= 0)
				{
					return 0.5f;
				}
				return Mathf.PingPong(reloadTicks, 25) / 100;
			}
		}

		public virtual Material CannonMaterial
		{
			get
			{
				if (cannonMaterialCache is null)
				{
					ResolveCannonGraphics(vehicle);
				}
				return cannonMaterialCache;
			}
		}

		public virtual Texture2D CannonTexture
		{
			get
			{
				if (CannonGraphicData.texPath.NullOrEmpty())
				{
					return null;
				}
				if (cannonTex is null)
				{
					cannonTex = ContentFinder<Texture2D>.Get(CannonGraphicData.texPath);
				}
				return cannonTex;
			}
		}

		public virtual Texture2D MainMaskTexture
		{
			get
			{
				if (CannonGraphicData.texPath.NullOrEmpty())
				{
					return null;
				}
				if (mainMaskTex is null)
				{
					mainMaskTex = ContentFinder<Texture2D>.Get(CannonGraphicData.texPath + Graphic_Cannon.MaskSuffix);
				}
				return mainMaskTex;
			}
		}

		public virtual Graphic_Cannon CannonGraphic
		{
			get
			{
				if (cannonGraphic is null)
				{
					ResolveCannonGraphics(vehicle);
				}
				return cannonGraphic;
			}
		}

		public virtual GraphicDataRGB CannonGraphicData
		{
			get
			{
				if (cachedGraphicData is null)
				{
					ResolveCannonGraphics(vehicle);
				}
				return cachedGraphicData;
			}
		}

		public virtual Texture2D GizmoIcon
		{
			get
			{
				if (!string.IsNullOrEmpty(turretDef.gizmoIconTexPath) && gizmoIcon is null)
				{
					gizmoIcon = ContentFinder<Texture2D>.Get(turretDef.gizmoIconTexPath);
				}
				else if (NoGraphic)
				{
					gizmoIcon = BaseContent.BadTex;
				}
				else if (gizmoIcon is null)
				{
					if(CannonTexture != null)
					{
						gizmoIcon = CannonTexture;
					}
					else
					{
						gizmoIcon = BaseContent.BadTex;
					}
				}
				return gizmoIcon;
			}
		}

		public Vector3 TurretLocation
		{
			get
			{
				float locationRotation = 0f;
				if(attachedTo != null)
				{
					locationRotation = attachedTo.TurretRotation;
				}
				Pair<float, float> turretLoc = RenderHelper.TurretDrawOffset(vehicle.Rotation.AsAngle + vehicle.Angle, turretRenderLocation.x, turretRenderLocation.y, out Pair<float,float> renderOffsets, locationRotation, attachedTo);
				return new Vector3(vehicle.DrawPos.x + turretLoc.First + renderOffsets.First, vehicle.DrawPos.y + drawLayer, vehicle.DrawPos.z + turretLoc.Second + renderOffsets.Second);
			}
		}


		public float TurretRotation
		{
			get
			{
				if (!IsTargetable && attachedTo is null)
				{
					return defaultAngleRotated + vehicle.FullRotation.AsAngle;
				}
				ValidateLockStatus();

				if (currentRotation > 360)
				{
					currentRotation -= 360;
				}
				else if (currentRotation < 0)
				{
					currentRotation += 360;
				}

				float rotation = 270 - currentRotation;
				if (rotation < 0)
				{
					rotation += 360;
				}

				if (attachedTo != null)
				{
					return rotation + attachedTo.TurretRotation;
				}
				return rotation;
			}
			set
			{
				currentRotation = Ext_Numeric.ClampAndWrap(value + 270, 0, 360);
			}
		}

		public float TurretRotationUncorrected
		{
			get
			{
				if (!IsTargetable && attachedTo is null)
				{
					return defaultAngleRotated -= 90 * (vehicle.Rotation.AsInt - parentRotCached.AsInt) + vehicle.Angle - parentAngleCached;
				}
				return currentRotation;
			}
		}

		public FireMode CurrentFireMode
		{
			get
			{
				if (currentFireMode < 0 || currentFireMode >= turretDef.fireModes.Count)
				{
					SmashLog.ErrorOnce($"Unable to retrieve fire mode at index {currentFireMode}. Outside of bounds for <field>fireModes</field> defined in <field>turretDef</field>. Defaulting to first fireMode.", GetHashCode() ^ currentFireMode);
					return turretDef.fireModes.FirstOrDefault();
				}
				return turretDef.fireModes[currentFireMode];
			}
			set
			{
				currentFireMode = turretDef.fireModes.IndexOf(value);
			}
		}

		public bool AutoTarget
		{
			get
			{
				return autoTargetingActive;
			}
			set
			{
				if (!autoTargeting || value == autoTargetingActive)
				{
					return;
				}
				autoTargetingActive = value;
			}
		}

		public float MaxRange
		{
			get
			{
				if (turretDef.maxRange < 0)
				{
					return 9999;
				}
				return turretDef.maxRange;
			}
		}

		public float MinRange
		{
			get
			{
				return turretDef.minRange;
			}
		}

		public virtual bool ActivateTimer(bool ignoreTimer = false)
		{
			if (reloadTicks > 0 && !ignoreTimer)
			{
				return false;
			}
			reloadTicks = MaxTicks;
			TargetLocked = false;
			return true;
		}

		public virtual void ActivateBurstTimer()
		{
			burstTicks = CurrentFireMode.ticksBetweenBursts;
		}

		public virtual void Tick()
		{
			if (turretDef.cooldown != null)
			{
				if (currentHeatRate > 0)
				{
					ticksSinceLastShot++;
				}

				if (currentHeatRate > MaxHeatCapacity)
				{
					triggeredCooldown = true;
					currentHeatRate = MaxHeatCapacity;
				}
				else if (currentHeatRate <= 0)
				{
					currentHeatRate = 0;
					triggeredCooldown = false;
				}

				if (ticksSinceLastShot >= TicksTillBeginCooldown)
				{
					float dissipationRate = turretDef.cooldown.dissipationRate;
					if (triggeredCooldown)
					{
						dissipationRate *= turretDef.cooldown.dissipationCapMultiplier;
					}
					currentHeatRate -= dissipationRate;
				}
			}
			TurretAutoTick();
			TurretRotationTick();
			TurretTargeterTick();
			if (Recoils)
			{
				rTracker.RecoilTick();
			}
		}

		protected virtual void TurretAutoTick()
		{
			if (!vehicle.CompCannons.QueuedToFire(this))
			{
				if (AutoTarget && Find.TickManager.TicksGame % AutoTargetInterval == 0)
				{
					if (CannonDisabled)
					{
						return;
					}
					if (!cannonTarget.IsValid && CannonTargeter.Instance.Cannon != this && reloadTicks <= 0 && HasAmmo)
					{
						LocalTargetInfo autoTarget = this.GetCannonTarget();
						if (autoTarget.IsValid)
						{
							AlignToAngleRestricted(TurretLocation.AngleToPointRelative(autoTarget.Thing.DrawPos));
							SetTarget(autoTarget);
						}
					}
				}
				if (reloadTicks > 0 && !OnCooldown)
				{
					reloadTicks--;
				}
				if (burstTicks > 0)
				{
					burstTicks--;
				}
			}
		}

		protected virtual void TurretRotationTick()
		{
			if (currentRotation != rotationTargeted)
			{
				//REDO - SET TO CHECK CANNON HANDLERS COMPONENT HEALTH
				if (true)
				{
					float relativeCurrentRotation = currentRotation + 90;
					float relativeTargetedRotation = rotationTargeted + 90;
					if (relativeCurrentRotation < 0)
					{
						relativeCurrentRotation += 360;
					}
					else if (relativeCurrentRotation > 360)
					{
						relativeCurrentRotation -= 360;
					}
					if (relativeTargetedRotation < 0)
					{
						relativeTargetedRotation += 360;
					}
					else if (relativeTargetedRotation > 360)
					{
						relativeTargetedRotation -= 360;
					}
					if (Math.Abs(relativeCurrentRotation - relativeTargetedRotation) < turretDef.rotationSpeed)
					{
						currentRotation = rotationTargeted;
					}
					else
					{
						if (relativeCurrentRotation < relativeTargetedRotation)
						{
							if (Math.Abs(relativeCurrentRotation - relativeTargetedRotation) < 180)
							{
								currentRotation += turretDef.rotationSpeed;
								foreach(VehicleTurret cannon in childCannons)
								{
									cannon.currentRotation += turretDef.rotationSpeed;
								}
							}
							else
							{
								currentRotation -= turretDef.rotationSpeed;
								foreach(VehicleTurret cannon in childCannons)
								{
									cannon.currentRotation -= turretDef.rotationSpeed;
								}
							}
						}
						else
						{
							if (Math.Abs(relativeCurrentRotation - relativeTargetedRotation) < 180)
							{
								currentRotation -= turretDef.rotationSpeed;
								foreach(VehicleTurret cannon in childCannons)
								{
									cannon.currentRotation -= turretDef.rotationSpeed;
								}
							}
							else
							{
								currentRotation += turretDef.rotationSpeed;
								foreach(VehicleTurret cannon in childCannons)
								{
									cannon.currentRotation += turretDef.rotationSpeed;
								}
							}
						}
					}
				}
				else
				{
					rotationTargeted = currentRotation;
				}
			}
		}

		protected virtual void TurretTargeterTick()
		{
			if (cannonTarget.IsValid && currentRotation == rotationTargeted && !TargetLocked)
			{
				TargetLocked = true;
				ResetPrefireTimer();
			}
			if (cannonTarget.Cell.IsValid)
			{
				if (IsTargetable && !CannonTargeter.TargetMeetsRequirements(this, cannonTarget))
				{
					SetTarget(LocalTargetInfo.Invalid);
					TargetLocked = false;
					return;
				}
				if (PrefireTickCount > 0)
				{
					if (cannonTarget.HasThing)
					{
						rotationTargeted = TurretLocation.AngleToPointRelative(cannonTarget.Thing.DrawPos);
						if (attachedTo != null)
						{
							rotationTargeted += attachedTo.TurretRotation;
						}
					}
					else
					{
						rotationTargeted = TurretLocation.ToIntVec3().AngleToCell(cannonTarget.Cell, vehicle.Map);
						if (attachedTo != null)
						{
							rotationTargeted += attachedTo.TurretRotation;
						}
					}

					if (turretDef.autoSnapTargeting)
					{
						currentRotation = rotationTargeted;
					}

					if (TargetLocked && ReadyToFire)
					{
						float facing = cannonTarget.Thing != null ? (cannonTarget.Thing.DrawPos - TurretLocation).AngleFlat() : (cannonTarget.Cell - TurretLocation.ToIntVec3()).AngleFlat;
						GenDraw.DrawAimPieRaw(TurretLocation + new Vector3(turretRenderOffset.x + aimPieOffset.x, 0.5f, turretRenderOffset.y + aimPieOffset.y).RotatedBy(TurretRotation), facing, (int)(PrefireTickCount * 0.5f));
						PrefireTickCount--;
					}
				}
				else if (ReadyToFire)
				{
					if (IsTargetable && RotationIsValid && targetPersists && (cannonTarget.Pawn is null || !SetTargetConditionalOnThing(LocalTargetInfo.Invalid)))
					{
						GroupTurrets.ForEach(t => t.PushTurretToQueue());
					}
					else if (FullAuto)
					{
						GroupTurrets.ForEach(t => t.PushTurretToQueue());
					}
				}
			}
			else if (IsTargetable)
			{
				SetTargetConditionalOnThing(LocalTargetInfo.Invalid);
			}
		}

		public virtual CompCannons.TurretData GenerateTurretData()
		{
			return new CompCannons.TurretData()
			{
				shots = CurrentFireMode.shotsPerBurst,
				ticksTillShot = 0,
				turret = this
			};
		}

		public virtual void PushTurretToQueue()
		{
			ActivateBurstTimer();
			vehicle.CompCannons.QueueTurret(GenerateTurretData());
		}

		public static bool TryFindShootLineFromTo(IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
		{
			resultingLine = new ShootLine(root, targ.Cell);
			return false;
		}

		public virtual void FireTurret()
		{
			if (!vehicle.Spawned)
			{
				return;
			}
			TryFindShootLineFromTo(TurretLocation.ToIntVec3(), cannonTarget, out ShootLine shootLine);
			
			float range = Vector3.Distance(TurretLocation, cannonTarget.CenterVector3);
			IntVec3 c = cannonTarget.Cell + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(CurrentFireMode.spreadRadius * (range / turretDef.maxRange)))];
			if (CurrentTurretFiring >= turretDef.projectileShifting.Count)
			{
				CurrentTurretFiring = 0;
			}
			float horizontalOffset = turretDef.projectileShifting.NotNullAndAny() ? turretDef.projectileShifting[CurrentTurretFiring] : 0;
			Vector3 launchCell = TurretLocation + new Vector3(horizontalOffset, 1f, turretDef.projectileOffset).RotatedBy(TurretRotation);

			ThingDef projectile;
			if (turretDef.ammunition != null && !turretDef.genericAmmo)
			{
				projectile = loadedAmmo?.projectileWhenLoaded;
			}
			else
			{
				projectile = turretDef.projectile;
			}
			try
			{
				Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, vehicle.Position, vehicle.Map, WipeMode.Vanish);
				if (turretDef.ammunition != null)
				{
					ConsumeShellChambered();
				}
				if (turretDef.cannonSound is null)
				{
					SoundDefOf_Ships.Explosion_PirateCannon.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map, false));
				}
				else
				{
					turretDef.cannonSound.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map, false));
				}
				if (turretDef.projectileSpeed > 0)
				{
					projectile2.AllComps.Add(new CompTurretProjectileProperties(vehicle)
					{
						speed = turretDef.projectileSpeed > 0 ? turretDef.projectileSpeed : projectile2.def.projectile.speed,
						hitflag = turretDef.hitFlags,
						hitflags = turretDef.attachProjectileFlag
					});
				}
				projectile2.Launch(vehicle, launchCell, c, cannonTarget, projectile2.HitFlags, vehicle);
				vehicle.vDrawer.rTracker.Notify_TurretRecoil(this, Ext_Math.RotateAngle(TurretRotation, 180));
				rTracker.Notify_TurretRecoil(this, Ext_Math.RotateAngle(TurretRotation, 180));
				PostTurretFire();
				InitTurretMotes(launchCell);
			}
			catch (Exception ex)
			{
				Log.Error($"Exception when firing Cannon: {turretDef.LabelCap} on Pawn: {vehicle.LabelCap}. Exception: {ex.Message}");
			}
		}

		public virtual void PostTurretFire()
		{
			ticksSinceLastShot = 0;
			if (turretDef.cooldown != null)
			{
				currentHeatRate += turretDef.cooldown.heatPerShot;
			}
		}

		public virtual void InitTurretMotes(Vector3 loc)
		{
			if (!turretDef.motes.NullOrEmpty())
			{
				foreach (AnimationProperties moteProps in turretDef.motes)
				{
					Vector3 moteLoc = loc;
					if (loc.ShouldSpawnMotesAt(vehicle.Map))
					{
						try
						{
							float altitudeLayer = Altitudes.AltitudeFor(moteProps.moteDef.altitudeLayer);
							moteLoc += new Vector3(moteProps.offset.x, altitudeLayer + moteProps.offset.y, moteProps.offset.z);
							Mote mote = (Mote)ThingMaker.MakeThing(moteProps.moteDef);
							mote.exactPosition = moteLoc;
							mote.exactRotation = moteProps.exactRotation.RandomInRange;
							mote.instanceColor = moteProps.color;
							mote.rotationRate = moteProps.rotationRate;
							mote.Scale = moteProps.scale;
							if (mote is MoteThrown thrownMote)
							{
								float thrownAngle = TurretRotation + moteProps.angleThrown.RandomInRange;
								if (thrownMote is MoteThrownExpand expandMote)
								{
									if (expandMote is MoteThrownSlowToSpeed accelMote)
									{
										accelMote.SetDecelerationRate(moteProps.deceleration.RandomInRange, moteProps.fixedAcceleration, thrownAngle);
									}
									expandMote.growthRate = moteProps.growthRate.RandomInRange;
								}
								thrownMote.SetVelocity(thrownAngle, moteProps.speedThrown.RandomInRange);
							}
							if (mote is Mote_CannonPlume cannonMote)
							{
								cannonMote.cyclesLeft = moteProps.cycles;
								cannonMote.animationType = moteProps.animationType;
								cannonMote.angle = TurretRotation;
							}
							mote.def = moteProps.moteDef;
							mote.PostMake();
							GenSpawn.Spawn(mote, moteLoc.ToIntVec3(), vehicle.Map, WipeMode.Vanish);
						}
						catch (Exception ex)
						{
							SmashLog.Error($"Failed to spawn mote at {loc}. MoteDef = <field>{moteProps.moteDef?.defName ?? "Null"}</field> Exception = {ex.Message}");
						}
					}
				}
			}
		}

		public virtual void Draw()
		{
			if (!NoGraphic)
			{
				RenderHelper.DrawAttachedThing(this);
				DrawTargeter();
			}
		}

		protected virtual void DrawTargeter()
		{
			if (GizmoHighlighted || CannonTargeter.Instance.Cannon == this)
			{
				if (angleRestricted != Vector2.zero)
				{
					var drawLinesTask = new Task(() => { RenderHelper.DrawAngleLines(TurretLocation, angleRestricted, MinRange, MaxRange, restrictedTheta, attachedTo?.TurretRotation ?? 0f); });
					drawLinesTask.RunSynchronously();
				}
				else if (turretDef.weaponType == TurretType.Static)
				{
					Vector3 target = TurretLocation.PointFromAngle(MaxRange, TurretRotation);
					float range = Vector3.Distance(TurretLocation, target);
					GenDraw.DrawRadiusRing(target.ToIntVec3(), CurrentFireMode.spreadRadius * (range / turretDef.maxRange));
				}
				else
				{
					if (MaxRange > -1)
					{
						Vector3 pos = TurretLocation;
						pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
						float currentAlpha = 0.65f;
						if (currentAlpha > 0f)
						{
							Color value = Color.grey;
							value.a *= currentAlpha;
							MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
							Matrix4x4 matrix = default;
							matrix.SetTRS(pos, Quaternion.identity, new Vector3(MaxRange * 2f, 1f, MaxRange * 2f));
							Graphics.DrawMesh(MeshPool.plane10, matrix, TexData.RangeMat((int)MaxRange), 0, null, 0, MatPropertyBlock);
						}


					}
					if (MinRange > 0)
					{
						Vector3 pos = TurretLocation;
						pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
						float currentAlpha = 0.65f;
						if (currentAlpha > 0f)
						{
							Color value = Color.red;
							value.a *= currentAlpha;
							MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
							Matrix4x4 matrix = default;
							matrix.SetTRS(pos, Quaternion.identity, new Vector3(MinRange * 2f, 1f, MinRange * 2f));
							Graphics.DrawMesh(MeshPool.plane10, matrix, TexData.RangeMat((int)MinRange), 0, null, 0, MatPropertyBlock);
						}
					}
				}
			}
		}

		public virtual void ResolveCannonGraphics(VehiclePawn forPawn, bool forceRegen = false)
		{
			if (NoGraphic)
			{
				return;
			}
			if (cachedGraphicData is null || forceRegen)
			{
				cachedGraphicData = new GraphicDataRGB();
				cachedGraphicData.CopyFrom(turretDef.graphicData);
				if (turretDef.matchParentColor)
				{
					cachedGraphicData.color = forPawn.DrawColor;
					cachedGraphicData.colorTwo = forPawn.DrawColorTwo;
					cachedGraphicData.colorThree = forPawn.DrawColorThree;
					cachedGraphicData.tiles = forPawn.tiles;
					cachedGraphicData.displacement = forPawn.displacement;
				}
			}

			if (cannonGraphic is null || forceRegen)
			{
				cannonGraphic = CannonGraphicData.Graphic as Graphic_Cannon;
			}
			if (cannonMaterialCache is null || forceRegen)
			{
				cannonMaterialCache = CannonGraphic.MatAt(Rot4.North, vehicle);
			}
		}

		public virtual void ResolveCannonGraphics(VehicleDef alternateDef, bool forceRegen = false)
		{
			if (NoGraphic)
			{
				return;
			}
			if (cachedGraphicData is null || forceRegen)
			{
				cachedGraphicData = new GraphicDataRGB();
				cachedGraphicData.CopyFrom(turretDef.graphicData);
				
				if (turretDef.matchParentColor)
				{
					var bodyGraphicData = alternateDef.graphicData;
					cachedGraphicData.color = bodyGraphicData.color;
					cachedGraphicData.colorTwo = bodyGraphicData.colorTwo;
					cachedGraphicData.colorThree = bodyGraphicData.colorThree;
					cachedGraphicData.tiles = bodyGraphicData.tiles;
					cachedGraphicData.displacement = bodyGraphicData.displacement;
				}
			}

			if (cannonGraphic is null || forceRegen)
			{
				cannonGraphic = CannonGraphicData.Graphic as Graphic_Cannon;
			}
			if(cannonMaterialCache is null || forceRegen)
			{
				cannonMaterialCache = CannonGraphic.MatAt(Rot4.North, vehicle);
			}
		}

		public bool AngleBetween(Vector3 mousePosition)
		{
			if (angleRestricted == Vector2.zero)
			{
				return true;
			}

			float rotationOffset = attachedTo != null ? attachedTo.TurretRotation : vehicle.Rotation.AsInt * 90 + vehicle.Angle;

			float start = angleRestricted.x + rotationOffset;
			float end = angleRestricted.y + rotationOffset;

			if (start > 360)
			{
				start -= 360;
			}
			if (end > 360)
			{
				end -= 360;
			}

			float mid = (mousePosition - TurretLocation).AngleFlat();
			end = (end - start) < 0f ? end - start + 360 : end - start;
			mid = (mid - start) < 0f ? mid - start + 360 : mid - start;
			return mid < end;
		}
		
		public void AlignToTargetRestricted()
		{
			if (cannonTarget.HasThing)
			{
				rotationTargeted = TurretLocation.AngleToPointRelative(cannonTarget.Thing.DrawPos);
				if (attachedTo != null)
				{
					rotationTargeted += attachedTo.TurretRotation;
				}
			}
			else
			{
				rotationTargeted = TurretLocation.ToIntVec3().AngleToCell(cannonTarget.Cell, vehicle.Map);
				if (attachedTo != null)
				{
					rotationTargeted += attachedTo.TurretRotation;
				}
			}
		}

		public void AlignToAngleRestricted(float angle)
		{
			float additionalAngle = attachedTo is null ? 0f : attachedTo.TurretRotation;
			if (turretDef.autoSnapTargeting)
			{
				currentRotation = angle + additionalAngle;
				rotationTargeted = currentRotation;
			}
			else
			{
				rotationTargeted = angle + additionalAngle;
			}
		}

		public virtual void ReloadCannon(ThingDef ammo = null, bool ignoreTimer = false)
		{
			if( (ammo == savedAmmoType || ammo is null) && shellCount == turretDef.magazineCapacity)
			{
				return;
			}
			if (turretDef.ammunition is null)
			{
				shellCount = turretDef.magazineCapacity;
				return;
			}
			if (loadedAmmo is null || (ammo != null && (shellCount < turretDef.magazineCapacity || (turretDef.ammunition != null && ammo != loadedAmmo))))
			{
				ReloadInternal(ammo);
			}
			else if( (loadedAmmo != null || turretDef.genericAmmo ) && shellCount > 0)
			{
				ActivateBurstTimer();
				return;
			}
			ActivateTimer(ignoreTimer);
		}

		/// <summary>
		/// Automatically reload magazine of VehicleTurret with first Ammo Type in inventory
		/// </summary>
		/// <returns>True if Cannon has been successfully reloaded.</returns>
		public virtual bool AutoReloadCannon()
		{
			ThingDef ammoType = vehicle.inventory.innerContainer.FirstOrDefault(t => turretDef.ammunition.Allows(t) || turretDef.ammunition.Allows(t.def.projectileWhenLoaded))?.def;
			if (ammoType != null)
			{
				ReloadInternal(ammoType);
				return true;
			}
			Debug.Warning($"Failed to auto-reload {turretDef.label}");
			return false;
		}

		protected void ReloadInternal(ThingDef ammo)
		{
			try
			{
				if (vehicle.inventory.innerContainer.Contains(savedAmmoType) || vehicle.inventory.innerContainer.Contains(ammo))
				{
					Thing storedAmmo = null;
					if (ammo != null)
					{
						storedAmmo = vehicle.inventory.innerContainer.FirstOrFallback(x => x.def == ammo);
						savedAmmoType = ammo;
						TryRemoveShell();
					}
					else if (savedAmmoType != null)
					{
						storedAmmo = vehicle.inventory.innerContainer.FirstOrFallback(x => x.def == savedAmmoType);
					}
					else
					{
						Log.Error("No saved or specified shell upon reload");
						return;
					}
					int countToTake = storedAmmo.stackCount >= turretDef.magazineCapacity - shellCount ? turretDef.magazineCapacity - shellCount : storedAmmo.stackCount;
					Thing loadedThing = vehicle.inventory.innerContainer.Take(storedAmmo, countToTake);
					int additionalCount = 0;
					if (countToTake + shellCount < turretDef.magazineCapacity)
					{
						foreach(Thing t in vehicle.inventory.innerContainer)
						{
							if(t.def == storedAmmo.def)
							{
								additionalCount = t.stackCount >= turretDef.magazineCapacity - (shellCount + countToTake) ? turretDef.magazineCapacity - (shellCount + countToTake) : t.stackCount;
								Thing additionalItem = vehicle.inventory.innerContainer.Take(t, additionalCount);
								if (additionalCount + countToTake >= turretDef.magazineCapacity)
									break;
							}    
						}
					}
						
					loadedAmmo = loadedThing.def;
					shellCount = loadedThing.stackCount + additionalCount;
					if(turretDef.reloadSound != null)
						turretDef.reloadSound.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map, false));
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to reload Cannon: {uniqueID} on Pawn: {vehicle.LabelShort}. Exception: {ex.Message}");
				return;
			}
		}

		public void ConsumeShellChambered()
		{
			shellCount--;
			if (shellCount <= 0 && vehicle.inventory.innerContainer.FirstOrFallback(x => x.def == loadedAmmo) is null)
			{
				loadedAmmo = null;
				shellCount = 0;
			}
		}

		public virtual void TryRemoveShell()
		{
			if(loadedAmmo != null && shellCount > 0)
			{
				Thing thing = ThingMaker.MakeThing(loadedAmmo);
				thing.stackCount = shellCount;
				vehicle.inventory.innerContainer.TryAdd(thing);
				loadedAmmo = null;
				shellCount = 0;
				ActivateTimer(true);
			}
		}

		public void CycleFireMode()
		{
			SoundDefOf.Click.PlayOneShotOnCamera(vehicle.Map);
			currentFireMode++;
			if (currentFireMode >= turretDef.fireModes.Count)
			{
				currentFireMode = 0;
			}
		}

		public virtual void SwitchAutoTarget()
		{
			if(autoTargeting)
			{
				SoundDefOf.Click.PlayOneShotOnCamera(vehicle.Map);
				AutoTarget = !AutoTarget;
				SetTarget(LocalTargetInfo.Invalid);
			}
			else
			{
				Messages.Message("AutoTargetingDisabled".Translate(), MessageTypeDefOf.RejectInput);
			}
		}

		public virtual void SetTarget(LocalTargetInfo target)
		{
			cannonTarget = target;
			TargetLocked = false;
			if (target.Pawn is Pawn)
			{
				if (target.Pawn.Downed)
				{
					CachedPawnTargetStatus = PawnStatusOnTarget.Down;
				}
				else if (target.Pawn.Dead)
				{
					CachedPawnTargetStatus = PawnStatusOnTarget.Dead;
				}
				else
				{
					CachedPawnTargetStatus = PawnStatusOnTarget.Alive;
				}
			}
			else
			{
				CachedPawnTargetStatus = PawnStatusOnTarget.None;
			}
		}

		/// <summary>
		/// Set target only if cannonTarget is no longer valid or if target is cell based
		/// </summary>
		/// <param name="target"></param>
		/// <returns>true if cannonTarget set to target, false if target is still valid</returns>
		public virtual bool SetTargetConditionalOnThing(LocalTargetInfo target, bool resetPrefireTimer = true)
		{
			if (cannonTarget.IsValid && HasAmmo && !OnCooldown && (cannonTarget.HasThing || FullAuto))
			{
				if(cannonTarget.Pawn != null)
				{
					if ( (cannonTarget.Pawn.Dead && CachedPawnTargetStatus != PawnStatusOnTarget.Dead ) || (cannonTarget.Pawn.Downed && CachedPawnTargetStatus != PawnStatusOnTarget.Down) )
					{
						SetTarget(target);
						return true;
					}
				}
				else if(cannonTarget.Thing != null)
				{
					if (cannonTarget.Thing.HitPoints > 0)
					{
						SetTarget(target);
						return true;
					}
				}
				return false;
			}
			SetTarget(target);
			return true;
		}

		public void ResetCannonAngle()
		{
			currentRotation = -defaultAngleRotated - 90;
			if (currentRotation < 360)
			{
				currentRotation += 360;
			}
			else if (currentRotation > 360)
			{
				currentRotation -= 360;
			}
			rotationTargeted = currentRotation;
		}

		public virtual void ResetPrefireTimer()
		{
			PrefireTickCount = WarmupTicks;
		}

		protected void ValidateLockStatus()
		{
			if (!cannonTarget.IsValid && CannonTargeter.Instance.Cannon != this)
			{
				float angleDifference = vehicle.Angle - parentAngleCached;
				if (attachedTo is null)
				{
					currentRotation -= 90 * (vehicle.Rotation.AsInt - parentRotCached.AsInt) + angleDifference;
				}
				rotationTargeted = currentRotation;
			}
			parentRotCached = vehicle.Rotation;
			parentAngleCached = vehicle.Angle;
		}

		public virtual string GetUniqueLoadID()
		{
			return "VehicleTurretGroup_" + uniqueID;
		}

		public override string ToString()
		{
			return $"{turretDef.LabelCap} : {GetUniqueLoadID()}";
		}

		public bool ContainsAmmoDefOrShell(ThingDef def)
		{
			ThingDef projectile = null;
			if(def.projectileWhenLoaded != null)
			{
				projectile = def.projectileWhenLoaded;
			}
			return turretDef.ammunition.Allows(def) || turretDef.ammunition.Allows(projectile);
		}

		public virtual IEnumerable<string> ConfigErrors(VehicleDef vehicleDef)
		{
			if (turretDef is null)
			{
				yield return $"<field>turretDef</field> is a required field for <type>VehicleTurret</type>.".ConvertRichText();
			}
			if (string.IsNullOrEmpty(key))
			{
				yield return "<field>key</field> must be included for each <type>VehicleTurret</type>".ConvertRichText();
			}
			if (vehicleDef.GetCompProperties<CompProperties_Cannons>().turrets.Select(x => x.key).GroupBy(y => y).Where(y => y.Count() > 1).Select(z => z.Key).NotNullAndAny())
			{
				yield return $"Duplicate cannon key {key}";
			}
			//REDO - Add groupKeys for validation so vehicle turrets all match
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref autoTargetingActive, "autoTargetingActive");

			Scribe_Values.Look(ref reloadTicks, "reloadTicks");
			Scribe_Values.Look(ref burstTicks, "burstTicks");
			Scribe_Values.Look(ref groupKey, "groupKey");

			Scribe_Values.Look(ref uniqueID, "uniqueID", -1);
			Scribe_Values.Look(ref parentKey, "parentKey");
			Scribe_Values.Look(ref key, "key");

			Scribe_Defs.Look(ref turretDef, "turretDef");

			Scribe_Values.Look(ref targetPersists, "targetPersists");
			Scribe_Values.Look(ref autoTargeting, "autoTargeting");
			Scribe_Values.Look(ref manualTargeting, "manualTargeting");

			Scribe_Values.Look(ref currentFireMode, "currentFireMode");
			Scribe_Values.Look(ref currentHeatRate, "currentHeatRate");
			Scribe_Values.Look(ref triggeredCooldown, "triggeredCooldown");
			Scribe_Values.Look(ref ticksSinceLastShot, "ticksSinceLastShot");

			Scribe_Values.Look(ref turretRenderOffset, "turretRenderOffset");
			Scribe_Values.Look(ref turretRenderLocation, "turretRenderLocation");

			Scribe_Values.Look(ref currentRotation, "currentRotation", defaultAngleRotated - 90);
			Scribe_Values.Look(ref rotationTargeted, "rotationTargeted", defaultAngleRotated - 90);
			Scribe_Values.Look(ref aimPieOffset, "aimPieOffset");
			Scribe_Values.Look(ref angleRestricted, "angleRestricted");
			Scribe_Values.Look(ref defaultAngleRotated, "defaultAngleRotated");
			Scribe_Values.Look(ref restrictedTheta, "restrictedTheta", (int)Math.Abs(angleRestricted.x - (angleRestricted.y + 360)).ClampAngle());

			Scribe_Values.Look(ref drawLayer, "drawLayer");

			Scribe_Defs.Look(ref loadedAmmo, "loadedAmmo");
			Scribe_Defs.Look(ref savedAmmoType, "savedAmmoType");
			Scribe_Values.Look(ref shellCount, "shellCount");
			Scribe_Values.Look(ref gizmoLabel, "gizmoLabel");

			Scribe_TargetInfo.Look(ref cannonTarget, "cannonTarget", LocalTargetInfo.Invalid);
		}
	}
}
