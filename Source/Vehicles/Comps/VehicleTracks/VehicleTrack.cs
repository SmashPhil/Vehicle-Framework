using System.Collections.Generic;
using Verse;

namespace Vehicles
{
	//TODO - Add custom damage for tracks
	public struct VehicleTrack
	{
		public Pair<IntVec2, IntVec2> trackPoint;

		public List<ThingCategory> destroyableCategories; //REDO
	}
}
