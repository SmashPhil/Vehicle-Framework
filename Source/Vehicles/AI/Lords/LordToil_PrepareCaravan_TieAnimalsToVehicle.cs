using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class LordToil_PrepareCaravan_TieAnimalsToVehicle : LordToil, IDebugLordMeetingPoint
	{
		protected IntVec3 meetingPoint;

		protected RotatingList<VehiclePawn> vehicles;

		public LordToil_PrepareCaravan_TieAnimalsToVehicle(IntVec3 meetingPoint)
		{
			this.meetingPoint = meetingPoint;
		}

		public IntVec3 MeetingPoint => meetingPoint;

		public VehiclePawn NextVehicle
		{
			get
			{
				if (vehicles.NullOrEmpty())
				{
					vehicles = lord.ownedPawns.Where(pawn => pawn is VehiclePawn).Cast<VehiclePawn>().ToRotatingList();
				}
				return vehicles.Next;
			}
		}

		public override void UpdateAllDuties()
		{
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				Pawn pawn = lord.ownedPawns[i];
				if (pawn.IsColonist || AnimalPenUtility.NeedsToBeManagedByRope(pawn))
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareVehicleCaravan_RopeAnimalsToVehicle, NextVehicle);
					pawn.mindState.duty.ropeeLimit = int.MaxValue;
				}
				else if (pawn is VehiclePawn)
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareVehicleCaravan_WaitVehicle);
				}
				else
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait, meetingPoint);
				}
			}
		}

		public override void LordToilTick()
		{
			if (Find.TickManager.TicksGame % 100 == 0)
			{
				bool tiedDown = true;
				foreach (Pawn pawn in lord.ownedPawns)
				{
					if (AnimalPenUtility.NeedsToBeManagedByRope(pawn))
					{
						if (!(pawn.roping?.RopedTo.Pawn is VehiclePawn vehicle) || (vehicle.GetLord() != lord))
						{
							tiedDown = false;
							break;
						}
					}
				}
				if (tiedDown)
				{
					lord.ReceiveMemo(MemoTrigger.AnimalsTied);
				}
			}
		}
	}
}
