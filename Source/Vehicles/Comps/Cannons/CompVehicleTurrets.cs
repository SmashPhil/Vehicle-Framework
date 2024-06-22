using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	[HeaderTitle(Label = nameof(CompVehicleTurrets))]
	public class CompVehicleTurrets : VehicleAIComp, IRefundable
	{
		private List<TurretData> turretQueue = new List<TurretData>();

		private bool deployed;
		internal int deployTicks;

		private Dictionary<VehicleTurret, int> turretQuotas = new Dictionary<VehicleTurret, int>();
		private List<BackupTurretQuota> backupQuotas = new List<BackupTurretQuota>();

		[TweakField]
		public List<VehicleTurret> turrets = new List<VehicleTurret>();

		private List<VehicleTurret> tickers = new List<VehicleTurret>();

		private List<VehicleTurret> tmpListTurrets = new List<VehicleTurret>();
		private List<int> tmpListTurretQuota = new List<int>();

		public bool CanDeploy { get; private set; }

		public bool Deployed => deployed;

		public int DeployTicks => Mathf.RoundToInt(SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleTurrets), nameof(CompProperties_VehicleTurrets.deployTime), Props.deployTime) * 60);

		public bool ShouldStopTicking => tickers.Count == 0;

		public CompProperties_VehicleTurrets Props => (CompProperties_VehicleTurrets)props;

		public float MinRange => turrets.Max(x => x.turretDef.minRange);

		public IEnumerable<(ThingDef thingDef, float count)> Refunds
		{
			get
			{
				foreach (VehicleTurret turret in turrets)
				{
					yield return (turret.loadedAmmo, turret.shellCount * turret.turretDef.chargePerAmmoCount);
				}
			}
		}

		public bool TurretsAligned
		{
			get
			{
				foreach (VehicleTurret turret in turrets)
				{
					bool alignTurret = (turret.deployment == DeploymentType.Deployed && Deployed) || (turret.deployment == DeploymentType.Undeployed && !Deployed);
					if (alignTurret && !turret.RotationAligned)
					{
						return false;
					}
				}
				return true;
			}
		}

		public float MaxRangeGrouped
		{
			get
			{
				IEnumerable<VehicleTurret> cannonRange = turrets.Where(x => x.turretDef.maxRange <= GenRadial.MaxRadialPatternRadius);
				if (!cannonRange.NotNullAndAny())
				{
					return (float)Math.Floor(GenRadial.MaxRadialPatternRadius);
				}
				return cannonRange.Min(x => x.turretDef.maxRange);
			}
		}

		public void FlagAllTurretsForAlignment()
		{
			foreach (VehicleTurret turret in turrets)
			{
				if (turret.deployment != DeploymentType.None)
				{
					if (TurretTargeter.Turret == turret)
					{
						TurretTargeter.Instance.StopTargeting(true);
					}
					if (turret.TurretRotation != turret.defaultAngleRotated)
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
			if (turretQuotas[turret] <= 0)
			{
				Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.LoadTurret);
			}
			else
			{
				Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(Vehicle, ReservationType.LoadTurret);
			}
		}

		public int GetQuotaLevel(VehicleTurret turret)
		{
			if (!turretQuotas.TryGetValue(turret, out int count))
			{
				count = Mathf.CeilToInt(turret.turretDef.autoRefuelProportion * turret.turretDef.magazineCapacity * turret.turretDef.chargePerAmmoCount);
			}
			return count;
		}

		public bool GetTurretToFill(out VehicleTurret turretToFill, out int quota)
		{
			turretToFill = null;
			quota = 0;
			if (!turrets.NullOrEmpty())
			{
				int massAvailable = Mathf.RoundToInt(Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity) - MassUtility.InventoryMass(Vehicle));
				foreach (VehicleTurret turret in turrets)
				{
					ThingDef reloadDef = turret.loadedAmmo;
					reloadDef ??= turret.turretDef.ammunition?.AllowedThingDefs.FirstOrDefault();
					if (reloadDef != null)
					{
						int desiredCount = GetQuotaLevel(turret);
						int maxCount = Mathf.RoundToInt(massAvailable / Mathf.Max(reloadDef.GetStatValueAbstract(StatDefOf.Mass), 0.1f));
						int existingCount = Vehicle.inventory.Count(reloadDef);
						int availableCount = desiredCount - existingCount;
						if (availableCount > 0)
						{
							turretToFill = turret;
							quota = Mathf.Min(maxCount, availableCount);
							return true;
						}
					}
				}
			}
			return false;
		}

		public VehicleTurret GetTurret(string key)
		{
			foreach (VehicleTurret turret in turrets)
			{
				if (turret.key == key)
				{
					return turret;
				}
			}
			return null;
		}

		public void AddTurret(VehicleTurret turret, string upgradeKey = null)
		{
			VehicleTurret newTurret = CreateTurret(Vehicle, turret, upgradeKey: upgradeKey);
			newTurret.FillEvents_Def();
			turrets.Add(newTurret);
			RevalidateTurrets();

			if (!backupQuotas.NullOrEmpty())
			{
				for (int i = 0; i < backupQuotas.Count; i++)
				{
					BackupTurretQuota quota = backupQuotas[i];
					if (quota.key == newTurret.key && quota.upgradeKey == newTurret.upgradeKey)
					{
						SetQuotaLevel(newTurret, quota.config);
						backupQuotas.RemoveAt(i);
						break;
					}
				}
			}

			if (Vehicle.CompUpgradeTree != null && Vehicle.CompUpgradeTree.TryGetStates(turret.key, out List<UpgradeState> states))
			{
				//Re-unlock turret-type settings to ensure proper values are initialized for new turrets of existing keys
				foreach (UpgradeState state in states)
				{
					if (!state.settings.NullOrEmpty())
					{
						foreach (UpgradeState.Setting setting in state.settings)
						{
							if (setting is UpgradeSetting_Turret turretSetting && turretSetting.turretKey == turret.key)
							{
								turretSetting.Unlocked(Vehicle, false);
							}
						}
					}
				}
			}
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
			turret.TryRemoveShell();
			if (turretQuotas.TryGetValue(turret, out int quota))
			{
				//Move turret quota to simple list for storage, may pull config later if turret is re-added
				backupQuotas.Add(new BackupTurretQuota()
				{
					key = turret.key,
					upgradeKey = turret.upgradeKey,
					config = quota,
				});
				turretQuotas.Remove(turret);
			}
			turret.OnDestroy();
			return turrets.Remove(turret);
		}

		public override void OnDestroy()
		{
			foreach (VehicleTurret turret in turrets) //Cleanup entire turret list
			{
				turret.OnDestroy();
			}
		}

		public override void PostLoad()
		{
			turrets ??= new List<VehicleTurret>();
			RecacheTurretPermissions();
		}

		public override void PostDraw()
		{
			for (int i = 0; i < turrets.Count; i++)
			{
				turrets[i].Draw();
			}
		}

		public override void PostDrawUnspawned(Vector3 drawPos, Rot8 rot, float rotation)
		{
			for (int i = 0; i < turrets.Count; i++)
			{
				turrets[i].DrawAt(drawPos, rot);
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (Vehicle.Faction != Faction.OfPlayer && !DebugSettings.ShowDevGizmos)
			{
				yield break; //Don't return any gizmos if belonging to another faction
			}

			bool upgrading = Vehicle.CompUpgradeTree != null && Vehicle.CompUpgradeTree.Upgrading;
			
			if (CanDeploy)
			{
				Command_Toggle deployToggle = new Command_Toggle
				{
					icon = Deployed ? VehicleTex.UndeployVehicle : VehicleTex.DeployVehicle,
					defaultLabel = Deployed ? "VF_Undeploy".Translate() : "VF_Deploy".Translate(),
					defaultDesc = "VF_DeployDescription".Translate(),
					toggleAction = delegate ()
					{
						Vehicle.jobs.StartJob(new Job(JobDefOf_Vehicles.DeployVehicle, targetA: Vehicle), JobCondition.InterruptForced);
						deployTicks = DeployTicks;
						
					},
					isActive = () => Deployed
				};
				if (!Vehicle.CanMoveFinal)
				{
					deployToggle.Disable();
				}
				if (Vehicle.Deploying)
				{
					deployToggle.Disable();
				}
				if (Vehicle.vehiclePather.Moving)
				{
					deployToggle.Disable();
				}
				if (upgrading)
				{
					deployToggle.Disable("VF_DisabledByVehicleUpgrading".Translate(Vehicle.LabelCap));
				}
				yield return deployToggle;
			}
			if (turrets.Count > 0)
			{
				int turretNumber = 0;
				var rotatables = turrets.Where(x => x.turretDef.turretType == TurretType.Rotatable);
				var statics = turrets.Where(x => x.turretDef.turretType == TurretType.Static);
				if (rotatables.NotNullAndAny())
				{
					foreach (VehicleTurret turret in rotatables)
					{
						if (turret.manualTargeting)
						{
							Command_TargeterCooldownAction turretTargeterGizmo = new Command_TargeterCooldownAction
							{
								vehicle = Vehicle,
								turret = turret,
								defaultLabel = !string.IsNullOrEmpty(turret.gizmoLabel) ? turret.gizmoLabel : $"{turret.turretDef.LabelCap} {turretNumber}",
								icon = turret.GizmoIcon,
								iconDrawScale = turret.turretDef.gizmoIconScale
							};
							if (!string.IsNullOrEmpty(turret.turretDef.gizmoDescription))
							{
								turretTargeterGizmo.defaultDesc = turret.turretDef.gizmoDescription;
							}
							turretTargeterGizmo.targetingParams = new TargetingParameters
							{
								//Buildings, Things, Animals, Humans, and Mechs default to targetable
								canTargetLocations = true
							};
							turretNumber++;
							foreach (VehicleHandler relatedHandler in Vehicle.GetAllHandlersMatch(HandlingTypeFlags.Turret, turret.key))
							{
								if (relatedHandler.handlers.Count < relatedHandler.role.SlotsToOperate && !VehicleMod.settings.debug.debugShootAnyTurret)
								{
									turretTargeterGizmo.Disable("VF_NotEnoughCrew".Translate(Vehicle.LabelShort, relatedHandler.role.label));
									break;
								}
							}
							if (turret.TurretRestricted)
							{
								turretTargeterGizmo.Disable(turret.restrictions.DisableReason);
							}
							if (!turret.DeploymentSatisfied)
							{
								turretTargeterGizmo.Disable(turret.DeploymentDisabledReason);
							}
							if (turret.ComponentDisabled)
							{
								turretTargeterGizmo.Disable("VF_TurretComponentDisabled".Translate(turret.component.Label));
							}
							if (upgrading)
							{
								turretTargeterGizmo.Disable("VF_DisabledByVehicleUpgrading".Translate(Vehicle.LabelCap));
							}
							yield return turretTargeterGizmo;
						}
						if (DebugSettings.ShowDevGizmos)
						{
							yield return new Command_Action()
							{
								defaultLabel = $"Full Refill: {turret.gizmoLabel}",
								action = delegate()
								{
									DevModeReloadTurret(turret);
								}
							};
						}
					}
				}
				if (statics.NotNullAndAny())
				{
					Dictionary<string, Command_CooldownAction> gizmoGroups = new Dictionary<string, Command_CooldownAction>();
					foreach (VehicleTurret turret in statics)
					{
						bool newCommand = false;
						if (turret.groupKey.NullOrEmpty() || !gizmoGroups.TryGetValue(turret.groupKey, out Command_CooldownAction turretCommand))
						{
							turretCommand = new Command_CooldownAction()
							{
								vehicle = Vehicle,
								turret = turret,
								defaultLabel = !string.IsNullOrEmpty(turret.gizmoLabel) ? turret.gizmoLabel : $"{turret.turretDef.LabelCap} {turretNumber}",
								icon = turret.GizmoIcon,
								iconDrawScale = turret.turretDef.gizmoIconScale
							};
							turretCommand.canReload = turrets.All(t => t.turretDef.ammunition != null);
							turretNumber++;
							newCommand = true;
							if (!string.IsNullOrEmpty(turret.turretDef.gizmoDescription))
							{
								turretCommand.defaultDesc = turret.turretDef.gizmoDescription;
							}
						}
						
						foreach (VehicleHandler relatedHandler in Vehicle.GetAllHandlersMatch(HandlingTypeFlags.Turret, turret.key))
						{
							if (relatedHandler.handlers.Count < relatedHandler.role.SlotsToOperate && !VehicleMod.settings.debug.debugShootAnyTurret)
							{
								turretCommand.Disable("VF_NotEnoughCrew".Translate(Vehicle.LabelShort, relatedHandler.role.label));
								break;
							}
						}
						if (!turret.DeploymentSatisfied)
						{
							turretCommand.Disable(turret.DeploymentDisabledReason);
						}
						if (turret.ComponentDisabled)
						{
							turretCommand.Disable("VF_TurretComponentDisabled".Translate(turret.component.Label));
						}
						if (newCommand)
						{
							if (upgrading)
							{
								turretCommand.Disable("VF_DisabledByVehicleUpgrading".Translate(Vehicle.LabelCap));
							}
							yield return turretCommand;
							if (!turret.groupKey.NullOrEmpty())
							{
								gizmoGroups.Add(turret.groupKey, turretCommand);
							}
							if (DebugSettings.ShowDevGizmos)
							{
								yield return new Command_Action()
								{
									defaultLabel = $"Full Refill: {turret.gizmoLabel}",
									action = delegate ()
									{
										if (turret.turretDef.ammunition is null)
										{
											turret.ReloadCannon(null);
										}
										else if (turret.turretDef.ammunition?.AllowedThingDefs.FirstOrDefault() is ThingDef thingDef)
										{
											Thing ammo = ThingMaker.MakeThing(thingDef);
											ammo.stackCount = thingDef.stackLimit;
											Vehicle.AddOrTransfer(ammo);
											turret.ReloadCannon(thingDef);
										}
									}
								};
							}
						}
					}
				}
			}
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
					if (!turretData.turret.cannonTarget.IsValid || turretData.turret.shellCount <= 0)
					{
						DequeueTurret(turretData);
						continue;
					}
					if (turretData.turret.TurretRestricted || turretData.turret.OnCooldown || (!turretData.turret.IsManned && !VehicleMod.settings.debug.debugShootAnyTurret))
					{
						turretData.turret.SetTarget(LocalTargetInfo.Invalid);
						DequeueTurret(turretData);
						continue;
					}
					turretQueue[i].turret.AlignToTargetRestricted();
					if (turretQueue[i].ticksTillShot <= 0)
					{
						turretData.turret.FireTurret();
						turretData.turret.CurrentTurretFiring++;
						turretData.shots--;
						turretData.ticksTillShot = turretData.turret.TicksPerShot;

						bool outOfAmmo = turretData.turret.turretDef.ammunition != null && turretData.turret.shellCount <= 0;
						if (turretData.turret.OnCooldown || turretData.shots == 0 || outOfAmmo)
						{
							//If target doesn't persist, immediately set target to invalid
							if (turretData.turret.targetPersists)
							{
								turretData.turret.CheckTargetInvalid();
							}
							else
							{
								if (turretData.turret.cannonTarget.Thing is Thing thing)
								{
									if (thing is Pawn pawn && !turretData.turret.targeting.HasFlag(TargetLock.Pawn))
									{
										turretData.turret.SetTarget(LocalTargetInfo.Invalid);
									}
									else if (!turretData.turret.targeting.HasFlag(TargetLock.Thing))
									{
										turretData.turret.SetTarget(LocalTargetInfo.Invalid);
									}
								}
								else if (!turretData.turret.targeting.HasFlag(TargetLock.Cell))
								{
									turretData.turret.SetTarget(LocalTargetInfo.Invalid);
								}
							}
							if (outOfAmmo)
							{
								turretData.turret.ReloadCannon();
							}
							DequeueTurret(turretData);
							continue;
						}
					}
					else
					{
						turretData.ticksTillShot--;
					}
					turretQueue[i] = turretData;
				}
				catch (Exception ex)
				{
					turretData.turret.SetTarget(LocalTargetInfo.Invalid);
					DequeueTurret(turretData);
					Log.Error($"Exception thrown while shooting turret {turretData.turret}. Removing from queue to resolve issue temporarily.\nException={ex}");
				}
			}
		}

		private void DevModeReloadTurret(VehicleTurret turret)
		{
			if (turret.turretDef.ammunition is null)
			{
				turret.ReloadCannon(null);
			}
			else if (turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault() is ThingDef thingDef)
			{
				Thing ammo = ThingMaker.MakeThing(thingDef);

				//Limit to vehicle's cargo capacity to avoid stack limit mods from adding hundreds or thousands at a time
				float capacity = Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
				float massLeft = capacity - MassUtility.InventoryMass(Vehicle);
				float thingMass = thingDef.GetStatValueAbstract(StatDefOf.Mass);
				int countTillOverEncumbered = Mathf.CeilToInt(massLeft / thingMass);
				ammo.stackCount = Mathf.Min(thingDef.stackLimit, countTillOverEncumbered);

				Vehicle.AddOrTransfer(ammo);
				turret.ReloadCannon(thingDef);
			}
			else
			{
				Log.Error($"Unable to reload {turret} through DevMode, no AllowedThingDefs in ammunition list.");
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			if (Vehicle.Spawned)
			{
				ResolveTurretQueue();
				//Only tick VehicleTurrets that actively request to be ticked
				for (int i = tickers.Count - 1; i >= 0; i--)
				{
					VehicleTurret turret = tickers[i];
					if (!turret.Tick())
					{
						DequeueTicker(turret);
					}
				}

				if (ShouldStopTicking)
				{
					StopTicking();
				}
			}
		}

		public override void AITick()
		{
			base.AITick();
		}

		public override void AIAutoCheck()
		{
			foreach (VehicleTurret cannon in turrets)
			{
				if (cannon.shellCount < Mathf.CeilToInt(cannon.turretDef.magazineCapacity / 4f) && (!cannon.TargetLocked || cannon.shellCount <= 0))
				{
					cannon.AutoReloadCannon();
				}
			}
		}

		public override bool IsThreat(IAttackTargetSearcher searcher)
		{
			if (!turrets.NullOrEmpty())
			{
				foreach (VehicleTurret turret in turrets)
				{
					if (turret.AutoTarget)
					{
						return true;
					}
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

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			for (int i = tickers.Count - 1; i >= 0; i--)
			{
				VehicleTurret turret = tickers[i];
				DequeueTicker(turret); //Dequeue all turrets if vehicle despawns
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
				turret.ResolveCannonGraphics(Vehicle.patternData, true);
			}
		}

		public override void EventRegistration()
		{
			Vehicle.AddEvent(VehicleEventDefOf.PawnEntered, RecacheTurretPermissions);
			Vehicle.AddEvent(VehicleEventDefOf.PawnExited, RecacheTurretPermissions);
			Vehicle.AddEvent(VehicleEventDefOf.PawnChangedSeats, RecacheTurretPermissions);
			Vehicle.AddEvent(VehicleEventDefOf.PawnKilled, RecacheTurretPermissions);
			Vehicle.AddEvent(VehicleEventDefOf.PawnCapacitiesDirty, RecacheTurretPermissions);
			
			foreach (VehicleTurret turret in turrets)
			{
				turret.FillEvents_Def();
			}
		}

		public static VehicleTurret CreateTurret(VehiclePawn vehicle, VehicleTurret reference, string upgradeKey = null)
		{
			VehicleTurret newTurret = (VehicleTurret)Activator.CreateInstance(reference.GetType(), new object[] { vehicle, reference });
			newTurret.SetTarget(LocalTargetInfo.Invalid);
			newTurret.ResetAngle();
			newTurret.upgradeKey = upgradeKey;
			return newTurret;
		}

		private void CreateTurretInstances()
		{
			if (Props.turrets.NotNullAndAny())
			{
				foreach (VehicleTurret turret in Props.turrets)
				{
					try
					{
						VehicleTurret newTurret = CreateTurret(Vehicle, turret);
						turrets.Add(newTurret);
					}
					catch (Exception ex)
					{
						SmashLog.Error($"Exception thrown while attempting to generate <text>{turret.turretDef.label}</text> for <text>{Vehicle.Label}</text>. Exception=\"{ex}\"");
					}
				}
				CheckDuplicateKeys();
			}
		}

		public void CheckDuplicateKeys()
		{
			if (turrets.Select(x => x.key).GroupBy(y => y).NotNullAndAny(key => key.Count() > 1))
			{
				Log.Warning("Duplicate VehicleTurret key has been found. These are intended to be unique.");
			}
		}

		public void RevalidateTurrets()
		{
			turretQueue ??= new List<TurretData>();
			ResolveChildTurrets();
			RecacheDeployment();
			RecacheTurretPermissions();
			InitTurrets();
		}

		public void ResolveChildTurrets()
		{
			foreach (VehicleTurret turret in turrets)
			{
				ResolveChildTurrets(turret);
			}
		}

		public void ResolveChildTurrets(VehicleTurret turret)
		{
			turret.childTurrets = new List<VehicleTurret>();
			if (!string.IsNullOrEmpty(turret.parentKey))
			{
				foreach (VehicleTurret parentTurret in turrets)
				{
					if (parentTurret.key == turret.parentKey)
					{
						turret.attachedTo = parentTurret;
						if (parentTurret.attachedTo == turret || turret == parentTurret)
						{
							Log.Error($"Recursive turret attachments detected, this is not allowed. Disconnecting turret from parent.");
							turret.attachedTo = null;
						}
						else
						{
							parentTurret.childTurrets.Add(turret);
						}
					}
				}
			}
		}

		public void InitTurrets()
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
					Log.Error($"Unable to find reference turret for key {turret.key}. Turrets can only be added if they exist in the vehicle's upgrade tree or in its initial turrets.");
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
								if (MatchingTurret(turret.key, turretUpgrade.turrets, out VehicleTurret upgradeResult))
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
				Log.Warning($"Unable to locate {turret.key} in CompProperties with null upgradeKey. Sweeping UpgradeTree for any matching turret.");
				foreach (UpgradeNode upgradeNode in Vehicle.CompUpgradeTree.Props.def.nodes)
				{
					if (!upgradeNode.upgrades.NullOrEmpty())
					{
						foreach (Upgrade upgrade in upgradeNode.upgrades)
						{
							if (upgrade is TurretUpgrade turretUpgrade)
							{
								if (MatchingTurret(turret.key, turretUpgrade.turrets, out VehicleTurret upgradeResult))
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

		private static bool MatchingTurret(string key, List<VehicleTurret> turrets, out VehicleTurret result)
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
			ResolveChildTurrets(turret);
			QueueTicker(turret); //Queue all turrets initially, will be sorted out after 1st tick
		}

		public void FillMagazineCapacity()
		{
			foreach (VehicleTurret turret in turrets)
			{
				turret.SetMagazineCount(int.MaxValue);
			}
		}

		public void RecacheDeployment()
		{
			CanDeploy = SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleTurrets), nameof(CompProperties_VehicleTurrets.deployTime), Props.deployTime) > 0;
		}

		public void RecacheTurretPermissions()
		{
			foreach (VehicleTurret turret in turrets)
			{
				turret.RecacheMannedStatus();
			}
		}

		private void RecacheTurretComponents()
		{
			foreach (VehicleTurret turret in turrets)
			{
				turret.component?.RecacheComponent(Vehicle);
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);

			string step = "";
			try
			{
				if (!Vehicle.Initialized)
				{
					step = "Revalidating turrets";
					RevalidateTurrets();
				}

				step = "Recaching turret permissions";
				RecacheTurretPermissions();
				RecacheTurretComponents();

				if (!respawningAfterLoad)
				{
					step = "Setting quota levels";
					foreach (VehicleTurret turret in turrets)
					{
						SetQuotaLevel(turret, GetQuotaLevel(turret)); //Stores default quota level
					}
				}
				step = "Done";
			}
			catch (Exception ex)
			{
				Log.Error($"Exception caught while initializing turrets in PostSpawnSetup at step={step} Exception={ex}");
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref deployed, nameof(deployed));
			Scribe_Values.Look(ref deployTicks, nameof(deployTicks));
			Scribe_Collections.Look(ref turrets, nameof(turrets), LookMode.Deep, ctorArgs: Vehicle);
			Scribe_Collections.Look(ref turretQueue, nameof(turretQueue), LookMode.Reference);
			Scribe_Collections.Look(ref turretQuotas, nameof(turretQuotas), LookMode.Reference, LookMode.Value, ref tmpListTurrets, ref tmpListTurretQuota);
			Scribe_Collections.Look(ref backupQuotas, nameof(backupQuotas), LookMode.Deep);

			turretQuotas ??= new Dictionary<VehicleTurret, int>();
			backupQuotas ??= new List<BackupTurretQuota>();
		}

		public struct TurretData : IExposable
		{
			public int shots;
			public int ticksTillShot;
			public VehicleTurret turret;

			public TurretData(int shots, int ticksTillShot, VehicleTurret turret)
			{
				this.shots = shots;
				this.ticksTillShot = ticksTillShot;
				this.turret = turret;
			}

			public void ExposeData()
			{
				Scribe_Values.Look(ref shots, nameof(shots));
				Scribe_Values.Look(ref ticksTillShot, nameof(ticksTillShot));
				Scribe_References.Look(ref turret, nameof(turret));
			}
		}

		/// <summary>
		/// Used for serializing turret quotas of turrets that were removed, such that they will reload the quota if re-added
		/// </summary>
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
}
