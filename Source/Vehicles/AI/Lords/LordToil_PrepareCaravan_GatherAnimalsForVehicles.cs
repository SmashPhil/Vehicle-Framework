using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public class LordToil_PrepareCaravan_GatherAnimalsForVehicles : LordToil_PrepareCaravan_GatherAnimals, IDebugLordMeetingPoint
	{
		public LordToil_PrepareCaravan_GatherAnimalsForVehicles(IntVec3 destinationPoint) : base(destinationPoint)
		{
		}

		public IntVec3 MeetingPoint => destinationPoint;

		public override void UpdateAllDuties()
		{
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				Pawn pawn = lord.ownedPawns[i];
				if (pawn.IsColonist || AnimalPenUtility.NeedsToBeManagedByRope(pawn))
				{
					pawn.mindState.duty = MakeRopeDuty();
					pawn.mindState.duty.ropeeLimit = ropeeLimit;
				}
				else if (pawn is VehiclePawn)
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareVehicleCaravan_WaitVehicle);
				}
				else
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait, destinationPoint);
				}
			}
		}
	}
}
