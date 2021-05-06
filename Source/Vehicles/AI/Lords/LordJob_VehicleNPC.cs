using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
	public abstract class LordJob_VehicleNPC : LordJob
	{
		/// <summary>
		/// Slow down speed to maximum value. Useful when needing pawns to keep up or use vehicles as cover.
		/// </summary>
		public abstract float MaxVehicleSpeed { get; }
	}
}
