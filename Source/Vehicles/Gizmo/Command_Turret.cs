using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace Vehicles
{
	public abstract class Command_Turret : Command
	{
		protected const float AmmoWindowOffset = 5f;
		protected readonly Color DarkGrey = new Color(0.05f, 0.05f, 0.05f, 0.5f);

		public TargetingParameters targetingParams;
		public bool canReload;

		public VehiclePawn vehicle;
		public VehicleTurret turret;

		protected float cachedGizmoWidth = -1;

		public float GizmoWidth
		{
			get
			{
				if (cachedGizmoWidth < 0)
				{
					cachedGizmoWidth = RecalculateWidth();
				}
				return cachedGizmoWidth;
			}
		}

		protected virtual float RecalculateWidth()
		{
			return 75f;
		}

		public override float GetWidth(float maxWidth)
		{
			return GizmoWidth;
		}

		public override void ProcessInput(Event @event)
		{
			base.ProcessInput(@event);
			SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
			FireTurrets();
		}

		public abstract void FireTurret(VehicleTurret turret);

		public virtual void FireTurrets()
		{
			if (!turret.groupKey.NullOrEmpty())
			{
				foreach (VehicleTurret groupTurret in turret.GroupTurrets)
				{
					FireTurret(groupTurret);
				}
			}
			else 
			{
				FireTurret(turret);
			}
		}

		public override bool GroupsWith(Gizmo other)
		{
			return other is Command_CooldownAction command_CooldownAction && command_CooldownAction.turret.GroupsWith(turret);
		}
	}
}
