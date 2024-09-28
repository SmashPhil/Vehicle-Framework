using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static SmashTools.Debug;

namespace Vehicles
{
	public class ThinkNode_ConditionalHasTurret : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			VehiclePawn vehicle = pawn as VehiclePawn;
			// Should never reach this conditional if pawn is not a vehicle
			Assert(vehicle != null);
			return vehicle.CompVehicleTurrets != null && vehicle.CompVehicleTurrets.turrets.Count > 0;
		}
	}
}
