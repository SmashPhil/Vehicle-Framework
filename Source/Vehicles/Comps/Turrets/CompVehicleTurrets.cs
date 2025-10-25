using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using Vehicles.Rendering;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Vehicles;

[PublicAPI]
[HeaderTitle(Label = nameof(CompVehicleTurrets))]
public class CompVehicleTurrets : VehicleAIComp, IRefundable
{
	private static readonly HashSet<string> DuplicateKeySet = [];

	private List<TurretData> turretQueue = [];

	private bool deployed;
	internal int deployTicks;

	private Dictionary<VehicleTurret, int> turretQuotas = [];
	private List<BackupTurretQuota> backupQuotas = [];

	[TweakField]
	private List<VehicleTurret> turrets = [];

	[Unsaved]
	private readonly List<VehicleTurret> tickers = [];

	private List<VehicleTurret> tmpListTurrets = [];
	private List<int> tmpListTurretQuota = [];


	// Gizmos
	private Command_Toggle deployToggle;
	private readonly List<Command_Turret> turretGizmos = [];

	public float MinRange { get; private set; }

	public float MaxRange { get; private set; }

	public bool CanDeploy { get; private set; }

	public bool Deployed => deployed;

	public int DeployTicks => Mathf.RoundToInt(SettingsCache.TryGetValue(Vehicle.VehicleDef,
		typeof(CompProperties_VehicleTurrets), nameof(CompProperties_VehicleTurrets.deployTime),
		Props.deployTime) * 60);

	public bool Deploying => Vehicle.jobs.curJob?.def == JobDefOf_Vehicles.DeployVehicle;

	private bool ShouldStopTicking => tickers.Count == 0;

	public CompProperties_VehicleTurrets Props => (CompProperties_VehicleTurrets)props;

	public IReadOnlyList<VehicleTurret> Turrets => turrets;

	public IEnumerable<(ThingDef thingDef, float count)> Refunds
	{
		get
		{
			foreach (VehicleTurret turret in turrets)
			{
				yield return (turret.loadedAmmo, turret.shellCount * turret.def.chargePerAmmoCount);
			}
		}
	}

	public bool TurretsAligned
	{
		get
		{
			foreach (VehicleTurret turret in turrets)
			{
				bool alignTurret = (turret.deployment == DeploymentType.Deployed && Deployed) ||
					(turret.deployment == DeploymentType.Undeployed && !Deployed);
				if (alignTurret && !turret.RotationAligned)
				{
					return false;
				}
			}

			return true;
		}
	}

	public float OptimalDistance
	{
		get
		{
			// TODO - set max range based on explosive / breach turrets
			return MaxRange * Vehicle.VehicleDef.npcProperties.targetPositionRadiusPercent;
		}
	}

	public void FlagAllTurretsForAlignment()
	{
		foreach (VehicleTurret turret in turrets)
		{
			if (turret.deployment != DeploymentType.None)
			{
				if (TurretTargeter.Turret == turret)
					TurretTargeter.Instance.StopTargeting(true);

				if (!Mathf.Approximately(turret.TurretRotation, turret.defaultAngleRotated))
				{
					turret.SetTarget(LocalTargetInfo.Invalid);
					turret.FlagForAlignment();
					turret.StartTicking();
				}
			}
		}
	}

	public void SetQuotaLevel(VehicleTurret turret, int level)
	{
		turretQuotas[turret] = level;
		RecacheTurretAutoLoading(turret);
	}

	public int GetQuotaLevel(VehicleTurret turret)
	{
		if (!turretQuotas.TryGetValue(turret, out int count))
		{
			count = Mathf.CeilToInt(turret.def.autoRefuelProportion *
				turret.def.magazineCapacity *
				turret.def.chargePerAmmoCount);
		}
		return count;
	}

	public VehicleTurret GetTurret(string key)
	{
		foreach (VehicleTurret turret in turrets)
		{
			if (turret.key == key)
				return turret;
		}
		return null;
	}

