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
	public class CompCannons : VehicleAIComp
	{
		/// PARAMS => (# Shots Fired, VehicleTurret, tickCount}
		private List<TurretData> multiFireCannon = new List<TurretData>();

		private List<VehicleTurret> cannons = new List<VehicleTurret>();

		public CompProperties_Cannons Props => (CompProperties_Cannons)props;

		public VehiclePawn Vehicle => parent as VehiclePawn;

		public bool WeaponStatusOnline => !Vehicle.Downed && !Vehicle.Dead; //REDO - Add vehicle component health as check

		public float MinRange => Cannons.Max(x => x.turretDef.minRange);

		public float MaxRangeGrouped
		{
			get
			{
				IEnumerable<VehicleTurret> cannonRange = Cannons.Where(x => x.turretDef.maxRange <= GenRadial.MaxRadialPatternRadius);
				if(!cannonRange.NotNullAndAny())
				{
					return (float)Math.Floor(GenRadial.MaxRadialPatternRadius);
				}
				return cannonRange.Min(x => x.turretDef.maxRange);
			}
		}

		public List<VehicleTurret> Cannons
		{
			get
			{
				if (cannons is null)
				{
					cannons = new List<VehicleTurret>();
				}
				return cannons;
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
				cannons.RemoveAll(t => t.key == newTurret.key);
				cannons.Add(newTurret);
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
				VehicleTurret resultingHandler = cannons.FirstOrDefault(c => c.key == cannon.key);
				if(resultingHandler is null)
				{
					Log.Error($"Unable to locate {cannon.key} in cannonList for removal. Is Key missing on upgraded cannon?");
				}
				cannons.Remove(resultingHandler);
			}
		}

		public override void PostDraw()
		{
			Cannons.ForEach(c => c.Draw());
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (Cannons.Count > 0)
			{
				int turretNumber = 0;
				var rotatables = Cannons.Where(x => x.turretDef.weaponType == TurretType.Rotatable);
				var statics = Cannons.Where(x => x.turretDef.weaponType == TurretType.Static);
				if (rotatables.NotNullAndAny())
				{
					foreach (VehicleTurret turret in rotatables)
					{
						if (turret.manualTargeting)
						{
							Command_TargeterCooldownAction turretCannons = new Command_TargeterCooldownAction
							{
								turret = turret,
								defaultLabel = !string.IsNullOrEmpty(turret.gizmoLabel) ? turret.gizmoLabel : $"{turret.turretDef.LabelCap} {turretNumber}",
								icon = turret.GizmoIcon,
								iconDrawScale = turret.turretDef.gizmoIconScale
							};
							if (!string.IsNullOrEmpty(turret.turretDef.gizmoDescription))
							{
								turretCannons.defaultDesc = turret.turretDef.gizmoDescription;
							}
							turretCannons.targetingParams = new TargetingParameters
							{
								//Buildings, Things, Animals, Humans, and Mechs default to targetable
								canTargetLocations = true
							};
							turretNumber++;
							foreach (VehicleHandler relatedHandler in Vehicle.GetAllHandlersMatch(HandlingTypeFlags.Cannon, turret.key))
							{
								if (relatedHandler.handlers.Count < relatedHandler.role.slotsToOperate && !DebugSettings.godMode)
								{
									turretCannons.Disable("NotEnoughCannonCrew".Translate(Vehicle.LabelShort, relatedHandler.role.label));
									break;
								}
							}
							yield return turretCannons;
							if (Vehicle.Faction != Faction.OfPlayer)
							{
								turretCannons.Disable("CannotOrderNonControlled".Translate());
							}
							//(bool enabled = turret.TurretEnabled(Vehicle.VehicleDef, TurretDisableType.Always);
							//if (disabled)
							//{
							//	turretCannons.Disable(reason);
							//}
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
								turrets = new List<VehicleTurret>() { turret },
								defaultLabel = !string.IsNullOrEmpty(turret.gizmoLabel) ? turret.gizmoLabel : $"{turret.turretDef.LabelCap} {turretNumber}",
								icon = turret.GizmoIcon,
								iconDrawScale = turret.turretDef.gizmoIconScale
							};
							turretCommand.PostVariablesInit();
							turretNumber++;
							newCommand = true;
							if (!string.IsNullOrEmpty(turret.turretDef.gizmoDescription))
							{
								turretCommand.defaultDesc = turret.turretDef.gizmoDescription;
							}
						}
						else
						{
							turretCommand.turrets.Add(turret);
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
			multiFireCannon.Add(turretData);
		}

		public void DequeueTurret(TurretData turretData)
		{
			turretData.turret.queuedToFire = false;
			multiFireCannon.RemoveAll(td => td.turret == turretData.turret);
		}

		private void ResolveCannons()
		{
			if (multiFireCannon?.Count > 0)
			{
				for (int i = 0; i < multiFireCannon.Count; i++)
				{
					TurretData turretData = multiFireCannon[i];
					if (!turretData.turret.cannonTarget.IsValid || (turretData.turret.shellCount <= 0 && !DebugSettings.godMode))
					{
						DequeueTurret(turretData);
						continue;
					}
					if (turretData.turret.OnCooldown)
					{
						turretData.turret.SetTarget(LocalTargetInfo.Invalid);
						DequeueTurret(turretData);
						continue;
					}

					multiFireCannon[i].turret.AlignToTargetRestricted();
					if (multiFireCannon[i].ticksTillShot <= 0)
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
					multiFireCannon[i] = turretData;
				}
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			ResolveCannons();
			foreach (VehicleTurret turret in Cannons)
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
			foreach(VehicleTurret cannon in Cannons)
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
			InitializeTurrets();
			TurretSetup();
		}

		public static VehicleTurret CreateTurret(VehiclePawn vehicle, VehicleTurret reference)
		{
			VehicleTurret newTurret = (VehicleTurret)Activator.CreateInstance(reference.GetType(), new object[] { vehicle, reference });
			newTurret.SetTarget(LocalTargetInfo.Invalid);
			newTurret.ResetCannonAngle();
			return newTurret;
		}

		private void InitializeTurrets()
		{
			if (Props.turrets.NotNullAndAny())
			{
				foreach(VehicleTurret cannon in Props.turrets)
				{
					try
					{
						VehicleTurret newTurret = CreateTurret(Vehicle, cannon);
						cannons.Add(newTurret);
					}
					catch (Exception ex)
					{
						SmashLog.Error($"Exception thrown while attempting to generate <text>{cannon.turretDef.label}</text> for <text>{Vehicle.Label}</text>. Exception=\"{ex.Message}\"");
					}
				}
				if(Cannons.Select(x => x.key).GroupBy(y => y).NotNullAndAny(key => key.Count() > 1))
				{
					Log.Warning("Duplicate VehicleTurret key has been found. These are intended to be unique.");
				}
			}
		}

		public void TurretSetup()
		{
			foreach (VehicleTurret cannon in Cannons)
			{
				if(!string.IsNullOrEmpty(cannon.key))
				{
					cannon.childCannons = new List<VehicleTurret>();
					foreach (VehicleTurret cannon2 in Cannons.Where(c => c.parentKey == cannon.key))
					{
						cannon2.attachedTo = cannon;
						cannon.childCannons.Add(cannon2);
					}
				}
				cannon.vehicle = Vehicle;
			}
			multiFireCannon ??= new List<TurretData>();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref cannons, "cannons", LookMode.Deep);
			Scribe_Collections.Look(ref multiFireCannon, "multiFireCannon", LookMode.Reference);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				TurretSetup();
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
