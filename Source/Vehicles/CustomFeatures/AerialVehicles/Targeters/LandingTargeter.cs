using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles.World;
using Verse;
using Verse.Sound;

namespace Vehicles;

// TODO 1.7 - This whole class is a disaster, get rid of it!
public class LandingTargeter : BaseTargeter
{
	public const int PingPongTickLength = 100;

	public static readonly Color GhostOccupiedColor = new Color(1, 0.5f, 0.2f, 0.5f);

	private static float middleMouseDownTime;
	private static float framesOpen;

	private Map map;

	private Action<LocalTargetInfo, Rot4> action;
	private Rot4 landingRotation;
	private LocalTargetInfo cachedTarget;
	private Func<LocalTargetInfo, bool> targetValidator;
	private bool allowRotating;

	private Queue<Action> targeterQueue = new Queue<Action>();

	private static (IntVec3 startingCell, Rot4 rotation, bool result) restrictionCached;

	public static LandingTargeter Instance { get; private set; }

	public bool ForcedTargeting { get; private set; }

	private bool Paused { get; set; }

	public override bool IsTargeting => action != null;

	public void BeginTargetingAndFocusMap(VehiclePawn vehicle, [CanBeNull] Map map, Action<LocalTargetInfo, Rot4> action,
		Func<LocalTargetInfo, bool> targetValidator = null, Action actionWhenFinished = null,
		Texture2D mouseAttachment = null, bool allowRotating = false, bool forcedTargeting = false)
	{
		Current.Game.CurrentMap = map;
		BeginTargeting(vehicle, map, action, targetValidator,
			delegate () { Current.Game.CurrentMap = map; }, actionWhenFinished, mouseAttachment,
			allowRotating, forcedTargeting: forcedTargeting);
	}

	public void BeginTargeting(VehiclePawn vehicle, [CanBeNull] Map map, Action<LocalTargetInfo, Rot4> action,
		Func<LocalTargetInfo, bool> targetValidator = null, Action actionOnStart = null,
		Action actionWhenFinished = null, Texture2D mouseAttachment = null,
		bool allowRotating = false, bool forcedTargeting = false)
	{
		this.vehicle = vehicle;
		this.map = map;

		targeterQueue.Enqueue(delegate
		{
			actionOnStart?.Invoke();
			this.vehicle = vehicle;
			this.action = action;
			this.actionWhenFinished = actionWhenFinished;
			this.mouseAttachment = mouseAttachment;
			this.targetValidator = targetValidator;
			this.allowRotating = allowRotating;
			landingRotation =
				vehicle.CompVehicleLauncher.launchProtocol
				 .GetProperties(LaunchProtocol.LaunchType.Landing, landingRotation)?.forcedRotation ??
				Rot4.North;
			ForcedTargeting = forcedTargeting;
			ResetRestrictionCache();
		});

		TryStartNextTargeter();
		OnStart();
	}

	public override void StopTargeting()
	{
		if (actionWhenFinished != null)
		{
			actionWhenFinished();
			actionWhenFinished = null;
		}
		map = null;
		action = null;
		targetValidator = null;
		framesOpen = 0;
		ForcedTargeting = false;

		TryStartNextTargeter();
	}

	private void TryStartNextTargeter()
	{
		if (!IsTargeting && targeterQueue.Count > 0)
		{
			Action beginTargeting = targeterQueue.Dequeue();
			beginTargeting();
		}
	}

	private static void ResetRestrictionCache()
	{
		restrictionCached = (IntVec3.Invalid, Rot4.Invalid, true);
	}

	private void CheckStillValid()
	{
		if (map != null && Current.Game.CurrentMap != map)
		{
			TryCancelTargeter();
		}
	}

	private void TryCancelTargeter()
	{
		if (ForcedTargeting)
		{
			Paused = true;
			Event.current.Use();
			Dialog_MessageBox messageBox = Dialog_MessageBox.CreateConfirmation(
				"VF_ConfirmationCancelLanding".Translate(), delegate ()
				{
					CaravanHelper.MakeVehicleCaravan([vehicle], vehicle.Faction, map.Tile, addToWorldPawnsIfNotAlready: true);
					StopTargeting();
					Paused = false;
				}, delegate ()
				{
					Current.Game.CurrentMap = map;
					CameraJumper.TryHideWorld();
					Paused = false;
				});
			Find.WindowStack.Add(messageBox);
			return;
		}
		SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
		StopTargeting();
		Event.current.Use();
	}

