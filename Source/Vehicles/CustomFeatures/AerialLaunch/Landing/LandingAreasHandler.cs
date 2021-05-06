using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class LandingAreasHandler : MapComponent
	{
		public List<LandingArea> landingAreas = new List<LandingArea>();
		public LandingAreasHandler(Map map) : base(map)
		{
			if (landingAreas is null)
			{
				landingAreas = new List<LandingArea>();
			}
		}
	}
}
