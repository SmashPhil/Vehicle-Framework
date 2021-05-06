using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class CannonTargeter
	{
		private Action<LocalTargetInfo> action;

		private Pawn caster;

		private TargetingParameters targetParams;

		private Action actionWhenFinished;

		private Texture2D mouseAttachment;

		private Map map;

		public VehicleTurret Cannon { get; private set; }

		public bool IsTargeting => action != null;

		public void BeginTargeting(TargetingParameters targetParams, Action<LocalTargetInfo> action, VehicleTurret cannon, Action actionWhenFinished = null, Texture2D mouseAttachment = null)
		{
			this.action = action;
			this.targetParams = targetParams;
			caster = cannon.vehicle;
			Cannon = cannon;
			Cannon.SetTarget(LocalTargetInfo.Invalid);
			this.actionWhenFinished = actionWhenFinished;
			this.mouseAttachment = mouseAttachment;
			map = cannon.vehicle.Map;
		}

		public static bool TargetMeetsRequirements(VehicleTurret cannon, LocalTargetInfo obj)
		{
			float distance = (cannon.TurretLocation.ToIntVec3() - obj.Cell).LengthHorizontal;
			bool los = false;
			if (obj.HasThing)
			{
				if (!obj.Thing.Spawned || obj.Thing.Destroyed)
				{
					return false;
				}
				los = GenSight.LineOfSightToThing(cannon.TurretLocation.ToIntVec3(), obj.Thing, cannon.vehicle.Map);
			}
			else
			{
				los = GenSight.LineOfSight(cannon.TurretLocation.ToIntVec3(), obj.Cell, cannon.vehicle.Map);
			}
			bool result = (distance >= cannon.MinRange && (distance < cannon.MaxRange || cannon.MaxRange <= -1))
						&& cannon.AngleBetween(obj.CenterVector3) && ((cannon.loadedAmmo?.projectileWhenLoaded?.projectile?.flyOverhead ?? false) || los);
			return result;
		}

		public void StopTargeting(bool canceled = true)
		{
			if(actionWhenFinished != null)
			{
				Action action = actionWhenFinished;
				actionWhenFinished = null;
				action();
			}
			if (canceled && Cannon != null)
			{
				Cannon.AlignToAngleRestricted(Cannon.TurretRotationUncorrected);
				//cannon.TurretRotation = cannon.currentRotation;
			}
			Cannon = null;
			action = null;
		}

		public void ProcessInputEvents()
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
						if(obj.Cell.InBounds(map) && TargetMeetsRequirements(Cannon, obj))
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
					StopTargeting();
					SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
					Event.current.Use();
				}
			}
		}

		public void TargeterOnGUI()
		{
			if(action != null)
			{
				float distance = (Cannon.TurretLocation.ToIntVec3() - CurrentTargetUnderMouse().Cell).LengthHorizontal;
				if (TargetMeetsRequirements(Cannon, CurrentTargetUnderMouse()))
				{
					Texture2D icon = mouseAttachment ?? TexCommand.Attack;
					GenUI.DrawMouseAttachment(icon);
				}
			}
		}

		public void TargeterUpdate()
		{
			if(IsTargeting)
			{
				float distance = (Cannon.TurretLocation.ToIntVec3() - CurrentTargetUnderMouse().Cell).LengthHorizontal;
				if (TargetMeetsRequirements(Cannon, CurrentTargetUnderMouse()))
				{
					GenDraw.DrawTargetHighlight(CurrentTargetUnderMouse());

					if(CurrentTargetUnderMouse() != Cannon.vehicle)
						Cannon.AlignToAngleRestricted((float)Cannon.TurretLocation.ToIntVec3().AngleToCell(CurrentTargetUnderMouse().Cell, map));
				}
				//REDO Radius Circle
				//if(cannon.MinRange > 0)
				//    GenDraw.DrawRadiusRing(cannon.TurretLocation.ToIntVec3(), cannon.turretDef.minRange, Color.red);
				//if(cannon.turretDef.maxRange <= GenRadial.MaxRadialPatternRadius)
				//    GenDraw.DrawRadiusRing(cannon.TurretLocation.ToIntVec3(), cannon.MaxRange, Color.white);
			}
		}

		private void ConfirmStillValid()
		{
			if(caster is null || (caster.Map != Find.CurrentMap || caster.Destroyed || !Find.Selector.IsSelected(caster)))
			{
				StopTargeting();
			}
		}

		private LocalTargetInfo CurrentTargetUnderMouse()
		{
			if(!IsTargeting)
				return LocalTargetInfo.Invalid;
			LocalTargetInfo localTarget = LocalTargetInfo.Invalid;
			using(IEnumerator<LocalTargetInfo> enumerator = GenUI.TargetsAtMouse(targetParams, false).GetEnumerator())
			{
				if(enumerator.MoveNext())
				{
					LocalTargetInfo localTarget2 = enumerator.Current;
					localTarget = localTarget2;
				}
			}
			return localTarget;
		}
	}
}
