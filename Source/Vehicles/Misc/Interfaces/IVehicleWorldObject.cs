using System;
using System.Collections.Generic;
using Verse;

namespace Vehicles
{
	public interface IVehicleWorldObject : IThingHolder
	{
		public IEnumerable<VehiclePawn> Vehicles { get; }

		public IEnumerable<Pawn> DismountedPawns { get; }

		public bool CanDismount { get; }
	}
}