	public override void OnDestroy()
	{
		// Cleanup entire turret list
		foreach (VehicleTurret turret in turrets)
		{
			turret.OnDestroy();
		}
	}

	public override void PostLoad()
	{
		turrets ??= [];
		RecacheTurretPermissions();
		foreach (VehicleTurret turret in turrets)
		{
			turret.PostPostLoadInit();
		}
	}

	private void RecacheGizmos()
	{
		turretGizmos.Clear();

		if (CanDeploy)
		{
			deployToggle = new Command_Toggle
			{
				toggleAction = delegate
				{
					Vehicle.jobs.StartJob(new Job(JobDefOf_Vehicles.DeployVehicle, targetA: Vehicle),
						JobCondition.InterruptForced);
					deployTicks = DeployTicks;
				},
				isActive = () => Deployed,
			};
		}
		HashSet<string> gizmoGroups = [];
		foreach (VehicleTurret turret in turrets)
		{
			switch (turret.def.turretType)
			{
				case TurretType.Rotatable:
				{
					if (!turret.manualTargeting)
						continue;
					turretGizmos.Add(GetRotatableTurretGizmo(turret));
				}
				break;
				case TurretType.Static:
				{
					if (turret.groupKey.NullOrEmpty() || !gizmoGroups.Contains(turret.groupKey))
					{
						turretGizmos.Add(GetStaticTurretGizmo(turret));
					}
				}
				break;
				default:
					throw new NotImplementedException(nameof(TurretType));
			}
		}
	}

	private Command_Turret GetStaticTurretGizmo(VehicleTurret turret)
	{
		Command_CooldownAction command = new()
		{
			vehicle = Vehicle,
			turret = turret,
			defaultLabel = !string.IsNullOrEmpty(turret.gizmoLabel) ?
				turret.gizmoLabel :
				turret.def.LabelCap,
			icon = turret.GizmoIcon,
			iconDrawScale = turret.def.gizmoIconScale,
			canReload = turrets.All(t => t.def.ammunition != null)
		};
		if (!string.IsNullOrEmpty(turret.def.gizmoDescription))
			command.defaultDesc = turret.def.gizmoDescription;
		return command;
	}

	private Command_Turret GetRotatableTurretGizmo(VehicleTurret turret)
	{
		Command_TargeterCooldownAction command =
			new()
			{
				vehicle = Vehicle,
				turret = turret,
				defaultLabel = !string.IsNullOrEmpty(turret.gizmoLabel) ?
					turret.gizmoLabel :
					turret.def.LabelCap,
				icon = turret.GizmoIcon,
				iconDrawScale = turret.def.gizmoIconScale
			};
		if (!string.IsNullOrEmpty(turret.def.gizmoDescription))
		{
			command.defaultDesc = turret.def.gizmoDescription;
		}

		command.targetingParams = new TargetingParameters
		{
			//Buildings, Things, Animals, Humans, and Mechs default to targetable
			canTargetLocations = true
		};
		return command;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (Vehicle.Faction != Faction.OfPlayer && !DebugSettings.ShowDevGizmos)
			yield break; // Don't return any gizmos if belonging to another faction

		bool upgrading = Vehicle.CompUpgradeTree is { Upgrading: true };

		if (CanDeploy)
		{
			deployToggle.Disabled = false;
			deployToggle.disabledReason = null;
			deployToggle.icon = Deployed ? VehicleTex.UndeployVehicle : VehicleTex.DeployVehicle;
			deployToggle.defaultLabel = Deployed ? "VF_Undeploy".Translate() : "VF_Deploy".Translate();
			deployToggle.defaultDesc = "VF_DeployDescription".Translate();
			if (!Vehicle.CanMoveFinal || Deploying || Vehicle.vehiclePather.Moving)
				deployToggle.Disable();
			if (upgrading)
				deployToggle.Disable("VF_DisabledByVehicleUpgrading".Translate(Vehicle.LabelCap));
			yield return deployToggle;
		}

		foreach (Command_Turret command in turretGizmos)
		{
			command.Disabled = false;
			command.disabledReason = null;

			VehicleTurret turret = command.turret;
			foreach (VehicleRoleHandler relatedHandler in Vehicle.GetHandlers(HandlingType.Turret)
			 .Where(handler => handler.role.TurretIds.NotNullAndContains(turret.key)))
			{
				if (relatedHandler.thingOwner.Count < relatedHandler.role.SlotsToOperate &&
					!VehicleMod.settings.debug.debugShootAnyTurret)
				{
					command.Disable("VF_NotEnoughCrew".Translate(Vehicle.LabelShort,
						relatedHandler.role.label));
					break;
				}
			}

			// Verify disable conditions
			if (turret.IsDisabled(out string disableReason))
				command.Disable(disableReason);
			if (upgrading)
				command.Disable("VF_DisabledByVehicleUpgrading".Translate(Vehicle.LabelCap));

			yield return command;

			if (DebugSettings.ShowDevGizmos)
			{
				yield return new Command_Action
				{
					defaultLabel = $"Full Refill: {turret.gizmoLabel}",
					action = delegate { DevModeReloadTurret(turret); }
				};
			}
		}
	}