	public PositionState GetPosState(LocalTargetInfo localTargetInfo, bool drawRestriction = false)
	{
		IntVec3 cell = localTargetInfo.Cell;
		Vector3 position = new Vector3(cell.x, AltitudeLayer.Building.AltitudeFor(), cell.z)
		 .ToIntVec3().ToVector3Shifted();
		Map map = Current.Game.CurrentMap;
		//VehiclePawn vehicleAtPos = MapHelper.VehicleInPosition(vehicle, Current.Game.CurrentMap, cell, landingRotation);
		bool invalidPosition = !localTargetInfo.IsValid ||
			(targetValidator != null && !targetValidator(localTargetInfo));
		invalidPosition |=
			MapHelper.ImpassableOrVehicleBlocked(vehicle, map, localTargetInfo.Cell, landingRotation);
		bool obstructed =
			MapHelper.NonStandableOrVehicleBlocked(vehicle, map, localTargetInfo.Cell, landingRotation);
		bool restricted = false;
		if (vehicle.CompVehicleLauncher.launchProtocol
			 .GetProperties(LaunchProtocol.LaunchType.Landing, landingRotation)
			?.restriction is { } launchRestriction)
		{
			if (restrictionCached.startingCell != cell || restrictionCached.rotation != landingRotation)
			{
				bool result = !launchRestriction.CanStartProtocol(vehicle, Current.Game.CurrentMap, cell,
					landingRotation);
				restrictionCached = (cell, landingRotation, result);
			}
			if (drawRestriction)
			{
				launchRestriction.DrawRestrictionsTargeter(vehicle, Current.Game.CurrentMap, cell,
					landingRotation);
			}
			restricted = restrictionCached.result;
		}
		if (invalidPosition || restricted)
			return PositionState.Invalid;
		if (obstructed)
			return PositionState.Obstructed;

		if (vehicle.CompVehicleLauncher.Props.canRoofPunch)
		{
			CellRect occupiedRect = GenAdj.OccupiedRect(cell, landingRotation, vehicle.VehicleDef.Size);
			foreach (IntVec3 occupiedCell in occupiedRect)
			{
				if (RoofPunchOverride(map, occupiedCell, out int stateInt))
				{
					return (PositionState)stateInt;
				}
				if (occupiedCell.GetRoof(map) is { } roofDef)
				{
					if (roofDef.GetModExtension<RoofDefPositionStateDefModExtension>() is { } roofDefPosition)
					{
						return roofDefPosition.state;
					}
					if (roofDef.isThickRoof)
					{
						return PositionState.Invalid;
					}
					return PositionState.Obstructed;
				}
			}
		}
		return PositionState.Valid;
	}

	/// <summary>
	/// Hook for modders to patch with no VehicleFramework types. Returns the int value of <see cref="PositionState"/> at <see cref="cell"/>
	/// </summary>
	private bool RoofPunchOverride(Map map, IntVec3 cell, out int stateInt)
	{
		stateInt = 0;
		return false;
	}

	public override void ProcessInputEvents()
	{
		HandleRotationShortcuts();
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			LocalTargetInfo localTargetInfo = CurrentTargetUnderMouse();
			if (action != null && localTargetInfo.Cell.InBounds(Current.Game.CurrentMap))
			{
				if (GetPosState(localTargetInfo) != PositionState.Invalid)
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					action(localTargetInfo, landingRotation);
					StopTargeting();
				}
				else
				{
					SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
				}
			}
			Event.current.Use();
		}
		if ((Event.current.type == EventType.MouseDown && Event.current.button == 1) ||
			KeyBindingDefOf.Cancel.KeyDownEvent)
		{
			TryCancelTargeter();
		}
	}

	public override void TargeterOnGUI()
	{
		if (Paused)
			return;

		DoExtraGuiControls();
		GenUI.DrawMouseAttachment(mouseAttachment ?? CompLaunchable.TargeterMouseAttachment);
	}

	public override void TargeterUpdate()
	{
		if (Paused)
			return;

		framesOpen++;
		LocalTargetInfo localTargetInfo = CurrentTargetUnderMouse();
		if (localTargetInfo.IsValid)
		{
			Color color = GhostDrawerColor(GetPosState(localTargetInfo, true));
			color.a = Mathf.PingPong(framesOpen, PingPongTickLength / 1.5f) / PingPongTickLength + 0.25f;
			GhostDrawer.DrawGhostThing(localTargetInfo.Cell, landingRotation, vehicle.VehicleDef.buildDef,
				vehicle.VehicleDef.buildDef.graphic,
				color, AltitudeLayer.Blueprint);
		}
		if (framesOpen % 60 == 0) // Reset every second
		{
			ResetRestrictionCache();
		}
		CheckStillValid();
	}

	public static Color GhostDrawerColor(PositionState state)
	{
		return state switch
		{
			PositionState.Invalid => Designator_Place.CannotPlaceColor,
			PositionState.Obstructed => GhostOccupiedColor,
			_ => Designator_Place.CanPlaceColor,
		};
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
			Rect winRect = new Rect(0, UI.screenHeight - 35 - 90f, 200f, 90f);
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
				if (Event.current.type == EventType.MouseUp &&
					Time.realtimeSinceStartup - middleMouseDownTime < 0.15f)
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

	public enum PositionState
	{
		Invalid = 0,
		Obstructed = 1,
		Valid = 2
	}
}