using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using SmashTools;
using Verse.AI;

namespace Vehicles
{
	public class VehicleNPCProperties
	{
		/// <summary>
		/// If targets get within minimum distance, vehicle will chase them down and run them over.
		/// </summary>
		public bool runDownTargets = false;
		public bool reverseWhileFleeing = true;

		/// <summary>
		/// Preferred distance from target based on max range of turrets. If <see cref="stopToShoot"/> is false, vehicle will ignore this.
		/// </summary>
		public float targetPositionRadiusPercent = 0.85f;
		public float targetAcquireRadius = 65f;
		public float targetKeepRadius = 65f;

		/// <summary>
		/// Vehicle will stop in place before shooting at targets.
		/// </summary>
		/// <remarks>Primarily for providing cover to dismounted raiders.</remarks>
		public bool stopToShoot = true;
		public float distanceWeight = 2.5f;

		/// <summary>
		/// Configuration for injecting vehicles into raids
		/// </summary>
		public VehicleRaidParamsDef raidParams;

		// TODO
		public VehicleStrategyDef strategy;

		/// <summary>
		/// Defines target priorities for an NPC vehicle. This is separate from turret target priorities, 
		/// which are defined in VehicleTurretDef. However, during raids the target priorities will override
		/// turret target priorities under certain conditions.
		/// </summary>
		public SimpleDictionary<TargetCategory, int> targets;
	}
}