	public override AcceptanceReport CanMove(FloatMenuContext context)
	{
		if (Deploying || Deployed)
			return "VF_VehicleImmobileDeployed".Translate(Vehicle);
		return true;
	}

	public override AcceptanceReport CanDraft()
	{
		if (Deploying)
			return "VF_VehicleUnableToMove".Translate(Vehicle);
		return true;
	}

	public void QueueTicker(VehicleTurret turret)
	{
		if (!tickers.Contains(turret))
		{
			tickers.Add(turret);
			StartTicking();
		}
	}

	public void DequeueTicker(VehicleTurret turret)
	{
		tickers.Remove(turret);
		if (ShouldStopTicking)
		{
			StopTicking();
		}
	}

	public void QueueTurret(TurretData turretData)
	{
		turretData.turret.queuedToFire = true;
		turretQueue.Add(turretData);
		turretData.turret.EventRegistry[VehicleTurretEventDefOf.Queued].ExecuteEvents();
	}

	public void DequeueTurret(TurretData turretData)
	{
		turretData.turret.queuedToFire = false;
		turretQueue.RemoveAll(td => td.turret == turretData.turret);
		turretData.turret.EventRegistry[VehicleTurretEventDefOf.Dequeued].ExecuteEvents();
	}

	private void ResolveTurretQueue()
	{
		for (int i = turretQueue.Count - 1; i >= 0; i--)
		{
			TurretData turretData = turretQueue[i];
			try
			{
				VehicleTurret turret = turretData.turret;
				if (!turret.targetInfo.IsValid || !turret.HasAmmo)
				{
					DequeueTurret(turretData);
					continue;
				}

				if (!turretData.CanTarget)
				{
					turret.SetTarget(LocalTargetInfo.Invalid);
					DequeueTurret(turretData);
					continue;
				}

				turret.AlignToTargetRestricted();
				if (turretData.ticksTillShot <= 0)
				{
					turret.FireTurret();
					turret.CurrentTurretFiring++;
					turretData.shots--;
					turretData.ticksTillShot = turret.TicksPerShot;

					if (turret.OnCooldown || turretData.shots == 0 || !turret.HasAmmo)
					{
						if (turret.targetPersists)
						{
							turret.CheckTargetInvalid();
						}
						else
						{
							if (turret.targetInfo.Thing is { } thing)
							{
								if ((turret.targeting & TargetLock.Thing) == 0 ||
									thing is Pawn && (turret.targeting & TargetLock.Pawn) == 0)
								{
									turret.SetTarget(LocalTargetInfo.Invalid);
								}
							}
							else if ((turret.targeting & TargetLock.Cell) == 0)
							{
								turret.SetTarget(LocalTargetInfo.Invalid);
							}
						}

						if (!turret.HasAmmo)
						{
							turret.Reload();
							// If unable to auto reload, invalidate target to reset turret state to unused.
							if (!turret.HasAmmo)
								turret.SetTarget(LocalTargetInfo.Invalid);
						}

						DequeueTurret(turretData);
					}
				}
				else
				{
					turretData.ticksTillShot--;
				}
			}
			catch (Exception ex)
			{
				turretData.turret.SetTarget(LocalTargetInfo.Invalid);
				DequeueTurret(turretData);
				Log.Error($"Exception thrown while shooting turret {turretData.turret}. Removing from " +
					$"queue to resolve issue temporarily.{Environment.NewLine}Exception={ex}");
			}
		}
	}

