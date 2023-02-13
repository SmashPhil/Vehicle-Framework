using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class Command_TargeterCooldownAction : Command_CooldownAction
	{
		public override void FireTurrets()
		{
			FireTurret(turret);
			if (!turret.groupKey.NullOrEmpty())
			{
				SmashLog.Warning($"Grouped Turrets is not yet supported for turrets of type <type>Rotatable</type>");
			}
		}

		public override void FireTurret(VehicleTurret turret)
		{
			if (turret.ReloadTicks <= 0)
			{
				turret.SetTarget(LocalTargetInfo.Invalid);
				TurretTargeter.BeginTargeting(targetingParams, delegate(LocalTargetInfo target)
				{
					turret.SetTarget(target);
					turret.ResetPrefireTimer();
				}, turret, null, null);
			}
		}
	}
}
