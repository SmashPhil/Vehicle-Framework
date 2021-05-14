using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class LandingTargeter : BaseTargeter
	{
		public const int PingPongTickLength = 100;

		private static float middleMouseDownTime;
		private static float ticksOpen;

		private LaunchProtocol launchProtocol;
		private Action<LocalTargetInfo, Rot4> action;
		private Rot4 landingRotation;
		private LocalTargetInfo cachedTarget;
		private Func<LocalTargetInfo, bool> targetValidator;
		private bool allowRotating;

		public static LandingTargeter Instance { get; private set; }

		public bool ForcedTargeting { get; set; }

		public override bool IsTargeting => action != null;

		public void BeginTargeting(VehiclePawn vehicle, LaunchProtocol launchProtocol, Map map, Action<LocalTargetInfo, Rot4> action, Func<LocalTargetInfo, bool> targetValidator = null, Action actionWhenFinished = null, Texture2D mouseAttachment = null, bool allowRotating = false, bool forcedTargeting = false)
		{
			Current.Game.CurrentMap = map;
			BeginTargeting(vehicle, launchProtocol, action, targetValidator, actionWhenFinished, mouseAttachment, allowRotating);
			ForcedTargeting = forcedTargeting;
		}

		public void BeginTargeting(VehiclePawn vehicle, LaunchProtocol launchProtocol, Action<LocalTargetInfo, Rot4> action, Func<LocalTargetInfo, bool> targetValidator = null, Action actionWhenFinished = null, Texture2D mouseAttachment = null, bool allowRotating = false, bool forcedTargeting = false)
		{
			this.vehicle = vehicle;
			this.launchProtocol = launchProtocol;
			this.action = action;
			this.actionWhenFinished = actionWhenFinished;
			this.mouseAttachment = mouseAttachment;
			this.targetValidator = targetValidator;
			this.allowRotating = allowRotating;
			landingRotation = launchProtocol.landingProperties?.forcedRotation ?? Rot4.North;
			ForcedTargeting = forcedTargeting;
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
			ticksOpen = 0;
			ForcedTargeting = false;
		}

		public override void ProcessInputEvents()
		{
			HandleRotationShortcuts();
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
			{
				LocalTargetInfo localTargetInfo = CurrentTargetUnderMouse();
				if (action != null)
				{
					if (targetValidator != null)
					{
						if (targetValidator(localTargetInfo) && !MapHelper.VehicleBlockedInPosition(vehicle, Current.Game.CurrentMap, localTargetInfo.Cell, landingRotation))
						{
							action(localTargetInfo, landingRotation);
							StopTargeting();
						}
					}
					else if (localTargetInfo.IsValid)
					{
						action(localTargetInfo, landingRotation);
						StopTargeting();
					}
				}
				SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
				Event.current.Use();
			}
			if ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || KeyBindingDefOf.Cancel.KeyDownEvent)
			{
				if (ForcedTargeting)
				{
					SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
					Messages.Message("MustTargetLanding".Translate(), MessageTypeDefOf.RejectInput);
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
			DoExtraGuiControls();
			GenUI.DrawMouseAttachment(mouseAttachment ?? CompLaunchable.TargeterMouseAttachment);
		}

		public override void TargeterUpdate()
		{
			ticksOpen++;
			LocalTargetInfo localTargetInfo = CurrentTargetUnderMouse();
			if (localTargetInfo.IsValid)
			{
				IntVec3 cell = localTargetInfo.Cell;
				Vector3 position = new Vector3(cell.x, AltitudeLayer.Building.AltitudeFor(), cell.z).ToIntVec3().ToVector3Shifted();
				Color color = MapHelper.VehicleBlockedInPosition(vehicle, Current.Game.CurrentMap, localTargetInfo.Cell, landingRotation) ||
					(targetValidator != null && !targetValidator(localTargetInfo)) ? Designator_Place.CannotPlaceColor : Designator_Place.CanPlaceColor;
				color.a = (Mathf.PingPong(ticksOpen, PingPongTickLength / 1.5f) / PingPongTickLength) + 0.25f;
				GhostDrawer.DrawGhostThing(cell, landingRotation, vehicle.VehicleDef.buildDef, vehicle.VehicleDef.buildDef.graphic, color, AltitudeLayer.Blueprint);
			}
		}

		public void RecacheLandingPad(LocalTargetInfo target)
		{
			cachedTarget = target;
			IntVec2 size = vehicle.VehicleDef.Size;
		}

		public void DoExtraGuiControls()
		{
			if (allowRotating)
			{
				Rect winRect = new Rect(0, Verse.UI.screenHeight - 35 - 90f, 200f, 90f);
				Find.WindowStack.ImmediateWindow(73095, winRect, WindowLayer.GameUI, delegate
				{
					RotationDirection rotationDirection = RotationDirection.None;
					Text.Anchor = TextAnchor.MiddleCenter;
					Text.Font = GameFont.Medium;
					Rect rect = new Rect(winRect.width / 2f - 64f - 5f, 15f, 64f, 64f);
					if (Widgets.ButtonImage(rect, TexUI.RotLeftTex, true))
					{
						SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
						rotationDirection = RotationDirection.Counterclockwise;
						Event.current.Use();
					}
					Widgets.Label(rect, KeyBindingDefOf.Designator_RotateLeft.MainKeyLabel);
					Rect rect2 = new Rect(winRect.width / 2f + 5f, 15f, 64f, 64f);
					if (Widgets.ButtonImage(rect2, TexUI.RotRightTex, true))
					{
						SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
						rotationDirection = RotationDirection.Clockwise;
						Event.current.Use();
					}
					Widgets.Label(rect2, KeyBindingDefOf.Designator_RotateRight.MainKeyLabel);
					if (rotationDirection != RotationDirection.None)
					{
						landingRotation.Rotate(rotationDirection);
					}
					Text.Anchor = TextAnchor.UpperLeft;
					Text.Font = GameFont.Small;
				}, true, false, 1f);
			}
		}

		private void HandleRotationShortcuts()
		{
			if (allowRotating)
			{
				RotationDirection rotationDirection = RotationDirection.None;
				if (Event.current.button == 2)
				{
					if (Event.current.type == EventType.MouseDown)
					{
						Event.current.Use();
						middleMouseDownTime = Time.realtimeSinceStartup;
					}
					if (Event.current.type == EventType.MouseUp && Time.realtimeSinceStartup - middleMouseDownTime < 0.15f)
					{
						rotationDirection = RotationDirection.Clockwise;
					}
				}
				if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
				{
					rotationDirection = RotationDirection.Clockwise;
				}
				if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
				{
					rotationDirection = RotationDirection.Counterclockwise;
				}
				if (rotationDirection == RotationDirection.Clockwise)
				{
					SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
					landingRotation.Rotate(RotationDirection.Clockwise);
				}
				if (rotationDirection == RotationDirection.Counterclockwise)
				{
					SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
					landingRotation.Rotate(RotationDirection.Counterclockwise);
				}
			}
		}

		protected override LocalTargetInfo CurrentTargetUnderMouse()
		{
			LocalTargetInfo target = base.CurrentTargetUnderMouse();
			if (!cachedTarget.IsValid || cachedTarget != target)
			{
				RecacheLandingPad(target);
			}
			return target;
		}

		public override void PostInit()
		{
			Instance = this;
		}
	}
}