	private void DevModeReloadTurret(VehicleTurret turret)
	{
		if (turret.def.ammunition is null)
		{
			turret.Reload();
		}
		else if (turret.def.ammunition.AllowedThingDefs.FirstOrDefault() is { } thingDef)
		{
			Thing ammo = ThingMaker.MakeThing(thingDef);

			// Limit to vehicle's cargo capacity to avoid stack limit mods
			// adding hundreds or thousands at a time.
			float capacity = Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
			float massLeft = capacity - MassUtility.InventoryMass(Vehicle);
			float thingMass = thingDef.GetStatValueAbstract(StatDefOf.Mass);
			int countTillOverEncumbered = Mathf.CeilToInt(massLeft / thingMass);
			ammo.stackCount = Mathf.Min(thingDef.stackLimit, countTillOverEncumbered);

			Vehicle.AddOrTransfer(ammo);
			turret.Reload(thingDef);
		}
		else
		{
			Log.Error(
				$"Unable to reload {turret} through DevMode, no AllowedThingDefs in ammunition list.");
		}
	}

	[Profile]
	public override void CompTick()
	{
		base.CompTick();
		if (Vehicle.Spawned)
		{
			ResolveTurretQueue();
			// Only tick VehicleTurrets that have requested ticking
			for (int i = tickers.Count - 1; i >= 0; i--)
			{
				VehicleTurret turret = tickers[i];
				if (Vehicle.stances.stunner.Stunned && turret.def.empDisables)
					continue;

				// VehicleTurret::Tick determines when it should be removed from
				// the ticker queue.
				if (!turret.Tick())
					DequeueTicker(turret);
			}

			if (ShouldStopTicking)
			{
				StopTicking();
			}
		}
	}

	public override void AIAutoCheck()
	{
		foreach (VehicleTurret cannon in turrets)
		{
			if (cannon.shellCount < Mathf.CeilToInt(cannon.def.magazineCapacity / 4f) &&
				(!cannon.TargetLocked || cannon.shellCount <= 0))
			{
				cannon.AutoReload();
			}
		}
	}

	public override bool IsThreat(IAttackTargetSearcher searcher)
	{
		if (!turrets.NullOrEmpty())
		{
			foreach (VehicleTurret turret in turrets)
			{
				if (!turret.TurretDisabled)
					return true;
			}
		}
		return false;
	}

	public void ToggleDeployment()
	{
		deployed = !deployed;
		deployTicks = 0;

		if (deployed)
		{
			Props.deploySound?.PlayOneShot(Vehicle);
			Vehicle.EventRegistry[VehicleEventDefOf.Deployed].ExecuteEvents();
		}
		else
		{
			Props.undeploySound?.PlayOneShot(Vehicle);
			Vehicle.EventRegistry[VehicleEventDefOf.Undeployed].ExecuteEvents();
		}
	}

	public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
	{
		base.PostDeSpawn(map, mode: mode);
		for (int i = tickers.Count - 1; i >= 0; i--)
		{
			VehicleTurret turret = tickers[i];
			DequeueTicker(turret); // Dequeue all turrets if vehicle despawns
		}
	}

	public override void PostGeneration()
	{
		CreateTurretInstances();
		if (Vehicle.Faction != Faction.OfPlayer)
		{
			FillMagazineCapacity();
		}
	}

	public override void Notify_ColorChanged()
	{
		foreach (VehicleTurret turret in turrets)
		{
			turret.ResolveGraphics(Vehicle.patternData, true);
		}
	}

