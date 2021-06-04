using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	[MustImplement("BeginTargeting")]
	public abstract class BaseTargeter
	{
		protected VehiclePawn vehicle;
		protected Action actionWhenFinished;
		protected Texture2D mouseAttachment;

		public abstract bool IsTargeting { get; }

		public abstract void StopTargeting();

		public abstract void ProcessInputEvents();

		public abstract void TargeterOnGUI();

		public abstract void TargeterUpdate();

		protected virtual LocalTargetInfo CurrentTargetUnderMouse()
		{
			if (!IsTargeting)
			{
				return LocalTargetInfo.Invalid;
			}
			LocalTargetInfo target = Verse.UI.MouseCell();
			return target;
		}

		public virtual void PostInit()
		{
		}
	}
}
