using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public class AntiAirTargeter : BaseWorldTargeter
	{
		protected const float BaseFeedbackTexSize = 0.8f;

		protected Thing caster;
		protected Func<GlobalTargetInfo, float, bool> action;

		public AntiAirTargeter()
		{
			
		}

		public static AntiAirTargeter Instance { get; private set; }

		public override bool IsTargeting => action != null;

		public void BeginTargeting(Thing caster, Func<GlobalTargetInfo, float, bool> action, int origin, bool canTargetTiles, Texture2D mouseAttachment = null, bool closeWorldTabWhenFinished = false, Action onUpdate = null,
			Func<GlobalTargetInfo, List<FlightNode>, float, string> extraLabelGetter = null)
		{
			this.caster = caster;
			this.action = action;
			originOnMap = WorldHelper.GetTilePos(origin);
			this.canTargetTiles = canTargetTiles;
			this.mouseAttachment = mouseAttachment;
			this.closeWorldTabWhenFinished = closeWorldTabWhenFinished;
			this.onUpdate = onUpdate;
			OnStart();
		}

		public void BeginTargeting(VehiclePawn vehicle, Func<GlobalTargetInfo, float, bool> action, AerialVehicleInFlight aerialVehicle, bool canTargetTiles, Texture2D mouseAttachment = null, bool closeWorldTabWhenFinished = false, Action onUpdate = null,
			Func<GlobalTargetInfo, List<FlightNode>, float, string> extraLabelGetter = null)
		{
			this.action = action;
			this.canTargetTiles = canTargetTiles;
			this.mouseAttachment = mouseAttachment;
			this.closeWorldTabWhenFinished = closeWorldTabWhenFinished;
			this.onUpdate = onUpdate;
			OnStart();
		}

		public override void StopTargeting()
		{
			if (closeWorldTabWhenFinished)
			{
				CameraJumper.TryHideWorld();
			}
			action = null;
			canTargetTiles = false;
			mouseAttachment = null;
			closeWorldTabWhenFinished = false;
			onUpdate = null;
		}

		public override void ProcessInputEvents()
		{
			if (Event.current.type == EventType.MouseDown && IsTargeting)
			{
				if (Event.current.button == 0)
				{
					GlobalTargetInfo arg = CurrentTargetUnderMouse();
					Event.current.Use();
				}
				if (Event.current.button == 1)
				{
					SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
					StopTargeting();
					Event.current.Use();
				}
			}
			if (KeyBindingDefOf.Cancel.KeyDownEvent && IsTargeting)
			{
				SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
				StopTargeting();
				Event.current.Use();
			}
		}

		public override void TargeterOnGUI()
		{
			if (IsTargeting)
			{
				if (!Mouse.IsInputBlockedNow)
				{
					GlobalTargetInfo mouseTarget = CurrentTargetUnderMouse();

					Vector2 mousePosition = Event.current.mousePosition;
					Texture2D image = mouseAttachment ?? TexCommand.Attack;
					Rect position = new Rect(mousePosition.x + 8f, mousePosition.y + 8f, 32f, 32f);
					GUI.DrawTexture(position, image);
				}
			}
		}

		public override void TargeterUpdate()
		{
			if (IsTargeting)
			{

				onUpdate?.Invoke();
			}
		}

		public override void PostInit()
		{
			Instance = this;
		}
	}
}