	public override void EventRegistration()
	{
		Vehicle.AddEvent(VehicleEventDefOf.PawnEntered, RecacheTurretPermissions, ReloadAllTurretsIfEmpty);
		Vehicle.AddEvent(VehicleEventDefOf.PawnExited, RecacheTurretPermissions);
		Vehicle.AddEvent(VehicleEventDefOf.CargoAdded, ReloadAllTurretsIfEmpty);
		Vehicle.AddEvent(VehicleEventDefOf.PawnChangedSeats, RecacheTurretPermissions, ReloadAllTurretsIfEmpty);
		Vehicle.AddEvent(VehicleEventDefOf.PawnKilled, RecacheTurretPermissions);
		Vehicle.AddEvent(VehicleEventDefOf.PawnCapacitiesDirty, RecacheTurretPermissions);

		Vehicle.inventory.innerContainer.OnContentsChanged += RecacheAllTurretAutoLoading;

		foreach (VehicleTurret turret in turrets)
		{
			RegisterEventsFor(turret);
		}
	}

	private void RegisterEventsFor(VehicleTurret turret)
	{
		turret.FillEventsDef();
		turret.RegisterEvents();
		// TODO Raiders - add stop to shoot AI behavior in a less problematic way. Needs to account for vehicles swapping hands
		//if (Vehicle.VehicleDef.npcProperties is { stopToShoot: true })
		//  turret.AddEvent(VehicleTurretEventDefOf.Queued, Vehicle.vehiclePather.StopDead);
	}

	private void CreateTurretInstances()
	{
		if (!Props.turrets.NullOrEmpty())
		{
			foreach (VehicleTurret turret in Props.turrets)
			{
				try
				{
					CopyAndAddTurret(turret);
				}
				catch (Exception ex)
				{
					Log.Error(
						$"Exception thrown while attempting to generate {turret.def.label} " +
						$"for {Vehicle.Label}. Exception=\"{ex}\"");
				}
			}
			CheckDuplicateKeys();
		}
	}

	internal void CheckDuplicateKeys()
	{
		using ClearOnDispose<string> cod = new(DuplicateKeySet);
		foreach (VehicleTurret turret in turrets)
		{
			if (!DuplicateKeySet.Add(turret.key))
			{
				Log.Warning($"Duplicate VehicleTurret key {turret.key}. These must be unique.");
			}
		}
	}

	/// <summary>
	/// Creates shallow copy of turret reference and adds it to the turret list.
	/// </summary>
	/// <returns>Instance of vehicle turret created from <paramref name="reference"/></returns>
	public VehicleTurret CopyAndAddTurret(VehicleTurret reference, string upgradeKey = null)
	{
		VehicleTurret newTurret =
			(VehicleTurret)Activator.CreateInstance(reference.GetType(), Vehicle, reference);
		SetDefaults(newTurret);
		newTurret.Init(reference);
		AddTurret(newTurret, upgradeKey);
		return newTurret;
	}

	private void SetDefaults(VehicleTurret turret)
	{
		turret.SetTarget(LocalTargetInfo.Invalid);
		turret.ResetAngle();
		RegisterEventsFor(turret);
	}

