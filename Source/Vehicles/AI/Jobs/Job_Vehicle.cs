using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class Job_Vehicle : Job
	{
		public VehicleHandler handler;

		public Job_Vehicle()
		{
		}

		public Job_Vehicle(JobDef def) : base(def)
		{
		}

		public Job_Vehicle(JobDef def, LocalTargetInfo targetA) : base(def, targetA, null)
		{
		}

		public Job_Vehicle(JobDef def, LocalTargetInfo targetA, LocalTargetInfo targetB) : base(def, targetA, targetB)
		{
		}

		public Job_Vehicle(JobDef def, LocalTargetInfo targetA, LocalTargetInfo targetB, LocalTargetInfo targetC) : base(def, targetA, targetB, targetC)
		{
		}

		public Job_Vehicle(JobDef def, LocalTargetInfo targetA, int expiryInterval, bool checkOverrideOnExpiry = false) : base(def, targetA, expiryInterval, checkOverrideOnExpiry)
		{
		}

		public Job_Vehicle(JobDef def, int expiryInterval, bool checkOverrideOnExpiry = false) : base(def, expiryInterval, checkOverrideOnExpiry)
		{
		}
	}
}
