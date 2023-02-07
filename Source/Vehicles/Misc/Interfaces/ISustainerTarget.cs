using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace Vehicles
{
	/// <summary>
	/// Attach to objects containing vehicle that should be valid for sustainer events
	/// </summary>
	public interface ISustainerTarget
	{
		public TargetInfo Target { get; }

		public MaintenanceType MaintenanceType { get; }
	}
}