	// TODO - Can be optimized to only revalidate turret children at the end if multiple turrets are added
	/// <summary>
	/// Adds the turret to the turret list and re-registers turret to renderer if a graphic exists.
	/// </summary>
	public void AddTurret(VehicleTurret turret, string upgradeKey)
	{
		turret.upgradeKey = upgradeKey;
		turrets.Add(turret);
		RevalidateTurrets();
		if (Vehicle.Spawned)
			LongEventHandler.ExecuteWhenFinished(RecacheGizmos);

		turret.loadConfig ??= new AutoLoadConfig(turret);

		// TODO 1.7 - Remove deprecated method of saving quota levels.
		if (!backupQuotas.NullOrEmpty())
		{
			for (int i = 0; i < backupQuotas.Count; i++)
			{
				BackupTurretQuota quota = backupQuotas[i];
				if (quota.key == turret.key && quota.upgradeKey == turret.upgradeKey)
				{
					SetQuotaLevel(turret, quota.config);
					backupQuotas.RemoveAt(i);
					break;
				}
			}
		}

		if (upgradeKey != null && Vehicle.CompUpgradeTree != null &&
			Vehicle.CompUpgradeTree.TryGetStates(turret.key, out List<UpgradeState> states))
		{
			// Re-unlock turret-type settings to ensure proper values are
			// initialized for new turrets of existing keys
			foreach (UpgradeState state in states)
			{
				if (state.settings.NullOrEmpty())
					continue;

				foreach (UpgradeState.Setting setting in state.settings)
				{
					if (setting is UpgradeSetting_Turret turretSetting &&
						turretSetting.turretKey == turret.key)
					{
						turretSetting.Unlocked(Vehicle, false);
					}
				}
			}
		}
		CacheBoundaries();
	}

	public bool RemoveTurret(string key)
	{
		for (int i = turrets.Count - 1; i >= 0; i--)
		{
			VehicleTurret turret = turrets[i];
			if (turret.key == key)
			{
				return RemoveTurret(turret);
			}
		}
		return false;
	}

	public bool RemoveTurret(VehicleTurret turret)
	{
		turret.TryClearChamber();
		if (turretQuotas.TryGetValue(turret, out int quota))
		{
			// Move turret quota to simple list for storage,
			// may pull config later if turret is re-added
			backupQuotas.Add(new BackupTurretQuota()
			{
				key = turret.key,
				upgradeKey = turret.upgradeKey,
				config = quota,
			});
			turretQuotas.Remove(turret);
		}
		turret.OnDestroy();
		bool removed = turrets.Remove(turret);
		TryDeregisterRenderer(turret);
		if (Vehicle.Spawned)
			LongEventHandler.ExecuteWhenFinished(RecacheGizmos);
		return removed;
	}

	private void TryRegisterRenderer(VehicleTurret turret)
	{
		// Parent turrets take ownership for rendering child turrets so transform data can be properly passed down and
		// inherited in the final render results.
		if (!turret.NoGraphic && turret.attachedTo is null)
			Vehicle.DrawTracker.AddRenderer(turret);
	}

	private void TryDeregisterRenderer(VehicleTurret turret)
	{
		// If attachedTo is somehow null at the time of registering, we still need to ensure no references are left behind
		// otherwise the renderer will be there forever until the game is reloaded.
		if (!turret.NoGraphic)
			Vehicle.DrawTracker.RemoveRenderer(turret);
	}

	public void RevalidateTurrets()
	{
		ResolveAllTurretChildren();
		RecacheDeployment();
		RecacheTurretPermissions();
	}

	private void ResolveAllTurretChildren()
	{
		// Break all connections, then reassign. Out of sequence parent and renderer registration can result in child 
		// turrets being registered as renderers, even though they're drawn by their parents.
		foreach (VehicleTurret turret in turrets)
		{
			turret.attachedTo = null;
			turret.childTurrets.Clear();
			TryDeregisterRenderer(turret);
			ResolveTurretChildren(turret);
		}
		foreach (VehicleTurret turret in turrets)
		{
			TryRegisterRenderer(turret);
		}
	}

	private void ResolveTurretChildren(VehicleTurret turret)
	{
		turret.childTurrets = [];
		if (!string.IsNullOrEmpty(turret.parentKey))
		{
			foreach (VehicleTurret parentTurret in turrets)
			{
				if (parentTurret.key == turret.parentKey)
				{
					turret.attachedTo = parentTurret;
					if (parentTurret.attachedTo == turret || turret == parentTurret)
					{
						Log.Error($"Recursive turret attachments detected, this is not allowed. " +
							$"Disconnecting turret from parent.");
						turret.attachedTo = null;
						continue;
					}
					parentTurret.childTurrets.Add(turret);
				}
			}
		}
	}

