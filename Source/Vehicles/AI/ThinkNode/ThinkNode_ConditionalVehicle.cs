using Verse;
using Verse.AI;

namespace Vehicles
{
	public class ThinkNode_ConditionalVehicle : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			return pawn is VehiclePawn;
		}
	}
}
