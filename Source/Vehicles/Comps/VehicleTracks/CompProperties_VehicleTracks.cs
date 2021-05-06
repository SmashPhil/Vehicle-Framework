using System.Collections.Generic;
using Verse;

namespace Vehicles
{
	//[HeaderTitle(Label = "VehicleVehicleTracks", Translate = true)]
	public class CompProperties_VehicleTracks : CompProperties
	{
		public List<VehicleTrack> tracks = new List<VehicleTrack>();

		public CompProperties_VehicleTracks()
		{
			compClass = typeof(CompVehicleTracks);
		}
	}
}
