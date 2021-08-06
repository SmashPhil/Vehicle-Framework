using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class CustomCostDefModExtension : DefModExtension
	{
		public List<VehicleDef> vehicles = new List<VehicleDef>();
		public int pathCost;

		public override IEnumerable<string> ConfigErrors()
		{
			if (vehicles.NullOrEmpty())
			{
				yield return "<field>vehicles</field> must have 1 or more VehicleDefs specified.";
			}
		}
	}
}
