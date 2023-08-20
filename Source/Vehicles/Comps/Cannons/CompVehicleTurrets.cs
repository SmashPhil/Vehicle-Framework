using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	[HeaderTitle(Label = nameof(CompVehicleTurrets))]
	public class CompVehicleTurrets : VehicleAIComp, IRefundable
	{
		/// PARAMS => (# Shots Fired, VehicleTurret, tickCount}
		private List<TurretData> turretQueue = new List<TurretData>();

		private Dictionary<VehicleTurret, int> turretQuotas = new Dictionary<VehicleTurret, int>();

		[TweakField]
		public List<VehicleTurret> turrets = new List<VehicleTurret>();

		private List<VehicleTurret> tickers = new List<VehicleTurret>();

		private static List<VehicleTurret> tmpListTurrets = new List<VehicleTurret>();
		private static List<int> tmpListTurretQuota = new List<int>();

		public CompProperties_VehicleTurrets Props => (CompProperties_VehicleTurrets)props;

		public bool WeaponStatusOnline => !Vehicle.Downed && !Vehicle.Dead; //REDO - Add vehicle component health as check

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

		public void AddTurrets(List<VehicleTurret> cannonList)
		{
			if (cannonList.NullOrEmpty())
			{
				return;
			}
			foreach (VehicleTurret turret in cannonList)
			{
				VehicleTurret newTurret = CreateTurret(Vehicle, turret);
				turrets.RemoveAll(t => t.key == newTurret.key);
				turrets.Add(newTurret);
			}
		}

		public void RemoveTurrets(List<VehicleTurret> turrets)
		{
			if (turrets.NullOrEmpty())
			{
				return;
			}
			for (int i = turrets.Count - 1; i >= 0; i--)
			{
				VehicleTurret turret = turrets[i];
				VehicleTurret matchingTurret = turrets.FirstOrDefault(c => c.key == turret.key);
				if (matchingTurret is null)
				{
					Log.Error($"Unable to locate {turret.key} in cannonList for removal. Is Key missing on upgraded cannon?");
				}
				RemoveTurret(matchingTurret);
			}
		}

		public bool RemoveTurret(VehicleTurret turret)
		{
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

		public override void PostDrawUnspawned(Vector3 drawPos, float rotation)
		{
			for (int i = 0; i < turrets.Count; i++)
			{
				turrets[i].DrawAt(drawPos);
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
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
								if (relatedHandler.handlers.Count < relatedHandler.role.slotsToOperate && !VehicleMod.settings.debug.debugShootAnyTurret)
								{
									turretTargeterGizmo.Disable("VF_NotEnoughCrew".Translate(Vehicle.LabelShort, relatedHandler.role.label));
									break;
								}
							}
							if (Vehicle.Faction != Faction.OfPlayer)
							{
								turretTargeterGizmo.Disable("CannotOrderNonControlled".Translate());
							}
							if (turret.TurretRestricted)
							{
								turretTargeterGizmo.Disable(turret.restrictions.DisableReason);
							}
							yield return turretTargeterGizmo;
						}
						if (Prefs.DevMode && DebugSettings.godMode)
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
							if (relatedHandler.handlers.Count < relatedHandler.role.slotsToOperate && !VehicleMod.settings.debug.debugShootAnyTurret)
							{
								turretCommand.Disable("VF_NotEnoughCrew".Translate(Vehicle.LabelShort, relatedHandler.role.label));
								break;
							}
						}
						if (Vehicle.Faction != Faction.OfPlayer)
						{
							turretCommand.Disable("CannotOrderNonControlled".Translate());
						}
						//(bool disabled, string reason) = turret.DisableGizmo;
						//if (disabled)
						//{
						//	turretCommand.Disable(reason);
						//}

						if (newCommand)
						{
							yield return turretCommand;
							if (!turret.groupKey.NullOrEmpty())
							{
								gizmoGroups.Add(turret.groupKey, turretCommand);
							}
							if (Prefs.DevMode && DebugSettings.godMode)
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
			if (tickers.Count == 0)
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
						if (turretData.turret.OnCooldown || turretData.shots == 0 || (turretData.turret.turretDef.ammunition != null && turretData.turret.shellCount <= 0))
						{
							if (turretData.turret.targetPersists)
							{
								turretData.turret.SetTargetConditionalOnThing(LocalTargetInfo.Invalid);
							}
							else
							{
								turretData.turret.SetTarget(LocalTargetInfo.Invalid);
							}
							turretData.turret.ReloadCannon();
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
		}

		public override void AITick()
		{
			base.AITick();
		}

		public override void AIAutoCheck()
		{
			foreach(VehicleTurret cannon in turrets)
			{
				if (cannon.shellCount < Mathf.CeilToInt(cannon.turretDef.magazineCapacity / 4f) && (!cannon.TargetLocked || cannon.shellCount <= 0))
				{
					cannon.AutoReloadCannon();
				}
			}
		}

		public override void PostGeneration()
		{
			CreateTurretInstances();
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

		public static VehicleTurret CreateTurret(VehiclePawn vehicle, VehicleTurret reference)
		{
			VehicleTurret newTurret = (VehicleTurret)Activator.CreateInstance(reference.GetType(), new object[] { vehicle, reference });
			newTurret.SetTarget(LocalTargetInfo.Invalid);
			newTurret.ResetAngle();
			return newTurret;
		}

		private void CreateTurretInstances()
		{
			if (Props.turrets.NotNullAndAny())
			{
				foreach(VehicleTurret turret in Props.turrets)
				{
					try
					{
						VehicleTurret newTurret = CreateTurret(Vehicle, turret);
						turrets.Add(newTurret);
					}
					catch (Exception ex)
					{
						SmashLog.Error($"Exception thrown while attempting to generate <text>{turret.turretDef.label}</text> for <text>{Vehicle.Label}</text>. Exception=\"{ex.Message}\"");
					}
				}
				if (turrets.Select(x => x.key).GroupBy(y => y).NotNullAndAny(key => key.Count() > 1))
				{
					Log.Warning("Duplicate VehicleTurret key has been found. These are intended to be unique.");
				}
			}
		}

		public void RevalidateTurrets()
		{
			turretQueue ??= new List<TurretData>();
			ResolveChildTurrets();
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
				foreach (VehicleTurret parentTurret in turrets.Where(c => c.key == turret.parentKey))
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

		public void InitTurrets()
		{
			for (int i = turrets.Count - 1; i >= 0; i--)
			{
				VehicleTurret turret = turrets[i];
				if (Props.turrets.FirstOrDefault(turretProps => turretProps.key == turret.key) is VehicleTurret turretProps)
				{
					turret.Init(turretProps);
					ResolveChildTurrets(turret);
					QueueTicker(turret); //Queue all turrets initially, will be sorted out after 1st tick
				}
				else
				{
					Log.Error($"Unable to find matching turret from save file to CompProperties based on key {turret.key}. Was this changed or removed?");
					turrets.Remove(turret); //Remove from turret list, invalid turret will throw exceptions
				}
			}
		}

		public void RecacheTurretPermissions()
		{
			foreach (VehicleTurret turret in turrets)
			{
				turret.RecacheMannedStatus();
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);

			string step = "";
			try
			{
				step = "Revalidating turrets";
				RevalidateTurrets();
				step = "Recaching turret permissions";
				RecacheTurretPermissions();

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
			Scribe_Collections.Look(ref turrets, nameof(turrets), LookMode.Deep, ctorArgs: Vehicle);
			Scribe_Collections.Look(ref turretQueue, nameof(turretQueue), LookMode.Reference);
			Scribe_Collections.Look(ref turretQuotas, nameof(turretQuotas), LookMode.Reference, LookMode.Value, ref tmpListTurrets, ref tmpListTurretQuota);

			turretQuotas ??= new Dictionary<VehicleTurret, int>();
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
	}
}