	private void InitTurrets()
	{
		for (int i = turrets.Count - 1; i >= 0; i--)
		{
			VehicleTurret turret = turrets[i];
			VehicleTurret reference = FindTurretReference(turret);

			if (reference != null)
			{
				InitTurret(turret, reference);
			}
			else
			{
				Log.Error(
					$"Unable to find reference turret for key {turret.key}. Turrets can only be added if " +
					$"they exist in the vehicle's upgrade tree or in its initial turrets.");
				turrets.Remove(turret); //Remove from turret list, invalid turret will throw exceptions
			}
		}
	}

	private VehicleTurret FindTurretReference(VehicleTurret turret)
	{
		if (!turret.upgradeKey.NullOrEmpty() && Vehicle.CompUpgradeTree != null)
		{
			foreach (UpgradeNode upgradeNode in Vehicle.CompUpgradeTree.Props.def.nodes)
			{
				if (!upgradeNode.upgrades.NullOrEmpty())
				{
					foreach (Upgrade upgrade in upgradeNode.upgrades)
					{
						if (upgrade is TurretUpgrade turretUpgrade)
						{
							if (MatchingTurret(turret.key, turretUpgrade.turrets,
								out VehicleTurret upgradeResult))
							{
								return upgradeResult;
							}
						}
					}
				}
			}
		}

		if (MatchingTurret(turret.key, Props.turrets, out VehicleTurret result))
		{
			return result;
		}
		else if (Vehicle.CompUpgradeTree != null)
		{
			Log.Warning($"Unable to locate {turret.key} in CompProperties with null upgradeKey. " +
				$"Sweeping UpgradeTree for any matching turret.");
			foreach (UpgradeNode upgradeNode in Vehicle.CompUpgradeTree.Props.def.nodes)
			{
				if (!upgradeNode.upgrades.NullOrEmpty())
				{
					foreach (Upgrade upgrade in upgradeNode.upgrades)
					{
						if (upgrade is TurretUpgrade turretUpgrade)
						{
							if (MatchingTurret(turret.key, turretUpgrade.turrets,
								out VehicleTurret upgradeResult))
							{
								return upgradeResult;
							}
						}
					}
				}
			}
		}

		return null;
	}

	private static bool MatchingTurret(string key, List<VehicleTurret> turrets,
		out VehicleTurret result)
	{
		result = null;
		if (turrets.NullOrEmpty())
		{
			return false;
		}

		foreach (VehicleTurret turret in turrets)
		{
			if (turret.key == key)
			{
				result = turret;
				return true;
			}
		}

		return false;
	}

	private void InitTurret(VehicleTurret turret, VehicleTurret reference)
	{
		turret.Init(reference);
		ResolveTurretChildren(turret);
		QueueTicker(turret); // Queue all turrets initially, will be sorted out after 1st tick
	}

	private void FillMagazineCapacity()
	{
		foreach (VehicleTurret turret in turrets)
		{
			turret.SetMagazineCount(int.MaxValue);
		}
	}

	private void CacheBoundaries()
	{
		if (!turrets.NullOrEmpty())
		{
			MinRange = turrets.Min(turret => turret.MinRange);
			MaxRange = turrets.Min(turret => turret.MaxRange);
		}
	}

	public void RecacheDeployment()
	{
		CanDeploy = SettingsCache.TryGetValue(Vehicle.VehicleDef,
			typeof(CompProperties_VehicleTurrets),
			nameof(CompProperties_VehicleTurrets.deployTime), Props.deployTime) > 0;
	}

	public void RecacheTurretPermissions()
	{
		foreach (VehicleTurret turret in turrets)
		{
			turret.RecacheMannedStatus();
		}
	}

	public void ReloadAllTurretsIfEmpty()
	{
		foreach (VehicleTurret turret in turrets)
		{
			turret.ReloadIfEmpty();
		}
	}

	private void RecacheTurretComponents()
	{
		foreach (VehicleTurret turret in turrets)
		{
			turret.component?.RecacheComponent(Vehicle);
		}
	}

