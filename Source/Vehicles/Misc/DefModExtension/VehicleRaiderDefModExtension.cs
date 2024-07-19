using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Vehicles
{
	public class VehicleRaiderDefModExtension : DefModExtension
	{
		public float pointMultiplier = 1;
		public bool techLevelRestricted = true;

		public HashSet<PawnsArrivalModeDef> arrivalModes;
	}
}
