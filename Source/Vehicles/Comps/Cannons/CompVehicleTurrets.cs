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
	public class CompVehicleTurrets : VehicleAIComp, IRefundable
	{
		/// PARAMS => (# Shots Fired, VehicleTurret, tickCount}
		private List<TurretData> turretQueue = new List<TurretData>();

		public List<VehicleTurret> turrets = new List<VehicleTurret>();

		public CompProperties_VehicleTurrets Props => (CompProperties_VehicleTurrets)props;

		public VehiclePawn Vehicle => parent as VehiclePawn;

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
				if(!cannonRange.NotNullAndAny())
				{
					return (float)Math.Floor(GenRadial.MaxRadialPatternRadius);
				}
				return cannonRange.Min(x => x.turretDef.maxRange);
			}
		}

		public void AddCannons(List<VehicleTurret> cannonList)
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

		public void RemoveCannons(List<VehicleTurret> cannonList)
		{
			if (cannonList.NullOrEmpty())
			{
				return;
			}
			foreach(VehicleTurret cannon in cannonList)
			{
				VehicleTurret resultingHandler = turrets.FirstOrDefault(c => c.key == cannon.key);
				if(resultingHandler is null)
				{
					Log.Error($"Unable to locate {cannon.key} in cannonList for removal. Is Key missing on upgraded cannon?");
				}
				turrets.Remove(resultingHandler);
			}
		}

		public override void PostLoad()
		{
			turrets ??= new List<VehicleTurret>();
		}

		public override void PostDraw()
		{
			turrets.ForEach(c => c.Draw());
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
							foreach (VehicleHandler relatedHandler in Vehicle.GetAllHandlersMatch(HandlingTypeFlags.Cannon, turret.key))
							{
								if (relatedHandler.handlers.Count < relatedHandler.role.slotsToOperate && !DebugSettings.godMode)
								{
									turretTargeterGizmo.Disable("NotEnoughCannonCrew".Translate(Vehicle.LabelShort, relatedHandler.role.label));
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
						if (Prefs.DevMode)
						{
							yield return new Command_Action()
							{
								defaultLabel = $"Full Refill: {turret.gizmoLabel}",
								action = delegate()
								{
									if (turret.turretDef.ammunition is null)
									{
										turret.ReloadCannon(null);
									}
									else if (turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault() is ThingDef thingDef)
									{
										Thing ammo = ThingMaker.MakeThing(thingDef);
										ammo.stackCount = thingDef.stackLimit;
										Vehicle.inventory.innerContainer.TryAddOrTransfer(ammo);
										turret.ReloadCannon(thingDef);
									}
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
						
						foreach (VehicleHandler relatedHandler in Vehicle.GetAllHandlersMatch(HandlingTypeFlags.Cannon, turret.key))
						{
							if(relatedHandler.handlers.Count < relatedHandler.role.slotsToOperate && !DebugSettings.godMode)
							{
								turretCommand.Disable("NotEnoughCannonCrew".Translate(Vehicle.LabelShort, relatedHandler.role.label));
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
							if (Prefs.DevMode)
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
											Vehicle.inventory.innerContainer.TryAddOrTransfer(ammo);
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

		public void QueueTurret(TurretData turretData)
		{
			turretData.turret.queuedToFire = true;
			turretQueue.Add(turretData);
		}

		public void DequeueTurret(TurretData turretData)
		{
			turretData.turret.queuedToFire = false;
			turretQueue.RemoveAll(td => td.turret == turretData.turret);
		}

		private void ResolveTurretQueue()
		{
			for (int i = turretQueue.Count - 1; i >= 0; i--)
			{
				TurretData turretData = turretQueue[i];
				if (!turretData.turret.cannonTarget.IsValid || turretData.turret.shellCount <= 0)
				{
					DequeueTurret(turretData);
					continue;
				}
				if (turretData.turret.TurretRestricted || turretData.turret.OnCooldown || (!turretData.turret.IsManned && !DebugSettings.godMode))
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
		}

		public override void CompTick()
		{
			base.CompTick();
			ResolveTurretQueue();
			foreach (VehicleTurret turret in turrets)
			{
				turret.Tick();
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

		public override void PostGenerationSetup()
		{
			base.PostGenerationSetup();
			CreateTurretInstances();
			RevalidateTurrets();
		}

		public static VehicleTurret CreateTurret(VehiclePawn vehicle, VehicleTurret reference)
		{
			VehicleTurret newTurret = (VehicleTurret)Activator.CreateInstance(reference.GetType(), new object[] { vehicle, reference });
			newTurret.SetTarget(LocalTargetInfo.Invalid);
			newTurret.ResetCannonAngle();
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
				turret.childCannons = new List<VehicleTurret>();
				foreach (VehicleTurret cannon2 in turrets.Where(c => c.parentKey == turret.key))
				{
					cannon2.attachedTo = turret;
					turret.childCannons.Add(cannon2);
				}
			}
		}

		public void InitTurrets()
		{
			foreach (VehicleTurret turretProps in Props.turrets)
			{
				VehicleTurret matchingTurret = turrets.FirstOrDefault(turret => turret.key == turretProps.key);
				matchingTurret.Init(turretProps);
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref turrets, nameof(turrets), LookMode.Deep, ctorArgs: Vehicle);
			Scribe_Collections.Look(ref turretQueue, nameof(turretQueue), LookMode.Reference);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				RevalidateTurrets();
			}
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
				Scribe_Values.Look(ref shots, "shots");
				Scribe_Values.Look(ref ticksTillShot, "ticksTillShot");
				Scribe_References.Look(ref turret, "turret");
			}
		}
	}
}
