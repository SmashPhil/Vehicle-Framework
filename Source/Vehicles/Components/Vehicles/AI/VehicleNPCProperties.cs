using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleNPCProperties
	{
		public bool runDownTargets = false;
		public float targetPositionRadius = 9.9f;
		public float targetAcquireRadius = 65f;
		public float targetKeepRadius = 65f;

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
