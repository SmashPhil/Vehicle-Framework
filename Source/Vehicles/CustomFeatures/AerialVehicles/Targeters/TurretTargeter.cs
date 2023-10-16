using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class TurretTargeter : BaseTargeter
	{
		private Action<LocalTargetInfo> action;
		private TargetingParameters targetParams;
		private Map map;

		public static TurretTargeter Instance { get; private set; }

		public static VehicleTurret Turret { get; private set; }

		public override bool IsTargeting => action != null;

		public static void BeginTargeting(TargetingParameters targetParams, Action<LocalTargetInfo> action, VehicleTurret turret, Action actionWhenFinished = null, Texture2D mouseAttachment = null)
		{
			Instance.action = action;
			Instance.targetParams = targetParams;
			Instance.vehicle = turret.vehicle;
			Turret = turret;
			Turret.SetTarget(LocalTargetInfo.Invalid);
			Instance.actionWhenFinished = actionWhenFinished;
			Instance.mouseAttachment = mouseAttachment;
			Instance.map = turret.vehicle.Map;

			Turret.StartTicking();
		}

		public override void StopTargeting()
		{
			if (actionWhenFinished != null)
			{
				Action action = actionWhenFinished;
				actionWhenFinished = null;
				action();
			}
			Turret = null;
			action = null;
		}

		public void StopTargeting(bool canceled)
		{
			if (canceled && Turret != null)
			{
				Turret.AlignToAngleRestricted(Turret.TurretRotationUncorrected);
			}
			StopTargeting();
		}

		public override void ProcessInputEvents()
		{
			ConfirmStillValid();
			if(IsTargeting)
			{
				if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					Event.current.Use();
					if(action != null)
					{                      
						LocalTargetInfo obj = CurrentTargetUnderMouse();
						if(obj.Cell.InBounds(map) && TargetMeetsRequirements(Turret, obj))
						{
							action(obj);
							SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
							StopTargeting(false);
						}
						else
						{
							SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
						}
					}
					
				}
				if ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || KeyBindingDefOf.Cancel.KeyDownEvent)
				{
					StopTargeting(true);
					SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
					Event.current.Use();
				}
			}
		}

		public override void TargeterOnGUI()
		{
			if(action != null)
			{
				float distance = (Turret.TurretLocation.ToIntVec3() - CurrentTargetUnderMouse().Cell).LengthHorizontal;
				if (TargetMeetsRequirements(Turret, CurrentTargetUnderMouse()))
				{
					Texture2D icon = mouseAttachment ?? TexCommand.Attack;
					GenUI.DrawMouseAttachment(icon);
				}
			}
		}

		public override void TargeterUpdate()
		{
			if(IsTargeting)
			{
				float distance = (Turret.TurretLocation.ToIntVec3() - CurrentTargetUnderMouse().Cell).LengthHorizontal;
				if (TargetMeetsRequirements(Turret, CurrentTargetUnderMouse()))
				{
					GenDraw.DrawTargetHighlight(CurrentTargetUnderMouse());

					if (CurrentTargetUnderMouse() != Turret.vehicle)
					{
						Turret.AlignToAngleRestricted((float)Turret.TurretLocation.ToIntVec3().AngleToCell(CurrentTargetUnderMouse().Cell, map));
					}
				}
				//REDO Radius Circle
				//if (Cannon.MinRange > 0)
				//{
				//	GenDraw.DrawRadiusRing(Cannon.TurretLocation.ToIntVec3(), Cannon.turretDef.minRange, Color.red);
				//}
				//if (Cannon.turretDef.maxRange <= GenRadial.MaxRadialPatternRadius)
				//{
				//	GenDraw.DrawRadiusRing(Cannon.TurretLocation.ToIntVec3(), Cannon.MaxRange, Color.white);
				//}
			}
		}

		private void ConfirmStillValid()
		{
			if (vehicle is null || (vehicle.Map != Find.CurrentMap || vehicle.Destroyed || !Find.Selector.IsSelected(vehicle)))
			{
				StopTargeting(true);
			}
		}

		protected override LocalTargetInfo CurrentTargetUnderMouse()
		{
			if (!IsTargeting)
			{
				return LocalTargetInfo.Invalid;
			}
			return GenUI.TargetsAtMouse(targetParams).FirstOrFallback(LocalTargetInfo.Invalid);
		}

		public static bool TargetMeetsRequirements(VehicleTurret turret, LocalTargetInfo obj)
		{
			float distance = (turret.TurretLocation.ToIntVec3() - obj.Cell).LengthHorizontal;
			bool los = false;
			if (obj.HasThing)
			{
				if (!obj.Thing.Spawned || obj.Thing.Destroyed)
				{
					return false;
				}
				los = GenSight.LineOfSightToThing(turret.TurretLocation.ToIntVec3(), obj.Thing, turret.vehicle.Map);
			}
			else
			{
				los = GenSight.LineOfSight(turret.TurretLocation.ToIntVec3(), obj.Cell, turret.vehicle.Map);
			}
			if (distance < turret.MinRange || (turret.MaxRange > 0 && distance >= turret.MaxRange))
			{
				return false;
			}
			if (!turret.AngleBetween(obj.CenterVector3))
			{
				return false;
			}
			ThingDef projectileDef;
			if (turret.turretDef.genericAmmo)
			{
				projectileDef = turret.turretDef.projectile;
			}
			else
			{
				projectileDef = turret.loadedAmmo?.projectileWhenLoaded;
			}
			if (projectileDef != null && projectileDef.projectile.flyOverhead)
			{
				return true;
			}
			return los;
		}

		public override void PostInit()
		{
			Instance = this;
		}
	}
}
