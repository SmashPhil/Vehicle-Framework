using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Vehicles
{
	public class JobGiver_ExitMapRandom : JobGiver_ExitMap
	{
		protected override bool TryFindGoodExitDest(VehiclePawn vehicle, out IntVec3 cell)
		{
			// TODO - Add sapper capabilities for a vehicle to blast its way out
			return CellFinderExtended.TryFindRandomExitSpot(vehicle, out cell);
		}
	}
}
