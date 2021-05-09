using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class StrafeTargeter
	{
		private static float middleMouseDownTime;
		private static float ticksOpen;

		private VehiclePawn vehicle;
		private LaunchProtocol launchProtocol;
		private Action<IntVec3, IntVec3> action;
		private IntVec3 start;
		private IntVec3 end;
		private Action actionWhenFinished;
		private Texture2D mouseAttachment;
		private Func<LocalTargetInfo, bool> targetValidator;

		public bool ForcedTargeting { get; set; }

		public bool IsTargeting => action != null;

		public void BeginTargeting(VehiclePawn vehicle, LaunchProtocol launchProtocol, Map map, Action<IntVec3, IntVec3> action, Func<LocalTargetInfo, bool> targetValidator = null, Action actionWhenFinished = null, Texture2D mouseAttachment = null, bool forcedTargeting = false)
		{
			Current.Game.CurrentMap = map;
			BeginTargeting(vehicle, launchProtocol, action, targetValidator, actionWhenFinished, mouseAttachment);
			ForcedTargeting = forcedTargeting;
		}

		public void BeginTargeting(VehiclePawn vehicle, LaunchProtocol launchProtocol, Action<IntVec3, IntVec3> action, Func<LocalTargetInfo, bool> targetValidator = null, Action actionWhenFinished = null, Texture2D mouseAttachment = null, bool forcedTargeting = false)
		{
			this.vehicle = vehicle;
			this.launchProtocol = launchProtocol;
			this.action = action;
			this.actionWhenFinished = actionWhenFinished;
			this.mouseAttachment = mouseAttachment;
			this.targetValidator = targetValidator;
			ForcedTargeting = forcedTargeting;
		}

		public void StopTargeting()
		{
			if (actionWhenFinished != null)
			{
				actionWhenFinished();
				actionWhenFinished = null;
			}
			action = null;
			targetValidator = null;
			ticksOpen = 0;
			ForcedTargeting = false;
		}

		public void ProcessInputEvents()
		{
			if (IsTargeting)
			{
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					LocalTargetInfo localTargetInfo = CurrentTargetUnderMouse();
					if (action != null)
					{
						if (targetValidator != null)
						{
							if (localTargetInfo.IsValid && targetValidator(localTargetInfo))
							{
								if (start.IsValid)
								{
									end = localTargetInfo.Cell;
									action(start, end);
									StopTargeting();
								}
								else
								{
									start = localTargetInfo.Cell;
								}
							}
						}
						else if (localTargetInfo.IsValid)
						{
							action(start, end);
							StopTargeting();
						}
					}
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					Event.current.Use();
				}
				if ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || KeyBindingDefOf.Cancel.KeyDownEvent)
				{
					if (start.IsValid)
					{
						start = IntVec3.Invalid;
						Event.current.Use();
						return;
					}
					if (ForcedTargeting)
					{
						SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
						Messages.Message("MustTargetStrafe".Translate(), MessageTypeDefOf.RejectInput);
						Event.current.Use();
						return;
					}
					SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
					StopTargeting();
					Event.current.Use();
				}
			}
		}

		public void TargeterOnGUI()
		{
			if (IsTargeting)
			{
				if (start.IsValid)
				{
					GenDraw.DrawCircleOutline(start.ToVector3Shifted(), 1);
				}
				GenUI.DrawMouseAttachment(mouseAttachment ?? CompLaunchable.TargeterMouseAttachment);
			}
		}

		public void TargeterUpdate()
		{
			if (IsTargeting && action != null)
			{
				ticksOpen++;
				LocalTargetInfo localTargetInfo = CurrentTargetUnderMouse();
				if (localTargetInfo.IsValid)
				{
					//Draw Targeting Rect Here
				}
			}
		}

		private LocalTargetInfo CurrentTargetUnderMouse()
		{
			if (!IsTargeting)
			{
				return LocalTargetInfo.Invalid;
			}
			LocalTargetInfo target = Verse.UI.MouseCell();
			return target;
		}
	}
}
