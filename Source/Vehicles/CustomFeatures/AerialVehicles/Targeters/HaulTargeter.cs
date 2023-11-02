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
	public class HaulTargeter : BaseTargeter
	{
		private Action<LocalTargetInfo> action;
		private TargetingParameters targetParams;
		private Map map;

		public static HaulTargeter Instance { get; private set; }

		public override bool IsTargeting => action != null;

		public static void BeginTargeting(TargetingParameters targetParams, Action<LocalTargetInfo> action, VehiclePawn vehicle, Action actionWhenFinished = null, Texture2D mouseAttachment = null)
		{
			Instance.action = action;
			Instance.targetParams = targetParams;
			Instance.vehicle = vehicle;
			Instance.actionWhenFinished = actionWhenFinished;
			Instance.mouseAttachment = mouseAttachment;
			Instance.map = vehicle.Map;
		}

		public override void StopTargeting()
		{
			if (actionWhenFinished != null)
			{
				Action action = actionWhenFinished;
				actionWhenFinished = null;
				action();
			}
			action = null;
		}

		public override void ProcessInputEvents()
		{
			ConfirmStillValid();
			if (IsTargeting)
			{
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					Event.current.Use();
					if (action != null)
					{                      
						LocalTargetInfo obj = CurrentTargetUnderMouse();
						if (obj.Cell.InBounds(map) && TargetMeetsRequirements(obj))
						{
							action(obj);
							SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
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

		public override void TargeterOnGUI()
		{
			if (action != null)
			{
				Texture2D icon = mouseAttachment ?? TexCommand.Attack;
				if (icon)
				{
					GenUI.DrawMouseAttachment(icon);
				}
			}
		}

		public override void TargeterUpdate()
		{
			if (IsTargeting)
			{
				LocalTargetInfo target = CurrentTargetUnderMouse();
				SimpleColor lineColor = SimpleColor.Red;
				if (TargetMeetsRequirements(target))
				{
					lineColor = SimpleColor.White;
				}
				Vector3 linePos = UI.MouseMapPosition();
				if (target.IsValid)
				{
					GenDraw.DrawTargetHighlight(target);
					linePos = target.CenterVector3;
				}
				GenDraw.DrawLineBetween(vehicle.DrawPos, linePos, lineColor);
			}
		}

		private void ConfirmStillValid()
		{
			if (vehicle is null || vehicle.Map != Find.CurrentMap || vehicle.Destroyed || !Find.Selector.IsSelected(vehicle))
			{
				StopTargeting();
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

		public bool TargetMeetsRequirements(LocalTargetInfo target)
		{
			if (target.HasThing && (target.Thing is Pawn || target.Thing.def.EverHaulable))
			{
				if (!target.Thing.Spawned || target.Thing.Destroyed)
				{
					return false;
				}
				return true;// vehicle.Map.reachability.CanReach(vehicle.Position, target, PathEndMode.Touch, TraverseMode.ByPawn, Danger.Deadly);
			}
			return false;
		}

		public override void PostInit()
		{
			Instance = this;
		}
	}
}
