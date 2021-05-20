using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	[MustImplement("BeginTargeting")]
	public abstract class BaseWorldTargeter
	{
		protected VehiclePawn vehicle;
		protected AerialVehicleInFlight aerialVehicle;
		protected Vector3 originOnMap;
		protected Action actionWhenFinished;
		protected Action onUpdate;
		protected Texture2D mouseAttachment;
		protected bool canTargetTiles;
		public bool closeWorldTabWhenFinished;

		public abstract bool IsTargeting { get; }

		public abstract void RegisterActionOnTile(int tile, AerialVehicleArrivalAction arrivalAction);

		public abstract void StopTargeting();

		public abstract void ProcessInputEvents();

		public abstract void TargeterOnGUI();

		public abstract void TargeterUpdate();

		protected virtual GlobalTargetInfo CurrentTargetUnderMouse()
		{
			if (!IsTargeting)
			{
				return GlobalTargetInfo.Invalid;
			}
			List<WorldObject> list = GenWorldUI.WorldObjectsUnderMouse(Verse.UI.MousePositionOnUI);
			if (list.Any())
			{
				return list[0];
			}
			if (!canTargetTiles)
			{
				return GlobalTargetInfo.Invalid;
			}
			int num = GenWorld.MouseTile(false);
			if (num >= 0)
			{
				return new GlobalTargetInfo(num);
			}
			return GlobalTargetInfo.Invalid;
		}

		public virtual void PostInit()
		{
		}
	}
}
