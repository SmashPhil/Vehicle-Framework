using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse.AI;
using Verse;
using SmashTools.Performance;

namespace Vehicles
{
	[NoProfiling]
	public class JobGiver_AIBreachWalls : JobGiver_VehicleAI
	{
		protected override bool TryFindShootingPosition(VehiclePawn vehicle, out IntVec3 position)
		{
			throw new NotImplementedException();
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			throw new NotImplementedException();
		}
	}
}