	private void RecacheAllTurretAutoLoading()
	{
		foreach (VehicleTurret turret in turrets)
		{
			RecacheTurretAutoLoading(turret);
		}
	}

	private void RecacheTurretAutoLoading(VehicleTurret turret)
	{
		if (!Vehicle.Spawned)
			return;

		int quota = turretQuotas.TryGetValue(turret);
		if (quota <= 0 || QuotaInCargo(Vehicle, turret) >= quota)
		{
			Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle,
				ReservationType.LoadTurret);
		}
		else
		{
			Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(Vehicle,
				ReservationType.LoadTurret);
		}
		return;

		static int QuotaInCargo(VehiclePawn vehicle, VehicleTurret turret)
		{
			int count = 0;
			foreach (Thing thing in vehicle.inventory.innerContainer)
			{
				if (turret.def.ammunition != null && turret.def.ammunition.Allows(thing) ||
					turret.def.genericAmmo && turret.def.projectile == thing.def)
				{
					count += thing.stackCount;
				}
			}
			return count;
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		try
		{
			RevalidateTurrets();
			RecacheTurretComponents();

			foreach (VehicleTurret turret in turrets)
			{
				turret.PostSpawnSetup(respawningAfterLoad);
			}

			CacheBoundaries();
			if (!respawningAfterLoad)
			{
				foreach (VehicleTurret turret in turrets)
				{
					SetQuotaLevel(turret, GetQuotaLevel(turret)); //Stores default quota level
				}
			}
			LongEventHandler.ExecuteWhenFinished(RecacheGizmos);
		}
		catch (Exception ex)
		{
			Log.Error(
				$"Exception caught while initializing turrets in PostSpawnSetup.\nException={ex}");
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref deployed, nameof(deployed));
		Scribe_Values.Look(ref deployTicks, nameof(deployTicks));
		Scribe_Collections.Look(ref turrets, nameof(turrets), LookMode.Deep, ctorArgs: Vehicle);
		Scribe_Collections.Look(ref turretQueue, nameof(turretQueue), LookMode.Reference);
		Scribe_Collections.Look(ref turretQuotas, nameof(turretQuotas), LookMode.Reference,
			LookMode.Value, ref tmpListTurrets, ref tmpListTurretQuota);
		Scribe_Collections.Look(ref backupQuotas, nameof(backupQuotas), LookMode.Deep);

		turrets ??= [];
		turretQueue ??= [];
		turretQuotas ??= [];
		backupQuotas ??= [];

		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			InitTurrets();
			LongEventHandler.ExecuteWhenFinished(RecacheAllTurretAutoLoading);
		}
	}

	public class TurretData : IExposable
	{
		public int shots;
		public int ticksTillShot;
		public VehicleTurret turret;

		public TurretData()
		{
		}

		public TurretData(int shots, int ticksTillShot, VehicleTurret turret)
		{
			this.shots = shots;
			this.ticksTillShot = ticksTillShot;
			this.turret = turret;
		}

		public bool CanTarget
		{
			get
			{
				if (turret.TurretRestricted)
					return false;
				if (turret.OnCooldown)
					return false;

				if (!turret.IsManned)
				{
					return VehicleMod.settings.debug.debugShootAnyTurret &&
						turret.vehicle.Faction.IsPlayerSafe();
				}

				return true;
			}
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref shots, nameof(shots));
			Scribe_Values.Look(ref ticksTillShot, nameof(ticksTillShot));
			Scribe_References.Look(ref turret, nameof(turret));
		}
	}

	/// <summary>
	/// Used for serializing turret quotas of turrets that were removed, 
	/// such that they will reload the quota if re-added
	/// </summary>
	[UsedImplicitly]
	public struct BackupTurretQuota : IExposable
	{
		public string key;
		public string upgradeKey;
		public int config;

		public void ExposeData()
		{
			Scribe_Values.Look(ref key, nameof(key));
			Scribe_Values.Look(ref upgradeKey, nameof(upgradeKey));
			Scribe_Values.Look(ref config, nameof(config));
		}
	}
}