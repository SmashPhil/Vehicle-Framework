using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Vehicles
{
	public class JobGiver_ExitMapBest : JobGiver_ExitMap
	{
		protected override bool TryFindGoodExitDest(VehiclePawn vehicle, out IntVec3 cell)
		{
			return CellFinderExtended.TryFindBestExitSpot(vehicle, out cell, TraverseMode.ByPawn);
		}
	}
}
