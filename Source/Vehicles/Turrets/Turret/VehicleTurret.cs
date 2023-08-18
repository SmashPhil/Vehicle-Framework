using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	[HeaderTitle(Label = "Turret")]
	public class VehicleTurret : IExposable, ILoadReferenceable, IEventManager<VehicleTurretEventDef>, IMaterialCacheTarget
	{
		public const int AutoTargetInterval = 50;
		public const int TicksPerOverheatingFrame = 15;
		public const int TicksTillBeginCooldown = 60;
		public const float MaxHeatCapacity = 100;

		//WIP - may be removed in the future
		public static HashSet<Pair<string, TurretDisableType>> conditionalTurrets = new HashSet<Pair<string, TurretDisableType>>();

		/* --- Parsed --- */

		public int uniqueID = -1;
		public string parentKey;
		public string key;
		public string groupKey;

		public VehicleTurretDef turretDef;

		[TweakField(SettingsType = UISettingsType.Checkbox)]
		public bool targetPersists = true;
		[TweakField(SettingsType = UISettingsType.Checkbox)]
		public bool autoTargeting = true;
		[TweakField(SettingsType = UISettingsType.Checkbox)]
		public bool manualTargeting = true;

		[TweakField]
		public VehicleTurretRender renderProperties = new VehicleTurretRender();

		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector2 aimPieOffset = Vector2.zero;
		[TweakField(SettingsType = UISettingsType.FloatBox)]
		public Vector2 angleRestricted = Vector2.zero;
		public int drawLayer = 1;

		public float defaultAngleRotated = 0f;
		public string gizmoLabel;

		/* ----------------- */

		public LocalTargetInfo cannonTarget;

		protected float rotation = 0; //True rotation of turret separate from Vehicle rotation and angle for saving
		protected float restrictedTheta;

		public ThingDef loadedAmmo;
		public ThingDef savedAmmoType;

		public int shellCount;

		protected bool autoTargetingActive;

		private int reloadTicks;
		private int burstTicks;

		protected int currentFireMode;
		public float currentHeatRate;
		protected bool triggeredCooldown;
		protected int ticksSinceLastShot;
		public bool queuedToFire = false;

		protected Rot4 parentRotCached = default;
		protected float parentAngleCached = 0f;

		[Unsaved]
		protected float currentRotation = 0f; //Rotation taking Vehicle rotation and angle into account
		[Unsaved]
		protected float rotationTargeted = 0f;

		[Unsaved]
		public VehiclePawn vehicle;
		[Unsaved]
		public VehicleDef vehicleDef; //necessary separate from vehicle since VehicleTurrets can exist uninitialized in CompProperties_VehicleTurrets
		[Unsaved]
		public VehicleTurret attachedTo;
		[Unsaved]
		public List<VehicleTurret> childTurrets = new List<VehicleTurret>();
		[Unsaved]
		protected List<VehicleTurret> groupTurrets;
		[Unsaved]
		public TurretRestrictions restrictions;

		//Cache all root draw pos on spawn
		[Unsaved]
		protected Vector3 rootDrawPos_North;
		[Unsaved]
		protected Vector3 rootDrawPos_East;
		[Unsaved]
		protected Vector3 rootDrawPos_South;
		[Unsaved]
		protected Vector3 rootDrawPos_West;
		[Unsaved]
		protected Vector3 rootDrawPos_NorthEast;
		[Unsaved]
		protected Vector3 rootDrawPos_SouthEast;
		[Unsaved]
		protected Vector3 rootDrawPos_SouthWest;
		[Unsaved]
		protected Vector3 rootDrawPos_NorthWest;

		protected Texture2D cannonTex;
		protected Material cannonMaterialCache;

		public Texture2D currentFireIcon;
		protected Texture2D gizmoIcon;
		protected Texture2D mainMaskTex;
		protected Graphic_Turret cannonGraphic;

		protected GraphicDataRGB cachedGraphicData;
		protected RotatingList<Texture2D> overheatIcons;

		protected MaterialPropertyBlock mtb;

		public Turret_RecoilTracker rTracker;

		/* --------- CE hooks for compatibility --------- */
		/// <summary>
		/// (projectileDef, origin, intendedTarget, launcher, shotAngle, shotRotation, shotHeight, shotSpeed, CE projectile)
		/// </summary>
		public static Func<ThingDef, Vector2, LocalTargetInfo, VehiclePawn, float, float, float, float, object> LaunchProjectileCE = null;

		/// <summary>
		/// (velocity, range, heightDiff, flyOverhead, gravityModifier, angle
		/// </summary>
		public static Func<float, float, float, bool, float, float> ProjectileAngleCE = null;
		/* ---------------------------------------------- */

		/// <summary>
		/// Init from CompProperties
		/// </summary>
		public VehicleTurret()
		{
		}

		/// <summary>
		/// Init from save file
		/// </summary>
		public VehicleTurret(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			vehicleDef = vehicle.VehicleDef;
			rTracker = new Turret_RecoilTracker(this);
		}

		/// <summary>
		/// Newly Spawned
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="reference">VehicleTurret as defined in xml</param>
		public VehicleTurret(VehiclePawn vehicle, VehicleTurret reference)
		{
			this.vehicle = vehicle;
			vehicleDef = vehicle.VehicleDef;

			uniqueID = Find.UniqueIDsManager.GetNextThingID();
			turretDef = reference.turretDef;

			gizmoLabel = reference.gizmoLabel;

			key = reference.key;

			targetPersists = reference.turretDef.turretType == TurretType.Static ? false : reference.targetPersists;
			autoTargeting = reference.turretDef.turretType == TurretType.Static ? false : reference.autoTargeting;
			manualTargeting = reference.turretDef.turretType == TurretType.Static ? false : reference.manualTargeting;

			currentFireMode = 0;
			currentFireIcon = OverheatIcons.FirstOrDefault();
			ticksSinceLastShot = 0;

			ResolveCannonGraphics(vehicle);

			rTracker = new Turret_RecoilTracker(this);
		}

		public bool GizmoHighlighted { get; set; }

		public bool TargetLocked { get; private set; }

		public int PrefireTickCount { get; private set; }

		public int CurrentTurretFiring { get; set; }

		public bool IsManned { get; private set; }

		public PawnStatusOnTarget CachedPawnTargetStatus { get; set; }

		public bool IsTargetable => turretDef?.turretType == TurretType.Rotatable;

		public bool RotationIsValid => currentRotation == rotationTargeted;

		public bool TurretRestricted => restrictions?.Disabled ?? false;

		public virtual bool TurretDisabled => TurretRestricted || !IsManned;

		protected virtual bool TurretTargetValid => cannonTarget.Cell.IsValid && !TurretDisabled;

		public bool NoGraphic => turretDef.graphicData is null;

		public bool CanAutoTarget => autoTargeting || VehicleMod.settings.debug.debugShootAnyTurret;

		public int MaxTicks => Mathf.CeilToInt(turretDef.reloadTimer * 60f);

		public int WarmupTicks => Mathf.CeilToInt(turretDef.warmUpTimer * 60f);

		public bool OnCooldown => triggeredCooldown;

		public bool CanOverheat => VehicleMod.settings.main.overheatMechanics && turretDef.cooldown != null && turretDef.cooldown.heatPerShot > 0;

		public bool Recoils => turretDef.recoil != null;

		public bool HasAmmo => turretDef.ammunition is null || shellCount > 0;

		public bool ReadyToFire => groupKey.NullOrEmpty() ? (burstTicks <= 0 && ReloadTicks <= 0 && !TurretDisabled) : GroupTurrets.Any(t => t.burstTicks <= 0 && t.ReloadTicks <= 0 && !t.TurretDisabled);

		public bool FullAuto => CurrentFireMode.ticksBetweenBursts == CurrentFireMode.ticksBetweenShots;

		public int ReloadTicks => reloadTicks;

		public Dictionary<VehicleTurretEventDef, EventTrigger> EventRegistry { get; set; }

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
						groupTurrets = vehicle.CompVehicleTurrets.turrets.Where(t => t.groupKey == groupKey).ToList();
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
					if (!CanOverheat)
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
				if (ReloadTicks <= 0)
				{
					return 1;
				}
				return Mathf.PingPong(ReloadTicks, 25) / 50f + 0.25f; //ping pong between 0.25 and 0.75 alpha
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
					mainMaskTex = ContentFinder<Texture2D>.Get(CannonGraphicData.texPath + Graphic_Turret.TurretMaskSuffix);
				}
				return mainMaskTex;
			}
		}

		public virtual Graphic_Turret CannonGraphic
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
				if (gizmoIcon is null)
				{
					if (!string.IsNullOrEmpty(turretDef.gizmoIconTexPath))
					{
						gizmoIcon = ContentFinder<Texture2D>.Get(turretDef.gizmoIconTexPath);
					}
					else if (NoGraphic)
					{
						gizmoIcon = BaseContent.BadTex;
					}
					else
					{
						if (CannonTexture != null)
						{
							gizmoIcon = CannonTexture;
						}
						else
						{
							gizmoIcon = BaseContent.BadTex;
						}
					}
				}
				return gizmoIcon;
			}
		}

		/// <summary>
		/// Smaller actions inside VehicleTurret gizmo
		/// </summary>
		public virtual IEnumerable<SubGizmo> SubGizmos
		{
			get
			{
				yield return SubGizmo_RemoveAmmo(this);
				yield return SubGizmo_ReloadFromInventory(this);
				yield return SubGizmo_FireMode(this);
				if (autoTargeting)
				{
					yield return SubGizmo_AutoTarget(this);
				}
			}
		}

		public Vector3 TurretLocation
		{
			get
			{
				if (attachedTo != null)
				{
					//Don't use cached value if attached to parent turret (position may change with rotations)
					return TurretDrawLocFor(vehicle.FullRotation, vehicle.DrawPos);
				}
				return vehicle.FullRotation.AsInt switch
				{
					//North
					0 => vehicle.DrawPos + rootDrawPos_North,
					//East
					1 => vehicle.DrawPos + rootDrawPos_East,
					//South
					2 => vehicle.DrawPos + rootDrawPos_South,
					//West
					3 => vehicle.DrawPos + rootDrawPos_West,
					//NorthEast
					4 => vehicle.DrawPos + rootDrawPos_NorthEast,
					//SouthEast
					5 => vehicle.DrawPos + rootDrawPos_SouthEast,
					//SouthWest
					6 => vehicle.DrawPos + rootDrawPos_SouthWest,
					//NorthWest
					7 => vehicle.DrawPos + rootDrawPos_NorthWest,
					//Should not reach here
					_ => throw new NotImplementedException("Invalid Rot8")
				};
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
				currentRotation = value.ClampAndWrap(0, 360);
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
				if (!CanAutoTarget || value == autoTargetingActive)
				{
					return;
				}

				autoTargetingActive = value;

				if (autoTargetingActive)
				{
					StartTicking();
				}
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

		public int MaterialCount => 1;

		public string Name => $"{turretDef}_{key}_{vehicle?.ThingID ?? "Def"}";

		public PatternDef PatternDef
		{
			get
			{
				if (!CannonGraphicData.shaderType.Shader.SupportsRGBMaskTex())
				{
					return null;
				}
				if (turretDef.matchParentColor)
				{
					return vehicle?.PatternDef ?? PatternDefOf.Default;
				}
				return CannonGraphicData.pattern;
			}
		}

		public void Init(VehicleTurret reference)
		{
			groupKey = reference.groupKey;
			parentKey = reference.parentKey;

			renderProperties = new VehicleTurretRender(reference.renderProperties);
			aimPieOffset = reference.aimPieOffset;
			angleRestricted = reference.angleRestricted;
			restrictedTheta = (int)Mathf.Abs(angleRestricted.x - (angleRestricted.y + 360)).ClampAngle();

			defaultAngleRotated = reference.defaultAngleRotated;

			drawLayer = reference.drawLayer;
			if (reference.turretDef.restrictionType != null)
			{
				restrictions = (TurretRestrictions)Activator.CreateInstance(reference.turretDef.restrictionType);
				restrictions.Init(vehicle, this);
			}

			ResetAngle();
			LongEventHandler.ExecuteWhenFinished(RecacheRootDrawPos);
		}

		/// <summary>
		/// Caches VehicleTurret draw location based on <see cref="renderProperties"/>, <see cref="attachedTo"/> draw loc is not cached, as rotating can alter the final draw location
		/// </summary>
		public void RecacheRootDrawPos()
		{
			if (CannonGraphicData != null)
			{
				rootDrawPos_North = TurretDrawLocFor(Rot8.North, Vector3.zero, fullLoc: false);
				rootDrawPos_East = TurretDrawLocFor(Rot8.East, Vector3.zero, fullLoc: false);
				rootDrawPos_South = TurretDrawLocFor(Rot8.South, Vector3.zero, fullLoc: false);
				rootDrawPos_West = TurretDrawLocFor(Rot8.West, Vector3.zero, fullLoc: false);
				rootDrawPos_NorthEast = TurretDrawLocFor(Rot8.NorthEast, Vector3.zero, fullLoc: false);
				rootDrawPos_SouthEast = TurretDrawLocFor(Rot8.SouthEast, Vector3.zero, fullLoc: false);
				rootDrawPos_SouthWest = TurretDrawLocFor(Rot8.SouthWest, Vector3.zero, fullLoc: false);
				rootDrawPos_NorthWest = TurretDrawLocFor(Rot8.NorthWest, Vector3.zero, fullLoc: false);
			}
		}

		public void RecacheMannedStatus()
		{
			IsManned = true;
			foreach (VehicleHandler handler in vehicle.handlers)
			{
				if (handler.role.handlingTypes.HasFlag(HandlingTypeFlags.Turret) && (handler.role.turretIds.Contains(key) || handler.role.turretIds.Contains(groupKey)))
				{
					if (!handler.RoleFulfilled)
					{
						IsManned = false;
						break;
					}
				}
			}
			IsManned |= VehicleMod.settings.debug.debugShootAnyTurret; //Only if debug shoot any turret = true, set satisfied to true anyways.
		}

		public bool GroupsWith(VehicleTurret turret)
		{
			return !groupKey.NullOrEmpty() && groupKey == turret.groupKey;
		}

		public Vector3 TurretDrawLocFor(Rot8 rot, Vector3 pos, bool fullLoc = true)
		{
			float locationRotation = 0f;
			if (fullLoc && attachedTo != null)
			{
				locationRotation = TurretRotationFor(rot, attachedTo.TurretRotationUncorrected);
			}
			Vector2 turretLoc = VehicleGraphics.TurretDrawOffset(rot, renderProperties, locationRotation, fullLoc ? attachedTo : null);
			Vector3 graphicOffset = CannonGraphic?.DrawOffset(rot) ?? Vector3.zero;
			return new Vector3(pos.x + graphicOffset.x + turretLoc.x, pos.y + graphicOffset.y + drawLayer * Altitudes.AltInc, pos.z + graphicOffset.z + turretLoc.y);
		}

		public Rect ScaleUIRectRecursive(VehicleDef vehicleDef, Rect rect, Rot8 rot, float iconScale = 1)
		{
			//Scale to VehicleDef drawSize
			Vector2 size = vehicleDef.ScaleDrawRatio(turretDef.graphicData, rect.size, iconScale: iconScale);
			//Adjust position from new rect size
			Vector2 adjustedPosition = rect.position + (rect.size - size) / 2f;
			// Size / V_max = scalar
			float scalar = rect.size.x / Mathf.Max(vehicleDef.graphicData.drawSize.x, vehicleDef.graphicData.drawSize.y);
			Vector2 offset = rot.AsInt switch
			{
				//North
				0 => renderProperties.north.Offset,
				//East
				1 => renderProperties.east.Offset,
				//South
				2 => renderProperties.south.Offset,
				//West
				3 => renderProperties.west.Offset,
				//Diagonals not handled
				_ => throw new NotImplementedException("Diagonal rotations"),
			};

			Vector3 graphicOffset = turretDef.graphicData.DrawOffsetForRot(rot);
			
			Vector2 position = adjustedPosition + (scalar * new Vector2(graphicOffset.x, graphicOffset.z));
			Vector2 offsetPosition = scalar * offset;
			Vector2 parentPosition = Vector2.zero;
			if (attachedTo != null)
			{
				parentPosition = attachedTo.ScaleUIRectRecursive(vehicleDef, rect, rot).position; //Recursively adjust from parent positions
				float parentRotation = TurretRotationFor(rot, attachedTo.TurretRotationUncorrected) + rot.AsAngle;
				offsetPosition = Ext_Math.RotatePointClockwise(offsetPosition, parentRotation); //Rotate around parent's position
			}
			offsetPosition.y *= -1; //Invert y axis post-calculations, UI y-axis is top to bottom
			offsetPosition += parentPosition;
			position += offsetPosition;
			return new Rect(position, size);
		}

		public static float TurretRotationFor(Rot8 rot, float currentRotation)
		{
			float zeroAngle = 270 - currentRotation;
			return zeroAngle - 45 * rot.AsIntClockwise;
		}

		//REDO - disable type implementation
		public virtual bool TurretEnabled(VehicleDef vehicleDef, TurretDisableType turretKey)
		{
			if (conditionalTurrets.Contains(new Pair<string, TurretDisableType>(vehicleDef.defName, turretKey)))
			{

			}
			return false;
		}

		public virtual bool ActivateTimer(bool ignoreTimer = false)
		{
			if (ReloadTicks > 0 && !ignoreTimer)
			{
				return false;
			}
			reloadTicks = MaxTicks;
			TargetLocked = false;
			StartTicking();
			return true;
		}

		public virtual void ActivateBurstTimer()
		{
			burstTicks = CurrentFireMode.ticksBetweenBursts;
		}

		public void StartTicking()
		{
			vehicle.CompVehicleTurrets.QueueTicker(this);
		}

		/// <summary>
		/// Should only be called in the event that this turret needs to stop ticking unconditionally, otherwise let <see cref="CompVehicleTurrets"/> dequeue
		/// when it's determined this turret no longer requires ticking.
		/// </summary>
		public void StopTicking()
		{
			vehicle.CompVehicleTurrets.DequeueTicker(this);
		}

		public virtual bool Tick()
		{
			bool cooldownTicked = TurretCooldownTick();
			bool reloadTicked = TurretReloadTick();
			bool autoTicked = TurretAutoTick();
			bool rotationTicked = TurretRotationTick();
			bool targeterTicked = TurretTargeterTick();
			bool recoilTicked = false;
			if (Recoils)
			{
				recoilTicked = rTracker.RecoilTick();
			}
			//Keep ticking until no longer needed
			return cooldownTicked || reloadTicked || autoTicked || rotationTicked || targeterTicked || recoilTicked;
		}

		protected virtual bool TurretCooldownTick()
		{
			if (CanOverheat)
			{
				if (currentHeatRate > 0)
				{
					ticksSinceLastShot++;
				}

				if (currentHeatRate > MaxHeatCapacity)
				{
					triggeredCooldown = true;
					currentHeatRate = MaxHeatCapacity;
					EventRegistry[VehicleTurretEventDefOf.Cooldown].ExecuteEvents();
				}
				else if (currentHeatRate <= 0)
				{
					currentHeatRate = 0;
					triggeredCooldown = false;
					return false;
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
				return true;
			}
			return false;
		}

		protected virtual bool TurretReloadTick()
		{
			if (vehicle.Spawned && !queuedToFire)
			{
				if (ReloadTicks > 0 && !OnCooldown)
				{
					reloadTicks--;
					return true;
				}
				if (burstTicks > 0)
				{
					burstTicks--;
					return true;
				}
			}
			return false;
		}

		protected virtual bool TurretAutoTick()
		{
			//Todo - only tick if active threats on the map
			if (vehicle.Spawned && !queuedToFire && AutoTarget)
			{
				if (Find.TickManager.TicksGame % AutoTargetInterval == 0)
				{
					if (TurretDisabled)
					{
						return false;
					}
					if (!cannonTarget.IsValid && TurretTargeter.Turret != this && ReloadTicks <= 0 && HasAmmo)
					{
						LocalTargetInfo autoTarget = this.GetCannonTarget();
						if (autoTarget.IsValid)
						{
							AlignToAngleRestricted(TurretLocation.AngleToPointRelative(autoTarget.Thing.DrawPos));
							SetTarget(autoTarget);
						}
					}
				}
				return true;
			}
			return false;
		}

		protected virtual bool TurretRotationTick()
		{
			if (currentRotation != rotationTargeted)
			{
				//REDO - SET TO CHECK CANNON HANDLERS COMPONENT HEALTH
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
					int rotationDir;
					if (relativeCurrentRotation < relativeTargetedRotation)
					{
						if (Math.Abs(relativeCurrentRotation - relativeTargetedRotation) < 180)
						{
							rotationDir = 1;
						}
						else
						{
							rotationDir = -1;
						}
					}
					else
					{
						if (Math.Abs(relativeCurrentRotation - relativeTargetedRotation) < 180)
						{
							rotationDir = -1;
						}
						else
						{
							rotationDir = 1;
						}
					}
					currentRotation += turretDef.rotationSpeed * rotationDir;
					foreach (VehicleTurret turret in childTurrets)
					{
						turret.currentRotation += turretDef.rotationSpeed * rotationDir;
					}
				}
				return true;
			}
			return false;
		}

		protected virtual bool TurretTargeterTick()
		{
			if (cannonTarget.IsValid)
			{
				if (currentRotation == rotationTargeted && !TargetLocked)
				{
					TargetLocked = true;
					ResetPrefireTimer();
				}
				else if (!TurretTargetValid)
				{
					SetTarget(LocalTargetInfo.Invalid);
					return TurretTargeter.Turret == this;
				}
			}
			if (TurretTargetValid)
			{
				if (IsTargetable && !TurretTargeter.TargetMeetsRequirements(this, cannonTarget))
				{
					SetTarget(LocalTargetInfo.Invalid);
					TargetLocked = false;
					return TurretTargeter.Turret == this;
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
						GenDraw.DrawAimPieRaw(TurretLocation + new Vector3(aimPieOffset.x, Altitudes.AltInc, aimPieOffset.y).RotatedBy(TurretRotation), facing, (int)(PrefireTickCount * 0.5f));
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
			else if (IsTargetable && SetTargetConditionalOnThing(LocalTargetInfo.Invalid))
			{
				return TurretTargeter.Turret == this;
			}
			return true;
		}

		public virtual CompVehicleTurrets.TurretData GenerateTurretData()
		{
			return new CompVehicleTurrets.TurretData()
			{
				shots = CurrentFireMode.shotsPerBurst,
				ticksTillShot = 0,
				turret = this
			};
		}

		public virtual void PushTurretToQueue()
		{
			ActivateBurstTimer();
			vehicle.CompVehicleTurrets.QueueTurret(GenerateTurretData());
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
			IntVec3 cell = cannonTarget.Cell + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(CurrentFireMode.spreadRadius * (range / turretDef.maxRange)))];
			if (CurrentTurretFiring >= turretDef.projectileShifting.Count)
			{
				CurrentTurretFiring = 0;
			}
			float horizontalOffset = turretDef.projectileShifting.NotNullAndAny() ? turretDef.projectileShifting[CurrentTurretFiring] : 0;
			Vector3 launchCell = TurretLocation + new Vector3(horizontalOffset, 1f, turretDef.projectileOffset).RotatedBy(TurretRotation);

			ThingDef projectile = turretDef.projectile;
			if (turretDef.ammunition != null && !turretDef.genericAmmo)
			{
				projectile = loadedAmmo?.projectileWhenLoaded ?? loadedAmmo; //nc to loaded ammo for CE handling
			}
			try
			{
				if (turretDef.ammunition != null)
				{
					ConsumeShellChambered();
				}
				
				if (LaunchProjectileCE is null)
				{
					Projectile projectileInstance = (Projectile)GenSpawn.Spawn(projectile, vehicle.Position, vehicle.Map, WipeMode.Vanish);
					if (turretDef.projectileSpeed > 0)
					{
						projectileInstance.TryAddComp(new CompTurretProjectileProperties(vehicle)
						{
							speed = turretDef.projectileSpeed > 0 ? turretDef.projectileSpeed : projectileInstance.def.projectile.speed,
							hitflag = turretDef.hitFlags,
							hitflags = turretDef.attachProjectileFlag
						});
					}
					projectileInstance.Launch(vehicle, launchCell, cell, cannonTarget, projectileInstance.HitFlags, false, vehicle);
				}
				else
				{
					float speed = turretDef.projectileSpeed > 0 ? turretDef.projectileSpeed : projectile.projectile.speed;
					float swayAndSpread = Mathf.Atan2(CurrentFireMode.spreadRadius, MaxRange) * Mathf.Rad2Deg;
					float sway = swayAndSpread * 0.84f;
					float spread = swayAndSpread * 0.16f;
					if (turretDef.GetModExtension<CETurretDataDefModExtension>() is CETurretDataDefModExtension turretData)
					{
						if (turretData.speed > 0)
						{
							speed = turretData.speed;
						}
						if (turretData.sway >= 0)
						{
							sway = turretData.sway;
						}
						if (turretData.spread >= 0)
						{
							spread = turretData.spread;
						}
					}
					float distance = (launchCell - cannonTarget.CenterVector3).magnitude;
					LaunchProjectileCE(projectile, new Vector2(launchCell.x, launchCell.z), cannonTarget, vehicle, ProjectileAngleCE(speed, distance, -0.5f, false, 1f), -TurretRotation, 1f, speed);
				}
				turretDef.shotSound?.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map));
				vehicle.Drawer.rTracker.Notify_TurretRecoil(this, Ext_Math.RotateAngle(TurretRotation, 180));
				rTracker.Notify_TurretRecoil(this, Ext_Math.RotateAngle(TurretRotation, 180));
				EventRegistry[VehicleTurretEventDefOf.ShotFired].ExecuteEvents();
				PostTurretFire();
				InitTurretMotes(launchCell);
			}
			catch (Exception ex)
			{
				Log.Error($"Exception when firing VehicleTurret: {turretDef.defName} on vehicle: {vehicle}.\nException: {ex.Message}\nStackTrace {ex.StackTrace}");
			}
		}

		public virtual void PostTurretFire()
		{
			ticksSinceLastShot = 0;
			if (CanOverheat)
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

		public virtual void DrawAt(Vector3 drawPos)
		{
			if (!NoGraphic)
			{
				VehicleGraphics.DrawTurret(this, drawPos, Rot8.North);
				DrawTargeter();
			}
		}

		public virtual void Draw()
		{
			if (!NoGraphic)
			{
				VehicleGraphics.DrawTurret(this, Rot8.North);
				DrawTargeter();
			}
		}

		protected virtual void DrawTargeter()
		{
			if (GizmoHighlighted || TurretTargeter.Turret == this)
			{
				if (angleRestricted != Vector2.zero)
				{
					VehicleGraphics.DrawAngleLines(TurretLocation, angleRestricted, MinRange, MaxRange, restrictedTheta, attachedTo?.TurretRotation ?? vehicle.FullRotation.AsAngle);
				}
				else if (turretDef.turretType == TurretType.Static)
				{
					if (!groupKey.NullOrEmpty())
					{
						foreach (VehicleTurret turret in GroupTurrets)
						{
							Vector3 target = turret.TurretLocation.PointFromAngle(turret.MaxRange, turret.TurretRotation);
							float range = Vector3.Distance(turret.TurretLocation, target);
							GenDraw.DrawRadiusRing(target.ToIntVec3(), turret.CurrentFireMode.spreadRadius * (range / turret.turretDef.maxRange));
						}
					}
					else
					{
						Vector3 target = TurretLocation.PointFromAngle(MaxRange, TurretRotation);
						float range = Vector3.Distance(TurretLocation, target);
						GenDraw.DrawRadiusRing(target.ToIntVec3(), CurrentFireMode.spreadRadius * (range / turretDef.maxRange));
					}
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

		public virtual void ResolveCannonGraphics(VehiclePawn vehicle, bool forceRegen = false)
		{
			ResolveCannonGraphics(vehicle.patternData, forceRegen);
		}

		public virtual void ResolveCannonGraphics(PatternData patternData, bool forceRegen = false)
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
					cachedGraphicData.color = patternData.color;
					cachedGraphicData.colorTwo = patternData.colorTwo;
					cachedGraphicData.colorThree = patternData.colorThree;
					cachedGraphicData.tiles = patternData.tiles;
					cachedGraphicData.displacement = patternData.displacement;
				}

				if (cachedGraphicData.shaderType.Shader.SupportsRGBMaskTex())
				{
					RGBMaterialPool.CacheMaterialsFor(this);
					cachedGraphicData.Init(this);
					cannonGraphic = CannonGraphicData.Graphic as Graphic_Turret;
					RGBMaterialPool.SetProperties(this, patternData, cannonGraphic.TexAt, cannonGraphic.MaskAt);
				}
				else
				{
					cannonGraphic = ((GraphicData)cachedGraphicData).Graphic as Graphic_Turret;
				}
			}

			if (cannonGraphic is null || forceRegen)
			{
				cannonGraphic = CannonGraphicData.Graphic as Graphic_Turret;
			}
			if (cannonMaterialCache is null || forceRegen)
			{
				cannonMaterialCache = CannonGraphic.MatAt(Rot8.North, vehicle);
			}
		}

		public virtual void ResolveCannonGraphics(VehicleDef vehicleDef, bool forceRegen = false)
		{
			if (NoGraphic)
			{
				return;
			}
			GraphicDataRGB defaultDrawData = VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData);
			if (cachedGraphicData is null || forceRegen)
			{
				cachedGraphicData = new GraphicDataRGB();
				cachedGraphicData.CopyFrom(turretDef.graphicData);
				cachedGraphicData.CopyDrawData(defaultDrawData);
				PatternData patternData = defaultDrawData;
				if (turretDef.matchParentColor)
				{
					patternData = VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData);
					cachedGraphicData.color = patternData.color;
					cachedGraphicData.colorTwo = patternData.colorTwo;
					cachedGraphicData.colorThree = patternData.colorThree;
					cachedGraphicData.tiles = patternData.tiles;
					cachedGraphicData.displacement = patternData.displacement;
				}

				if (cachedGraphicData.shaderType.Shader.SupportsRGBMaskTex())
				{
					RGBMaterialPool.CacheMaterialsFor(this);
					cachedGraphicData.Init(this);
					cannonGraphic = CannonGraphicData.Graphic as Graphic_Turret;
					RGBMaterialPool.SetProperties(this, patternData, cannonGraphic.TexAt, cannonGraphic.MaskAt);
				}
				else
				{
					cannonGraphic = ((GraphicData)cachedGraphicData).Graphic as Graphic_Turret;
				}
			}
			if (cannonMaterialCache is null || forceRegen)
			{
				cannonMaterialCache = CannonGraphic.MatAtFull(Rot8.North);
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
			float additionalAngle = attachedTo?.TurretRotation ?? 0;
			if (turretDef.autoSnapTargeting)
			{
				TurretRotation = angle + additionalAngle;
				rotationTargeted = currentRotation;
			}
			else
			{
				rotationTargeted = (angle + additionalAngle).ClampAndWrap(0, 360);
			}
		}

		public virtual void ReloadCannon(ThingDef ammo = null, bool ignoreTimer = false)
		{
			if ( (ammo == savedAmmoType || ammo is null) && shellCount == turretDef.magazineCapacity)
			{
				return;
			}
			if (turretDef.ammunition is null)
			{
				shellCount = turretDef.magazineCapacity;
				return;
			}
			if (loadedAmmo is null || (ammo != null && shellCount < turretDef.magazineCapacity) || shellCount <= 0 || ammo != null)
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
					int countToRefill = turretDef.magazineCapacity - shellCount;
					int countToTake = Mathf.FloorToInt(turretDef.chargePerAmmoCount * countToRefill);
					if (countToTake == 0)
					{
						Log.Error($"CountToTake = 0, if you don't want to charge ammo then don't set it up for ammo.");
						return;
					}
					if (countToTake > storedAmmo.stackCount)
					{
						countToTake = storedAmmo.stackCount;
						countToRefill = Mathf.FloorToInt(countToTake / (float)turretDef.chargePerAmmoCount);
					}
					if (countToRefill == 0 || countToTake == 0)
					{
						return;
					}
					//vehicle.inventory.innerContainer.Take(storedAmmo, countToTake);
					vehicle.TakeFromInventory(storedAmmo, countToTake);
					int additionalCount = 0;
					int additionalCountToTake = 0;
					if (countToRefill + shellCount < turretDef.magazineCapacity)
					{
						foreach(Thing t in vehicle.inventory.innerContainer)
						{
							if (t.def == storedAmmo.def)
							{
								additionalCount = t.stackCount >= turretDef.magazineCapacity - (shellCount + countToRefill) ? turretDef.magazineCapacity - (shellCount + countToRefill) : t.stackCount;
								additionalCountToTake = Mathf.FloorToInt(turretDef.chargePerAmmoCount * additionalCount);
								//vehicle.inventory.innerContainer.Take(t, additionalCountToTake);
								vehicle.TakeFromInventory(t, additionalCountToTake);
								if (additionalCount + countToRefill >= turretDef.magazineCapacity) break;
							}    
						}
					}
					
					loadedAmmo = storedAmmo.def;
					shellCount = countToRefill + additionalCount;
					EventRegistry[VehicleTurretEventDefOf.Reload].ExecuteEvents();
					if (turretDef.reloadSound != null)
					{
						turretDef.reloadSound.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map, false));
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to reload Cannon: {uniqueID} on Pawn: {vehicle.LabelShort}. Exception: {ex}");
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
			if (loadedAmmo != null && shellCount > 0)
			{
				Thing thing = ThingMaker.MakeThing(loadedAmmo);
				thing.stackCount = shellCount * turretDef.chargePerAmmoCount;
				//vehicle.inventory.innerContainer.TryAdd(thing);
				vehicle.AddOrTransfer(thing);
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
			if (CanAutoTarget)
			{
				SoundDefOf.Click.PlayOneShotOnCamera(vehicle.Map);
				AutoTarget = !AutoTarget;
				SetTarget(LocalTargetInfo.Invalid);
				if (AutoTarget)
				{
					StartTicking();
				}
			}
			else
			{
				Messages.Message("VF_AutoTargetingDisabled".Translate(), MessageTypeDefOf.RejectInput);
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

			if (cannonTarget.IsValid)
			{
				StartTicking();
			}
		}

		/// <summary>
		/// Set target only if cannonTarget is no longer valid or if target is cell based
		/// </summary>
		/// <param name="target"></param>
		/// <returns>true if cannonTarget set to target, false if target is still valid</returns>
		public virtual bool SetTargetConditionalOnThing(LocalTargetInfo target, bool resetPrefireTimer = true)
		{
			if (cannonTarget.IsValid && (cannonTarget.HasThing || FullAuto))
			{
				if (cannonTarget.Pawn != null)
				{
					if ( (cannonTarget.Pawn.Dead && CachedPawnTargetStatus != PawnStatusOnTarget.Dead ) || (cannonTarget.Pawn.Downed && CachedPawnTargetStatus != PawnStatusOnTarget.Down) )
					{
						SetTarget(target);
						return true;
					}
				}
				else if (cannonTarget.Thing != null && cannonTarget.Thing.HitPoints <= 0)
				{
					SetTarget(target);
					return true;
				}
				return false;
			}
			SetTarget(target);
			return true;
		}

		public void ResetAngle()
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
			EventRegistry[VehicleTurretEventDefOf.Warmup].ExecuteEvents();
		}

		protected void ValidateLockStatus()
		{
			if (!cannonTarget.IsValid && TurretTargeter.Turret != this) 
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
			return $"{turretDef}_{GetUniqueLoadID()}";
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
			if (vehicleDef.GetCompProperties<CompProperties_VehicleTurrets>().turrets.Select(x => x.key).GroupBy(y => y).Where(y => y.Count() > 1).Select(z => z.Key).NotNullAndAny())
			{
				yield return $"Duplicate turret key {key}";
			}
		}

		public static string TurretEnableTypeDisableReason(TurretDisableType currentType)
		{
			return currentType switch
			{
				TurretDisableType.InFlight => "TurretDisableType_Always".Translate().ToString(),
				TurretDisableType.Strafing => "TurretDisableType_Always".Translate().ToString(),
				TurretDisableType.Grounded => "TurretDisableType_Always".Translate().ToString(),
				_ => "TurretDisableType_Always".Translate().ToString(),
			};
		}

		public virtual void OnDestroy()
		{
			RGBMaterialPool.Release(this);
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref autoTargetingActive, nameof(autoTargetingActive));

			Scribe_Values.Look(ref reloadTicks, nameof(reloadTicks));
			Scribe_Values.Look(ref burstTicks, nameof(burstTicks));

			Scribe_Values.Look(ref uniqueID, nameof(uniqueID), defaultValue: -1);
			Scribe_Values.Look(ref key, nameof(key));

			Scribe_Defs.Look(ref turretDef, nameof(turretDef));

			Scribe_Values.Look(ref targetPersists, nameof(targetPersists));
			Scribe_Values.Look(ref autoTargeting, nameof(autoTargeting));
			Scribe_Values.Look(ref manualTargeting, nameof(manualTargeting));

			Scribe_Values.Look(ref queuedToFire, nameof(queuedToFire));
			Scribe_Values.Look(ref currentFireMode, nameof(currentFireMode));
			Scribe_Values.Look(ref currentHeatRate, nameof(currentHeatRate));
			Scribe_Values.Look(ref triggeredCooldown, nameof(triggeredCooldown));
			Scribe_Values.Look(ref ticksSinceLastShot, nameof(ticksSinceLastShot));

			Scribe_Values.Look(ref rotation, nameof(rotation), defaultValue: defaultAngleRotated);
			Scribe_Values.Look(ref restrictedTheta, nameof(restrictedTheta), defaultValue: (int)Mathf.Abs(angleRestricted.x - (angleRestricted.y + 360)).ClampAngle());

			Scribe_Defs.Look(ref loadedAmmo, nameof(loadedAmmo));
			Scribe_Defs.Look(ref savedAmmoType, nameof(savedAmmoType));
			Scribe_Values.Look(ref shellCount, nameof(shellCount));
			Scribe_Values.Look(ref gizmoLabel, nameof(gizmoLabel));

			Scribe_TargetInfo.Look(ref cannonTarget, nameof(cannonTarget), defaultValue: LocalTargetInfo.Invalid);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				TurretRotation = Mathf.Abs(rotation - 270); //convert from traditional to relative, vehicle rotation will be taken into account during lock validation
				if (cannonTarget.IsValid)
				{
					AlignToTargetRestricted(); //reassigns rotationTargeted for turrets currently turning
				}
			}
		}

		public static SubGizmo SubGizmo_RemoveAmmo(VehicleTurret turret)
		{
			return new SubGizmo
			{
				drawGizmo = delegate (Rect rect)
				{
					//Widgets.DrawTextureFitted(rect, BGTex, 1);
					if (turret.loadedAmmo != null)
					{
						GUIState.Push();
						{
							GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, turret.CannonIconAlphaTicked); //Only modify alpha
							Widgets.DrawTextureFitted(rect, turret.loadedAmmo.uiIcon, 1);
							
							GUIState.Reset();

							Rect ammoCountRect = new Rect(rect);
							string ammoCount = turret.vehicle.inventory.innerContainer.Where(td => td.def == turret.loadedAmmo).Select(t => t.stackCount).Sum().ToStringSafe();
							ammoCountRect.y += ammoCountRect.height / 2;
							ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
							Widgets.Label(ammoCountRect, ammoCount);
						}
						GUIState.Pop();
					}
					else if (turret.turretDef.genericAmmo && turret.turretDef.ammunition.AllowedDefCount > 0)
					{
						Widgets.DrawTextureFitted(rect, turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault().uiIcon, 1);
						
						Rect ammoCountRect = new Rect(rect);
						string ammoCount = turret.vehicle.inventory.innerContainer.Where(td => td.def == turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault()).Select(t => t.stackCount).Sum().ToStringSafe();
						ammoCountRect.y += ammoCountRect.height / 2;
						ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
						Widgets.Label(ammoCountRect, ammoCount);
					}
				},
				canClick = delegate ()
				{
					return turret.shellCount > 0;
				},
				onClick = delegate ()
				{
					turret.TryRemoveShell();
					SoundDefOf.Artillery_ShellLoaded.PlayOneShot(new TargetInfo(turret.vehicle.Position, turret.vehicle.Map, false));
				},
				tooltip = turret.loadedAmmo?.LabelCap
			};
		}

		public static SubGizmo SubGizmo_ReloadFromInventory(VehicleTurret turret)
		{
			return new SubGizmo
			{
				drawGizmo = delegate (Rect rect)
				{
					Widgets.DrawTextureFitted(rect, VehicleTex.ReloadIcon, 1);
				},
				canClick = delegate ()
				{
					return true;
				},
				onClick = delegate ()
				{
					if (turret.turretDef.genericAmmo)
					{
						if (!turret.vehicle.inventory.innerContainer.Contains(turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault()))
						{
							Messages.Message("VF_NoAmmoAvailable".Translate(), MessageTypeDefOf.RejectInput);
						}
						else
						{
							turret.ReloadCannon(turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault());
						}
					}
					else
					{
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						var ammoAvailable = turret.vehicle.inventory.innerContainer.Where(d => turret.ContainsAmmoDefOrShell(d.def)).Select(t => t.def).Distinct().ToList();
						for (int i = ammoAvailable.Count - 1; i >= 0; i--)
						{
							ThingDef ammo = ammoAvailable[i];
							options.Add(new FloatMenuOption(ammoAvailable[i].LabelCap, delegate ()
							{
								turret.ReloadCannon(ammo, true);
							}));
						}
						if (options.NullOrEmpty())
						{
							FloatMenuOption noAmmoOption = new FloatMenuOption("VF_VehicleTurrets_NoAmmoToReload".Translate(), null);
							noAmmoOption.Disabled = true;
							options.Add(noAmmoOption);
						}
						Find.WindowStack.Add(new FloatMenu(options));
					}
				},
				tooltip = "VF_ReloadVehicleTurret".Translate()
			};
		}

		public static SubGizmo SubGizmo_FireMode(VehicleTurret turret)
		{
			return new SubGizmo
			{
				drawGizmo = delegate (Rect rect)
				{
					Widgets.DrawTextureFitted(rect, turret.CurrentFireMode.Icon, 1);
				},
				canClick = delegate ()
				{
					return turret.turretDef.fireModes.Count > 1;
				},
				onClick = delegate ()
				{
					turret.CycleFireMode();
				},
				tooltip = turret.CurrentFireMode.label
			};
		}

		public static SubGizmo SubGizmo_AutoTarget(VehicleTurret turret)
		{
			return new SubGizmo
			{
				drawGizmo = delegate (Rect rect)
				{
					Widgets.DrawTextureFitted(rect, VehicleTex.AutoTargetIcon, 1);
					Rect checkboxRect = new Rect(rect.x + rect.width / 2, rect.y + rect.height / 2, rect.width / 2, rect.height / 2);
					GUI.DrawTexture(checkboxRect, turret.AutoTarget ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);
				},
				canClick = delegate ()
				{
					return turret.CanAutoTarget;
				},
				onClick = delegate ()
				{
					turret.SwitchAutoTarget();
				},
				tooltip = "VF_AutoTargeting".Translate(turret.AutoTarget.ToStringYesNo())
			};
		}

		public (Texture2D mainTex, Texture2D maskTex) GetTextures(Rot8 rot)
		{
			throw new NotImplementedException();
		}

		public struct SubGizmo
		{
			public Action<Rect> drawGizmo;
			public Func<bool> canClick;
			public Action onClick;
			public string tooltip;

			public bool IsValid => onClick != null;

			public static SubGizmo None { get; private set; } = new SubGizmo();
		}
	}
}
