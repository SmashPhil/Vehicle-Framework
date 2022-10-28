using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace Vehicles
{
	public abstract class Command_Turret : Command
	{
		protected const float AmmoWindowOffset = 5f;
		protected readonly Color DarkGrey = new Color(0.05f, 0.05f, 0.05f, 0.5f);

		public TargetingParameters targetingParams;
		public bool canReload;

		public VehicleTurret turret;
		protected Color alphaColorTicked = new Color(255, 255, 255, 0.5f);
		
		public override float GetWidth(float maxWidth)
		{
			return turret.turretDef.cooldown != null ? 279 : 210f;
		}

		public override bool GroupsWith(Gizmo other)
		{
			return other is Command_CooldownAction command_CooldownAction && command_CooldownAction.turret.GroupsWith(turret);
		}
	}
}
