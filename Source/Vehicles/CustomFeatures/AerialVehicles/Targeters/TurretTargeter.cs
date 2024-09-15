using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
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

		public bool TargeterValid
		{
			get
			{
				if (vehicle is null || vehicle.Map != Find.CurrentMap || vehicle.Destroyed)
				{
					return false;
				}
				if (Turret is null || Turret.ComponentDisabled)
				{
					return false;
				}
				if (!Find.Selector.IsSelected(vehicle))
				{
					return false;
				}
				return true;
			}
		}

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
				Turret.AlignToAngleRestricted(Turret.TurretRotation);
				Turret.SetTarget(LocalTargetInfo.Invalid);
			}
			StopTargeting();
		}

		public override void ProcessInputEvents()
		{
			if (!TargeterValid)
			{
				StopTargeting(true);
				return;
			}
			if (IsTargeting)
			{
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					Event.current.Use();
					if (action != null)
					{                      
						LocalTargetInfo obj = CurrentTargetUnderMouse();
						if (obj.Cell.InBounds(map) && TargetingHelper.TargetMeetsRequirements(Turret, obj))
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
			if (action != null)
			{
				if (TargetingHelper.TargetMeetsRequirements(Turret, CurrentTargetUnderMouse()))
				{
					Texture2D icon = mouseAttachment ?? TexCommand.Attack;
					GenUI.DrawMouseAttachment(icon);
				}
			}
		}

		public override void TargeterUpdate()
		{
			if (IsTargeting)
			{
				LocalTargetInfo mouseTarget = CurrentTargetUnderMouse();
				if (TargetingHelper.TargetMeetsRequirements(Turret, mouseTarget))
				{
					GenDraw.DrawTargetHighlight(mouseTarget);
					if (Turret.CurrentFireMode.spreadRadius > 1)
					{
						GenDraw.DrawRadiusRing(mouseTarget.Cell, Turret.CurrentFireMode.spreadRadius);
					}

					if (mouseTarget != Turret.vehicle)
					{
						Turret.AlignToAngleRestricted((float)Turret.TurretLocation.ToIntVec3().AngleToCell(mouseTarget.Cell));
					}
				}
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

		public override void PostInit()
		{
			Instance = this;
		}
	}
}
