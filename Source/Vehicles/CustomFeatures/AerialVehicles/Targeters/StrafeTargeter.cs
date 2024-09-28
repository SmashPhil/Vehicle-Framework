using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class StrafeTargeter : BaseTargeter
	{
		private LaunchProtocol launchProtocol;
		private Action<IntVec3, IntVec3> action;
		private IntVec3 start;
		private IntVec3 end;
		private Func<LocalTargetInfo, bool> targetValidator;

		public static StrafeTargeter Instance { get; private set; }

		public bool ForcedTargeting { get; set; }

		public override bool IsTargeting => action != null;

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

			start = IntVec3.Invalid;
			end = IntVec3.Invalid;
			OnStart();
		}

		public override void StopTargeting()
		{
			if (actionWhenFinished != null)
			{
				actionWhenFinished();
				actionWhenFinished = null;
			}
			action = null;
			targetValidator = null;
			ForcedTargeting = false;
		}

		public override void ProcessInputEvents()
		{
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
			{
				LocalTargetInfo localTargetInfo = CurrentTargetUnderMouse();
				if (action != null)
				{
					if (localTargetInfo.IsValid && localTargetInfo.Cell != start && (targetValidator is null || targetValidator(localTargetInfo)))
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

		public override void TargeterOnGUI()
		{
			GenUI.DrawMouseAttachment(mouseAttachment ?? CompLaunchable.TargeterMouseAttachment);
		}

		public override void TargeterUpdate()
		{
			IntVec3 cell = CurrentTargetUnderMouse().Cell;
			if (cell.IsValid && (targetValidator is null || targetValidator(cell)))
			{
				GenDraw.DrawTargetHighlight(cell);
			}
			if (start.IsValid && cell != start)
			{
				Vector3 startPoint = start.ToVector3Shifted();
				Vector3 cellPoint = cell.ToVector3Shifted();
				GenDraw.DrawTargetHighlight(start);
				GenDraw.DrawLineBetween(startPoint, cellPoint, SimpleColor.Red);
				Vector3 startToEdge = startPoint.PointToEdge(Current.Game.CurrentMap, cellPoint.AngleToPoint(startPoint));
				GenDraw.DrawLineBetween(startToEdge, startPoint);
				Vector3 endToEdge = cellPoint.PointToEdge(Current.Game.CurrentMap, startPoint.AngleToPoint(cellPoint));
				GenDraw.DrawLineBetween(endToEdge, cellPoint);
			}

			LocalTargetInfo localTargetInfo = CurrentTargetUnderMouse();
			if (localTargetInfo.IsValid)
			{
				//Draw Targeting Rect Here
			}
		}

		public override void PostInit()
		{
			Instance = this;
		}
	}
}
