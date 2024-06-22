using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class RoofDefPositionStateDefModExtension : DefModExtension
	{
		public LandingTargeter.PositionState state = LandingTargeter.PositionState.Invalid;
	}
}
